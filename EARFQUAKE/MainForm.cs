using ScottPlot.WinForms;

namespace EARFQUAKE
{
    /// <summary>
    /// Главное окно, собранное кодом (Designer не нужен).
    /// Вычисления делает AnalysisService, график строит SpectralPlotBuilder.
    /// </summary>
    public class MainForm : Form
    {
        // --- ввод ---
        private readonly DateTimePicker _time = new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            Value = new DateTime(2012, 8, 14, 2, 59, 0),
            Width = 170
        };
        private readonly NumericUpDown _duration = new()
        {
            Minimum = 60, Maximum = 86400, Increment = 600, Value = 3600, Width = 70
        };
        private readonly Button _run = new() { Text = "Вычислить", AutoSize = true };
        private readonly Button _cancel = new() { Text = "Отмена", AutoSize = true, Enabled = false };

        // --- результат ---
        private readonly ComboBox _featureBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190 };
        private readonly ComboBox _channelBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
        private readonly FormsPlot _plot = new() { Dock = DockStyle.Fill };
        private readonly DataGridView _grid = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
        };

        // --- статус ---
        private readonly TextBox _log = new()
        {
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Bottom, Height = 110
        };
        private readonly ProgressBar _progress = new()
        {
            Dock = DockStyle.Bottom, Style = ProgressBarStyle.Marquee, Visible = false, Height = 8
        };

        private CancellationTokenSource? _cts;
        private List<StationSpectralResult> _results = new();
        private bool _updatingChannels;

        public MainForm()
        {
            Text = "EARFQUAKE — спектральный анализ землетрясений";
            Width = 1100;
            Height = 780;
            StartPosition = FormStartPosition.CenterScreen;

            // Верхняя панель ввода
            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8),
                WrapContents = true
            };
            top.Controls.Add(new Label { Text = "Время события (UTC):", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            top.Controls.Add(_time);
            top.Controls.Add(new Label { Text = "Длительность, с:", AutoSize = true, Margin = new Padding(12, 6, 4, 0) });
            top.Controls.Add(_duration);
            top.Controls.Add(_run);
            top.Controls.Add(_cancel);

            // Вкладка «Графики»
            var plotTop = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4) };
            plotTop.Controls.Add(new Label { Text = "Признак:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            plotTop.Controls.Add(_featureBox);
            plotTop.Controls.Add(new Label { Text = "Компонента:", AutoSize = true, Margin = new Padding(12, 6, 4, 0) });
            plotTop.Controls.Add(_channelBox);

            var plotPage = new TabPage("Графики");
            plotPage.Controls.Add(_plot);      // Fill — добавляем первым
            plotPage.Controls.Add(plotTop);    // Top — после, чтобы встал над графиком

            var gridPage = new TabPage("Данные");
            gridPage.Controls.Add(_grid);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(plotPage);
            tabs.TabPages.Add(gridPage);

            // Порядок добавления важен для Dock: Fill первым, края после
            Controls.Add(tabs);
            Controls.Add(top);
            Controls.Add(_progress);
            Controls.Add(_log);

            // Выбор признака
            foreach (SpectralFeature f in Enum.GetValues<SpectralFeature>())
                _featureBox.Items.Add(f);
            _featureBox.Format += (_, e) =>
            {
                if (e.ListItem is SpectralFeature f) e.Value = SpectralPlotBuilder.DisplayName(f);
            };
            _featureBox.SelectedIndex = 0;

            _channelBox.Items.Add("Все");
            _channelBox.SelectedIndex = 0;

            _featureBox.SelectedIndexChanged += (_, _) => RedrawPlot();
            _channelBox.SelectedIndexChanged += (_, _) => { if (!_updatingChannels) RedrawPlot(); };

            _run.Click += async (_, _) => await RunAsync();
            _cancel.Click += (_, _) => _cts?.Cancel();
            FormClosing += (_, _) => _cts?.Cancel();
        }

        private async Task RunAsync()
        {
            _cts = new CancellationTokenSource();
            SetBusy(true);
            _log.Clear();

            string outDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EARFQUAKE", "output");

            // Progress создан в UI-потоке, поэтому Report сам возвращается в UI-поток
            var progress = new Progress<string>(AppendLog);

            try
            {
                var service = new AnalysisService(RunnerFactory.Create());
                var output = await service.RunAsync(
                    new AnalysisRequest(_time.Value, (int)_duration.Value, outDir),
                    progress, _cts.Token);

                _results = output.Results;
                FillChannels();
                FillGrid();
                RedrawPlot();
            }
            catch (OperationCanceledException)
            {
                AppendLog("Отменено.");
            }
            catch (Exception ex)
            {
                AppendLog("Ошибка: " + ex.Message);
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void SetBusy(bool busy)
        {
            _run.Enabled = !busy;
            _cancel.Enabled = busy;
            _time.Enabled = !busy;
            _duration.Enabled = !busy;
            _progress.Visible = busy;
        }

        private void AppendLog(string line) => _log.AppendText(line + Environment.NewLine);

        private void FillChannels()
        {
            _updatingChannels = true;
            _channelBox.Items.Clear();
            _channelBox.Items.Add("Все");
            foreach (var ch in _results.Select(r => r.Channel).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c))
                _channelBox.Items.Add(ch);

            // По умолчанию вертикальная компонента, как в консольной версии
            int bhz = _channelBox.Items.IndexOf("BHZ");
            _channelBox.SelectedIndex = bhz >= 0 ? bhz : 0;
            _updatingChannels = false;
        }

        private void RedrawPlot()
        {
            if (_featureBox.SelectedItem is not SpectralFeature feature)
                return;

            string? channel = _channelBox.SelectedIndex <= 0 ? null : _channelBox.SelectedItem as string;
            var plt = SpectralPlotBuilder.BuildVsDistance(_results, feature, channel);

            _plot.Reset(plt);
            _plot.Refresh();
        }

        private void FillGrid()
        {
            _grid.DataSource = _results
                .OrderBy(r => r.DistanceKm)
                .Select(r => new
                {
                    r.Network,
                    r.Station,
                    r.Channel,
                    DistanceKm = r.DistanceKm,
                    DominantHz = r.Features.DominantFrequency,
                    CentroidHz = r.Features.SpectralCentroid,
                    BandwidthHz = r.Features.SpectralBandwidth,
                    Energy = r.Features.SpectralEnergy
                })
                .ToList();

            foreach (DataGridViewColumn col in _grid.Columns)
            {
                col.DefaultCellStyle.Format = col.Name == "Energy" ? "E3"
                    : col.Name == "DistanceKm" ? "F0"
                    : col.ValueType == typeof(double) ? "F3" : "";
            }
        }
    }
}
