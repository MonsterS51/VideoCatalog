using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using NReco.VideoInfo;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VideoCatalog.Panels;
using VideoCatalog.Windows;
using YAXLib;

namespace VideoCatalog.Main {
	/// <summary>
	/// Класс элемента каталога.
	/// </summary>
	public class CatalogEntry : AbstractEntry {

		//---
		[YAXDontSerialize]
		public FileInfo EntAbsFile { get; set; }

		[YAXDontSerialize]
		public string EntAbsPath { 
			get { return EntAbsFile.FullName; } 
			set { 
				EntAbsFile = new FileInfo(value);
				string result = value.Substring(CatalogRoot.CatDir.FullName.Length);
				_entRelPath = result.Substring(catAlb.AlbRelPath.Length);
			} 
		}

		public string EntRelPath { get { return _entRelPath; } set { _entRelPath = value; } }
		private string _entRelPath;

		//---
		public CatalogAlbum catAlb = null;

		private string[] imgExt = new string[]{".png", ".gif", ".jpg", ".jpeg", ".bmp", ".tiff" };

		private string coverArtPath = null;

		public int duration = 0;
		public string width = null;
		public string height = null;
		public VideoResolution vidRes;
		public DateTime DateCreate = DateTime.Now;
		public DateTime DateModify = DateTime.Now;

		///<summary> Усредненная оценка разрешения видео. </summary>
		public enum VideoResolution {
			LQ,
			HD,
			FHD,
			QHD,
			UHD
		}


		public CatalogEntry() {}
		
		public CatalogEntry(FileInfo file, CatalogAlbum CatAlb) {
			catAlb = CatAlb;
			EntAbsPath = file.FullName;
			UpdatePaths();
			Name = file.Name;
			SearchCoverArt();
			GetMetaData();
		}
		public void UpdatePaths() {
			EntAbsFile = new FileInfo(CatalogRoot.CatDir.FullName + catAlb.AlbRelPath + _entRelPath);
		}

		///<summary> Поиск файла обложки для эпизода. </summary>
		private void SearchCoverArt() {
			coverArtPath = null;
			string imgPath = EntAbsFile.Directory + Path.GetFileNameWithoutExtension(EntAbsFile.FullName) + "" ;


			try {
				// первое изображение  с тем же именем, что и файл
				var imgFilesList = EntAbsFile.Directory.EnumerateFiles().FirstOrDefault(f => imgExt.Contains(f.Extension)
					&& Path.GetFileNameWithoutExtension(EntAbsFile.FullName) == Path.GetFileNameWithoutExtension(f.FullName));

				coverArtPath = imgFilesList?.FullName;
			} catch (UnauthorizedAccessException) {
				Console.WriteLine("No access to " + EntAbsFile.Directory.FullName);
			}
		}

		/// <summary> Формирование обложки эпизода. </summary>
		public void LoadCover(bool forceUpdate = false) {
			EntAbsFile.Refresh();
			if (!EntAbsFile.Exists) return;
			// если облогу не находили или она перестала существовать
			if (coverArtPath == null || !File.Exists(coverArtPath)) {
				SearchCoverArt();
			}

			if (CoverImage == null | forceUpdate) { // только если еще не грузили или нужно обновить принудительно
				CoverImage = null;
				BitmapImage bmi;
				if (coverArtPath != null) {
					// грузим ковер из папки
					bmi = CatalogEngine.LoadBitMap(coverArtPath);
				} else {
					// создаем ковер из кадра файла

					float vidPos = 0;
					if (duration > 1) vidPos = (float)(duration / 2);       // если меньше секунды, кадр из середины выдернуть не может и выбрасывает

					bmi = CatalogEngine.LoadBitMapFromVideo(EntAbsPath, int.Parse(width), vidPos);
				}
				CoverImage = bmi;
			}

			vp?.Dispatcher?.Invoke(DispatcherPriority.Render, EmptyDelegate);	// принудительная перерисовка обложки после загрузки
		}

		private static Action EmptyDelegate = delegate () { };

		//---

		/// <summary> Создание плэйта эпизода. </summary>
		public override ViewPlate CreatePlate() {
			if (vp == null) vp = new ViewPlate();

			vp.path = EntAbsPath;
			vp.DataContext = this;
			vp.duration = duration;
			vp.onDoubleClick = () => System.Diagnostics.Process.Start(EntAbsPath);
			vp.onClick = () => App.MainWin.OpenSidePanel(this);

			TimeSpan ts = new TimeSpan(0, 0, duration);

			int hours = (int)ts.TotalHours;
			int minutes = ts.Minutes;

			if (hours == 0 & minutes == 0) TopRightText = $"{width}x{height}\n{ts.Seconds} sec";
			else TopRightText = $"{width}x{height}\n{hours}:{minutes}";

			UpdateIconBrokenState();
			UpdateVideoResIcons();

			return vp; 
		}

