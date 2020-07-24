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
using VideoCatalog.Windows;

namespace VideoCatalog.Panels {
	/// <summary>
	/// Interaction logic for TagPlate.xaml
	/// </summary>
	public partial class TagPlate : UserControl {
		public TagPlate() {
			InitializeComponent();
			MouseDown += OnClick;
		}

		/// <summary> Обработка нажатий мышью по тэгу. </summary>
		private void OnClick(object sender, MouseButtonEventArgs e) {
			if (App.MainWin?.MainPanel == null) return;
			if (e.ChangedButton == MouseButton.Left) {
				if (e.ClickCount == 1) App.MainWin.MainPanel.filterPanel.AddTextToSearch("+"+TagLabel.Text);
			}
			if (e.ChangedButton == MouseButton.Middle) {
				if (e.ClickCount == 1) App.MainWin.MainPanel.filterPanel.AddTextToSearch("*" + TagLabel.Text);
			}
			if (e.ChangedButton == MouseButton.Right) {
				if (e.ClickCount == 1) App.MainWin.MainPanel.filterPanel.AddTextToSearch("-" + TagLabel.Text);
			}

			e.Handled = true;
		}

	}
}
