import os
import sys

# Папка, из которой запустили программу (нужна для относительных путей в аргументах)
ORIG_CWD = os.getcwd()

# Внутри exe obspy строит пути к своим файлам относительно текущей папки,
# поэтому в exe переходим в папку с распакованными файлами программы.
if getattr(sys, "frozen", False):
    os.chdir(sys._MEIPASS)

from obspy import UTCDateTime, read
from obspy.clients.fdsn import Client
import os
import json
import msgpack
import numpy as np
import zlib
from pathlib import Path
from typing import List, Dict, Any, Optional
import base64
import argparse, sys


# ============================================================================
# КОНСТАНТЫ И НАСТРОЙКИ
# ============================================================================

class SACConfig:
    """Конфигурация для обработки SAC файлов"""
    DEFAULT_CLIENT = "RESIF"  # FDSN клиент по умолчанию
    DEFAULT_OUTPUT_DIR = "SAC_PROCESSED"
    DEFAULT_CHANNELS = "BH?"  # Широкополосные сейсмические каналы

    # Значения по умолчанию для отсутствующих координат
    UNDEFINED_VALUE = -12345.0


# ============================================================================
# КЛАСС ДЛЯ ЗАГРУЗКИ И ОБРАБОТКИ ДАННЫХ
# ============================================================================

