using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VideoCatalog.Main;

namespace VideoCatalog.Windows {
	/// <summary>
	/// Interaction logic for AlbumPanel.xaml
	/// </summary>
	public partial class AlbumPanel : UserControl {

		private FilterSorterModule fsm;
		public IEnumerable<AbstractEntry> baseList;
		public IEnumerable<AbstractEntry> srcList = new List<AbstractEntry>();   // отфильтрованные альбомы

		private bool isRoot = false;

		public AlbumPanel() {
			InitializeComponent();
			Btn_SidePanelSwitch(null, null);
			LoadSettings();
		}

		public AlbumPanel(IEnumerable<AbstractEntry> BaseList, bool isRoot = false) {
			InitializeComponent();
			this.isRoot = isRoot;

			if (isRoot) {
				toolbarMainPanel.Visibility = Visibility.Visible;
				toolbarMainPanel.newBtn.Click += CatalogEngine.MainWin.OpenFolder;
				toolbarMainPanel.loadBtn.Click += CatalogEngine.MainWin.LoadCatalog;
				toolbarMainPanel.saveBtn.Click += CatalogEngine.MainWin.SaveCatalog;
				toolbarMainPanel.closeBtn.Click += CatalogEngine.MainWin.CloseCatalog;
				toolbarMainPanel.updBtn.Click += CatalogEngine.MainWin.UpdateCatalog;
				toolbarMainPanel.chkBtn.Click += CatalogEngine.MainWin.CatEng.CatRoot.ChkAlbAndEntState;
				toolbarMainPanel.settingBtn.Click += CatalogEngine.MainWin.OpenSettingTab;
			} else {
				toolbarMainPanel.Visibility = Visibility.Collapsed;
			}

			baseList = BaseList;
			fsm = new FilterSorterModule(baseList);
			Btn_SidePanelSwitch(null, null);
			LoadSettings();
		}

		/// <summary> Очистка панели (для повторного использования плашек). </summary>
		public void ClearPanel() {
			foreach (var panelEnt in entPlates.Children) {
				if (panelEnt is UniformGrid) {
					var grid = panelEnt as UniformGrid;
					grid.Children.Clear();
				}
			}
			entPlates.Children.Clear();
		}


		///<summary> Обработка изменения скролла с панелями альбомов. </summary>
		private void Scroll_ValueChanged(object sender, EventArgs e) {
			// находим первую видимую плашку альбома
			AbstractEntry ent = srcList?.FirstOrDefault(alb => IsUserVisible(alb.vp, scrollViewer));
			if (ent != null) {
				// в зависимости от режима сортировки выводим справочную надпись
				switch (mode) {
					case FilterSorterModule.SortMode.NAME: {
						scrollHelperLbl.Text = "" + ent.Name.First();
						break;
					}
					case FilterSorterModule.SortMode.CREATE_DATE: {
						scrollHelperLbl.Text = "" + ent.GetDateCreate().ToString("dd/MM/yyyy");
						break;
					}
					case FilterSorterModule.SortMode.MODIF_DATE_FILE: {
						scrollHelperLbl.Text = "" + ent.GetDateModify().ToString("dd/MM/yyyy");
						break;
					}
					case FilterSorterModule.SortMode.CREATE_DATE_FILE: {
						scrollHelperLbl.Text = "" + ent.GetDateCreate().ToString("dd/MM/yyyy");
						break;
					}
					default: {
						break;
					}
				}

				if (Mouse.LeftButton == MouseButtonState.Pressed) {
					scrollHelperLbl.Visibility = Visibility.Visible;
				} else {
					scrollHelperLbl.Visibility = Visibility.Hidden;
				}

			}
		}


		private void Scroll_MouseUp(object sender, EventArgs e) {
			scrollHelperLbl.Visibility = Visibility.Hidden;
		}


		///<summary> 
		/// Проверка видимости элемента внутри контейнера.
		/// https://stackoverflow.com/questions/19397780/scrollviewer-indication-of-child-element-scrolled-into-view 
		///</summary>
		private bool IsUserVisible(FrameworkElement element, FrameworkElement container) {
			if (element == null) return false;
			if (!element.IsVisible) return false;

			Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
			Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
			return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);  // видене ЛВ или ПН угол элемента
		}

