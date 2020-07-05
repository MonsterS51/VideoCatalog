using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Microsoft.WindowsAPICodePack.Shell;
using NReco.VideoConverter;     // для FFMpegConverter (выдергивание кадра)
using NReco.VideoInfo;			// для FFProbe
using YAXLib;

namespace VideoCatalog.Main {
	/// <summary>
	/// Основной класс каталога.
	/// </summary>
	public class CatalogEngine {
		public static FFProbe ffProbe = new FFProbe();
		public static string[] vidExt = new string[] { ".mkv", ".mp4", ".m4v", ".avi", ".mpg", ".m2ts", ".mpeg", ".wmv", ".flw", ".flv", ".mov", ".divx", ".webm", ".gif", ".vob"};
		public CatalogRoot CatRoot;
		public UdpClientModule udpCM;
		public static MainWindow MainWin;

		public static int coverWidth = 1000;
		public static int maxThreads = 8;

		public CatalogEngine(MainWindow mw) {
			MainWin = mw;

			//emptyCover = new BitmapImage(new Uri(@"pack://application:,,,/Assets/Icons/cross.png", UriKind.RelativeOrAbsolute));
			
			//udpCM = new UdpClientModule();
			//udpCM.SendUdp("VERSION");
			//udpCM.TestUdp("MonsterS51", "555833iddqd", "");
		}




		public void LoadCatalogRoot(string path) {
			CatRoot = new CatalogRoot();
			CatRoot.LoadRootFolder(path);
		}

		public void SaveCatalogXML(string path) {
			XElement catData = Serialize_YAX(CatRoot);
			catData.Save(path);
		}

		public void LoadCatalogXML(string path) {
			XDocument xDoc = XDocument.Load(path);
			if (xDoc != null) {
				Console.WriteLine("Deserialize " + path);
				CatalogRoot loadedCatRoot = Deserialize_YAX(xDoc.Root) as CatalogRoot;
				if (loadedCatRoot != null) {
					Console.WriteLine($"Loaded <{path}>");
					CatalogEngine.MainWin.startToolbar.Visibility = Visibility.Hidden;
					CatRoot = loadedCatRoot;
					CatRoot.CatPath = new FileInfo(path).Directory.FullName;    // корень - папка с файлом
					CatRoot.LoadDeserial();
					Console.WriteLine("Restore albumes and entrys DONE !");
				}
			} else {
				System.Windows.MessageBox.Show($"Can`t load <{path}>", "Error");
				CatalogEngine.MainWin.CloseCatalog(null, null);
			}

		}



		/// <summary> Загрузка изображения по пути к файлу. </summary>
		public static BitmapImage LoadBitMap(string path) {
			BitmapImage coverImage = new BitmapImage();

			//TODO подсовывать зафризеную пустышку
			if (string.IsNullOrWhiteSpace(path)) return coverImage;
			FileInfo fi = new FileInfo(path);
			if (!fi.Exists) return coverImage;

			using (FileStream memory = File.OpenRead(path)) {
				try {
					coverImage.BeginInit();
					coverImage.StreamSource = memory;
					coverImage.CacheOption = BitmapCacheOption.OnLoad;
					coverImage.DecodePixelWidth = CatalogEngine.coverWidth;
					coverImage.EndInit();
					coverImage.StreamSource = null;
					memory.Close();
					memory.Dispose();
				} catch (Exception ex) {
					Console.WriteLine(ex);
				}
			}
			coverImage.Freeze();	// замораживаем только после избавления от стрима, иначе GC уже не может его ликвидировать
			return coverImage;
		}

