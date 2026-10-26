namespace EARFQUAKE
{
    partial class MainForm
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Метод для поддержки Конструктора. Не изменяйте его вручную,
        /// правьте окно через Конструктор.
        /// </summary>
        private void InitializeComponent()
        {
            topPanel = new FlowLayoutPanel();
            labelTime = new Label();
            timePicker = new DateTimePicker();
            labelDuration = new Label();
            durationBox = new NumericUpDown();
            runButton = new Button();
            cancelButton = new Button();
            tabs = new TabControl();
            plotPage = new TabPage();
            plot = new ScottPlot.WinForms.FormsPlot();
            plotTop = new FlowLayoutPanel();
            labelFeature = new Label();
            featureBox = new ComboBox();
            labelChannel = new Label();
            channelBox = new ComboBox();
            gridPage = new TabPage();
            grid = new DataGridView();
            progressBar = new ProgressBar();
            logBox = new TextBox();
            topPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)durationBox).BeginInit();
            tabs.SuspendLayout();
            plotPage.SuspendLayout();
            plotTop.SuspendLayout();
            gridPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
            SuspendLayout();
            // 
            // topPanel
            // 
            topPanel.AutoSize = true;
            topPanel.Controls.Add(labelTime);
            topPanel.Controls.Add(timePicker);
            topPanel.Controls.Add(labelDuration);
            topPanel.Controls.Add(durationBox);
            topPanel.Controls.Add(runButton);
            topPanel.Controls.Add(cancelButton);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Padding = new Padding(8, 9, 8, 9);
            topPanel.Size = new Size(1084, 55);
            topPanel.TabIndex = 0;
            // 
            // labelTime
            // 
            labelTime.AutoSize = true;
            labelTime.Location = new Point(8, 16);
            labelTime.Margin = new Padding(0, 7, 4, 0);
            labelTime.Name = "labelTime";
            labelTime.Size = new Size(138, 17);
            labelTime.TabIndex = 0;
            labelTime.Text = "Время события (UTC):";
            // 
            // timePicker
            // 
            timePicker.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            timePicker.Format = DateTimePickerFormat.Custom;
            timePicker.Location = new Point(153, 12);
            timePicker.Name = "timePicker";
            timePicker.Size = new Size(170, 25);
            timePicker.TabIndex = 1;
            timePicker.Value = new DateTime(2012, 8, 14, 2, 59, 0, 0);
            // 
            // labelDuration
            // 
            labelDuration.AutoSize = true;
            labelDuration.Location = new Point(338, 16);
            labelDuration.Margin = new Padding(12, 7, 4, 0);
            labelDuration.Name = "labelDuration";
            labelDuration.Size = new Size(106, 17);
            labelDuration.TabIndex = 2;
            labelDuration.Text = "Длительность, с:";
            // 
            // durationBox
            // 
            durationBox.Increment = new decimal(new int[] { 600, 0, 0, 0 });
            durationBox.Location = new Point(451, 12);
            durationBox.Maximum = new decimal(new int[] { 86400, 0, 0, 0 });
            durationBox.Minimum = new decimal(new int[] { 60, 0, 0, 0 });
            durationBox.Name = "durationBox";
            durationBox.Size = new Size(70, 25);
            durationBox.TabIndex = 3;
            durationBox.Value = new decimal(new int[] { 3600, 0, 0, 0 });
            // 
            // runButton
            // 
            runButton.AutoSize = true;
            runButton.Location = new Point(527, 12);
            runButton.Name = "runButton";
            runButton.Size = new Size(81, 31);
            runButton.TabIndex = 4;
            runButton.Text = "Вычислить";
            runButton.UseVisualStyleBackColor = true;
            runButton.Click += runButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.AutoSize = true;
            cancelButton.Enabled = false;
            cancelButton.Location = new Point(614, 12);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(67, 31);
            cancelButton.TabIndex = 5;
            cancelButton.Text = "Отмена";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += cancelButton_Click;
            // 
            // tabs
            // 
            tabs.Controls.Add(plotPage);
            tabs.Controls.Add(gridPage);
            tabs.Dock = DockStyle.Fill;
            tabs.Location = new Point(0, 55);
            tabs.Name = "tabs";
            tabs.SelectedIndex = 0;
            tabs.Size = new Size(1084, 663);
            tabs.TabIndex = 1;
            // 
            // plotPage
            // 
            plotPage.Controls.Add(plot);
            plotPage.Controls.Add(plotTop);
            plotPage.Location = new Point(4, 26);
            plotPage.Name = "plotPage";
            plotPage.Size = new Size(1076, 633);
            plotPage.TabIndex = 0;
            plotPage.Text = "Графики";
            plotPage.UseVisualStyleBackColor = true;
            // 
            // plot
            // 
            plot.Dock = DockStyle.Fill;
            plot.Location = new Point(0, 41);
            plot.Name = "plot";
            plot.Size = new Size(1076, 592);
            plot.TabIndex = 1;
            // 
            // plotTop
            // 
            plotTop.AutoSize = true;
            plotTop.Controls.Add(labelFeature);
            plotTop.Controls.Add(featureBox);
            plotTop.Controls.Add(labelChannel);
            plotTop.Controls.Add(channelBox);
            plotTop.Dock = DockStyle.Top;
            plotTop.Location = new Point(0, 0);
            plotTop.Name = "plotTop";
            plotTop.Padding = new Padding(4, 5, 4, 5);
            plotTop.Size = new Size(1076, 41);
            plotTop.TabIndex = 0;
            // 
            // labelFeature
            // 
            labelFeature.AutoSize = true;
            labelFeature.Location = new Point(4, 12);
            labelFeature.Margin = new Padding(0, 7, 4, 0);
            labelFeature.Name = "labelFeature";
            labelFeature.Size = new Size(61, 17);
            labelFeature.TabIndex = 0;
            labelFeature.Text = "Признак:";
            // 
            // featureBox
            // 
            featureBox.DropDownStyle = ComboBoxStyle.DropDownList;
            featureBox.FormattingEnabled = true;
            featureBox.Location = new Point(72, 8);
            featureBox.Name = "featureBox";
            featureBox.Size = new Size(190, 25);
            featureBox.TabIndex = 1;
            featureBox.SelectedIndexChanged += featureBox_SelectedIndexChanged;
            featureBox.Format += featureBox_Format;
            // 
            // labelChannel
            // 
            labelChannel.AutoSize = true;
            labelChannel.Location = new Point(277, 12);
            labelChannel.Margin = new Padding(12, 7, 4, 0);
            labelChannel.Name = "labelChannel";
            labelChannel.Size = new Size(84, 17);
            labelChannel.TabIndex = 2;
            labelChannel.Text = "Компонента:";
            // 
            // channelBox
            // 
            channelBox.DropDownStyle = ComboBoxStyle.DropDownList;
            channelBox.FormattingEnabled = true;
            channelBox.Items.AddRange(new object[] { "Все" });
            channelBox.Location = new Point(368, 8);
            channelBox.Name = "channelBox";
            channelBox.Size = new Size(90, 25);
            channelBox.TabIndex = 3;
            channelBox.SelectedIndexChanged += channelBox_SelectedIndexChanged;
            // 
            // gridPage
            // 
            gridPage.Controls.Add(grid);
            gridPage.Location = new Point(4, 26);
            gridPage.Name = "gridPage";
            gridPage.Size = new Size(1076, 633);
            gridPage.TabIndex = 1;
            gridPage.Text = "Данные";
            gridPage.UseVisualStyleBackColor = true;
            // 
            // grid
            // 
            grid.AllowUserToAddRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            grid.Dock = DockStyle.Fill;
            grid.Location = new Point(0, 0);
            grid.Name = "grid";
            grid.ReadOnly = true;
            grid.Size = new Size(1076, 633);
            grid.TabIndex = 0;
            // 
            // progressBar
            // 
            progressBar.Dock = DockStyle.Bottom;
            progressBar.Location = new Point(0, 718);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(1084, 9);
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.TabIndex = 2;
            progressBar.Visible = false;
            // 
            // logBox
            // 
            logBox.Dock = DockStyle.Bottom;
            logBox.Location = new Point(0, 727);
            logBox.Multiline = true;
            logBox.Name = "logBox";
            logBox.ReadOnly = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.Size = new Size(1084, 124);
            logBox.TabIndex = 3;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 851);
            Controls.Add(tabs);
            Controls.Add(topPanel);
            Controls.Add(progressBar);
            Controls.Add(logBox);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Спектральный анализ землетрясений";
            FormClosing += MainForm_FormClosing;
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)durationBox).EndInit();
            tabs.ResumeLayout(false);
            plotPage.ResumeLayout(false);
            plotPage.PerformLayout();
            plotTop.ResumeLayout(false);
            plotTop.PerformLayout();
            gridPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)grid).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel topPanel;
        private System.Windows.Forms.Label labelTime;
        private System.Windows.Forms.DateTimePicker timePicker;
        private System.Windows.Forms.Label labelDuration;
        private System.Windows.Forms.NumericUpDown durationBox;
        private System.Windows.Forms.Button runButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage plotPage;
        private ScottPlot.WinForms.FormsPlot plot;
        private System.Windows.Forms.FlowLayoutPanel plotTop;
        private System.Windows.Forms.Label labelFeature;
        private System.Windows.Forms.ComboBox featureBox;
        private System.Windows.Forms.Label labelChannel;
        private System.Windows.Forms.ComboBox channelBox;
        private System.Windows.Forms.TabPage gridPage;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.TextBox logBox;
    }
}
