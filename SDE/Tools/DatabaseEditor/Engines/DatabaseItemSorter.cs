using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using Database;
using TokeiLibrary.WPF.Styles.ListView;

namespace SDE.Tools.DatabaseEditor.Engines {
	public class DatabaseItemSorter : ListViewCustomComparer {
		private readonly AttributeList _list;
		private int _attributeSortIndex;

		public DatabaseItemSorter(AttributeList list) {
			_list = list;
		}

		public override void SetSort(string sortColumn, ListSortDirection dir) {
			try {
				base.SetSort(sortColumn, dir);
				_attributeSortIndex = _list.Attributes.IndexOf(_list.Attributes.FirstOrDefault(p => p.AttributeName == sortColumn || p.DisplayName == sortColumn));
			}
			catch { }
		}

		public override int Compare(object x, object y) {
			object xProp = ((Tuple)x).GetValue(_attributeSortIndex);
			object yProp = ((Tuple)y).GetValue(_attributeSortIndex);
			if (xProp == null || yProp == null)
				return 1;

			if (xProp is string || xProp is int || xProp is bool)
				return (_direction == ListSortDirection.Ascending ? 1 : -1) * Comparer.Default.Compare(xProp, yProp);

			return (_direction == ListSortDirection.Ascending ? 1 : -1) * Comparer.Default.Compare(xProp.ToString(), yProp.ToString());
		}
	}

	public class DatabaseItemSorter<T> : ListViewCustomComparer<T> where T : Tuple {
		private readonly AttributeList _list;
		private Func<T, int, int> _getGetIntDelegate;
		private Func<T, int, string> _getGetStringDelegate;
		private int _use;

		public DatabaseItemSorter(AttributeList list) {
			_list = list;
		}

		public override void SetSort(string sortColumn, ListSortDirection dir) {
			try {
				if (sortColumn != null) {
					base.SetSort(sortColumn, dir);

					DbAttribute attribute = _list.Attributes.First(p => p.AttributeName == sortColumn);

					_columnIndex = _list.Attributes.IndexOf(attribute);
					_use = attribute.DataType == typeof (int) ? 0 : 1;
					_sortColumn = sortColumn;

					if (_use == 0) {
						_getGetIntDelegate = (Func<T, int, int>)Delegate.CreateDelegate(typeof(Func<T, int, int>), typeof(T).GetMethod("GetIntValue"));
					}
					else if (_use == 1) {
						_getGetStringDelegate = (Func<T, int, string>)Delegate.CreateDelegate(typeof(Func<T, int, string>), typeof(T).GetMethod("GetStringValue"));
					}
				}
			}
			catch { }
		}

		public override int Compare(T x, T y) {
			if (_sortColumn == null)
				return 0;

			int res = 0;
			
			if (_use == 0) {
				int x1 = _getGetIntDelegate(x, _columnIndex);
				int y1 = _getGetIntDelegate(y, _columnIndex);

				return _direction == ListSortDirection.Ascending ? (x1 - y1) : (y1 - x1);
			}

			if (_use == 1) {
				res = String.Compare(_getGetStringDelegate(x, _columnIndex), _getGetStringDelegate(y, _columnIndex), StringComparison.InvariantCultureIgnoreCase);
			}

			return (_direction == ListSortDirection.Ascending ? 1 : -1) * res;
		}
	}
}
