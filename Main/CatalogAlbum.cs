using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VideoCatalog.Panels;
using VideoCatalog.Util;
using VideoCatalog.Windows;
using YAXLib;

namespace VideoCatalog.Main {
	/// <summary>
	/// Набор объектов каталога.
	/// </summary>
	public class CatalogAlbum : AbstractEntry {

		//---

		[YAXDontSerialize]
		public DirectoryInfo AlbAbsDir {
			get { return new DirectoryInfo(CatalogRoot.CatDir.FullName + RelPath); }
			set { RelPath = value.FullName.Substring(CatalogRoot.CatDir.FullName.Length); }
		}

		//---
		public List<CatalogEntry> EntryList { get; set; } = new List<CatalogEntry>();

		public bool WithSubDir { get; set; } = false;

		public override CatalogEntry BaseEntry { get { return EntryList?.Where(ent => !ent.IsExcepted & !ent.isBroken).FirstOrDefault(); } }

		private object locker = new object();



		public CatalogAlbum() { }

		public CatalogAlbum(DirectoryInfo dir, bool withSubDir) {
			AlbAbsDir = dir;
			//UpdatePaths();
			WithSubDir = withSubDir;
			Name = dir.Name;
			DateAdded = DateTime.Today;
		}

		///<summary> Обновление ссылок элементов альбома на содержащий их альбом. </summary>
		public void UpdateEntCatAlb() {
			foreach (var ent in EntryList) {
				ent.catAlb = this;
			}
		}

		///<summary> Формирование элементов альбома по путям. </summary>
		public void LoadDir() {
			AlbAbsDir.Refresh();
			if (!AlbAbsDir.Exists) return;
			List<FileInfo> vidList = null;

			try {
				if (WithSubDir) {
					vidList = AlbAbsDir.EnumerateFiles("*.*", SearchOption.AllDirectories).Where(f => CatalogEngine.vidExt.ContainsIC(f.Extension)).ToList();
				} else {
					vidList = AlbAbsDir.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly).Where(f => CatalogEngine.vidExt.ContainsIC(f.Extension)).ToList();
				}
			} catch (UnauthorizedAccessException) {
				Console.WriteLine("No access to folder " + AlbAbsDir.FullName);
				return;
			}

			// удаляем файлы с пустым расширением, почему то они проходят проверку на расширения
			vidList.RemoveAll(vid => string.IsNullOrWhiteSpace(vid.Extension));

			Parallel.ForEach(vidList, new ParallelOptions { MaxDegreeOfParallelism = CatalogEngine.maxThreads },
				file => {
					// не формируем, если такое было
					lock (locker) if (EntryList.Any(ent2 => ent2.EntAbsFile.FullName == file.FullName)) return;
					CatalogEntry newEnt = new CatalogEntry(file, this);
					lock (locker) EntryList.Add(newEnt);
				}
			);

			// сортируем, т.к. потоки закончились в разнобой
			EntryList = EntryList.OrderBy(x => x.Name, new AlphanumComparatorFast()).ToList();
		}

		//---

		#region Covers Load

		/// <summary> Формирование обложки альбома на основе первого эпизода. </summary>
		public void LoadAlbumCover() {
			BaseEntry?.LoadCover(true);
			CoverImage = BaseEntry?.CoverImage;
			vp?.Dispatcher?.Invoke(DispatcherPriority.Render, EmptyDelegate);   // принудительная перерисовка обложки после загрузки
		}

		private static Action EmptyDelegate = delegate () { };

		private Thread updThread;
		/// <summary> Формирование обложек эпизодов альбома в отдельном потоке в паралельном режиме. </summary>
		public void LoadEntCoversThreaded(bool forceUpdate = false) {
			void thread() {
				Parallel.ForEach(EntryList, new ParallelOptions { MaxDegreeOfParallelism = CatalogEngine.maxThreads },
				  ent => { ent.LoadCover(forceUpdate); }
				  );

				// таски здесь почему то запускаются с лютой задержкой в ~20 сек
			}
			updThread = new Thread(thread);
			updThread.Start();
		}

		/// <summary> Принудительная остановка потока загрузки обложек. </summary>
		public void StopThread() {
			if (updThread != null) {
				updThread.Abort();
			}
		}

		/// <summary> Принудительное обновление обложек альбома и его эпизодов. </summary>
		public void UpdateAlbumArt() {
			LoadAlbumCover();
			LoadEntCoversThreaded(true);
		}
		#endregion

		//---

		/// <summary> Создание плэйта альбома. </summary>
		public override ViewPlate CreatePlate() {
			if (vp == null) {
				vp = new ViewPlate();
				vp.DataContext = this;

				vp.onClick = () => App.MainWin.OpenSidePanel(this);
				vp.onDoubleClick = () => App.MainWin.OpenAlbumTab(this);
				vp.onWheelClick = () => App.MainWin.OpenAlbumTab(this, false);
			}

			TopRightText = "" + EntryList.Count;

			UpdateIconBrokenState();
			UpdateVideoResIcons();
			return vp;
		}

		/// <summary> Создание плэйта альбома. </summary>
		public override ListPlate CreateListPlate() {
			if (lp == null) {
				lp = new ListPlate();
				lp.DataContext = this;

				lp.onClick = () => App.MainWin.OpenSidePanel(this);
				lp.onDoubleClick = () => App.MainWin.OpenAlbumTab(this);
				lp.onWheelClick = () => App.MainWin.OpenAlbumTab(this, false);
			}

			TopRightText = $"({EntryList.Count} ep.)";

			UpdateAtrText();
			if (string.IsNullOrWhiteSpace(AtrText)) lp.lblAtr.Visibility = Visibility.Collapsed;
			else lp.lblAtr.Visibility = Visibility.Visible;

			UpdateIconBrokenState();
			UpdateVideoResIcons();
			return lp;
		}