		/// <summary> Изменение слайдера размера плашек. </summary>
		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			if (entPlates != null) {
				foreach (var item in entPlates.Children) {
					if (item is UniformGrid) {
						var grid = item as UniformGrid;
						grid.Columns = (int)e.NewValue;
						SaveSettings();
					}
				}
			}

		}

		private FilterSorterModule.SortMode mode;
		///<summary> Обновление после изменения одного из контролов фильтра. </summary>
		public void FilterChanged(object sender, RoutedEventArgs e) {
			switch (filterPanel.sortMode.SelectedIndex) {
				case 0: {
					mode = FilterSorterModule.SortMode.NAME;
					break;
				}
				case 1: {
					mode = FilterSorterModule.SortMode.CREATE_DATE;
					break;
				}
				case 2: {
					mode = FilterSorterModule.SortMode.CREATE_DATE_FILE;
					break;
				}
				case 3: {
					mode = FilterSorterModule.SortMode.MODIF_DATE_FILE;
					break;
				}
				default: {
					mode = FilterSorterModule.SortMode.NAME;
					break;
				}
			}
			UpdateSrcList(filterPanel.filterBox.Text, mode, filterPanel.ascendChkBox.IsChecked ?? false, filterPanel.brokenChkBox.IsChecked ?? false);
			FillPlates();

		}

		///<summary> Прогон всех альбомов через фильтр и обновление конечного списка. </summary>
		public void UpdateSrcList(string str, FilterSorterModule.SortMode sortMode = FilterSorterModule.SortMode.NAME, bool ascend = true, bool broken = false) {
			Stopwatch sw = new Stopwatch();
			sw.Start();
			srcList = fsm.FilterByName(str, CatalogEngine.MainWin?.CatEng?.CatRoot?.tagsList, sortMode, ascend, broken);
			FillPlates();
			Console.WriteLine($"FillPlates {sw.ElapsedMilliseconds}ms");
			sw.Stop();
		}


		/// <summary> Перезаполнение плитками альбомов панели. </summary>
		public void FillPlates() {
			foreach (var panelEnt in entPlates.Children) {
				if (panelEnt is UniformGrid) {
					var grid = panelEnt as UniformGrid;
					grid.Children.Clear();
				}
			}
			entPlates.Children.Clear();

			if (srcList.FirstOrDefault() is CatalogAlbum) {
				//+ режим альбомов
				UniformGrid newGrid = new UniformGrid();
				newGrid.VerticalAlignment = VerticalAlignment.Top;
				newGrid.Columns = (int)sliderGridCol.Value;
				foreach (var ent in srcList) {
					ent.CreatePlate();
					if (ent.vp.Parent != null) MainWindow.RemoveChild(ent.vp.Parent, ent.vp);
					newGrid.Children.Add(ent.vp);
				}
				entPlates.Children.Add(newGrid);
			} else {
				//+ режим элементов

				// разбиваем на подпапки
				var subFoldersMap = new SortedDictionary<string, List<string>>();
				foreach (var ent in srcList) {
					var catEnt = ent as CatalogEntry;

					if (!subFoldersMap.ContainsKey(catEnt.EntAbsFile.Directory.FullName)) {
						// создаем новую подпапку
						subFoldersMap.Add(catEnt.EntAbsFile.Directory.FullName, new List<string>());
					}
					subFoldersMap[catEnt.EntAbsFile.Directory.FullName].Add(catEnt.EntAbsFile.FullName);
				}

				// заполняем в соотвествии с подпапками
				foreach (var dirEnt in subFoldersMap) {
					// разделитель с названием подпапки, если их больше одной
					if (subFoldersMap.Count > 1) {
						StackPanel newSeparatorPanel = new StackPanel();
						newSeparatorPanel.Orientation = Orientation.Horizontal;
						newSeparatorPanel.Margin = new Thickness(5, 15, 5, 5);
						TextBlock subHeader = new TextBlock();
						subHeader.FontSize = 16;
						subHeader.Foreground = SystemColors.WindowTextBrush;
						subHeader.Text = new DirectoryInfo(dirEnt.Key).Name + "  ";
						Separator line = new Separator();
						line.HorizontalAlignment = HorizontalAlignment.Stretch;
						line.Width = 4000;
						newSeparatorPanel.Children.Add(subHeader);
						newSeparatorPanel.Children.Add(line);
						entPlates.Children.Add(newSeparatorPanel);
					}


					UniformGrid newGrid = new UniformGrid();
					newGrid.VerticalAlignment = VerticalAlignment.Top;
					newGrid.Columns = (int) sliderGridCol.Value;
					foreach (var entPath in dirEnt.Value) {
						var catEnt = srcList.Where(ent => (ent as CatalogEntry).EntAbsPath == entPath).FirstOrDefault();
						catEnt.CreatePlate();
						if (catEnt.vp.Parent != null) MainWindow.RemoveChild(catEnt.vp.Parent, catEnt.vp);
						newGrid.Children.Add(catEnt.vp);
					}
					entPlates.Children.Add(newGrid);
				}
			}

			Application.Current.Dispatcher.BeginInvoke((Action)(() => CatalogEngine.MainWin.MainPanel.lblCountTotal.Text = "Total: " + srcList.Count()));
		}

		//---
		#region UI States
		public void SetUiStateClosed() {
			toolbarMainPanel.loadBtn.IsEnabled = true;
			toolbarMainPanel.saveBtn.IsEnabled = false;
			toolbarMainPanel.updBtn.IsEnabled = false;
			toolbarMainPanel.closeBtn.IsEnabled = false;
			filterPanel.IsEnabled = false;
			scrollHelperLbl.Visibility = Visibility.Hidden;
			filterPanel.filterBox.Text = "";
			infoText.Text = "";
			lblCountTotal.Text = "";
			loadingPanel.Visibility = Visibility.Hidden;
		}

		public void SetUiStateLoading() {
			loadingPanel.Visibility = Visibility.Visible;
			pBar.Value = 0;

			toolbarMainPanel.loadBtn.IsEnabled = false;
			toolbarMainPanel.saveBtn.IsEnabled = false;
			toolbarMainPanel.updBtn.IsEnabled = false;
			toolbarMainPanel.closeBtn.IsEnabled = true;
			filterPanel.IsEnabled = false;
			scrollHelperLbl.Visibility = Visibility.Hidden;
			filterPanel.filterBox.Text = "";
		}

		public void SetUiStateOpened() {
			toolbarMainPanel.loadBtn.IsEnabled = true;
			toolbarMainPanel.saveBtn.IsEnabled = true;
			toolbarMainPanel.updBtn.IsEnabled = true;
			toolbarMainPanel.closeBtn.IsEnabled = true;
			filterPanel.IsEnabled = true;
			scrollHelperLbl.Visibility = Visibility.Hidden;
		}



		#endregion

		//---

		private bool spIsShown = true;
		//private void btnRightMenuHide_Click(object sender, RoutedEventArgs e) {
		//	ShowHideMenu("sbHideRightMenu", btnRightMenuHide, btnRightMenuShow, pnlRightMenu);
		//}

		//private void btnRightMenuShow_Click(object sender, RoutedEventArgs e) {
		//	ShowHideMenu("sbShowRightMenu", btnRightMenuHide, btnRightMenuShow, pnlRightMenu);
		//}
		//private void ShowHideMenu(string StoryboardStr, Button btnHide, Button btnShow, StackPanel pnl) {
		//	Storyboard sb = Resources[StoryboardStr] as Storyboard;
		//	sb.Begin(pnl);

		//	if (StoryboardStr.Contains("Show")) {
		//		btnHide.Visibility = System.Windows.Visibility.Visible;
		//		btnShow.Visibility = System.Windows.Visibility.Hidden;
		//		spIsShown = true;
		//	} else if (StoryboardStr.Contains("Hide")) {
		//		btnHide.Visibility = System.Windows.Visibility.Hidden;
		//		btnShow.Visibility = System.Windows.Visibility.Visible;
		//		spIsShown = false;
		//	}
		//}

		//public void ShowSidePanel(UIElement contPanel) {
		//	sidePanelSlot.Children.Clear();
		//	if (contPanel == null) return;
		//	sidePanelSlot.Children.Add(contPanel);

		//	if (!spIsShown) {
		//		ShowHideMenu("sbShowRightMenu", btnRightMenuHide, btnRightMenuShow, pnlRightMenu);
		//	}
		//}

		public void SetSidePanel(UIElement contPanel) {
			sidePanelSlot.Children.Clear();
			if (contPanel == null) return;
			sidePanelSlot.Children.Add(contPanel);
		}

		private double lastSpWidth = 400;
		private void Btn_SidePanelSwitch(object sender, RoutedEventArgs e) {
			if (spIsShown) {
				lastSpWidth = spColumn.Width.Value;
				spColumn.Width = new GridLength(0, GridUnitType.Star);
				gridSplitter.IsEnabled = false;
				sidePanelSlot.Visibility = Visibility.Collapsed;
				spIsShown = false;
				spBtnSwitchIcon.Source = new BitmapImage(new Uri(@"pack://application:,,,/Assets/Icons/left.png", UriKind.RelativeOrAbsolute));
			} else {
				sidePanelSlot.Visibility = Visibility.Visible;
				gridSplitter.IsEnabled = true;
				spColumn.Width = new GridLength(lastSpWidth, GridUnitType.Star);
				spBtnSwitchIcon.Source = new BitmapImage(new Uri(@"pack://application:,,,/Assets/Icons/right.png", UriKind.RelativeOrAbsolute));
				spIsShown = true;
			}
		}

		///<summary> Загрузка настроек панели и ее элементов. </summary>
		private void LoadSettings() {
			// восстанавливаем настройку сетки плиток
			if (isRoot) sliderGridCol.Value = Properties.Settings.Default.GridSizeAlbum;
			else sliderGridCol.Value = Properties.Settings.Default.GridSizeEnt;
			if (sliderGridCol.Value <= 0) sliderGridCol.Value = 4;
		}

		///<summary> Сохранение настроек панели и ее элементов. </summary>
		private void SaveSettings() {
			if (isRoot) Properties.Settings.Default.GridSizeAlbum = (int) sliderGridCol.Value;
			else Properties.Settings.Default.GridSizeEnt = (int)sliderGridCol.Value;
		}

	}
}
