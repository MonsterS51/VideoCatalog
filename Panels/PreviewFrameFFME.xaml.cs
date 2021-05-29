using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace VideoCatalog.Panels {
	/// <summary>
	/// Interaction logic for PreviewFrameFFME.xaml
	/// </summary>
	public partial class PreviewFrameFFME : UserControl {
		public PreviewFrameFFME() {
			InitializeComponent();

			mediaPlayerFFME.IsMuted = true;
			mediaPlayerFFME.ScrubbingEnabled = false;
			mediaPlayerFFME.VerticalSyncEnabled = true;
			mediaPlayerFFME.RendererOptions.UseLegacyAudioOut = true;
			mediaPlayerFFME.RendererOptions.VideoImageType = Unosquare.FFME.Common.VideoRendererImageType.InteropBitmap;

			totalSteps = Properties.Settings.Default.PreviewSteps;
			secSpan = Properties.Settings.Default.PreviewTime;

		}

		private int curStep = 1;
		private int totalSteps = 8;
		private DispatcherTimer timer;
		private int duration = 100;
		private int secSpan = 2;

		public bool isPlaying = false;

		/// <summary> Запуск превью видео. </summary>
		public void StartPreview(string path, int duration) {
			if (isPlaying) return;
			this.duration = duration;
			mediaPlayerFFME.Opacity = 0;

			if (duration > totalSteps * secSpan) {
				// режим с шагом через время для длинных видео
				prevProgress.Maximum = totalSteps;
				prevProgress.Value = 1;
				prevProgress.Visibility = Visibility.Visible;
				prevProgress.IsIndeterminate = false;
				curStep = 1;

				if (timer == null) {
					timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, secSpan) };     // смещение через 2 секунды
					timer.Tick += Timer_Tick;
				}

				Application.Current.Dispatcher.BeginInvoke((Action)(async () => {
					await mediaPlayerFFME.Open(new Uri(@path));
					await mediaPlayerFFME.Seek(new TimeSpan(0, 0, 0, (duration / totalSteps) * curStep));
					curStep++;
					await mediaPlayerFFME.Play();

					// плавное появление плеера
					var alphaIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300)));
					mediaPlayerFFME.BeginAnimation(OpacityProperty, alphaIn);

					if (timer != null) timer.Start();
				}));
			} else {
				// режим непрерывного проигрывания для коротких видео
				prevProgress.Visibility = Visibility.Visible;
				prevProgress.IsIndeterminate = true;
				mediaPlayerFFME.MediaEnded += new EventHandler(m_MediaEnded); // заLOOPа
				mediaPlayerFFME.LoopingBehavior = Unosquare.FFME.Common.MediaPlaybackState.Manual;
				Application.Current.Dispatcher.BeginInvoke((Action)(async () => {
					await mediaPlayerFFME.Open(new Uri(@path));
					await mediaPlayerFFME.Play();
					// плавное появление плеера
					var alphaIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300)));
					mediaPlayerFFME.BeginAnimation(OpacityProperty, alphaIn);
				}));
			}
			isPlaying = true;
		}

		public void StopPreview() {
			prevProgress.Visibility = Visibility.Hidden;
			if (timer != null) timer.Stop();
			timer = null;

			// разрушаем плеер
			Application.Current.Dispatcher.BeginInvoke((Action)(async () => {
				await mediaPlayerFFME.Stop();
				await mediaPlayerFFME.Close();
				await mediaPlayerFFME.ChangeMedia();
				mediaPlayerFFME.Dispose();      //! обязательно - иначе не будет давать очищать элементы по всему дереву родителей
				isPlaying = false;
			}));


		}

		/// <summary> Смещение видео по времени для шага. </summary>
		private void Timer_Tick(object sender, object e) {
			Application.Current.Dispatcher.BeginInvoke((Action)(async () => {
				prevProgress.Value = curStep;
				await mediaPlayerFFME.Seek(new TimeSpan(0, 0, 0, (duration / totalSteps) * curStep));
				curStep++;
				if (curStep >= totalSteps) curStep = 0;
			}));
		}


		async void m_MediaEnded(object sender, EventArgs e) {
			//BUG штатная перемотка сжирает первую секунду, использую переоткрытие
			var src = mediaPlayerFFME.Source;
			await mediaPlayerFFME.Close();
			await mediaPlayerFFME.Open(src);
			await mediaPlayerFFME.Play();

		}


	}
}
