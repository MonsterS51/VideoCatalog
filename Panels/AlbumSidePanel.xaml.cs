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
				Color lblColor = Color.FromRgb((byte)~bgCol.R, (byte)~bgCol.G, (byte)~bgCol.B);

				var newTag = new TagPlate();
				newTag.TagLabel.Text = tag;
				newTag.TagLabel.Fill = new SolidColorBrush(lblColor);
				newTag.TagBrd.Background = new SolidColorBrush(bgCol);
				TagPanel.Children.Add(newTag);
			}

		}
	}
}
