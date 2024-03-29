﻿using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System;
using System.IO;
using System.Linq;
using System.Windows;
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
		public FileInfo EntAbsFile {
			get { return new FileInfo(CatalogRoot.CatDir.FullName + catAlb.RelPath + RelPath); }
			set { RelPath = value.FullName.Substring(CatalogRoot.CatDir.FullName.Length).Substring(catAlb.RelPath.Length); }
		}

		//---
		public CatalogAlbum catAlb = null;

		private string[] imgExt = new string[] { ".png", ".gif", ".jpg", ".jpeg", ".bmp", ".tiff" };

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

		public CatalogEntry() {
		}

		public CatalogEntry(FileInfo file, CatalogAlbum CatAlb) {
			catAlb = CatAlb;
			EntAbsFile = file;
			Name = file.Name;
			SearchCoverArt();
			GetMetaData();
			DateAdded = DateTime.Today;
		}

		///<summary> Поиск файла обложки для эпизода. </summary>
		private void SearchCoverArt() {
			coverArtPath = null;
			string imgPath = EntAbsFile.Directory + Path.GetFileNameWithoutExtension(EntAbsFile.FullName) + "";

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
					var w = int.Parse(width);
					if (Properties.Settings.Default.UseShellCover) {
						// грузим обложку/кадр, созданную виндой
						bmi = CatalogEngine.GetBitMapFromShell(EntAbsFile.FullName, w);
					} else {
						// создаем ковер из кадра файла
						float vidPos = 0;
						if (duration > 1) vidPos = (float)(duration / 2);       // если меньше секунды, кадр из середины выдернуть не может и выбрасывает
						bmi = CatalogEngine.LoadBitMapFromVideo(EntAbsFile.FullName, w, vidPos);
					}
				}
				CoverImage = bmi;
			}

			// принудительная перерисовка обложки после загрузки
			Action EmptyDelegate = delegate () { };
			vp?.Dispatcher?.Invoke(DispatcherPriority.Render, EmptyDelegate);
			lp?.Dispatcher?.Invoke(DispatcherPriority.Render, EmptyDelegate);
		}

		//---

		/// <summary> Создание плэйта эпизода. </summary>
		public override ViewPlate CreatePlate() {
			if (vp == null) vp = new ViewPlate();

			vp.DataContext = this;
			vp.onDoubleClick = () => System.Diagnostics.Process.Start(EntAbsFile.FullName);
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

			lp.DataContext = this;
			lp.onDoubleClick = () => System.Diagnostics.Process.Start(EntAbsFile.FullName);
			lp.onClick = () => App.MainWin.OpenSidePanel(this);

			TimeSpan ts = new TimeSpan(0, 0, duration);

			int hours = (int)ts.TotalHours;
			int minutes = ts.Minutes;

			if (hours == 0 & minutes == 0) TopRightText = $"{width}x{height}	({ts.Seconds} sec)";
			else TopRightText = $"{width}x{height}	({hours}:{minutes})";

			UpdateAtrText();
			if (string.IsNullOrWhiteSpace(AtrText)) lp.lblAtr.Visibility = Visibility.Collapsed;
			else lp.lblAtr.Visibility = Visibility.Visible;

			UpdateIconBrokenState();
			UpdateVideoResIcons();

			return lp;
		}

		//---
		///<summary> Получение метаданных из видеофайла. </summary>
		public void GetMetaData() {
			if (!EntAbsFile.Exists) return;

			duration = CatalogEngine.GetDuration(EntAbsFile.FullName);
			DateCreate = EntAbsFile.CreationTime;
			DateModify = EntAbsFile.LastWriteTime;

			//+ загрузка данных через ShellFile
			//? для древних и не стандартных видеофайлов лучше поставить кодеки (иначе не сможет читать из них данные) !
			try {
				var file = ShellFile.FromFilePath(EntAbsFile.FullName);
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
					var dataSrc = CatalogEngine.ffProbe.GetMediaInfo(EntAbsFile.FullName).Streams.First();
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
		private void ChkVideoResolution() {
			int.TryParse(height, out int h);
			int.TryParse(width, out int w);

			if (((float)w / h) < 1.77f) {
				if (h < 700) { vidRes = VideoResolution.LQ; return; }
				if (h < 1000) { vidRes = VideoResolution.HD; return; }
				if (h < 1400) { vidRes = VideoResolution.FHD; return; }
				if (h < 2100) { vidRes = VideoResolution.QHD; return; }
			} else {
				if (w < 1200) { vidRes = VideoResolution.LQ; return; }
				if (w < 1900) { vidRes = VideoResolution.HD; return; }
				if (w < 2500) { vidRes = VideoResolution.FHD; return; }
				if (w < 4000) { vidRes = VideoResolution.QHD; return; }
			}

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

		///<summary> Получить максимальное качество из всех элементов альбома. </summary>
		public override CatalogEntry.VideoResolution GetMaxRes() {
			return vidRes;
		}

		public override string ToString() {
			return Name;
		}
	}
}