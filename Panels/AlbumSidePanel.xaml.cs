using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VideoCatalog.Main;
using VideoCatalog.Panels;
using VideoCatalog.Windows;

namespace VideoCatalog.Panels {
	/// <summary>
	/// Interaction logic for AlbumSidePanel.xaml
	/// </summary>
	public partial class AlbumSidePanel : UserControl {
		public AlbumSidePanel() {
			InitializeComponent();
		}

		///<summary> Обновление панели с тегами при изменении их в элементе каталога. </summary>
		private void TagSrcChanged(object sender, DependencyPropertyChangedEventArgs e) {
			TagPanel.Children.Clear();

			var tagArray = (DataContext as AbstractEntry).GetTagList();
			Array.Sort(tagArray);

			foreach (var tag in tagArray) {
				var newTag = new TagPlate();
				newTag.TagLabel.Text = tag;
				newTag.TagLabel.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 255));
				newTag.TagBrd.Background = CatalogRoot.GetTagColor(tag);
				TagPanel.Children.Add(newTag);
			}

		}

		///<summary> Отобразить меню добавления существующих тэгов. </summary>
		private void ShowTagPopUp(object sender, EventArgs e) {
			var entry = DataContext as AbstractEntry;
			var cm = new ContextMenu();
			foreach (var tagName in CatalogRoot.tagsList) {
				if (!entry.GetTagList().Contains(tagName)) {  // убираем уже имеющиеся тэги
					var mItem = new MenuItem();
					mItem.Header = tagName;
					mItem.Click += (s, ea) => {
						entry.AddTag(tagName);
					};
					mItem.FontSize = 10;
					cm.Items.Add(mItem);
				}
			}

			if (cm.Items.Count > 0) {
				cm.PlacementTarget = sender as Button;
				cm.IsOpen = true;
			}

		}

		///<summary> Отобразить меню добавления существующих атрибутов. </summary>
		private void ShowAtrPopUp(object sender, EventArgs e) {
			var entry = DataContext as AbstractEntry;
			var cm = new ContextMenu();
			foreach (var atrName in CatalogRoot.atrList) {
				if (!entry.AtrMap.Any(atrObj => atrObj.AtrName == atrName)) {  // убираем уже имеющиеся в строке
					var mItem = new MenuItem();
					mItem.Header = atrName;
					mItem.Click += (s, ea) => {
						var newAtrEnt = new AbstractEntry.AtrEnt(atrName, "");
						entry.AtrMap.Add(newAtrEnt);
					};
					mItem.FontSize = 10;
					cm.Items.Add(mItem);
				}
			}

			if (cm.Items.Count > 0) {
				cm.PlacementTarget = sender as Button;
				cm.IsOpen = true;
			}

		}

		///<summary> Удалить все атрибуты элемента. </summary>
		private void ClearAllAtr(object sender, EventArgs e) {
			var entry = DataContext as AbstractEntry;
			entry.AtrMap.Clear();
		}

		///<summary> Отобразить меню поиска в интернете по паттерну. </summary>
		private void ShowSearchPopUp(object sender, EventArgs e) {
			if (Properties.Settings.Default.SearchStrings.Length <= 0) return;
			var entry = DataContext as AbstractEntry;

			// собираем карту название поисковика + паттерн поиска
			var serchPat = new Dictionary<string, string>();
			foreach (var oneLine in Properties.Settings.Default.SearchStrings.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) {
				var onePatt = oneLine.Split(';', ',');
				if (onePatt.Length > 1 && !serchPat.ContainsKey(onePatt[0])) {
					serchPat.Add(onePatt[0], onePatt[1]);
				}
			}

			// собираем меню
			var cm = new ContextMenu();
			foreach (var pattEnt in serchPat) {
				var mItem = new MenuItem();
				mItem.Header = pattEnt.Key;
				mItem.Click += (s, ea) => {
					System.Diagnostics.Process.Start(pattEnt.Value.Replace("%s", entry.Name));
				};
				mItem.FontSize = 10;
				cm.Items.Add(mItem);
			}

			if (cm.Items.Count > 0) {
				cm.PlacementTarget = sender as Button;
				cm.IsOpen = true;
			}

		}

		///<summary> Нажатие на кнопку удаления записи. </summary>
		private void RemoveEntBtnClick(object sender, EventArgs e) {
			var entry = DataContext as AbstractEntry;
			var result = MessageBox.Show($"Remove <{entry.Name}> from catalog?", "Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result == MessageBoxResult.Yes) {
				App.MainWin.RemoveEntryAndUpdateUI(entry);
			}
		}

		///<summary> Нажатие на кнопку исключения записи. </summary>
		private void ExceptEntBtnClick(object sender, EventArgs e) {
			var entry = DataContext as AbstractEntry;
			entry.IsExcepted = !entry.IsExcepted;
			UpdateExceptLbl();
			App.MainWin.GetCurrentAlbumePanel().UpdatePanelContent();
		}

		///<summary> Открыть в проводнике. </summary>
		private void FolderEntBtnClick(object sender, EventArgs e) {
			var entry = DataContext as AbstractEntry;
			entry.OpenInExplorer();
		}

		private void CopyEntDataBtnClick(object sender, EventArgs e) {
			var entry = DataContext as AbstractEntry;
			CatalogRoot.entForDataCopy = entry;
		}

		private void PasteEntDataBtnClick(object sender, EventArgs e) {
			var entry = DataContext as AbstractEntry;
			if (CatalogRoot.entForDataCopy != null) entry.PasteEntData(CatalogRoot.entForDataCopy);
		}

		///<summary> Обновление отображения метки исключенности элемента. </summary>
		public void UpdateExceptLbl() {
			var entry = DataContext as AbstractEntry;
			if (entry.IsExcepted) exceptedLbl.Visibility = Visibility.Visible;
			else exceptedLbl.Visibility = Visibility.Collapsed;
		}

		private void tabsPanel_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			Properties.Settings.Default.SidePanelTab = tabsPanel.SelectedIndex;
		}
	}
}
