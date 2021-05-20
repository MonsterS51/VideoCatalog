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
using System.Windows.Media;
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

		public bool ListMode = false;

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
				toolbarMainPanel.openBtn.Click += App.MainWin.OpenFolder;
				toolbarMainPanel.loadBtn.Click += App.MainWin.LoadCatalog;
				toolbarMainPanel.saveBtn.Click += App.MainWin.SaveCatalog;
				toolbarMainPanel.closeBtn.Click += (_, __) => { App.MainWin.CloseCatalog(); };
				toolbarMainPanel.updBtn.Click += App.MainWin.UpdateCatalog;
				toolbarMainPanel.chkBtn.Click += App.MainWin.CatEng.CatRoot.ChkAlbAndEntState;
				toolbarMainPanel.cleanBtn.Click += App.MainWin.CleanCatalog;
				toolbarMainPanel.utilBtn.Click += ShowUtilPopUp;
				toolbarMainPanel.settingBtn.Click += App.MainWin.OpenSettingTab;
			} else {
				toolbarMainPanel.Visibility = Visibility.Collapsed;
			}

			baseList = BaseList;
			Btn_SidePanelSwitch(null, null);
			LoadSettings();

			// цепляем тут, иначе портит настройки, т.к. срабатывает после LoadSettings
			sliderGridCol.ValueChanged += Slider_ValueChanged;
			sliderListHeight.ValueChanged += SliderList_ValueChanged;

			loadingPanel.Visibility = Visibility.Hidden;

			SetTotalCountText($"Total: {srcList.Count()}");
		}

		//---Y

		///<summary> Отобразить утилитарное меню. </summary>
		private void ShowUtilPopUp(object sender, EventArgs e) {
			var cm = new ContextMenu();

			var mRepReg = new MenuItem();
			mRepReg.Header = @"Replace (Regex) in Names";
			cm.Items.Add(mRepReg);

			var mRepRegAll = new MenuItem();
			mRepRegAll.Header = "All";
			mRepRegAll.Click += (s, ea) => { DoRegexReplaceInNames(true, true); };

			var mRepRegAlb = new MenuItem();
			mRepRegAlb.Header = "Albums";
			mRepRegAlb.Click += (s, ea) => { DoRegexReplaceInNames(true, false); };

			var mRepRegEnt = new MenuItem();
			mRepRegEnt.Header = "Entrys";
			mRepRegEnt.Click += (s, ea) => { DoRegexReplaceInNames(false, true); };

			mRepReg.Items.Add(mRepRegAll);
			mRepReg.Items.Add(mRepRegAlb);
			mRepReg.Items.Add(mRepRegEnt);

			//---

			var mExtRem = new MenuItem();
			mExtRem.Header = @"Remove extensions in Entrys Names";
			cm.Items.Add(mExtRem);
			mExtRem.Click += (s, ea) => { RemoveExtInNames(); };

			//---

			var mTrim = new MenuItem();
			mTrim.Header = @"Trim Names";
			cm.Items.Add(mTrim);
			mTrim.Click += (s, ea) => { TrimNames(); };

			//---

			cm.PlacementTarget = sender as Button;
			cm.IsOpen = true;
		}

		///<summary> Трим пробелов в названиях. </summary>
		private void TrimNames() {
			foreach (var alb in App.MainWin.CatEng.CatRoot.AlbumsList) {
				alb.Name = alb.Name.Trim();
				foreach (var ent in alb.EntryList) {
					ent.Name = ent.Name.Trim();
				}
			}
		}

		///<summary> Удаление расширения видеофайла из названия элемента. </summary>
		private void RemoveExtInNames() {
			foreach (var alb in App.MainWin.CatEng.CatRoot.AlbumsList) {
				foreach (var ent in alb.EntryList) {
					var extStr = ent.EntAbsFile.Extension;
					Console.WriteLine($"{extStr} in {ent.Name}");
					if (!ent.Name.EndsWith(extStr)) continue;
					ent.Name = ent.Name.Remove(ent.Name.Length - extStr.Length, extStr.Length);
				}
			}
		}

		///<summary> Замена подстрок в названиях элементов по Regex паттерну. </summary>
		private void DoRegexReplaceInNames(bool inAlb, bool inEnt) {
			ReplaceStrDialog rsd = new ReplaceStrDialog("\\[.*?\\]");
			if (rsd.ShowDialog() == true) {
				rsd.Result(out string src, out string tar);
				foreach (var alb in App.MainWin.CatEng.CatRoot.AlbumsList) {
					if (inAlb) alb.Name = System.Text.RegularExpressions.Regex.Replace(alb.Name, src, tar).Trim();
					if (inEnt)
						foreach (var ent in alb.EntryList) {
							ent.Name = System.Text.RegularExpressions.Regex.Replace(ent.Name, src, tar).Trim();
						}
				}
			}
		}

		//---B
		#region Scroll and helper label

		///<summary> Обработка изменения скролла с панелями альбомов. </summary>
		private void Scroll_ValueChanged(object sender, EventArgs e) {
			// находим первую видимую плашку альбома
			AbstractEntry ent = null;
			if (ListMode) ent = srcList?.FirstOrDefault(alb => IsUserVisible(alb.lp, scrollViewer));
			else ent = srcList?.FirstOrDefault(alb => IsUserVisible(alb.vp, scrollViewer));

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

		private int oneLineHeight = 20;
		/// <summary> Изменение слайдера размера плашек. </summary>
		private void SliderList_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			foreach (var ent in srcList) {
				ent.ListHeight = (int) (oneLineHeight * e.NewValue) + 4;
			}
			SaveSettings();
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
			bool excepted = filterPanel.exceptedChkBox.IsChecked ?? false;

			if (!grpEnabled) {
				grpMode = GroupModes.FOLDER;
			}


			srcList = FilterSorterModule.FilterAndSort(baseList, filterPanel.filterBox.Text, CatalogRoot.tagsList, sortMode, ascend, broken, excepted, atrName);
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
				UniformGrid newGrid = new UniformGrid();
				newGrid.VerticalAlignment = VerticalAlignment.Top;
				newGrid.Margin = new Thickness(0, 2, 0, 15);
				// разделитель с названием подпапки
				if (isSeparable) entPlates.Children.Add(CreateSeparator(dirEnt.Key, newGrid));

				if (ListMode) {
					newGrid.Columns = 1;
					foreach (var entry in dirEnt.Value) {
						entry.CreateListPlate();
						entry.ListHeight = (int)(oneLineHeight * sliderListHeight.Value) + 4;
						if (entry.lp.Parent != null) MainWindow.RemoveChild(entry.lp.Parent, entry.lp);
						newGrid.Children.Add(entry.lp);
					}
				} else {
					newGrid.Columns = (int)sliderGridCol.Value;
					foreach (var entry in dirEnt.Value) {
						entry.CreatePlate();
						if (entry.vp.Parent != null) MainWindow.RemoveChild(entry.vp.Parent, entry.vp);
						newGrid.Children.Add(entry.vp);
					}
				}



				entPlates.Children.Add(newGrid);
			}

			readyMap.Clear();

			Application.Current.Dispatcher.BeginInvoke((Action)(() => { 
				SetTotalCountText($"Total: {srcList.Count()}"); 
			}));
		}

		///<summary> Создать панель-сепаратор с названием. </summary>
		private StackPanel CreateSeparator(string header, UIElement postElem) {
			StackPanel newSeparatorPanel = new StackPanel();
			newSeparatorPanel.Orientation = Orientation.Horizontal;
			newSeparatorPanel.Margin = new Thickness(5, 2, 5, 2);

			// кнопка сворачивания разворачивания групп
			if (postElem != null) {
				Button newSepBtn = new Button();
				var img = new Image {
					Source = new BitmapImage(new Uri(@"pack://application:,,,/Assets/Icons/minus.png", UriKind.RelativeOrAbsolute)),
					VerticalAlignment = VerticalAlignment.Center
				};
				RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
				newSepBtn.Content = img;
				newSepBtn.Height = 16;
				newSepBtn.Width = 16;
				newSepBtn.Style = (Style)Application.Current.FindResource(ToolBar.ButtonStyleKey);
				newSepBtn.BorderThickness = new Thickness(0);
				newSepBtn.Padding = new Thickness(0);
				newSepBtn.Margin = new Thickness(2, 0, 5, 0);
				newSepBtn.VerticalAlignment = VerticalAlignment.Center;
				newSepBtn.Click += (o, reh) => {
					if (postElem.Visibility == Visibility.Visible) { 
						postElem.Visibility = Visibility.Collapsed;
						img = new Image {
							Source = new BitmapImage(new Uri(@"pack://application:,,,/Assets/Icons/plus.png", UriKind.RelativeOrAbsolute)),
							VerticalAlignment = VerticalAlignment.Center
						};
						RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
						newSepBtn.Content = img;
					} else { 
						postElem.Visibility = Visibility.Visible;
						img = new Image {
							Source = new BitmapImage(new Uri(@"pack://application:,,,/Assets/Icons/minus.png", UriKind.RelativeOrAbsolute)),
							VerticalAlignment = VerticalAlignment.Center
						};
						RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
						newSepBtn.Content = img;
					}
				};
				newSeparatorPanel.Children.Add(newSepBtn);
			}

			// заголовок сепаратора
			TextBlock subHeader = new TextBlock();
			subHeader.FontSize = 16;
			subHeader.Foreground = SystemColors.WindowTextBrush;
			subHeader.Text = header + "  ";

			// линия
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
			toolbarMainPanel.openBtn.IsEnabled = true;
			toolbarMainPanel.loadBtn.IsEnabled = true;
			toolbarMainPanel.saveBtn.IsEnabled = false;
			toolbarMainPanel.closeBtn.IsEnabled = false;
			toolbarMainPanel.updBtn.IsEnabled = false;
			toolbarMainPanel.chkBtn.IsEnabled = false;
			toolbarMainPanel.cleanBtn.IsEnabled = false;
			toolbarMainPanel.utilBtn.IsEnabled = false;
			toolbarMainPanel.settingBtn.IsEnabled = true;

			filterPanel.IsEnabled = false;
			scrollHelperLbl.Visibility = Visibility.Hidden;
			filterPanel.filterBox.Text = "";
			SetInfoText(string.Empty);
			SetTotalCountText(string.Empty);
			loadingPanel.Visibility = Visibility.Hidden;
		}

		public void SetUiStateLoading() {
			loadingPanel.Visibility = Visibility.Visible;
			pBar.Value = 0;

			toolbarMainPanel.openBtn.IsEnabled = false;
			toolbarMainPanel.loadBtn.IsEnabled = false;
			toolbarMainPanel.saveBtn.IsEnabled = false;
			toolbarMainPanel.closeBtn.IsEnabled = true;
			toolbarMainPanel.updBtn.IsEnabled = false;
			toolbarMainPanel.chkBtn.IsEnabled = false;
			toolbarMainPanel.cleanBtn.IsEnabled = false;
			toolbarMainPanel.utilBtn.IsEnabled = false;
			toolbarMainPanel.settingBtn.IsEnabled = false;

			filterPanel.IsEnabled = false;
			scrollHelperLbl.Visibility = Visibility.Hidden;
			filterPanel.filterBox.Text = "";
		}

		public void SetUiStateOpened() {
			toolbarMainPanel.openBtn.IsEnabled = true;
			toolbarMainPanel.loadBtn.IsEnabled = true;
			toolbarMainPanel.saveBtn.IsEnabled = true;
			toolbarMainPanel.closeBtn.IsEnabled = true;
			toolbarMainPanel.updBtn.IsEnabled = true;
			toolbarMainPanel.chkBtn.IsEnabled = true;
			toolbarMainPanel.cleanBtn.IsEnabled = true;
			toolbarMainPanel.utilBtn.IsEnabled = true;
			toolbarMainPanel.settingBtn.IsEnabled = true;

			filterPanel.IsEnabled = true;
			scrollHelperLbl.Visibility = Visibility.Hidden;
			loadingPanel.Visibility = Visibility.Hidden;
		}

		///<summary> Установить текст в нижней панели. </summary>
		public void SetInfoText(string text) {
			infoText.Text = text;
		}

		public void SetTotalCountText(string text) {
			lblCountTotal.Text = text;
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

		//---

		///<summary> Загрузка настроек панели и ее элементов. </summary>
		private void LoadSettings() {
			// восстанавливаем настройку размеров плиток
			if (isRoot) {
				sliderGridCol.Value = Properties.Settings.Default.GridSizeAlbum;
				sliderListHeight.Value = Properties.Settings.Default.ListSizeAlbum;
				ListMode = Properties.Settings.Default.ListModeAlbum;
			} else { 
				sliderGridCol.Value = Properties.Settings.Default.GridSizeEnt;
				sliderListHeight.Value = Properties.Settings.Default.ListSizeEnt;
				ListMode = Properties.Settings.Default.ListModeEnt;
			}
			UpdateUiMode();
		}

		///<summary> Сохранение настроек панели и ее элементов. </summary>
		private void SaveSettings() {
			if (isRoot) {
				Properties.Settings.Default.GridSizeAlbum = (int)sliderGridCol.Value;
				Properties.Settings.Default.ListSizeAlbum = (int)sliderListHeight.Value;
				Properties.Settings.Default.ListModeAlbum = ListMode;
			} else { 
				Properties.Settings.Default.GridSizeEnt = (int)sliderGridCol.Value;
				Properties.Settings.Default.ListSizeEnt = (int)sliderListHeight.Value;
				Properties.Settings.Default.ListModeEnt = ListMode;
			}
		}

		///<summary> Смена режима отображения плиток. </summary>
		private void ListModeChange(object sender, EventArgs e) {
			ListMode = !ListMode;
			UpdateUiMode();
			UpdatePanelContent();
			SaveSettings();
		}

		///<summary> Скрываем регуляторы для других режимов. </summary>
		private void UpdateUiMode() {
			if (ListMode) {
				sliderGridCol.Visibility = Visibility.Collapsed;
				sliderListHeight.Visibility = Visibility.Visible;
			} else {
				sliderGridCol.Visibility = Visibility.Visible;
				sliderListHeight.Visibility = Visibility.Collapsed;
			}
		}

	}
}
