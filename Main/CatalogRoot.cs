using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using VideoCatalog.Windows;
using YAXLib;

namespace VideoCatalog.Main {

	public class CatalogRoot {

		[YAXDontSerialize]
		public static DirectoryInfo CatDir { get; set; }   // например D:\data

		[YAXDontSerialize]
		public string CatPath { get { return CatDir.FullName; } set { CatDir = new DirectoryInfo(value); } }

		public List<CatalogAlbum> AlbumsList { get; set; } = new List<CatalogAlbum>();

		public CatalogRoot() {
		}

		private object locker = new object();

		public static bool useCatFile = false;
		public static AbstractEntry entForDataCopy = null;

		public void LoadRootFolder(string path) {
			CatPath = path;
			Console.WriteLine($"Load Root <{CatDir}>");

			App.MainWin.OpenMainTab();
			App.MainWin.MainPanel.SetUiStateLoading();
			App.MainWin.MainPanel.pBar.IsIndeterminate = false;

			var worker = new BackgroundWorker();
			worker.DoWork += new DoWorkEventHandler(Worker_CreateAlbums);
			worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_CreateAlbumsDone);
			worker.RunWorkerAsync();
		}

		private int procDone = 0;
		private int dirsCount = 1;

		private void Worker_CreateAlbums(object sender, DoWorkEventArgs e) {
			Stopwatch sw = new Stopwatch();
			sw.Start();
			procDone = 0;

			CreateAlbum(CatDir, false);    // рут альбом для файлов в корне

			var dirs = CatDir.GetDirectories();
			dirsCount = dirs.Count() + 1;

			Parallel.ForEach(dirs, new ParallelOptions { MaxDegreeOfParallelism = CatalogEngine.maxThreads },
				dir => { CreateAlbum(dir, true); }
			);

			Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.SetInfoText("Search files done!")));

			Console.WriteLine($"Search files done for {sw.ElapsedMilliseconds} ms");
			sw.Stop();
		}

		private void Worker_CreateAlbumsDone(object sender, RunWorkerCompletedEventArgs e) {
			Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.SetInfoText("Create albums done!")));
			RunLoadAlbumesCoversThread();
			UpdateTagsList();
			UpdateAttributesList();
			ChkAlbAndEntState();
			App.MainWin.MainPanel.SetUiStateOpened();
			App.MainWin.MainPanel.UpdatePanelContent();

			if (App.MainWin?.MainPanel != null) App.MainWin.MainPanel.SetTotalCountText($"Total: {AlbumsList.Count()}");

			Console.WriteLine("Update albumes and entrys DONE !");
		}

		public void LoadDeserial() {
			Console.WriteLine($"Load Root <{CatDir}>");
			App.MainWin.OpenMainTab();
			App.MainWin.MainPanel.SetUiStateLoading();
			//App.MainWin.MainPanel.pBar.IsIndeterminate = true;

			var worker = new BackgroundWorker();
			worker.DoWork += new DoWorkEventHandler(DataRestore);
			worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_CreateAlbumsDone);
			worker.RunWorkerAsync();
		}

		///<summary> Восстановленние элементов каталога. </summary>
		public void DataRestore(object sender, DoWorkEventArgs e) {
			int restCount = 0;
			int restAlbCount = 0;

			dirsCount = AlbumsList.Count();

			Stopwatch sw = new Stopwatch();
			sw.Start();

			Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.SetInfoText("Restore data")));

			foreach (var alb in AlbumsList) {
				alb.UpdateEntCatAlb();
				Parallel.ForEach(alb.EntryList, new ParallelOptions { MaxDegreeOfParallelism = CatalogEngine.maxThreads },
					ent => {
						ent.GetMetaData();
						Interlocked.Increment(ref restCount);
						Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.pBar.Value = (restAlbCount / (float)dirsCount) * 100));
						Application.Current.Dispatcher.BeginInvoke((Action)(() =>
							App.MainWin.MainPanel.SetInfoText($"Restore data =>   Alb: {restAlbCount}/{dirsCount}   Ent: {restCount}")));
					}
				);
				Interlocked.Increment(ref restAlbCount);
			}

			Console.WriteLine($"Restore data done for {sw.ElapsedMilliseconds} ms");
			sw.Stop();
		}

		///<summary> Попытка создать альбом или обновить его состав. </summary>
		private void CreateAlbum(DirectoryInfo path, bool withSubDir) {
			// проверка наличия альбома по этому пути
			lock (locker) {
				if (AlbumsList.Any(alb => alb.AlbAbsDir.FullName == path.FullName)) {
					// обновляем старый
					CatalogAlbum oldAlbume = AlbumsList.First(alb => alb.AlbAbsDir.FullName == path.FullName);
					oldAlbume.LoadDir();
					Interlocked.Increment(ref procDone);
					if (App.MainWin?.MainPanel != null) {
						Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.pBar.Value = (procDone / (float)dirsCount) * 100));
						Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.SetInfoText($"({procDone} / {dirsCount})  {path}")));
					}
					return;
				}
			}

			// создаем новый
			//Console.WriteLine($"New album <{path}>");
			CatalogAlbum newAlbume = new CatalogAlbum(path, withSubDir);
			newAlbume.LoadDir();
			if (newAlbume.EntryList.Count > 0) {
				lock (locker) {
					AlbumsList.Add(newAlbume);
				}
			}

			Interlocked.Increment(ref procDone);
			if (App.MainWin?.MainPanel != null) {
				Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.pBar.Value = (procDone / (float)dirsCount) * 100));
				Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.SetInfoText($"({procDone} / {dirsCount})  {path}")));
			}
		}

		///<summary> Проверяем состояние альбомов и их содержимое. </summary>
		public void ChkAlbAndEntState(object sender = null, RoutedEventArgs e = null) {
			if (AlbumsList.Count <= 0) return;

			// удаляем все альбомы, пути которых перестали существовать или в них больше нет видеофайлов
			foreach (var alb in AlbumsList) {
				alb.ChkAlbState();
			}
		}

		//---

		#region Albums Covers Load

		private CancellationTokenSource cts = null;

		///<summary> Запуск загрузки обложек только альбомов параллельно в отдельном потоке. </summary>
		public void RunLoadAlbumesCoversThread() {
			StopLoadAlbumesCoversThread();
			cts = new CancellationTokenSource();

			var po = new ParallelOptions();
			po.CancellationToken = cts.Token;
			po.MaxDegreeOfParallelism = CatalogEngine.maxThreads;

			Task.Run(() => {
				try {
					Parallel.ForEach(AlbumsList, po, (ca) => ca.LoadAlbumCover());
				} catch (OperationCanceledException) {
					Console.WriteLine("Cancel LoadAlbumesCovers");
				} finally {
					cts.Dispose();
					cts = null;
				}
			});
		}

		/// <summary> Принудительная остановка потока загрузки обложек. </summary>
		public void StopLoadAlbumesCoversThread() {
			if (cts != null) cts.Cancel();
		}

		#endregion Albums Covers Load

		#region Tags & Attributes

		//---

		public static List<string> tagsList = new List<string>();
		private static Dictionary<string, SolidColorBrush> tagsColors = new Dictionary<string, SolidColorBrush>();

		///<summary> Обновление списка всех тегов, использованных в альбомах. </summary>
		public void UpdateTagsList() {
			tagsList.Clear();
			foreach (var alb in AlbumsList) {
				// тэги самого альбома
				foreach (var tag in alb.GetTagList()) {
					if (!tagsList.Contains(tag)) tagsList.Add(tag);
				}

				// тэги элементов альбома
				foreach (var ent in alb.EntryList) {
					foreach (var tag in ent.GetTagList()) {
						if (!tagsList.Contains(tag)) tagsList.Add(tag);
					}
				}
			}
			tagsList.Sort();
		}

		private static Random rnd = new Random();

		///<summary> Получить цвет для тэга. </summary>
		public static SolidColorBrush GetTagColor(string tag) {
			if (tagsColors.ContainsKey(tag)) return tagsColors[tag];

			Color bgCol = Color.FromRgb((byte)rnd.Next(1, 255), (byte)rnd.Next(1, 255), (byte)rnd.Next(1, 233));
			var scb = new SolidColorBrush(bgCol);
			tagsColors.Add(tag, scb);
			return scb;
		}

		//---

		public static List<string> atrList = new List<string>();

		///<summary> Обновление списка всех тегов, использованных в альбомах. </summary>
		public void UpdateAttributesList() {
			atrList.Clear();

			// сразу берем настроенные для отображения в списочном режиме
			atrList.AddRange(Properties.Settings.Default.AtrToShowList.Split(';', ','));

			foreach (var alb in AlbumsList) {
				// атрибуты самого альбома
				foreach (var atrEnt in alb.AtrMap) {
					if (!atrList.Contains(atrEnt.AtrName)) atrList.Add(atrEnt.AtrName);
				}

				// атрибуты элементов альбома
				foreach (var ent in alb.EntryList) {
					foreach (var atrEnt in ent.AtrMap) {
						if (!atrList.Contains(atrEnt.AtrName)) atrList.Add(atrEnt.AtrName);
					}
				}
			}
			atrList.Sort();
		}

		#endregion Tags & Attributes
	}
}