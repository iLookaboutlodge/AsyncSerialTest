using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NmeaEmulator
{
    public partial class Form1 : Form
    {

        /*
         * SEE  http://com0com.sourceforge.net/
         * 
         * Setup a virtual comport pair.
         * Set the port name for this emulator to one of the port-pair ports.
         * Connect to the other port in the pair as if it were an external device sending NMEA sentences.
         */


        public Form1()
        {
            InitializeComponent();

            string[] ports = SerialPort.GetPortNames();
            comboBox1.Items.AddRange(ports);
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
                Task writeTask = EmulateDeviceAsync(portName, ct.Token);
                button1.Text = "Stop";
                button1.Enabled = true;
                try
                {
                    await writeTask;
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


        public static async Task EmulateDeviceAsync(string portName, CancellationToken ct)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "NmeaEmulator.nmeaSample.txt";

            await Task.Factory.StartNew(() =>
            {

                List<string> lines = new List<string>();
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        string nmea = reader.ReadLine();
                        lines.Add(nmea);
                    }
                }

                using (SerialPort sp = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One))
                {
                    sp.Open();

                    sp.BaseStream.WriteTimeout = 100;
                    int packetId = 0;
                    while (!ct.IsCancellationRequested)
                    {
                        string s = lines[packetId] + "\r\n";
                        byte[] buffer = Encoding.ASCII.GetBytes(s);
                        packetId++;
                        if (packetId == lines.Count) packetId = 0;

                        try
                        {
                            sp.BaseStream.Write(buffer, 0, buffer.Length);
                            
                            try { Task.Delay(300, ct).Wait(); } catch { } //This allows the delay to be arbitrarily large, but interruptible by the cancellation token                         }
                        }
                        catch (TimeoutException) { }
                    }
                }
            }, TaskCreationOptions.LongRunning);

            Debug.WriteLine("done emulator");
        }

    }
}
