using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Database;
using Database.Commands;
using ErrorManager;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.TabNavigationEngine;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.View;
using SDE.View.Controls;
using SDE.View.Dialogs;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using TokeiLibrary.WpfBugFix;
using Utilities;
using Utilities.Extension;

namespace SDE.Editor.Generic.UI.FormatConverters {
	public class CustomTableInitializer {
		public ServerDbs ServerDb { get; set; }
		public DbAttribute AttributeTable { get; set; }
		public AttributeList SubTableAttributeList { get; set; }
		public ServerDbs SubTableServerDbSearch { get; set; }
		public DbAttribute SubTableParentAttribute { get; set; }
		public int MaxElementsToCopy { get; set; }
		public ListView ListView { get; set; }
	}

	public class CustomSubTable<TKey> : FormatConverter<TKey, ReadableTuple<TKey>> {
		public GSearchEngine<TKey, ReadableTuple<TKey>> SearchEngine { get; private set; }
		public GTabSettings<TKey, ReadableTuple<TKey>> Settings { get; private set; }

		protected GDbTabWrapper<TKey, ReadableTuple<TKey>> _tab;
		protected DisplayableProperty<TKey, ReadableTuple<TKey>> _dp;
		protected RangeListView _lv;

		private Table<int, ReadableTuple<int>> __table;
		protected CustomTableInitializer _configuration;

		protected Table<int, ReadableTuple<int>> _table {
			get { return __table ?? (__table = _tab.ProjectDatabase.GetTable<int>(_configuration.ServerDb)); }
		}

		private void _commandChanged(object sender, ITableCommand<int, ReadableTuple<int>> command) {
			_commandChanged2(null, null);
		}

		private void _visualUpdate(object sender, ITableCommand<int, ReadableTuple<int>> command) {
			if (_lv != null && _lv.ItemsSource != null) {
				_lv.Dispatch(p => ((RangeObservableCollection<ReadableTuple<int>>)_lv.ItemsSource).Update());
			}
		}

		private void _commandChanged2(object sender, IGenericDbCommand command) {
			if (_tab.List.SelectedItem != null) {
				_lv_SelectionChanged(null, null);
			}
		}

		private void _lv_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var itemView = _lv.SelectedItem as ReadableTuple<int>;
			_tab.ItemsEventsDisabled = true;

			try {
				if (itemView == null) {
					_dp.Reset();
				}
				else {
					_dp.Display((ReadableTuple<TKey>)(object)itemView, null);
				}
			}
			finally {
				_tab.ItemsEventsDisabled = false;
			}
		}