class SACDataProcessor:
    """Основной класс для работы с сейсмическими данными и SAC файлами"""

    def __init__(self, client_name: str = None, output_dir: str = None):
        """
        Инициализация процессора

        Args:
            client_name: имя FDSN клиента (RESIF, IRIS, etc.)
            output_dir: папка для сохранения результатов
        """
        self.client = Client(client_name or SACConfig.DEFAULT_CLIENT)
        self.output_dir = Path(output_dir or SACConfig.DEFAULT_OUTPUT_DIR)
        self.output_dir.mkdir(exist_ok=True)

    # ============================================================================
    # ЭТАП 1: ЗАГРУЗКА ДАННЫХ С FDSN СЕРВЕРА
    # ============================================================================

    def download_event_data(self,
                            event_time: str,
                            duration_seconds: int = 3600,
                            network: str = "*",
                            station: str = "*",
                            location: str = "*",
                            channel: str = None) -> Dict[str, Any]:

        print(f"[1/3] Запрос данных события {event_time}...")

        t0 = UTCDateTime(event_time)
        t1 = t0 + duration_seconds

        st = self.client.get_waveforms(
            network=network,
            station=station,
            location=location,
            channel=channel or SACConfig.DEFAULT_CHANNELS,
            starttime=t0,
            endtime=t1,
            attach_response=True
        )

        print(f"    Получено трасс: {len(st)}")

        # Координаты станций берём из метаданных сервера
        inventory = None
        try:
            print("    Запрос координат станций...")
            inventory = self.client.get_stations(
                network=network,
                station=station,
                location=location,
                channel=channel or SACConfig.DEFAULT_CHANNELS,
                starttime=t0,
                endtime=t1,
                level="channel"
            )
        except Exception as e:
            print(f"    Не удалось получить координаты станций: {e}")

        return self._process_stream_to_sac(st, event_time, inventory)

    # ============================================================================
    # ЭТАП 2: КОНВЕРТАЦИЯ В SAC ФОРМАТ
    # ============================================================================

    def _process_stream_to_sac(self, stream, event_time: str,
                               inventory=None) -> Dict[str, Any]:
        """
        Обрабатывает Stream, сохраняет SAC-файлы
        и формирует единую структуру события.

        Args:
            stream: Obspy Stream объект
            event_time: время события

        Returns:
            Словарь с информацией о событии и его записях
        """

        print("[2/3] Обработка сейсмических данных...")

        records = []

        # ============================================================
        # ПАРАМЕТРЫ СОБЫТИЯ
        # ============================================================

        event = {
            "id": f"event_{event_time}",
            "origin_time": "2012-08-14T02:59:38",
            "latitude": 49.8,
            "longitude": 145.064,
            "depth_km": 583.0,
            "magnitude": 7.7
        }

        # ============================================================
        # ОБРАБОТКА ТРАСС
        # ============================================================

        for tr in stream:
            try:
                # ----------------------------------------------------
                # Удаляем инструментальный отклик
                # ----------------------------------------------------

                tr.remove_response(output="VEL")

                # ----------------------------------------------------
                # Сохраняем координаты станции в SAC
                # ----------------------------------------------------

                tr.stats.sac = {}

                coords = None
                if inventory is not None:
                    try:
                        coords = inventory.get_coordinates(
                            tr.id, datetime=tr.stats.starttime)
                    except Exception:
                        coords = None

                if coords:
                    tr.stats.sac.stla = coords["latitude"]
                    tr.stats.sac.stlo = coords["longitude"]
                    tr.stats.sac.stel = coords["elevation"]
                else:
                    print(f"    Нет координат для {tr.id}")
                    tr.stats.sac.stla = SACConfig.UNDEFINED_VALUE
                    tr.stats.sac.stlo = SACConfig.UNDEFINED_VALUE
                    tr.stats.sac.stel = SACConfig.UNDEFINED_VALUE

                # ----------------------------------------------------
                # Сохраняем информацию о событии в SAC
                # ----------------------------------------------------

                tr.stats.sac.evla = event["latitude"]
                tr.stats.sac.evlo = event["longitude"]
                tr.stats.sac.evdp = event["depth_km"]
                tr.stats.sac.mag = event["magnitude"]

                tr.stats.sac.o = 0.0
                tr.stats.sac.b = 0.0
                tr.stats.sac.e = tr.stats.delta * tr.stats.npts

                # ----------------------------------------------------
                # Сохраняем SAC
                # ----------------------------------------------------

                filename = (
                    f"{tr.id}."
                    f"{tr.stats.starttime.strftime('%Y%m%dT%H%M%S')}.sac"
                )

                filepath = self.output_dir / filename

                tr.write(str(filepath), format="SAC")

                # ----------------------------------------------------
                # Формируем Record
                # ----------------------------------------------------

                record = self._extract_record(tr)
                records.append(record)

                print(f"    Сохранено: {filename}")

            except Exception as e:
                import traceback
                print(f"    Ошибка обработки {tr.id}: {e}")
                traceback.print_exc()

        print(f"    Всего обработано: {len(records)} записей")

        # ============================================================
        # ФИНАЛЬНАЯ СТРУКТУРА
        # ============================================================

        return {
            "event": event,
            "records": records
        }

    # ============================================================================
    # ЭТАП 3: ПАРСИНГ СУЩЕСТВУЮЩИХ SAC ФАЙЛОВ
    # ============================================================================

    def parse_existing_sac_files(self, sac_dir: str) -> Dict[str, Any]:
        """
        Парсит существующие SAC-файлы и формирует единую структуру события.
        """

        print(f"[Парсинг] SAC-файлы из {sac_dir}...")

        sac_dir = Path(sac_dir)
        records = []

        sac_files = sorted(p for p in sac_dir.iterdir() if p.suffix.lower() == ".sac")

        if not sac_files:
            print("    SAC файлы не найдены!")

            return {
                "event": {},
                "records": []
            }

        print(f"    Найдено файлов: {len(sac_files)}")

        event = None

        for sac_file in sac_files:
            try:
                st = read(str(sac_file))
                tr = st[0]

                # Получаем данные события только один раз
                if event is None:
                    event = self._extract_event_metadata(tr)

                # Получаем данные конкретной записи
                record = self._extract_record(tr)

                records.append(record)

                print(f"    Парсинг: {sac_file.name}")

            except Exception as e:
                print(f"    Ошибка парсинга {sac_file.name}: {e}")

        print(f"    Всего обработано записей: {len(records)}")

        return {
            "event": event or {},
            "records": records
        }

    def _extract_sac_metadata(self, trace, filepath: Path) -> Dict[str, Any]:
        """
        Извлекает метаданные из Obspy Trace

        Args:
            trace: Obspy Trace объект
            filepath: путь к файлу

        Returns:
            Словарь с метаданными
        """
        metadata = {
            'filename': filepath.name,
            'filepath': str(filepath),
            'network': trace.stats.network,
            'station': trace.stats.station,
            'location': trace.stats.location,
            'channel': trace.stats.channel,
            'starttime': str(trace.stats.starttime),
            'sampling_rate': trace.stats.sampling_rate,
            'npts': trace.stats.npts,
            'delta': trace.stats.delta,
        }

        # Извлекаем SAC заголовок если есть
        if hasattr(trace.stats, 'sac'):
            sac = trace.stats.sac

            # Координаты станции
            metadata.update({
                'stla': sac.stla if sac.stla != SACConfig.UNDEFINED_VALUE else None,
                'stlo': sac.stlo if sac.stlo != SACConfig.UNDEFINED_VALUE else None,
                'stel': sac.stel if sac.stel != SACConfig.UNDEFINED_VALUE else None,
            })

            # Координаты события
            metadata.update({
                'evla': sac.evla if hasattr(sac, 'evla') and sac.evla != SACConfig.UNDEFINED_VALUE else None,
                'evlo': sac.evlo if hasattr(sac, 'evlo') and sac.evlo != SACConfig.UNDEFINED_VALUE else None,
                'evdp': sac.evdp if hasattr(sac, 'evdp') else None,
                'mag': sac.mag if hasattr(sac, 'mag') else None,
            })

            # Временные параметры
            metadata.update({
                'sac_o': sac.o if hasattr(sac, 'o') else None,
                'sac_b': sac.b if hasattr(sac, 'b') else None,
                'sac_e': sac.e if hasattr(sac, 'e') else None,
            })

        # Добавляем первые N точек данных для предпросмотра
        metadata['data_preview'] = trace.data.tolist() if len(trace.data) > 0 else []

        return metadata

    def _extract_event_metadata(self, trace) -> Dict[str, Any]:
        """
        Извлекает параметры сейсмического события из SAC-заголовка.

        Args:
            trace: Obspy Trace объект

        Returns:
            Словарь с параметрами события
        """

        event = {
            "origin_time": None,
            "latitude": None,
            "longitude": None,
            "depth_km": None,
            "magnitude": None
        }

        if not hasattr(trace.stats, "sac"):
            return event

        sac = trace.stats.sac

        if hasattr(sac, "evla") and sac.evla != SACConfig.UNDEFINED_VALUE:
            event["latitude"] = sac.evla

        if hasattr(sac, "evlo") and sac.evlo != SACConfig.UNDEFINED_VALUE:
            event["longitude"] = sac.evlo

        if hasattr(sac, "evdp") and sac.evdp != SACConfig.UNDEFINED_VALUE:
            event["depth_km"] = sac.evdp

        if hasattr(sac, "mag") and sac.mag != SACConfig.UNDEFINED_VALUE:
            event["magnitude"] = sac.mag

        return event

    def _extract_record(self, trace) -> Dict[str, Any]:
        """
        Извлекает данные одной сейсмической записи.

        Одна запись = одна станция/канал + соответствующий временной ряд.
        """
        station_latitude = None
        station_longitude = None

        if hasattr(trace.stats, "coordinates"):
            station_latitude = trace.stats.coordinates.latitude
            station_longitude = trace.stats.coordinates.longitude
        elif hasattr(trace.stats, "sac"):
            lat = trace.stats.sac.get("stla")
            lon = trace.stats.sac.get("stlo")
            if lat is not None and lat != SACConfig.UNDEFINED_VALUE:
                station_latitude = lat
            if lon is not None and lon != SACConfig.UNDEFINED_VALUE:
                station_longitude = lon
        else:
            station_latitude = None
            station_longitude = None

        return {
            "network": trace.stats.network,
            "station": trace.stats.station,
            "location": trace.stats.location,
            "channel": trace.stats.channel,

            "station_latitude": station_latitude,
            "station_longitude": station_longitude,

            "signal": {
                "start_time": str(trace.stats.starttime),
                "sampling_rate": trace.stats.sampling_rate,
                "samples": trace.data.tolist()
            }
        }

    # ============================================================================
    # СЕРИАЛИЗАЦИЯ РЕЗУЛЬТАТОВ
    # ============================================================================

    def _convert_numpy_types(self, obj):
        """Рекурсивно конвертирует numpy типы в стандартные Python типы"""
        if isinstance(obj, dict):
            return {k: self._convert_numpy_types(v) for k, v in obj.items()}
        elif isinstance(obj, list):
            return [self._convert_numpy_types(item) for item in obj]
        elif isinstance(obj, np.generic):
            return obj.item()
        elif isinstance(obj, np.ndarray):
            return obj.tolist()
        elif hasattr(obj, 'dtype'):
            try:
                return float(obj)
            except:
                return str(obj)
        else:
            return obj

    def save_to_optimized_json(self,
                               event_data: Dict[str, Any],
                               output_name: str = "seismic_event",
                               include_data_samples: int = 10_000_000) -> str:
        """
        Сохраняет данные сейсмического события в оптимизированный JSON.

        Args:
            event_data:
                Словарь структуры:
                {
                    "event": {...},
                    "records": [...]
                }

            output_name:
                Имя выходного файла без расширения.

            include_data_samples:
                Максимальное количество отсчётов,
                сохраняемых для каждой записи.

        Returns:
            Путь к сжатому JSON-файлу.
        """

        print("[Сериализация] Сохранение данных события...")

        event = event_data["event"]
        records = event_data["records"]

        optimized_records = []

        for record in records:

            # --------------------------------------------------------
            # Копируем метаданные записи
            # --------------------------------------------------------

            optimized_record = {
                "network": record["network"],
                "station": record["station"],
                "location": record["location"],
                "channel": record["channel"],

                "station_latitude": record["station_latitude"],
                "station_longitude": record["station_longitude"],

                "signal": {
                    "start_time": record["signal"]["start_time"],
                    "sampling_rate": record["signal"]["sampling_rate"]
                }
            }

            # --------------------------------------------------------
            # Сжатие данных сигнала
            # --------------------------------------------------------

            if include_data_samples > 0:
                samples = record["signal"]["samples"]

                data_array = np.array(
                    samples[:include_data_samples],
                    dtype=np.float32
                )

                data_bytes = data_array.tobytes()

                compressed = zlib.compress(
                    data_bytes,
                    level=6
                )

                optimized_record["signal"]["samples_b64"] = (
                    base64.b64encode(compressed)
                    .decode("ascii")
                )

            optimized_records.append(optimized_record)

        # ------------------------------------------------------------
        # Финальная структура
        # ------------------------------------------------------------

        result = {
            "format_version": "2.0",

            "event": event,

            "records": optimized_records,

            "total_records": len(optimized_records),

            "compression": "float32+zlib+base64"
        }

        # ------------------------------------------------------------
        # JSON
        # ------------------------------------------------------------

        result_clean = self._convert_numpy_types(result)

        json_str = json.dumps(
            result_clean,
            separators=(",", ":")
        )

        # ------------------------------------------------------------
        # Сжатие JSON
        # ------------------------------------------------------------

        compressed_json = zlib.compress(
            json_str.encode("utf-8"),
            level=9
        )

        output_path = (
                self.output_dir /
                f"{output_name}.json.z"
        )

        with open(output_path, "wb") as f:
            f.write(compressed_json)

        original_size = len(
            json_str.encode("utf-8")
        )

        compressed_size = len(compressed_json)

        print(
            f"    Сохранено в: {output_path}"
        )

        print(
            f"    Размер: {compressed_size:,} bytes "
            f"(сжатие {compressed_size / original_size * 100:.1f}%)"
        )

        return str(output_path)

    def save_to_msgpack(self,
                        metadata_list: List[Dict[str, Any]],
                        output_name: str = "sac_metadata") -> str:
        """
        Сохраняет метаданные в MessagePack (бинарный формат)

        Args:
            metadata_list: список метаданных
            output_name: имя выходного файла

        Returns:
            Путь к сохраненному файлу
        """
        print(f"[Сериализация] Сохранение в MessagePack...")

        # Подготавливаем данные
        packed_data = {
            'files': metadata_list,
            'total': len(metadata_list),
            'timestamp': str(UTCDateTime.now())
        }

        # Сериализуем в MessagePack
        packed = msgpack.packb(packed_data, use_bin_type=True)

        # Сжимаем
        compressed = zlib.compress(packed, level=9)

        # Сохраняем
        output_path = self.output_dir / f"{output_name}.msgpack.z"
        with open(output_path, 'wb') as f:
            f.write(compressed)

        print(f"    Сохранено в: {output_path}")
        print(f"    Размер: {len(compressed):,} bytes")

        return str(output_path)


