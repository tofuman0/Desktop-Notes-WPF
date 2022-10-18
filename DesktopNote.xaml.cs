using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public void Config(App.JsonConfig config)
        {
            Note_Text.Text = config.Note;
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
            if (styles.Contains("standard")) { }
            if (styles.Contains("bold"))
            {
                Note_Text.FontWeight = FontWeights.Bold;
            }
            if (styles.Contains("italic")) 
            {
                Note_Text.FontStyle = FontStyles.Italic;
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
                this.Left = Convert.ToDouble(config.LocationX);
            }

            if (config.LocationY.HasValue)
            {
                this.Top = Convert.ToDouble(config.LocationY);
            }
        }
    }
}
