using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace VideoCatalog.Panels {

	public partial class SettingsPanel : UserControl {
		public SettingsPanel() {
			InitializeComponent();
			LoadSettings();
		}

		private void LoadSettings() {
			CB_previewMode.SelectedValue = Properties.Settings.Default.PreviewMode;
		}

		private void Accept_Click(object sender, RoutedEventArgs e) {
			Properties.Settings.Default.PreviewMode = CB_previewMode.SelectedValue as string;
			Properties.Settings.Default.Save();

		}
	}
}
