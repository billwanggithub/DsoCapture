using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSO;
using DsoCapture;
using DsoCapture.Utility;
using NationalInstruments.Visa;
using OxyPlot;
using ScottPlot;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Action = System.Action;
using Brush = System.Windows.Media.Brush;
using Image = System.Drawing.Image;

namespace Test_DSO
{
    partial class ViewModel : ObservableObject
    {
        public MainWindow? mainWindow = null;

        [ObservableProperty]
        ObservableCollection<string> deviceFilter = new()
        {
            "?*",
            "ASRL?*INSTR",
            "GPIB?*",
            "GPIB?*INSTR",
            "GPIB?*INTFC",
            "PXI?*",
            "PXI?*BACKPLANE",
            "PXI?*INSTR",
            "TCPIP?*",
            "TCPIP?*INSTR",
            "TCPIP?*SOCKET",
            "USB?*",
            "USB?*INSTR",
            "USB?*RAW",
            "VXI?*",
            "VXI?*BACKPLANE",
            "VXI?*INSTR",
        };
        [ObservableProperty]
        string selectedFilter = "USB?*INSTR";

        [ObservableProperty]
        List<string>? visaDeviceNames;
        [ObservableProperty]
        string selectedDeviceName = "";

        [ObservableProperty]
        ObservableCollection<VisaDevice>? visaDevices = new();

        int fileCounter = 0;

        [ObservableProperty]
        Brush buttonBackColor = new SolidColorBrush(Colors.GreenYellow);

        [ObservableProperty]
        public BitmapSource? bitmapSource;
        [ObservableProperty]
        bool isShowImage = false;
        [ObservableProperty]
        bool isInkSaver = true;
        [ObservableProperty]
        bool isSaveImage = false;
        [ObservableProperty]
        bool isButtonEnabled = true;
        [ObservableProperty]
        bool isConvertToExcel = true;
        [ObservableProperty]
        int imageWidth = 320;
        [ObservableProperty]
        int imageHeight = 240;
        [ObservableProperty]
        string statusText = "";

        MessageBasedSession? dso;
        HardCopyWindow? hardCopyWindow;
        int hardCopyWidth = 1920;
        int hardCopyHeight = 1080;

        [ObservableProperty]
        int progressBarValue = 1;
        [ObservableProperty]
        int progressBarMax = 1;

        readonly Brush defaultGroundColor = new SolidColorBrush(Colors.GreenYellow);

        [ObservableProperty]
        ObservableCollection<int>? resolutions = new() { 5, 10, 15, 20, 0 };
        [ObservableProperty]
        int selectedResolution = 5;

        [ObservableProperty]
        ObservableCollection<string> dsoChannels = new() { "1", "2", "3", "4" };
        [ObservableProperty]
        int selectedDsoChannelIndex = 0;

        [ObservableProperty]
        PlotModel myModel;

        public ViewModel()
        {
            FindAllVisaDevices();
        }

        [RelayCommand]
        void RefreshDevices()
        {
            FindAllVisaDevices();
        }

        [RelayCommand]
        async Task HardCopyDso()
        {
            ButtonBackColor = new SolidColorBrush(Colors.Red);
            //IsButtonEnabled = false;
            StatusText = "Reading...";

            //byte[] bmpBytes = File.ReadAllBytes(tempFile);

            // Get bmp file byes from DSO
            byte[]? bmpBytes = await GetBmpFromDso();
            if (bmpBytes is not null)
            {
                if (IsSaveImage)
                {
                    File.WriteAllBytes($"dso_{fileCounter++}.bmp", bmpBytes);
                }
            }
            else
            {
                StatusText = "Error";
                ButtonBackColor = defaultGroundColor;
                //IsButtonEnabled = true;
                return;
            }
            // convert bmp byte array to Image
            Image dsoBmp;
            using (MemoryStream ms = new(bmpBytes))
            {
                // 將 MemoryStream 轉換為 System.Drawing.Image
                dsoBmp = Image.FromStream(ms);
            }

            // ImageTo Clipboard
            System.Windows.Forms.Clipboard.SetImage(dsoBmp);

            // Image to Bitmap
            Bitmap bmp = new(dsoBmp); // From Image
            //Bitmap bmp = new Bitmap(tempFile); // From File

            // 將 Bitmap 轉換為 BitmapSource
            IntPtr hBitmap = bmp.GetHbitmap();
            BitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            StatusText = $"Size={hardCopyWidth}x{hardCopyHeight}";

            if (IsShowImage)
            {
                hardCopyWindow = new HardCopyWindow
                {
                    DataContext = this,
                    Width = 320,
                    Height = 240
                };
                hardCopyWindow.Show();
            }

            if (IsConvertToExcel)
            {
                ExcelHelper.ClipBoardToExcel(hardCopyWidth, SelectedResolution);
                //ParseToExcel();
            }

            await Task.Delay(0);
            ButtonBackColor = defaultGroundColor;
            //IsButtonEnabled = true;
        }

