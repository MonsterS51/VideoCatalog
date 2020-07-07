using System;
using System.Windows;
using System.Windows.Controls;
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

			totalSteps = Properties.Settings.Default.PreviewSteps;
			secSpan = Properties.Settings.Default.PreviewTime;
		}

		private int curStep = 1;
		private int totalSteps = 8;
		private DispatcherTimer timer;
		private int duration = 100;
		private int secSpan = 2;

		/// <summary> Запуск превью видео. </summary>
		public void StartPreview(string path, int duration) {
			this.duration = duration;

			if (duration > totalSteps * secSpan) {
				// режим с шагом через время для длинных видео
				prevProgress.Maximum = totalSteps;
				prevProgress.Value = 1;
				prevProgress.Visibility = Visibility.Visible;
				prevProgress.IsIndeterminate = false;
				curStep = 1;

				timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, secSpan) };     // смещение через 2 секунды
				timer.Tick += Timer_Tick;
				//Timer_Tick(null, null);

				Application.Current.Dispatcher.BeginInvoke((Action)(async () => {
					await mediaPlayerFFME.Open(new Uri(@path));
					await mediaPlayerFFME.Seek(new TimeSpan(0, 0, 0, (duration / totalSteps) * curStep));
					curStep++;
					await mediaPlayerFFME.Play();
					if (timer != null) timer.Start();
				}));
			} else {
				// режим непрерывного проигрывания для коротких видео
				prevProgress.Visibility = Visibility.Visible;
				prevProgress.IsIndeterminate = true;
				mediaPlayerFFME.MediaEnded += new EventHandler(m_MediaEnded); // заLOOPа

				Application.Current.Dispatcher.BeginInvoke((Action)(async () => {
					await mediaPlayerFFME.Play();
				}));
			}
		}

		public void StopPreview() {
			prevProgress.Visibility = Visibility.Hidden;
			if (timer != null) timer.Stop();
			timer = null;
			mediaPlayerFFME.Close();
		}

		/// <summary> Смещение видео по времени для шага. </summary>
		private void Timer_Tick(object sender, object e) {
			Application.Current.Dispatcher.BeginInvoke((Action)(async () => {
				prevProgress.Value = curStep;
				await mediaPlayerFFME.Seek(new TimeSpan(0, 0, 0, (duration / totalSteps) * curStep));
				curStep++;
				if (curStep >= totalSteps) curStep = 1;
			}));		
		}

		void m_MediaEnded(object sender, EventArgs e) {
			Application.Current.Dispatcher.BeginInvoke((Action)(async () => {
				await mediaPlayerFFME.Stop();
				await mediaPlayerFFME.Play();
			}));
		}


	}
}
