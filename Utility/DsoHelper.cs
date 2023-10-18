using Ivi.Visa;
using NationalInstruments.Visa;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace DSO
{
    public class DSO_DATA
    {

        public byte[] raw = new byte[1];  // raw data , including header
        public byte[] data = new byte[1]; // waveform data without header
        public byte[] header = new byte[1]; // header bytes
        public double[] x = new double[1];
        public double[] y = new double[1];
        public string? ch;
        public int bytes_per_point;
        public float vertical_gain;
        public float vertical_offset;
        public float vertical_zero = 0; // for tektronix
        public float horizontal_interval;
        public double horizontal_offset;

        public static int FindIndex(byte[] bytes, string key)
        {
            int byte_count = bytes.Length;
            byte[] buffer = new byte[key.Length];
            int i;
            for (i = 0; i < byte_count; i++)
            {
                Buffer.BlockCopy(bytes, i, buffer, 0, 8);
                if (Encoding.UTF8.GetString(buffer) == key)
                {
                    break;
                }
            }
            return i;
        }
    }
    class DsoHelper
    {
        public static Dictionary<string, DSO_DATA> data = new() {
            { "C1", new DSO_DATA() },
            { "C2", new DSO_DATA() },
            { "C3", new DSO_DATA() },
            { "C4", new DSO_DATA() },
        };

        public static ObservableCollection<VisaDevice>? FindResources(string filter = "USB?*")
        {
            MessageBasedSession mb;
            ObservableCollection<VisaDevice> visaDevices = new();
            // This example uses an instance of the NationalInstruments.Visa.ResourceManager class to find resources on the system.
            // Alternatively, static methods provided by the Ivi.Visa.ResourceManager class may be used when an application
            // requires additional VISA .NET implementations.
            List<string>? resources;
            using (var rm = new ResourceManager())
            {
                try
                {
                    resources = rm.Find(filter).ToList();
                    foreach (string s in resources)
                    {
                        //可以根據ParseResult查詢出硬體類型，如Custom，Gpib，Serial，Usb等
                        ParseResult parseResult = rm.Parse(s);
                        HardwareInterfaceType hardwareType = parseResult.InterfaceType;

                        mb = (MessageBasedSession)rm.Open(s);
                        mb.TimeoutMilliseconds = 5000;
                        mb.RawIO.Write("*IDN?\n");
                        string name = mb.RawIO.ReadString().Replace("\n", "").Replace("*IDN", "");
                        mb.Dispose();

                        visaDevices.Add(new VisaDevice() { Address = s, HwType = hardwareType, Name = name });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            return visaDevices; // resources;
        }
        public static double GetPixelCount(double cm, double scale)
        {
            double dpi = 96; // 220, 150, 96
            return (cm * dpi * scale) / 2.54;
        }

        public static string? GetVisaAddress(ObservableCollection<VisaDevice> devices, string name)
        {
            return devices.Where(x => x.Name == name).Select(x => x.Address).ToList().FirstOrDefault();
        }
        public static MessageBasedSession? OpenDso(ObservableCollection<VisaDevice> devices, string name)
        {
            if (devices == null)
            {
                return null;
            }

            if (devices.Count == 0)
            {
                return null;
            }
            ResourceManager rm = new();
            string? address = GetVisaAddress(devices, name);
            return (MessageBasedSession)rm.Open(address);
        }

        public static void HardCopy(MessageBasedSession mb, string name, bool isInkSaver)
        {
            mb.TimeoutMilliseconds = 10000;
            if (name.Contains("LECROY"))
            {
                if (isInkSaver)
                {
                    mb.RawIO.Write("HARDCOPY_SETUP DEV,BMP,FORMAT,LANDSCAPE,BCKG,WHITE,DEST,REMOTE\n");
                }
                else
                {
                    mb.RawIO.Write("HARDCOPY_SETUP DEV,BMP,FORMAT,LANDSCAPE,BCKG,BLACK,DEST,REMOTE\n");
                }
                mb.RawIO.Write("SCREEN_DUMP\n");
            }
            else if (name.Contains("TEKTRONIX"))
            {
                mb.RawIO.Write("SAVE:IMAGE:FILEFORMAT BMP\n");
                if (isInkSaver)
                {
                    mb.RawIO.Write("HARDCOPY:INKSAVER ON\n");
                }
                else
                {
                    mb.RawIO.Write("HARDCOPY:INKSAVER OFF\n");
                }
                mb.RawIO.Write("HARDCOPY START\n");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="name"></param>
        /// <param name="channel">C1~C4</param>
        /// <returns></returns>
        public static byte[]? ReadRawWaveform(MessageBasedSession? mb, string channel)
        {
            if (mb == null)
                return null;

            byte[]? bytes = null;
            ////// Read waveform all command
            mb.RawIO.Write($"{channel}:WAVEFORM?\n");
            data[channel].raw = mb.RawIO.Read(1000000 * 30);
            return bytes;
        }

        public static string? Init(MessageBasedSession? mb)
        {
            ////// 連接示波器

            ////// 設定示波器波形參數
            if (mb == null)
            {
                return null;
            }
            mb.RawIO.Write("COMM_HEADER SHORT\n");  //短header
            mb.RawIO.Write("COMM_FORMAT DEF9,WORD,BIN\n"); //16 bits binary   

            ////// Read Waveform setup
            mb.RawIO.Write("WAVEFORM_SETUP?\n");
            Thread.Sleep(1000);
            string echo_string = Encoding.UTF8.GetString(mb.RawIO.Read());
            Console.WriteLine(echo_string);
            return echo_string;
        }

        /*
         * 讀出波形參數: data count, gain , offset
         */
        public static string ParseRawWaveform(string name)
        {
            /* Extract data from read back waveform  */
            ////// 找"WAVEDESC"出現的offset
            int descriptor_offset = 0; // the first "WAVEDESC" position , short header:21, no header:15
            byte[] wavedesc_buffer = new byte[8];
            for (int i = 0; i < 50; i++)
            {
                descriptor_offset = i;
                Buffer.BlockCopy(data[name].raw, i, wavedesc_buffer, 0, 8);
                if (Encoding.UTF8.GetString(wavedesc_buffer) == "WAVEDESC")
                {
                    break;
                }
            }

            if (descriptor_offset > 30)
            {
                return "No \"WAVEDESC\" header Found!!";
            }


            int waveform_descriptor_size = (int)BitConverter.ToUInt32(data[name].raw, descriptor_offset + 36);
            int data_offset = descriptor_offset + waveform_descriptor_size;
            int data_count = (int)BitConverter.ToInt32(data[name].raw, descriptor_offset + 116);
            /*
              < 32>          COMM_TYPE: enum          ; chosen by remote command COMM_FORMAT
               _0      byte             
               _1      word             
               endenum     
             */
            data[name].bytes_per_point = (int)BitConverter.ToInt16(data[name].raw, descriptor_offset + 32);
            //byte[] ch1_bytes = new byte[data_count * (data[name].bytes_per_point + 1)];

            Array.Resize(ref data[name].data, data_count * 2);
            Array.Resize(ref data[name].x, data_count);
            Array.Resize(ref data[name].y, data_count);
            Buffer.BlockCopy(data[name].raw, data_offset, data[name].data, 0, data_count * 2); // data_count*2 = byte count

            ////// Get Vertical Scale information
            data[name].vertical_gain = BitConverter.ToSingle(data[name].raw, descriptor_offset + 156);
            data[name].vertical_offset = BitConverter.ToSingle(data[name].raw, descriptor_offset + 160);
            string vertical_uint = System.Text.Encoding.UTF8.GetString(data[name].raw, descriptor_offset + 196, 48);
            Console.WriteLine("Vertical Uint = " + vertical_uint);

            ////// Get Horizonal Scale information
            data[name].horizontal_interval = BitConverter.ToSingle(data[name].raw, descriptor_offset + 176);
            data[name].horizontal_offset = BitConverter.ToDouble(data[name].raw, descriptor_offset + 180);
            string horizontal_uint = System.Text.Encoding.UTF8.GetString(data[name].raw, descriptor_offset + 244, 48);
            Console.WriteLine("Horizontal Uint = " + horizontal_uint);
            Console.WriteLine($"dt = {data[name].horizontal_interval}");
            GC.Collect();
            return "WAVEDESC OK";
        }

        public static void ProcessWaveform(ref DSO_DATA data)
        {
            ///// Scaling the vertical data
            int data_count = data.x.Length;
            int update_interval = 100000;

            //motor_ui.console_print(richTextBox_console, $"total {toolStripProgressBar1.ProgressBar.Maximum}\n");
            if (data_count < 2)
            {
                //motor_ui.console_print(richTextBox_console, $"no data on {0}\n");
                return;
            }

            for (int i = 0; i < data_count; i++)
            {
                data.y[i] = BitConverter.ToInt16(data.data, i * 2) * data.vertical_gain - data.vertical_offset;
                data.x[i] = i * data.horizontal_interval + data.horizontal_offset;
                if ((i % update_interval) == 0)
                {
                    //Thread.Sleep(1);
                    //motor_ui.console_print(richTextBox_console, ".");
                }
            }
        }
    }
}
