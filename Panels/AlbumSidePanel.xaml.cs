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
			Random r = new Random();

			var tagArray = (DataContext as AbstractEntry).TagList;
			Array.Sort(tagArray);

			foreach (var tag in tagArray) {
				Color bgCol = Color.FromRgb((byte)r.Next(1, 255), (byte)r.Next(1, 255), (byte)r.Next(1, 233));


				//Color lblColor = Color.FromRgb((byte)~bgCol.R, (byte)~bgCol.G, (byte)~bgCol.B);	// инвертированный цвет

				var newTag = new TagPlate();
				newTag.TagLabel.Text = tag;
				newTag.TagLabel.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 255));
				newTag.TagBrd.Background = new SolidColorBrush(bgCol);
				TagPanel.Children.Add(newTag);
			}

		}

		///<summary> Отобразить меню добавления в строку поиска существующих тэгов. </summary>
		private void ShowTagPopUp(object sender, EventArgs e) {
			var cm = new ContextMenu();

			foreach (var tagName in CatalogRoot.tagsList) {
				if (!tagStr.Text.Contains(tagName)) {  // убираем уже имеющиеся в строке
					var mItem = new MenuItem();
					mItem.Header = tagName;
					mItem.Click += (s, ea) => {
						tagStr.Text = tagStr.Text + " ," + tagName;
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

		private void tagStr_TextChanged(object sender, TextChangedEventArgs e) {
			Console.WriteLine("" + tagStr.Text);
		}
	}
}
