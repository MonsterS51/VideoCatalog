using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using VideoCatalog.Windows;

namespace VideoCatalog.Panels {

	public partial class SettingsPanel : UserControl {
		public SettingsPanel() {
			InitializeComponent();
			LoadSettings();
		}


		private void LinkOnRequestNavigate(object sender, RequestNavigateEventArgs e) {
			System.Diagnostics.Process.Start(e.Uri.ToString());
			e.Handled = true;
		}

		///<summary> Подгрузка настроек в форму. </summary>
		private void LoadSettings() {
			PreviewEnabler.IsChecked = Properties.Settings.Default.PreviewEnabled;
			CheckBox_Click(null, null);

			CB_previewMode.SelectedValue = Properties.Settings.Default.PreviewMode;
			ffmpegPath.Text = Properties.Settings.Default.FFMpegBinPath;
			CB_previewMode_SelectionChanged(null, null);

			PreviewSteps.Value = Properties.Settings.Default.PreviewSteps;
			PreviewTime.Value = Properties.Settings.Default.PreviewTime;

			CoverMaxSize.Value = Properties.Settings.Default.CoverMaxSize;

			AspectRatio.Value = Properties.Settings.Default.CoverAspectRatio;
		}

		///<summary> Принятие изменений в настройках. </summary>
		private void Accept_Click(object sender, RoutedEventArgs e) {

			// preview
			//---

			Properties.Settings.Default.PreviewEnabled = (bool) PreviewEnabler.IsChecked;

			Properties.Settings.Default.PreviewMode = CB_previewMode.SelectedValue as string;

			if (new DirectoryInfo(ffmpegPath.Text).Exists) { 
				Properties.Settings.Default.FFMpegBinPath = ffmpegPath.Text;
				App.LoadFFMpegLibs();
			}


			Properties.Settings.Default.PreviewSteps = (int) PreviewSteps.Value;
			Properties.Settings.Default.PreviewTime = (int) PreviewTime.Value;

			Properties.Settings.Default.CoverMaxSize = (int) CoverMaxSize.Value;

			Properties.Settings.Default.CoverAspectRatio = (decimal) AspectRatio.Value;

			//---

			Properties.Settings.Default.Save();
		}

		///<summary> Нажатие кнопки вызова диалога выбора папки с библиотеками FFMpeg. </summary>
		private void btnSelectFFMpegPath_Click(object sender, RoutedEventArgs e) {
			using (CommonOpenFileDialog dialog = new CommonOpenFileDialog()) {
				dialog.Title = "Select bin folder of ffmpeg libs:";
				dialog.IsFolderPicker = true;
				if (dialog.ShowDialog() == CommonFileDialogResult.Ok && !string.IsNullOrWhiteSpace(dialog.FileName)) {
					ffmpegPath.Text = dialog.FileName;
				}
			}
		}

		///<summary> Скрытие/отображение настройки пути до библиотек FFMpeg. </summary>
		private void CB_previewMode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (ffmePathOpt == null) return;
			if (CB_previewMode.SelectedValue as string == "FFME") ffmePathOpt.Visibility = Visibility.Visible;
			else ffmePathOpt.Visibility = Visibility.Collapsed;
		}

		///<summary> Скрытие/отображение всех настроек предпросмотра. </summary>
		private void CheckBox_Click(object sender, RoutedEventArgs e) {
			if (PreviewOpts == null) return;
			if ((bool) PreviewEnabler.IsChecked) PreviewOpts.Visibility = Visibility.Visible;
			else PreviewOpts.Visibility = Visibility.Collapsed;
		}





		//---B

		#region Context Menu

		private string comString = "Open with VidCat";

		///<summary> Добавляет контекстное меню открытия папки через реестр. </summary>
		private void AddContextActionToReg(object sender, RoutedEventArgs e) {
			string exePath = "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"";

			//? один вариант для меню иконки папки, другой для меню по полю папки

			Microsoft.Win32.RegistryKey classShell_0 = Microsoft.Win32.Registry.LocalMachine.
				OpenSubKey("Software")?.
				OpenSubKey("Classes")?.
				OpenSubKey("Directory")?.
				OpenSubKey("Background")?.
				OpenSubKey("shell", true);

			if (classShell_0 != null) {
				Microsoft.Win32.RegistryKey rootKey_0 = classShell_0.CreateSubKey(comString);
				rootKey_0.SetValue("Icon", $"{exePath},0");
				Microsoft.Win32.RegistryKey comKey_0 = rootKey_0.CreateSubKey("command");
				comKey_0.SetValue(null, $"{exePath} \"%V\"");
				rootKey_0.Close();
			}

			//?---------------------------------------------------------------

			Microsoft.Win32.RegistryKey classShell_1 = Microsoft.Win32.Registry.LocalMachine.
				OpenSubKey("Software")?.
				OpenSubKey("Classes")?.
				OpenSubKey("Directory")?.
				OpenSubKey("shell", true);

			if (classShell_1 != null) {
				Microsoft.Win32.RegistryKey rootKey_1 = classShell_1.CreateSubKey(comString);
				rootKey_1.SetValue("Icon", $"{exePath},0");
				Microsoft.Win32.RegistryKey comKey_1 = rootKey_1.CreateSubKey("command");
				comKey_1.SetValue(null, $"{exePath} \"%1\"");
				rootKey_1.Close();
			}


		}

		///<summary> Удаление контекстного меню открытия папки через реестр. </summary>
		private void RemoveContextActionFromReg(object sender, RoutedEventArgs e) {
			try {
				Microsoft.Win32.RegistryKey classShell_0 = Microsoft.Win32.Registry.LocalMachine.
					OpenSubKey("Software")?.
					OpenSubKey("Classes")?.
					OpenSubKey("Directory")?.
					OpenSubKey("Background")?.
					OpenSubKey("shell", true);

				classShell_0?.DeleteSubKeyTree(comString);

				Microsoft.Win32.RegistryKey classShell_1 = Microsoft.Win32.Registry.LocalMachine.
					OpenSubKey("Software")?.
					OpenSubKey("Classes")?.
					OpenSubKey("Directory")?.
					OpenSubKey("shell", true);

				classShell_1?.DeleteSubKeyTree(comString);
			} catch (Exception ex) {
				Console.WriteLine(""+ex);
			}
		}
		#endregion

	}
}
