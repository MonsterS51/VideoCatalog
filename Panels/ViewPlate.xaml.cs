using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VideoCatalog.Main;
using VideoCatalog.Windows;

namespace VideoCatalog.Panels {
	/// <summary>
	/// Interaction logic for ViewPlate.xaml
	/// </summary>
	public partial class ViewPlate : UserControl {
		public ViewPlate() {
			InitializeComponent();
			MouseDown += OnClick;
			tempColor = border.BorderBrush.Clone();

			PreviewDecorator.AspectRatio = (float) Properties.Settings.Default.CoverAspectRatio;

			TurnOffPreviewMode();
		}

		private Brush tempColor;

		public Action onDoubleClick;
		public Action onClick;
		public Action onWheelClick;

		private PreviewFrameWPF pfWPF = null;
		private PreviewFrameFFME pfFFME = null;

		///<summary> Навели мышь на плитку - обводим, запускаем предпросмотр. </summary>
		protected override void OnMouseEnter(MouseEventArgs e) {
			base.OnMouseEnter(e);


			border.BorderBrush = SystemColors.MenuHighlightBrush;

			// если предпросмотр отключен
			if (!Properties.Settings.Default.PreviewEnabled) return;

			var entry = DataContext as AbstractEntry;
			if (entry == null | entry.BaseEntry == null || !entry.BaseEntry.EntAbsFile.Exists) return;

			Task.Delay(500).ContinueWith((task) => {
				Dispatcher.BeginInvoke((Action)(() => {
					if (IsMouseOver) {
						// определяем метод отрисовки предпросмотра и формируем нужную панель
						switch (Properties.Settings.Default.PreviewMode) {
							case "WPF": {
								if (pfWPF == null) {
									pfWPF = new PreviewFrameWPF();
									previewGrid.Children.Clear();
									previewGrid.Children.Add(pfWPF);
								}
								pfWPF?.StartPreview(entry.BaseEntry.EntAbsFile.FullName, entry.BaseEntry.duration);
								break;
							}
							case "FFME": {
								if (!App.FoundFFMpegLibs) break;
								if (pfFFME == null) {
									pfFFME = new PreviewFrameFFME();
									previewGrid.Children.Clear();
									previewGrid.Children.Add(pfFFME);
								}
								pfFFME?.StartPreview(entry.BaseEntry.EntAbsFile.FullName, entry.BaseEntry.duration);
								break;
							}
							default: {
								break;
							}
						}

						TurnOnPreviewMode();
					}
				}));
			});



			
		}

		///<summary> Убрали мышь с плитки - снимаем обводку, прячем и останавливаем предпросмотр. </summary>
		protected override void OnMouseLeave(MouseEventArgs e) {
			base.OnMouseLeave(e);

			border.BorderBrush = tempColor;

			TurnOffPreviewMode();

			pfWPF?.StopPreview();
			pfFFME?.StopPreview();

			previewGrid.Children.Clear();
			pfWPF = null;
			pfFFME = null;

		}

		private int fadeTime = 300;

		///<summary> Переход в режим превью. </summary>
		public void TurnOnPreviewMode() {
			previewGrid.Visibility = Visibility.Visible;
			lblTopRight.Visibility = Visibility.Hidden;
			panelTopLeft.Visibility = Visibility.Hidden;

			// плавное скрытие кавера
			var alphaIn = new DoubleAnimation(CoverArt.Opacity, 0, new Duration(TimeSpan.FromMilliseconds(fadeTime)));
			CoverArt.BeginAnimation(Image.OpacityProperty, alphaIn);
		}

		///<summary> Переход в обычный режим. </summary>
		public void TurnOffPreviewMode() {
			lblTopRight.Visibility = Visibility.Visible;
			panelTopLeft.Visibility = Visibility.Visible;

			// плавное проявление кавера
			var alphaOut = new DoubleAnimation(CoverArt.Opacity, 1, new Duration(TimeSpan.FromMilliseconds(fadeTime)));
			CoverArt.BeginAnimation(Image.OpacityProperty, alphaOut);

			alphaOut.Completed += (s, e) =>	{ previewGrid.Visibility = Visibility.Hidden; };
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
				RmbMenuOpen(sender, e);
			}
		}

		private void RmbMenuOpen(object sender, MouseButtonEventArgs e) {
			var cm = new ContextMenu();

			var mOpenInExp = new MenuItem();
			mOpenInExp.Header = "Open in Explorer";
			mOpenInExp.Click += (s, ea) => { MenuItem_OpenInExplorer(sender, e); };
			cm.Items.Add(mOpenInExp);

			var mUpdCov = new MenuItem();
			mUpdCov.Header = "Update Cover";
			mUpdCov.Click += (s, ea) => { MenuItem_UpdateCoverArt(sender, e); };
			cm.Items.Add(mUpdCov);

			if (DataContext is AbstractEntry) {
				cm.Items.Add(new Separator());

				var entry = DataContext as AbstractEntry;
				var mRemove = new MenuItem();
				mRemove.Header = "Remove";
				mRemove.Click += (s, ea) => {
					var result = MessageBox.Show($"Remove <{entry.Name}> from catalog?", "Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);
					if (result == MessageBoxResult.Yes) App.MainWin.RemoveEntryAndUpdateUI(entry);
				};
				cm.Items.Add(mRemove);

				var mExcept = new MenuItem();
				mExcept.Header = "Except Entry";
				mExcept.Click += (s, ea) => {
					entry.IsExcepted = !entry.IsExcepted;
					App.MainWin.ClearSidePanel();
					App.MainWin.GetCurrentAlbumePanel().UpdatePanelContent();
				};
				cm.Items.Add(mExcept);
			}
			

			cm.PlacementTarget = sender as Button;
			cm.IsOpen = true;
		}



		/// <summary> Открытие в проводнике по привязанному DataContext. </summary>
		private void MenuItem_OpenInExplorer(object sender, RoutedEventArgs e) {
			if (DataContext != null) {
				(DataContext as AbstractEntry)?.OpenInExplorer();
			}
		}

		/// <summary> Обновление обложек альбома/эпизода. </summary>
		private void MenuItem_UpdateCoverArt(object sender, RoutedEventArgs e) {
			if (DataContext != null) {
				// альбом
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
				// альбом
				if (DataContext is CatalogAlbum) {
					var dc = DataContext as CatalogAlbum;

				} else
				// эпизод альбома
				if (DataContext is CatalogEntry) {
					var dc = DataContext as CatalogEntry;

				}
			}
		}

		///<summary> Обновление отображения плашек качества. </summary>
		public void UpdateVideoResIcons(bool lq, bool hd, bool fhd, bool qhd, bool uhd) {
			Icon_LQ.Visibility = Visibility.Collapsed;
			Icon_HD.Visibility = Visibility.Collapsed;
			Icon_FHD.Visibility = Visibility.Collapsed;
			Icon_QHD.Visibility = Visibility.Collapsed;
			Icon_UHD.Visibility = Visibility.Collapsed;

			if (lq) Icon_LQ.Visibility = Visibility.Visible;
			if (hd) Icon_HD.Visibility = Visibility.Visible;
			if (fhd) Icon_FHD.Visibility = Visibility.Visible;
			if (qhd) Icon_QHD.Visibility = Visibility.Visible;
			if (uhd) Icon_UHD.Visibility = Visibility.Visible;
		}

	}

}

