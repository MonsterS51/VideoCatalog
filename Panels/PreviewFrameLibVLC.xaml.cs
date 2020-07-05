using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace VideoCatalog.Windows {

	public partial class PreviewFrameLibVLC : UserControl {

		public PreviewFrameLibVLC(ViewPlate vp) {
			this.vp = vp;
			InitializeComponent();
		}

		private ViewPlate vp;

		private int curStep = 1;
		private int totalSteps = 8;
		private int secSpan = 2;
		private DispatcherTimer timer;


		private MediaPlayer mp;
		private LibVLC lVlc;
		private Media media;

		/// <summary> Запуск превью видео. </summary>
		private void StartPreview() {
			Console.WriteLine("StartPreview! " + vp.path);

			lVlc = new LibVLC();

			media = new Media(lVlc, new Uri(vp.path));
			media.AddOption(":no-audio");

			if (mediaPlayer.MediaPlayer == null) {
				mp = new MediaPlayer(media);
				mp.EnableKeyInput = false;
				mp.EnableMouseInput = false;
				mediaPlayer.MediaPlayer = mp;
			}

			if (vp.duration > totalSteps * secSpan) {
				// режим с шагом через время для длинных видео
				prevProgress.Maximum = totalSteps;
				prevProgress.Value = 1;
				prevProgress.Visibility = Visibility.Visible;
				prevProgress.IsIndeterminate = false;
				curStep = 1;

				timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, secSpan) };     // смещение через 2 секунды
				timer.Tick += Timer_Tick;

				mediaPlayer.MediaPlayer = mp;

				mediaPlayer.Loaded += (sender, e) => {
					mediaPlayer.MediaPlayer.Position = 0f;
					mediaPlayer.MediaPlayer.Play();

					Timer_Tick(null, null);
					if (timer != null) timer.Start();
				};


			} else {
				// режим непрерывного проигрывания для коротких видео
				prevProgress.Visibility = Visibility.Visible;
				prevProgress.IsIndeterminate = true;

				mediaPlayer.Loaded += (sender, e) => {
					mediaPlayer.MediaPlayer = mp;
					mediaPlayer.MediaPlayer.Position = 0f;
					mediaPlayer.MediaPlayer.Play();
				};
			}



		}

		private void StopPreview() {
			Console.WriteLine("StopPreview!");

			if (timer != null) timer.Stop();
			timer = null;

			if (mediaPlayer.MediaPlayer != null) {
				mediaPlayer.MediaPlayer.Stop();
				mediaPlayer.MediaPlayer.Position = 0f;
				mediaPlayer.MediaPlayer = null;
			}

			mp?.Dispose();
			media?.Dispose();
			lVlc?.Dispose();

			GC.Collect();

			prevProgress.Visibility = Visibility.Hidden;

		}

		/// <summary> Смещение видео по времени для шага. </summary>
		private void Timer_Tick(object sender, object e) {
			prevProgress.Value = curStep;

			if (mediaPlayer.MediaPlayer != null) mediaPlayer.MediaPlayer.Time = (long) new TimeSpan(0, 0, 0, (vp.duration / totalSteps) * curStep).TotalMilliseconds;

			curStep++;
			if (curStep >= totalSteps) curStep = 1;
		}


		private void playerRect_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) {
			Console.WriteLine("playerRect_MouseEnter!");
			vp?.TurnOnPreviewMode();
			StartPreview();
		}

		private void playerRect_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
			Console.WriteLine("playerRect_MouseLeave!");
			vp?.TurnOffPreviewMode();
			StopPreview();
		}
	}
}
