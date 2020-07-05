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

namespace VideoCatalog.Windows {
	/// <summary>
	/// Interaction logic for FilterPanel.xaml
	/// </summary>
	public partial class FilterPanel : UserControl {
		public FilterPanel() {
			InitializeComponent();
			this.DataContext = this;
		}

		private void FilterChangedAct(object sender, RoutedEventArgs e) {
            RaiseFilterChangedEvent();
        }

        private void FilterBoxClr(object sender, EventArgs e) {
            filterBox.Text = "";
        }

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
