﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace VideoCatalog.Windows {
	/// <summary>
	/// Interaction logic for PreviewFrameWPF.xaml
	/// </summary>
	public partial class PreviewFrameWPF : UserControl {
		public PreviewFrameWPF() {
			InitializeComponent();
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

			if (duration > totalSteps * secSpan) {
				// режим с шагом через время для длинных видео
				prevProgress.Maximum = totalSteps;
				prevProgress.Value = 1;
				prevProgress.Visibility = Visibility.Visible;
				prevProgress.IsIndeterminate = false;
				curStep = 1;

				timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, secSpan) };     // смещение через 2 секунды
				timer.Tick += Timer_Tick;

				Timer_Tick(null, null);
				mediaPlayer.Play();
				if (timer != null) timer.Start();
			} else {
				// режим непрерывного проигрывания для коротких видео
				prevProgress.Visibility = Visibility.Visible;
				prevProgress.IsIndeterminate = true;
				mediaPlayer.MediaEnded += new RoutedEventHandler(m_MediaEnded);	// заLOOPа
				mediaPlayer.Play();
				
			}

		}

		public void StopPreview() {
			prevProgress.Visibility = Visibility.Hidden;
			if (timer != null) timer.Stop();
			timer = null;
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
			mediaPlayer.Position = TimeSpan.FromSeconds(0);
			mediaPlayer.Play();
		}

	}
}
