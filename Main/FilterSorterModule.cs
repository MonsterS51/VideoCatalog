using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VideoCatalog.Util;

namespace VideoCatalog.Main {
	public class FilterSorterModule {
		IEnumerable<AbstractEntry> entList;

		public enum SortMode{
			NAME,
			CREATE_DATE,
			CREATE_DATE_FILE,
			MODIF_DATE_FILE,
		}



		public FilterSorterModule(IEnumerable<AbstractEntry> EntList) {
			entList = EntList;
		}

		public List<AbstractEntry> FilterByName(string filterStr, List<string> allTags, SortMode sortMode = SortMode.NAME, bool ascend = true, bool broken = false) {
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
			switch(sortMode) {
              	case SortMode.NAME: {
					if (ascend) resultList = resultList.OrderBy(o => o.Name, new AlphanumComparatorFast()).ToList();
					else resultList = resultList.OrderByDescending(o => o.Name, new AlphanumComparatorFast()).ToList();
					break;
              	}
				case SortMode.CREATE_DATE_FILE: {
					if (ascend) resultList = resultList.OrderBy(o => o.GetDateCreate()).ToList();
					else resultList = resultList.OrderByDescending(o => o.GetDateCreate(true)).ToList();
					break;
				}
				case SortMode.MODIF_DATE_FILE: {
					if (ascend) resultList = resultList.OrderBy(o => o.GetDateModify()).ToList();
					else resultList = resultList.OrderByDescending(o => o.GetDateModify(true)).ToList();
					break;
				}
				default:{
              		break;
              	}
            }





			return resultList;
		}




		///<summary> Цифро-буквенный сортировщик. </summary>
		public class AlphanumComparatorFast : IComparer<string> {
			public int Compare(string x, string y) {
				string s1 = x as string;
				if (s1 == null) {
					return 0;
				}
				string s2 = y as string;
				if (s2 == null) {
					return 0;
				}

				int len1 = s1.Length;
				int len2 = s2.Length;
				int marker1 = 0;
				int marker2 = 0;

				// Walk through two the strings with two markers.
				while (marker1 < len1 && marker2 < len2) {
					char ch1 = s1[marker1];
					char ch2 = s2[marker2];

					// Some buffers we can build up characters in for each chunk.
					char[] space1 = new char[len1];
					int loc1 = 0;
					char[] space2 = new char[len2];
					int loc2 = 0;

					// Walk through all following characters that are digits or
					// characters in BOTH strings starting at the appropriate marker.
					// Collect char arrays.
					do {
						space1[loc1++] = ch1;
						marker1++;

						if (marker1 < len1) {
							ch1 = s1[marker1];
						} else {
							break;
						}
					} while (char.IsDigit(ch1) == char.IsDigit(space1[0]));

					do {
						space2[loc2++] = ch2;
						marker2++;

						if (marker2 < len2) {
							ch2 = s2[marker2];
						} else {
							break;
						}
					} while (char.IsDigit(ch2) == char.IsDigit(space2[0]));

					// If we have collected numbers, compare them numerically.
					// Otherwise, if we have strings, compare them alphabetically.
					string str1 = new string(space1);
					string str2 = new string(space2);

					int result;

					if (char.IsDigit(space1[0]) && char.IsDigit(space2[0])) {
						int.TryParse(str1, out int thisNumericChunk);
						int.TryParse(str2, out int thatNumericChunk);
						result = thisNumericChunk.CompareTo(thatNumericChunk);
					} else {
						result = str1.CompareTo(str2);
					}

					if (result != 0) {
						return result;
					}
				}
				return len1 - len2;
			}
		}

	}
}
