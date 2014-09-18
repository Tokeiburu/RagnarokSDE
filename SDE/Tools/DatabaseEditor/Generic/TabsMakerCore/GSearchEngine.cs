using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Database;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Engines;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;

namespace SDE.Tools.DatabaseEditor.Generic.TabsMakerCore {
	public partial class GSearchEngine<TKey, TValue> where TValue : Tuple {
		#region Delegates

		public delegate void CDEEventHandler(object sender, List<TValue> modified);

		#endregion

		private readonly object _filterLock = new object();
		private readonly GSearchSettings _itemsSearchSettings;
		private readonly GTabSettings<TKey, TValue> _settings;
		private readonly Dictionary<DbAttribute, bool> _states = new Dictionary<DbAttribute, bool>();
		private DbAttribute[] _attributes;
		private ComboBox _cbSearchItemsMode;
		private DatabaseItemSorter<TValue> _entryComparer;
		private ListView _items;
		private bool _searchFirstTimeSet;
		private string _searchItemsFilter = "";
		private Table<TKey, TValue> _table;
		private TextBox _tbItemsRange;
		private TextBox _tbSearchItems;

		public GSearchEngine(string tabName, GTabSettings<TKey, TValue> settings) {
			_settings = settings;
			_itemsSearchSettings = new GSearchSettings(SDEConfiguration.ConfigAsker, tabName);
		}

		public Func<TValue, bool> SubsetCondition { get; set; }
		public Action<TValue> SetupImageDataGetter { get; set; }

		public RangeObservableCollection<TValue> Collection {
			get { return _items.ItemsSource as RangeObservableCollection<TValue>; }
		}

		public RangeObservableCollection<TValue> CollectionSafe {
			get { return (RangeObservableCollection<TValue>) _items.Dispatcher.Invoke(new Func<RangeObservableCollection<TValue>>(() => (RangeObservableCollection<TValue>)_items.ItemsSource)); }
		}

		public bool IsFiltering { get; private set; }

		public void SetSettings(DbAttribute attribute, bool state) {
			_states[attribute] = state;
		}

		public event CDEEventHandler FilterFinished;

		public void OnFilterFinished(List<TValue> items) {
			CDEEventHandler handler = FilterFinished;
			if (handler != null) handler(this, items);
		}

		private void _advance(ref int row, ref int column) {
			if (column >= 2) {
				column = 0;
				row++;
			}
			else {
				column += 2;
			}
		}

		public void Init(Grid searchDrop, TextBox searchBox, ListView view, Table<TKey, TValue> table) {
			try {
				_items = view;
				_table = table;
				_tbSearchItems = searchBox;

				foreach (var attribute in _states) {
					_itemsSearchSettings[attribute.Key] = attribute.Value;
				}

				_tbSearchItems.TextChanged += new TextChangedEventHandler(_tbSearchItems_TextChanged);
				int row = 0;
				int column = 0;

				_addSearch(searchDrop, "Search options", null, row, column);

				_nextRow2(ref row, ref column);
				column = -2;

				foreach (DbAttribute attribute in _attributes) {
					_advance(ref row, ref column);

					if (attribute == null) {
						continue;
					}

					DbAttribute attributeCopy = attribute;
					_addSearchAttribute(searchDrop, attributeCopy, row, column);
				}

				_attributes = _attributes.Where(p => p != null).ToArray();

				_itemsSearchSettings[GSearchSettings.TupleAdded] = false;
				_itemsSearchSettings[GSearchSettings.TupleModified] = false;
				_nextRow(ref row, ref column);
				_addSearchAttributeSub(searchDrop, GSearchSettings.TupleAdded, row, column);
				_advance(ref row, ref column);
				_addSearchAttributeSub(searchDrop, GSearchSettings.TupleModified, row, column);

				_tbItemsRange = new TextBox();

				_cbSearchItemsMode = new ComboBox();
				_cbSearchItemsMode.MinWidth = 120;

				_cbSearchItemsMode.PreviewMouseDown += delegate(object sender, MouseButtonEventArgs args) {
					ComboBoxItem item = WpfUtilities.FindParentControl<ComboBoxItem>((Mouse.DirectlyOver as DependencyObject));

					if (item != null) {
						StackPanel panel = WpfUtilities.FindParentControl<StackPanel>(item);

						if (panel != null) {
							if (panel.Children.Count == 1)
								return;
						}

						item.IsSelected = true;
						args.Handled = true;
					}
				};

				_cbSearchItemsMode.SelectionChanged += new SelectionChangedEventHandler(_cbSearchItemsMode_SelectionChanged);
				_cbSearchItemsMode.Items.Add("Widen search");
				_cbSearchItemsMode.Items.Add("Narrow search");
				_advance(ref row, ref column);
				_addSearch(searchDrop, "Mode", _cbSearchItemsMode, row, column);
				_nextRow2(ref row, ref column);

				if (typeof(TKey) == typeof(int))
					_addSearch(searchDrop, "Range (5-10;-4;15+)", _tbItemsRange, row, column);

				_itemsSearchSettings[GSearchSettings.TupleRange] = false;
				//_itemsSearchSettings[] = false;
				_tbItemsRange.TextChanged += new TextChangedEventHandler((sender, e) => _itemsSearchSettings[GSearchSettings.TupleRange] = _tbItemsRange.Text.Trim() != "");
				_cbSearchItemsMode.SelectedIndex = 1;

				_itemsSearchSettings.Modified += new GSearchSettings.SearchSettingsEventHandler(_filter);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void Init(Grid searchDrop, TextBox searchBox, GDbTabWrapper<TKey, TValue> tab) {
			Init(searchDrop, searchBox, tab.List, tab.Table);
		}

		private void _nextRow(ref int row, ref int column) {
			if (column != 0) {
				column = 0;
				row++;
			}
		}

		private void _nextRow2(ref int row, ref int column) {
			column = 0;
			row++;
		}

		private void _addSearch(Grid searchGrid, string display, FrameworkElement element, int row, int column) {
			Label label = new Label();
			label.Content = display;

			while (searchGrid.RowDefinitions.Count <= row)
				searchGrid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(-1, GridUnitType.Auto)});

			label.SetValue(Grid.RowProperty, row);
			label.SetValue(Grid.ColumnProperty, column);

			if (element != null) {
				element.SetValue(Grid.RowProperty, row);
				element.SetValue(Grid.ColumnProperty, column + 2);
				element.Margin = new Thickness(2);

				searchGrid.Children.Add(element);
			}

			searchGrid.Children.Add(label);
		}

