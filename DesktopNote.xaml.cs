using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.IO;
using System.Net;

namespace Desktop_Notes_WPF
{
    /// <summary>
    /// Interaction logic for DesktopNote.xaml
    /// </summary>
    /// 

    public partial class DesktopNote : Window
    {
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;

        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        private bool autoRefresh = false;
        private UInt32 autoRefreshTime = 60;
        private App.JsonConfig storedConfig = null;

        public DesktopNote()
        {
            InitializeComponent();
        }
        static void SendWpfWindowBack(Window window)
        {
            var hWnd = new WindowInteropHelper(window).Handle;
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
        }

        private void Window_GotFocus(object sender, RoutedEventArgs e)
        {
            SendToBack();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SendToBack();
        }
        private void Window_Activated(object sender, EventArgs e)
        {
            SendToBack();
        }

        public void SendToBack()
        {
            SendWpfWindowBack(this);
        }

        public void Config(App.JsonConfig config, bool firstRun = false)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    storedConfig = config;
                    string Note = "";
                    if (config.Note.Contains("{{ref="))
                    {
                        Int32 offset = 0;
                        while (config.Note.IndexOf("{{ref=", offset) >= 0)
                        {
                            Note += config.Note[offset..config.Note.IndexOf("{{ref=", offset)];
                            int find = config.Note.IndexOf("{{ref=", offset) + 6;
                            int length = config.Note.IndexOf("}}", offset) - find;

                            if (find > 0 && length > 0)
                            {
                                // Add filename length and "{{ref=" and "}}" lengths to offset.
                                offset = find + length + 2;

                                string refPath = config.Note.Substring(find, length);

                                if (refPath.Substring(0, 4) == "http")
                                {
                                    try
                                    {
                                        var wc = new WebClient();
                                        string webContent = wc.DownloadString(refPath);
                                        Note += webContent;
                                    }
                                    catch (Exception)
                                    {
                                        Note += "Failed to load data from " + refPath;
                                    }
                                }
                                else if (refPath.Substring(0, 8) == "datetime")
                                {
                                    refPath = refPath.Replace("\'", "\"");
                                    if (refPath.Contains("(\""))
                                    {
                                        string format = refPath[10..refPath.IndexOf("\"", 10)];
                                        string datestring = System.DateTime.Now.ToString(format);
                                        Note += datestring;
                                    }
                                    else
                                    {
                                        Note += System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                                    }
                                }
                                else if (refPath.Substring(0, 6) == "system")
                                {
                                    refPath = refPath.Replace("\'", "\"");
                                    if (refPath.Contains("(\""))
                                    {
                                        string attribute = (refPath[8..refPath.IndexOf("\"", 8)]).ToLower();
                                        string systemstring = null;
                                        if (attribute == "cpu" || attribute == "processor")
                                        {
                                            var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0\");
                                            var processorName = key.GetValue("ProcessorNameString");
                                            systemstring = processorName.ToString();
                                        }
                                        else if (attribute == "ram" || attribute == "memory")
                                        {
                                            var ram = GetWindowsMetrics();
                                            if (ram.Total >= 1048576)
                                            {
                                                systemstring = Math.Round(ram.Total / 1024 /1024, 2) + "TB";
                                            }
                                            else if (ram.Total >= 1024)
                                            {
                                                systemstring = Math.Round(ram.Total / 1024, 2) + "GB";
                                            }
                                            else
                                            {
                                                systemstring = (ram.Total) + "MB";
                                            }
                                        }
                                        else if (attribute == "name" || attribute == "computername")
                                        {
                                            systemstring = System.Environment.GetEnvironmentVariable("computername");
                                        }
                                        Note += systemstring;
                                    }
                                }
                                else if (File.Exists(refPath))
                                {
                                    Note += File.ReadAllText(refPath);
                                }
                            }
                        }
                        if (offset < config.Note.Length)
                        {
                            Note += config.Note[offset..];
                        }
                    }
                    else
                    {
                        Note = config.Note;
                    }

