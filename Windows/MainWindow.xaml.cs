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
using VideoCatalog.Tabs;
using VideoCatalog.Windows;

namespace VideoCatalog.Windows {
	/// <summary>
	/// Логика главного окна каталога. 
	/// </summary>
	public partial class MainWindow : Window {

		public CatalogEngine CatEng;

		public static string catFileExt = ".vcat";

		public MainWindow() {
			InitializeComponent();
			CatEng = new CatalogEngine(this);
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
		}

		///<summary> Открытие папки через путь (обычно используется при открытии из проводника). Имеет проверку на наличие файла каталога и диалог его открытия. </summary>
		public void OpenFolder(DirectoryInfo path) {
			CloseCatalog(null, null);

			var xmlList = path.EnumerateFiles().FirstOrDefault(f => f.Extension == catFileExt);

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

		/// <summary> Сохранение каталога в файл. </summary>
		public void SaveCatalog(object sender, EventArgs e) {
			var sfd = new Microsoft.Win32.SaveFileDialog();
			sfd.InitialDirectory = CatalogRoot.CatDir.FullName;
			sfd.Filter = $"vcat files (*{catFileExt})|*{catFileExt}|All files (*.*)|*.*";
			sfd.FilterIndex = 1;
			sfd.RestoreDirectory = true;

			if (sfd.ShowDialog() == true) {
				CatEng.SaveCatalogXML(sfd.FileName);
			}
		}

		/// <summary> Загрузка каталога из файла сохранения. </summary>
		public void LoadCatalog(object sender, EventArgs e) {
			var ofd = new Microsoft.Win32.OpenFileDialog();
			//ofd.InitialDirectory = CatEng.CatDir.FullName;
			ofd.Filter = $"vcat files (*{catFileExt})|*{catFileExt}|All files (*.*)|*.*";
			ofd.FilterIndex = 2;
			ofd.RestoreDirectory = true;

			if (ofd.ShowDialog() == true) {
				CatEng.LoadCatalogXML(ofd.FileName);
			}
		}

		/// <summary> Закрытие каталога и очистка ресурсов. </summary>
		public void CloseCatalog(object sender, EventArgs e) {
			CloseAllTab();

			CatEng?.CatRoot?.StopLoadAlbumesCoversThread();

			var albList = CatEng?.CatRoot?.AlbumsList;

			if (albList != null) {
				foreach (var alb in albList) {
					alb.StopThread();
					alb.CoverImage = null;
					alb.vp = null;
					foreach (var ent in alb.EntryList) {
						ent.CoverImage = null;
						ent.vp = null;
					}
				}
			}
			CatEng?.CatRoot?.AlbumsList?.Clear();

			CatEng = null;
			CatEng = new CatalogEngine(this);

			GC_Forcer();
		}

		///<summary> Запуск обновления открытого каталога. </summary>
		public void UpdateCatalog(object sender, EventArgs e) {
			CatEng?.CatRoot?.LoadRootFolder(CatEng.CatRoot.CatPath);
			GC_Forcer();
		}

		///<summary> Принудительный запуск GC. </summary>
		private void GC_Forcer() {
			GC.Collect();
			GC.WaitForPendingFinalizers();
			//HACK - дергаем очистку еще раз через время, чтобы начал убираться наверняка
			Task.Delay(20000).ContinueWith(_ => {
				GC.Collect();
				GC.WaitForPendingFinalizers();
			});
		}

		#endregion

		//---G

		#region Tabs

		public AlbumPanel MainPanel;

		///<summary> Карта для удобного учета вкладок по их имени. </summary>
		private Dictionary<string, TabItem> tabMap = new Dictionary<string, TabItem>(); // хранилище вкладок

		/// <summary> Открытие вкладки альбома. </summary>
		public void OpenAlbumTab(CatalogAlbum album, bool focus = true) {
			Console.WriteLine("Open tab " + album);
			string tabName = "" + album.Name;

			// проверяем существование вкладки
			if (tabMap.ContainsKey(tabName)) {
				if (focus) Dispatcher.BeginInvoke((Action)(() => tabMap[tabName].IsSelected = true));
				return;
			} 

			// формируем альбомную панель
			AlbumPanel albPanel = new AlbumPanel(album.EntryList);
			albPanel.UpdatePanelContent();
			var targetTab = new ClosableTab(tabName, albPanel, CloseTab);

			AddTab(targetTab, tabName, focus);

			// запускаем фоновый генератор обложек для эпизодов в альбоме
			album.LoadEntCoversThreaded();		
		}

		///<summary> Открытие основной вкладки каталога. </summary>
		public void OpenMainTab() {
			string tabName = "Main";
			if (tabMap.ContainsKey(tabName)) { 
				Dispatcher.BeginInvoke((Action)(() => tabMap[tabName].IsSelected = true)); 
				return; 
			}

			MainPanel = new AlbumPanel(CatEng.CatRoot.AlbumsList, true);
			var targetTab = new ClosableTab(tabName, MainPanel, CloseTab);

			AddTab(targetTab, tabName);
		}


		///<summary> Открытие вкладки настроек. </summary>
		public void OpenSettingTab(object sender, EventArgs e) {
			string tabName = "Settings";
			if (tabMap.ContainsKey(tabName)) {
				Dispatcher.BeginInvoke((Action)(() => tabMap[tabName].IsSelected = true));
				return;
			}

			var setPanel = new SettingsPanel();
			var targetTab = new ClosableTab(tabName, setPanel, CloseTab);

			AddTab(targetTab, tabName);
		}

		///<summary> Закрытие заданной вкладки. </summary>
		public void CloseTab(TabItem targetTab) {
			string tabName = tabMap.FirstOrDefault(ent => ent.Value == targetTab).Key;

			// закрываем каталог при закрытии главной панели
			if (tabName == "Main") {
				CloseCatalog(null,null);
				return;
			}

			// чистим, если панель альбома
			if (targetTab.Content is AlbumPanel) {
				AlbumPanel albPanel = targetTab.Content as AlbumPanel;
				albPanel.ClearPanel();
				var album = CatEng.CatRoot.AlbumsList.Where(alb => alb.Name == tabName).FirstOrDefault();
				album?.StopThread();
			}

			RemoveTab(targetTab);
		}

		///<summary> Закрытие всех вкладок. </summary>
		public void CloseAllTab() {
			foreach (var targetTab in tabMap.Values.ToArray()) {
				string tabName = tabMap.FirstOrDefault(ent => ent.Value == targetTab).Key;

				AlbumPanel albPanel = targetTab.Content as AlbumPanel;
				albPanel.ClearPanel();
				var album = CatEng.CatRoot.AlbumsList.Where(alb => alb.Name == tabName).FirstOrDefault();
				album?.StopThread();

				RemoveTab(targetTab);
			}

			//MainPanel?.entPlates.Children.Clear();
			MainPanel = null;
		}

		//---

		///<summary> Добавление и регистрация созданной вкладки. </summary>
		private void AddTab(TabItem targetTab, string tabName, bool focus = true) {
			if (targetTab == null | tabName == null) return;

			tabsPanel.Items.Add(targetTab);
			tabMap.Add(tabName, targetTab);
			if (focus) Dispatcher.BeginInvoke((Action)(() => targetTab.IsSelected = true));

			UpdateStartToolbarState();
		}


		///<summary> Удаление и разрегистрация существующей вкладки. </summary>
		private void RemoveTab(TabItem targetTab) {
			if (targetTab == null) return;

			// убираем с панели вкладок
			tabsPanel.Items.Remove(targetTab);

			// убираем из карты вкладок
			if (tabMap.ContainsValue(targetTab)) {
				tabMap.Remove(tabMap.First(ent => ent.Value == targetTab).Key);
			}

			UpdateStartToolbarState();
		}

		///<summary> При манипуляциях с вкладками решаем, показывать ли дефолтное меню. </summary>
		private void UpdateStartToolbarState() {
			Console.WriteLine(""+ tabMap.Count);
			if (tabMap.Count == 0) startToolbar.Visibility = Visibility.Visible;
			else startToolbar.Visibility = Visibility.Collapsed;
		}

		#endregion

		//---G


		///<summary> Открытие боковой панели для заданного элемента каталога. </summary>
		public void OpenSidePanel(AbstractEntry openerEntry) {
			var selTab = tabMap.Values.First(tab => tab.IsSelected);
			AlbumPanel albPanel = selTab.Content as AlbumPanel;

			AlbumSidePanel asp = new AlbumSidePanel();
			asp.DataContext = openerEntry;

			if (openerEntry is CatalogAlbum) {
				// обработка кнопки удаления альбома из каталога
				asp.removeEntBtn.Click += (o, i) => {
					CatEng.CatRoot.AlbumsList.Remove(openerEntry as CatalogAlbum);
					albPanel.SetSidePanel(null);
					albPanel.UpdatePanelContent();
				};
			}

			albPanel.SetSidePanel(asp);
		}

		//---R

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