# ============================================================================
# ФУНКЦИИ ДЛЯ ВЫЗОВА ИЗ C#
# ============================================================================

def download_and_process_event(event_time: str,
                               duration: int = 3600,
                               output_dir: str = None) -> str:
    """
    Скачивает и обрабатывает данные сейсмического события.

    Args:
        event_time: время события "YYYY-MM-DDTHH:MM:SS"
        duration: длительность записи в секундах
        output_dir: папка для сохранения результатов

    Returns:
        Путь к сжатому JSON-файлу с результатами
    """

    processor = SACDataProcessor(output_dir=output_dir)

    # Скачиваем и формируем структуру события
    event_data = processor.download_event_data(
        event_time=event_time,
        duration_seconds=duration
    )

    # Сохраняем событие
    result_file = processor.save_to_optimized_json(
        event_data
    )

    return result_file


def process_existing_sac_files(sac_dir: str, output_dir: str = None) -> str:
    """
    Парсит существующие SAC файлы.

    Args:
        sac_dir: папка с SAC файлами
        output_dir: папка для сохранения результатов

    Returns:
        Путь к JSON-файлу с результатами
    """

    processor = SACDataProcessor(output_dir=output_dir)

    # Парсим SAC-файлы
    event_data = processor.parse_existing_sac_files(sac_dir)

    # Сохраняем результат
    if event_data["records"]:
        result_file = processor.save_to_optimized_json(event_data)
        return result_file

    return ""


