using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VideoCatalog.Main;
using VideoCatalog.Panels;
using VideoCatalog.Windows;

namespace VideoCatalog {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		public CatalogEngine CatEng;


		public MainWindow() {
			InitializeComponent();
			CatEng = new CatalogEngine(this);
			//Console.WriteLine("The number of processors = {0}", Environment.ProcessorCount);

		}


		//---
		#region Catalog Actions

		private string prevPath = "";
		/// <summary> Открытие папки и формирование нового каталога. </summary>
		public void OpenFolder(object sender, RoutedEventArgs e) {
			using (CommonOpenFileDialog dialog = new CommonOpenFileDialog()) {
				if (!string.IsNullOrEmpty(prevPath)) dialog.InitialDirectory = prevPath;   // открываем с последней открытой папки
				dialog.IsFolderPicker = true;
				if (dialog.ShowDialog() == CommonFileDialogResult.Ok && !string.IsNullOrWhiteSpace(dialog.FileName)) {
					CloseCatalog(null, null);
					prevPath = dialog.FileName;
					CatEng.LoadCatalogRoot(dialog.FileName);
				}
			}

			//using (var fbd = new Microsoft.Win32.FolderBrowserDialog()) {
			//	if (!string.IsNullOrEmpty(prevPath)) fbd.SelectedPath = prevPath;	// открываем с последней открытой папки
			//	var result = fbd.ShowDialog();
			//	if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath)) {
			//		CloseCatalog(null, null);
			//		prevPath = fbd.SelectedPath;
			//		CatEng.LoadCatalogRoot(fbd.SelectedPath);
			//	}
			//}
		}

