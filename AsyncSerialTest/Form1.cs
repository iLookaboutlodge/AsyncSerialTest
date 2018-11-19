using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AsyncSerialTest2
{
    public partial class Form1 : Form
    {
        private string HostSerialPort = "COM4"; //Hold this here so that we don't have to set each time we debug 

        public Form1()
        {
            InitializeComponent();
            string[] ports = SerialPort.GetPortNames();
            comboBox1.Items.AddRange(ports);
            comboBox1.SelectedItem = HostSerialPort;
        }


        private async void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Start")
            {
                button1.Text = "Starting";
                button1.Enabled = false;
                comboBox1.Enabled = false;
                string portName = comboBox1.Text;
                await Task.Delay(1000);
                var ct = new CancellationTokenSource();
                button1.Tag = ct;

                Task readTask = ReadFromSerialPortAsync(portName,
                (packet) =>
                {
                    Invoke((MethodInvoker)delegate
                    {
                        // Running on the UI thread
                        label1.Text = packet;
                    });
                }, ct.Token);

                button1.Text = "Stop";
                button1.Enabled = true;
                try
                {
                    await readTask;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                button1.Text = "Start";
                button1.Enabled = true;
                comboBox1.Enabled = true;
            }
            else if (button1.Text == "Stop")
            {
                button1.Text = "Stopping";
                button1.Enabled = false;
                await Task.Delay(200);
                (button1.Tag as CancellationTokenSource).Cancel();
            }
        }


        private async Task ReadFromSerialPortAsync(string portName, Action<string> onPacket, CancellationToken ct)
        {
            using (SerialPort sp = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One))
            {
                sp.Open();
                await Task.Factory.StartNew(() =>
                {
                    sp.BaseStream.ReadTimeout = 1500;
                    sp.DiscardInBuffer();
                    int packetId = 0;
                    var reader = new StringReader(sp.BaseStream, "$", "\r\n");
                    while (!ct.IsCancellationRequested)
                    {
                        try
                        {
                            string packetText = reader.Read();
                            if (NMEA.IsValid(packetText))
                            {
                                // Maybe parse the packetText here...
                                packetId++;
                                onPacket(packetId.ToString() + " " + packetText);
                            }
                            else
                            {
                                Debug.WriteLine(packetText);
                            }
                        }
                        catch (TimeoutException) { }
                        Thread.Yield();
                    }
                }, TaskCreationOptions.LongRunning);
            }
            Debug.WriteLine("done reader");
        }


    }
}
