using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VideoCatalog.Main;
using VideoCatalog.Util;
using VideoCatalog.Windows;
using static VideoCatalog.Main.FilterSorterModule;

namespace VideoCatalog.Panels {
	/// <summary>
	/// Interaction logic for AlbumPanel.xaml
	/// </summary>
	public partial class AlbumPanel : UserControl {

		private IEnumerable<AbstractEntry> baseList;
		private IEnumerable<AbstractEntry> srcList = new List<AbstractEntry>();   // отфильтрованные альбомы

		private bool isRoot = false;

		/// <summary>
		/// Панель представления набора элементов каталога.
		/// </summary>
		/// <param name="BaseList"> Набор элементов для панели. </param>
		/// <param name="isRoot"> Является панелью каталога. </param>
		public AlbumPanel(IEnumerable<AbstractEntry> BaseList, bool isRoot = false) {
			InitializeComponent();
			this.isRoot = isRoot;

			if (isRoot) {
				toolbarMainPanel.Visibility = Visibility.Visible;
				toolbarMainPanel.newBtn.Click += App.MainWin.OpenFolder;
				toolbarMainPanel.loadBtn.Click += App.MainWin.LoadCatalog;
				toolbarMainPanel.saveBtn.Click += App.MainWin.SaveCatalog;
				toolbarMainPanel.closeBtn.Click += App.MainWin.CloseCatalog;
				toolbarMainPanel.updBtn.Click += App.MainWin.UpdateCatalog;
				toolbarMainPanel.chkBtn.Click += App.MainWin.CatEng.CatRoot.ChkAlbAndEntState;
				toolbarMainPanel.settingBtn.Click += App.MainWin.OpenSettingTab;
			} else {
				toolbarMainPanel.Visibility = Visibility.Collapsed;
			}

			baseList = BaseList;
			Btn_SidePanelSwitch(null, null);
			LoadSettings();
		}

		//---B
		#region Scroll and helper label

		///<summary> Обработка изменения скролла с панелями альбомов. </summary>
		private void Scroll_ValueChanged(object sender, EventArgs e) {
			// находим первую видимую плашку альбома
			AbstractEntry ent = srcList?.FirstOrDefault(alb => IsUserVisible(alb.vp, scrollViewer));
			if (ent != null) {
				scrollHelperLbl.Text = ent.sortHelper;

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
			return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);  // виден ЛВ или ПН угол элемента
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
		#endregion

		//---Y
		#region Panel Content Update

		///<summary> Переформирование содержимого панели с учетом фильтрации/сортировки. </summary>
		public void UpdatePanelContent() {
			FilterChanged(null, null);
			filterPanel.FillSortCombo();
		}

		private FilterSorterModule.SortMode sortMode;
		///<summary> Обновление после изменения одного из контролов фильтра. </summary>
		private void FilterChanged(object sender, RoutedEventArgs e) {
			Stopwatch sw = new Stopwatch();
			sw.Start();

			string atrName = "";    // для проброса имени атрибута из названия пункта комбобокса сортировки 
			GroupModes grpMode = GroupModes.NONE;

			switch (filterPanel.sortModeComBox.SelectedIndex) {
				case 0: {
					sortMode = FilterSorterModule.SortMode.NAME;
					grpMode = GroupModes.FIRST_CHAR;
					break;
				}
				case 1: {
					sortMode = FilterSorterModule.SortMode.DATE_ADD;
					grpMode = GroupModes.DATE_ADD;
					break;
				}
				case 2: {
					sortMode = FilterSorterModule.SortMode.DATE_CREATE;
					grpMode = GroupModes.DATE_CREATE;
					break;
				}
				case 3: {
					sortMode = FilterSorterModule.SortMode.DATE_MODIFIED;
					grpMode = GroupModes.DATE_MODIFIED;
					break;
				}
				case -1: goto case 0;
				default: {
					sortMode = FilterSorterModule.SortMode.ATTRIBUTE;
					grpMode = GroupModes.ATTRIBUTE;
					string selItemName = (filterPanel.sortModeComBox.SelectedItem as TextBlock).Text;
					atrName = selItemName.Substring(selItemName.IndexOf(" ") + 1);	// отрезаем слово аттрибут
					break;
				}
			}

			bool grpEnabled = filterPanel.grpChkBox.IsChecked ?? false;
			bool ascend = filterPanel.ascendChkBox.IsChecked ?? false;
			bool broken = filterPanel.brokenChkBox.IsChecked ?? false;

			if (!grpEnabled) {
				grpMode = GroupModes.FOLDER;
			}


			srcList = FilterSorterModule.FilterAndSort(baseList, filterPanel.filterBox.Text, CatalogRoot.tagsList, sortMode, ascend, broken, atrName);
			FillPlates(grpMode, ascend, atrName);
			
			Console.WriteLine($"FilterChanged {sw.ElapsedMilliseconds}ms");
			sw.Stop();
		}

		/// <summary> Перезаполнение плитками панель. </summary>
		private void FillPlates(GroupModes grpMode, bool ascend = true, string atrName="") {
			// зачищаем имеющееся
			ClearPanel();

			// группируем
			var readyMap = FilterSorterModule.GroupProcess(srcList, grpMode, ascend, atrName);

			bool isSeparable = readyMap.Count() > 1;

			// заполняем в соотвествии с группами
			foreach (var dirEnt in readyMap) {
				// разделитель с названием подпапки
				if (isSeparable) entPlates.Children.Add(CreateSeparator(dirEnt.Key));

				//! + сюда можно запилить кнопку сворачивания / разворачивания групп

				UniformGrid newGrid = new UniformGrid();
				newGrid.VerticalAlignment = VerticalAlignment.Top;
				newGrid.Columns = (int) sliderGridCol.Value;
				foreach (var entry in dirEnt.Value) {
					entry.CreatePlate();
					if (entry.vp.Parent != null) MainWindow.RemoveChild(entry.vp.Parent, entry.vp);
					newGrid.Children.Add(entry.vp);
				}
				entPlates.Children.Add(newGrid);
			}

			readyMap.Clear();

			Application.Current.Dispatcher.BeginInvoke((Action)(() => { 
				if (App.MainWin?.MainPanel != null) App.MainWin.MainPanel.lblCountTotal.Text = "Total: " + srcList.Count(); 
			}));
		}

		///<summary> Создать панель-сепаратор с названием. </summary>
		private StackPanel CreateSeparator(string header) {
			StackPanel newSeparatorPanel = new StackPanel();
			newSeparatorPanel.Orientation = Orientation.Horizontal;
			newSeparatorPanel.Margin = new Thickness(5, 15, 5, 5);

			TextBlock subHeader = new TextBlock();
			subHeader.FontSize = 16;
			subHeader.Foreground = SystemColors.WindowTextBrush;
			subHeader.Text = header + "  ";

			Border line = new Border();
			line.Width = 4000;
			line.Height = 1;
			line.HorizontalAlignment = HorizontalAlignment.Stretch;

			line.BorderBrush = subHeader.Foreground;
			line.BorderThickness = new Thickness(1);

			newSeparatorPanel.Children.Add(subHeader);
			newSeparatorPanel.Children.Add(line);
			return newSeparatorPanel;
		}

		/// <summary> Очистка панели (для повторного использования плашек). </summary>
		public void ClearPanel() {
			foreach (var panelEnt in entPlates.Children) {
				if (panelEnt is UniformGrid) {
					var grid = panelEnt as UniformGrid;
					grid?.Children.Clear();
				}
			}
			entPlates.Children.Clear();
		}

		#endregion

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
