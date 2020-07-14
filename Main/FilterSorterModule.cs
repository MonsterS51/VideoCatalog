using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VideoCatalog.Util;

namespace VideoCatalog.Main {
	public class FilterSorterModule {

		public enum SortMode{
			NAME,
			DATE_ADD,
			DATE_CREATE,
			DATE_MODIFIED,
			ATTRIBUTE
		}

		///<summary> Фильтрация и сортировка списка элементов. </summary>
		public static List<AbstractEntry> FilterAndSort(
				IEnumerable<AbstractEntry> entList,
				string filterStr, 
				List<string> allTags, 
				SortMode sortMode = SortMode.NAME, 
				bool ascend = true, 
				bool broken = false, 
				string atrName = "") {

			if (entList == null) return new List<AbstractEntry>();

			//+ вычленяем теги
			string tempStr = filterStr;
			List<string> incTag = new List<string>();   // * обязательные теги (есть все)
			List<string> optTag = new List<string>();   // + частичные теги (есть хотя бы один из)
			List<string> excTag = new List<string>();   // - исключенные теги (нет всех)


			//+ подбор известных тегов
			// нагорожено в основном для тегов из нескольких слов
			foreach (var tag in allTags) {
				if (filterStr.ContainsIC("+"+ tag)) {
					optTag.Add(tag);
					tempStr = Regex.Replace(tempStr, @"\+" + tag, "", RegexOptions.IgnoreCase);		// замена с игнором регистра
				} else
				if (filterStr.ContainsIC("*" + tag)) {
					incTag.Add(tag);
					tempStr = Regex.Replace(tempStr, @"\*" + tag, "", RegexOptions.IgnoreCase);
				} else
				if (filterStr.ContainsIC("-" + tag)) {
					excTag.Add(tag);
					tempStr = Regex.Replace(tempStr, @"\-" + tag, "", RegexOptions.IgnoreCase);
				}
			}

			//+ подбор неизвестных тегов
			var rawWords = tempStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
			foreach (var word in rawWords) {
				if (word.Length > 1) {
					if (word.StartsWith("*")) {
						incTag.Add(word.Substring(1).ToLower());
						tempStr = tempStr.Replace(word, "");
					} else
					if (word.StartsWith("+")) {
						optTag.Add(word.Substring(1).ToLower());
						tempStr = tempStr.Replace(word, "");
					} else
					if (word.StartsWith("-")) {
						excTag.Add(word.Substring(1).ToLower());
						tempStr = tempStr.Replace(word, "");
					}
				}
			}


			tempStr = tempStr.Trim();   // строка поиска без тегов
			Console.WriteLine("tempStr " + tempStr);
			Console.WriteLine("incTag " + string.Join(",", incTag));
			Console.WriteLine("optTag " + string.Join(",", optTag)); 
			Console.WriteLine("excTag " + string.Join(",", excTag));

			//+ фильтрация по имени или его части с отбросом мещающих знаков
			List<AbstractEntry> resultList;
			if (!string.IsNullOrWhiteSpace(tempStr)) {
				resultList = new List<AbstractEntry>();
				foreach (var ent in entList) {

					// проверяем имя с различными комбинациями разделителей
					bool nameContain = (ent.Name.ContainsIC(tempStr) ||
						ent.Name.Replace(' ', '_').ContainsIC(tempStr) ||
						ent.Name.Replace('_', ' ').ContainsIC(tempStr) ||
						ent.Name.Replace(" ", "").ContainsIC(tempStr) ||
						ent.Name.Replace("_", "").ContainsIC(tempStr)
						);
					if (!nameContain) continue;

					resultList.Add(ent);

				}
			} else {
				resultList = new List<AbstractEntry>(entList);
			}

			//+ только сломанные
			if (broken) {
				foreach (var ent in resultList.ToArray()) {
					if (!ent.isBroken) resultList.Remove(ent);
				}
			}


			//+ фильтруем по тегам
			if (incTag.Count > 0 | excTag.Count > 0 | optTag.Count > 0) {
				foreach (var filtEnt in resultList.ToArray()) {
					var entTagArr = filtEnt.TagList;
					Console.WriteLine(filtEnt.Name + " " + string.Join(",", entTagArr));

					if (optTag.Count > 0) {
						// не проходит по частичным тегам
						if (!entTagArr.Any(optTag.Contains)) resultList.Remove(filtEnt);
					}

					if (incTag.Count > 0) {
						// не проходит по обязательным тегам
						if (! incTag.All(entTagArr.Contains)) resultList.Remove(filtEnt);
					}

					if (excTag.Count > 0) {
						// проходит по исключающим тегам
						if (entTagArr.Any(excTag.Contains)) resultList.Remove(filtEnt);
					}
				}
			}


			//+ сортировка	
			// здесь же заполняем метки для отображения в справочной плашке при скролле
			switch (sortMode) {
              	case SortMode.NAME: {
					resultList = resultList.OrderBy(o => o.Name, new AlphanumComparatorFast()).ToList();
					if (!ascend) resultList.Reverse();

					foreach (var ent in resultList) {
						ent.sortHelper = ent.Name.First().ToString().ToUpper();
					}
					break;
              	}
				case SortMode.DATE_CREATE: {
					resultList = resultList.OrderBy(o => o.GetDateCreate(!ascend)).ToList();
					if (!ascend) resultList.Reverse();

					foreach (var ent in resultList) {
						ent.sortHelper = ent.GetDateCreate(!ascend).ToString("d");
					}
					break;
				}
				case SortMode.DATE_MODIFIED: {
					resultList = resultList.OrderBy(o => o.GetDateModify(!ascend)).ToList();
					if (!ascend) resultList.Reverse();

					foreach (var ent in resultList) {
						ent.sortHelper = ent.GetDateModify(!ascend).ToString("d");
					}
					break;
				}
				case SortMode.ATTRIBUTE: {
					resultList = resultList.OrderBy(o => o.GetAttribute(atrName)).ToList();
					if (!ascend) resultList.Reverse();

					foreach (var ent in resultList) {
						ent.sortHelper = ent.GetAttribute(atrName);
					}
					break;
				}
				default:{
              		break;
              	}
            }

			return resultList;
		}

