using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Database;
using ErrorManager;
using SDE.WPF;
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

		public static void GenerateListViewTemplate(ListView list, ListViewDataTemplateHelper.GeneralColumnInfo[] columnInfos, ListViewCustomComparer sorter, IList<string> triggers,  params string[] extraCommands) {
			Gen1(list);

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(list, columnInfos, sorter, triggers, extraCommands);
			GridView grid = (GridView)list.View;

			if (grid.Columns.Count > 0 && columnInfos.Any(p => p.IsFill)) {
				Style style = list.ItemContainerStyle;
				GridViewColumn lastColumn = null;

				for (int i = 0; i < columnInfos.Length; i++) {
					if (columnInfos[i].IsFill) {
						lastColumn = grid.Columns[i];
						break;
					}
				}

				if (lastColumn == null) {
					return;
				}

				style.Setters.Add(new Setter(
										FrameworkElement.WidthProperty,
										new Binding("ActualWidth") {
											Source = lastColumn,
											Converter = new ListViewWItemWidthConverter(list.BorderThickness),
											ConverterParameter = list
										}));
			}
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

		public static void Gen2(ListView list) {
			try {
				Style style = list.ItemContainerStyle;
				GridView grid = (GridView)list.View;
				var lastColumn = grid.Columns.Last();

				style.Setters.Add(new Setter(
					FrameworkElement.WidthProperty,
					new Binding("ActualWidth") {
						Source = lastColumn,
						Converter = new ListViewWItemWidthConverter(),
						ConverterParameter = list
					}));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}
