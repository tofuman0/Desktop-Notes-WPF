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
using System.Text.Json;
using System.Management;
using System.Net.Http;
using System.Windows.Forms.VisualStyles;
using System.CodeDom.Compiler;

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

        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_SHOWWINDOW = 0x0040;

        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        public bool autoRefresh = false;
        private UInt32 autoRefreshTime = 60;
        private App.JsonConfig storedConfig = null;
        
        public DesktopNote()
        {
            InitializeComponent();
        }

        ~DesktopNote()
        {
            Stop();
        }
        static void SendWpfWindowBack(Window window)
        {
            var hWnd = new WindowInteropHelper(window).Handle;
            //SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
            IntPtr desktopHwnd = GetShellWindow();
            SetWindowPos(hWnd, desktopHwnd, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);
        }

        public void Stop()
        {
            autoRefresh = false;
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

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Show();
                WindowState = WindowState.Normal;
            }
        }

        public void SendToBack()
        {
            SendWpfWindowBack(this);
        }

        public void Config(App.JsonConfig config, bool firstRun = false)
        {
            try
            {
                this.Dispatcher.Invoke(async () =>
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

                                string refPath = config.Note.Substring(find, length).ToLower();

                                if (refPath.Substring(0, 4) == "http")
                                {
                                    try
                                    {
                                        var hc = new HttpClient();
                                        string webContent = await hc.GetStringAsync(refPath);
                                        hc.Dispose();
                                        Note += webContent;
                                    }
                                    catch (Exception)
                                    {
                                        Note += "Failed to load data from " + refPath + "\n";
                                    }
                                }
                                else if (refPath.Substring(0,5) == "wmi(\"")
                                {
                                    var query = refPath[5..refPath.IndexOf("\"", 5)];
                                    var option = (refPath.Split("\",\"").Length > 1) ? refPath.Split("\",\"")[1].Replace("\")","") : null;
                                    if(query.Contains("select") && query.Contains("from"))
                                    {
                                        try
                                        {
                                            var wmi = GetWmiResults(query);

                                            if (option == null || option == "0" || option == "false")
                                            {
                                                Int32 maxElements = Int32.MaxValue;
                                                foreach (var results in wmi.Values)
                                                {
                                                    if (results.Count < maxElements)
                                                    {
                                                        maxElements = results.Count;
                                                    }
                                                }
                                                for(Int32 i = 0; i < maxElements; i++)
                                                {
                                                    foreach(var key in wmi.Keys)
                                                    {
                                                        Note += key + ": " + wmi[key][i] + "\n";
                                                    }
                                                }
                                            }
                                            else if (option == "1" || option == "true")
                                            {
                                                foreach (var results in wmi)
                                                {
                                                    Note += results.Key + ": ";
                                                    foreach (var value in results.Value)
                                                    {
                                                        Note += value + "\n";
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Note += "Invalid option in WMI query: " + option + "\n";
                                            }
                                            /*
                                            var wmi = new ManagementObjectSearcher(query).Get();
                                            
                                            foreach(var result in wmi)
                                            {
                                                var variables = result.Properties;
                                                foreach(var v in variables)
                                                {
                                                    if (result[v.Name] != null)
                                                    {
                                                        Note += v.Name + ": " + result[v.Name].ToString() + "\n";
                                                    }
                                                }
                                                Note += "\n";
                                            }
                                            */
                                        }
                                        catch
                                        {
                                            Note += "Invalid WMI query: " + query + "\n";
                                        }
                                    }
                                    else
                                    {
                                        Note += "Invalid WMI query: " + query + "\n";
                                    }
                                }
                                else if (refPath.Substring(0, 6) == "json(\"")
                                {
                                    refPath = refPath.Replace("\'", "\"");
                                    var tokens = refPath[6..refPath.IndexOf(")", 6)].ToLower().Split("\",\"");
                                    if (tokens.Count() >= 1)
                                    {
                                        try
                                        {
                                            var hc = new HttpClient();
                                            if (tokens.Count() == 1)
                                            {
                                                tokens[0] = tokens[0].Replace("\"", "");
                                            }

                                            string webContent = await hc.GetStringAsync(tokens[0]);
                                            hc.Dispose();

                                            if (tokens.Count() > 1)
                                            {
                                                // Check for properties
                                                Dictionary<string, object> elements = null;
                                                string elementString = webContent;
                                                bool arrayNewlineSeparator = false;
                                                string elementsuffix = "";
                                                string elementprefix = "";
                                                // If nested it will loop through until it finds the property
                                                for (Int32 i = 1; i < tokens.Length; i++)
                                                {
                                                    if(tokens[i] == "newlineseparator=true")
                                                    {
                                                        arrayNewlineSeparator = true;
                                                        continue;
                                                    }
                                                    else if (tokens[i] == "newlineseparator=false")
                                                    {
                                                        arrayNewlineSeparator = false;
                                                        continue;
                                                    }
                                                    else if (tokens[i].StartsWith("elementsuffix="))
                                                    {
                                                        elementsuffix = tokens[i].Substring(14);
                                                        continue;
                                                    }
                                                    else if (tokens[i].StartsWith("elementprefix="))
                                                    {
                                                        elementprefix = tokens[i].Substring(14);
                                                        continue;
                                                    }
                                                    elements = JsonSerializer.Deserialize<Dictionary<string, object>>(elementString);
                                                    elementString = elements[tokens[i].Replace("\"","")].ToString();                                                    
                                                }
                                                if (elementString.Contains("[") && elementString.Contains("]"))
                                                {
                                                    elementString = elementString.Replace("[", "");
                                                    elementString = elementString.Replace("]", "");
                                                    if(arrayNewlineSeparator == false)
                                                    {
                                                        elementString = elementprefix + elementString.Replace(",", elementsuffix + ", " + elementprefix);
                                                        elementString = elementString + elementsuffix;
                                                    }
                                                    else
                                                    {
                                                        elementString = elementprefix + elementString.Replace(",", elementsuffix + "\n" + elementprefix);
                                                        elementString = elementString + elementsuffix;
                                                    }
                                                }
                                                Note += elementString;
                                            }
                                            else if (tokens.Count() == 1)
                                            {
                                                Note += webContent;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Note += "Failed to load json data: " + ex.Message + "\n";
                                        }
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
                                        var tokens = (refPath[8..refPath.IndexOf(")", 8)]).Replace("\"", "").Replace(" ", "").ToLower().Split(',');
                                        if (tokens.Length > 0)
                                        {
                                            string attribute = tokens[0];
                                            string systemstring = null;
                                            if (attribute == "cpu" || attribute == "processor")
                                            {
                                                var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0\");
                                                var processorName = key.GetValue("ProcessorNameString");
                                                systemstring = processorName.ToString().Trim();
                                            }
                                            else if (attribute == "ram" || attribute == "memory")
                                            {
                                                var ram = GetWindowsRamMetrics();
                                                if (ram.Total >= 1048576)
                                                {
                                                    systemstring = Math.Round(ram.Total / 1024 / 1024, 2) + "TB";
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
                                            else if (attribute.StartsWith("harddisks") || attribute.StartsWith("hdd"))
                                            {
                                                var hdds = GetWindowsDiskMetrics();
                                                bool includeFreeSpace = attribute.EndsWith("_includefreespace");
                                                foreach (var hdd in hdds)
                                                {
                                                    if(tokens.Length > 1 && tokens.Contains(hdd.DeviceID.Substring(0, 1).ToLower()) == false)
                                                    {
                                                        continue;
                                                    }

                                                    if (includeFreeSpace)
                                                    {
                                                        if (hdd.FreeSpace >= 1048576)
                                                        {
                                                            systemstring += hdd.DeviceID + " " + Math.Round(hdd.FreeSpace / 1024 / 1024, 2) + "TB of ";
                                                        }
                                                        else if (hdd.Size >= 1024)
                                                        {
                                                            systemstring += hdd.DeviceID + " " + Math.Round(hdd.FreeSpace / 1024, 2) + "GB of ";
                                                        }
                                                        else
                                                        {
                                                            systemstring += hdd.DeviceID + " " + Math.Round(hdd.FreeSpace, 2) + "MB of ";
                                                        }

                                                        if (hdd.Size >= 1048576)
                                                        {
                                                            systemstring += Math.Round(hdd.Size / 1024 / 1024, 2) + "TB\n";
                                                        }
                                                        else if (hdd.Size >= 1024)
                                                        {
                                                            systemstring += Math.Round(hdd.Size / 1024, 2) + "GB\n";
                                                        }
                                                        else
                                                        {
                                                            systemstring += Math.Round(hdd.Size, 2) + "MB\n";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (hdd.Size >= 1048576)
                                                        {
                                                            systemstring += hdd.DeviceID + " " + Math.Round(hdd.Size / 1024 / 1024, 2) + "TB\n";
                                                        }
                                                        else if (hdd.Size >= 1024)
                                                        {
                                                            systemstring += hdd.DeviceID + " " + Math.Round(hdd.Size / 1024, 2) + "GB\n";
                                                        }
                                                        else
                                                        {
                                                            systemstring += hdd.DeviceID + " " + Math.Round(hdd.Size, 2) + "MB\n";
                                                        }
                                                    }
                                                }
                                                if (systemstring != null)
                                                    systemstring = systemstring.Trim('\n');
                                            }
                                            else if (attribute.Substring(0, 2) == "ip")
                                            {
                                                var ipdetails = GetIPDetails();
                                                foreach (var ipdetail in ipdetails)
                                                {
                                                    if (attribute == "ipv6")
                                                    {
                                                        foreach (var ipv6 in ipdetail.IPv6)
                                                        {
                                                            systemstring += ipv6 + "\n";
                                                        }
                                                    }
                                                    else if (attribute == "ipv4" || attribute == "ip")
                                                    {
                                                        foreach (var ipv4 in ipdetail.IPv4)
                                                        {
                                                            systemstring += ipv4 + "\n";
                                                        }
                                                    }
                                                    else if(attribute == "ipgateway")
                                                    {
                                                        foreach (var ipgateway in ipdetail.Gateway)
                                                        {
                                                            systemstring += ipgateway + "\n";
                                                        }
                                                    }
                                                    else if (attribute == "ipdetail")
                                                    {
                                                        systemstring += ipdetail.Description + "\n";
                                                        if (ipdetail.IPv4.Count() > 0)
                                                        {
                                                            systemstring += "IPv4: ";
                                                            foreach (var ipv4 in ipdetail.IPv4)
                                                            {
                                                                systemstring += ipv4 + ", ";
                                                            }
                                                            systemstring = systemstring.TrimEnd(' ');
                                                            systemstring = systemstring.TrimEnd(',');
                                                            systemstring += "\n";
                                                        }
                                                        if (ipdetail.IPv6.Count() > 0)
                                                        {
                                                            systemstring += "IPv6: ";
                                                            foreach (var ipv6 in ipdetail.IPv6)
                                                            {
                                                                systemstring += ipv6 + ", ";
                                                            }
                                                            systemstring = systemstring.TrimEnd(' ');
                                                            systemstring = systemstring.TrimEnd(',');
                                                            systemstring += "\n";
                                                        }
                                                        if (ipdetail.Gateway.Count() > 0)
                                                        {
                                                            systemstring += "IP Gateway: ";
                                                            foreach (var ipgateway in ipdetail.Gateway)
                                                            {
                                                                systemstring += ipgateway + ", ";
                                                            }
                                                            systemstring = systemstring.TrimEnd(' ');
                                                            systemstring = systemstring.TrimEnd(',');
                                                            systemstring += "\n";
                                                        }
                                                    }
                                                    systemstring += "\n";
                                                }
                                                if (systemstring != null)
                                                    systemstring = systemstring.Trim('\n');
                                            }
                                            else if (attribute == "name" || attribute == "computername")
                                            {
                                                systemstring = System.Environment.GetEnvironmentVariable("computername");
                                            }
                                            if (systemstring != null)
                                                Note += systemstring;
                                        }
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
            RefreshConfig();
            UInt32 waitTime = autoRefreshTime;
            while(waitTime > 0)
            {
                if (autoRefresh == false)
                {
                    return;
                }
                waitTime -= 1;
                Thread.Sleep(Convert.ToInt32(1000));
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

        public class DiskMetrics
        {
            public string DeviceID;
            public string Decription;
            public double Size;
            public double FreeSpace;
        }

        public class IPDetails
        {
            public string Description;
            public List<string> IPv4 = [];
            public List<string> IPv6 = [];
            public List<string> Gateway = [];
        }

        private Dictionary<String, List<String>> GetWmiResults(String query)
        {
            var wmi = new ManagementObjectSearcher(query).Get();
            var results = new Dictionary<String, List<String>>();

            foreach (var result in wmi)
            {
                var variables = result.Properties;
                foreach (var v in variables)
                {
                    if (result[v.Name] != null)
                    {
                        if (results.ContainsKey(v.Name) == false)
                        {
                            results.Add(v.Name, []);
                        }
                        results[v.Name].Add(result[v.Name].ToString());
                    }
                }
            }
            return results;
        }

        private MemoryMetrics GetWindowsRamMetrics()
        {
            var wmi = new ManagementObjectSearcher("SELECT FreePhysicalMemory,TotalVisibleMemorySize FROM Win32_OperatingSystem").Get().OfType<ManagementObject>().FirstOrDefault();
            var freeMemory = wmi["FreePhysicalMemory"].ToString();
            var totalMemory = wmi["TotalVisibleMemorySize"].ToString();
            var metrics = new MemoryMetrics();
            metrics.Total = Math.Round(double.Parse(totalMemory) / 1024, 0);
            metrics.Free = Math.Round(double.Parse(freeMemory) / 1024, 0);
            metrics.Used = metrics.Total - metrics.Free;

            return metrics;
        }

        private List<DiskMetrics> GetWindowsDiskMetrics()
        {
            var wmi = new ManagementObjectSearcher("SELECT Description,DeviceID,Size,FreeSpace FROM Win32_LogicalDisk").Get();
            var metrics = new List<DiskMetrics>();
            
            foreach(var disk in wmi)
            {
                var diskmetric = new DiskMetrics();
                if (disk["Size"] != null)
                {
                    diskmetric.Decription = disk["Description"].ToString();
                    diskmetric.DeviceID = disk["DeviceID"].ToString();
                    diskmetric.Size = Convert.ToDouble(disk["Size"].ToString());
                    diskmetric.FreeSpace = Convert.ToDouble(disk["FreeSpace"].ToString());

                    if(diskmetric.Size > 0)
                    {
                        diskmetric.Size = diskmetric.Size / 1024 / 1024;
                    }

                    if(diskmetric.FreeSpace > 0)
                    {
                        diskmetric.FreeSpace = diskmetric.FreeSpace / 1024 / 1024;
                    }

                    metrics.Add(diskmetric);
                }
            }
           
            return metrics;
        }
        
        private List<IPDetails> GetIPDetails()
        {
            var wmi = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration").Get();
            var ipDetails = new List<IPDetails>();

            foreach(var nic in wmi)
            {
                var ipdetail = new IPDetails();
                if (nic["IPAddress"] != null)
                {
                    ipdetail.Description = nic["Description"].ToString();
                    foreach(var ip in ((String[])nic["IPAddress"]))
                    {
                        if (ip.Contains(':'))
                        {
                            ipdetail.IPv6.Add(ip);
                        }
                        else
                        {
                            ipdetail.IPv4.Add(ip);
                        }
                    }
                    if (nic["DefaultIPGateway"] != null)
                    {
                        foreach(var gateway in ((String[])nic["DefaultIPGateway"]))
                        {
                            ipdetail.Gateway.Add(gateway.ToString());
                        }
                    }
                    ipDetails.Add(ipdetail);
                }
            }

            return ipDetails;
        }
    }
}