		///<summary> Режимы группировки элементов каталога. </summary>
		public enum GroupModes {
			NONE,
			FIRST_CHAR,
			FOLDER,
			DATE_ADD,
			DATE_CREATE,
			DATE_MODIFIED,
			ATTRIBUTE
		}

		///<summary> Формирование групп для списка элементов. </summary>
		public static List<KeyValuePair<string, List<AbstractEntry>>> GroupProcess(
			IEnumerable<AbstractEntry> srcList, 
			GroupModes grpMode, 
			bool ascend = true,
			string atrName = "") {

			var subListsMap = new SortedDictionary<string, List<AbstractEntry>>(new AlphanumComparatorFast());
			var readyMap = new List<KeyValuePair<string, List<AbstractEntry>>>();

			//? по умолчанию - сортировка Alphanumeric, но для дат - сортируем вручную через парсинг.

			switch (grpMode) {
				case GroupModes.NONE: {
					subListsMap.Add("", srcList as List<AbstractEntry>);

					readyMap = subListsMap.ToList();
					break;
				}
				case GroupModes.FIRST_CHAR: {

					foreach (var ent in srcList) {
						var ch = ent.Name.First().ToString().ToUpper();
						if (!subListsMap.ContainsKey(ch)) subListsMap.Add(ch, new List<AbstractEntry>());
						subListsMap[ch].Add(ent);
					}

					readyMap = subListsMap.ToList();
					break;
				}
				case GroupModes.DATE_CREATE: {
					foreach (var ent in srcList) {
						var date = ent.GetDateCreate(!ascend).ToString("d");
						if (!subListsMap.ContainsKey(date)) subListsMap.Add(date, new List<AbstractEntry>());					
						subListsMap[date].Add(ent);
					}

					// правильная сортировка дат
					readyMap = subListsMap.ToList().OrderBy(kv => DateTime.Parse(kv.Key)).ToList();
					break;
				}
				case GroupModes.DATE_MODIFIED: {
					foreach (var ent in srcList) {
						var date = ent.GetDateModify(!ascend).ToString("d");
						if (!subListsMap.ContainsKey(date)) subListsMap.Add(date, new List<AbstractEntry>());
						subListsMap[date].Add(ent);
					}

					// правильная сортировка дат
					readyMap = subListsMap.ToList().OrderBy(kv => DateTime.Parse(kv.Key)).ToList();
					break;
				}
				case GroupModes.ATTRIBUTE: {
					foreach (var ent in srcList) {
						var atrData = ent.GetAttribute(atrName);
						if (!subListsMap.ContainsKey(atrData)) subListsMap.Add(atrData, new List<AbstractEntry>());
						subListsMap[atrData].Add(ent);
					}

					readyMap = subListsMap.ToList();
					break;
				}
				case GroupModes.FOLDER: {
					// не работает для корневого 
					if (!(srcList.FirstOrDefault() is CatalogEntry)) goto case GroupModes.NONE;

					foreach (var ent in srcList) {
						var catEnt = ent as CatalogEntry;
					
						// отрезаем путь от папки альбома
						string subPath = catEnt.EntAbsFile.Directory.FullName.Replace(catEnt.catAlb.AlbAbsPath, "").TrimStart('/', '\\');

						if (!subListsMap.ContainsKey(subPath))subListsMap.Add(subPath, new List<AbstractEntry>());
						subListsMap[subPath].Add(ent);
					}

					readyMap = subListsMap.ToList();
					break;
				}
				default: {
					break;
				}
			}

			// обращаем порядок
			if (!ascend) readyMap.Reverse();

			subListsMap.Clear();

			return readyMap;
		}



	}
}
