using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VideoCatalog.Main;
using VideoCatalog.Util;
using VideoCatalog.Windows;

namespace VideoCatalog.Panels {
	/// <summary>
	/// Interaction logic for ListPlate.xaml
	/// </summary>
	public partial class ListPlate : UserControl {
		public ListPlate() {
			InitializeComponent();
			MouseDown += OnClick;
			tempColor = border.BorderBrush.Clone();

			LeftPreviewDecorator.AspectRatio = (float) Properties.Settings.Default.CoverAspectRatio;

			TurnOffPreviewMode();

			LeftPreviewDecorator.MouseEnter += OnMousePreviewEnter;
			LeftPreviewDecorator.MouseLeave += OnMousePreviewLeave;
		}

		private Brush tempColor;

		public Action onDoubleClick;
		public Action onClick;
		public Action onWheelClick;

		private PreviewFrameWPF pfWPF = null;
		private PreviewFrameFFME pfFFME = null;

		///<summary> Навели мышь на плитку - обводим. </summary>
		protected override void OnMouseEnter(MouseEventArgs e) {
			base.OnMouseEnter(e);
			border.BorderBrush = SystemColors.MenuHighlightBrush;
		}

		/// <summary> Запуск предпросмотра по наведению на ковер. </summary>
		private void OnMousePreviewEnter(object sender, MouseEventArgs e) {
			// если предпросмотр отключен
			if (!Properties.Settings.Default.PreviewEnabled) return;

			var entry = DataContext as AbstractEntry;
			if (entry == null | entry.BaseEntry == null || !entry.BaseEntry.EntAbsFile.Exists) return;

			Task.Delay(Properties.Settings.Default.PreviewStartDelay).ContinueWith((task) => {
				Dispatcher.BeginInvoke((Action)(() => {
					if (LeftPreviewDecorator.IsMouseOver) {
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

		///<summary> Убрали мышь с плитки - снимаем обводку. </summary>
		protected override void OnMouseLeave(MouseEventArgs e) {
			base.OnMouseLeave(e);
			border.BorderBrush = tempColor;
		}

		/// <summary> Прячем и останавливаем предпросмотр. </summary>
		private void OnMousePreviewLeave(object sender, MouseEventArgs e) {
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

			// плавное скрытие кавера
			var alphaIn = new DoubleAnimation(CoverArt.Opacity, 0, new Duration(TimeSpan.FromMilliseconds(fadeTime)));
			CoverArt.BeginAnimation(Image.OpacityProperty, alphaIn);
		}

		///<summary> Переход в обычный режим. </summary>
		public void TurnOffPreviewMode() {
			// плавное проявление кавера
			var alphaOut = new DoubleAnimation(CoverArt.Opacity, 1, new Duration(TimeSpan.FromMilliseconds(fadeTime)));
			CoverArt.BeginAnimation(Image.OpacityProperty, alphaOut);

			alphaOut.Completed += (s, e) => { previewGrid.Visibility = Visibility.Hidden; };
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
				PlateUtil.RmbMenuOpen(sender, DataContext);
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

		///<summary> Обновление панели с тегами при изменении их в элементе каталога. </summary>
		private void TagSrcChanged(object sender, DependencyPropertyChangedEventArgs e) {
			TagPanel.Children.Clear();

			var tagArray = (DataContext as AbstractEntry).GetTagList();
			Array.Sort(tagArray);

			foreach (var tag in tagArray) {
				var newTag = new TagPlate();
				newTag.TagLabel.Text = tag;
				newTag.TagLabel.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 255));
				newTag.TagBrd.Background = CatalogRoot.GetTagColor(tag);
				TagPanel.Children.Add(newTag);
			}

		}

	}

}

