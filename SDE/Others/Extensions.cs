using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ErrorManager;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;

namespace SDE.Others {
	public static class Extensions {
		private static readonly Dictionary<RangeListView, object> _defaultSearches = new Dictionary<RangeListView, object>();

		public static DefaultComparer<T> BindDefaultSearch<T>(RangeListView lv, string id) {
			if (!_defaultSearches.ContainsKey(lv)) {
				_defaultSearches[lv] = new DefaultComparer<T>();
			}

			DefaultComparer<T> comparer = (DefaultComparer<T>) _defaultSearches[lv];
			lv.Dispatch(p => comparer.SetOrder(WpfUtils.GetLastGetSearchAccessor(lv) ?? id, WpfUtils.GetLastSortDirection(lv)));
			return comparer;
		}

		public static void InsertIntoList<T>(RangeListView lv, T item, IList<T> allItems) {
			if (!_defaultSearches.ContainsKey(lv)) {
				_defaultSearches[lv] = new DefaultComparer<T>();
			}

			DefaultComparer<T> comparer = (DefaultComparer<T>)_defaultSearches[lv];
			var index = allItems.ToList().BinarySearch(item, comparer);
			if (index < 0) index = ~index;
			allItems.Insert(index, item);
		}

		public static void SetMinimalSize(Window window) {
			window.Loaded += delegate {
				window.MinHeight = window.ActualHeight;
				window.MinWidth = window.ActualWidth;
			};
		}

		public static bool GetIntFromFloatValue(string text, out int ival) {
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

		public static void GenerateListViewTemplate(ListView list, ListViewDataTemplateHelper.GeneralColumnInfo[] columnInfos, ListViewCustomComparer sorter, IList<string> triggers,  params string[] extraCommands) {
			Gen1(list);
			ListViewDataTemplateHelper.GenerateListViewTemplateNew(list, columnInfos, sorter, triggers, extraCommands);
		}

		public static void Gen1(ListView list) {
			try {
				Style style = new Style();
				style.TargetType = typeof(ListViewItem);

				style.Setters.Add(new Setter(
					FrameworkElement.HorizontalAlignmentProperty,
					HorizontalAlignment.Left
					));
				style.Setters.Add(new Setter(
					Control.HorizontalContentAlignmentProperty,
					HorizontalAlignment.Stretch
					));

				list.ItemContainerStyle = style;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public static int ParseToInt(string text) {
			int value;

			if ((text.StartsWith("0x") || text.StartsWith("0X")) && text.Length > 2) {
				value = Convert.ToInt32(text, 16);
			}
			else {
				Int32.TryParse(text, out value);
			}

			return value;
		}
	}
}