		/// <summary> Загрузка изображения из середины видео по пути к файлу. </summary>
		public static BitmapImage LoadBitMapFromVideo(string path) {
			BitmapImage coverImage = new BitmapImage();

			if (string.IsNullOrWhiteSpace(path)) return coverImage;
			FileInfo fi = new FileInfo(path);
			if (!fi.Exists) return coverImage;

			using (MemoryStream memory = new MemoryStream()) {
				try {
					FFMpegConverter ffMpeg = new FFMpegConverter();

					var totDur = ffProbe.GetMediaInfo(path).Duration.TotalSeconds;
					float middle = 0;
					if (totDur > 1) middle = (float)(totDur / 2) ;		// если меньше секунды, кадр из середины выдернуть не может и выбрасывает
					ffMpeg.GetVideoThumbnail(path, memory, middle);
					memory.Position = 0;

					try {
						coverImage = new BitmapImage();
						coverImage.BeginInit();
						coverImage.StreamSource = memory;
						coverImage.CacheOption = BitmapCacheOption.OnLoad;
						coverImage.DecodePixelWidth = CatalogEngine.coverWidth;
						coverImage.EndInit();
						coverImage.StreamSource = null;
					} catch (NotSupportedException ex2) { 
						Console.WriteLine(ex2);
						coverImage = new BitmapImage();
					}

					memory.Close();
					memory.Dispose();
					ffMpeg.Stop();
				} catch (FFMpegException ex) {
					Console.WriteLine(ex);
				} catch (IOException ex3) {
					Console.WriteLine(ex3);
				} catch (ThreadAbortException ex4) {
					Console.WriteLine(ex4);
				} catch (Exception ex5) {
					Console.WriteLine(ex5);
				}
			}

			coverImage.Freeze();	// замораживаем только после избавления от стрима, иначе GC уже не может его ликвидировать
			return coverImage;
		}

		/// <summary> Получить продолжительность видео в секундах. </summary>
		public static int GetDuration(string path) {
			if (string.IsNullOrWhiteSpace(path)) return 1;
			FileInfo fi = new FileInfo(path);
			if (!fi.Exists) return 1;

			object dur = null;
			try {
				var file = ShellFile.FromFilePath(path);
				dur = file?.Properties?.System?.Media?.Duration?.ValueAsObject;
			} catch (ArgumentException ex) {
				Console.WriteLine(ex.Message);
			}

			if (dur == null) {
				// на случай, если не смогли получить через шел
				try {
					return (int)ffProbe.GetMediaInfo(path).Duration.TotalSeconds;
				} catch (Exception) {
					return 0;
				}
			}

			var t = (ulong) dur;
			return (int) TimeSpan.FromTicks((long)t).TotalSeconds;
		}

		//---R

		#region YAX Serializer
		public static XElement Serialize_YAX(object obj) {
			YAXSerializer serializer = new YAXSerializer(obj.GetType(), YAXSerializationOptions.DontSerializeNullObjects);
			return serializer.SerializeToXDocument(obj).Root;
		}

		public static object Deserialize_YAX(XElement xml) {
			Console.WriteLine($"Load type <{xml.Name.LocalName}>");
			Type type = Type.GetType("VideoCatalog.Main." + xml.Name.LocalName);
			if (type != null) {
				YAXSerializer serializer = new YAXSerializer(type, YAXSerializationOptions.DontSerializeNullObjects);
				//BUG YAX при десериализации портит XML данные (как минимум для списков с кастомными объектами), поэтому перегоняем XElement в строку
				return serializer.Deserialize(xml.ToString());
			} else {
				System.Windows.MessageBox.Show($"Wrong type <{xml.Name.LocalName}>", "Error Deserialize");
			}
			return null;
		}
		#endregion

		#region Shell32
		public static void OpenExplorer(string filePath) {
			if (filePath == null)
				throw new ArgumentNullException("filePath");

			IntPtr pidl = ILCreateFromPathW(filePath);
			SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
			ILFree(pidl);
		}

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr ILCreateFromPathW(string pszPath);

		[DllImport("shell32.dll")]
		private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, int cild, IntPtr apidl, int dwFlags);

		[DllImport("shell32.dll")]
		private static extern void ILFree(IntPtr pidl);
		#endregion
	}
}