def main():
    # Русские буквы в консоли и в канале связи с C# должны быть в UTF-8
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")

    parser = argparse.ArgumentParser(description="Сейсмические данные -> .json.z")
    sub = parser.add_subparsers(dest="mode", required=True)

    # Режим 1: разобрать папку с готовыми SAC-файлами
    p_dir = sub.add_parser("sac-dir", help="разобрать папку с SAC-файлами")
    p_dir.add_argument("--dir", required=True, help="папка с .sac файлами")
    p_dir.add_argument("--out", required=True, help="папка для результата")

    # Режим 2: скачать событие с FDSN-сервера по времени
    p_dl = sub.add_parser("download", help="скачать событие по времени")
    p_dl.add_argument("--time", required=True, help="YYYY-MM-DDTHH:MM:SS")
    p_dl.add_argument("--duration", type=int, default=3600, help="секунд")
    p_dl.add_argument("--out", required=True, help="папка для результата")

    args = parser.parse_args()

        # Относительные пути считаем от папки, откуда запустили программу
    args.out = os.path.abspath(os.path.join(ORIG_CWD, args.out))
    if args.mode == "sac-dir":
        args.dir = os.path.abspath(os.path.join(ORIG_CWD, args.dir))

    try:
        if args.mode == "sac-dir":
            result = process_existing_sac_files(args.dir, args.out)
        else:
            result = download_and_process_event(args.time, args.duration, args.out)
    except Exception as e:
        print(f"Ошибка: {e}", file=sys.stderr)
        sys.exit(1)

    if not result:
        print("Ошибка: данные не получены (папка пустая или нет SAC-файлов)",
              file=sys.stderr)
        sys.exit(2)

    # C# ищет именно строку, начинающуюся с RESULT:
    print(f"RESULT:{result}", flush=True)


if __name__ == "__main__":
    main()