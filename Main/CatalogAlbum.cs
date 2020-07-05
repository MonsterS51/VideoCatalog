using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using YAXLib;
using VideoCatalog.Windows;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using System.Windows.Documents;

namespace VideoCatalog.Main {
	/// <summary>
	/// Набор объектов каталога.
	/// </summary>
	public class CatalogAlbum : AbstractEntry {

		//---

		[YAXDontSerialize]
		private DirectoryInfo AlbAbsDir { get; set; }

		// при задании полного пути, обновляем относительный путь
		[YAXDontSerialize]
		public string AlbAbsPath { get { return AlbAbsDir.FullName; } 
			set { AlbAbsDir = new DirectoryInfo(value);
				_albRelPath = value.Substring(CatalogRoot.CatDir.FullName.Length); } 
		}

		// при восстановление относительного, воссоздаем и абсолютный путь
		public string AlbRelPath { get { return _albRelPath; } set { _albRelPath = value;  } }	

		private string _albRelPath = "";

		//---

		public List<CatalogEntry> EntryList { get; set; } = new List<CatalogEntry>();


		public bool WithSubDir { get; set; } = false;

		private int duration = -1;

		[YAXDontSerialize]
		public string FirstEntPath { get { return EntryList?.FirstOrDefault()?.EntAbsPath; } }

		private object locker = new object();



		public CatalogAlbum() {}

		public CatalogAlbum(DirectoryInfo dir, bool withSubDir) {
			AlbAbsPath = dir.FullName;
			UpdatePaths();
			WithSubDir = withSubDir;
			Name = dir.Name;
		}

		public void UpdatePaths() {
			AlbAbsDir = new DirectoryInfo(CatalogRoot.CatDir.FullName + _albRelPath);
		}

		///<summary> Обновление ссылок элементов альбома на содержащий их альбом. </summary>
		public void UpdateEntCatAlb() {
			foreach (var ent in EntryList) {
				ent.catAlb = this;
			}
		}

		public int GetDuration() {
			if (duration < 0 & EntryList.Count > 0) {
				duration = CatalogEngine.GetDuration(FirstEntPath);
			}
			return duration;
		}

		///<summary> Обновление состава альбома. </summary>
		public void UpdateAlbumFiles() {
			LoadDir();
		}

		///<summary> Формирование элементов альбома по путям. </summary>
		private void LoadDir() {
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



			List<Task> tasksList = new List<Task>();


			foreach (var file in vidList) {
				var newTask = Task.Factory.StartNew(() => {
					// не формируем, если такое было
					lock (locker) if (EntryList.Any(ent => ent.EntAbsPath == file.FullName)) return;

					CatalogEntry newEnt = new CatalogEntry(file, this);

					lock (locker) EntryList.Add(newEnt);
					
				}, TaskCreationOptions.AttachedToParent);
				tasksList.Add(newTask);
			}

			Task.WaitAll(tasksList.ToArray());

			GetDuration();

			// сортируем, т.к. потоки закончились в разнобой
			EntryList.Sort((x, y) => x.Name.CompareTo(y.Name));
		}

		//---

		#region Covers Load

		/// <summary> Формирование обложки альбома на основе первого эпизода. </summary>
		public void LoadAlbumCover() {
			EntryList.First().LoadCover(true);
			CoverImage = EntryList.First().CoverImage;
			vp?.Dispatcher?.Invoke(DispatcherPriority.Render, EmptyDelegate);   // принудительная перерисовка обложки после загрузки
		}

		private static Action EmptyDelegate = delegate () { };

		private Thread updThread;
		/// <summary> Формирование обложек эпизодов альбома в отдельном потоке в паралельном режиме. </summary>
		public void LoadEntCoversThreaded(bool forceUpdate = false) {
			void thread() {
				Console.WriteLine("Load Ent Start");

				Parallel.ForEach(EntryList, new ParallelOptions { MaxDegreeOfParallelism = CatalogEngine.maxThreads },
				  ent => { ent.LoadCover(forceUpdate); }
				  );

				// таски здесь почему то запускаются с лютой задержкой в ~20 сек
				//List<Task> tasksList = new List<Task>();
				//foreach (var ent in EntryList) {
				//	var newTask = Task.Factory.StartNew(() => { ent.LoadCover(forceUpdate); }, TaskCreationOptions.AttachedToParent);
				//	tasksList.Add(newTask);
				//	Console.WriteLine("Add task " + newTask.Id);
				//}
				//Task.WaitAll(tasksList.ToArray());

				Console.WriteLine("Load Ent Done");
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
			if (vp == null) vp = new ViewPlate();
			vp.DataContext = this;
			vp.path = FirstEntPath;
			vp.duration = GetDuration();
			vp.onClick = () => CatalogEngine.MainWin.OpenSidePanel(this);
			vp.onDoubleClick = () => CatalogEngine.MainWin.OpenAlbumTab(this);
			vp.onWheelClick = () => CatalogEngine.MainWin.OpenAlbumTab(this, false);
			TopRightText = ""+EntryList.Count;
			UpdateIconBrokenState();
			return vp;
		}

		///<summary> Возвращает самую раннюю(позднюю) дату создания файла из всех входящих элементов. </summary>
		public override DateTime GetDateCreate(bool byLatest = false) {
			if (byLatest) return EntryList.OrderBy(a => a.DateCreate).Last().DateCreate;
			return EntryList.OrderBy(a => a.DateCreate).First().DateCreate;
		}

		///<summary> Возвращает самую раннюю(позднюю) дату изменения файла из всех входящих элементов. </summary>
		public override DateTime GetDateModify(bool byLatest = false) {
			if (byLatest) return EntryList.OrderBy(a => a.DateModify).Last().DateModify;
			return EntryList.OrderBy(a => a.DateModify).First().DateModify;
		}

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


		public override string ToString() {
			return Name;
		}

	}

}
