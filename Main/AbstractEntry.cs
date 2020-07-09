using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using VideoCatalog.Windows;
using YAXLib;

namespace VideoCatalog.Main {
	public class AbstractEntry : INotifyPropertyChanged {
		private string _name;
		public string Name { get { return _name; } set { _name = value; OnPropertyChanged("Name"); } }
		private string _descr;
		public string Descr { get { return _descr; } set { _descr = value; OnPropertyChanged("Descr"); } }

		[YAXDontSerializeIfNull]
		public DateTime ProdTime { get; set; } = new DateTime(1800, 01, 01);

		//---

		public string TagStr { get; set; } = "";
		[YAXDontSerialize]
		public string[] TagList {
			get {
				if (string.IsNullOrWhiteSpace(TagStr)) return new string[0];
				return TagStr.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(tag => tag.Trim().ToLower()).ToArray();
			}
		}

		//---

		protected BitmapImage _coverImage = null;
		[YAXDontSerialize]
		public BitmapImage CoverImage { get { return _coverImage; } set { _coverImage = null; _coverImage = value; OnPropertyChanged("CoverImage"); } }

		//---

		[YAXDontSerialize]
		private string _topRightText;
		[YAXDontSerialize]
		public string TopRightText { get { return _topRightText; } set { _topRightText = value; OnPropertyChanged("TopRightText"); } }

		//---

		public bool isBroken = false;

		//---
		
		///<summary> Возвращает самую раннюю(позднюю) дату создания файла из всех входящих элементов. </summary>
		public virtual DateTime GetDateCreate(bool byLatest = false) {
			return DateTime.Now;
		}

		///<summary> Возвращает самую раннюю(позднюю) дату изменения файла из всех входящих элементов. </summary>
		public virtual DateTime GetDateModify(bool byLatest = false) {
			return DateTime.Now;
		}

		//---

		public ViewPlate vp;
		/// <summary> Создание плэйта эпизода. </summary>
		public virtual ViewPlate CreatePlate() {
			if (vp == null) vp = new ViewPlate();
			return vp;
		}

		protected void UpdateIconBrokenState() {
			if (vp != null) {
				if (isBroken) vp.brokenIcon.Visibility = System.Windows.Visibility.Visible;
				else vp.brokenIcon.Visibility = System.Windows.Visibility.Collapsed;
			}
		}

		//---
		
		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged([CallerMemberName]string prop = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}
	}
}
