using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;
using System.IO;
using System.Buffers;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Controls;

namespace Desktop_Notes_WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		public class JsonConfig
        {
#nullable enable
			public string? Font { get; set; }
			public string? FontSize { get; set; }
			public string? FontStyle { get; set; }
			public string? FontColour { get; set; }
			public string? TextAlign { get; set; }
			public Int32? LocationX { get; set; }
			public UInt32? LocationY { get; set; }
			public double? Width { get; set; }
			public double? Height { get; set; }
			public string? Note { get; set; }
			public bool? AutoRefresh { get; set; }
			public UInt32? RefreshTime { get; set; }
#nullable disable
		}

		private JsonConfig config = null;
		private bool settingsOpen = false;
		private bool autoRefresh = false;
		private static readonly DesktopNote dtNote = new DesktopNote();
		private ContextMenu ContextMenu = new ContextMenu();

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			TaskbarIcon taskbarIcon = new TaskbarIcon();
			taskbarIcon.Icon = System.Drawing.Icon.FromHandle(Desktop_Notes_WPF.Properties.Resources.Desktop_Notes.Handle);
			taskbarIcon.ToolTipText = "Desktop Notes";
			
			MenuItem itemSettings = new MenuItem();
			itemSettings.Header = "Settings";
			itemSettings.Click += Settings_Click;
			ContextMenu.Items.Add(itemSettings);

			MenuItem itemRefresh = new MenuItem();
			itemRefresh.Header = "Auto refresh";
			itemRefresh.IsCheckable = true;
			itemRefresh.Click += AutoRefresh_Click;
			ContextMenu.Items.Add(itemRefresh);

			ContextMenu.Items.Add(new Separator());

			MenuItem itemExit = new MenuItem();
			itemExit.Header = "Exit";
			itemExit.Click += Exit_Click;
			ContextMenu.Items.Add(itemExit);

			taskbarIcon.ContextMenu = ContextMenu;
			taskbarIcon.TrayMouseDoubleClick += Settings_Click;

			CheckConfig(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Desktop-Notes.json");
			config = ReadConfig(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Desktop-Notes.json");

			((MenuItem)ContextMenu.Items[1]).IsChecked = Convert.ToBoolean(config.AutoRefresh);

			dtNote.Config(config, true);
			dtNote.Show();
		}

		private void Settings_Click(object sender, System.EventArgs e)
		{
			try
			{
				if (settingsOpen == false && config != null)
				{
					settingsOpen = true;
					Settings settings = new Settings(config);
					settings.Topmost = true;
					if (settings.ShowDialog() == true)
					{
						config = settings.config;
						dtNote.Config(config);
						WriteConfig(config, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Desktop-Notes.json");
					}
					settingsOpen = false;
				}
			}
			catch (Exception ex)
            {
				MessageBox.Show(ex.Message);
            }
		}

		private void AutoRefresh_Click(object sender, System.EventArgs e)
        {
			autoRefresh = ((MenuItem)ContextMenu.Items[1]).IsChecked;
			config.AutoRefresh = autoRefresh;
			dtNote.Config(config, true);
			WriteConfig(config, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Desktop-Notes.json");
		}
		private void Exit_Click(object sender, System.EventArgs e)
        {
			dtNote.autoRefresh = false;
			dtNote.Close();
			Current.Shutdown();
		}

		private void CheckConfig(string path)
        {
			if (File.Exists(path) == true)
			{
				bool writeConfig = false;
				var jsonConfig = ReadConfig(path);
				if (jsonConfig.Font == null)
				{
					jsonConfig.Font = "Arial";
					writeConfig = true;
				}
				if (jsonConfig.FontSize == null)
				{
					jsonConfig.FontSize = "12.0";
					writeConfig = true;
				}
				if (jsonConfig.FontStyle == null)
				{
					jsonConfig.FontStyle = "Standard";
					writeConfig = true;
				}
				if (jsonConfig.FontColour == null)
				{
					jsonConfig.FontColour = "E0FFFFFF";
					writeConfig = true;
				}
				if (jsonConfig.TextAlign == null)
				{
					jsonConfig.TextAlign = "Left";
					writeConfig = true;
				}
				if (jsonConfig.LocationX == null)
				{
					jsonConfig.LocationX = 150;
					writeConfig = true;
				}
				if (jsonConfig.LocationY == null)
				{
					jsonConfig.LocationY = 150;
					writeConfig = true;
				}
				if (jsonConfig.Width == null)
				{
					jsonConfig.Width = 150;
					writeConfig = true;
				}
				if (jsonConfig.Height == null)
				{
					jsonConfig.Height = 150;
					writeConfig = true;
				}
				if (jsonConfig.Note == null)
				{
					jsonConfig.Note = "Desktop Notes";
					writeConfig = true;
				}
				if (jsonConfig.AutoRefresh == null)
                {
					jsonConfig.AutoRefresh = false;
					writeConfig = true;
                }
				if (jsonConfig.RefreshTime == null)
				{
					jsonConfig.RefreshTime = 60;
					writeConfig = true;
				}

				if (writeConfig == true)
					WriteConfig(jsonConfig, path);
				return;
			}

			GenerateDefaultConfig(path);
		}

		private void WriteConfig(JsonConfig config, string path)
        {
			try
			{
				string jsonDefault = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(path, jsonDefault);
			}
			catch (Exception ex)
            {
				MessageBox.Show(ex.Message);
            }
		}

		private JsonConfig ReadConfig(string path)
        {
			string jsonFile = File.ReadAllText(path);
#nullable enable
			JsonConfig? jsonConfig = JsonSerializer.Deserialize<JsonConfig>(jsonFile);
#nullable disable

			return jsonConfig;
        }

		private void GenerateDefaultConfig(string path)
        {
			if (File.Exists(Convert.ToString(AppDomain.CurrentDomain.BaseDirectory) + "\\default-desktop-notes.json") == true)
			{
				var defaultConfig = ReadConfig(Convert.ToString(AppDomain.CurrentDomain.BaseDirectory) + "\\default-desktop-notes.json");
				WriteConfig(defaultConfig, path);
			}
			else
			{
				var defaultConfig = new JsonConfig
				{
					Font = "Arial",
					FontSize = "12.0",
					FontStyle = "Standard",
					FontColour = "E0FFFFFF",
					TextAlign = "Left",
					LocationX = 150,
					LocationY = 150,
					Width = 300,
					Height = 200,
					Note = "Desktop Notes",
					AutoRefresh = false,
					RefreshTime = 60
				};

				WriteConfig(defaultConfig, path);
			}
		}
	}
}
