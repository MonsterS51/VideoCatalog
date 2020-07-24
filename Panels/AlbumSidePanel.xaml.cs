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
			var cm = new ContextMenu();

			var entry = DataContext as AbstractEntry;

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
			var cm = new ContextMenu();

			var entry = DataContext as AbstractEntry;

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

	}
}
