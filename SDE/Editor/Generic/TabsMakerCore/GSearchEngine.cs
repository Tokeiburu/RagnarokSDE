using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Database;
using ErrorManager;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Generic.Lists;
using SDE.View.Controls;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using Utilities;

namespace SDE.Editor.Generic.TabsMakerCore {
	/// <summary>
	/// This class is responsible for sorting the items. It also
	/// generates the UI for the search panel.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	public partial class GSearchEngine<TKey, TValue> where TValue : Database.Tuple {
		#region Delegates
		public delegate void CDEEventHandler(object sender, List<TValue> modified);
		#endregion

		public static string LastSearch = "";

		private readonly object _filterLock = new object();
		private readonly GSearchSettings _itemsSearchSettings;
		private readonly GTabSettings<TKey, TValue> _settings;
		private readonly Dictionary<DbAttribute, bool> _states = new Dictionary<DbAttribute, bool>();
		private DbAttribute[] _attributes;
		private ComboBox _cbSearchItemsMode;
		private DatabaseItemSorter<TValue> _entryComparer;
		private bool _isLoaded;
		private ListView _items;
		private Grid _searchDrop;
		private bool _searchFirstTimeSet;
		private string _searchItemsFilter = "";
		private TextBox _tbItemsRange;
		private TextBox _tbSearchItems;
		private CheckBox _cbAdded;
		private CheckBox _cbModified;
		private Func<List<TValue>> _getItemsFunction;
		private readonly List<ComboBox> _resetFields = new List<ComboBox>();
		private DbSearchPanel _panel;

		public GSearchEngine(string tabName, GTabSettings<TKey, TValue> settings) {
			_settings = settings;
			_itemsSearchSettings = new GSearchSettings(ProjectConfiguration.ConfigAsker, tabName);
		}

		public bool IsLoaded {
			get { return _isLoaded; }
		}

		public bool IsFiltering { get; private set; }
		public Func<TValue, bool> SubsetCondition { get; set; }
		public Action<TValue> SetupImageDataGetter { get; set; }

		public RangeObservableCollection<TValue> Collection {
			get {
				_validateLoaded();
				return _items.ItemsSource as RangeObservableCollection<TValue>;
			}
		}

		private void _validateLoaded() {
			if (!_isLoaded) {
				if (_entryComparer == null) {
					_entryComparer = new DatabaseItemSorter<TValue>(_settings.AttributeList);
					_entryComparer.SetSort(_settings.AttId.AttributeName, ListSortDirection.Ascending);
				}

				_load();
				_isLoaded = true;
			}
		}

		private void _load() {
			_searchDrop.Dispatch(delegate {
				try {
					foreach (var attribute in _states) {
						_itemsSearchSettings[attribute.Key] = attribute.Value;
					}

					_tbSearchItems.TextChanged += _tbSearchItems_TextChanged;
					int row = 0;
					int column = 0;

					_addSearch(_searchDrop, "Search options", null, row, column, true);

					_nextRow2(ref row, ref column);
					column = -2;

					foreach (DbAttribute attribute in _attributes) {
						_advance(ref row, ref column);

						if (attribute == null) {
							continue;
						}

						DbAttribute attributeCopy = attribute;
						_addSearchAttribute(_searchDrop, attributeCopy, row, column);
					}

					_attributes = _attributes.Where(p => p != null).ToArray();

					_itemsSearchSettings[GSearchSettings.TupleAdded] = false;
					_itemsSearchSettings[GSearchSettings.TupleModified] = false;
					_nextRow(ref row, ref column);
					_cbAdded = _addSearchAttributeSub(_searchDrop, GSearchSettings.TupleAdded, row, column);
					_advance(ref row, ref column);
					_cbModified = _addSearchAttributeSub(_searchDrop, GSearchSettings.TupleModified, row, column);

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

					_cbSearchItemsMode.SelectionChanged += _cbSearchItemsMode_SelectionChanged;
					_cbSearchItemsMode.Items.Add("Widen search");
					_cbSearchItemsMode.Items.Add("Narrow search");
					_advance(ref row, ref column);
					_addSearch(_searchDrop, "Mode", _cbSearchItemsMode, row, column);
					_nextRow2(ref row, ref column);

					if (typeof(TKey) == typeof(int))
						_addSearch(_searchDrop, "Range (5-10;-4;15+)", _tbItemsRange, row, column);

					_itemsSearchSettings[GSearchSettings.TupleRange] = false;
					//_itemsSearchSettings[] = false;
					_tbItemsRange.TextChanged += (sender, e) => _itemsSearchSettings[GSearchSettings.TupleRange] = _tbItemsRange.Text.Trim() != "";
					_cbSearchItemsMode.SelectedIndex = 1;

					_itemsSearchSettings.Modified += _filter;
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			});
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

		private void _addSearch(Grid searchGrid, string display, FrameworkElement element, int row, int column, bool isItalic = false) {
			Label label = new Label();
			label.Content = display;
			label.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextForeground"] as Brush);

			if (isItalic)
				label.FontStyle = FontStyles.Italic;

			while (searchGrid.RowDefinitions.Count <= row)
				searchGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto) });

