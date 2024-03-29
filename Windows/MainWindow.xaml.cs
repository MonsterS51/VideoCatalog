﻿using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Linq;
using VideoCatalog.Main;
using VideoCatalog.Panels;
using VideoCatalog.Tabs;

namespace VideoCatalog.Windows {
	/// <summary>
	/// Логика главного окна каталога. 
	/// </summary>
	public partial class MainWindow : Window {

		public CatalogEngine CatEng;

		public static string catFileExt = ".vcat";

		public MainWindow() {
			InitializeComponent();
			UpdateStartToolbarState();
			clrRecentBtn.Click += (d, y) => {
				Properties.Settings.Default.RecentFolders = "";
				UpdateStartToolbarState();
			};

			//HACK открываем и закрываем невидимое контекстное меню, чтобы принудительно загрузилась NETовская Accessibility.dll
			// иначе происходит ощутимый лаг при первом вызове контекстного меню или попапа - ленивая загрузка это круто (=_= )
			var cm = new ContextMenu();
			cm.Opacity = 0;
			cm.IsOpen = true;
			Application.Current.Dispatcher.BeginInvoke((Action)(() => cm.IsOpen = false));
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
					prevPath = dialog.FileName;
					OpenFolder(new DirectoryInfo(dialog.FileName));
				}
			}
		}

		///<summary> Открытие папки через путь. Имеет проверку на наличие файла каталога и диалог его открытия. </summary>
		public void OpenFolder(DirectoryInfo path) {
			if (!CloseCatalog()) return;    // отменили закрытие

			var vidCat = SearchForVidCatFile(path);
			if (vidCat != null) {
				var result = MessageBox.Show($"In folder found catalog file <{vidCat.Name}>. Open?", "Open catalog", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
				switch (result) {
					case MessageBoxResult.Yes:
						CatEng = new CatalogEngine();
						CatEng.LoadCatalogXML(vidCat.FullName);
						break;
					case MessageBoxResult.No:
						CatEng = new CatalogEngine();
						CatEng.LoadCatalogRoot(path.FullName);
						break;
					case MessageBoxResult.Cancel:
						break;
				}
			} else {
				CatEng = new CatalogEngine();
				CatEng.LoadCatalogRoot(path.FullName);
			}

			SaveRecent(path.FullName);
		}

		///<summary> Ищем файл каталога в директории (предпочтительно с тем же названием). </summary>
		private FileInfo SearchForVidCatFile(DirectoryInfo path) {
			var xmlList = path.EnumerateFiles().Where(f => f.Extension == catFileExt);
			if (xmlList.Count() == 0) return null;

			var exactFile = xmlList.FirstOrDefault(file => file.Name.Replace(file.Extension, "") == path.Name);
			if (exactFile != null) return exactFile;
			else return xmlList.First();
		}

		/// <summary> Сохранение каталога в файл. </summary>
		public void SaveCatalog(object sender, EventArgs e) {
			// предупреждаем о перезаписи не открытого файла каталога
			var vidCat = SearchForVidCatFile(CatalogRoot.CatDir);
			if (!CatalogRoot.useCatFile & vidCat != null) {
				var result = MessageBox.Show($"In folder found catalog file <{vidCat.Name}>, but it was NOT opened!\nDo you really want to OVERWRITE them?",
					"Catalog found", MessageBoxButton.OKCancel, MessageBoxImage.Hand);
				if (result == MessageBoxResult.Cancel) return;
			}

			var sfd = new Microsoft.Win32.SaveFileDialog();
			sfd.InitialDirectory = CatalogRoot.CatDir.FullName;
			sfd.FileName = CatalogRoot.CatDir.Name + catFileExt;
			sfd.Filter = $"vcat files (*{catFileExt})|*{catFileExt}|All files (*.*)|*.*";
			sfd.FilterIndex = 1;
			sfd.RestoreDirectory = true;

			if (sfd.ShowDialog() == true) {
				CatEng.SaveCatalogXML(sfd.FileName);
				CatalogRoot.useCatFile = true;
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
				CatEng = new CatalogEngine();
				CatEng.LoadCatalogXML(ofd.FileName);
				SaveRecent(new FileInfo(ofd.FileName).Directory.FullName);
			}

		}

		/// <summary> Закрытие каталога (с проверкой изменений) и очистка ресурсов. </summary>
		public bool CloseCatalog() {
			if (!CheckDiff()) return false; // отменили закрытие несохраненного каталога

			CatEng?.CatRoot?.StopLoadAlbumesCoversThread();

			var albList = CatEng?.CatRoot?.AlbumsList;

			ForceLinkDestroy();

			CatEng?.CatRoot?.AlbumsList?.Clear();
			CatEng = null;

			if (MainPanel != null) {
				CloseAllTab();
				MainPanel = null;
			}

			GC_Forcer();
			return true;
		}

		///<summary> Запуск обновления открытого каталога. </summary>
		public void UpdateCatalog(object sender, EventArgs e) {
			CatEng?.CatRoot?.LoadRootFolder(CatEng.CatRoot.CatPath);
			GC_Forcer();
		}

		///<summary> Очистка альбома от мертвых элементов. </summary>
		public void CleanCatalog(object sender, EventArgs e) {
			List<AbstractEntry> removeList = new List<AbstractEntry>();

			// находим мертвые элементы
			foreach (var alb in CatEng.CatRoot.AlbumsList) {
				alb.ChkAlbState();
				if (alb.isBroken) {
					foreach (var ent in alb.EntryList) {
						if (ent.isBroken) removeList.Add(ent);
					}
				}
			}

			// спрашиваем и удаляем
			if (removeList.Count > 0) {
				var str = $"Found {removeList.Count} elements:\n" + string.Join(";\n", removeList);
				if (str.Length > 600) str = str.Substring(0, 600) + "...";
				var result = MessageBox.Show(str,
					"Remove broken elements", MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result == MessageBoxResult.Yes) {
					foreach (var removeItem in removeList) {
						RemoveEntry(removeItem);
					}
				}
			}

			// находим пустые альбомы
			removeList.Clear();
			foreach (var alb in CatEng.CatRoot.AlbumsList) {
				alb.ChkAlbState();
				if (alb.EntryList.Count <= 0) removeList.Add(alb); // удаляем пустые альбомы
			}

			// спрашиваем и удаляем
			if (removeList.Count > 0) {
				var str = $"Found {removeList.Count} empty albums:\n" + string.Join(";\n", removeList);
				if (str.Length > 600) str = str.Substring(0, 600) + "...";
				var result = MessageBox.Show(str,
					"Remove empty albums", MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result == MessageBoxResult.Yes) {
					foreach (var removeItem in removeList) {
						RemoveEntry(removeItem);
					}
				}
			}

			ClearSidePanel();
			MainPanel.UpdatePanelContent();
			GC_Forcer();
		}



		///<summary> Магическая чистка, которая позволяет GC почти сразу затирать BitmapImage из неуправляемой памяти. </summary>
		private void ForceLinkDestroy() {
			var albList = CatEng?.CatRoot?.AlbumsList;
			if (albList != null) {
				foreach (var alb in albList) {
					if (alb?.vp != null) {
						BindingOperations.ClearAllBindings(alb.vp.CoverArt);
						alb.vp.DataContext = null;
						alb.vp.CoverArt.Source = null;
						alb.vp.BG.Source = null;
						alb.vp = null;
					}
					alb.StopLoadEntCoversThread();
					alb.CoverImage = null;

					foreach (var ent in alb.EntryList) {
						if (ent?.vp != null) {
							BindingOperations.ClearAllBindings(ent.vp.CoverArt);
							ent.vp.DataContext = null;
							ent.vp.CoverArt.Source = null;
							ent.vp.BG.Source = null;
							ent.vp = null;
						}
						ent.CoverImage = null;
					}
				}
			}
		}

		///<summary> Принудительный запуск GC. </summary>
		public static void GC_Forcer() {
			if (!Properties.Settings.Default.ForceGC) return;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			//HACK - дергаем очистку еще раз через время, чтобы начал убираться наверняка
			Task.Delay(30000).ContinueWith(_ => {
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
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
			string tabName = "" + album.Name;

			// проверяем существование вкладки
			if (tabMap.ContainsKey(tabName)) {
				if (focus) Dispatcher.BeginInvoke((Action)(() => tabMap[tabName].IsSelected = true));
				return;
			}

			// формируем альбомную панель
			AlbumPanel albPanel = new AlbumPanel(album.EntryList);
			albPanel.UpdatePanelContent();
			albPanel.DataContext = album;
			var targetTab = new ClosableTab(tabName, albPanel, CloseTab);

			AddTab(targetTab, tabName, focus);

			// запускаем фоновый генератор обложек для эпизодов в альбоме
			album.RunLoadEntCoversThreaded();
		}

		///<summary> Открытие основной вкладки каталога. </summary>
		public void OpenMainTab() {
			string tabName = "Main";
			if (tabMap.ContainsKey(tabName)) {
				Dispatcher.BeginInvoke((Action)(() => tabMap[tabName].IsSelected = true));
				return;
			}

			MainPanel = new AlbumPanel(CatEng.CatRoot.AlbumsList, true);
			MainPanel.DataContext = CatEng.CatRoot;
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
				CloseCatalog();
				return;
			}

			// чистим, если панель альбома
			if (targetTab.Content is AlbumPanel) {
				AlbumPanel albPanel = targetTab.Content as AlbumPanel;
				albPanel.ClearPanel();
				if (albPanel.DataContext is CatalogAlbum) {
					var album = albPanel.DataContext as CatalogAlbum;
					album?.StopLoadEntCoversThread();
				}
			}

			RemoveTab(targetTab);
		}

		///<summary> Закрытие всех вкладок. </summary>
		public void CloseAllTab() {
			foreach (var targetTab in tabMap.Values.ToArray()) {
				string tabName = tabMap.FirstOrDefault(ent => ent.Value == targetTab).Key;

				// вкладка с панелью альбома
				if (targetTab.Content is AlbumPanel) {
					AlbumPanel albPanel = targetTab.Content as AlbumPanel;
					albPanel.ClearPanel();

					if (albPanel.DataContext is CatalogAlbum) {
						var album = albPanel.DataContext as CatalogAlbum;
						album?.StopLoadEntCoversThread();
					}
				}

				RemoveTab(targetTab);
			}
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

			//HACK принудительно переходим к предыдущему табу, иначе удаление начинает жрать время при переходе к тяжелым вкладкам
			if (targetTab.IsSelected & tabsPanel.SelectedIndex > 0) tabsPanel.SelectedIndex = tabsPanel.SelectedIndex - 1;

			// убираем с панели вкладок
			tabsPanel.Items.Remove(targetTab);

			// убираем из карты вкладок
			if (tabMap.ContainsValue(targetTab)) tabMap.Remove(tabMap.First(ent => ent.Value == targetTab).Key);
			if (tabMap.Count == 0) UpdateStartToolbarState();
			//GC_Forcer();
		}

		///<summary> При манипуляциях с вкладками решаем, показывать ли дефолтное меню. </summary>
		private void UpdateStartToolbarState() {
			if (tabMap.Count == 0) startToolbar.Visibility = Visibility.Visible;
			else startToolbar.Visibility = Visibility.Collapsed;

			// набираем недавние папки
			recentSlot.Children.Clear();
			foreach (var path in Properties.Settings.Default.RecentFolders.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) {
				DirectoryInfo dir = null;
				try {
					dir = new DirectoryInfo(path);
				} catch (Exception ex) {
					Console.WriteLine("UpdateStartToolbarState(): " + ex);
				}

				if (dir != null && dir.Exists) {
					Button dirBtn = new Button();
					dirBtn.Content = "" + path;
					dirBtn.Style = (Style)Application.Current.FindResource(ToolBar.ButtonStyleKey);
					dirBtn.HorizontalAlignment = HorizontalAlignment.Left;
					dirBtn.Click += (e, g) => { OpenFolder(dir); };
					dirBtn.MouseRightButtonDown += (e, g) => {
						Properties.Settings.Default.RecentFolders = Properties.Settings.Default.RecentFolders.Replace(path + ";", "");
						UpdateStartToolbarState();
					};
					recentSlot.Children.Add(dirBtn);
				} else {
					// чистим мертвые пути
					Properties.Settings.Default.RecentFolders = Properties.Settings.Default.RecentFolders.Replace(path + ";", "");
				}
			}

			if (recentSlot.Children.Count <= 0) recentPanel.Visibility = Visibility.Collapsed;
			else recentPanel.Visibility = Visibility.Visible;
		}

		///<summary> Сохранение пути в недавно открытые. </summary>
		private void SaveRecent(string folder) {
			// если был, убираем, чтобы сместить к началу
			if (Properties.Settings.Default.RecentFolders.Contains(folder + ";"))
				Properties.Settings.Default.RecentFolders = Properties.Settings.Default.RecentFolders.Replace(folder + ";", "");
			// добавляем
			Properties.Settings.Default.RecentFolders = folder + ";" + Properties.Settings.Default.RecentFolders;

			var splitFolders = Properties.Settings.Default.RecentFolders.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (splitFolders.Length > 10) {
				Properties.Settings.Default.RecentFolders = string.Join(";", splitFolders.Take(10));
			}
		}


		#endregion

		//---G
		#region SidePanel

		///<summary> Открытие боковой панели для заданного элемента каталога. </summary>
		public void OpenSidePanel(AbstractEntry openerEntry) {
			var albPanel = GetCurrentAlbumePanel();

			// скипаем переоткрытие, иначе вылет если был фокус на датагриде
			if (albPanel.sidePanelSlot.Children.Count > 0) {
				var curASP = albPanel.sidePanelSlot.Children[0] as AlbumSidePanel;
				if (curASP.DataContext == openerEntry) return;
			}

			AlbumSidePanel asp = new AlbumSidePanel();
			asp.DataContext = openerEntry;
			asp.UpdateExceptLbl();
			albPanel.SetSidePanel(asp);

			asp.tabsPanel.SelectedIndex = Properties.Settings.Default.SidePanelTab;
		}

		///<summary> Очистка боковой панели. </summary>
		public void ClearSidePanel() {
			var albPanel = GetCurrentAlbumePanel();
			albPanel.SetSidePanel(null);
		}


		#endregion
		//---R

		///<summary> Получение текущей открытой альбомной панели </summary>
		public AlbumPanel GetCurrentAlbumePanel() {
			var selTab = tabMap.Values.First(tab => tab.IsSelected);
			return selTab.Content as AlbumPanel;
		}

		///<summary> Удаление элемента каталога или альбома. </summary>
		public void RemoveEntry(AbstractEntry ent) {
			Console.WriteLine($"Remove Entry {ent.Name}");
			if (ent is CatalogAlbum) {
				CatEng.CatRoot.AlbumsList.Remove(ent as CatalogAlbum);
			} else {
				(ent as CatalogEntry).catAlb.EntryList.Remove(ent as CatalogEntry);
			}
		}

		///<summary> Удаление элемента каталога или альбома, затем обновление UI. </summary>
		public void RemoveEntryAndUpdateUI(AbstractEntry ent) {
			RemoveEntry(ent);
			App.MainWin.UpdateCurrentPanel();
			App.MainWin.ClearSidePanel();
		}

		///<summary> Обновление плиток текущей панели. </summary>
		public void UpdateCurrentPanel() {
			var albPanel = GetCurrentAlbumePanel();
			albPanel.UpdatePanelContent();
		}

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

		///<summary> Действия по закрытию окна. </summary>
		private void OnClosing(object sender, CancelEventArgs e) {
			if (!CheckDiff()) e.Cancel = true;
		}

		///<summary> Сравнение текущего состояния каталога с каталогом в файле. Выводит диалог при наличии разницы. Возвращает false при отмене действий.</summary>
		private bool CheckDiff() {
			if (CatEng == null) return true;
			if (!CatalogRoot.useCatFile) return true;

			// сравниваем текщее состояние с файлом каталога
			var vidCat = SearchForVidCatFile(CatalogRoot.CatDir);
			if (vidCat == null) return true;

			var curRootXml = Normalize(CatalogEngine.Serialize_YAX(CatEng.CatRoot)).ToString();
			var oldRootXML = Normalize(XDocument.Load(vidCat.FullName).Root).ToString();
			bool isDiff = curRootXml != oldRootXML;
			if (!isDiff) return true;   // нет отличий
			var diffCount = curRootXml.Length - oldRootXML.Length;

			// спрашиваем про сохранение
			MessageBoxResult result = MessageBox.Show($"Catalog was changed!\nSave to <{vidCat.Name}>?", $"Diff = {diffCount}", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
			if (result == MessageBoxResult.Yes) {
				CatEng.SaveCatalogXML(vidCat.FullName);
				return true;
			}

			// отменяем действия
			if (result == MessageBoxResult.Cancel) return false;

			return true;    // выбрали нет, успешно закрываемся
		}

		///<summary> Нормализация XElement. </summary>
		private static XElement Normalize(XElement element) {
			if (element.HasElements) {
				return new XElement(
					element.Name,
					element.Attributes().Where(a => a.Name.Namespace == XNamespace.Xmlns)
					.OrderBy(a => a.Name.ToString()),
					element.Elements().OrderBy(a => a.Name.ToString())
					.Select(e => Normalize(e)));
			}

			if (element.IsEmpty) {
				return new XElement(element.Name, element.Attributes()
					.OrderBy(a => a.Name.ToString()));
			}
			return new XElement(element.Name, element.Attributes()
				.OrderBy(a => a.Name.ToString()), element.Value);
		}


	}
}
