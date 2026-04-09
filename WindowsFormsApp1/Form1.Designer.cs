namespace WindowsFormsApp1
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.panelChannelsBottom = new System.Windows.Forms.Panel();
            this.checkDirectMode = new System.Windows.Forms.CheckBox();
            this.btnUpdateChannels = new System.Windows.Forms.Button();
            this.btnLoadConfigFile = new System.Windows.Forms.Button();
            this.btnSaveVah = new System.Windows.Forms.Button();
            this.btnWriteChannels = new System.Windows.Forms.Button();
            this.gridChannels = new System.Windows.Forms.DataGridView();
            this.colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colChannel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colType = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colK = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCurrent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.panelStartMode = new System.Windows.Forms.Panel();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnRead = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.checkAutoScaleY = new System.Windows.Forms.CheckBox();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.btnWriteConfig = new System.Windows.Forms.Button();
            this.btnLoadConfig = new System.Windows.Forms.Button();
            this.labelSubStatus = new System.Windows.Forms.Label();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.labelError = new System.Windows.Forms.Label();
            this.labelStatus = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBaud = new System.Windows.Forms.ComboBox();
            this.comboPorts = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnRefreshPorts = new System.Windows.Forms.Button();
            this.btnBigStart = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage2.SuspendLayout();
            this.panelChannelsBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridChannels)).BeginInit();
            this.tabPage1.SuspendLayout();
            this.panelStartMode.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.panel2.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Interval = 200;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.panelChannelsBottom);
            this.tabPage2.Controls.Add(this.gridChannels);
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(874, 524);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Каналы";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // panelChannelsBottom
            // 
            this.panelChannelsBottom.Controls.Add(this.checkDirectMode);
            this.panelChannelsBottom.Controls.Add(this.btnUpdateChannels);
            this.panelChannelsBottom.Controls.Add(this.btnLoadConfigFile);
            this.panelChannelsBottom.Controls.Add(this.btnSaveVah);
            this.panelChannelsBottom.Controls.Add(this.btnWriteChannels);
            this.panelChannelsBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelChannelsBottom.Location = new System.Drawing.Point(3, 449);
            this.panelChannelsBottom.Name = "panelChannelsBottom";
            this.panelChannelsBottom.Size = new System.Drawing.Size(868, 72);
            this.panelChannelsBottom.TabIndex = 27;
            this.panelChannelsBottom.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // checkDirectMode
            // 
            this.checkDirectMode.AutoSize = true;
            this.checkDirectMode.Location = new System.Drawing.Point(390, 49);
            this.checkDirectMode.Name = "checkDirectMode";
            this.checkDirectMode.Size = new System.Drawing.Size(168, 20);
            this.checkDirectMode.TabIndex = 27;
            this.checkDirectMode.Text = "Режим значений (мВ)";
            this.checkDirectMode.UseVisualStyleBackColor = true;
            this.checkDirectMode.CheckedChanged += new System.EventHandler(this.checkDirectMode_CheckedChanged_1);
            // 
            // btnUpdateChannels
            // 
            this.btnUpdateChannels.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnUpdateChannels.Location = new System.Drawing.Point(6, 44);
            this.btnUpdateChannels.Name = "btnUpdateChannels";
            this.btnUpdateChannels.Size = new System.Drawing.Size(210, 23);
            this.btnUpdateChannels.TabIndex = 26;
            this.btnUpdateChannels.Text = "Обновить каналы";
            this.btnUpdateChannels.UseVisualStyleBackColor = true;
            this.btnUpdateChannels.Click += new System.EventHandler(this.btnChannelsApply_Click);
            // 
            // btnLoadConfigFile
            // 
            this.btnLoadConfigFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLoadConfigFile.Location = new System.Drawing.Point(728, 44);
            this.btnLoadConfigFile.Name = "btnLoadConfigFile";
            this.btnLoadConfigFile.Size = new System.Drawing.Size(136, 23);
            this.btnLoadConfigFile.TabIndex = 22;
            this.btnLoadConfigFile.Text = "Открыть архив";
            this.btnLoadConfigFile.UseVisualStyleBackColor = true;
            this.btnLoadConfigFile.Click += new System.EventHandler(this.btnLoadConfigFile_Click);
            // 
            // btnSaveVah
            // 
            this.btnSaveVah.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveVah.Location = new System.Drawing.Point(728, 17);
            this.btnSaveVah.Name = "btnSaveVah";
            this.btnSaveVah.Size = new System.Drawing.Size(136, 23);
            this.btnSaveVah.TabIndex = 25;
            this.btnSaveVah.Text = "Сохранить данные";
            this.btnSaveVah.UseVisualStyleBackColor = true;
            this.btnSaveVah.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnWriteChannels
            // 
            this.btnWriteChannels.Location = new System.Drawing.Point(6, 17);
            this.btnWriteChannels.Name = "btnWriteChannels";
            this.btnWriteChannels.Size = new System.Drawing.Size(210, 23);
            this.btnWriteChannels.TabIndex = 24;
            this.btnWriteChannels.Text = "Прочесть и записать каналы";
            this.btnWriteChannels.UseVisualStyleBackColor = true;
            this.btnWriteChannels.Click += new System.EventHandler(this.btnChannelsLoad_Click);
            // 
            // gridChannels
            // 
            this.gridChannels.AllowUserToAddRows = false;
            this.gridChannels.AllowUserToDeleteRows = false;
            this.gridChannels.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridChannels.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridChannels.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colEnabled,
            this.colChannel,
            this.colType,
            this.colK,
            this.colB,
            this.colCurrent,
            this.colUnit});
            this.gridChannels.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridChannels.GridColor = System.Drawing.SystemColors.ButtonShadow;
            this.gridChannels.Location = new System.Drawing.Point(3, 3);
            this.gridChannels.Name = "gridChannels";
            this.gridChannels.RowHeadersVisible = false;
            this.gridChannels.RowHeadersWidth = 51;
            this.gridChannels.RowTemplate.Height = 24;
            this.gridChannels.Size = new System.Drawing.Size(868, 518);
            this.gridChannels.TabIndex = 21;
            this.gridChannels.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick_1);
            // 
            // colEnabled
            // 
            this.colEnabled.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colEnabled.HeaderText = "Включение:";
            this.colEnabled.MinimumWidth = 6;
            this.colEnabled.Name = "colEnabled";
            // 
            // colChannel
            // 
            this.colChannel.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colChannel.HeaderText = "Канал";
            this.colChannel.MinimumWidth = 6;
            this.colChannel.Name = "colChannel";
            this.colChannel.ReadOnly = true;
            // 
            // colType
            // 
            this.colType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colType.HeaderText = "Тип";
            this.colType.Items.AddRange(new object[] {
            "",
            "Напряжение",
            "Ток"});
            this.colType.MinimumWidth = 6;
            this.colType.Name = "colType";
            // 
            // colK
            // 
            this.colK.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colK.HeaderText = "k";
            this.colK.MinimumWidth = 6;
            this.colK.Name = "colK";
            // 
            // colB
            // 
            this.colB.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colB.HeaderText = "b";
            this.colB.MinimumWidth = 6;
            this.colB.Name = "colB";
            // 
            // colCurrent
            // 
            this.colCurrent.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colCurrent.HeaderText = "Значение";
            this.colCurrent.MinimumWidth = 6;
            this.colCurrent.Name = "colCurrent";
            // 
            // colUnit
            // 
            this.colUnit.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colUnit.HeaderText = "Ед.";
            this.colUnit.MinimumWidth = 6;
            this.colUnit.Name = "colUnit";
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.panelStartMode);
            this.tabPage1.Controls.Add(this.panel2);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(874, 524);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Связь\n";
            this.tabPage1.UseVisualStyleBackColor = true;
            this.tabPage1.Click += new System.EventHandler(this.tabPage1_Click);
            // 
            // panelStartMode
            // 
            this.panelStartMode.Controls.Add(this.btnClear);
            this.panelStartMode.Controls.Add(this.btnRead);
            this.panelStartMode.Controls.Add(this.btnStart);
            this.panelStartMode.Controls.Add(this.btnStop);
            this.panelStartMode.Controls.Add(this.checkAutoScaleY);
            this.panelStartMode.Controls.Add(this.chart1);
            this.panelStartMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelStartMode.Location = new System.Drawing.Point(3, 3);
            this.panelStartMode.Name = "panelStartMode";
            this.panelStartMode.Size = new System.Drawing.Size(868, 518);
            this.panelStartMode.TabIndex = 49;
            this.panelStartMode.Visible = false;
            this.panelStartMode.Paint += new System.Windows.Forms.PaintEventHandler(this.panelStartMode_Paint);
            // 
            // btnClear
            // 
            this.btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClear.Location = new System.Drawing.Point(602, 498);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(266, 23);
            this.btnClear.TabIndex = 47;
            this.btnClear.Text = "Очистить график полностью";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click_1);
            // 
            // btnRead
            // 
            this.btnRead.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRead.Location = new System.Drawing.Point(602, 470);
            this.btnRead.Name = "btnRead";
            this.btnRead.Size = new System.Drawing.Size(266, 23);
            this.btnRead.TabIndex = 48;
            this.btnRead.Text = "Прочитать одно значение";
            this.btnRead.UseVisualStyleBackColor = true;
            this.btnRead.Click += new System.EventHandler(this.btnRead_Click_1);
            // 
            // btnStart
            // 
            this.btnStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnStart.Location = new System.Drawing.Point(6, 470);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(134, 23);
            this.btnStart.TabIndex = 44;
            this.btnStart.Text = "Старт графика";
            this.btnStart.UseVisualStyleBackColor = true;
            // 
            // btnStop
            // 
            this.btnStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnStop.Location = new System.Drawing.Point(6, 495);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(134, 23);
            this.btnStop.TabIndex = 45;
            this.btnStop.Text = "Стоп графика";
            this.btnStop.UseVisualStyleBackColor = true;
            // 
            // checkAutoScaleY
            // 
            this.checkAutoScaleY.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkAutoScaleY.AutoSize = true;
            this.checkAutoScaleY.Location = new System.Drawing.Point(146, 498);
            this.checkAutoScaleY.Name = "checkAutoScaleY";
            this.checkAutoScaleY.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.checkAutoScaleY.Size = new System.Drawing.Size(73, 20);
            this.checkAutoScaleY.TabIndex = 46;
            this.checkAutoScaleY.Text = "Авто Y";
            this.checkAutoScaleY.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkAutoScaleY.UseVisualStyleBackColor = true;
            // 
            // chart1
            // 
            chartArea1.AxisX.Title = "Время (сек)";
            chartArea1.AxisY.Title = "Напряженность";
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            this.chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chart1.Location = new System.Drawing.Point(0, 0);
            this.chart1.Name = "chart1";
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            series1.Name = "Series1";
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(868, 518);
            this.chart1.TabIndex = 51;
            this.chart1.Text = "chart1";
            this.chart1.Click += new System.EventHandler(this.chart1_Click_2);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label3);
            this.panel2.Controls.Add(this.btnWriteConfig);
            this.panel2.Controls.Add(this.btnLoadConfig);
            this.panel2.Controls.Add(this.labelSubStatus);
            this.panel2.Controls.Add(this.btnConnect);
            this.panel2.Controls.Add(this.btnDisconnect);
            this.panel2.Controls.Add(this.labelError);
            this.panel2.Controls.Add(this.labelStatus);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.label6);
            this.panel2.Controls.Add(this.comboBaud);
            this.panel2.Controls.Add(this.comboPorts);
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.btnRefreshPorts);
            this.panel2.Controls.Add(this.btnBigStart);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(3, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(868, 518);
            this.panel2.TabIndex = 44;
            this.panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.panel2_Paint);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 18);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 16);
            this.label3.TabIndex = 42;
            this.label3.Text = "Cостояние:";
            this.label3.Click += new System.EventHandler(this.label3_Click_2);
            // 
            // btnWriteConfig
            // 
            this.btnWriteConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnWriteConfig.Location = new System.Drawing.Point(6, 490);
            this.btnWriteConfig.Name = "btnWriteConfig";
            this.btnWriteConfig.Size = new System.Drawing.Size(210, 23);
            this.btnWriteConfig.TabIndex = 33;
            this.btnWriteConfig.Text = "Записать конфигурации";
            this.btnWriteConfig.UseVisualStyleBackColor = true;
            this.btnWriteConfig.Click += new System.EventHandler(this.btnSaveToDevice_Click);
            // 
            // btnLoadConfig
            // 
            this.btnLoadConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnLoadConfig.Location = new System.Drawing.Point(6, 463);
            this.btnLoadConfig.Name = "btnLoadConfig";
            this.btnLoadConfig.Size = new System.Drawing.Size(210, 23);
            this.btnLoadConfig.TabIndex = 32;
            this.btnLoadConfig.Text = "Загрузить конфигурации";
            this.btnLoadConfig.UseVisualStyleBackColor = true;
            this.btnLoadConfig.Click += new System.EventHandler(this.btnLoadConfig_Click_1);
            // 
            // labelSubStatus
            // 
            this.labelSubStatus.AutoSize = true;
            this.labelSubStatus.Location = new System.Drawing.Point(90, 18);
            this.labelSubStatus.Name = "labelSubStatus";
            this.labelSubStatus.Size = new System.Drawing.Size(19, 16);
            this.labelSubStatus.TabIndex = 43;
            this.labelSubStatus.Text = "---";
            this.labelSubStatus.Click += new System.EventHandler(this.label4_Click);
            // 
            // btnConnect
            // 
            this.btnConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConnect.Location = new System.Drawing.Point(742, 463);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(121, 23);
            this.btnConnect.TabIndex = 4;
            this.btnConnect.Text = "Подключиться";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDisconnect.Location = new System.Drawing.Point(742, 490);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(123, 23);
            this.btnDisconnect.TabIndex = 5;
            this.btnDisconnect.Text = "Отключиться";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.button1_Click);
            // 
            // labelError
            // 
            this.labelError.AutoSize = true;
            this.labelError.ForeColor = System.Drawing.Color.Red;
            this.labelError.Location = new System.Drawing.Point(3, 34);
            this.labelError.Name = "labelError";
            this.labelError.Size = new System.Drawing.Size(63, 16);
            this.labelError.TabIndex = 23;
            this.labelError.Text = "Ошибка: ";
            this.labelError.Visible = false;
            this.labelError.Click += new System.EventHandler(this.labelError_Click);
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(65, 2);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(19, 16);
            this.labelStatus.TabIndex = 22;
            this.labelStatus.Text = "---";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(761, 7);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(71, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "Скорость:";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 2);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(56, 16);
            this.label6.TabIndex = 19;
            this.label6.Text = "Статус:";
            this.label6.Click += new System.EventHandler(this.label6_Click);
            // 
            // comboBaud
            // 
            this.comboBaud.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBaud.FormattingEnabled = true;
            this.comboBaud.Location = new System.Drawing.Point(742, 26);
            this.comboBaud.Name = "comboBaud";
            this.comboBaud.Size = new System.Drawing.Size(121, 24);
            this.comboBaud.TabIndex = 3;
            this.comboBaud.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged_1);
            // 
            // comboPorts
            // 
            this.comboPorts.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboPorts.DropDownWidth = 121;
            this.comboPorts.FormattingEnabled = true;
            this.comboPorts.Location = new System.Drawing.Point(602, 26);
            this.comboPorts.Name = "comboPorts";
            this.comboPorts.Size = new System.Drawing.Size(134, 24);
            this.comboPorts.TabIndex = 1;
            this.comboPorts.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(634, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "COM-порт:";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // btnRefreshPorts
            // 
            this.btnRefreshPorts.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefreshPorts.Location = new System.Drawing.Point(742, 56);
            this.btnRefreshPorts.Name = "btnRefreshPorts";
            this.btnRefreshPorts.Size = new System.Drawing.Size(121, 23);
            this.btnRefreshPorts.TabIndex = 17;
            this.btnRefreshPorts.Text = "Обновить порты";
            this.btnRefreshPorts.UseVisualStyleBackColor = true;
            // 
            // btnBigStart
            // 
            this.btnBigStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBigStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 48F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnBigStart.Location = new System.Drawing.Point(-3, 161);
            this.btnBigStart.Name = "btnBigStart";
            this.btnBigStart.Size = new System.Drawing.Size(874, 181);
            this.btnBigStart.TabIndex = 50;
            this.btnBigStart.Text = "ПУСК";
            this.btnBigStart.UseVisualStyleBackColor = true;
            this.btnBigStart.Click += new System.EventHandler(this.button1_Click_5);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(882, 553);
            this.tabControl1.TabIndex = 21;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(882, 553);
            this.Controls.Add(this.tabControl1);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tabPage2.ResumeLayout(false);
            this.panelChannelsBottom.ResumeLayout(false);
            this.panelChannelsBottom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridChannels)).EndInit();
            this.tabPage1.ResumeLayout(false);
            this.panelStartMode.ResumeLayout(false);
            this.panelStartMode.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Panel panelChannelsBottom;
        private System.Windows.Forms.Button btnUpdateChannels;
        private System.Windows.Forms.Button btnLoadConfigFile;
        private System.Windows.Forms.Button btnSaveVah;
        private System.Windows.Forms.Button btnWriteChannels;
        private System.Windows.Forms.DataGridView gridChannels;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn colChannel;
        private System.Windows.Forms.DataGridViewComboBoxColumn colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colK;
        private System.Windows.Forms.DataGridViewTextBoxColumn colB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCurrent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUnit;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnRead;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.CheckBox checkAutoScaleY;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnWriteConfig;
        private System.Windows.Forms.Button btnLoadConfig;
        private System.Windows.Forms.Label labelSubStatus;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Label labelError;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBaud;
        private System.Windows.Forms.ComboBox comboPorts;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnRefreshPorts;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.Panel panelStartMode;
        private System.Windows.Forms.Button btnBigStart;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.CheckBox checkDirectMode;
    }
}

