using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VideoCatalog.Main;
using VideoCatalog.Windows;

namespace VideoCatalog.Panels {
	/// <summary>
	/// Interaction logic for FilterPanel.xaml
	/// </summary>
	public partial class FilterPanel : UserControl {
		public FilterPanel() {
			InitializeComponent();
            FillSortCombo();
            sortModeComBox.SelectedIndex = 0;
        }

		private void FilterChangedAct(object sender, RoutedEventArgs e) {
            RaiseFilterChangedEvent();
        }

        private void FilterBoxClr(object sender, EventArgs e) {
            filterBox.Text = "";
        }

        ///<summary> Заполняем комбобокс режимов сортировки. </summary>
        public void FillSortCombo() {
            int tempSelInd = sortModeComBox.SelectedIndex;
            sortModeComBox.Items.Clear();

            List<string> itemNames = new List<string>() { "Name", "Add Date", "File Create Date", "File Modify Date" };
            foreach (var atrName in CatalogRoot.atrList) {
                itemNames.Add("Atribute " + atrName);
            }

            foreach (var name in itemNames) {
                var tb = new TextBlock();
                tb.Text = name;
                sortModeComBox.Items.Add(tb);
            }

            sortModeComBox.SelectedIndex = tempSelInd;
        }

        //---
        // Кастомный эвент на изменение фильтра
        //---

        // Create a custom routed event by first registering a RoutedEventID
        // This event uses the bubbling routing strategy
        public static readonly RoutedEvent FilterChangedEvent = EventManager.RegisterRoutedEvent(
            "OnFilterChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FilterPanel));

        // Provide CLR accessors for the event
        public event RoutedEventHandler OnFilterChanged {
            add { AddHandler(FilterChangedEvent, value); }
            remove { RemoveHandler(FilterChangedEvent, value); }
        }

        // This method raises the Tap event
        void RaiseFilterChangedEvent() {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(FilterPanel.FilterChangedEvent);
            RaiseEvent(newEventArgs);
        }


    }
}