		public override void Init(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, DisplayableProperty<TKey, ReadableTuple<TKey>> dp) {
			_tab = tab;
			_configuration = (CustomTableInitializer)tab.Settings.AttributeList[1].AttachedObject;
			_configuration.AttributeTable = tab.Settings.AttributeList[1];
			_tab.Settings.NewItemAddedFunction += item => item.SetRawValue(_configuration.AttributeTable, new Dictionary<int, ReadableTuple<int>>());

			SdeDatabase gdb = _tab.ProjectDatabase;
			_table.Commands.CommandUndo += _commandChanged;
			_table.Commands.CommandRedo += _commandChanged;
			_table.Commands.CommandExecuted += _commandChanged;
			_table.Commands.ModifiedStateChanged += _visualUpdate;

			gdb.Commands.CommandUndo += _commandChanged2;
			gdb.Commands.CommandRedo += _commandChanged2;
			gdb.Commands.CommandExecuted += _commandChanged2;

			Grid grid = new Grid();

			grid.SetValue(Grid.RowProperty, _row);
			grid.SetValue(Grid.ColumnProperty, 0);
			grid.SetValue(Grid.ColumnSpanProperty, 1);

			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto) });
			grid.RowDefinitions.Add(new RowDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition());

			_lv = new RangeListView();
			_lv.SetValue(TextSearch.TextPathProperty, "ID");
			_lv.SetValue(WpfUtils.IsGridSortableProperty, true);
			_lv.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
			_lv.SetValue(Grid.RowProperty, 1);
			_lv.Width = 334;
			_lv.FocusVisualStyle = null;
			_lv.Margin = new Thickness(3);
			_lv.BorderThickness = new Thickness(1);
			_lv.HorizontalAlignment = HorizontalAlignment.Left;
			_lv.SelectionChanged += _lv_SelectionChanged;
			SdeEditor.Instance.ProjectDatabase.Reloaded += _database_Reloaded;

			OnInitListView();

			_lv.PreviewMouseDown += delegate {
				_lv.Focus();
				Keyboard.Focus(_lv);
			};
			_lv.ContextMenu = new ContextMenu();
			_configuration.ListView = _lv;
			dp.AddResetField(_lv);
			dp.AddUpdateAction(_updateTable);

			_dp = new DisplayableProperty<TKey, ReadableTuple<TKey>>();
			_dp.IsDico = true;
			_dp.DicoConfiguration = _configuration;

			grid.Children.Add(_lv);
			OnDeplayTable();
			_tab.PropertiesGrid.Children.Add(grid);
			_dp.Deploy(_tab, null, true);

			DbSearchPanel dbSearchPanel = new DbSearchPanel();
			dbSearchPanel._border1.BorderThickness = new Thickness(1);
			dbSearchPanel.Margin = new Thickness(3, 0, 3, 0);
			grid.Children.Add(dbSearchPanel);

			Settings = new GTabSettings<TKey, ReadableTuple<TKey>>(ServerDbs.MobGroups, tab.DbComponent);

			var attributes = _configuration.SubTableAttributeList.Take(_configuration.MaxElementsToCopy).Where(p => !p.IsSkippable).ToList();

			if (!attributes.Any(p => p.IsDisplayAttribute)) {
				var att = _configuration.SubTableAttributeList.Attributes.FirstOrDefault(p => p.IsDisplayAttribute);

				if (att != null)
					attributes.Insert(1, att);
			}

			if (attributes.Count % 2 != 0) {
				attributes.Add(null);
			}

			Settings.AttributeList = _configuration.SubTableAttributeList;
			Settings.DbData = _configuration.ServerDb;
			Settings.AttId = attributes[0];
			Settings.AttDisplay = attributes[1];

			SearchEngine = Settings.SearchEngine;
			SearchEngine.SetAttributes();
			SearchEngine.SetSettings(attributes[0], true);
			SearchEngine.SetSettings(attributes[1], true);
			SearchEngine.SetAttributes(attributes);

			SearchEngine.Init(dbSearchPanel, _lv, () => {
				var dico = _getSelectedGroups();

				if (dico == null)
					return new List<ReadableTuple<TKey>>();

				return dico.Values.Cast<ReadableTuple<TKey>>().ToList();
			});

			ApplicationShortcut.Link(ApplicationShortcut.Cut, Cut, _lv);

			foreach (var update in _dp.Updates) {
				Tuple<DbAttribute, FrameworkElement> x = update;

				if (x.Item1.DataType == typeof(bool)) {
					CheckBox element = (CheckBox)x.Item2;
					_dp.AddUpdateAction(new Action<ReadableTuple<TKey>>(item => element.Dispatch(p => Debug.Ignore(() => p.IsChecked = item.GetValue<bool>(x.Item1)))));

					element.Checked += delegate { _dp.ApplyDicoCommand(_tab, _lv, (ReadableTuple<TKey>)_tab.List.SelectedItem, _configuration.AttributeTable, (ReadableTuple<TKey>)_lv.SelectedItem, x.Item1, true); };
					element.Unchecked += delegate { _dp.ApplyDicoCommand(_tab, _lv, (ReadableTuple<TKey>)_tab.List.SelectedItem, _configuration.AttributeTable, (ReadableTuple<TKey>)_lv.SelectedItem, x.Item1, false); };
				}
				else if (
					x.Item1.DataType == typeof(string) ||
					x.Item1.DataType == typeof(int)) {
					// This will convert integers to strings, but the database engine
					// is smart enough to auto-convert them to integers afterwards.
					TextBox element = (TextBox)x.Item2;
					_dp.AddUpdateAction(new Action<ReadableTuple<TKey>>(item => element.Dispatch(
						delegate {
							try {
								string val = item.GetValue<string>(x.Item1);

								if (val == element.Text)
									return;

								element.Text = item.GetValue<string>(x.Item1);
								element.UndoLimit = 0;
								element.UndoLimit = int.MaxValue;
							}
							catch {
							}
						})));

					element.TextChanged += delegate { _dp.ApplyDicoCommand(_tab, _lv, (ReadableTuple<TKey>)_tab.List.SelectedItem, _configuration.AttributeTable, (ReadableTuple<TKey>)_lv.SelectedItem, x.Item1, element.Text); };
				}
			}

			foreach (var property in _dp.FormattedProperties) {
				//property.Initialize()
				property.Init(tab, _dp);
			}
		}

		public override void OnInitialized() {
			TabControl parentItem = SdeEditor.Instance._mainTabControl;

			if (parentItem != null) {
				parentItem.SelectionChanged += new SelectionChangedEventHandler(_parent_SelectionChanged);
			}

			base.OnInitialized();
		}

		private void _parent_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (e.AddedItems.Count > 0 && e.AddedItems[0] is GDbTab && ReferenceEquals(e.AddedItems[0], _tab)) {
				TabChanged();
			}
		}

		public void TabChanged() {
			SearchEngine.Filter(this);
		}

		private void _database_Reloaded(object sender) {
			_lv.Dispatch(delegate {
				if (((Grid)_tab.Content).IsVisible) {
					SearchEngine.Filter(this);
				}
			});
		}

		protected Dictionary<int, ReadableTuple<int>> _getSelectedGroups() {
			return _tab.Dispatch(delegate {
				if (_tab.List.SelectedItem == null)
					return null;

				return (Dictionary<int, ReadableTuple<int>>)((ReadableTuple<int>)_tab.List.SelectedItem).GetRawValue(1);
			});
		}

		private void _updateTable(ReadableTuple<TKey> item) {
			Dictionary<int, ReadableTuple<int>> groups = (Dictionary<int, ReadableTuple<int>>)item.GetRawValue(1);

			if (groups == null) {
				groups = new Dictionary<int, ReadableTuple<int>>();
				item.SetRawValue(1, groups);
			}

			SearchEngine.Filter(this);

			//SearchEngine.FilterFinished += (s, l) => {
			//	_lv.Dispatch(p => {
			//		
			//		//_lv.OnResize(new SizeChangedInfo(_lv, new Size(_lv.Width, _lv.Height), true, false));
			//		//_lv.Measure(new Size(_lv.Width, _lv.Height));
			//	});
			//};
		}

		public void SelectItem() {
			if (_lv.SelectedItems.Count > 0) {
				TabNavigation.SelectList(_configuration.SubTableServerDbSearch, _lv.SelectedItems.OfType<ReadableTuple<int>>().ToList().Select(p => p.Key).ToList());
			}
		}

		public void DeleteSelection() {
			if (_lv.SelectedItems.Count <= 0)
				return;

			Table<int, ReadableTuple<int>> btable = _table;

			btable.Commands.Begin();

			try {
				for (int i = 0; i < _lv.SelectedItems.Count; i++) {
					var selectedItem = (ReadableTuple<int>)_lv.SelectedItems[i];
					_table.Commands.DeleteDico((ReadableTuple<int>)_tab.List.SelectedItem, _configuration.AttributeTable, selectedItem.Key);
					((RangeObservableCollection<ReadableTuple<int>>)_lv.ItemsSource).Remove(selectedItem);
					i--;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			btable.Commands.EndEdit();
			((RangeObservableCollection<ReadableTuple<int>>)_lv.ItemsSource).Update();
		}

		public void EditSelection(DbAttribute attribute) {
			if (_lv.SelectedItems.Count <= 0)
				return;

			Table<int, ReadableTuple<int>> btable = _tab.ProjectDatabase.GetTable<int>(_configuration.ServerDb);

			try {
				btable.Commands.Begin();

				var selectedItem = (ReadableTuple<int>)_lv.SelectedItem;
				DropEditDialog dialog = new DropEditDialog(selectedItem.Key.ToString(CultureInfo.InvariantCulture), selectedItem.GetValue<int>(attribute).ToString(CultureInfo.InvariantCulture), _configuration.SubTableServerDbSearch, _tab.ProjectDatabase) { Element2 = attribute.DisplayName };
				dialog.Owner = WpfUtilities.TopWindow;

				if (dialog.ShowDialog() == true) {
					string sid = dialog.Id;
					string svalue = dialog.DropChance;
					int value;
					int id;

					Int32.TryParse(sid, out id);
					Int32.TryParse(svalue, out value);

					if (id <= 0) {
						return;
					}

					var dico = _getSelectedGroups();

					if (selectedItem.Key != id && dico.ContainsKey(id)) {
						throw new Exception("The item ID " + id + " already exists.");
					}

					btable.Commands.SetDico((ReadableTuple<int>)_tab.List.SelectedItem, _configuration.AttributeTable, selectedItem, attribute, value);

					if (selectedItem.Key != id) {
						btable.Commands.ChangeKeyDico((ReadableTuple<int>)_tab.List.SelectedItem, _configuration.AttributeTable, selectedItem.Key, id, null);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				btable.Commands.End();
				((RangeObservableCollection<ReadableTuple<int>>)_lv.ItemsSource).Update();
			}
		}

		public void AddItem(string itemId, string itemProperty, bool selectId, DbAttribute attribute) {
			Table<int, ReadableTuple<int>> btable = _table;

			try {
				DropEditDialog dialog = new DropEditDialog(itemId, itemProperty, _configuration.SubTableServerDbSearch, _tab.ProjectDatabase, selectId) { Element2 = attribute.DisplayName };
				dialog.Owner = WpfUtilities.TopWindow;

				if (dialog.ShowDialog() == true) {
					string sid = dialog.Id;
					string svalue = dialog.DropChance;
					int value;
					int id;

					Int32.TryParse(sid, out id);
					Int32.TryParse(svalue, out value);

					if (id <= 0) {
						return;
					}

					ReadableTuple<int> tuple = new ReadableTuple<int>(id, _configuration.SubTableAttributeList);
					tuple.SetRawValue(attribute, value);
					tuple.SetRawValue(_configuration.SubTableParentAttribute, ((ReadableTuple<int>)_tab.List.SelectedItem).Key);
					tuple.Added = true;
					btable.Commands.AddTupleDico((ReadableTuple<int>)_tab.List.SelectedItem, _configuration.AttributeTable, id, tuple, _addedItem);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			btable.Commands.EndEdit();
		}

		public void CopyItems() {
			try {
				if (_lv.SelectedItems.Count > 0) {
					StringBuilder builder = new StringBuilder();

					foreach (var item in _lv.SelectedItems.OfType<ReadableTuple<int>>()) {
						builder.AppendLine(string.Join(",", item.GetRawElements().Take(_configuration.MaxElementsToCopy).Select(p => (p ?? "").ToString()).ToArray()));
					}

					Clipboard.SetDataObject(builder.ToString());
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void Cut() {
			CopyItems();
			DeleteSelection();
		}

		public void PasteItems() {
			try {
				string data = Clipboard.GetText();

				_table.Commands.Begin();

				try {
					Dictionary<int, ReadableTuple<int>> dico = _getSelectedGroups();

					using (StreamReader reader = new StreamReader(new MemoryStream(Encoding.Default.GetBytes(data)))) {
						while (!reader.EndOfStream) {
							string line = reader.ReadLine();

							if (String.IsNullOrEmpty(line)) break;

							if (line.Length > 1 && line[0] == '/' && line[1] == '/')
								continue;

							string[] elements = line.Split(',');

							try {
								int id = Int32.Parse(elements[0]);

								if (dico.ContainsKey(id)) {
									ReadableTuple<int> tuple = dico[id];

									for (int i = 1; i < elements.Length && i < _configuration.MaxElementsToCopy; i++) {
										_table.Commands.SetDico((ReadableTuple<int>)_tab.List.SelectedItem, _configuration.AttributeTable, tuple, tuple.Attributes[i], elements[i]);
									}

									tuple.SetRawValue(_configuration.SubTableParentAttribute, ((ReadableTuple<int>)_tab.List.SelectedItem).GetValue<int>(0));
								}
								else {
									// New value
									ReadableTuple<int> newValue = new ReadableTuple<int>(id, _configuration.SubTableAttributeList);

									for (int i = 1; i < elements.Length && i < _configuration.MaxElementsToCopy; i++) {
										newValue.SetRawValue(i, elements[i]);
									}

									newValue.SetRawValue(_configuration.SubTableParentAttribute, ((ReadableTuple<int>)_tab.List.SelectedItem).GetValue<int>(0));
									newValue.Added = true;
									_table.Commands.AddTupleDico(_tab.List.SelectedItem as ReadableTuple<int>, _configuration.AttributeTable, id, newValue, _addedItem);
								}
							}
							catch {
								// invalid item
							}
						}
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
				finally {
					_table.Commands.End();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected void _addedItem(ReadableTuple<int> tupleParent, int dkey, ReadableTuple<int> dvalue, bool executed) {
			RangeObservableCollection<ReadableTuple<int>> result = (RangeObservableCollection<ReadableTuple<int>>)_lv.ItemsSource;

			if (executed) {
				Dictionary<int, ReadableTuple<int>> dico = (Dictionary<int, ReadableTuple<int>>)tupleParent.GetRawValue(1);
				SearchEngine.AddTuple((ReadableTuple<TKey>)(object)dico[dkey]);
			}
			else {
				result.Remove(result.FirstOrDefault(p => p.Key == dkey));
			}

			((RangeObservableCollection<ReadableTuple<int>>)_lv.ItemsSource).Update();
		}

		public virtual void OnDeplayTable() {
		}

		public virtual void OnInitListView() {
		}
	}
}