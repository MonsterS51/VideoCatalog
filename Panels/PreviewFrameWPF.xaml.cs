using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace VideoCatalog.Panels {
	/// <summary>
	/// Interaction logic for PreviewFrameWPF.xaml
	/// </summary>
	public partial class PreviewFrameWPF : UserControl {
		public PreviewFrameWPF() {
			InitializeComponent();

			totalSteps = Properties.Settings.Default.PreviewSteps;
			secSpan = Properties.Settings.Default.PreviewTime;
		}

		private int curStep = 1;
		private int totalSteps = 8;
		private int secSpan = 2;
		private DispatcherTimer timer;
		private int duration = 100;

		/// <summary> Запуск превью видео. </summary>
		public void StartPreview(string path, int duration) {
			this.duration = duration;
			mediaPlayer.Source = new Uri(@path);
			mediaPlayer.Opacity = 0;

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
					Timer_Tick(null, null);
				}

				mediaPlayer.Play();

				// плавное появление плеера
				var alphaIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(800)));
				mediaPlayer.BeginAnimation(OpacityProperty, alphaIn);

				if (timer != null) timer.Start();
			} else {
				// режим непрерывного проигрывания для коротких видео
				prevProgress.Visibility = Visibility.Visible;
				prevProgress.IsIndeterminate = true;
				mediaPlayer.MediaEnded += new RoutedEventHandler(m_MediaEnded);	// заLOOPа
				mediaPlayer.Play();

				// плавное появление плеера
				var alphaIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(800)));
				mediaPlayer.BeginAnimation(OpacityProperty, alphaIn);
			}

		}

		public void StopPreview() {
			prevProgress.Visibility = Visibility.Hidden;
			if (timer != null) timer.Stop();
			timer = null;
			mediaPlayer.Stop();
			mediaPlayer.Close();
		}

		/// <summary> Смещение видео по времени для шага. </summary>
		private void Timer_Tick(object sender, object e) {
			prevProgress.Value = curStep;
			mediaPlayer.Position = new TimeSpan(0, 0, 0, (duration / totalSteps) * curStep);
			curStep++;
			if (curStep >= totalSteps) curStep = 1;
		}


		void m_MediaEnded(object sender, RoutedEventArgs e) {
			//! полностью нулевой не работает на видео короче 1 сек
			mediaPlayer.Position = new TimeSpan(0, 0, 0, 0, 1);
			mediaPlayer.Play();
		}

	}
}
