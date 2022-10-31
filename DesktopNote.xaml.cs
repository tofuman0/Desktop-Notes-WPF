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
using System.Windows.Forms;
using System.IO;

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
            try
            {
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
                            if (File.Exists(refPath))
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
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show("Error Loading Settings", ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
