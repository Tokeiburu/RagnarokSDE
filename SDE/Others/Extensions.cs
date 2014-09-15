using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Database;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;

namespace SDE.Others {
	public static class Extensions {
		private static readonly Dictionary<RangeListView, object> _defaultSearches = new Dictionary<RangeListView, object>();

		public static List<T> RemoveAt<T>(this IEnumerable<T> list, int indexToRemove) {
			List<T> items = list.ToList();
			items.RemoveAt(indexToRemove);
			return items;
		}

		public static List<T> RemoveAt<T>(this IEnumerable<T> list, DbAttribute attribute) {
			List<T> items = list.ToList();
			items.RemoveAt(attribute.Index);
			return items;
		}

		public static DefaultComparer<T> BindDefaultSearch<T>(RangeListView lv, string id) {
			if (!_defaultSearches.ContainsKey(lv)) {
				_defaultSearches[lv] = new DefaultComparer<T>();
			}

			DefaultComparer<T> comparer = (DefaultComparer<T>) _defaultSearches[lv];
			lv.Dispatch(p => comparer.SetOrder(WpfUtils.GetLastGetSearchAccessor(lv) ?? id, WpfUtils.GetLastSortDirection(lv)));
			return comparer;
		}

		public static void SetMinimalSize(Window window) {
			window.Loaded += delegate {
				window.MinHeight = window.ActualHeight;
				window.MinWidth = window.ActualWidth;
			};
		}

		public static bool GetValue(string text, out int ival) {
			float fval;

			if (Int32.TryParse(text, out ival)) {
				return true;
			}

			string tdot = text.Replace(",", ".");

			if (float.TryParse(tdot, out fval)) {
				ival = (int) (fval * 100);
				return true;
			}

			string tcomma = text.Replace(".", ",");

			if (float.TryParse(tcomma, out fval)) {
				ival = (int)(fval * 100);
				return true;
			}

			ival = 0;
			return false;
		}
	}
}
