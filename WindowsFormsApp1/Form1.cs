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
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private bool isConnected = false;
        private ChannelConfig[] channels = new ChannelConfig[8];
        private bool _fillingGrid;
        private ModbusClient client;
        private TextBox _kbEditor;
        private int _kbEditRow = -1;
        private string _kbEditCol = "";

        int[] rawRegs;

        private DateTime? _measureStartTime;
        private Panel _graphTopPanel;
        private Label _graphInfoMain;
        private Label _graphInfoSelected;
        private bool _uiReady; 
        private DateTime _nextReadAllowedAt = DateTime.MinValue;
        private bool[] _breakSeriesOnNextPoint = new bool[8];
        private StreamWriter _autoSaveWriter;
        private string _currentAutoSavePath = "";

        private bool _directModeSnapshotSaved;
        private ChannelConfig[] _directModeBackup;
        public Form1()
        {
            InitializeComponent();
            // растягивание UI вместе с Form
            tabControl1.Dock = DockStyle.Fill;   // если у тебя tabControl называется иначе — подставь имя

            // на вкладке "Графики" (или где chart1 стоит) пусть график тянется по вкладке
            chart1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            checkDirectMode.CheckedChanged += checkDirectMode_CheckedChanged;
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
            
            btnDisconnect.Click -= button1_Click;
            btnDisconnect.Click -= button1_Click_1;
            btnDisconnect.Click += btnDisconnect_Click;

            _graphTopPanel = null;
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
            LoadLastLinkConfig();
            _uiReady = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CloseAutoSaveSession();
            base.OnFormClosing(e);
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
            btnRead.Enabled = true;
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

        private static long ToTimeSec(long index, int intervalMs)
        {
            return (index * (long)intervalMs) / 1000;
        }


        private string AutoSaveFolderPath =>
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "autosave");

        private string FixedAutoSavePath =>
    Path.Combine(AutoSaveFolderPath, "DAM_AUTOSAVE.csv");

        private static string FormatCsvTimestamp(DateTime value)
        {
            return value.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.GetCultureInfo("ru-RU"));
        }

        private static bool IsChannelHeaderText(string text, int channelNumber)
        {
            text = (text ?? "").Trim().TrimStart('\uFEFF');

            return text.Equals($"Channel {channelNumber}", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals($"Channel{channelNumber}", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals($"CH{channelNumber}", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMeasurementCsvHeader(IReadOnlyList<string> headers)
        {
            if (headers == null || headers.Count < 9)
                return false;

            if (!headers[0].Trim().TrimStart('\uFEFF').Equals("Time", StringComparison.OrdinalIgnoreCase))
                return false;

            for (int i = 1; i <= 8; i++)
            {
                if (!IsChannelHeaderText(headers[i], i))
                    return false;
            }

            return true;
        }
        private void EnsureAutoSaveSession()
        {
            if (_autoSaveWriter != null)
                return;

            Directory.CreateDirectory(AutoSaveFolderPath);

            _currentAutoSavePath = FixedAutoSavePath;

            var fs = new FileStream(
                _currentAutoSavePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.ReadWrite,
                4096,
                FileOptions.WriteThrough);

            _autoSaveWriter = new StreamWriter(fs, new UTF8Encoding(true))
            {
                AutoFlush = true
            };

            _autoSaveWriter.WriteLine("Time;Channel 1;Channel 2;Channel 3;Channel 4;Channel 5;Channel 6;Channel 7;Channel 8");
            _autoSaveWriter.Flush();

            RememberLastCsv(_currentAutoSavePath);
            SetSubStatus("Автозапись включена");
        }

        private void AppendAutoSaveSnapshot(DateTime timestamp, double?[] channelValues)
        {
            EnsureAutoSaveSession();

            if (channelValues == null || channelValues.Length < 8)
                throw new ArgumentException("Ожидается массив из 8 значений каналов", nameof(channelValues));

            var ru = CultureInfo.GetCultureInfo("ru-RU");
            var parts = new string[9];
            parts[0] = FormatCsvTimestamp(timestamp);

            for (int i = 0; i < 8; i++)
            {
                parts[i + 1] = channelValues[i].HasValue
                    ? channelValues[i].Value.ToString("0.######", ru)
                    : "";
            }

            _autoSaveWriter.WriteLine(string.Join(";", parts));
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
                return;
            }

            // Изменили галочку "Включение"
            if (column.Name == "colEnabled")
            {
                // забираем новые значения Enabled из грида в массив channels
                ReadChannelsFromGrid();

                // включаем/выключаем серии на графике
                UpdateSeriesVisibility();
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

        private void DisconnectFromDevice()
        {
            timer1.Stop();
            CloseAutoSaveSession();

            try
            {
                if (client != null && client.Connected)
                    client.Disconnect();
            }
            catch
            {
            }
            finally
            {
                client = null;
            }

            isConnected = false;
            labelError.Text = "";
            labelError.Visible = false;
            labelStatus.Text = "Отключено";
            SetSubStatus("Соединение разорвано");
            ShowStartMode();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            DisconnectFromDevice();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (gridChannels.IsCurrentCellInEditMode)
                    gridChannels.EndEdit();

                ReadChannelsFromGrid();
                UpdateSeriesVisibility();
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
                btnRead.Enabled = false;

                gridChannels.Refresh();
                chart1.Invalidate();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка запуска", ex);
            }
        }


        private void btnStop_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            btnRead.Enabled = true;
            ShowGraphMode();
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (client == null || !client.Connected)
                    throw new InvalidOperationException("Нет подключения к устройству");

                if (!TryReadAllRawChannels(out rawRegs, out var readErr))
                    throw new InvalidOperationException("Не удалось прочитать каналы: " + readErr);

                long x = sampleIndex;
                bool anyMeasured = false;
                DateTime rowTime = DateTime.Now;
                double?[] rowValues = new double?[8];

                for (int i = 0; i < channels.Length; i++)
                {
                    var cfg = channels[i];
                    if (cfg == null || !cfg.Enabled)
                        continue;

                    if (_breakSeriesOnNextPoint[i])
                    {
                        InsertSeriesBreakForChannel(i, x);
                        _breakSeriesOnNextPoint[i] = false;
                    }

                    double value = ReadChannelByIndex(i);
                    rowValues[i] = value;
                    anyMeasured = true;

                    if (i < gridChannels.Rows.Count)
                        gridChannels.Rows[i].Cells["colCurrent"].Value =
                            value.ToString("F3", CultureInfo.InvariantCulture);

                    var item = new SampleLog
                    {
                        Index = x,
                        IntervalMs = timer1.Interval,
                        Channel = cfg.ChannelNumber,
                        Value = value,
                        Unit = cfg.Unit,
                        Timestamp = rowTime
                    };


                    string seriesName = $"CH{cfg.ChannelNumber}";
                    var s = chart1.Series[seriesName];

                    long tSec = ToTimeSec(x, timer1.Interval);
                    s.Points.AddXY(tSec, value);
                }

                if (!anyMeasured)
                    return;

                AppendAutoSaveSnapshot(rowTime, rowValues);

                sampleIndex++;
                ApplyXAxisScale();

                if (checkAutoScaleY.Checked)
                    ApplyFullAutoScaleFromGlobal();

                labelError.Visible = false;
                labelError.Text = "";

                gridChannels.Refresh();
                chart1.Invalidate();
            }
            catch (Exception ex)
            {
                timer1.Stop();
                btnRead.Enabled = true;
                ShowError("Ошибка чтения каналов", ex);
            }
        }

        //=== Кнопка Прочитать ===
        private async void btnRead_Click(object sender, EventArgs e)
        {
            if (DateTime.Now < _nextReadAllowedAt)
                return;

            try
            {
                _nextReadAllowedAt = DateTime.Now.AddMilliseconds(950);
                btnRead.Enabled = false;

                if (gridChannels.IsCurrentCellInEditMode)
                    gridChannels.EndEdit();

                ReadChannelsFromGrid();
                UpdateSeriesVisibility();

                if (client == null || !client.Connected)
                    throw new InvalidOperationException("Нет подключения к устройству");

                if (!_measureStartTime.HasValue || sampleIndex == 0)
                    _measureStartTime = DateTime.Now;

                EnsureAutoSaveSession();
                DoSingleMeasurementStep();

                labelError.Visible = false;
                labelError.Text = "";
            }
            catch (Exception ex)
            {
                ShowError("Ошибка при ручном чтении", ex);
            }
            finally
            {
                await Task.Delay(900);

                if (!IsDisposed && btnRead != null)
                    btnRead.Enabled = true;
            }
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            string portName = (comboPorts.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(portName))
            {
                ShowError("Ошибка подключения", new ArgumentException("Не выбран COM-порт"));
                return;
            }

            if (!SerialPort.GetPortNames().Any(p => string.Equals(p, portName, StringComparison.OrdinalIgnoreCase)))
            {
                ShowError("Ошибка подключения", new InvalidOperationException("Выбран неверный COM-порт"));
                return;
            }

            if (!int.TryParse(comboBaud.Text, out int baud))
            {
                ShowError("Ошибка подключения", new FormatException("Некорректная скорость COM-порта"));
                return;
            }

            SetConnectionUiBusy(true);
            ModbusClient newClient = null;
            int[] typeRegs = null;
            Exception connectError = null;

            try
            {
                timer1.Stop();

                if (client != null)
                {
                    try
                    {
                        if (client.Connected)
                            client.Disconnect();
                    }
                    catch
                    {
                    }

                    client = null;
                }

                await Task.Run(() =>
                {
                    try
                    {
                        newClient = new ModbusClient(portName)
                        {
                            UnitIdentifier = 1,
                            Baudrate = baud,
                            Parity = Parity.None,
                            StopBits = StopBits.One,
                            ConnectionTimeout = 2000
                        };

                        newClient.Connect();
                        typeRegs = newClient.ReadHoldingRegisters(40201 - 40001, 8);

                        if (typeRegs == null || typeRegs.Length < 8)
                            connectError = new InvalidOperationException("Устройство не отвечает или выбран неверный COM-порт");
                    }
                    catch (TimeoutException)
                    {
                        connectError = new InvalidOperationException("Устройство не отвечает или выбран неверный COM-порт");
                    }
                    catch (IOException)
                    {
                        connectError = new InvalidOperationException("Проблема с адаптером, кабелем или выбран неверный COM-порт");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        connectError = new InvalidOperationException("COM-порт занят другой программой");
                    }
                    catch (Exception ex)
                    {
                        connectError = ex;
                    }
                });

                if (connectError != null)
                    throw connectError;

                client = newClient;
                newClient = null;

                ApplyTypeCodesFromRegisters(typeRegs);
                FillGridFromChannels();
                ReadChannelsFromGrid();

                isConnected = true;
                labelStatus.Text = "Подключено";
                labelError.Visible = false;
                labelError.Text = "";
                SetSubStatus("Соединение установлено");

                ShowStartMode();
            }
            catch (Exception ex)
            {
                isConnected = false;
                labelStatus.Text = "Отключено";

                try
                {
                    if (newClient != null && newClient.Connected)
                        newClient.Disconnect();
                }
                catch
                {
                }

                try
                {
                    if (client != null && client.Connected)
                        client.Disconnect();
                }
                catch
                {
                }

                client = null;
                ShowError("Ошибка подключения", ex);
            }
            finally
            {
                SetConnectionUiBusy(false);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            DisconnectFromDevice();
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

        private void SetConnectionUiBusy(bool busy)
        {
            UseWaitCursor = busy;

            btnConnect.Enabled = !busy;
            btnDisconnect.Enabled = !busy;
            btnRefreshPorts.Enabled = !busy;
            btnLoadConfig.Enabled = !busy;
            btnWriteConfig.Enabled = !busy;
            comboPorts.Enabled = !busy;
            comboBaud.Enabled = !busy;
        }

        private void ApplyTypeCodesFromRegisters(int[] typeRegs)
        {
            if (typeRegs == null || typeRegs.Length < 8)
                throw new InvalidOperationException("Выбран неверный COM-порт или устройство не отвечает");

            for (int i = 0; i < channels.Length && i < typeRegs.Length; i++)
            {
                channels[i].TypeCode = typeRegs[i];
                channels[i].Range = DecodeInputTypeFromRegister(typeRegs[i]);

                if (string.IsNullOrWhiteSpace(channels[i].Unit) ||
                    channels[i].Unit == "В" ||
                    channels[i].Unit == "мВ" ||
                    channels[i].Unit == "мА")
                {
                    channels[i].Unit = GetDefaultUnitByTypeCode(typeRegs[i]);
                }
            }
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
            else if (ex is InvalidOperationException)
            {
                var msg = ex.Message ?? "";

                if (msg.IndexOf("COM", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("порт", StringComparison.OrdinalIgnoreCase) >= 0)
                    message = msg;
                else if (msg.IndexOf("устройство не отвечает", StringComparison.OrdinalIgnoreCase) >= 0)
                    message = msg;
                else
                    message = "Некорректная операция";
            }
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
                Unit = "мВ",
                K = -0.30599,
                B = 123.4606
            };

            channels[1] = new ChannelConfig
            {
                Enabled = true,
                ChannelNumber = 2,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "мВ",
                K = -0.32777,
                B = 77.4365
            };

            channels[2] = new ChannelConfig
            {
                Enabled = true,
                ChannelNumber = 3,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "мВ",
                K = -0.3258,
                B = 36.9236
            };

            channels[3] = new ChannelConfig
            {
                Enabled = true,
                ChannelNumber = 4,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "мВ",
                K = -0.30551,
                B = 68.2308
            };

            channels[4] = new ChannelConfig
            {
                Enabled = true,
                ChannelNumber = 5,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "мВ",
                K = -0.33117,
                B = -54.2287
            };

            channels[5] = new ChannelConfig
            {
                Enabled = false,
                ChannelNumber = 6,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "мВ",
                K = 1.0,
                B = 0.0
            };

            channels[6] = new ChannelConfig
            {
                Enabled = false,
                ChannelNumber = 7,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "мВ",
                K = 1.0,
                B = 0.0
            };

            channels[7] = new ChannelConfig
            {
                Enabled = false,
                ChannelNumber = 8,
                TypeCode = 28,
                Range = "0–10 В",
                Unit = "мВ",
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

                    string typeText = DecodeInputTypeFromRegister(cfg.TypeCode);
                    row.Cells["colType"].Value = typeText == "неизвестный тип" ? $"код {cfg.TypeCode}" : typeText;

                    if (string.IsNullOrWhiteSpace(cfg.Unit) ||
                        cfg.Unit == "В" ||
                        cfg.Unit == "мВ" ||
                        cfg.Unit == "мА")
                    {
                        cfg.Unit = GetDefaultUnitByTypeCode(cfg.TypeCode);
                    }
                    row.Cells["colK"].Value = cfg.K;
                    row.Cells["colB"].Value = cfg.B;
                    row.Cells["colCurrent"].Value = "";
                    row.Cells["colUnit"].Value = GetGridUnitText(cfg);
                }
            }
            finally
            {
                _fillingGrid = false;
            }

        }
        private void ReadChannelsFromGrid()
        {
            if (gridChannels.Rows.Count < 8)
                FillGridFromChannels();

            if (gridChannels.Rows.Count < 8)
                return;

            for (int i = 0; i < 8; i++)
            {
                var row = gridChannels.Rows[i];
                var cfg = channels[i];

                cfg.Enabled = Convert.ToBoolean(row.Cells["colEnabled"].Value);
                cfg.ChannelNumber = i + 1;

                string kText = Convert.ToString(row.Cells["colK"].Value)?.Trim() ?? "";
                string bText = Convert.ToString(row.Cells["colB"].Value)?.Trim() ?? "";

                if (!TryParseGridDouble(kText, out double kValue))
                    kValue = cfg.K;

                if (!TryParseGridDouble(bText, out double bValue))
                    bValue = cfg.B;

                cfg.K = kValue;
                cfg.B = bValue;

                cfg.Unit = GetDefaultUnitByTypeCode(cfg.TypeCode);
                cfg.Range = DecodeInputTypeFromRegister(cfg.TypeCode);
            }
        }
        private void UpdateSeriesVisibility()
        {
            {
                for (int i = 0; i < channels.Length; i++)
                {
                    string seriesName = $"CH{i + 1}";
                    int idx = chart1.Series.IndexOf(seriesName);
                    if (idx < 0)
                        continue;

                    bool enabled = channels[i] != null && channels[i].Enabled;
                    var s = chart1.Series[idx];

                    bool wasEnabled = s.Enabled;

                    s.Enabled = enabled;
                    s.IsVisibleInLegend = enabled;

                    if (!wasEnabled && enabled && s.Points.Count > 0)
                        _breakSeriesOnNextPoint[i] = true;
                }

                if (checkAutoScaleY.Checked)
                    ApplyFullAutoScaleFromGlobal();

                chart1.Invalidate();
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

                    if (dlg.ShowDialog() != DialogResult.OK)
                        return;

                    ValidateDamCsvOrThrow(dlg.FileName);
                    LoadMeasurementsCsvToChart(dlg.FileName);
                    RememberLastCsv(dlg.FileName);
                }

                timer1.Stop();

                tabControl1.SelectedTab = tabPage1;
                btnBigStart.Visible = false;
                panelStartMode.Visible = true;
                panelStartMode.BringToFront();
                PrepareGraphModeUi();

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
            {
                if (s.Name.StartsWith("CH"))
                    s.Points.Clear();
            }

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

            UpdateSeriesVisibility();
            UpdateGridCurrentFromSamples();

            if (checkAutoScaleY.Checked)
                ApplyFullAutoScaleFromGlobal();
            else
                ResetAutoScale();

            _measureStartTime = samples.Count == 0 ? (DateTime?)null : samples.Min(x => x.Timestamp);

            ApplyXAxisScale();
            chart1.Invalidate();
        }
        private List<SampleLog> ReadSamplesFromCsv(string path)
        {
            var list = new List<SampleLog>();
            var ru = CultureInfo.GetCultureInfo("ru-RU");
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);

            if (lines.Length == 0)
                return list;

            string headerLine = lines.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";
            var headers = headerLine
                .Split(';')
                .Select(x => (x ?? "").Trim().TrimStart('\uFEFF'))
                .ToList();

            if (!IsMeasurementCsvHeader(headers))
                throw new InvalidDataException("Неверный CSV: ожидается заголовок Time;Channel 1;Channel 2;Channel 3;Channel 4;Channel 5;Channel 6;Channel 7;Channel 8");

            string[] dateFormats =
            {
        "dd.MM.yyyy HH:mm:ss",
        "dd.MM.yyyy HH:mm:ss.fff",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff"
    };

            var rows = new List<(DateTime Timestamp, double?[] Values)>();

            foreach (var rawLine in lines)
            {
                var line = (rawLine ?? "").Trim();
                if (line.Length == 0)
                    continue;

                var parts = line.Split(';');
                if (parts.Length < 9)
                    continue;

                string firstCell = (parts[0] ?? "").Trim().TrimStart('\uFEFF');
                if (firstCell.Equals("Time", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!DateTime.TryParseExact(firstCell, dateFormats, ru, DateTimeStyles.None, out DateTime ts) &&
                    !DateTime.TryParseExact(firstCell, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out ts) &&
                    !DateTime.TryParse(firstCell, ru, DateTimeStyles.None, out ts) &&
                    !DateTime.TryParse(firstCell, CultureInfo.InvariantCulture, DateTimeStyles.None, out ts))
                    continue;

                var values = new double?[8];

                for (int ch = 1; ch <= 8; ch++)
                {
                    string valueText = (parts[ch] ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(valueText))
                        continue;

                    if (double.TryParse(valueText, NumberStyles.Float, ru, out double value) ||
                        double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    {
                        values[ch - 1] = value;
                    }
                }

                rows.Add((ts, values));
            }

            rows = rows
                .OrderBy(x => x.Timestamp)
                .ToList();

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];

                for (int ch = 1; ch <= 8; ch++)
                {
                    if (!row.Values[ch - 1].HasValue)
                        continue;

                    string unit = (ch - 1 >= 0 && ch - 1 < channels.Length && channels[ch - 1] != null)
                        ? (channels[ch - 1].Unit ?? "")
                        : "";

                    list.Add(new SampleLog
                    {
                        Channel = ch,
                        Index = rowIndex,
                        IntervalMs = 1000,
                        Value = row.Values[ch - 1].Value,
                        Unit = unit,
                        Timestamp = row.Timestamp
                    });
                }
            }

            return list;
        }
        private double ConvertRawToPhysical(int raw, ChannelConfig cfg)
        {
            int code = cfg.TypeCode;
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

        private void ResetAutoScale()
        {
            var axisY = chart1.ChartAreas[0].AxisY;
            axisY.IsStartedFromZero = false;
            axisY.Minimum = double.NaN;
            axisY.Maximum = double.NaN;
            axisY.Interval = double.NaN;
            axisY.MajorGrid.Interval = double.NaN;
            axisY.MajorTickMark.Interval = double.NaN;
            axisY.LabelStyle.Interval = double.NaN;
            axisY.LabelStyle.Format = "0.###";

            chart1.Invalidate();
        }

        private void checkAutoScaleY_CheckedChanged(object sender, EventArgs e)
        {
            if (checkAutoScaleY.Checked)
            {
                ApplyFullAutoScaleFromGlobal();
            }
            else
            {
                ResetAutoScale();
                chart1.Invalidate();
            }
        }

        private void UpdateGridCurrentFromSamples()
        {
            for (int i = 0; i < gridChannels.Rows.Count; i++)
                gridChannels.Rows[i].Cells["colCurrent"].Value = "";

            for (int ch = 1; ch <= 8; ch++)
            {
                var last = samples
                    .Where(x => x.Channel == ch)
                    .OrderByDescending(x => x.Index)
                    .FirstOrDefault();

                if (last == null)
                    continue;

                int rowIndex = ch - 1;
                if (rowIndex >= 0 && rowIndex < gridChannels.Rows.Count)
                    gridChannels.Rows[rowIndex].Cells["colCurrent"].Value =
                        last.Value.ToString("F3", CultureInfo.InvariantCulture);
            }

            gridChannels.Refresh();
        }
        private void ApplyFullAutoScaleFromGlobal()
        {
            var area = chart1.ChartAreas[0];
            var axisY = area.AxisY;

            double minY = double.PositiveInfinity;
            double maxY = double.NegativeInfinity;

            foreach (Series s in chart1.Series)
            {
                if (!s.Name.StartsWith("CH"))
                    continue;

                if (!s.Enabled)
                    continue;

                foreach (DataPoint p in s.Points)
                {
                    if (p == null || p.IsEmpty)
                        continue;

                    if (p.YValues == null || p.YValues.Length == 0)
                        continue;

                    double y = p.YValues[0];
                    if (double.IsNaN(y) || double.IsInfinity(y))
                        continue;

                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (double.IsPositiveInfinity(minY) || double.IsNegativeInfinity(maxY))
                return;

            if (minY == maxY)
            {
                minY -= 1.0;
                maxY += 1.0;
            }

            axisY.IsStartedFromZero = false;
            axisY.Minimum = minY;
            axisY.Maximum = maxY;
            axisY.Interval = double.NaN;
            axisY.MajorGrid.Interval = double.NaN;
            axisY.MajorTickMark.Interval = double.NaN;
            axisY.LabelStyle.Interval = double.NaN;
            axisY.LabelStyle.Format = "0.###";

            chart1.Invalidate();
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

            if (string.IsNullOrWhiteSpace(_currentAutoSavePath) || !File.Exists(_currentAutoSavePath))
                throw new InvalidOperationException("Нет данных: CSV файл измерений не найден.");

            string src = Path.GetFullPath(_currentAutoSavePath);
            string dst = Path.GetFullPath(path);

            if (!string.Equals(src, dst, StringComparison.OrdinalIgnoreCase))
                File.Copy(src, dst, true);
        }
        private static ChannelConfig CloneChannel(ChannelConfig src){
            if (src == null)
                return null;

            return new ChannelConfig
            {
                Enabled = src.Enabled,
                ChannelNumber = src.ChannelNumber,
                TypeCode = src.TypeCode,
                Range = src.Range,
                Unit = src.Unit,
                K = src.K,
                B = src.B
            };
        }

private static ChannelConfig[] CloneChannels(ChannelConfig[] source)
{
    if (source == null)
        return null;

    var copy = new ChannelConfig[source.Length];
    for (int i = 0; i < source.Length; i++)
        copy[i] = CloneChannel(source[i]);

    return copy;
}

private void EnableDirectMode()
{
    if (!_directModeSnapshotSaved)
    {
        ReadChannelsFromGrid();
        _directModeBackup = CloneChannels(channels);
        _directModeSnapshotSaved = true;
    }

    for (int i = 0; i < channels.Length; i++)
    {
        channels[i].K = 1.0;
        channels[i].B = 0.0;
        channels[i].Enabled = (i == 0);
    }

    FillGridFromChannels();
    UpdateSeriesVisibility();
    SetSubStatus("Режим прямого отображения включён");
}

private void DisableDirectMode()
{
    if (_directModeSnapshotSaved && _directModeBackup != null && _directModeBackup.Length == channels.Length)
    {
        channels = CloneChannels(_directModeBackup);
    }

    _directModeSnapshotSaved = false;
    _directModeBackup = null;

    FillGridFromChannels();
    UpdateSeriesVisibility();
    SetSubStatus("Режим прямого отображения выключен");
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

            var cols = header
                .Split(';')
                .Select(x => (x ?? "").Trim().TrimStart('\uFEFF'))
                .ToList();

            if (!IsMeasurementCsvHeader(cols))
                throw new InvalidDataException("Неверный CSV: ожидается заголовок Time;Channel 1;Channel 2;Channel 3;Channel 4;Channel 5;Channel 6;Channel 7;Channel 8");
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

        private static bool IsVoltageTypeCode(int code)
        {
            return code == 0 || code == 1 || code == 2 || code == 8 || code == 9 || code == 28 || code == 29;
        }

        private static bool IsCurrentTypeCode(int code)
        {
            return code == 3 || code == 7 || code == 27;
        }

        private static string GetDefaultUnitByTypeCode(int code)
        {
            if (IsCurrentTypeCode(code))
                return "мА";

            if (IsVoltageTypeCode(code))
                return "мВ";

            return "";
        }

        private string GetGridUnitText(ChannelConfig cfg)
        {
            if (cfg == null)
                return "";

            if (!checkDirectMode.Checked)
            {
                if (IsVoltageTypeCode(cfg.TypeCode))
                    return "мВ/М";

                if (IsCurrentTypeCode(cfg.TypeCode))
                    return "мА/М";

                return "";
            }

            return GetDefaultUnitByTypeCode(cfg.TypeCode);
        }


        private bool TryReadAllRawChannels(out int[] regs, out string err)
        {
            return TryReadRegs(0, 8, out regs, out err);
        }

        private bool TryReadAllTypeCodes(out int[] regs, out string err)
        {
            return TryReadRegs(40201 - 40001, 8, out regs, out err);
        }

        private void SyncChannelTypesFromDevice(bool refreshGrid)
        {
            if (client == null || !client.Connected)
                throw new InvalidOperationException("Нет подключения к устройству");

            if (!TryReadAllTypeCodes(out var typeRegs, out var err))
                throw new InvalidOperationException("Выбран неверный COM-порт или устройство не отвечает");

            ApplyTypeCodesFromRegisters(typeRegs);

            if (refreshGrid)
                FillGridFromChannels();
        }

        private void checkDirectMode_CheckedChanged(object sender, EventArgs e)
        {
            if (!_uiReady)
                return;

            try
            {
                if (gridChannels.IsCurrentCellInEditMode)
                    gridChannels.EndEdit();

                if (checkDirectMode.Checked)
                    EnableDirectMode();
                else
                    DisableDirectMode();

                labelError.Visible = false;
                labelError.Text = "";
            }
            catch (Exception ex)
            {
                ShowError("Ошибка переключения режима", ex);
            }
        }
        // === Кнопка Обновить каналы ===

        private async void btnUpdateChannels_Click(object sender, EventArgs e)
        {
            SetConnectionUiBusy(true);

            try
            {
                if (client == null || !client.Connected)
                    throw new InvalidOperationException("Нет подключения к устройству");

                int[] typeRegs = null;

                await Task.Run(() =>
                {
                    typeRegs = client.ReadHoldingRegisters(40201 - 40001, 8);

                    if (typeRegs == null || typeRegs.Length < 8)
                        throw new InvalidOperationException("Выбран неверный COM-порт или устройство не отвечает");
                });

                ApplyTypeCodesFromRegisters(typeRegs);
                FillGridFromChannels();
                ReadChannelsFromGrid();

                SetSubStatus("Конфигурация каналов обновлена из устройства");
                labelError.Visible = false;
                labelError.Text = "";
            }
            catch (Exception ex)
            {
                ShowError("Ошибка при обновлении каналов", ex);
            }
            finally
            {
                SetConnectionUiBusy(false);
            }
        }

        //=== для вкладки Каналы===
        private void InitChannelSeries()
        {
            var toRemove = new List<Series>();
            foreach (Series s in chart1.Series)
                if (s.Name != "VAH")
                    toRemove.Add(s);

            foreach (var s in toRemove)
                chart1.Series.Remove(s);

            if (chart1.Legends.Count == 0)
                chart1.Legends.Add(new Legend("MainLegend"));

            var legend = chart1.Legends[0];
            legend.Enabled = true;
            legend.Docking = Docking.Right;
            legend.Alignment = StringAlignment.Near;
            legend.LegendStyle = LegendStyle.Column;

            for (int i = 0; i < channels.Length; i++)
            {
                string name = $"CH{i + 1}";
                var s = chart1.Series.Add(name);
                s.ChartType = SeriesChartType.Spline;
                s.BorderWidth = 2;
                s.XValueType = ChartValueType.Int32;
                s.IsVisibleInLegend = true;
                s.Legend = legend.Name;
                s.LegendText = name;
                s.Enabled = channels[i] != null && channels[i].Enabled;
            }
        }
        private double ReadChannelByIndex(int index)
        {
            if (index < 0 || index >= channels.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (rawRegs == null || rawRegs.Length < channels.Length)
                throw new InvalidOperationException("Нет актуальных сырых данных каналов");

            var cfg = channels[index];
            if (!cfg.Enabled)
                throw new InvalidOperationException("Канал выключен");

            int raw = rawRegs[index];

            double physical = ConvertRawToPhysical(raw, cfg);
            double q = ApplyCalibration(physical, cfg);

            return q;
        }
        private void DoSingleMeasurementStep()
        {
            if (client == null || !client.Connected)
                throw new InvalidOperationException("Нет подключения к устройству");

            if (!TryReadAllRawChannels(out rawRegs, out var readErr))
                throw new InvalidOperationException("Не удалось прочитать каналы: " + readErr);

            long x = sampleIndex;
            bool anyMeasured = false;
            DateTime rowTime = DateTime.Now;
            double?[] rowValues = new double?[8];

            for (int i = 0; i < channels.Length; i++)
            {
                var cfg = channels[i];
                if (cfg == null || !cfg.Enabled)
                    continue;

                double value = ReadChannelByIndex(i);
                rowValues[i] = value;
                anyMeasured = true;

                if (i < gridChannels.Rows.Count)
                    gridChannels.Rows[i].Cells["colCurrent"].Value =
                        value.ToString("F3", CultureInfo.InvariantCulture);

                var item = new SampleLog
                {
                    Index = x,
                    IntervalMs = timer1.Interval,
                    Channel = cfg.ChannelNumber,
                    Value = value,
                    Unit = cfg.Unit,
                    Timestamp = rowTime
                };

                string seriesName = $"CH{cfg.ChannelNumber}";
                var s = chart1.Series[seriesName];

                long tSec = ToTimeSec(x, timer1.Interval);
                s.Points.AddXY(tSec, value);
            }

            if (!anyMeasured)
                return;

            AppendAutoSaveSnapshot(rowTime, rowValues);

            sampleIndex++;
            ApplyXAxisScale();

            if (checkAutoScaleY.Checked)
                ApplyFullAutoScaleFromGlobal();

            gridChannels.Refresh();
            chart1.Invalidate();
        }


        private static bool TryParseGridDouble(string text, out double value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            return
                double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
                double.TryParse(text, NumberStyles.Float, CultureInfo.GetCultureInfo("ru-RU"), out value) ||
                double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }


        //=== выгрузка 
        private string LastCsvPathFile =>
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last_csv_path.txt");

        private void RememberLastCsv(string path)
        {
            File.WriteAllText(LastCsvPathFile, path ?? "", Encoding.UTF8);
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
            DisconnectFromDevice();
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

        private void checkDirectMode_CheckedChanged_1(object sender, EventArgs e)
        {

        }
    }
}
