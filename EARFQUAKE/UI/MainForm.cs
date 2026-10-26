using EARFQUAKE.Models;
using EARFQUAKE.Services;
using EARFQUAKE.Visualization;
using ScottPlot;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace EARFQUAKE
{
    /// <summary>
    /// Главное окно: здесь только логика. Внешний вид правится в Конструкторе
    /// (MainForm.Designer.cs). Вычисления делает AnalysisService,
    /// график строит SpectralPlotBuilder.
    /// </summary>
    public partial class MainForm : Form
    {
        private CancellationTokenSource? _cts;
        private List<StationSpectralResult> _results = new();
        private bool _updatingChannels;

        public MainForm()
        {
            InitializeComponent();

            // Список признаков берём из enum, чтобы не дублировать его в Конструкторе
            foreach (SpectralFeature f in Enum.GetValues<SpectralFeature>())
                featureBox.Items.Add(f);
            featureBox.SelectedIndex = 0;
            channelBox.SelectedIndex = 0;
        }

        // ====================================================================
        // Обработчики событий (подключаются в Конструкторе)
        // ====================================================================

        private async void runButton_Click(object? sender, EventArgs e) => await RunAsync();

        private void cancelButton_Click(object? sender, EventArgs e) => _cts?.Cancel();

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e) => _cts?.Cancel();

        private void featureBox_Format(object? sender, ListControlConvertEventArgs e)
        {
            if (e.ListItem is SpectralFeature f)
                e.Value = SpectralPlotBuilder.DisplayName(f);
        }

        private void featureBox_SelectedIndexChanged(object? sender, EventArgs e) => RedrawPlot();

        private void channelBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_updatingChannels)
                RedrawPlot();
        }

        // ====================================================================
        // Основная логика
        // ====================================================================

        private async Task RunAsync()
        {
            _cts = new CancellationTokenSource();
            SetBusy(true);
            logBox.Clear();

            string outDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EARFQUAKE", "output");

            // Progress создан в UI-потоке, поэтому Report сам возвращается в UI-поток
            var progress = new Progress<string>(AppendLog);

            try
            {
                var service = new AnalysisService(RunnerFactory.Create());
                var output = await service.RunAsync(
                    new AnalysisRequest(timePicker.Value, (int)durationBox.Value, outDir),
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
            runButton.Enabled = !busy;
            cancelButton.Enabled = busy;
            timePicker.Enabled = !busy;
            durationBox.Enabled = !busy;
            progressBar.Visible = busy;
        }

        private void AppendLog(string line) => logBox.AppendText(line + Environment.NewLine);

        private void FillChannels()
        {
            _updatingChannels = true;
            channelBox.Items.Clear();
            channelBox.Items.Add("Все");
            foreach (var ch in _results.Select(r => r.Channel).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c))
                channelBox.Items.Add(ch);

            // По умолчанию вертикальная компонента, как в консольной версии
            int bhz = channelBox.Items.IndexOf("BHZ");
            channelBox.SelectedIndex = bhz >= 0 ? bhz : 0;
            _updatingChannels = false;
        }

        private void RedrawPlot()
        {
            if (featureBox.SelectedItem is not SpectralFeature feature)
                return;

            string? channel = channelBox.SelectedIndex <= 0 ? null : channelBox.SelectedItem as string;
            var plt = SpectralPlotBuilder.BuildVsDistance(_results, feature, channel);

            plot.Reset(plt);
            plot.Refresh();
        }

        private void FillGrid()
        {
            grid.DataSource = _results
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

            foreach (DataGridViewColumn col in grid.Columns)
            {
                col.DefaultCellStyle.Format = col.Name == "Energy" ? "E3"
                    : col.Name == "DistanceKm" ? "F0"
                    : col.ValueType == typeof(double) ? "F3" : "";
            }
        }
    }
}