        [RelayCommand]
        async Task ReadWaveForm()
        {
            if (VisaDevices is null)
            {
                return;
            }

            MessageBasedSession? dso = DsoHelper.OpenDso(VisaDevices, SelectedDeviceName);
            if (dso is null)
            {
                StatusText = "Read Waveform Error";
                return;
            }

            GC.Collect();

            string channel = $"C{SelectedDsoChannelIndex + 1}";
            StatusText = $"Reading Waveform {channel}";

            DSO_DATA data_temp = new();
            await Task.Run(() =>
            {
                DsoHelper.Init(dso);
                DsoHelper.ReadRawWaveform(dso, channel);
                DsoHelper.ParseRawWaveform(channel);
                data_temp = DsoHelper.data[channel];
                DsoHelper.ProcessWaveform(ref data_temp);
                StatusText = $"{data_temp.x.Length} Points";
            });


            GC.Collect();
            //// for x86, 超過 5M點會Memory Overflow
            //if (data_temp.x.Length > 5000002)
            //{
            //    MessageBox.Show("Data Length > 5M");
            //    return;
            //}

            WaveformWindow wfmWindow = new()
            {
                DataContext = this
            };
            wfmWindow.Show();

            GC.Collect();

            await Task.Run(() =>
            {
                wfmWindow.Dispatcher.Invoke(new Action(() =>
                {
                    // Plot wit WPFPlot
                    WpfPlot wpfPlot = wfmWindow.WpfPlot1;
                    wpfPlot = new();

                    double[] xs = data_temp.x;
                    double[] ys = data_temp.y;
                    SignalPlotXY series = wfmWindow.WpfPlot1.Plot.AddSignalXY(xs, ys);
                    //series.Smooth = true;
                    wfmWindow.WpfPlot1.Plot.AxisAuto();
                    wfmWindow.WpfPlot1.Refresh();
                }));
            });

            GC.Collect();

            //// Plot with OxyPlot 
            //MyModel = new PlotModel { Title = "HDO4034A" };

            //var series = new OxyPlot.Series.LineSeries()
            ////var series = new OxyPlot.Series.ScatterSeries()
            //{
            //    MarkerType = MarkerType.Circle,
            //    MarkerSize = 0.5,
            //    MarkerStroke = OxyColors.Blue,
            //    MarkerFill = OxyColors.Blue
            //};
            //MyModel.Series.Add(series);

            ////var series = new OxyPlot.Series.LineSeries();
            //for (int i = 0; i < data_temp.x.Length; i++)
            //{
            //    var x = data_temp.x[i];
            //    var y = data_temp.y[i];

            //    series.Points.Add(new(x, y));
            //}

            ////series.Decimator = Decimator.Decimate;

            //wfmWindow.Dispatcher.Invoke(new Action(() =>
            //{
            //    series.Title = "CH1";
            //    MyModel.InvalidatePlot(true);
            //}));

            if (!IsSaveImage)
            {
                return;
            }
            StatusText = $"Writing File....";
            await Task.Run(() =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                long time1 = sw.ElapsedMilliseconds;
                var lines = Enumerable.Range(0, data_temp.x.Length).Select(i => $"{data_temp.x[i]},{data_temp.y[i]}");
                File.WriteAllText("Dso.csv", "X,Y\n");
                File.AppendAllLines("Dso.csv", lines);
                long time2 = sw.ElapsedMilliseconds;
                long diff1 = time2 - time1;
                Debug.WriteLine($"process time = {diff1} ms");
            });


            //// Slower
            //time1 = sw.ElapsedMilliseconds;
            //var points = data_temp.x.Zip(data_temp.y, (first, second) => first + "," + second).ToArray();
            //time2 = sw.ElapsedMilliseconds;
            //long diff2 = time2 - time1;
            StatusText = $"{data_temp.x.Length} Points";
            StatusText += $", Write File Done";
        }

        public void FindAllVisaDevices()
        {
            VisaDevices ??= new();
            VisaDevices = DsoHelper.FindResources(SelectedFilter);

            VisaDeviceNames = VisaDevices!.Select(x => x.Name).ToList()!;
            if (VisaDeviceNames.Count != 0)
            {
                SelectedDeviceName = VisaDeviceNames.Last();
            }
            else
            {
                StatusText = "No Devices Found";
            }
        }

        async Task<byte[]?> GetBmpFromDso()
        {
            if (VisaDevices is null)
                return null;

            try
            {
                dso = DsoHelper.OpenDso(VisaDevices, SelectedDeviceName);
                if (dso is null)
                    return null;

                dso.TimeoutMilliseconds = 10000;
                DsoHelper.HardCopy(dso, SelectedDeviceName, IsInkSaver);

                // bmp format:  https://crazycat1130.pixnet.net/blog/post/1345538
                // Read bmp header to calculate sise
                byte[] header = dso.RawIO.Read(0x36);
                hardCopyWidth = (int)(header[18] | header[19] << 8 | header[20] << 16 | header[21] << 24);  //0x12:  4 bytes
                hardCopyHeight = (int)(header[22] | header[23] << 8 | header[24] << 16 | header[24] << 24); //0x16: 4 bytes
                int size = hardCopyWidth * hardCopyHeight * 4;
                byte[] bmpBytes = new byte[size];

                ProgressBarMax = hardCopyHeight;
                ProgressBarValue = 0;
                int linesPerRead = 100;
                for (int i = 0; i < hardCopyHeight / linesPerRead; i++)
                {

                    int len = hardCopyWidth * 4 * linesPerRead;
                    byte[] bmpLine = dso.RawIO.Read(len);
                    Array.Copy(bmpLine, 0, bmpBytes, i * len, len);
                    StatusText = $"Reading {i * linesPerRead}";
                    ProgressBarValue = (i + 1) * linesPerRead;
                    await Task.Delay(1);
                }

                int lines = hardCopyHeight % linesPerRead;
                if (lines != 0)
                {
                    int len = hardCopyWidth * 4 * lines;
                    byte[] bmpLine = dso.RawIO.Read(len);
                    int lastPosition = (hardCopyHeight / linesPerRead) * linesPerRead * hardCopyWidth * 4;
                    Array.Copy(bmpLine, 0, bmpBytes, lastPosition, len);
                    ProgressBarValue += lines;
                    await Task.Delay(1);
                }

                // read pixel data
                //byte[] bmpBytes = mb.RawIO.Read(size);

                return DataHelper.CombineByteArrays(header, bmpBytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                StatusText = "Device Error";
            }
            return null;
        }
    }
}

