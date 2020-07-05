using Meta.Vlc.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace VideoCatalog.Windows {
	/// <summary>
	/// Interaction logic for PreviewFrameWPF.xaml
	/// </summary>
	public partial class PreviewFrameVLC : UserControl {
		public PreviewFrameVLC() {
			mediaPlayer = new VlcPlayer();
			mediaPlayer.Initialize(Properties.Settings.Default.VlcLibPath, new string[] { "-I", "dummy", "--ignore-config", "--no-video-title" });

			//mediaPlayer = App.mediaPlayer;

			InitializeComponent();
		}

		private int secSpan = 2;
		private int curStep = 1;
		private int totalSteps = 8;
		private DispatcherTimer timer;

		private bool canPlay = true;

		/// <summary> Запуск превью видео. </summary>
		public void StartPreview(string path, int duration) {
			Console.WriteLine("StartPreview");

			canPlay = true;

			timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 2) };     // смещение через 2 секунды
			timer.Tick += Timer_Tick;

			mediaPlayer.LoadMedia(@path);
			mediaPlayer.Volume = 0;
			mediaPlayer.IsMute = true;

			if (duration > totalSteps * secSpan) {
				// режим с шагом через время для длинных видео
				prevProgress.Maximum = totalSteps;
				prevProgress.Value = 1;
				prevProgress.Visibility = Visibility.Visible;
				prevProgress.IsIndeterminate = false;
				curStep = 1;

				void threadWithTimer() {
					// поток с задержкой нужен, чтобы не залипало при слишком частом запуске плеера (если теребить плитку мышкой)
					//Thread.Sleep(1000);
					//if (!IsMouseOver | !canPlay) return;

					Application.Current.Dispatcher.BeginInvoke((Action)(() => {
						Console.WriteLine("Play");



						mediaPlayer.Play();

						Timer_Tick(null, null);
						timer?.Start();
					}));
				}
				new Thread(threadWithTimer).Start();

				// без перемотки для коротких видео
			} else {
				// режим непрерывного проигрывания для коротких видео
				prevProgress.Visibility = Visibility.Visible;
				prevProgress.IsIndeterminate = true;

				void threadNoTimer() {
					// поток с задержкой нужен, чтобы не залипало при слишком частом запуске плеера (если теребить плитку мышкой)
					Thread.Sleep(500);
					if (!IsMouseOver | !canPlay) return;

					Application.Current.Dispatcher.BeginInvoke((Action)(() => {
						if (IsMouseOver) mediaPlayer.Play();
					}));
				}
				new Thread(threadNoTimer).Start();
			}
		}

		public void StopPreview() {
			prevProgress.Visibility = Visibility.Hidden;
			if (timer != null) timer.Stop();
			timer = null;

			void threadWithTimer() {
				Application.Current.Dispatcher.BeginInvoke((Action)(() => {
					Thread.Sleep(500);
					mediaPlayer.Stop();

					mediaPlayer.Dispose();
					mediaPlayer = null;
					GC.Collect();

					Thread.Sleep(500);
				}));
			}
			new Thread(threadWithTimer).Start();



		}

		/// <summary> Смещение видео по времени для шага. </summary>
		private void Timer_Tick(object sender, object e) {
			//Console.WriteLine("Step " + (float)curStep / totalSteps);

			prevProgress.Value = curStep;
			mediaPlayer.Position = (float)curStep / totalSteps;

			curStep++;
			if (curStep >= totalSteps) curStep = 1;
		}

		//private void Grid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
		//	Console.WriteLine("Leave");
		//	//mediaPlayer.Stop();
		//	canPlay = false;
		//}
	}
}