                    Note_Text.Text = Note;
                    Note_Text.FontFamily = new FontFamily(config.Font);
                    Note_Text.FontSize = Convert.ToDouble(config.FontSize);
                    Int32 colour = Convert.ToInt32(config.FontColour, 16);
                    Color argb = Color.FromArgb(
                        Convert.ToByte((colour >> 24) & 0xFF),
                        Convert.ToByte((colour >> 16) & 0xFF),
                        Convert.ToByte((colour >> 8) & 0xFF),
                        Convert.ToByte((colour) & 0xFF)
                    );
                    Note_Text.Foreground = new SolidColorBrush(argb);
                    if (config.TextAlign.ToLower() == "left")
                        Note_Text.TextAlignment = TextAlignment.Left;
                    else if (config.TextAlign.ToLower() == "center")
                        Note_Text.TextAlignment = TextAlignment.Center;
                    else if (config.TextAlign.ToLower() == "right")
                        Note_Text.TextAlignment = TextAlignment.Right;

                    string[] styles = config.FontStyle.ToLower().Split(" ");
                    Style TextStyle = new Style();
                    if (styles.Contains("standard"))
                    {
                        Note_Text.FontWeight = FontWeights.Normal;
                    }
                    else if (styles.Contains("bold"))
                    {
                        Note_Text.FontWeight = FontWeights.Bold;
                    }
                    if (styles.Contains("italic"))
                    {
                        Note_Text.FontStyle = FontStyles.Italic;
                    }
                    else
                    {
                        Note_Text.FontStyle = FontStyles.Normal;
                    }
                    if (styles.Contains("underlined"))
                    {
                        Note_Text.TextDecorations.Add(TextDecorations.Underline);
                    }

                    if (config.Width.HasValue)
                    {
                        this.Width = (double)config.Width;
                        Note_Text.Width = this.Width;
                    }
                    if (config.Height.HasValue)
                    {
                        this.Height = (double)config.Height;
                        Note_Text.Height = this.Height;
                    }

                    if (config.LocationX.HasValue)
                    {
                        if (config.LocationX < 0)
                        {
                            // If a negative number is used place from the right edge of the screen
                            Int32 width = 0;
                            foreach (Screen curScreen in Screen.AllScreens)
                            {
                                width += curScreen.Bounds.Width;
                            }
                            this.Left = Convert.ToDouble(width + config.LocationX);
                        }
                        else
                            this.Left = Convert.ToDouble(config.LocationX);
                    }

                    if (config.LocationY.HasValue)
                    {
                        this.Top = Convert.ToDouble(config.LocationY);
                    }

                    if (config.AutoRefresh.HasValue)
                    {
                        autoRefresh = Convert.ToBoolean(config.AutoRefresh);
                    }

                    if (config.RefreshTime.HasValue)
                    {
                        autoRefreshTime = Convert.ToUInt32(config.RefreshTime);
                    }
                });

                if (firstRun == true && autoRefresh == true)
                {
                    firstRun = true;
                    Thread t = new Thread(new ThreadStart(RefreshThreadProc));
                    t.Start();
                }
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error Loading Settings", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RefreshThreadProc()
        {
            if(autoRefresh == false)
            {
                return;
            }
            RefreshConfig();
            Thread.Sleep(Convert.ToInt32(autoRefreshTime * 1000));
            if (autoRefresh == false)
            {
                return;
            }
            Thread t = new Thread(new ThreadStart(RefreshThreadProc));
            t.Start();
        }

        public void RefreshConfig()
        {
            if (storedConfig != null)
            {
                Config(storedConfig);
            }
        }


        // https://gunnarpeipman.com/dotnet-core-system-memory/
        public class MemoryMetrics
        {
            public double Total;
            public double Used;
            public double Free;
        }

        private MemoryMetrics GetWindowsMetrics()
        {
            var output = "";

            var info = new System.Diagnostics.ProcessStartInfo();
            info.FileName = "wmic";
            info.Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value";
            info.RedirectStandardOutput = true;
            info.CreateNoWindow = true;

            using (var process = System.Diagnostics.Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }

            var lines = output.Trim().Split("\n");
            var freeMemoryParts = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
            var totalMemoryParts = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries);

            var metrics = new MemoryMetrics();
            metrics.Total = Math.Round(double.Parse(totalMemoryParts[1]) / 1024, 0);
            metrics.Free = Math.Round(double.Parse(freeMemoryParts[1]) / 1024, 0);
            metrics.Used = metrics.Total - metrics.Free;

            return metrics;
        }
    }
}
