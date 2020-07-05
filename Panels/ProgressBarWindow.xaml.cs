using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
	/// Interaction logic for ProgressBar.xaml
	/// </summary>
	public partial class ProgressBarWindow : Window {
		public ProgressBarWindow() {
			InitializeComponent();
		}

		private DoWorkEventHandler workEv;

		public void StartWork(DoWorkEventHandler WorkEv) {
			if (WorkEv == null) return;
			workEv = WorkEv;
			BackgroundWorker worker = new BackgroundWorker();
			worker.WorkerReportsProgress = true;
			worker.DoWork += WorkEv;
			worker.ProgressChanged += worker_ProgressChanged;
			worker.RunWorkerAsync();
		}

		private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
			pBar.Value = e.ProgressPercentage;
		}
	}
}
