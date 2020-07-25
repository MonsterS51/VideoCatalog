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
using System.Windows.Shapes;

namespace VideoCatalog.Windows {
	/// <summary>
	/// Диалоговое окно замены подстрок.
	/// </summary>
	public partial class ReplaceStrDialog : Window {

		public ReplaceStrDialog(string rplSrc = "", string rplTar = "") {
			InitializeComponent();
			txtRplSrc.Text = rplSrc;
			txtRplTar.Text = rplTar;
		}

		private void btnDialogOk_Click(object sender, RoutedEventArgs e) {
			DialogResult = true;
		}

		private void Window_ContentRendered(object sender, EventArgs e) {
			txtRplSrc.SelectAll();
			txtRplSrc.Focus();
		}

		public void Result (out string rplSrc, out string rplTar) {
			rplSrc = txtRplSrc.Text;
			rplTar = txtRplTar.Text;
		}

	}
}
