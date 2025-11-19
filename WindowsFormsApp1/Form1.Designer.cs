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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea5 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series5 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.label1 = new System.Windows.Forms.Label();
            this.comboPorts = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBaud = new System.Windows.Forms.ComboBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.numChannel = new System.Windows.Forms.NumericUpDown();
            this.btnRead = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.labelValue = new System.Windows.Forms.Label();
            this.labelStatus = new System.Windows.Forms.Label();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.checkEmu = new System.Windows.Forms.CheckBox();
            this.btnRefreshPorts = new System.Windows.Forms.Button();
            this.labelError = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numChannel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(41, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "COM-порт:";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // comboPorts
            // 
            this.comboPorts.FormattingEnabled = true;
            this.comboPorts.Location = new System.Drawing.Point(12, 31);
            this.comboPorts.Name = "comboPorts";
            this.comboPorts.Size = new System.Drawing.Size(134, 24);
            this.comboPorts.TabIndex = 1;
            this.comboPorts.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(178, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(71, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "Скорость:";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // comboBaud
            // 
            this.comboBaud.FormattingEnabled = true;
            this.comboBaud.Location = new System.Drawing.Point(152, 31);
            this.comboBaud.Name = "comboBaud";
            this.comboBaud.Size = new System.Drawing.Size(121, 24);
            this.comboBaud.TabIndex = 3;
            this.comboBaud.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged_1);
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(562, 415);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(111, 23);
            this.btnConnect.TabIndex = 4;
            this.btnConnect.Text = "Подключить";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Location = new System.Drawing.Point(679, 415);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(109, 23);
            this.btnDisconnect.TabIndex = 5;
            this.btnDisconnect.Text = "Отключить";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.button1_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(455, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(56, 16);
            this.label6.TabIndex = 6;
            this.label6.Text = "Статус:";
            this.label6.Click += new System.EventHandler(this.label3_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(300, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(50, 16);
            this.label3.TabIndex = 7;
            this.label3.Text = "Канал:";
            this.label3.Click += new System.EventHandler(this.label3_Click_1);
            // 
            // numChannel
            // 
            this.numChannel.Location = new System.Drawing.Point(279, 32);
            this.numChannel.Maximum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.numChannel.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numChannel.Name = "numChannel";
            this.numChannel.Size = new System.Drawing.Size(97, 22);
            this.numChannel.TabIndex = 8;
            this.numChannel.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // btnRead
            // 
            this.btnRead.Location = new System.Drawing.Point(279, 60);
            this.btnRead.Name = "btnRead";
            this.btnRead.Size = new System.Drawing.Size(97, 23);
            this.btnRead.TabIndex = 9;
            this.btnRead.Text = "Прочитать";
            this.btnRead.UseVisualStyleBackColor = true;
            this.btnRead.Click += new System.EventHandler(this.btnRead_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(455, 25);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(134, 16);
            this.label4.TabIndex = 10;
            this.label4.Text = "Текущее значение:";
            // 
            // labelValue
            // 
            this.labelValue.AutoSize = true;
            this.labelValue.Location = new System.Drawing.Point(598, 25);
            this.labelValue.Name = "labelValue";
            this.labelValue.Size = new System.Drawing.Size(19, 16);
            this.labelValue.TabIndex = 11;
            this.labelValue.Text = "---";
            this.labelValue.Click += new System.EventHandler(this.label5_Click);
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(598, 9);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(19, 16);
            this.labelStatus.TabIndex = 12;
            this.labelStatus.Text = "---";
            this.labelStatus.Click += new System.EventHandler(this.labelStatus_Click);
            // 
            // chart1
            // 
            chartArea5.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea5);
            this.chart1.Location = new System.Drawing.Point(0, 87);
            this.chart1.Name = "chart1";
            series5.ChartArea = "ChartArea1";
            series5.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
            series5.Name = "Series1";
            this.chart1.Series.Add(series5);
            this.chart1.Size = new System.Drawing.Size(800, 322);
            this.chart1.TabIndex = 13;
            this.chart1.Text = "chart1";
            this.chart1.Click += new System.EventHandler(this.chart1_Click);
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(12, 415);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 14;
            this.btnStart.Text = "Старт графика";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(93, 415);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 15;
            this.btnStop.Text = "Стоп графика";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // timer1
            // 
            this.timer1.Interval = 200;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // checkEmu
            // 
            this.checkEmu.AutoSize = true;
            this.checkEmu.Location = new System.Drawing.Point(181, 418);
            this.checkEmu.Name = "checkEmu";
            this.checkEmu.Size = new System.Drawing.Size(139, 20);
            this.checkEmu.TabIndex = 16;
            this.checkEmu.Text = "Режим эмуляции";
            this.checkEmu.UseVisualStyleBackColor = true;
            this.checkEmu.CheckedChanged += new System.EventHandler(this.checkEmu_CheckedChanged);
            // 
            // btnRefreshPorts
            // 
            this.btnRefreshPorts.Location = new System.Drawing.Point(12, 58);
            this.btnRefreshPorts.Name = "btnRefreshPorts";
            this.btnRefreshPorts.Size = new System.Drawing.Size(134, 23);
            this.btnRefreshPorts.TabIndex = 17;
            this.btnRefreshPorts.Text = "Обновить порты";
            this.btnRefreshPorts.UseVisualStyleBackColor = true;
            // 
            // labelError
            // 
            this.labelError.AutoSize = true;
            this.labelError.ForeColor = System.Drawing.Color.Red;
            this.labelError.Location = new System.Drawing.Point(455, 41);
            this.labelError.Name = "labelError";
            this.labelError.Size = new System.Drawing.Size(63, 16);
            this.labelError.TabIndex = 18;
            this.labelError.Text = "Ошибка: ";
            this.labelError.Visible = false;
            this.labelError.Click += new System.EventHandler(this.label5_Click_1);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.labelError);
            this.Controls.Add(this.btnRefreshPorts);
            this.Controls.Add(this.checkEmu);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.labelValue);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnRead);
            this.Controls.Add(this.numChannel);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.comboBaud);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboPorts);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.numChannel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboPorts;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBaud;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numChannel;
        private System.Windows.Forms.Button btnRead;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelValue;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.CheckBox checkEmu;
        private System.Windows.Forms.Button btnRefreshPorts;
        private System.Windows.Forms.Label labelError;
    }
}

