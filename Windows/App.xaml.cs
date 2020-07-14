using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace VideoCatalog.Windows {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		public static string[] mArgs;

		public static bool FoundFFMpegLibs;

		public static MainWindow MainWindow;

		private void Application_Startup(object sender, StartupEventArgs e) {
			LoadFFMpegLibs();

			MainWindow = new MainWindow();
			MainWindow.Show();

			if (e.Args.Length > 0) {
				mArgs = e.Args;
				MainWindow.OpenFolder(new DirectoryInfo(mArgs.FirstOrDefault()));
			}

			// сохранение настроек по закрытию приложения
			Exit += (s, eea) => { VideoCatalog.Properties.Settings.Default.Save(); };

		}

		///<summary> Загрузка библиотек ffmpeg для FFME. </summary>
		public static void LoadFFMpegLibs() {
			if (new DirectoryInfo(VideoCatalog.Properties.Settings.Default.FFMpegBinPath).Exists) {
				try {
					Console.WriteLine("Try load FFmpeg libs at " + VideoCatalog.Properties.Settings.Default.FFMpegBinPath);
					Unosquare.FFME.Library.FFmpegDirectory = VideoCatalog.Properties.Settings.Default.FFMpegBinPath;
					Unosquare.FFME.Library.LoadFFmpeg();
					FoundFFMpegLibs = true;
				} catch (FileNotFoundException) {
					FoundFFMpegLibs = false;
					Console.WriteLine("Can`t load FFmpeg libs at " + VideoCatalog.Properties.Settings.Default.FFMpegBinPath);
				}
			} else {
				FoundFFMpegLibs = false;
				Console.WriteLine("Folder with FFmpeg libs at " + VideoCatalog.Properties.Settings.Default.FFMpegBinPath + " not found !");
			}
		}



		}

}
