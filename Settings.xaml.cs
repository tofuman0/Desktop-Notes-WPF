using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;

namespace Desktop_Notes_WPF
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public App.JsonConfig config = null;
        public Settings(App.JsonConfig lconfig)
        {
            if (lconfig == null)
                this.Close();
            config = lconfig;
            InitializeComponent();
            tbX.Text = Convert.ToString(config.LocationX);
            tbY.Text = Convert.ToString(config.LocationY);
            tbAlpha.Text = Convert.ToString(
                Convert.ToUInt32(
                    (100.0 / 255.0) * 
                    ((Convert.ToUInt32(Convert.ToString(config.FontColour), 16) >> 24) & 0xFF)
                )
            );
            cbAlignment.Text = Convert.ToString(config.TextAlign);
            tbWidth.Text = Convert.ToString(config.Width);
            tbHeight.Text = Convert.ToString(config.Height);
            DesktopNote.Text = Convert.ToString(config.Note);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                config.Note = DesktopNote.Text;
                config.LocationX = Convert.ToInt32(tbX.Text);
                config.LocationY = Math.Max(0, Convert.ToUInt32(tbY.Text));
                config.TextAlign = cbAlignment.Text;
                config.Width = Math.Max(0, Convert.ToUInt32(tbWidth.Text));
                config.Height = Math.Max(0, Convert.ToUInt32(tbHeight.Text));
                UInt32 colour = Convert.ToUInt32(Convert.ToString(config.FontColour), 16) & 0x00FFFFFF;
                UInt32 alpha = (Convert.ToUInt32((255.0 / 100.0) * Math.Min(100, Math.Max(0, Convert.ToUInt32(tbAlpha.Text)))) & 0xFF) << 24;
                config.FontColour = Convert.ToString(colour | alpha, 16);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private string GetFontStyle(System.Drawing.FontStyle fontStyle)
        {
            string style = "";
            if ((fontStyle & System.Drawing.FontStyle.Bold) != System.Drawing.FontStyle.Bold)
            {
                style += "standard ";
            }
            if ((fontStyle & System.Drawing.FontStyle.Bold) == System.Drawing.FontStyle.Bold)
            {
                style += "bold ";
            }
            if ((fontStyle & System.Drawing.FontStyle.Italic) == System.Drawing.FontStyle.Italic)
            {
                style += "italic ";
            }
            if ((fontStyle & System.Drawing.FontStyle.Underline) == System.Drawing.FontStyle.Underline)
            {
                style += "underline ";
            }
            return style;
        }

        private void btnChangeColour_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            colorDialog.Color = System.Drawing.Color.FromArgb(
                Convert.ToByte((Convert.ToUInt32(config.FontColour, 16) >> 24) & 0xFF),
                Convert.ToByte((Convert.ToUInt32(config.FontColour, 16) >> 16) & 0xFF),
                Convert.ToByte((Convert.ToUInt32(config.FontColour, 16) >> 8) & 0xFF),
                Convert.ToByte((Convert.ToUInt32(config.FontColour, 16) >> 0) & 0xFF)
            );
            if(colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                config.FontColour = Convert.ToString(colorDialog.Color.ToArgb(), 16);
            }
        }

        private void btnChangeFont_Click(object sender, RoutedEventArgs e)
        {
            FontDialog fontDialog = new FontDialog();
            fontDialog.ShowColor = false;
            string[] styles = config.FontStyle.ToLower().Split(" ");
            System.Drawing.Font font = new System.Drawing.Font(Convert.ToString(config.Font), Convert.ToSingle(config.FontSize));
            System.Drawing.FontStyle fontStyle = new System.Drawing.FontStyle();
            if (styles.Contains("standard")) { }
            if (styles.Contains("bold"))
            {
                fontStyle |= System.Drawing.FontStyle.Bold;
            }
            if (styles.Contains("italic"))
            {
                fontStyle |= System.Drawing.FontStyle.Italic;
            }
            if (styles.Contains("underlined"))
            {
                fontStyle |= System.Drawing.FontStyle.Underline;
            }

            fontDialog.Font = new System.Drawing.Font(font, fontStyle);

            if (fontDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                config.Font = fontDialog.Font.Name;
                config.FontSize = Convert.ToString(fontDialog.Font.Size);
                config.FontStyle = GetFontStyle(fontDialog.Font.Style);
            }
        }
    }
}
