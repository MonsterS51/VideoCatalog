﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VideoCatalog.Main;

namespace VideoCatalog.Windows {
	/// <summary>
	/// Interaction logic for AlbumPlate.xaml
	/// </summary>
	public partial class ViewPlate : UserControl {
		public ViewPlate() {
			InitializeComponent();
			MouseDown += OnClick;
			tempColor = border.BorderBrush.Clone();

			TurnOffPreviewMode();
		}

		private Brush tempColor;

		public Action onDoubleClick;
		public Action onClick;
		public Action onWheelClick;

		public int duration = 100;
		public string path;

		private PreviewFrameWPF pfWPF = null;
		private PreviewFrameFFME pfFFME = null;

		protected override void OnMouseEnter(MouseEventArgs e) {
			base.OnMouseEnter(e);
			if (Properties.Settings.Default.PreviewMode == "VLC") return;

			// определяем метод отрисовки предпросмотра и формируем нужную панель
			switch (Properties.Settings.Default.PreviewMode) {
            	case "WPF":{
					if (pfWPF == null) {
						pfWPF = new PreviewFrameWPF();
						previewGrid.Children.Clear();
						previewGrid.Children.Add(pfWPF);
					}
					pfWPF?.StartPreview(path, duration);
					break;
            	}
				case "FFME": {
					if (pfFFME == null) {
						pfFFME = new PreviewFrameFFME();
						previewGrid.Children.Clear();
						previewGrid.Children.Add(pfFFME);
					}
					pfFFME?.StartPreview(path, duration);
					break;
				}
				default:{
            		break;
            	}
            }

			TurnOnPreviewMode();
		}

		protected override void OnMouseLeave(MouseEventArgs e) {
			base.OnMouseLeave(e);
			if (Properties.Settings.Default.PreviewMode == "VLC") return;

			TurnOffPreviewMode();

			pfWPF?.StopPreview();
			pfFFME?.StopPreview();
		}


		public void TurnOnPreviewMode() {
			border.BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230));
			previewGrid.Visibility = Visibility.Visible;
		}

		public void TurnOffPreviewMode() {
			border.BorderBrush = tempColor;
			previewGrid.Visibility = Visibility.Hidden;
		}

		//---

		/// <summary> Обработка нажатий мышью по плэйту. </summary>
		private void OnClick(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left) {
				if (e.ClickCount == 1) onClick?.Invoke();
				if (e.ClickCount == 2) onDoubleClick?.Invoke();
			}
			if (e.ChangedButton == MouseButton.Middle) {
				onWheelClick?.Invoke();
			}
			if (e.ChangedButton == MouseButton.Right) {
				//Console.WriteLine("rmb");
				ContextMenu cm = FindResource("cmButton") as ContextMenu;
				cm.PlacementTarget = sender as Button;
				cm.IsOpen = true;
			}
		}

		/// <summary> Открытие в проводнике по привязанному DataContext. </summary>
		private void MenuItem_OpenInExplorer(object sender, RoutedEventArgs e) {
			if (DataContext != null) {
				// альбом
				if (DataContext is CatalogAlbum) {
					var dc = DataContext as CatalogAlbum;
					string path = dc.GetFirstEntPath();
					if (!string.IsNullOrWhiteSpace(path)) {
						CatalogEngine.OpenExplorer(path);
					}			
				} else
				// эпизод альбома
				if (DataContext is CatalogEntry) {
					var dc = DataContext as CatalogEntry;
					dc.EntAbsFile.Refresh();
					if (dc.EntAbsFile.Exists) CatalogEngine.OpenExplorer(dc.EntAbsFile.FullName);					
				}
			}
		}

		/// <summary> Обновление обложек альбома/эпизода. </summary>
		private void MenuItem_UpdateCoverArt(object sender, RoutedEventArgs e) {
			if (DataContext != null) {
				// албьбом
				if (DataContext is CatalogAlbum) {
					var dc = DataContext as CatalogAlbum;
					dc.UpdateAlbumArt();
				} else
				// эпизод альбома
				if (DataContext is CatalogEntry) {
					var dc = DataContext as CatalogEntry;
					dc.LoadCover(true);
				}
			}
		}

		/// <summary> Запуск поиска новых файлов. </summary>
		private void MenuItem_UpdateFiles(object sender, RoutedEventArgs e) {
			if (DataContext != null) {
				// албьбом
				if (DataContext is CatalogAlbum) {
					var dc = DataContext as CatalogAlbum;

				} else
				// эпизод альбома
				if (DataContext is CatalogEntry) {
					var dc = DataContext as CatalogEntry;

				}
			}
		}

	}

}