		//---

		///<summary> Возвращает самую раннюю(позднюю) дату создания файла из всех входящих элементов. </summary>
		public override DateTime GetDateCreate(bool byLatest = false) {
			if (EntryList.Count == 0) return new DateTime();
			if (byLatest) return EntryList.OrderBy(a => a.DateCreate).Last().DateCreate;
			return EntryList.OrderBy(a => a.DateCreate).First().DateCreate;
		}

		///<summary> Возвращает самую раннюю(позднюю) дату изменения файла из всех входящих элементов. </summary>
		public override DateTime GetDateModify(bool byLatest = false) {
			if (EntryList.Count == 0) return new DateTime();
			if (byLatest) return EntryList.OrderBy(a => a.DateModify).LastOrDefault().DateModify;
			return EntryList.OrderBy(a => a.DateModify).FirstOrDefault().DateModify;
		}

		///<summary> Открыть место хранения файла элемента. </summary>
		public override void OpenInExplorer() {
			string path = GetFirstEntPath();
			if (!string.IsNullOrWhiteSpace(path)) CatalogEngine.OpenExplorer(path);
		}

		//---

		///<summary> Получение пути к первой директории или папке альбома (например для перехода в проводнике). </summary>
		public string GetFirstEntPath() {
			AlbAbsDir.Refresh();
			if (!AlbAbsDir.Exists) return null;
			var dirs = AlbAbsDir.GetDirectories();
			if (dirs.Count() > 0) {
				return dirs.FirstOrDefault().FullName;
			} else {
				var files = AlbAbsDir.GetFiles();
				if (files.Count() > 0) {
					return files.FirstOrDefault().FullName;
				}
			}
			return null;
		}

		///<summary> Проверка состояния актуальности альбома. </summary>
		public void ChkAlbState() {
			isBroken = false;
			AlbAbsDir.Refresh();
			if (!AlbAbsDir.Exists) {
				isBroken = true;
				Console.WriteLine($"Albume {Name} path {AlbAbsDir} not exist !");
			} else {

				try {
					if (WithSubDir) {
						if (!AlbAbsDir.EnumerateFiles("*.*", SearchOption.AllDirectories).Any(f => CatalogEngine.vidExt.ContainsIC(f.Extension))) {
							isBroken = true;
							Console.WriteLine($"Albume {Name} folder {AlbAbsDir} and subfolders not contain video !");
						}
					} else {
						if (!AlbAbsDir.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly).Any(f => CatalogEngine.vidExt.ContainsIC(f.Extension))) {
							isBroken = true;
							Console.WriteLine($"Albume {Name} folder {AlbAbsDir} not contain video !");
						}
					}
				} catch (UnauthorizedAccessException) {
					Console.WriteLine("No access to folder " + AlbAbsDir.FullName);
					isBroken = true;
				}
			}

			foreach (var ent in EntryList) {
				ent.ChkEntState();
				if (ent.isBroken) isBroken = true;
			}

			UpdateIconBrokenState();
		}

		///<summary> Обновление отображения плашек качества. </summary>
		public void UpdateVideoResIcons() {
			var resList = new List<CatalogEntry.VideoResolution>();

			foreach (var ent in EntryList) {
				if (!resList.Contains(ent.vidRes)) resList.Add(ent.vidRes);
			}

			if (vp != null) {
				vp.UpdateVideoResIcons(
					resList.Contains(CatalogEntry.VideoResolution.LQ),
					resList.Contains(CatalogEntry.VideoResolution.HD),
					resList.Contains(CatalogEntry.VideoResolution.FHD),
					resList.Contains(CatalogEntry.VideoResolution.QHD),
					resList.Contains(CatalogEntry.VideoResolution.UHD)
				);
			}

			if (lp != null) {
				lp.UpdateVideoResIcons(
					resList.Contains(CatalogEntry.VideoResolution.LQ),
					resList.Contains(CatalogEntry.VideoResolution.HD),
					resList.Contains(CatalogEntry.VideoResolution.FHD),
					resList.Contains(CatalogEntry.VideoResolution.QHD),
					resList.Contains(CatalogEntry.VideoResolution.UHD)
				);
			}


		}

		///<summary> Получить максимальное качество из всех элементов альбома. </summary>
		public override CatalogEntry.VideoResolution GetMaxRes() {
			var resList = new List<CatalogEntry.VideoResolution>();
			foreach (var ent in EntryList) {
				if (!resList.Contains(ent.vidRes)) resList.Add(ent.vidRes);			
			}

			if (resList.Contains(CatalogEntry.VideoResolution.UHD)) return CatalogEntry.VideoResolution.UHD;
			if (resList.Contains(CatalogEntry.VideoResolution.QHD)) return CatalogEntry.VideoResolution.QHD;
			if (resList.Contains(CatalogEntry.VideoResolution.FHD)) return CatalogEntry.VideoResolution.FHD;
			if (resList.Contains(CatalogEntry.VideoResolution.HD)) return CatalogEntry.VideoResolution.HD;
			return CatalogEntry.VideoResolution.LQ;
		}

		public override string ToString() {
			return Name;
		}

	}

}