		///<summary> Открытие папки через путь (обычно используется при открытии из проводника). Имеет проверку на наличие файла каталога и диалог его открытия. </summary>
		public void OpenFolder(DirectoryInfo path) {
			CloseCatalog(null, null);

			var xmlList = path.EnumerateFiles().FirstOrDefault(f => f.Extension == ".vcat");

			if (xmlList != null) {
				var result = MessageBox.Show($"В папке найден файл каталога <{xmlList.Name}>. Открыть файл?" , "Открытие каталога", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
				switch (result) {
					case MessageBoxResult.Yes:
						CatEng.LoadCatalogXML(xmlList.FullName);
						break;
					case MessageBoxResult.No:
						CatEng.LoadCatalogRoot(path.FullName);
						break;
					case MessageBoxResult.Cancel:
						break;
				}
			} else {
				CatEng.LoadCatalogRoot(path.FullName);
			}

		}

		/// <summary> Сохранение каталога. </summary>
		public void SaveCatalog(object sender, System.EventArgs e) {
			var sfd = new Microsoft.Win32.SaveFileDialog();
			sfd.InitialDirectory = CatalogRoot.CatDir.FullName;
			sfd.Filter = "vcat files (*.vcat)|*.vcat|All files (*.*)|*.*";
			sfd.FilterIndex = 1;
			sfd.RestoreDirectory = true;

			if (sfd.ShowDialog() == true) {
				CatEng.SaveCatalogXML(sfd.FileName);
			}
		}

		/// <summary> Загрузка каталога из файла сохранения. </summary>
		public void LoadCatalog(object sender, System.EventArgs e) {
			var ofd = new Microsoft.Win32.OpenFileDialog();
			//ofd.InitialDirectory = CatEng.CatDir.FullName;
			ofd.Filter = "vcat files (*.vcat)|*.vcat|All files (*.*)|*.*";
			ofd.FilterIndex = 2;
			ofd.RestoreDirectory = true;

			if (ofd.ShowDialog() == true) {
				CatEng.LoadCatalogXML(ofd.FileName);
			}
		}

		/// <summary> Закрытие каталога и очистка ресурсов. </summary>
		public void CloseCatalog(object sender, System.EventArgs e) {
			CloseAllTab();

			var albList = CatEng?.CatRoot?.AlbumsList;
			if (albList != null) {
				foreach (var alb in albList) {
					alb.CoverImage = null;
					foreach (var ent in alb.EntryList) {
						ent.CoverImage = null;
					}
				}
			}
			CatEng?.CatRoot?.StopLoadAlbumesCoversThread();
			CatEng?.CatRoot?.AlbumsList?.Clear();

			CatEng = null;
			CatEng = new CatalogEngine(this);

			CatalogEngine.MainWin.startToolbar.Visibility = Visibility.Visible;

			GC_Forcer();
		}

		public void UpdateCatalog(object sender, System.EventArgs e) {
			CatEng?.CatRoot?.LoadRootFolder(CatEng.CatRoot.CatPath);
			GC_Forcer();
		}

		private void GC_Forcer() {
			GC.Collect();
			GC.WaitForPendingFinalizers();
			//HACK - дергаем очистку еще раз через время, чтобы начал убираться наверняка
			Task.Delay(20000).ContinueWith(_ => {
				GC.Collect();
				GC.WaitForPendingFinalizers();
			});
		}

		///<summary> Действия по закрытию. </summary>
		private void OnExit(object sender, ExitEventArgs e) {
			Properties.Settings.Default.Save();
			Meta.Vlc.Wpf.ApiManager.ReleaseAll();
		}

		#endregion


		//---G

		#region Tabs

		public AlbumPanel MainPanel;
		private Dictionary<string, TabItem> tabMap = new Dictionary<string, TabItem>(); // хранилище вкладок

		/// <summary> Открытие вкладки альбома. </summary>
		public void OpenAlbumTab(CatalogAlbum album, bool focus = true) {
			Console.WriteLine("Open tab " + album);

			string tabname = "" + album.Name;

			// проверяем существование вкладки
			if (tabMap.ContainsKey(tabname)) {
				if (focus) Dispatcher.BeginInvoke((Action)(() => tabMap[tabname].IsSelected = true));
			} else {
				// формируем альбомную панель
				AlbumPanel albPanel = new AlbumPanel(album.EntryList);
				albPanel.FilterChanged(null, null);

				// обрабатываем вкладку
				var targetTab = new ClosableTab(tabname, albPanel, CloseTab);

				tabsPanel.Items.Add(targetTab);
				tabMap.Add(tabname, targetTab);
				if (focus) Dispatcher.BeginInvoke((Action)(() => targetTab.IsSelected = true));
				// запускаем фоновый генератор обложек для эпизодов в альбоме
				album.LoadEntCoversThreaded();
			}
		}

		public void OpenMainTab() {
			string tabname = "Main";
			if (tabMap.ContainsKey(tabname)) { 
				Dispatcher.BeginInvoke((Action)(() => tabMap[tabname].IsSelected = true)); 
				return; 
			}

			MainPanel = new AlbumPanel(CatEng.CatRoot.AlbumsList, true);

			// обрабатываем вкладку
			var targetTab = new ClosableTab(tabname, MainPanel, CloseTab);
			tabsPanel.Items.Add(targetTab);
			tabMap.Add(tabname, targetTab);
			Dispatcher.BeginInvoke((Action)(() => targetTab.IsSelected = true));
		}



		public void OpenSettingTab(object sender, System.EventArgs e) {
			string tabname = "Settings";
			if (tabMap.ContainsKey(tabname)) {
				Dispatcher.BeginInvoke((Action)(() => tabMap[tabname].IsSelected = true));
				return;
			}
			var setPanel = new SettingsPanel();

			// обрабатываем вкладку
			var targetTab = new ClosableTab(tabname, setPanel, CloseTab);
			tabsPanel.Items.Add(targetTab);
			tabMap.Add(tabname, targetTab);
			Dispatcher.BeginInvoke((Action)(() => targetTab.IsSelected = true));
		}


		public void CloseTab(TabItem targetTab) {
			string tabName = tabMap.FirstOrDefault(ent => ent.Value == targetTab).Key;

			// закрываем каталог при закрытии главной панели
			if (tabName == "Main") {
				tabMap.Remove("Main");
				CloseCatalog(null,null);
			}

			// чистим, если панель альбома
			if (targetTab.Content is AlbumPanel) {
				AlbumPanel albPanel = targetTab.Content as AlbumPanel;
				albPanel.ClearPanel();
			}


			tabsPanel.Items.Remove(targetTab);
			if (tabMap.ContainsValue(targetTab)) {
				var album = CatEng.CatRoot.AlbumsList.Where(alb => alb.Name == tabName).FirstOrDefault();
				album?.StopThread();
				tabMap.Remove(tabName);
			}
		}

		public void CloseAllTab() {
			foreach (var targetTab in tabMap.Values.ToArray()) {
				string tabName = tabMap.FirstOrDefault(ent => ent.Value == targetTab).Key;

				AlbumPanel albPanel = targetTab.Content as AlbumPanel;
				albPanel.ClearPanel();

				tabsPanel.Items.Remove(targetTab);
				if (tabMap.ContainsValue(targetTab)) {
					var album = CatEng.CatRoot.AlbumsList.Where(alb => alb.Name == tabName).FirstOrDefault();
					album?.StopThread();
					tabMap.Remove(tabName);
				}
			}

			MainPanel?.entPlates.Children.Clear();
			MainPanel = null;
		}

		public void FillMainPanelPlates() {
			MainPanel?.FillPlates();
		}

		#endregion

		//---G



		public void OpenSidePanel(AbstractEntry openerEntry) {
			var selTab = tabMap.Values.First(tab => tab.IsSelected);
			AlbumPanel albPanel = selTab.Content as AlbumPanel;

			AlbumSidePanel asp = new AlbumSidePanel();
			asp.DataContext = openerEntry;

			if (openerEntry is CatalogAlbum) {
				asp.removeEntBtn.Click += (o, i) => {
					CatEng.CatRoot.AlbumsList.Remove(openerEntry as CatalogAlbum);
					albPanel.SetSidePanel(null);
					albPanel.FilterChanged(null,null);
				};
			}



			albPanel.SetSidePanel(asp);
		}

		//---R

		private void AddContextActionToReg(object sender, System.EventArgs e) {
			// добавление открытия через контекстное меню папки винды
			string exePath = "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"";

			Microsoft.Win32.RegistryKey classShell2 = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("Classes").OpenSubKey("Directory").OpenSubKey("Background").OpenSubKey("shell", true);
			Microsoft.Win32.RegistryKey rootKey2 = classShell2.CreateSubKey("Open with VidCat");
			rootKey2.SetValue("Icon", $"{exePath},0");
			Microsoft.Win32.RegistryKey comKey2 = rootKey2.CreateSubKey("command");
			comKey2.SetValue(null, $"{exePath} \"%V\"");
			rootKey2.Close();

			Microsoft.Win32.RegistryKey classShell = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("Classes").OpenSubKey("Directory").OpenSubKey("shell", true);
			Microsoft.Win32.RegistryKey rootKey = classShell.CreateSubKey("Open with VidCat");
			rootKey.SetValue("Icon", $"{exePath},0");
			Microsoft.Win32.RegistryKey comKey = rootKey.CreateSubKey("command");
			comKey.SetValue(null, $"{exePath} \"%1\"");
			rootKey.Close();
		}

		public void btnEnterName_Click(object sender, RoutedEventArgs e) {
			InputDialog inputDialog = new InputDialog("Please enter your name:", "John Doe");
			//if (inputDialog.ShowDialog() == true)
			//lblName.Text = inputDialog.Answer;
		}


		//---

		///<summary> Удаление дочерних элементов с UIElement. </summary>
		public static void RemoveChild(DependencyObject parent, UIElement child) {
			var panel = parent as Panel;
			if (panel != null) {
				panel.Children.Remove(child);
				return;
			}

			var decorator = parent as Decorator;
			if (decorator != null) {
				if (decorator.Child == child) {
					decorator.Child = null;
				}
				return;
			}

			var contentPresenter = parent as ContentPresenter;
			if (contentPresenter != null) {
				if (contentPresenter.Content == child) {
					contentPresenter.Content = null;
				}
				return;
			}

			var contentControl = parent as ContentControl;
			if (contentControl != null) {
				if (contentControl.Content == child) {
					contentControl.Content = null;
				}
				return;
			}

			// maybe more
		}

	}
}
