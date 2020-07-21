using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using VideoCatalog.Windows;
using YAXLib;

namespace VideoCatalog.Main {
	public class CatalogRoot {
		[YAXDontSerialize]
		public static DirectoryInfo CatDir { get; set; }   // например D:\data
		[YAXDontSerialize]
		public string CatPath { get { return CatDir.FullName; } set { CatDir = new DirectoryInfo(value); } }
		public List<CatalogAlbum> AlbumsList { get; set; } = new List<CatalogAlbum>();

		private BackgroundWorker worker;    // воркер для асинхронной загрузки альбомов



		public CatalogRoot() { }

		private object locker = new object();



		public void LoadRootFolder(string path) {
			CatPath = path;
			Console.WriteLine($"Load Root <{CatDir}>");

			App.MainWin.OpenMainTab();
			App.MainWin.MainPanel.SetUiStateLoading();
			App.MainWin.MainPanel.pBar.IsIndeterminate = false;

			worker = new BackgroundWorker();
			worker.DoWork += new DoWorkEventHandler(Worker_CreateAlbums);
			worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_CreateAlbumsDone);
			worker.RunWorkerAsync();
		}

		private int procDone = 0;
		private int dirsCount = 1;

		void Worker_CreateAlbums(object sender, DoWorkEventArgs e) {
			Stopwatch sw = new Stopwatch();
			sw.Start();
			procDone = 0;

			CreateAlbum(CatDir, false);    // рут альбом для файлов в корне

			var dirs = CatDir.GetDirectories();
			dirsCount = dirs.Count() + 1;

			Parallel.ForEach(dirs, new ParallelOptions { MaxDegreeOfParallelism = CatalogEngine.maxThreads },
				dir => { CreateAlbum(dir, true); }
			);

			//procDone = 0;
			//Parallel.ForEach(AlbumsList, new ParallelOptions { MaxDegreeOfParallelism = CatalogEngine.maxThreads },
			//	alb => {
			//		Parallel.ForEach(alb.EntryList, new ParallelOptions { MaxDegreeOfParallelism = CatalogEngine.maxThreads },
			//			ent => { ent.GetMetaData(); }
			//		);

			//		Interlocked.Increment(ref procDone);
			//		if (App.MainWindow?.MainPanel != null) {
			//			Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWindow.MainPanel.pBar.Value = (procDone / (float)AlbumsList.Count) * 100));
			//			Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWindow.MainPanel.infoText.Text = $"({procDone} / {AlbumsList.Count})  {alb.AlbRelPath}"));
			//		}
			//	}
			//);

			Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.infoText.Text = "Search files done!"));

			Console.WriteLine($"Search files done for {sw.ElapsedMilliseconds}");
			sw.Stop();
		}

		void Worker_CreateAlbumsDone(object sender, RunWorkerCompletedEventArgs e) {
			Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.infoText.Text = "Done!"));
			RunLoadAlbumesCoversThread();
			UpdateTagsList();
			UpdateAttributesList();
			ChkAlbAndEntState();
			App.MainWin.MainPanel.UpdatePanelContent();
			App.MainWin.MainPanel.loadingPanel.Visibility = Visibility.Hidden;
			App.MainWin.MainPanel.SetUiStateOpened();
		}


		public void LoadDeserial() {
			Console.WriteLine($"Load Root <{CatDir}>");

			App.MainWin.OpenMainTab();
			App.MainWin.MainPanel.SetUiStateLoading();
			App.MainWin.MainPanel.pBar.IsIndeterminate = true;

			worker = new BackgroundWorker();
			worker.DoWork += new DoWorkEventHandler(DataRestore);
			worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_CreateAlbumsDone);
			worker.RunWorkerAsync();
		}

		private int restCount = 0;
		///<summary> Восстановленние элементов каталога. </summary>
		public void DataRestore(object sender, DoWorkEventArgs e) {
			Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.infoText.Text = "Restore data"));


			//! ! протестировать
			foreach (var alb in AlbumsList) {
				alb.UpdatePaths();
				alb.UpdateEntCatAlb();
				Parallel.ForEach(alb.EntryList, new ParallelOptions { MaxDegreeOfParallelism = CatalogEngine.maxThreads },
					ent => {
						ent.UpdatePaths();
						ent.GetMetaData();
						Interlocked.Increment(ref restCount);
						Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.infoText.Text = $"Restore data ({restCount})"));
					}
				);
			}


			//List<Task> tasksList = new List<Task>();
			//foreach (var alb in AlbumsList) {
			//	alb.UpdatePaths();
			//	alb.UpdateEntCatAlb();
			//	foreach (var ent in alb.EntryList) {
			//		var newTask = Task.Factory.StartNew(() => {
			//			ent.UpdatePaths();
			//			ent.GetMetaData();
			//			Interlocked.Increment(ref restCount);
			//			Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWindow.MainPanel.infoText.Text = $"Restore data ({restCount})"));
			//		}, TaskCreationOptions.LongRunning);
			//		tasksList.Add(newTask);
			//	}
			//}
			//Task.WaitAll(tasksList.ToArray());

			Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.infoText.Text = $"Restored {restCount} entry. Generate plates."));

		}

		///<summary> Попытка создать альбом или обновить его состав. </summary>
		private void CreateAlbum(DirectoryInfo path, bool withSubDir) {
			// проверка наличия альбома по этому пути
			lock (locker) {
				if (AlbumsList.Any(alb => alb.AlbAbsPath == path.FullName)) {
					// обновляем старый
					CatalogAlbum oldAlbume = AlbumsList.First(alb => alb.AlbAbsPath == path.FullName);
					oldAlbume.UpdateAlbumFiles();
					Interlocked.Increment(ref procDone);
					if (App.MainWin?.MainPanel != null) {
						Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.pBar.Value = (procDone / (float)dirsCount) * 100));
						Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.infoText.Text = $"({procDone} / {dirsCount})  {path}"));
					}
					return;
				}
			}

			// создаем новый
			//Console.WriteLine($"New album <{path}>");
			CatalogAlbum newAlbume = new CatalogAlbum(path, withSubDir);
			newAlbume.UpdateAlbumFiles();
			if (newAlbume.EntryList.Count > 0) {
				lock (locker) {
					AlbumsList.Add(newAlbume);
				}
			}

			Interlocked.Increment(ref procDone);
			if (App.MainWin?.MainPanel != null) {
				Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.pBar.Value = (procDone / (float)dirsCount) * 100));
				Application.Current.Dispatcher.BeginInvoke((Action)(() => App.MainWin.MainPanel.infoText.Text = $"({procDone} / {dirsCount})  {path}"));
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

		private Thread coverLoadThread;
		///<summary> Запуск загрузки обложек только альбомов параллельно в отдельном потоке. </summary>
		public void RunLoadAlbumesCoversThread() {
			StopLoadAlbumesCoversThread();

			void thread() {
				Parallel.ForEach(AlbumsList, new ParallelOptions { MaxDegreeOfParallelism = CatalogEngine.maxThreads },
				  ca => ca.LoadAlbumCover()
				  );
			}
			
			coverLoadThread = new Thread(thread);
			coverLoadThread.Start();		
		}

		/// <summary> Принудительная остановка потока загрузки обложек. </summary>
		public void StopLoadAlbumesCoversThread() {
			if (coverLoadThread != null) {
				coverLoadThread.Abort();
			}
		}
		#endregion


		#region Tags & Attributes

		//---

		public static List<string> tagsList = new List<string>();

		///<summary> Обновление списка всех тегов, использованных в альбомах. </summary>
		public void UpdateTagsList() {
			tagsList.Clear();
			foreach (var alb in AlbumsList) {
				// тэги самого альбома
				foreach (var tag in alb.TagList) {
					if (!tagsList.Contains(tag)) tagsList.Add(tag);
				}

				// тэги элементов альбома
				foreach (var ent in alb.EntryList) {
					foreach (var tag in ent.TagList) {
						if (!tagsList.Contains(tag)) tagsList.Add(tag);
					}
				}
			}
		}

		//---

		public static List<string> atrList = new List<string>();

		///<summary> Обновление списка всех тегов, использованных в альбомах. </summary>
		public void UpdateAttributesList() {
			atrList.Clear();
			foreach (var alb in AlbumsList) {
				// атрибуты самого альбома
				foreach (var atrEnt in alb.atrMap) {
					if (!atrList.Contains(atrEnt.AtrName)) atrList.Add(atrEnt.AtrName);
				}

				// атрибуты элементов альбома
				foreach (var ent in alb.EntryList) {
					foreach (var atrEnt in ent.atrMap) {
						if (!atrList.Contains(atrEnt.AtrName)) atrList.Add(atrEnt.AtrName);
					}
				}
			}
		}



		#endregion

	}
}