		/// <summary> Создание плэйта эпизода. </summary>
		public override ListPlate CreateListPlate() {
			if (lp == null) lp = new ListPlate();

			lp.path = EntAbsPath;
			lp.DataContext = this;
			lp.duration = duration;
			lp.onDoubleClick = () => System.Diagnostics.Process.Start(EntAbsPath);
			lp.onClick = () => App.MainWin.OpenSidePanel(this);

			TimeSpan ts = new TimeSpan(0, 0, duration);

			int hours = (int)ts.TotalHours;
			int minutes = ts.Minutes;

			if (hours == 0 & minutes == 0) TopRightText = $"{width}x{height}	({ts.Seconds} sec)";
			else TopRightText = $"{width}x{height}	({hours}:{minutes})";

			UpdateIconBrokenState();
			UpdateVideoResIcons();

			return lp;
		}


		//---
		public void GetMetaData() {
			if (!EntAbsFile.Exists) return;

			duration = CatalogEngine.GetDuration(EntAbsPath);
			DateCreate = EntAbsFile.CreationTime;
			DateModify = EntAbsFile.LastWriteTime;

			//+ загрузка данных через ShellFile
			//? для древних и не стандартных видеофайлов лучше поставить кодеки (иначе не сможет читать из них данные) !
			try {
				var file = ShellFile.FromFilePath(EntAbsPath);
				if (file != null) {
					file.Properties.System.Video.FrameWidth.TryFormatForDisplay(PropertyDescriptionFormatOptions.None, out width);
					file.Properties.System.Video.FrameHeight.TryFormatForDisplay(PropertyDescriptionFormatOptions.None, out height);
				}
			} catch (ArgumentException ex) {
				Console.WriteLine(ex.Message);
			} catch (IndexOutOfRangeException ex2) {
				Console.WriteLine(ex2.Message);
			}

			//+ на случай, если не смогли получить через шел или надпись "Нет данных"
			//! ! значительно медленней, чем через ShellFile
			if (string.IsNullOrWhiteSpace(width) || width.Any(x => char.IsLetter(x)) || string.IsNullOrWhiteSpace(height) || height.Any(x => char.IsLetter(x))) {
				//Console.WriteLine("" + _entRelPath);
				try {
					var dataSrc = CatalogEngine.ffProbe.GetMediaInfo(EntAbsPath).Streams.First();
					width = "" + dataSrc.Width;
					height = "" + dataSrc.Height;
				} catch (Exception ex) {
					width = "0";
					height = "0";
					Console.WriteLine(ex.Message);
				}
			}

			ChkVideoResolution();
		}

		///<summary> Возвращает самую раннюю(позднюю) дату создания файла из всех входящих элементов. </summary>
		public override DateTime GetDateCreate(bool byLatest = false) {
			return DateCreate;
		}

		///<summary> Возвращает самую раннюю(позднюю) дату изменения файла из всех входящих элементов. </summary>
		public override DateTime GetDateModify(bool byLatest = false) {
			return DateModify;
		}

		///<summary> Открыть место хранения файла элемента. </summary>
		public override void OpenInExplorer() {
			EntAbsFile.Refresh();
			if (EntAbsFile.Exists) CatalogEngine.OpenExplorer(EntAbsFile.FullName);
		}

		///<summary> Проверка состояния актуальности элемента каталога. </summary>
		public void ChkEntState() {
			isBroken = false;
			EntAbsFile.Refresh();
			if (!EntAbsFile.Exists) {
				isBroken = true;
				Console.WriteLine($"Entry {Name} path {EntAbsFile} not exist !");
			}

			UpdateIconBrokenState();
		}

		///<summary> Оценка качества видео. </summary>
		public void ChkVideoResolution() {
			int.TryParse(height, out int h);
			if (h < 700) { vidRes = VideoResolution.LQ; return; }
			if (h < 1000) { vidRes = VideoResolution.HD; return; }
			if (h < 1400) { vidRes = VideoResolution.FHD; return; }
			if (h < 2100) { vidRes = VideoResolution.QHD; return; }
			vidRes = VideoResolution.UHD;
		}

		///<summary> Обновление отображения плашек качества. </summary>
		public void UpdateVideoResIcons() {

			if (vp != null) {
				vp.UpdateVideoResIcons(
					vidRes == (CatalogEntry.VideoResolution.LQ),
					vidRes == (CatalogEntry.VideoResolution.HD),
					vidRes == (CatalogEntry.VideoResolution.FHD),
					vidRes == (CatalogEntry.VideoResolution.QHD),
					vidRes == (CatalogEntry.VideoResolution.UHD)
				);
			}

			if (lp != null) {
				lp.UpdateVideoResIcons(
					vidRes == (CatalogEntry.VideoResolution.LQ),
					vidRes == (CatalogEntry.VideoResolution.HD),
					vidRes == (CatalogEntry.VideoResolution.FHD),
					vidRes == (CatalogEntry.VideoResolution.QHD),
					vidRes == (CatalogEntry.VideoResolution.UHD)
				);
			}
		}


	}
}
