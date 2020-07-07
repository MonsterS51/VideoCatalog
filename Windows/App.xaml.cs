using LibVLCSharp.Shared;
using Meta.Vlc.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace VideoCatalog {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		public static string[] mArgs;

		public static VlcPlayer mediaPlayer;

		private void Application_Startup(object sender, StartupEventArgs e) {
			// загрузка библиотек ffmpeg для FFME
			if (new DirectoryInfo(VideoCatalog.Properties.Settings.Default.FFMpegBinPath).Exists) {
				try {
					Unosquare.FFME.Library.FFmpegDirectory = VideoCatalog.Properties.Settings.Default.FFMpegBinPath;
					Unosquare.FFME.Library.LoadFFmpeg();
					VideoCatalog.Properties.Settings.Default.FoundFFMpegLibs = true;
				} catch (FileNotFoundException) {
					VideoCatalog.Properties.Settings.Default.FoundFFMpegLibs = false;
					Console.WriteLine("Cant load FFmpeg libs at " + VideoCatalog.Properties.Settings.Default.FFMpegBinPath);
				}
			} else {
				VideoCatalog.Properties.Settings.Default.FoundFFMpegLibs = false;
				Console.WriteLine("Folder with FFmpeg libs at " + VideoCatalog.Properties.Settings.Default.FFMpegBinPath + " not found !");
			}



			MainWindow mainWindow = new MainWindow();
			mainWindow.Show();

			if (e.Args.Length > 0) {
				mArgs = e.Args;
				mainWindow.OpenFolder(new DirectoryInfo(mArgs.FirstOrDefault()));
			}


		}

	}

}
