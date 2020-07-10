using System;

namespace VideoCatalog.Util {
    public static class StringExtensions {

        ///<summary> Сравнение строк без учета регистра букв. </summary>
        public static bool ContainsIC(this string source, string searchStr, StringComparison comp = StringComparison.CurrentCultureIgnoreCase) {
            return source?.IndexOf(searchStr, comp) >= 0;
        }

        public static bool ContainsIC(this string[] source, string searchStr, StringComparison comp = StringComparison.CurrentCultureIgnoreCase) {
            foreach (var str in source) {
                if (str.ContainsIC(searchStr, comp)) {
                    return true;
                }
            }
            return false;
        }

    }
}