		private void _addSearchAttributeSub(Grid searchGrid, string attribute, int row, int column) {
			CheckBox box = new CheckBox();
			box.Margin = new Thickness(3);

			TextBlock block = new TextBlock { Text = attribute };
			box.MouseEnter += delegate {
				block.Foreground = new SolidColorBrush(Color.FromArgb(255, 5, 119, 193));
				block.Cursor = Cursors.Hand;
				block.TextDecorations = TextDecorations.Underline;
			};

			box.MouseLeave += delegate {
				block.Foreground = Brushes.Black;
				block.Cursor = Cursors.Arrow;
				block.TextDecorations = null;
			};
			box.Content = block;

			while (searchGrid.RowDefinitions.Count <= row)
				searchGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto) });

			box.SetValue(Grid.RowProperty, row);
			box.SetValue(Grid.ColumnProperty, column);

			_itemsSearchSettings.Link(box, attribute);

			searchGrid.Children.Add(box);
		}

		private void _addSearchAttribute(Grid searchGrid, DbAttribute attribute, int row, int column) {
			if (attribute.DataType.BaseType == typeof(Enum)) {
				Grid grid = new Grid();

				Label display = new Label();
				display.Margin = new Thickness(3);
				display.Padding = new Thickness(0);
				display.Content = attribute.DisplayName;
				display.VerticalAlignment = VerticalAlignment.Center;

				ComboBox box = new ComboBox();
				box.Margin = new Thickness(3);
				List<string> items = Enum.GetValues(attribute.DataType).Cast<Enum>().Select(Description.GetDescription).ToList();
				items.Insert(0, "All");
				box.ItemsSource = items;
				box.SelectedIndex = 0;
				box.SetValue(Grid.ColumnProperty, 1);
				List<int> values = Enum.GetValues(attribute.DataType).Cast<int>().ToList();
				_itemsSearchSettings[attribute] = false;

				box.PreviewMouseDown += delegate(object sender, MouseButtonEventArgs args) {
					ComboBoxItem item  = WpfUtilities.FindParentControl<ComboBoxItem>((Mouse.DirectlyOver as DependencyObject));
					
					if (item != null) {
						StackPanel panel = WpfUtilities.FindParentControl<StackPanel>(item);

						if (panel != null) {
							if (panel.Children.Count == 1)
								return;
						}

						item.IsSelected = true;
						args.Handled = true;
					}
				};
				box.SelectionChanged += delegate {
					if (box.SelectedIndex > 0) {
						attribute.AttachedAttribute = values[box.SelectedIndex - 1].ToString(CultureInfo.InvariantCulture);
					}

					_itemsSearchSettings[attribute] = box.SelectedIndex != 0;
				};

				grid.SetValue(Grid.RowProperty, row);
				grid.SetValue(Grid.ColumnProperty, column);

				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
				grid.ColumnDefinitions.Add(new ColumnDefinition());

				grid.Children.Add(display);
				grid.Children.Add(box);

				searchGrid.Children.Add(grid);
			}
			else {
				_addSearchAttributeSub(searchGrid, attribute.AttributeName, row, column);
			}
		}

		private void _cbSearchItemsMode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			_itemsSearchSettings.Set(GSearchSettings.Mode, _cbSearchItemsMode.SelectedIndex);
		}
		private void _tbSearchItems_TextChanged(object sender, TextChangedEventArgs e) {
			_searchItemsFilter = _tbSearchItems.Text;
			_filter(this);
		}

		public void SetAttributes(params DbAttribute[] attributes) {
			_attributes = attributes;
		}

		public void SetAttributes(IEnumerable<DbAttribute> attributes) {
			_attributes = attributes.ToArray();
		}

		public void SetRange(List<int> indexes) {
			_tbItemsRange.Text = _getQuery(indexes);
		}

		public static List<Func<TValue, bool>> GetRangePredicates(string query) {
			try {
				List<string> rangeQueries = query.Split(';').Select(p => p.Trim()).ToList();
				List<Func<TValue, bool>> predicates = new List<Func<TValue, bool>>();

				foreach (string rangeQuery in rangeQueries) {
					try {
						if (rangeQuery.StartsWith("-")) {
							string queryPredicate = rangeQuery;
							int high = Int32.Parse(queryPredicate.Substring(1));

							predicates.Add(new Func<TValue, bool>(p => p.GetKey<int>() <= high));
						}
						else if (rangeQuery.Contains("-")) {
							string queryPredicate = rangeQuery;
							int low = Int32.Parse(queryPredicate.Split('-')[0]);
							int high = Int32.Parse(queryPredicate.Split('-')[1]);

							predicates.Add(new Func<TValue, bool>(p => low <= p.GetKey<int>() && p.GetKey<int>() <= high));
						}
						else if (rangeQuery.EndsWith("+")) {
							string queryPredicate = rangeQuery;
							int low = Int32.Parse(queryPredicate.Substring(0, rangeQuery.Length - 1));

							predicates.Add(new Func<TValue, bool>(p => p.GetKey<int>() >= low));
						}
						else {
							string queryPredicate = rangeQuery;
							int middle = Int32.Parse(queryPredicate);

							predicates.Add(new Func<TValue, bool>(p => p.GetKey<int>() == middle));
						}
					}
					catch { }
				}

				return predicates;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return new List<Func<TValue, bool>>();
			}
		}

		private string _getQuery(List<int> tupleIndexes) {
			tupleIndexes.Add(-1);

			string searchQuery = "";

			int oldIndex = -1;
			int endIndex = -1;
			int startIndex = -1;

			foreach (int tupleIndex in tupleIndexes) {
				if (startIndex == -1) {
					startIndex = tupleIndex;
					oldIndex = tupleIndex;
					if (tupleIndex == -1) break; continue;
				}

				if (tupleIndex == oldIndex + 1) {
					endIndex = tupleIndex;
					oldIndex = tupleIndex;
					if (tupleIndex == -1) break; continue;
				}

				if (endIndex != -1 && startIndex != endIndex) {
					searchQuery += startIndex + "-" + endIndex + ";";
					startIndex = tupleIndex;
					oldIndex = tupleIndex;
					endIndex = -1;
					if (tupleIndex == -1) break; continue;
				}

				if (startIndex != endIndex) {
					searchQuery += oldIndex + ";";
					startIndex = tupleIndex;
					oldIndex = tupleIndex;
					endIndex = -1;
					if (tupleIndex == -1) break;
				}
			}

			return searchQuery;
		}

		public void AddTuple(TValue tuple) {
			_items.Dispatch(delegate {
				if (_items.ItemsSource == null)
					return;

				RangeObservableCollection<TValue> allItems = (RangeObservableCollection<TValue>)_items.ItemsSource;

				var index = allItems.ToList().BinarySearch(tuple, _entryComparer);
				if (index < 0) index = ~index;
				allItems.Insert(index, tuple);
			});
		}

		public void SetOrder(TValue tuple) {
			_items.Dispatch(delegate {
				if (_items.ItemsSource == null)
					return;

				RangeObservableCollection<TValue> allItems = (RangeObservableCollection<TValue>)_items.ItemsSource;

				List<TValue> items = allItems.ToList();
				var oldInex = items.IndexOf(tuple);

				if (oldInex < 0) {
					var index = items.BinarySearch(tuple, _entryComparer);
					if (index < 0) index = ~index;
					allItems.Insert(index, tuple);
				}
				else {
					items.Remove(tuple);
					var index = items.BinarySearch(tuple, _entryComparer);
					if (index < 0) index = ~index;
					allItems.Move(oldInex, index);
				}
			});
		}
	}
}
