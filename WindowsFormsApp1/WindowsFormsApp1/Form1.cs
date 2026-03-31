using EasyModbus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private bool isConnected = false;
        private ChannelConfig[] channels = new ChannelConfig[8];
        
        //Удалить ЭТУ строку на случай работы 8 каналов.
        //private const int VISIBLE_CHANNELS = 5;

        private bool _fillingGrid;

        private double yGlobalMin = double.PositiveInfinity;
        private double yGlobalMax = double.NegativeInfinity;

        private ModbusClient client;

        private TextBox _kbEditor;
        private int _kbEditRow = -1;
        private string _kbEditCol = "";

        // подстрой под свой DAM: попробуй 65535, затем 20000, затем 10000

        int[] rawRegs;

        private DateTime? _measureStartTime;
        private Panel _graphTopPanel;
        private Label _graphInfoMain;
        private Label _graphInfoSelected;

        private bool[] _breakSeriesOnNextPoint = new bool[8];
        private StreamWriter _autoSaveWriter;
        private string _currentAutoSavePath = "";        

        public Form1()
        {
            InitializeComponent();
            // растягивание UI вместе с Form
            tabControl1.Dock = DockStyle.Fill;   // если у тебя tabControl называется иначе — подставь имя

            // на вкладке "Графики" (или где chart1 стоит) пусть график тянется по вкладке
            chart1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            gridChannels.AllowUserToAddRows = false;
            gridChannels.AllowUserToDeleteRows = false;
            gridChannels.RowHeadersVisible = false;
            labelSubStatus.Text = "---";

            gridChannels.EditingControlShowing += gridChannels_EditingControlShowing;
            this.gridChannels.CellEndEdit += gridChannels_CellEndEdit;

            // обработчик ошибок DataGridView (чтобы не вылетала ошибка ComboBox)
            this.gridChannels.DataError += gridChannels_DataError;


            // ВАЖНО: заполнить ComboBox-колонку до первой заливки строк
            colType.Items.Clear();
            colType.Items.Add("0–10 В");
            colType.Items.Add("0–5 В");
            colType.Items.Add("±10 В");
            colType.Items.Add("±5 В");
            colType.Items.Add("0–20 мА");
            colType.Items.Add("4–20 мА");


            labelStatus.Text = "Отключено";

            timer1.Interval = 1000;

            btnLoadConfig.Click -= btnLoadConfig_Click_1;
            btnLoadConfig.Click += btnLoadConfig_Click;

            btnWriteConfig.Click -= btnSaveToDevice_Click;
            btnWriteConfig.Click += btnWriteConfig_Click;
            btnSaveVah.Click -= btnSaveVah_Click;
            btnSaveVah.Click -= btnSaveVah_Click_1;
            btnSaveVah.Click -= btnSave_Click;
            btnSaveVah.Click += btnSave_Click;


            btnRefreshPorts.Click += btnRefreshPorts_Click;
            checkAutoScaleY.CheckedChanged += checkAutoScaleY_CheckedChanged;

            chart1.ChartAreas[0].AxisX.Title = "Время, с";
            chart1.ChartAreas[0].AxisY.Title = "Значение";

            BuildGraphTopPanel();
            btnStart.Click -= btnStart_Click;
            btnStart.Click += btnStart_Click;

            btnStop.Click -= btnStop_Click;
            btnStop.Click += btnStop_Click;

            btnRead.Click -= btnRead_Click_1;
            btnRead.Click -= btnRead_Click;
            btnRead.Click += btnRead_Click;

            btnClear.Click -= btnClear_Click_1;
            btnClear.Click -= btnClear_Click;
            btnClear.Click += btnClear_Click;

            PrepareGraphModeUi();
            ShowStartMode();

            if (comboBaud.Items.Count == 0)
            {
                comboBaud.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" });
                comboBaud.SelectedIndex = 0;
            }

            RefreshPorts();
            InitChannelsGrid();
            InitChannelConfigs();
            FillGridFromChannels();
            gridChannels.CellValueChanged += gridChannels_CellValueChanged;
            gridChannels.CurrentCellDirtyStateChanged += gridChannels_CurrentCellDirtyStateChanged;
            colType.ReadOnly = true;
            colType.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
            InitVahSeries();
            InitChannelSeries();
            UpdateSeriesVisibility();
            LoadLastConfig();
            UpdateGraphHeaderUi();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CloseAutoSaveSession();
            base.OnFormClosing(e);
        }

        private void BuildGraphTopPanel()
        {
            if (_graphTopPanel != null)
                return;

            _graphTopPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(6, 4, 6, 4)
            };
            _graphTopPanel.Resize += GraphTopPanel_Resize;

            _graphInfoMain = new Label
            {
                AutoSize = false,
                Location = new Point(6, 5),
                Size = new Size(360, 44),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _graphInfoSelected = new Label
            {
                AutoSize = false,
                Location = new Point(376, 5),
                Size = new Size(470, 44),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _graphTopPanel.Controls.Add(_graphInfoMain);
            _graphTopPanel.Controls.Add(_graphInfoSelected);

            panelStartMode.Controls.Add(_graphTopPanel);
            _graphTopPanel.BringToFront();

            GraphTopPanel_Resize(null, EventArgs.Empty);
        }

        private void PrepareGraphModeUi()
        {
            chart1.SendToBack();

            _graphTopPanel?.BringToFront();
            btnStart.BringToFront();
            btnStop.BringToFront();
            btnRead.BringToFront();
            btnClear.BringToFront();
            checkAutoScaleY.BringToFront();

            btnStart.Text = "Старт";
            btnStop.Text = "Стоп";
            btnRead.Text = "Прочитать";
            btnClear.Text = "Очистить";

            btnBigStart.Text = "ПУСК";
        }

        private void SetLinkControlsVisible(bool visible)
        {
            label1.Visible = visible;
            label2.Visible = visible;
            label3.Visible = visible;
            label6.Visible = visible;
            labelStatus.Visible = visible;
            labelSubStatus.Visible = visible;

            comboPorts.Visible = visible;
            comboBaud.Visible = visible;

            btnRefreshPorts.Visible = visible;
            btnConnect.Visible = visible;
            btnDisconnect.Visible = visible;
            btnLoadConfig.Visible = visible;
            btnWriteConfig.Visible = visible;

            if (visible)
                labelError.Visible = !string.IsNullOrWhiteSpace(labelError.Text);
            else
                labelError.Visible = false;
        }

        private void ShowStartMode()
        {
            tabControl1.SelectedTab = tabPage1;

            panelStartMode.Visible = false;
            btnBigStart.Visible = true;

            SetLinkControlsVisible(true);

            btnBigStart.BringToFront();
        }

        private void ShowGraphMode()
        {
            tabControl1.SelectedTab = tabPage1;

            SetLinkControlsVisible(false);

            btnBigStart.Visible = false;
            panelStartMode.Visible = true;
            panelStartMode.BringToFront();

            PrepareGraphModeUi();
        }
        private int GetActiveChannelsCount()
        {
            int count = 0;

            if (channels == null)
                return 0;

            for (int i = 0; i < channels.Length; i++)
            {
                if (channels[i] != null && channels[i].Enabled)
                    count++;
            }

            return count;
        }

        private int GetPreviewChannelIndex()
        {
            int rowIndex = gridChannels.CurrentCell?.RowIndex ?? -1;
            if (rowIndex >= 0 && rowIndex < channels.Length)
                return rowIndex;

            for (int i = 0; i < channels.Length; i++)
            {
                if (channels[i] != null && channels[i].Enabled)
                    return i;
            }

            return 0;
        }

        private static long ToTimeSec(long index, int intervalMs)
        {
            return (index * (long)intervalMs) / 1000;
        }

        private static string BuildCalibrationFormula(ChannelConfig cfg)
        {
            string bPart = cfg.B >= 0
                ? $"+ {cfg.B:0.#####}"
                : $"- {Math.Abs(cfg.B):0.#####}";

            return $"y = {cfg.K:0.#####} * x {bPart}";
        }

        private string GetLastMeasuredText(int channelNumber)
        {
            for (int i = samples.Count - 1; i >= 0; i--)
            {
                if (samples[i].Channel == channelNumber)
                    return $"{samples[i].Value:F3} {samples[i].Unit}";
            }

            int row = channelNumber - 1;
            if (row >= 0 && row < gridChannels.Rows.Count)
            {
                string value = Convert.ToString(gridChannels.Rows[row].Cells["colCurrent"].Value);
                string unit = Convert.ToString(gridChannels.Rows[row].Cells["colUnit"].Value);

                if (!string.IsNullOrWhiteSpace(value))
                    return $"{value} {unit}".Trim();
            }

            return "---";
        }

        private static string FmtDouble(double v)
        {
            if (double.IsNaN(v)) return "NaN";
            if (double.IsPositiveInfinity(v)) return "+Infinity";
            if (double.IsNegativeInfinity(v)) return "-Infinity";
            return v.ToString("0.######", CultureInfo.InvariantCulture);
        }
        private static string SafeSeriesPoint(Series s, int pointIndex)
        {
            try
            {
                if (s == null || pointIndex < 0 || pointIndex >= s.Points.Count)
                    return "";

                var p = s.Points[pointIndex];
                return $"X={FmtDouble(p.XValue)}, Y={FmtDouble(p.YValues[0])}";
            }
            catch
            {
                return "";
            }
        }

        private string AutoSaveFolderPath =>
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "autosave");

        private void EnsureAutoSaveSession()
        {
            if (_autoSaveWriter != null)
                return;

            Directory.CreateDirectory(AutoSaveFolderPath);

            _currentAutoSavePath = Path.Combine(
                AutoSaveFolderPath,
                $"DAM_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.csv");

            var fs = new FileStream(
                _currentAutoSavePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                4096,
                FileOptions.WriteThrough);

            _autoSaveWriter = new StreamWriter(fs, new UTF8Encoding(true))
            {
                AutoFlush = true
            };

            _autoSaveWriter.WriteLine("Channel;Time_sec;Value;Unit");
            _autoSaveWriter.Flush();

            RememberLastCsv(_currentAutoSavePath);
            SetSubStatus("Автозапись включена");
        }


        private void AppendAutoSavePoint(SampleLog item)
        {
            EnsureAutoSaveSession();

            long tSec = ToTimeSec(item.Index, item.IntervalMs);

            _autoSaveWriter.WriteLine(
                $"{item.Channel};{tSec};{item.Value.ToString("F6", CultureInfo.InvariantCulture)};{item.Unit}");
        }


        private void CloseAutoSaveSession()
        {
            if (_autoSaveWriter == null)
                return;

            try
            {
                _autoSaveWriter.Flush();
                _autoSaveWriter.Dispose();
            }
            finally
            {
                _autoSaveWriter = null;
            }
        }


        private void GraphTopPanel_Resize(object sender, EventArgs e)
        {
            if (_graphTopPanel == null || _graphInfoMain == null || _graphInfoSelected == null)
                return;

            _graphInfoMain.Left = 6;
            _graphInfoMain.Width = 360;
            _graphInfoSelected.Left = _graphInfoMain.Right + 10;
            _graphInfoSelected.Width = 470;
        }
        private void CopyAllReport_Click(object sender, EventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is string text)
                    Clipboard.SetText(text);
            }
            catch
            {
            }
        }

        private void ShowInfoDialog(string fullText)
        {
            var dlg = new Form
            {
                Text = "Информация об устройстве (полный отчёт)",
                StartPosition = FormStartPosition.CenterParent,
                Width = 1100,
                Height = 800,
                MinimizeBox = false,
                MaximizeBox = true,
                ShowInTaskbar = false
            };

            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                DetectUrls = false,
                Font = new Font("Consolas", 10F, FontStyle.Regular),
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.WindowText,
                Text = fullText
            };

            var bottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                Padding = new Padding(8)
            };

            var btnCopyAll = new Button
            {
                Text = "Копировать всё",
                Width = 150,
                Height = 28,
                Top = 8,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Tag = fullText
            };

            var btnClose = new Button
            {
                Text = "Закрыть",
                Width = 110,
                Height = 28,
                Top = 8,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                DialogResult = DialogResult.OK
            };

            bottom.Controls.Add(btnCopyAll);
            bottom.Controls.Add(btnClose);

            btnClose.Left = bottom.ClientSize.Width - btnClose.Width - 8;
            btnCopyAll.Left = btnClose.Left - btnCopyAll.Width - 8;

            btnCopyAll.Click += CopyAllReport_Click;

            dlg.Controls.Add(rtb);
            dlg.Controls.Add(bottom);

            dlg.AcceptButton = btnClose;
            dlg.CancelButton = btnClose;

            dlg.ShowDialog(this);
        }

        private void UpdateGraphHeaderUi()
        {
            if (_graphInfoMain == null || _graphInfoSelected == null)
                return;

            string startText = _measureStartTime.HasValue
                ? _measureStartTime.Value.ToString("dd.MM.yyyy HH:mm:ss")
                : "---";

            _graphInfoMain.Text =
                $"Старт: {startText}\r\n" +
                $"Период: {timer1.Interval / 1000} с | Активных: {GetActiveChannelsCount()} | Точек: {sampleIndex}";
            if (channels == null || channels.Length == 0 || channels.All(c => c == null))
            {
                _graphInfoSelected.Text = "Канал: ---";
                return;
            }

            int idx = GetPreviewChannelIndex();
            var cfg = channels[idx];

            string range = string.IsNullOrWhiteSpace(cfg.Range)
                ? DecodeInputTypeFromRegister(cfg.TypeCode)
                : cfg.Range;

            string unit = string.IsNullOrWhiteSpace(cfg.Unit) ? "-" : cfg.Unit;
            string current = GetLastMeasuredText(cfg.ChannelNumber);

            _graphInfoSelected.Text =
                $"CH{cfg.ChannelNumber} | {range} | Ед.: {unit} | {BuildCalibrationFormula(cfg)}\r\n" +
                $"Текущее: {current}";
        }

        private void gridChannels_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (_fillingGrid) return;
            if (gridChannels.IsCurrentCellDirty)
                gridChannels.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
        private void gridChannels_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_fillingGrid) return;
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var column = gridChannels.Columns[e.ColumnIndex];
            if (column == null)
                return;

            var row = gridChannels.Rows[e.RowIndex];

            // Изменили тип канала → подправить единицы измерения
            if (column.Name == "colType")
            {
                string type = Convert.ToString(row.Cells["colType"].Value);

                string unit;
                switch (type)
                {
                    case "0–10 В":
                    case "0–5 В":
                    case "±10 В":
                    case "±5 В":
                        unit = "В";
                        break;

                    case "0–20 мА":
                    case "4–20 мА":
                        unit = "мА";
                        break;

                    default:
                        unit = "";
                        break;
                }

                row.Cells["colUnit"].Value = unit;
                UpdateGraphHeaderUi();
            }

            // Изменили галочку "Включение"
            if (column.Name == "colEnabled")
            {
                // забираем новые значения Enabled из грида в массив channels
                ReadChannelsFromGrid();

                // включаем/выключаем серии на графике
                UpdateSeriesVisibility();
                UpdateGraphHeaderUi();
            }
        }

        private sealed class AppConfig
        {
            public ChannelConfig[] Channels { get; set; } = Array.Empty<ChannelConfig>();

            // Связь (выбор в UI, не факт подключения)
            public string SelectedPort { get; set; } = "";
            public string SelectedBaud { get; set; } = "9600";

            // Графики/опрос
            public int PollIntervalMs { get; set; } = 400;
            public bool AutoScaleY { get; set; } = true;

            // Твоя переменная
            public int AdcMax { get; set; } = 65535;
        }


        private void ApplyConfig(AppConfig cfg)
        {
            if (cfg == null) return;

            // Каналы
            if (cfg.Channels != null && cfg.Channels.Length == 8)
                channels = cfg.Channels;

            FillGridFromChannels();
            UpdateSeriesVisibility();

            // Связь (только выбор)
            if (!string.IsNullOrWhiteSpace(cfg.SelectedBaud))
            {
                int bi = comboBaud.Items.IndexOf(cfg.SelectedBaud);
                if (bi >= 0) comboBaud.SelectedIndex = bi;
                else comboBaud.Text = cfg.SelectedBaud;
            }

            if (!string.IsNullOrWhiteSpace(cfg.SelectedPort))
            {
                int pi = comboPorts.Items.IndexOf(cfg.SelectedPort);
                if (pi >= 0) comboPorts.SelectedIndex = pi;
                else comboPorts.Text = cfg.SelectedPort;
            }

            // Опрос/графики
            timer1.Interval = cfg.PollIntervalMs > 0 ? cfg.PollIntervalMs : 1000;

            checkAutoScaleY.Checked = cfg.AutoScaleY;
            UpdateGraphHeaderUi();
        }

        private void LoadLastConfig()
        {
            string json = WindowsFormsApp1.Properties.Settings.Default.LastConfigJson;
            if (string.IsNullOrWhiteSpace(json)) return;

            var cfg = JsonConvert.DeserializeObject<AppConfig>(json);
            ApplyConfig(cfg);
        }

        private void btnWriteConfig_Click(object sender, EventArgs e)
        {
            try
            {
                SaveLastLinkConfig();
                labelError.Visible = false;
                labelError.Text = "";
                SetSubStatus("Конфиг связи записан");
            }
            catch (Exception ex)
            {
                ShowError("Ошибка записи конфига", ex);
            }
        }

        private void btnLoadConfig_Click(object sender, EventArgs e)
        {
            try
            {
                LoadLastLinkConfig();
                labelError.Visible = false;
                labelError.Text = "";
                SetSubStatus("Конфиг связи загружен");
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки конфига", ex);
            }
        }

        public class ChannelConfig
        {
            public bool Enabled { get; set; }
            public int ChannelNumber { get; set; }   // 1..8

            public int TypeCode { get; set; }        // что лежит в 40201..40208
            public string Range { get; set; }        // строка, чтобы показывать человеку
            public string Unit { get; set; }         // "В", "мА", "°C" и т.п.

            public double K { get; set; }            // y = kx + b
            public double B { get; set; }            // y = kx + b

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            ResetAutoScale();

            if (client == null || !client.Connected)
            {
                ShowError("Ошибка", new InvalidOperationException("Нет подключения к устройству"));
                return;
            }

            if (!_measureStartTime.HasValue || sampleIndex == 0)
                _measureStartTime = DateTime.Now;

            EnsureAutoSaveSession();

            timer1.Start();
            UpdateGraphHeaderUi();
        }


        private void btnStop_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            UpdateGraphHeaderUi();
            ShowStartMode();
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (client == null || !client.Connected)
                    throw new InvalidOperationException("Нет подключения к устройству");

                long x = sampleIndex;
                DateTime measureTime = DateTime.Now;
                bool anyMeasured = false;

                for (int i = 0; i < channels.Length; i++)
                {
                    var cfg = channels[i];
                    if (!cfg.Enabled)
                        continue;

                    if (_breakSeriesOnNextPoint[i])
                    {
                        InsertSeriesBreakForChannel(i, x);
                        _breakSeriesOnNextPoint[i] = false;
                    }

                    double value = ReadChannelByIndex(i);
                    anyMeasured = true;

                    int editRow = gridChannels.CurrentCell?.RowIndex ?? -1;
                    bool editing = gridChannels.IsCurrentCellInEditMode;

                    if (i < gridChannels.Rows.Count && !(editing && editRow == i))
                        gridChannels.Rows[i].Cells["colCurrent"].Value = value.ToString("F3");
                    
                    var item = new SampleLog
                    {
                        Index = x,
                        IntervalMs = timer1.Interval,
                        Channel = cfg.ChannelNumber,
                        Value = value,
                        Unit = cfg.Unit
                    };

                    samples.Add(item);
                    AppendAutoSavePoint(item);

                    string seriesName = $"CH{cfg.ChannelNumber}";
                    var s = chart1.Series[seriesName];

                    long tSec = ToTimeSec(x, timer1.Interval);
                    s.Points.AddXY(tSec, value);

                    if (!checkAutoScaleY.Checked)
                        UpdateAutoScale(value);
                }

                if (!anyMeasured)
                    return;

                sampleIndex++;
                ApplyXAxisScale();

                if (checkAutoScaleY.Checked)
                {
                    RecalculateMinMaxFromSeries();
                    ApplyFullAutoScaleFromGlobal();
                }

                labelError.Visible = false;
                UpdateGraphHeaderUi();
            }
            catch (Exception ex)
            {
                timer1.Stop();
                CloseAutoSaveSession();
                ShowError("Ошибка при автоматическом чтении", ex);
            }
        }

        //=== Кнопка Прочитать ===
        private void btnRead_Click(object sender, EventArgs e)
        {
            try
            {
                if (gridChannels.IsCurrentCellInEditMode)
                    gridChannels.EndEdit();
                ReadChannelsFromGrid();
                if (client == null || !client.Connected)
                    throw new InvalidOperationException("Нет подключения к устройству");

                // один шаг измерения по всем включённым каналам,
                // лог + точка(точки) на графике
                if (!_measureStartTime.HasValue || sampleIndex == 0)
                    _measureStartTime = DateTime.Now;
                EnsureAutoSaveSession();
                DoSingleMeasurementStep();
                UpdateGraphHeaderUi();

                labelError.Visible = false;
                labelError.Text = "";
            }
            catch (Exception ex)
            {
                ShowError("Ошибка при ручном чтении", ex);
            }
        }


        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(comboPorts.Text))
            {
                ShowError("Ошибка подключения", new ArgumentException("Не выбран COM-порт"));
                return;
            }

            try
            {
                string portName = comboPorts.Text;
                int baud = int.Parse(comboBaud.Text);

                if (client != null && client.Connected)
                {
                    try { client.Disconnect(); }
                    catch { }
                }

                client = new ModbusClient(portName)
                {
                    UnitIdentifier = 1,
                    Baudrate = baud,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    ConnectionTimeout = 2000
                };

                client.Connect();

                isConnected = true;
                labelStatus.Text = "Подключено";
                labelError.Visible = false;
            }
            catch (Exception ex)
            {
                isConnected = false;
                ShowError("Ошибка подключения", ex);
            }
            UpdateGraphHeaderUi();
            ShowStartMode();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (client != null && client.Connected)
                    client.Disconnect();
            }
            catch
            {
            }

            timer1.Stop();
            CloseAutoSaveSession();

            isConnected = false;
            labelError.Text = "";
            labelError.Visible = false;
            labelStatus.Text = "Отключено";

            UpdateGraphHeaderUi();
        }
        // для графиков
        private void ApplyXAxisScale()
        {
            var ax = chart1.ChartAreas[0].AxisX;

            long x = Math.Max(0, ((sampleIndex - 1) * (long)timer1.Interval) / 1000);

            double min = 0;
            double max;
            double interval;

            if (x <= 1)
            {
                max = 5;
                interval = 1;
            }
            else if (x <= 5)
            {
                max = 5;
                interval = 1;
            }
            else if (x <= 10)
            {
                max = 10;
                interval = 2;
            }
            else if (x <= 20)
            {
                max = 20;
                interval = 5;
            }
            else if (x <= 50)
            {
                max = 50;
                interval = 10;
            }
            else if (x <= 100)
            {
                max = 100;
                interval = 20;
            }
            else if (x <= 200)
            {
                max = 200;
                interval = 50;
            }
            else
            {
                min = x - 200;
                max = x;
                interval = NiceInterval((max - min) / 5.0);
            }

            ax.Minimum = min;
            ax.Maximum = max;
            ax.Interval = interval;
            ax.LabelStyle.Format = "0";
            ax.MajorGrid.Interval = interval;
        }

        private static double NiceInterval(double raw)
        {
            if (raw <= 0) return 1;

            double exp = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double f = raw / exp;

            double nice;
            if (f <= 1) nice = 1;
            else if (f <= 2) nice = 2;
            else if (f <= 2.5) nice = 2.5;
            else if (f <= 5) nice = 5;
            else nice = 10;

            return nice * exp;
        }
        private void RefreshPorts()
        {
            comboPorts.Items.Clear();
            comboPorts.Items.AddRange(SerialPort.GetPortNames());

            if (comboPorts.Items.Count > 0)
                comboPorts.SelectedIndex = 0;
        }

        private void btnRefreshPorts_Click(object sender, EventArgs e)
        {
            RefreshPorts();
        }

        private void ShowError(string title, Exception ex)
        {
            string message;

            // 1) ВАЖНО: сначала самые частые и самые "вводящие в заблуждение" ошибки
            // Index out of range / invalid index — это НЕ COM-порт.
            if (ex is ArgumentOutOfRangeException || ex is IndexOutOfRangeException)
                message = "Ошибка индекса: таблица/массив каналов повреждены или пустые";
            // Формат числа (например baudrate парсится не числом)
            else if (ex is FormatException)
                message = "Некорректный формат числа (скорость/параметр введён неверно)";
            // 2) Связь/порт
            else if (ex is TimeoutException)
                message = "Устройство не отвечает";
            else if (ex is UnauthorizedAccessException)
                message = "Порт занят другой программой";
            else if (ex is IOException)
                message = "Проблема с адаптером или кабелем";
            // ArgumentException часто ловит и “не тот COM”, и кривые аргументы библиотек.
            // Поэтому уточняем по тексту.
            else if (ex is ArgumentException)
            {
                var msg = ex.Message ?? "";

                if (msg.IndexOf("COM", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("порт", StringComparison.OrdinalIgnoreCase) >= 0)
                    message = "Некорректное имя COM-порта";
                else if (msg.IndexOf("index", StringComparison.OrdinalIgnoreCase) >= 0)
                    message = "Ошибка индекса: таблица/массив каналов повреждены или пустые";
                else
                    message = "Некорректный аргумент (проверь параметры ввода/настройки)";
            }
            // 3) Скорость/baud
            else if (!string.IsNullOrEmpty(ex.Message) &&
                     (ex.Message.IndexOf("скорост", StringComparison.OrdinalIgnoreCase) >= 0 ||
                      ex.Message.IndexOf("baud", StringComparison.OrdinalIgnoreCase) >= 0))
                message = "Несовпадение скорости обмена между программой и устройством";
            // 4) Остальное
            else
                message = "Неизвестная ошибка";

            labelError.Visible = true;
            labelError.Text = $"Ошибка: {message}\n({ex.Message})";

            SetSubStatus(title);
            ShowStartMode();
        }

        private void InitChannelsGrid()
        {
            gridChannels.AllowUserToAddRows = false;
            gridChannels.AllowUserToDeleteRows = false;
            gridChannels.RowHeadersVisible = false;

            gridChannels.Rows.Clear();
        }


        private void InitChannelConfigs()
        {
            channels[0] = new ChannelConfig
            {
                Enabled = true,
                ChannelNumber = 1,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "В",
                K = -0.30599,
                B = 123.4606
            };

            channels[1] = new ChannelConfig
            {
                Enabled = true,
                ChannelNumber = 2,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "В",
                K = -0.32777,
                B = 77.4365
            };

            channels[2] = new ChannelConfig
            {
                Enabled = true,
                ChannelNumber = 3,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "В",
                K = -0.3258,
                B = 36.9236
            };

            channels[3] = new ChannelConfig
            {
                Enabled = true,
                ChannelNumber = 4,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "В",
                K = -0.30551,
                B = 68.2308
            };

            channels[4] = new ChannelConfig
            {
                Enabled = true,
                ChannelNumber = 5,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "В",
                K = -0.33117,
                B = -54.2287
            };

            channels[5] = new ChannelConfig
            {
                Enabled = true,
                ChannelNumber = 6,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "В",
                K = 1.0,
                B = 0.0
            };

            channels[6] = new ChannelConfig
            {
                Enabled = true,
                ChannelNumber = 7,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "В",
                K = 1.0,
                B = 0.0
            };

            channels[7] = new ChannelConfig
            {
                Enabled = true,
                ChannelNumber = 8,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "В",
                K = 1.0,
                B = 0.0
            };
        }

        private void FillGridFromChannels()
        {
            if (_fillingGrid) return;

            try
            {
                _fillingGrid = true;

                gridChannels.Rows.Clear();

                for (int i = 0; i < channels.Length; i++)
                {
                    var cfg = channels[i];

                    int r = gridChannels.Rows.Add();
                    var row = gridChannels.Rows[r];

                    row.Cells["colEnabled"].Value = cfg.Enabled;
                    row.Cells["colChannel"].Value = cfg.ChannelNumber;

                    switch (cfg.TypeCode)
                    {
                        case 0:
                        case 28:
                            row.Cells["colType"].Value = "0–10 В";
                            cfg.Unit = "В";
                            break;

                        case 29:
                            row.Cells["colType"].Value = "0–5 В";
                            cfg.Unit = "В";
                            break;

                        case 1:
                        case 8:
                            row.Cells["colType"].Value = "±10 В";
                            cfg.Unit = "В";
                            break;

                        case 2:
                        case 9:
                            row.Cells["colType"].Value = "±5 В";
                            cfg.Unit = "В";
                            break;

                        case 27:
                            row.Cells["colType"].Value = "0–20 мА";
                            cfg.Unit = "мА";
                            break;

                        case 3:
                        case 7:
                            row.Cells["colType"].Value = "4–20 мА";
                            cfg.Unit = "мА";
                            break;

                        default:
                            row.Cells["colType"].Value = $"код {cfg.TypeCode}";
                            // НЕ МЕНЯТЬ cfg.TypeCode
                            break;
                    }
                    row.Cells["colUnit"].Value = cfg.Unit;


                    row.Cells["colK"].Value = cfg.K;
                    row.Cells["colB"].Value = cfg.B;
                    row.Cells["colCurrent"].Value = "";
                    row.Cells["colUnit"].Value = cfg.Unit;
                }
            }
            finally
            {
                _fillingGrid = false;
            }
            //Удалить этот цикл на случай работы 8 каналов
            //for (int i = 0; i < gridChannels.Rows.Count; i++)
            //{
                //gridChannels.Rows[i].Visible = i < VISIBLE_CHANNELS;
            //}

        }



        private void ReadChannelsFromGrid()
        {
            // если грид пустой/сломанный — восстанавливаем 8 строк из channels[]
            if (gridChannels.Rows.Count < 8)
                FillGridFromChannels();

            // если после восстановления всё ещё меньше 8 — выходим без падения
            if (gridChannels.Rows.Count < 8)
                return;

            for (int i = 0; i < 8; i++)
            {
                var row = gridChannels.Rows[i];
                var cfg = channels[i];

                cfg.Enabled = Convert.ToBoolean(row.Cells["colEnabled"].Value);
                cfg.ChannelNumber = i + 1;

                cfg.K = Convert.ToDouble(row.Cells["colK"].Value);
                cfg.B = Convert.ToDouble(row.Cells["colB"].Value);
                cfg.Unit = Convert.ToString(row.Cells["colUnit"].Value);
                string typeUi = Convert.ToString(row.Cells["colType"].Value) ?? "";

                switch (typeUi)
                {
                    case "0–10 В":
                        cfg.TypeCode = 28;
                        cfg.Range = "0–10 В";
                        cfg.Unit = "В";
                        break;

                    case "0–5 В":
                        cfg.TypeCode = 29;
                        cfg.Range = "0–5 В";
                        cfg.Unit = "В";
                        break;

                    case "±10 В":
                        cfg.TypeCode = 8;
                        cfg.Range = "±10 В";
                        cfg.Unit = "В";
                        break;

                    case "±5 В":
                        cfg.TypeCode = 9;
                        cfg.Range = "±5 В";
                        cfg.Unit = "В";
                        break;

                    case "0–20 мА":
                        cfg.TypeCode = 27;
                        cfg.Range = "0–20 мА";
                        cfg.Unit = "мА";
                        break;

                    case "4–20 мА":
                        cfg.TypeCode = 7;
                        cfg.Range = "4–20 мА";
                        cfg.Unit = "мА";
                        break;
                }

                // если юзер руками поменял Unit — оставляешь как есть
                // но чтобы не было бардака: обнови ячейку Unit из cfg.Unit, если хочешь жёстко

            }
        }



        private void UpdateSeriesVisibility()
        {
            for (int i = 0; i < channels.Length; i++)
            {
                string seriesName = $"CH{i + 1}";
                int idx = chart1.Series.IndexOf(seriesName);
                if (idx < 0)
                    continue;

                //Закомментировать одну из строк на случай работы 8 каналов
                bool enabled = channels[i].Enabled; // для 8 каналов
                //bool enabled = i < VISIBLE_CHANNELS && channels[i].Enabled; // для 5 каналов
                var s = chart1.Series[idx];

                bool wasEnabled = s.Enabled;

                s.Enabled = enabled;
                s.IsVisibleInLegend = enabled;

                if (!wasEnabled && enabled && s.Points.Count > 0)
                    _breakSeriesOnNextPoint[i] = true;
            }

            if (checkAutoScaleY.Checked)
            {
                RecalculateMinMaxFromSeries();
                ApplyFullAutoScaleFromGlobal();
            }
        }


        private void InsertSeriesBreakForChannel(int channelIndex, long x)
        {
            if (channelIndex < 0 || channelIndex >= channels.Length)
                return;

            var cfg = channels[channelIndex];
            if (cfg == null)
                return;

            string seriesName = $"CH{cfg.ChannelNumber}";
            int idx = chart1.Series.IndexOf(seriesName);
            if (idx < 0)
                return;

            var s = chart1.Series[idx];

            var p = new DataPoint();
            p.XValue = ToTimeSec(x, timer1.Interval);
            p.IsEmpty = true;

            s.Points.Add(p);
        }

        private void btnLoadConfigFile_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Filter = "CSV файлы (*.csv)|*.csv";

                    if (dlg.ShowDialog() != DialogResult.OK) return;

                    ValidateDamCsvOrThrow(dlg.FileName);
                    LoadMeasurementsCsvToChart(dlg.FileName);
                }

                labelError.Visible = false;
                labelError.Text = "";
                SetSubStatus("CSV загружен");
            }
            catch (Exception ex)
            {
                ShowError("Ошибка открытия CSV", ex);
            }
        }

        private void LoadMeasurementsCsvToChart(string path)
        {
            var loaded = ReadSamplesFromCsv(path);

            samples = loaded;
            sampleIndex = samples.Count == 0 ? 0 : (samples.Max(x => x.Index) + 1);

            foreach (Series s in chart1.Series)
                if (s.Name.StartsWith("CH"))
                    s.Points.Clear();

            for (int ch = 1; ch <= 8; ch++)
            {
                string seriesName = $"CH{ch}";
                int idx = chart1.Series.IndexOf(seriesName);
                if (idx < 0)
                    continue;

                var s = chart1.Series[seriesName];

                var ordered = samples
                    .Where(x => x.Channel == ch)
                    .OrderBy(x => x.Index)
                    .ToList();

                foreach (var p in ordered)
                    s.Points.AddXY(ToTimeSec(p.Index, p.IntervalMs), p.Value);
            }

            if (checkAutoScaleY.Checked)
            {
                RecalculateMinMaxFromSeries();
                ApplyFullAutoScaleFromGlobal();
            }
            else
            {
                ResetAutoScale();
                foreach (var item in samples)
                    UpdateAutoScale(item.Value);
            }

            _measureStartTime = samples.Count == 0 ? (DateTime?)null : samples.Min(x => x.Timestamp);

            UpdateGraphHeaderUi();
            ApplyXAxisScale();
        }
        private List<SampleLog> ReadSamplesFromCsv(string path)
        {
            var list = new List<SampleLog>();
            var ru = CultureInfo.GetCultureInfo("ru-RU");
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);

            if (lines.Length == 0)
                return list;

            string headerLine = lines.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";
            var headers = headerLine.Split(';').Select(x => (x ?? "").Trim()).ToList();

            bool oldFormat =
                headers.Any(x => x.Equals("Channel", StringComparison.OrdinalIgnoreCase)) &&
                headers.Any(x => x.Equals("Time_sec", StringComparison.OrdinalIgnoreCase)) &&
                headers.Any(x => x.Equals("Value", StringComparison.OrdinalIgnoreCase)) &&
                headers.Any(x => x.Equals("Unit", StringComparison.OrdinalIgnoreCase));

            if (oldFormat)
            {
                int idxChannel = headers.FindIndex(x => x.Equals("Channel", StringComparison.OrdinalIgnoreCase));
                int idxTime = headers.FindIndex(x => x.Equals("Time_sec", StringComparison.OrdinalIgnoreCase));
                int idxValue = headers.FindIndex(x => x.Equals("Value", StringComparison.OrdinalIgnoreCase));
                int idxUnit = headers.FindIndex(x => x.Equals("Unit", StringComparison.OrdinalIgnoreCase));

                foreach (var rawLine in lines)
                {
                    var line = (rawLine ?? "").Trim();
                    if (line.Length == 0)
                        continue;

                    if (line.StartsWith("Channel", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var parts = line.Split(';');
                    if (parts.Length <= Math.Max(Math.Max(idxChannel, idxTime), Math.Max(idxValue, idxUnit)))
                        continue;

                    if (!int.TryParse(parts[idxChannel].Trim(), out int ch))
                        continue;

                    string timeText = parts[idxTime].Trim();
                    if (!long.TryParse(timeText, NumberStyles.Integer, ru, out long timeSec) &&
                        !long.TryParse(timeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out timeSec))
                        continue;

                    string valueText = parts[idxValue].Trim();
                    if (!double.TryParse(valueText, NumberStyles.Float, ru, out double value) &&
                        !double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        continue;

                    string unit = parts[idxUnit].Trim();

                    long index = timeSec;
                    DateTime ts = DateTime.Today.AddSeconds(timeSec);

                    list.Add(new SampleLog
                    {
                        Channel = ch,
                        Index = index,
                        IntervalMs = 1000,
                        Value = value,
                        Unit = unit,
                        Timestamp = ts
                    });
                }

                return list;
            }

            long rowIndex = 0;

            foreach (var rawLine in lines)
            {
                var line = (rawLine ?? "").Trim();
                if (line.Length == 0)
                    continue;

                if (line.StartsWith("Time", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = line.Split(';');
                if (parts.Length < 9)
                    continue;

                string timeText = parts[0].Trim();
                if (!DateTime.TryParseExact(timeText, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedTime) &&
                    !DateTime.TryParseExact(timeText, "HH:mm:ss", ru, DateTimeStyles.None, out parsedTime))
                    continue;

                DateTime ts = DateTime.Today
                    .AddHours(parsedTime.Hour)
                    .AddMinutes(parsedTime.Minute)
                    .AddSeconds(parsedTime.Second);

                for (int ch = 1; ch <= 8; ch++)
                {
                    string valueText = parts[ch].Trim();
                    if (string.IsNullOrWhiteSpace(valueText))
                        continue;

                    if (!double.TryParse(valueText, NumberStyles.Float, ru, out double value) &&
                        !double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        continue;

                    string unit = (ch - 1 >= 0 && ch - 1 < channels.Length && channels[ch - 1] != null)
                        ? (channels[ch - 1].Unit ?? "")
                        : "";

                    list.Add(new SampleLog
                    {
                        Channel = ch,
                        Index = rowIndex,
                        IntervalMs = 1000,
                        Value = value,
                        Unit = unit,
                        Timestamp = ts
                    });
                }

                rowIndex++;
            }

            return list;
        }


        // Перевод raw → x через 65536
        private const double ADC_FULL = 20000.0;
        private const double ADC_MID = 10000.0;
        private double ConvertRawToPhysical(int raw, ChannelConfig cfg)
        {
            int code = cfg.TypeCode;

            // 0–10 В: 0 или 28
            if (code == 0 || code == 28)
                return raw * 10.0 / ADC_FULL;

            // 0–5 В: 29
            if (code == 29)
                return raw * 5.0 / ADC_FULL;

            // ±10 В: 1 или 8
            if (code == 1 || code == 8)
                return (raw - ADC_MID) * 10.0 / ADC_MID;

            // ±5 В: 2 или 9
            if (code == 2 || code == 9)
                return (raw - ADC_MID) * 5.0 / ADC_MID;

            // 0–20 мА: 27
            if (code == 27)
                return raw * 20.0 / ADC_FULL;

            // 4–20 мА: 3 или 7 (x считаем как 0..20 мА, перевод в 4..20 делается k/b)
            if (code == 3 || code == 7)
                return raw * 20.0 / ADC_FULL;

            return raw;
        }


        private double ApplyCalibration(double x, ChannelConfig cfg)
        {
            return cfg.K * x + cfg.B;
        }


        //=== работа y=kx+b ===

        private void gridChannels_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_fillingGrid) return;
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var colName = gridChannels.Columns[e.ColumnIndex].Name;

            if (colName == "colK" || colName == "colB" || colName == "colUnit")
            {
                ReadChannelsFromGrid();

                // чтобы визуально было понятно, что применилось
                SetSubStatus("Калибровка обновлена");
                UpdateGraphHeaderUi();
            }
        }

        private void gridChannels_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (_kbEditor != null)
                _kbEditor.TextChanged -= kbEditor_TextChanged;

            _kbEditor = e.Control as TextBox;
            _kbEditRow = gridChannels.CurrentCell?.RowIndex ?? -1;
            _kbEditCol = gridChannels.CurrentCell?.OwningColumn?.Name ?? "";

            if (_kbEditor == null) return;

            if (_kbEditRow < 0 || _kbEditRow >= channels.Length) return;

            if (_kbEditCol == "colK" || _kbEditCol == "colB")
                _kbEditor.TextChanged += kbEditor_TextChanged;
        }

        private void kbEditor_TextChanged(object sender, EventArgs e)
        {
            if (_kbEditRow < 0 || _kbEditRow >= channels.Length) return;
            if (!(_kbEditCol == "colK" || _kbEditCol == "colB")) return;

            string text = _kbEditor?.Text ?? "";
            if (!double.TryParse(text, out double val))
                return;

            if (_kbEditCol == "colK") channels[_kbEditRow].K = val;
            if (_kbEditCol == "colB") channels[_kbEditRow].B = val;
        }


        private void InitVahSeries()
        {
            if (!chart1.Series.IsUniqueName("VAH"))
                chart1.Series.Remove(chart1.Series["VAH"]);

            var s = chart1.Series.Add("VAH");
            s.ChartType = SeriesChartType.Point;
            s.IsVisibleInLegend = true;
        }


        private void btnSaveVah_Click(object sender, EventArgs e)
        {
            btnSave_Click(sender, e);
        }

        private void gridChannels_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        private void UpdateAutoScale(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return;

            if (value < yGlobalMin) yGlobalMin = value;
            if (value > yGlobalMax) yGlobalMax = value;

            if (double.IsInfinity(yGlobalMin) || double.IsInfinity(yGlobalMax))
                return;

            var a = chart1.ChartAreas[0];
            var axisY = a.AxisY;

            a.AxisX.Interval = double.NaN;
            a.AxisX.MajorGrid.Interval = double.NaN;
            a.AxisX.MajorTickMark.Interval = double.NaN;
            a.AxisX.LabelStyle.Interval = double.NaN;

            a.AxisY.Interval = double.NaN;
            a.AxisY.MajorGrid.Interval = double.NaN;
            a.AxisY.MajorTickMark.Interval = double.NaN;
            a.AxisY.LabelStyle.Interval = double.NaN;

            axisY.IsStartedFromZero = false;

            double min = yGlobalMin;
            double max = yGlobalMax;

            if (max <= min)
                max = min + 0.000001;

            axisY.Minimum = min;
            axisY.Maximum = max;
            axisY.LabelStyle.Format = "0.#####";
        }



        private void ResetAutoScale()
        {
            yGlobalMin = double.PositiveInfinity;
            yGlobalMax = double.NegativeInfinity;

            var axisY = chart1.ChartAreas[0].AxisY;
            axisY.Minimum = double.NaN;
            axisY.Maximum = double.NaN;
        }


        private void checkAutoScaleY_CheckedChanged(object sender, EventArgs e)
        {
            if (checkAutoScaleY.Checked)
            {
                RecalculateMinMaxFromSeries();
                ApplyFullAutoScaleFromGlobal();
            }
        }

        private void RecalculateMinMaxFromSeries()
        {
            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;

            foreach (Series s in chart1.Series)
            {
                if (s.Name == "VAH")
                    continue;

                if (!s.Enabled)          // серия скрыта → не учитывается
                    continue;

                foreach (var p in s.Points)
                {
                    double v = p.YValues[0];
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            }

            if (double.IsPositiveInfinity(min))
            {
                yGlobalMin = double.PositiveInfinity;
                yGlobalMax = double.NegativeInfinity;
                return;
            }

            yGlobalMin = min;
            yGlobalMax = max;
        }



        private void ApplyFullAutoScaleFromGlobal()
        {
            if (double.IsInfinity(yGlobalMin) || double.IsInfinity(yGlobalMax))
                return;

            var axisY = chart1.ChartAreas[0].AxisY;

            axisY.IsStartedFromZero = false;
            axisY.Minimum = yGlobalMin;
            axisY.Maximum = yGlobalMax;
            axisY.Interval = 0;
            axisY.LabelStyle.Format = "0.#####";
        }
        

        // === Кнопка очистить ===
        private void btnClear_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Очистить график измерений?",
                "Очистка графика",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
                return;

            foreach (Series s in chart1.Series)
            {
                if (s.Name == "VAH" || s.Name.StartsWith("CH"))
                    s.Points.Clear();
            }

            // чистим колонку Тек.
            for (int i = 0; i < gridChannels.Rows.Count; i++)
                gridChannels.Rows[i].Cells["colCurrent"].Value = "";

            CloseAutoSaveSession();
            _currentAutoSavePath = "";

            samples.Clear();
            sampleIndex = 0;
            _measureStartTime = null;
            _breakSeriesOnNextPoint = new bool[8];

            ResetAutoScale();
            SetSubStatus("График очищен");
            labelError.Visible = false;
            labelError.Text = "";
            UpdateGraphHeaderUi();
        }

        //===save===///
        private class SampleLog
        {
            public long Index;          // номер шага измерения
            public int IntervalMs;      // период на момент измерения
            public int Channel;         // 1..8
            public double Value;        // значение
            public string Unit;         // "В", "мА"
            public DateTime Timestamp;  // реальное время ПК
        }
        private void SaveChannelAllPointsToFile(int channelNumber, string path)
        {
            if (channelNumber < 1 || channelNumber > 8)
                throw new ArgumentOutOfRangeException(nameof(channelNumber));

            if (samples.Count == 0)
                throw new InvalidOperationException("Лог измерений пустой — сохранять нечего.");

            var sb = new StringBuilder();
            sb.AppendLine("Channel;Time_sec;Value;Unit");

            bool any = false;

            foreach (var x in samples)
            {
                if (x.Channel != channelNumber)
                    continue;

                long tSec = ToTimeSec(x.Index, x.IntervalMs);
                sb.AppendLine($"{x.Channel};{tSec};{x.Value:F6};{x.Unit}");
                any = true;
            }

            if (!any)
                throw new InvalidOperationException($"Нет точек в логе для CH{channelNumber}.");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
        private int GetSelectedChannelNumber()
        {
            if (gridChannels.CurrentCell == null)
                return 1;

            int rowIndex = gridChannels.CurrentCell.RowIndex;
            if (rowIndex < 0 || rowIndex >= 8)
                return 1;

            object v = gridChannels.Rows[rowIndex].Cells["colChannel"].Value;

            if (v != null && int.TryParse(v.ToString(), out int ch) && ch >= 1 && ch <= 8)
                return ch;

            return rowIndex + 1;
        }

        private void btnSaveChannelGraph_Click(object sender, EventArgs e)
        {
            try
            {
                int ch = GetSelectedChannelNumber();

                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "CSV файлы (*.csv)|*.csv";
                    dlg.FileName = $"CH{ch}_ALL.csv";

                    if (dlg.ShowDialog() != DialogResult.OK)
                        return;

                    SaveChannelAllPointsToFile(ch, dlg.FileName);
                }

                labelError.Visible = false;
                labelError.Text = "";
                SetSubStatus($"Сохранены все точки CH{ch}");
            }
            catch (Exception ex)
            {
                ShowError("Ошибка сохранения всех точек канала", ex);
            }
        }

        private void SaveMeasurementsToFile(string path)
        {
            if (_autoSaveWriter != null)
                _autoSaveWriter.Flush();

            if (!string.IsNullOrWhiteSpace(_currentAutoSavePath) && File.Exists(_currentAutoSavePath))
            {
                string src = Path.GetFullPath(_currentAutoSavePath);
                string dst = Path.GetFullPath(path);

                if (!string.Equals(src, dst, StringComparison.OrdinalIgnoreCase))
                    File.Copy(src, dst, true);

                return;
            }

            if (samples == null || samples.Count == 0)
                throw new InvalidOperationException("Нет данных: лог измерений пустой.");

            var ordered = samples
                .OrderBy(x => x.Channel)
                .ThenBy(x => ToTimeSec(x.Index, x.IntervalMs))
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Channel;Time_sec;Value;Unit");

            int lastCh = -1;

            foreach (var x in ordered)
            {
                if (lastCh != -1 && x.Channel != lastCh)
                    sb.AppendLine("");

                long tSec = ToTimeSec(x.Index, x.IntervalMs);
                sb.AppendLine($"{x.Channel};{tSec};{x.Value:F6};{x.Unit}");
                lastCh = x.Channel;
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string defaultName = DateTime.Now.ToString("yyyy_MM_dd") + ".csv";

                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "CSV файлы (*.csv)|*.csv";
                    dlg.FileName = defaultName;

                    if (dlg.ShowDialog() != DialogResult.OK)
                        return;

                    SaveMeasurementsToFile(dlg.FileName);
                    RememberLastCsv(dlg.FileName);
                }

                labelError.Visible = false;
                labelError.Text = "";
                SetSubStatus($"Сохранён {defaultName}");
            }
            catch (Exception ex)
            {
                ShowError("Ошибка сохранения измерений", ex);
            }
        }
        private void ValidateDamCsvOrThrow(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException("Файл не найден", path);

            string header = File.ReadLines(path, Encoding.UTF8)
                                .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "";

            char[] seps = new[] { ';', ',', '\t' };

            var cols = header
                .Split(seps)
                .Select(x => (x ?? "").Trim())
                .Where(x => x.Length > 0)
                .ToList();

            bool oldFormat =
                cols.Any(x => x.Equals("Channel", StringComparison.OrdinalIgnoreCase)) &&
                cols.Any(x => x.Equals("Time_sec", StringComparison.OrdinalIgnoreCase)) &&
                cols.Any(x => x.Equals("Value", StringComparison.OrdinalIgnoreCase)) &&
                cols.Any(x => x.Equals("Unit", StringComparison.OrdinalIgnoreCase));

            bool newFormat =
                cols.Count >= 9 &&
                cols[0].Equals("Time", StringComparison.OrdinalIgnoreCase) &&
                cols[1].Equals("CH1", StringComparison.OrdinalIgnoreCase) &&
                cols[2].Equals("CH2", StringComparison.OrdinalIgnoreCase) &&
                cols[3].Equals("CH3", StringComparison.OrdinalIgnoreCase) &&
                cols[4].Equals("CH4", StringComparison.OrdinalIgnoreCase) &&
                cols[5].Equals("CH5", StringComparison.OrdinalIgnoreCase) &&
                cols[6].Equals("CH6", StringComparison.OrdinalIgnoreCase) &&
                cols[7].Equals("CH7", StringComparison.OrdinalIgnoreCase) &&
                cols[8].Equals("CH8", StringComparison.OrdinalIgnoreCase);

            if (!oldFormat && !newFormat)
                throw new InvalidDataException("Неверный CSV: ожидается либо старый формат Channel;Time_sec;Value;Unit, либо новый формат Time;CH1;CH2;CH3;CH4;CH5;CH6;CH7;CH8");
        }
        private void btnSaveChannelAll_Click(object sender, EventArgs e)
        {
            try
            {
                int ch = GetSelectedChannelNumber();

                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "CSV файлы (*.csv)|*.csv";
                    dlg.FileName = $"CH{ch}_ALL.csv";

                    if (dlg.ShowDialog() != DialogResult.OK)
                        return;

                    SaveChannelAllPointsToFile(ch, dlg.FileName); // этот метод тоже должен существовать
                }

                labelError.Visible = false;
                labelError.Text = "";
                SetSubStatus($"Сохранены все точки CH{ch}");
            }
            catch (Exception ex)
            {
                ShowError("Ошибка сохранения всех точек канала", ex);
            }
        }


        // ===информация
        private long sampleIndex = 0;                 // глобальный счётчик точек
        private List<SampleLog> samples = new List<SampleLog>();  // весь лог

        // === для информации ===
        // тип входа по коду из регистров 40201–40208 (таблица типов входа)
        private string DecodeInputTypeFromRegister(int code)
        {
            switch (code)
            {
                case 7: return "4–20 мА";
                case 8: return "±10 В";
                case 9: return "±5 В";
                case 27: return "0–20 мА";
                case 28: return "0–10 В";
                case 29: return "0–5 В";
                default: return "неизвестный тип";
            }
        }

        // скорость по коду из регистра 40182 (таблица скоростей)
        private string DecodeBaudFromRegister(int code)
        {
            switch (code)
            {
                case 3: return "1200";
                case 4: return "2400";
                case 5: return "4800";
                case 6: return "9600";
                case 7: return "19200";
                case 8: return "38400";
                case 9: return "57600";
                case 10: return "115200";
                default: return "неизвестная";
            }
        }

        // ===информация ===
        private static string CellStr(DataGridViewRow row, string colName)
        {
            try
            {
                var v = row.Cells[colName].Value;
                return v == null ? "" : Convert.ToString(v);
            }
            catch { return ""; }
        }

        private static string RegFmt(int v)
        {
            // и десятичное, и hex — удобно при сверке
            return $"{v} (0x{v:X4})";
        }

        private bool TryReadRegs(int startAddress, int count, out int[] regs, out string err)
        {
            regs = null;
            err = null;

            try
            {
                if (client == null || !client.Connected)
                {
                    err = "нет подключения";
                    return false;
                }

                regs = client.ReadHoldingRegisters(startAddress, count);
                return true;
            }
            catch (Exception ex)
            {
                err = ex.Message;
                return false;
            }
        }
        private void btnDeviceInfo_Click(object sender, EventArgs e)
        {
            var sb = new StringBuilder();

            bool connected = client != null && client.Connected;

            int selectedTabIndex = -1;
            string selectedTabName = "";
            string selectedTabText = "";

            if (tabControl1 != null)
            {
                selectedTabIndex = tabControl1.SelectedIndex;

                if (selectedTabIndex >= 0 && selectedTabIndex < tabControl1.TabPages.Count)
                {
                    selectedTabName = tabControl1.TabPages[selectedTabIndex].Name;
                    selectedTabText = tabControl1.TabPages[selectedTabIndex].Text;
                }
            }

            int selectedGridRow = -1;
            int selectedGridCol = -1;
            string selectedGridColName = "";

            if (gridChannels != null && gridChannels.CurrentCell != null)
            {
                selectedGridRow = gridChannels.CurrentCell.RowIndex;
                selectedGridCol = gridChannels.CurrentCell.ColumnIndex;

                if (selectedGridCol >= 0 && selectedGridCol < gridChannels.Columns.Count)
                    selectedGridColName = gridChannels.Columns[selectedGridCol].Name;
            }

            sb.AppendLine("=== СНИМОК ПРОГРАММЫ ===");
            sb.AppendLine("Время отчёта: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"));
            sb.AppendLine("AppDomain.BaseDirectory: " + AppDomain.CurrentDomain.BaseDirectory);
            sb.AppendLine("LinkConfigPath: " + LinkConfigPath);
            sb.AppendLine("LastChannelsConfigPath: " + LastChannelsConfigPath);
            sb.AppendLine("LastCsvPathFile: " + LastCsvPathFile);
            sb.AppendLine("LastCsv remembered: " + GetLastCsv());
            sb.AppendLine("Form.Name: " + Name);
            sb.AppendLine("Form.Text: " + Text);
            sb.AppendLine("Form.WindowState: " + WindowState);
            sb.AppendLine("Form.Size: " + Width + "x" + Height);
            sb.AppendLine("Form.ClientSize: " + ClientSize.Width + "x" + ClientSize.Height);
            sb.AppendLine("SelectedTab.Index: " + selectedTabIndex);
            sb.AppendLine("SelectedTab.Name: " + selectedTabName);
            sb.AppendLine("SelectedTab.Text: " + selectedTabText);
            sb.AppendLine("labelStatus: " + labelStatus.Text);
            sb.AppendLine("labelError.Visible: " + labelError.Visible);
            sb.AppendLine("labelError.Text: " + labelError.Text);
            sb.AppendLine("labelSubStatus: " + labelSubStatus.Text);
            sb.AppendLine("isConnected (flag): " + isConnected);
            sb.AppendLine("client == null: " + (client == null));
            sb.AppendLine("client.Connected: " + connected);
            sb.AppendLine("client.UnitIdentifier: " + (client != null ? client.UnitIdentifier.ToString() : "null"));
            sb.AppendLine("client.Baudrate: " + (client != null ? client.Baudrate.ToString() : "null"));
            sb.AppendLine("client.Parity: " + (client != null ? client.Parity.ToString() : "null"));
            sb.AppendLine("client.StopBits: " + (client != null ? client.StopBits.ToString() : "null"));
            sb.AppendLine("client.ConnectionTimeout: " + (client != null ? client.ConnectionTimeout.ToString() : "null"));
            sb.AppendLine("comboPorts.Text: " + comboPorts.Text);
            sb.AppendLine("comboPorts.Items.Count: " + comboPorts.Items.Count);
            sb.AppendLine("comboBaud.Text: " + comboBaud.Text);
            sb.AppendLine("comboBaud.Items.Count: " + comboBaud.Items.Count);
            sb.AppendLine("timer1.Enabled: " + timer1.Enabled);
            sb.AppendLine("timer1.Interval(ms): " + timer1.Interval);
            sb.AppendLine("checkAutoScaleY.Checked: " + checkAutoScaleY.Checked);
            sb.AppendLine("ADC_FULL: " + ADC_FULL);
            sb.AppendLine("ADC_MID: " + ADC_MID);
            sb.AppendLine("sampleIndex: " + sampleIndex);
            sb.AppendLine("samples.Count: " + samples.Count);
            sb.AppendLine("rawRegs == null: " + (rawRegs == null));
            sb.AppendLine("rawRegs.Length: " + (rawRegs != null ? rawRegs.Length.ToString() : "null"));

            if (rawRegs != null && rawRegs.Length > 0)
            {
                var rawSb = new StringBuilder();

                for (int i = 0; i < rawRegs.Length; i++)
                {
                    if (i > 0)
                        rawSb.Append(", ");

                    rawSb.Append(RegFmt(rawRegs[i]));
                }

                sb.AppendLine("rawRegs: " + rawSb.ToString());
            }

            sb.AppendLine();

            sb.AppendLine("=== GRID: ОБЩЕЕ СОСТОЯНИЕ ===");
            sb.AppendLine("gridChannels.Rows.Count: " + gridChannels.Rows.Count);
            sb.AppendLine("gridChannels.Columns.Count: " + gridChannels.Columns.Count);
            sb.AppendLine("gridChannels.AllowUserToAddRows: " + gridChannels.AllowUserToAddRows);
            sb.AppendLine("gridChannels.AllowUserToDeleteRows: " + gridChannels.AllowUserToDeleteRows);
            sb.AppendLine("gridChannels.RowHeadersVisible: " + gridChannels.RowHeadersVisible);
            sb.AppendLine("gridChannels.IsCurrentCellInEditMode: " + gridChannels.IsCurrentCellInEditMode);
            sb.AppendLine("gridChannels.CurrentCell.RowIndex: " + selectedGridRow);
            sb.AppendLine("gridChannels.CurrentCell.ColumnIndex: " + selectedGridCol);
            sb.AppendLine("gridChannels.CurrentCell.ColumnName: " + selectedGridColName);
            sb.AppendLine("_fillingGrid: " + _fillingGrid);
            sb.AppendLine("_kbEditRow: " + _kbEditRow);
            sb.AppendLine("_kbEditCol: " + _kbEditCol);
            sb.AppendLine("_kbEditor == null: " + (_kbEditor == null));

            if (_kbEditor != null)
                sb.AppendLine("_kbEditor.Text: " + _kbEditor.Text);

            sb.AppendLine();
            sb.AppendLine("=== GRID: СТРОКИ ===");
            sb.AppendLine("Формат: Row | Enabled | Channel | Type(UI) | K | B | Unit | Current");

            for (int i = 0; i < gridChannels.Rows.Count; i++)
            {
                var row = gridChannels.Rows[i];
                sb.AppendLine(
                    "Row " + i +
                    " | " + CellStr(row, "colEnabled") +
                    " | " + CellStr(row, "colChannel") +
                    " | " + CellStr(row, "colType") +
                    " | " + CellStr(row, "colK") +
                    " | " + CellStr(row, "colB") +
                    " | " + CellStr(row, "colUnit") +
                    " | " + CellStr(row, "colCurrent")
                );
            }

            sb.AppendLine();

            sb.AppendLine("=== PROGRAM CHANNELS[] ===");
            sb.AppendLine("channels == null: " + (channels == null));
            sb.AppendLine("channels.Length: " + (channels != null ? channels.Length.ToString() : "null"));

            if (channels != null)
            {
                for (int i = 0; i < channels.Length; i++)
                {
                    var cfg = channels[i];

                    if (cfg == null)
                    {
                        sb.AppendLine("CH" + (i + 1) + ": null");
                        continue;
                    }

                    sb.AppendLine(
                        "CH" + (i + 1) +
                        ": Enabled=" + cfg.Enabled +
                        ", ChannelNumber=" + cfg.ChannelNumber +
                        ", TypeCode=" + cfg.TypeCode +
                        ", Range=" + cfg.Range +
                        ", Unit=" + cfg.Unit +
                        ", K=" + FmtDouble(cfg.K) +
                        ", B=" + FmtDouble(cfg.B)
                    );
                }
            }

            sb.AppendLine();

            sb.AppendLine("=== CHART ===");
            sb.AppendLine("chart1.Visible: " + chart1.Visible);
            sb.AppendLine("chart1.Size: " + chart1.Width + "x" + chart1.Height);
            sb.AppendLine("chart1.Dock: " + chart1.Dock);
            sb.AppendLine("chart1.Anchor: " + chart1.Anchor);
            sb.AppendLine("chart1.Series.Count: " + chart1.Series.Count);
            sb.AppendLine("chart1.ChartAreas.Count: " + chart1.ChartAreas.Count);

            if (chart1.ChartAreas.Count > 0)
            {
                try
                {
                    var area = chart1.ChartAreas[0];
                    sb.AppendLine("--- ChartArea[0] ---");
                    sb.AppendLine("AxisX.Title: " + area.AxisX.Title);
                    sb.AppendLine("AxisX.Minimum: " + FmtDouble(area.AxisX.Minimum));
                    sb.AppendLine("AxisX.Maximum: " + FmtDouble(area.AxisX.Maximum));
                    sb.AppendLine("AxisX.Interval: " + FmtDouble(area.AxisX.Interval));
                    sb.AppendLine("AxisX.LabelStyle.Format: " + area.AxisX.LabelStyle.Format);
                    sb.AppendLine("AxisX.IsStartedFromZero: " + area.AxisX.IsStartedFromZero);
                    sb.AppendLine("AxisY.Title: " + area.AxisY.Title);
                    sb.AppendLine("AxisY.Minimum: " + FmtDouble(area.AxisY.Minimum));
                    sb.AppendLine("AxisY.Maximum: " + FmtDouble(area.AxisY.Maximum));
                    sb.AppendLine("AxisY.Interval: " + FmtDouble(area.AxisY.Interval));
                    sb.AppendLine("AxisY.LabelStyle.Format: " + area.AxisY.LabelStyle.Format);
                    sb.AppendLine("AxisY.IsStartedFromZero: " + area.AxisY.IsStartedFromZero);
                }
                catch (Exception exAxis)
                {
                    sb.AppendLine("ChartArea read error: " + exAxis.Message);
                }
            }

            sb.AppendLine();

            foreach (Series s in chart1.Series)
            {
                sb.AppendLine("--- Series '" + s.Name + "' ---");
                sb.AppendLine("ChartType=" + s.ChartType);
                sb.AppendLine("Enabled=" + s.Enabled);
                sb.AppendLine("VisibleInLegend=" + s.IsVisibleInLegend);
                sb.AppendLine("BorderWidth=" + s.BorderWidth);
                sb.AppendLine("Points.Count=" + s.Points.Count);

                if (s.Points.Count > 0)
                {
                    double minX = double.PositiveInfinity;
                    double maxX = double.NegativeInfinity;
                    double minY = double.PositiveInfinity;
                    double maxY = double.NegativeInfinity;
                    double sumY = 0.0;

                    for (int i = 0; i < s.Points.Count; i++)
                    {
                        var p = s.Points[i];
                        double x = p.XValue;
                        double y = p.YValues[0];

                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;

                        sumY += y;
                    }

                    sb.AppendLine("X range: min=" + FmtDouble(minX) + ", max=" + FmtDouble(maxX));
                    sb.AppendLine("Y range: min=" + FmtDouble(minY) + ", max=" + FmtDouble(maxY));
                    sb.AppendLine("Y avg: " + FmtDouble(sumY / s.Points.Count));
                    sb.AppendLine("First point: " + SafeSeriesPoint(s, 0));
                    sb.AppendLine("Last point: " + SafeSeriesPoint(s, s.Points.Count - 1));

                    int from = Math.Max(0, s.Points.Count - 5);
                    sb.AppendLine("Last up to 5 points:");

                    for (int i = from; i < s.Points.Count; i++)
                        sb.AppendLine("  [" + i + "] " + SafeSeriesPoint(s, i));
                }

                sb.AppendLine();
            }

            sb.AppendLine("=== FILES / CONFIG ===");
            sb.AppendLine("LinkConfig exists: " + File.Exists(LinkConfigPath));
            sb.AppendLine("LastChannelsConfig exists: " + File.Exists(LastChannelsConfigPath));
            sb.AppendLine("LastCsvPathFile exists: " + File.Exists(LastCsvPathFile));

            try
            {
                if (File.Exists(LinkConfigPath))
                    sb.AppendLine("LinkConfig size(bytes): " + new FileInfo(LinkConfigPath).Length);
            }
            catch
            {
            }

            try
            {
                if (File.Exists(LastChannelsConfigPath))
                    sb.AppendLine("LastChannelsConfig size(bytes): " + new FileInfo(LastChannelsConfigPath).Length);
            }
            catch
            {
            }

            try
            {
                if (File.Exists(LastCsvPathFile))
                    sb.AppendLine("LastCsvPathFile size(bytes): " + new FileInfo(LastCsvPathFile).Length);
            }
            catch
            {
            }

            sb.AppendLine();

            sb.AppendLine("=== DEVICE REGISTERS ===");

            int[] commRegs = null;
            int[] typeRegs = null;
            int[] rawRegsLocal = null;
            string err;

            if (!connected)
            {
                sb.AppendLine("Нет подключения. Регистры не читаются.");
                sb.AppendLine();
            }
            else
            {
                if (TryReadRegs(40181 - 40001, 3, out commRegs, out err))
                {
                    sb.AppendLine("40181–40183 (COMM):");
                    sb.AppendLine("40181 DeviceAddr: " + RegFmt(commRegs[0]));
                    sb.AppendLine("40182 BaudCode : " + RegFmt(commRegs[1]) + " => " + DecodeBaudFromRegister(commRegs[1]));
                    sb.AppendLine("40183 Parity   : " + RegFmt(commRegs[2]));
                }
                else
                {
                    sb.AppendLine("40181–40183 read error: " + err);
                }

                sb.AppendLine();

                if (TryReadRegs(40201 - 40001, 8, out typeRegs, out err))
                {
                    sb.AppendLine("40201–40208 (TYPE):");

                    for (int i = 0; i < 8; i++)
                        sb.AppendLine("4020" + (i + 1) + " CH" + (i + 1) + ": " + RegFmt(typeRegs[i]) + " => " + DecodeInputTypeFromRegister(typeRegs[i]));
                }
                else
                {
                    sb.AppendLine("40201–40208 read error: " + err);
                }

                sb.AppendLine();

                if (TryReadRegs(40001 - 40001, 8, out rawRegsLocal, out err))
                {
                    sb.AppendLine("40001–40008 (RAW):");

                    for (int i = 0; i < 8; i++)
                        sb.AppendLine("4000" + (i + 1) + " CH" + (i + 1) + ": " + RegFmt(rawRegsLocal[i]));
                }
                else
                {
                    sb.AppendLine("40001–40008 read error: " + err);
                }

                sb.AppendLine();
            }

            sb.AppendLine("=== CHANNEL ANALYSIS ===");

            for (int i = 0; i < channels.Length; i++)
            {
                int ch = i + 1;
                var cfg = channels[i];

                sb.AppendLine("--- КАНАЛ " + ch + " ---");

                if (cfg == null)
                {
                    sb.AppendLine("Program config: null");
                    sb.AppendLine();
                    continue;
                }

                int? hwType = null;
                int? hwRaw = null;

                if (typeRegs != null && typeRegs.Length == 8)
                    hwType = typeRegs[i];

                if (rawRegsLocal != null && rawRegsLocal.Length == 8)
                    hwRaw = rawRegsLocal[i];

                sb.AppendLine("Program.Enabled: " + cfg.Enabled);
                sb.AppendLine("Program.ChannelNumber: " + cfg.ChannelNumber);
                sb.AppendLine("Program.TypeCode: " + cfg.TypeCode);
                sb.AppendLine("Program.Range: " + cfg.Range);
                sb.AppendLine("Program.Unit: " + cfg.Unit);
                sb.AppendLine("Program.K: " + FmtDouble(cfg.K));
                sb.AppendLine("Program.B: " + FmtDouble(cfg.B));
                sb.AppendLine("Program.Formula: y = " + FmtDouble(cfg.K) + " * x " + (cfg.B >= 0 ? "+" : "-") + " " + FmtDouble(Math.Abs(cfg.B)));

                var row = i < gridChannels.Rows.Count ? gridChannels.Rows[i] : null;

                if (row != null)
                {
                    sb.AppendLine("Grid.Enabled: " + CellStr(row, "colEnabled"));
                    sb.AppendLine("Grid.Channel: " + CellStr(row, "colChannel"));
                    sb.AppendLine("Grid.Type(UI): " + CellStr(row, "colType"));
                    sb.AppendLine("Grid.K: " + CellStr(row, "colK"));
                    sb.AppendLine("Grid.B: " + CellStr(row, "colB"));
                    sb.AppendLine("Grid.Unit: " + CellStr(row, "colUnit"));
                    sb.AppendLine("Grid.Current: " + CellStr(row, "colCurrent"));
                }
                else
                {
                    sb.AppendLine("Grid row: отсутствует");
                }

                if (hwType.HasValue)
                    sb.AppendLine("HW.TypeCode: " + RegFmt(hwType.Value) + " => " + DecodeInputTypeFromRegister(hwType.Value));
                else
                    sb.AppendLine("HW.TypeCode: нет данных");

                if (hwRaw.HasValue)
                    sb.AppendLine("HW.Raw: " + RegFmt(hwRaw.Value));
                else
                    sb.AppendLine("HW.Raw: нет данных");

                if (hwType.HasValue && cfg.TypeCode != hwType.Value)
                    sb.AppendLine("!!! РАСХОЖДЕНИЕ TypeCode: Program=" + cfg.TypeCode + ", HW=" + hwType.Value);

                if (hwRaw.HasValue)
                {
                    try
                    {
                        var tmp = new ChannelConfig
                        {
                            Enabled = cfg.Enabled,
                            ChannelNumber = cfg.ChannelNumber,
                            TypeCode = hwType ?? cfg.TypeCode,
                            Range = cfg.Range,
                            Unit = cfg.Unit,
                            K = cfg.K,
                            B = cfg.B
                        };

                        double physical = ConvertRawToPhysical(hwRaw.Value, tmp);
                        double calibrated = ApplyCalibration(physical, tmp);

                        sb.AppendLine("Computed.Physical: " + FmtDouble(physical));
                        sb.AppendLine("Computed.Calibrated: " + FmtDouble(calibrated) + " " + tmp.Unit);
                    }
                    catch (Exception exCalc)
                    {
                        sb.AppendLine("Computed error: " + exCalc.Message);
                    }
                }

                int logCount = 0;
                double minV = double.PositiveInfinity;
                double maxV = double.NegativeInfinity;
                double sumV = 0.0;
                SampleLog firstLog = null;
                SampleLog lastLog = null;
                var lastFive = new List<SampleLog>();

                for (int k = 0; k < samples.Count; k++)
                {
                    var item = samples[k];
                    if (item.Channel != ch)
                        continue;

                    logCount++;

                    if (firstLog == null)
                        firstLog = item;

                    lastLog = item;

                    if (item.Value < minV) minV = item.Value;
                    if (item.Value > maxV) maxV = item.Value;
                    sumV += item.Value;

                    if (lastFive.Count == 5)
                        lastFive.RemoveAt(0);

                    lastFive.Add(item);
                }

                sb.AppendLine("Log.Count: " + logCount);

                if (logCount > 0 && firstLog != null && lastLog != null)
                {
                    sb.AppendLine("Log.Time range: " + ToTimeSec(firstLog.Index, firstLog.IntervalMs) + " .. " + ToTimeSec(lastLog.Index, lastLog.IntervalMs) + " сек");
                    sb.AppendLine("Log.Value range: min=" + FmtDouble(minV) + ", max=" + FmtDouble(maxV) + ", avg=" + FmtDouble(sumV / logCount));
                    sb.AppendLine("Log.First: TimeSec=" + ToTimeSec(firstLog.Index, firstLog.IntervalMs) + ", IntervalMs=" + firstLog.IntervalMs + ", Value=" + FmtDouble(firstLog.Value) + ", Unit=" + firstLog.Unit);
                    sb.AppendLine("Log.Last : TimeSec=" + ToTimeSec(lastLog.Index, lastLog.IntervalMs) + ", IntervalMs=" + lastLog.IntervalMs + ", Value=" + FmtDouble(lastLog.Value) + ", Unit=" + lastLog.Unit);
                    sb.AppendLine("Log.Last up to 5 samples:");

                    for (int k = 0; k < lastFive.Count; k++)
                    {
                        var item = lastFive[k];
                        sb.AppendLine("  [" + k + "] TimeSec=" + ToTimeSec(item.Index, item.IntervalMs) + ", IntervalMs=" + item.IntervalMs + ", Value=" + FmtDouble(item.Value) + ", Unit=" + item.Unit);
                    }
                }

                sb.AppendLine();
            }

            sb.AppendLine("=== GLOBAL SAMPLE LOG ===");
            sb.AppendLine("samples.Count total: " + samples.Count);

            if (samples.Count > 0)
            {
                long minIndex = samples[0].Index;
                long maxIndex = samples[0].Index;

                for (int i = 1; i < samples.Count; i++)
                {
                    if (samples[i].Index < minIndex) minIndex = samples[i].Index;
                    if (samples[i].Index > maxIndex) maxIndex = samples[i].Index;
                }

                sb.AppendLine("Time range total: " + ToTimeSec(minIndex, timer1.Interval) + " .. " + ToTimeSec(maxIndex, timer1.Interval) + " сек");

                for (int ch = 1; ch <= 8; ch++)
                {
                    int count = 0;
                    SampleLog first = null;
                    SampleLog last = null;

                    for (int i = 0; i < samples.Count; i++)
                    {
                        var item = samples[i];
                        if (item.Channel != ch)
                            continue;

                        count++;

                        if (first == null)
                            first = item;

                        last = item;
                    }

                    sb.AppendLine("CH" + ch + ": Count=" + count);

                    if (count > 0 && first != null && last != null)
                    {
                        sb.AppendLine("  First: TimeSec=" + ToTimeSec(first.Index, first.IntervalMs) + ", IntervalMs=" + first.IntervalMs + ", Value=" + FmtDouble(first.Value) + ", Unit=" + first.Unit);
                        sb.AppendLine("  Last : TimeSec=" + ToTimeSec(last.Index, last.IntervalMs) + ", IntervalMs=" + last.IntervalMs + ", Value=" + FmtDouble(last.Value) + ", Unit=" + last.Unit);
                    }
                }

                sb.AppendLine();
                sb.AppendLine("Last up to 20 records in total:");

                int start = Math.Max(0, samples.Count - 20);

                for (int i = start; i < samples.Count; i++)
                {
                    var item = samples[i];
                    sb.AppendLine("[" + i + "] CH=" + item.Channel + ", TimeSec=" + ToTimeSec(item.Index, item.IntervalMs) + ", IntervalMs=" + item.IntervalMs + ", Value=" + FmtDouble(item.Value) + ", Unit=" + item.Unit);
                }
            }

            sb.AppendLine();

            string fullText = sb.ToString();
            ShowInfoDialog(fullText);
        }
     
        // === Кнопка Обновить каналы ===
        private void btnUpdateChannels_Click(object sender, EventArgs e)
        {
            try
            {
                ReadChannelsFromGrid();
                SetSubStatus("Конфигурация каналов обновлена");
                labelError.Visible = false;
                labelError.Text = "";
                UpdateGraphHeaderUi();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка при обновлении каналов", ex);
            }
        }


        //=== для вкладки Каналы===
        private void InitChannelSeries()
        {
            var toRemove = new List<Series>();
            foreach (Series s in chart1.Series)
                if (s.Name != "VAH") toRemove.Add(s);

            foreach (var s in toRemove)
                chart1.Series.Remove(s);

            for (int i = 0; i < channels.Length; i++)
            {
                string name = $"CH{i + 1}";
                var s = chart1.Series.Add(name);
                s.ChartType = SeriesChartType.Spline;
                s.BorderWidth = 2;
                s.IsVisibleInLegend = true;
                s.XValueType = ChartValueType.Int32;

                //Удалить эти 2 строчки на случай работы 8 каналов
                //s.Enabled = i < VISIBLE_CHANNELS;
                //s.IsVisibleInLegend = i < VISIBLE_CHANNELS;

            }
        }


        private double ReadChannelByIndex(int index)
        {
            if (client == null || !client.Connected)
                throw new InvalidOperationException("Нет подключения к устройству");

            if (index < 0 || index >= channels.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var cfg = channels[index];
            if (!cfg.Enabled)
                throw new InvalidOperationException("Канал выключен");

            int startAddress = index;   // 40001 + index
            rawRegs = client.ReadHoldingRegisters(startAddress, 1);
            int raw = rawRegs[0];

            double physical = ConvertRawToPhysical(raw, cfg);
            double q = ApplyCalibration(physical, cfg);

            return q;
        }
        private void DoSingleMeasurementStep()
        {
            if (client == null || !client.Connected)
                throw new InvalidOperationException("Нет подключения к устройству");

            long x = sampleIndex;
            bool anyMeasured = false;

            for (int i = 0; i < channels.Length; i++)
            {
                var cfg = channels[i];
                if (!cfg.Enabled)
                    continue;

                double value = ReadChannelByIndex(i);
                anyMeasured = true;

                string unit = cfg.Unit ?? "";
                int editRow = gridChannels.CurrentCell?.RowIndex ?? -1;
                bool editing = gridChannels.IsCurrentCellInEditMode;

                if (!(editing && editRow == i))
                    gridChannels.Rows[i].Cells["colCurrent"].Value = value.ToString("F3");

                var item = new SampleLog
                {
                    Index = x,
                    IntervalMs = timer1.Interval,
                    Channel = cfg.ChannelNumber,
                    Value = value,
                    Unit = unit
                };

                samples.Add(item);
                AppendAutoSavePoint(item);

                string seriesName = $"CH{cfg.ChannelNumber}";
                var s = chart1.Series[seriesName];

                long tSec = ToTimeSec(x, timer1.Interval);
                s.Points.AddXY(tSec, value);

                if (!checkAutoScaleY.Checked)
                    UpdateAutoScale(value);
            }

            if (!anyMeasured)
                return;

            sampleIndex++;

            ApplyXAxisScale();
            chart1.Invalidate();

            if (checkAutoScaleY.Checked)
            {
                RecalculateMinMaxFromSeries();
                ApplyFullAutoScaleFromGlobal();
            }

            UpdateGraphHeaderUi();
        }


        //=== выгрузка 
        private string LastCsvPathFile =>
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last_csv_path.txt");

        private void RememberLastCsv(string path)
        {
            File.WriteAllText(LastCsvPathFile, path ?? "", Encoding.UTF8);
        }

        private string GetLastCsv()
        {
            if (!File.Exists(LastCsvPathFile)) return "";
            return (File.ReadAllText(LastCsvPathFile, Encoding.UTF8) ?? "").Trim();
        }



        //=== Подсостояние ===

        private void SetSubStatus(string text)
        {
            labelSubStatus.Text = string.IsNullOrWhiteSpace(text) ? "---" : text;
        }

        // ===Загрузить конфиг==
        private sealed class LinkConfig
        {
            public string SelectedPort { get; set; } = "";
            public string SelectedBaud { get; set; } = "9600";
            public int PollIntervalMs { get; set; } = 400;
        }

        private string LinkConfigPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last_link_config.json");


        private LinkConfig CaptureLinkConfig()
        {
            return new LinkConfig
            {
                SelectedPort = comboPorts.Text ?? "",
                SelectedBaud = comboBaud.Text ?? "9600",
                PollIntervalMs = timer1.Interval
            };
        }

        private void ApplyLinkConfig(LinkConfig cfg)
        {
            if (cfg == null) return;

            if (!string.IsNullOrWhiteSpace(cfg.SelectedBaud))
            {
                int bi = comboBaud.Items.IndexOf(cfg.SelectedBaud);
                if (bi >= 0) comboBaud.SelectedIndex = bi;
                else comboBaud.Text = cfg.SelectedBaud;
            }

            if (!string.IsNullOrWhiteSpace(cfg.SelectedPort))
            {
                int pi = comboPorts.Items.IndexOf(cfg.SelectedPort);
                if (pi >= 0) comboPorts.SelectedIndex = pi;
                else comboPorts.Text = cfg.SelectedPort;
            }

            timer1.Interval = cfg.PollIntervalMs > 0 ? cfg.PollIntervalMs : 1000;

            UpdateGraphHeaderUi();
        }
        private void SaveLastLinkConfig()
        {
            var cfg = CaptureLinkConfig();
            string json = JsonConvert.SerializeObject(cfg, Formatting.Indented);
            File.WriteAllText(LinkConfigPath, json, Encoding.UTF8);
        }

        private void LoadLastLinkConfig()
        {
            if (!File.Exists(LinkConfigPath))
                return;

            string json = File.ReadAllText(LinkConfigPath, Encoding.UTF8);
            var cfg = JsonConvert.DeserializeObject<LinkConfig>(json);
            ApplyLinkConfig(cfg);
        }

        // === сохранить настройки для Каналов

        private void EnsureChannelsAndGrid()
        {
            // channels должен быть ровно на 8 и не содержать null
            bool bad =
                channels == null ||
                channels.Length != 8 ||
                channels.Any(c => c == null);

            if (bad)
            {
                channels = new ChannelConfig[8];
                InitChannelConfigs();   // заполняет channels[0..7]
            }

            // grid должен иметь 8 строк (не считая новой "звёздочки")
            if (gridChannels.Rows.Count < 8)
            {
                FillGridFromChannels(); // заново рисует 8 строк из channels
            }
        }


        private string LastChannelsConfigPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last_channels_config.json");


        private void btnChannelsApply_Click(object sender, EventArgs e)
        {
            try
            {
                EnsureChannelsAndGrid();
                if (gridChannels.IsCurrentCellInEditMode) gridChannels.EndEdit();
                ReadChannelsFromGrid();


                string json = JsonConvert.SerializeObject(channels, Formatting.Indented);
                File.WriteAllText(LastChannelsConfigPath, json, Encoding.UTF8);

                UpdateSeriesVisibility();
                SetSubStatus("Каналы: применено");

                labelError.Visible = false;
                labelError.Text = "";
                UpdateGraphHeaderUi();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка применения каналов", ex);
            }
        }

        private void btnChannelsLoad_Click(object sender, EventArgs e)
        {
            try
            {
                EnsureChannelsAndGrid();
                if (!File.Exists(LastChannelsConfigPath))
                    throw new FileNotFoundException("Файл конфигурации каналов не найден", LastChannelsConfigPath);

                string json = File.ReadAllText(LastChannelsConfigPath, Encoding.UTF8);
                var loaded = JsonConvert.DeserializeObject<ChannelConfig[]>(json);

                if (loaded == null || loaded.Length != 8)
                    throw new InvalidDataException("Неверный файл конфигурации каналов (ожидается 8 каналов).");

                channels = loaded;

                FillGridFromChannels();
                UpdateSeriesVisibility();
                SetSubStatus("Каналы: загружено");

                labelError.Visible = false;
                labelError.Text = "";
                UpdateGraphHeaderUi();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки каналов", ex);
            }
        }




        // ============================================================


        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }

        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
        }

        private void label3_Click(object sender, EventArgs e)
        {
        }

        private void label3_Click_1(object sender, EventArgs e)
        {
        }

        private void labelStatus_Click(object sender, EventArgs e)
        {
        }

        private void chart1_Click(object sender, EventArgs e)
        {
        }

        private void numChannel_ValueChanged(object sender, EventArgs e)
        {
           
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView1_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void comboChannels_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (client != null && client.Connected)
                    client.Disconnect();
            }
            catch
            {
            }

            isConnected = false;
            labelError.Text = "";
            labelError.Visible = false;
            labelStatus.Text = "Отключено";
        }

        private void btnClearVah_Click(object sender, EventArgs e)
        {
            chart1.Series["VAH"].Points.Clear();
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void labelValue_Click(object sender, EventArgs e)
        {

        }


        private void labelError_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_2(object sender, EventArgs e)
        {

        }

        private void button1_Click_3(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click_2(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void btnSaveVah_Click_1(object sender, EventArgs e)
        {
            btnSave_Click(sender, e);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnSaveToDevice_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_4(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnLoadConfig_Click_1(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void chart1_Click_1(object sender, EventArgs e)
        {

        }

        private void panelStartMode_Paint(object sender, PaintEventArgs e)
        {

        }

        private void chart1_Click_2(object sender, EventArgs e)
        {

        }

        private void button1_Click_5(object sender, EventArgs e)
        {
            btnStart_Click(sender, e);

            if (timer1.Enabled)
                ShowGraphMode();
            else
                ShowStartMode();

        }

        private void btnRead_Click_1(object sender, EventArgs e)
        {

        }

        private void btnClear_Click_1(object sender, EventArgs e)
        {

        }
    }
}
