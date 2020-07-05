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
using System.Windows.Threading;

namespace VideoCatalog.Windows {
	/// <summary>
	/// Interaction logic for PreviewFrameFFME.xaml
	/// </summary>
	public partial class PreviewFrameFFME : UserControl {
		public PreviewFrameFFME() {
			InitializeComponent();
			mediaPlayerFFME.IsMuted = true;
			mediaPlayerFFME.ScrubbingEnabled = false;
			mediaPlayerFFME.VerticalSyncEnabled = true;
			mediaPlayerFFME.RendererOptions.VideoImageType = Unosquare.FFME.Common.VideoRendererImageType.InteropBitmap;
		}

		private int curStep = 1;
		private int totalSteps = 8;
		private DispatcherTimer timer;
		private int duration = 100;

		/// <summary> Запуск превью видео. </summary>
		public void StartPreview(string path, int duration) {
			this.duration = duration;
			prevProgress.Maximum = totalSteps;
			prevProgress.Value = 1;
			prevProgress.Visibility = Visibility.Visible;
			curStep = 1;
			//mediaPlayer.Play();

			timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 2) };     // смещение через 2 секунды
			timer.Tick += Timer_Tick;

			Application.Current.Dispatcher.BeginInvoke((Action)(async () => {
				await mediaPlayerFFME.Open(new Uri(@path));
				await mediaPlayerFFME.Seek(new TimeSpan(0, 0, 0, (duration / totalSteps) * curStep));
				curStep++;
				await mediaPlayerFFME.Play();
				if (timer != null) timer.Start();
			}));
		}

		public void StopPreview() {
			prevProgress.Visibility = Visibility.Hidden;
			if (timer != null) timer.Stop();
			timer = null;
			mediaPlayerFFME.Close();
			//mediaPlayerFFME.Dispose();
		}

		/// <summary> Смещение видео по времени для шага. </summary>
		private void Timer_Tick(object sender, object e) {
			if (mediaPlayerFFME.IsSeekable) {
				Application.Current.Dispatcher.BeginInvoke((Action)(async () => {
					prevProgress.Value = curStep;
					await mediaPlayerFFME.Seek(new TimeSpan(0, 0, 0, (duration / totalSteps) * curStep));
					curStep++;
					if (curStep >= totalSteps) curStep = 1;

				}));
			}
		}


	}
}
