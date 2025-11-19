using EasyModbus;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private bool isEmulation = false;
        private bool isConnected = false;
        private readonly Random rnd = new Random();
        private double t = 0.0;

        private ModbusClient client;

        public Form1()
        {
            InitializeComponent();

            chart1.Titles.Add("График канала");
            chart1.ChartAreas[0].AxisX.Title = "Время";
            chart1.ChartAreas[0].AxisY.Title = "Значение";

            isEmulation = checkEmu.Checked;

            labelStatus.Text = "Отключено";
            labelValue.Text = "---";

            timer1.Interval = 200;

            if (comboBaud.Items.Count == 0)
            {
                comboBaud.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" });
                comboBaud.SelectedIndex = 0;
            }

            RefreshPorts();
        }

        private void checkEmu_CheckedChanged(object sender, EventArgs e)
        {
            isEmulation = checkEmu.Checked;

            if (isEmulation)
            {
                if (isConnected)
                    labelStatus.Text = "Подключено (эмуляция)";
                else
                    labelStatus.Text = "Не подключено (эмуляция)";
            }
            else
            {
                labelStatus.Text = isConnected ? "Подключено" : "Отключено";
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            timer1.Stop();
        }

        private double ReadRealChannel()
        {
            if (!isConnected || client == null || !client.Connected)
                return 0.0;

            try
            {
                int channel = (int)numChannel.Value;      // 1..8
                int register = 40001 + (channel - 1);     // 40001..40008
                int startAddress = register - 40001;      // 0..7 для EasyModbus

                int[] raw = client.ReadHoldingRegisters(startAddress, 1);
                int rawValue = raw[0];

                // предположим диапазон 0–10 В
                double volts = rawValue * 10.0 / 65535.0;
                return volts;
            }
            catch (Exception ex)
            {
                ShowError("Ошибка чтения", ex);
                return 0.0;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            double value = 0.0;

            try
            {
                if (isEmulation)
                {
                    t += 0.1;
                    value = 5.0 + Math.Sin(t) * 2.0 + rnd.NextDouble();
                }
                else
                {
                    value = ReadRealChannel(); 
                }

                labelError.Visible = false;
                labelError.Text = "";
            }
            catch (Exception ex)
            {
                ShowError("Ошибка при чтении", ex);
                return;
            }

            labelValue.Text = value.ToString("F3");

            var s = chart1.Series[0];
            s.Points.AddY(value);

            if (s.Points.Count > 200)
                s.Points.RemoveAt(0);
        }


        private void btnRead_Click(object sender, EventArgs e)
        {
            try
            {
                double value;

                if (isEmulation)
                {
                    t += 0.1;
                    value = 5.0 + Math.Sin(t) * 2.0 + rnd.NextDouble();
                }
                else
                {
                    value = ReadRealChannel(); // обработка ошибок внутри
                }

                labelValue.Text = value.ToString("F3");

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
            if (isEmulation)
            {
                isConnected = true;
                labelStatus.Text = "Подключено (эмуляция)";
                labelError.Text = "";
                labelError.Visible = false;
                return;
            }
            try
            {
                string portName = comboPorts.Text;
                int baud = int.Parse(comboBaud.Text);

                client = new ModbusClient(portName);
                client.UnitIdentifier = 1;      // адрес DAM-4602
                client.Baudrate = baud;
                client.Parity = Parity.None;
                client.StopBits = StopBits.One;

                client.ConnectionTimeout = 2000; // 2 секунды на то, что подключиться 
                client.Connect();

                isConnected = true;
                labelStatus.Text = "Подключено";

                labelError.Text = "";
                labelError.Visible = false;
            }
            catch (Exception ex)
            {
                isConnected = false;
                ShowError("Ошибка подключения", ex);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!isEmulation)
            {
                try
                {
                    if (client != null && client.Connected)
                        client.Disconnect();
                }
                catch
                {
                }
            }

            isConnected = false;
            labelError.Text = "";           
            labelError.Visible = false;

            if (isEmulation)
                labelStatus.Text = "Отключено (эмуляция)";
            else
                labelStatus.Text = "Отключено";
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

            if (ex is TimeoutException)
                message = "Устройство не отвечает";
            else if (ex is UnauthorizedAccessException)
                message = "Порт занят другой программой";
            else if (ex is IOException)
                message = "Проблема с адаптером или кабелем";
            else if (ex is ArgumentException)
                message = "Некорректное имя COM-порта";
            else
                message = "Неизвестная ошибка";

            labelError.Visible = true;
            labelError.Text = $"Ошибка: {message}\n({ex.Message})";

            labelStatus.Text = title;
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

        private void label5_Click(object sender, EventArgs e)
        {
        }

        private void labelStatus_Click(object sender, EventArgs e)
        {
        }

        private void chart1_Click(object sender, EventArgs e)
        {
        }

        private void label5_Click_1(object sender, EventArgs e)
        {

        }
    }
}
