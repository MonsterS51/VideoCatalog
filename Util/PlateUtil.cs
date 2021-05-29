using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using VideoCatalog.Main;
using VideoCatalog.Windows;

namespace VideoCatalog.Util {
	public static class PlateUtil {

		///<summary> Контекстное меню для плиток альбомов. </summary>
		public static void RmbMenuOpen(object sender, object DataContext) {
			var cm = new ContextMenu();

			var mOpenInExp = new MenuItem();
			mOpenInExp.Header = "Open in Explorer";
			mOpenInExp.Click += (s, ea) => { MenuItem_OpenInExplorer(DataContext); };
			cm.Items.Add(mOpenInExp);

			var mUpdCov = new MenuItem();
			mUpdCov.Header = "Update Cover";
			mUpdCov.Click += (s, ea) => { MenuItem_UpdateCoverArt(DataContext); };
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
			if (App.MainWin.GetCurrentAlbumePanel().IsContainGroups()) {
				cm.Items.Add(new Separator());

				var mCollAll = new MenuItem();
				mCollAll.Header = "Collapse Groups";
				mCollAll.Click += (s, ea) => { App.MainWin.GetCurrentAlbumePanel().SetVisibilityOfAllGroups(Visibility.Collapsed); };
				cm.Items.Add(mCollAll);

				var mExpAll = new MenuItem();
				mExpAll.Header = "Expand Groups";
				mExpAll.Click += (s, ea) => { App.MainWin.GetCurrentAlbumePanel().SetVisibilityOfAllGroups(Visibility.Visible); };
				cm.Items.Add(mExpAll);
			}

			cm.PlacementTarget = sender as Button;
			cm.IsOpen = true;
		}

		/// <summary> Открытие в проводнике по привязанному DataContext. </summary>
		private static void MenuItem_OpenInExplorer(object DataContext) {
			if (DataContext != null) {
				(DataContext as AbstractEntry)?.OpenInExplorer();
			}
		}

		/// <summary> Обновление обложек альбома/эпизода. </summary>
		private static void MenuItem_UpdateCoverArt(object DataContext) {
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

	}
}
