﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using VideoCatalog.Panels;
using YAXLib;

namespace VideoCatalog.Main {

	public class AbstractEntry : INotifyPropertyChanged {
		private string _name;
		public string Name { get { return _name; } set { _name = value; OnPropertyChanged("Name"); } }
		private string _descr;
		public string Descr { get { return _descr; } set { _descr = value; OnPropertyChanged("Descr"); } }
		private string _relPath;
		public string RelPath { get { return _relPath; } set { _relPath = value; OnPropertyChanged("RelPath"); } }

		//---

		[YAXDontSerialize]
		public virtual CatalogEntry BaseEntry { get { return this as CatalogEntry; } }

		//---

		///<summary> Элемент каталога скрыт и не обрабатывается. </summary>
		public bool IsExcepted { get; set; } = false;

		[YAXFormat("d")]
		public DateTime DateAdded { get; set; } = DateTime.Today;

		[YAXDontSerialize]
		public DateTime DateMod { get { return GetDateModify().Date; } }

		[YAXDontSerialize]
		public DateTime DateCreated { get { return GetDateCreate().Date; } }

		//---

		private string _tagStr = "";

		///<summary> Объект тэгов для сериализации и UI. </summary>
		public string TagStr { get { return _tagStr; } set { _tagStr = value; OnPropertyChanged("TagStr"); } }

		///<summary> Получить список тэгов. </summary>
		public virtual string[] GetTagList() {
			if (string.IsNullOrWhiteSpace(_tagStr)) return new string[0];
			return _tagStr.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(tag => tag.Trim().ToLower()).ToArray();
		}

		///<summary> Добавление тэга. </summary>
		public void AddTag(string tag) {
			if (_tagStr.Length == 0) _tagStr = _tagStr + tag;
			else _tagStr = _tagStr + "; " + tag;
			OnPropertyChanged("TagStr");
		}

		//---

		///<summary> Хранилище атрибутов элемента каталога. </summary>
		public ObservableCollection<AtrEnt> AtrMap { get; set; } = new ObservableCollection<AtrEnt>();

		///<summary> Получить значение атрибута. При отсутствии - возвращает строку "<null>". </summary>
		public string GetAttribute(string atrName) {
			var atrData = AtrMap.FirstOrDefault(atrEnt => atrEnt.AtrName == atrName)?.AtrData;
			if (atrData != null) return atrData;
			return "<null>";
		}

		///<summary> Объект данных атрибута. </summary>
		public class AtrEnt {

			public AtrEnt() {
			}

			public AtrEnt(string AtrName, string AtrData) {
				this.AtrName = AtrName;
				this.AtrData = AtrData;
			}

			public string AtrName { get; set; }
			public string AtrData { get; set; }
		}

		//---

		protected BitmapImage _coverImage = null;

		[YAXDontSerialize]
		public BitmapImage CoverImage { get { return _coverImage; } set { _coverImage = null; _coverImage = value; OnPropertyChanged("CoverImage"); } }

		//---

		private string _topRightText;
		private string _atrText;

		[YAXDontSerialize]
		public string TopRightText { get { return _topRightText; } set { _topRightText = value; OnPropertyChanged("TopRightText"); } }

		[YAXDontSerialize]
		public string AtrText { get { return _atrText; } set { _atrText = value; OnPropertyChanged("AtrText"); } }

		///<summary> Обновление строки с атрибутами. </summary>
		public void UpdateAtrText() {
			AtrText = "";
			foreach (var atrName in Properties.Settings.Default.AtrToShowList.Split(';', ',')) {
				var atr = GetAttribute(atrName);
				if (atr != "<null>") {
					if (string.IsNullOrWhiteSpace(AtrText)) AtrText = $"{atrName}: {atr}";
					else AtrText = AtrText + $"   {atrName}: {atr}";
				}
			}
			AtrText.Trim();
		}

		//---

		///<summary> Строка для отображения в справочной панельке. </summary>
		public string sortHelper = "";

		//---

		#region Геттеры

		///<summary> Возвращает самую раннюю(позднюю) дату создания файла из всех входящих элементов. </summary>
		public virtual DateTime GetDateCreate(bool byLatest = false) {
			return DateTime.Now;
		}

		///<summary> Возвращает самую раннюю(позднюю) дату изменения файла из всех входящих элементов. </summary>
		public virtual DateTime GetDateModify(bool byLatest = false) {
			return DateTime.Now;
		}

		///<summary> Возвращает дату добавления элемента в каталог. </summary>
		public virtual DateTime GetDateAdded() {
			return DateAdded;
		}

		///<summary> Получить максимальное качество из всех элементов альбома. </summary>
		public virtual CatalogEntry.VideoResolution GetMaxRes() {
			return CatalogEntry.VideoResolution.LQ;
		}

		///<summary> Получить число входящих элементов. </summary>
		public virtual int GetEntrysCount() { return 0; }

		#endregion Геттеры

		//---

		#region Плитки

		public ViewPlate vp;
		public ListPlate lp;

		/// <summary> Создание плэйта эпизода. </summary>
		public virtual ViewPlate CreatePlate() {
			if (vp == null) vp = new ViewPlate();
			return vp;
		}

		/// <summary> Создание списочного плэйта эпизода. </summary>
		public virtual ListPlate CreateListPlate() {
			if (lp == null) lp = new ListPlate();
			return lp;
		}

		private int _h = 25;

		[YAXDontSerialize]
		public int ListHeight { get { return _h; } set { _h = value; OnPropertyChanged("ListHeight"); } }

		public bool isBroken = false;

		protected void UpdateIconBrokenState() {
			if (vp != null) {
				if (isBroken) vp.brokenIcon.Visibility = System.Windows.Visibility.Visible;
				else vp.brokenIcon.Visibility = System.Windows.Visibility.Collapsed;
			}
			if (lp != null) {
				if (isBroken) lp.brokenIcon.Visibility = System.Windows.Visibility.Visible;
				else lp.brokenIcon.Visibility = System.Windows.Visibility.Collapsed;
			}
		}

		#endregion Плитки

		//---
		///<summary> Открыть место хранения файла элемента. </summary>
		public virtual void OpenInExplorer() { }

		public void PasteEntData(AbstractEntry srcEnt) {
			if (srcEnt == null) return;

			Name = srcEnt.Name;
			Descr = srcEnt.Descr;
			TagStr = srcEnt.TagStr;

			AtrMap.Clear();
			foreach (var atrEnt in srcEnt.AtrMap) {
				AtrMap.Add(new AtrEnt(atrEnt.AtrName, atrEnt.AtrData));
			}
		}

		//---

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string prop = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}
	}
}