			WpfUtilities.SetGridPosition(label, row, column);

			if (element != null) {
				WpfUtilities.SetGridPosition(element, row, column + 2);
				element.Margin = new Thickness(2);

				searchGrid.Children.Add(element);
			}

			searchGrid.Children.Add(label);
		}

		private void _addSearchAttribute(Grid searchGrid, DbAttribute attribute, int row, int column) {
			if (attribute.DataType.BaseType == typeof(Enum)) {
				Grid grid = new Grid();

				Label display = new Label();
				display.Margin = new Thickness(3);
				display.Padding = new Thickness(0);
				display.Content = attribute.DisplayName;
				display.VerticalAlignment = VerticalAlignment.Center;
				display.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextForeground"] as Brush);

				ComboBox box = new ComboBox();
				_resetFields.Add(box);
				box.Margin = new Thickness(3);
				List<string> items = Enum.GetValues(attribute.DataType).Cast<Enum>().Select(Description.GetDescription).ToList();
				items.Insert(0, "All");
				box.ItemsSource = items;
				box.SelectedIndex = 0;
				box.SetValue(Grid.ColumnProperty, 1);
				List<int> values = Enum.GetValues(attribute.DataType).Cast<int>().ToList();
				_itemsSearchSettings[attribute] = false;

				box.PreviewMouseDown += delegate(object sender, MouseButtonEventArgs args) {
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
				box.SelectionChanged += delegate {
					if (box.SelectedIndex > 0) {
						attribute.AttachedAttribute = values[box.SelectedIndex - 1].ToString(CultureInfo.InvariantCulture);
					}

					_itemsSearchSettings[attribute] = box.SelectedIndex != 0;
				};

				WpfUtilities.SetGridPosition(grid, row, column);
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
				grid.ColumnDefinitions.Add(new ColumnDefinition());

				grid.Children.Add(display);
				grid.Children.Add(box);

				searchGrid.Children.Add(grid);
			}
			else {
				_addSearchAttributeSub(searchGrid, attribute.DisplayName, row, column);
			}
		}

		private CheckBox _addSearchAttributeSub(Grid searchGrid, string attribute, int row, int column) {
			CheckBox box = new CheckBox();
			box.Margin = new Thickness(3);
			box.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextForeground"] as Brush);

			TextBlock block = new TextBlock { Text = attribute };
			block.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextForeground"] as Brush);
			box.MouseEnter += delegate {
				block.Foreground = Application.Current.Resources["MouseOverTextBrush"] as Brush;
				block.Cursor = Cursors.Hand;
				block.TextDecorations = TextDecorations.Underline;
			};

			box.MouseLeave += delegate {
				block.Foreground = Application.Current.Resources["TextForeground"] as Brush;
				block.Cursor = Cursors.Arrow;
				block.TextDecorations = null;
			};
			box.Content = block;

			while (searchGrid.RowDefinitions.Count <= row)
				searchGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto) });

			WpfUtilities.SetGridPosition(box, row, column);
			_itemsSearchSettings.Link(box, attribute);
			searchGrid.Children.Add(box);

			return box;
		}

		private void _cbSearchItemsMode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			_itemsSearchSettings.Set(GSearchSettings.Mode, _cbSearchItemsMode.SelectedIndex);
		}

		private void _tbSearchItems_TextChanged(object sender, TextChangedEventArgs e) {
			_searchItemsFilter = _tbSearchItems.Text;
			_filter(this);

			if (SdeAppConfiguration.BindItemTabs && (_settings.DbData & ServerDbs.AllItemTables) != 0) {
				LastSearch = _tbSearchItems.Text;
			}
		}

		public void Init(DbSearchPanel panel, ListView view, Func<List<TValue>> getItemsFunction) {
			// The initialization is delayed, it will start when loading the tab
			_searchDrop = panel._gridSearchContent;
			_items = view;
			_getItemsFunction = getItemsFunction;
			_tbSearchItems = panel._searchTextBox;
			_panel = panel;
			panel._buttonResetSearch.Click += (sender, args) => Reset();
			ApplicationShortcut.Link(ApplicationShortcut.Search, () => {
				panel._searchTextBox.SelectAll();
				Keyboard.Focus(panel._searchTextBox);
			}, view);
		}

		public void Init(DbSearchPanel panel, ListView view, Table<TKey, TValue> table) {
			Init(panel, view, () => table.FastItems);
		}

		public void Init(DbSearchPanel panel, GDbTabWrapper<TKey, TValue> tab) {
			Init(panel, tab.List, tab.Table);
		}

		public void SetAttributes(params DbAttribute[] attributes) {
			_attributes = attributes;
		}

		public void SetAttributes(IEnumerable<DbAttribute> attributes) {
			_attributes = attributes.ToArray();
		}

		public void SetSettings(DbAttribute attribute, bool state) {
			_states[attribute] = state;
		}

		public void SetRange(List<int> indexes) {
			_validateLoaded();
			_tbItemsRange.Text = GetQuery(indexes);
		}

		public void AddTuple(TValue tuple) {
			_validateLoaded();
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
			_validateLoaded();
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
					catch {
					}
				}

				return predicates;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return new List<Func<TValue, bool>>();
			}
		}

		public static string GetQuery(List<int> tupleIndexes) {
			tupleIndexes.Add(-1);

			string searchQuery = "";

			int oldIndex = -1;
			int endIndex = -1;
			int startIndex = -1;

			foreach (int tupleIndex in tupleIndexes) {
				if (startIndex == -1) {
					startIndex = tupleIndex;
					oldIndex = tupleIndex;
					if (tupleIndex == -1) break;
					continue;
				}

				if (tupleIndex == oldIndex + 1) {
					endIndex = tupleIndex;
					oldIndex = tupleIndex;
					if (tupleIndex == -1) break;
					continue;
				}

				if (endIndex != -1 && startIndex != endIndex) {
					searchQuery += startIndex + "-" + endIndex + ";";
					startIndex = tupleIndex;
					oldIndex = tupleIndex;
					endIndex = -1;
					if (tupleIndex == -1) break;
					continue;
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

		public void Reset() {
			try {
				_filterEnabled = false;

				_tbItemsRange.Text = "";
				_tbSearchItems.Text = "";
				_cbSearchItemsMode.SelectedIndex = 1;

				_cbAdded.IsChecked = false;
				_cbModified.IsChecked = false;
				_filterEnabled = true;

				_resetFields.ForEach(p => p.SelectedIndex = 0);

				_filter(this);
			}
			finally {
				_filterEnabled = true;
			}
		}
	}
}