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

		public static MainWindow MainWin;

		private void Application_Startup(object sender, StartupEventArgs e) {
			LoadFFMpegLibs();

			MainWin = new MainWindow();
			MainWin.Show();

			if (e.Args.Length > 0) {
				mArgs = e.Args;
				MainWin.OpenFolder(new DirectoryInfo(mArgs.FirstOrDefault()));
			}

			// сохранение настроек по закрытию приложения
			Exit += (s, eea) => { VideoCatalog.Properties.Settings.Default.Save(); };

		}

		///<summary> Загрузка библиотек ffmpeg для FFME. </summary>
		public static void LoadFFMpegLibs() {
			var dirInfo = new DirectoryInfo(VideoCatalog.Properties.Settings.Default.FFMpegBinPath);
			if (dirInfo.Exists) {
				try {
					Console.WriteLine($"Try load FFmpeg libs at {dirInfo}");
					Unosquare.FFME.Library.FFmpegDirectory = dirInfo.FullName;
					Unosquare.FFME.Library.LoadFFmpeg();
					FoundFFMpegLibs = true;
				} catch (DllNotFoundException) {
					FoundFFMpegLibs = false;
					Console.WriteLine($"Can`t load FFmpeg libs at {dirInfo}");
				} catch (FileNotFoundException) {
					FoundFFMpegLibs = false;
					Console.WriteLine($"Can`t find some files of FFmpeg libs at {dirInfo}");
				}
			} else {
				FoundFFMpegLibs = false;
				Console.WriteLine($"Folder with FFmpeg libs at {dirInfo} not found !");
			}
		}



	}

}
