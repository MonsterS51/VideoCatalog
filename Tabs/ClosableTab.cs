using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VideoCatalog.Main;
using VideoCatalog.Windows;

namespace VideoCatalog.Tabs {
	/// <summary>
	/// Author: Ed Fink
	/// https://www.codeproject.com/Articles/84213/How-to-add-a-Close-button-to-a-WPF-TabItem
	/// </summary>
	class ClosableTab : TabItem {

		private Button clsbtn;
		public Action<TabItem> onClose;

		public ClosableTab(string tabname, UserControl panel, Action<TabItem> onClose) {
			this.onClose = onClose;
			// формируем вкладку
			StackPanel headStkPan = new StackPanel { Orientation = Orientation.Horizontal };
			headStkPan.Children.Add(new TextBlock {
				Text = tabname,
				MaxWidth = 150,
				TextTrimming = TextTrimming.CharacterEllipsis,
				Padding = new Thickness(0.5f, 0.5f, 0.5f, 0.5f),
			});

			clsbtn = new Button() {
				Width = 14f,
				Height = 14f,
				Style = App.MainWindow.FindResource("btnIconStl") as Style,
				Content = new Image { Style = App.MainWindow.FindResource("imgIconStl") as Style },
				Padding = new Thickness(-2f),
				Margin = new Thickness(5f, 0f, 0f, 0f)
			};
			headStkPan.Children.Add(clsbtn);
			Header = headStkPan;

			Content = panel;             // установка содержимого вкладки

			MaxWidth = 400;
			Padding = new Thickness(0.5f);
			Style = App.MainWindow.FindResource("tabItemCustomStl") as Style;


			// установка действий
			clsbtn.MouseEnter += new MouseEventHandler(button_close_MouseEnter);
			clsbtn.MouseLeave += new MouseEventHandler(button_close_MouseLeave);
			clsbtn.Click += new RoutedEventHandler(button_close_Click);
			headStkPan.MouseDown += button_tab_Click;
		}


		#region Overrides

		protected override void OnSelected(RoutedEventArgs e) {
			base.OnSelected(e);
			clsbtn.Visibility = Visibility.Visible;
		}

		protected override void OnUnselected(RoutedEventArgs e) {
			base.OnUnselected(e);
			clsbtn.Visibility = Visibility.Hidden;
		}

		protected override void OnMouseEnter(MouseEventArgs e) {
			base.OnMouseEnter(e);
			clsbtn.Visibility = Visibility.Visible;
		}

		protected override void OnMouseLeave(MouseEventArgs e) {
			base.OnMouseLeave(e);
			if (!IsSelected) {
				clsbtn.Visibility = Visibility.Hidden;
			}
		}
		#endregion


		#region Event Handlers

		void button_close_MouseEnter(object sender, MouseEventArgs e) {
			//clsbtn.Foreground = Brushes.Red;
		}

		void button_close_MouseLeave(object sender, MouseEventArgs e) {
			//clsbtn.Foreground = Brushes.Black;
		}


		void button_close_Click(object sender, RoutedEventArgs e) {
			onClose?.Invoke(this);
		}

		private void button_tab_Click(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed) {
				button_close_Click(sender, e);
			}
		}
		#endregion
	}
}
