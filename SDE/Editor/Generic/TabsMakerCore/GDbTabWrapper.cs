using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Database;
using Database.Commands;
using ErrorManager;
using GRF.System;
using GRF.Threading;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.TabNavigationEngine;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.View.Dialogs;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Extension;
using Extensions = SDE.Core.Extensions;

namespace SDE.Editor.Generic.TabsMakerCore {
	public class GDbTabWrapper<TKey, TValue> : GDbTab where TValue : Tuple {
		private readonly object _lock = new object();
		public bool ItemsEventsDisabled;
		private TValue _currentSelectedItem;
		private bool _isDeployed;

		public new object Content {
			get { return base.Content ?? ((Window)AttachedProperty["AttachedWindow"]).Content; }
			set { base.Content = value; }
		}

		public bool IsDetached {
			get { return AttachedProperty["AttachedWindow"] != null; }
		}

		public override bool IsSelected {
			get {
				if (IsDetached)
					return ((Window)AttachedProperty["AttachedWindow"]).IsActive;
				return base.IsSelected;
			}
			set {
				if (IsDetached)
					((Window)AttachedProperty["AttachedWindow"]).Activate();
				else
					base.IsSelected = value;
			}
		}

		public GTabSettings<TKey, TValue> Settings { get; private set; }
		public GSearchEngine<TKey, TValue> SearchEngine { get; private set; }

		public Grid PropertiesGrid {
			get { return _displayGrid; }
		}

		public ListView List {
			get { return _listView; }
		}

		public override sealed bool IsFiltering {
			get { return SearchEngine.IsFiltering; }
			set { base.IsFiltering = value; }
		}

		public override sealed DbAttribute DisplayAttribute {
			get { return Settings.AttDisplay; }
		}

		public override sealed DbAttribute IdAttribute {
			get { return Settings.AttId; }
		}

		public Table<TKey, TValue> Table { get; set; }

		public void Initialize(GTabSettings<TKey, TValue> settings) {
			Settings = settings;

			ProjectDatabase = settings.ClientDatabase;
			DbComponent = ProjectDatabase.GetDb<TKey>(settings.DbData);
			Table = Settings.Table;
			Header = Settings.TabName;
			Style = TryFindResource(settings.Style) as Style ?? Style;
			SearchEngine = settings.SearchEngine;
			SearchEngine.Init(_dbSearchPanel, this);
			Table.TableUpdated += new Table<TKey, TValue>.UpdateTableEventHandler(_table_TableUpdated);

			if (Settings.SearchEngine.SetupImageDataGetter != null) {
				Extensions.GenerateListViewTemplate(_listView, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
					new ListViewDataTemplateHelper.GeneralColumnInfo { Header = Settings.AttId.DisplayName, DisplayExpression = "[" + Settings.AttId.Index + "]", SearchGetAccessor = Settings.AttId.AttributeName, FixedWidth = Settings.AttIdWidth, TextAlignment = TextAlignment.Right, ToolTipBinding = "[" + Settings.AttId.Index + "]" },
					new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = Settings.AttId.AttributeName, FixedWidth = 26, MaxHeight = 24 },
					new ListViewDataTemplateHelper.RangeColumnInfo { Header = Settings.AttDisplay.DisplayName, DisplayExpression = "[" + Settings.AttDisplay.Index + "]", SearchGetAccessor = Settings.AttDisplay.AttributeName, IsFill = true, ToolTipBinding = "[" + Settings.AttDisplay.Index + "]", MinWidth = 100, TextWrapping = TextWrapping.Wrap }
				}, new DatabaseItemSorter(Settings.AttributeList), new string[] { "Deleted", "Red", "Modified", "Green", "Added", "Blue", "Normal", "Black" }, "generateStyle", "false");
			}
			else {
				Extensions.GenerateListViewTemplate(_listView, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
					new ListViewDataTemplateHelper.GeneralColumnInfo { Header = Settings.AttId.DisplayName, DisplayExpression = "[" + Settings.AttId.Index + "]", SearchGetAccessor = Settings.AttId.AttributeName, FixedWidth = Settings.AttIdWidth, TextAlignment = TextAlignment.Right, ToolTipBinding = "[" + Settings.AttId.Index + "]" },
					new ListViewDataTemplateHelper.RangeColumnInfo { Header = Settings.AttDisplay.DisplayName, DisplayExpression = "[" + Settings.AttDisplay.Index + "]", SearchGetAccessor = Settings.AttDisplay.AttributeName, IsFill = true, ToolTipBinding = "[" + Settings.AttDisplay.Index + "]", MinWidth = 100, TextWrapping = TextWrapping.Wrap }
				}, new DatabaseItemSorter(Settings.AttributeList), new string[] { "Deleted", "Red", "Modified", "Green", "Added", "Blue", "Normal", "Black" }, "generateStyle", "false");
			}

			if (!Settings.CanBeDelayed || Settings.AttributeList.Attributes.Any(p => p.IsSkippable))
				_deployTabControls();

			_initTableEvents();

			if (Settings.ContextMenu != null) {
				if (Header is Control)
					((Control)Header).ContextMenu = Settings.ContextMenu;
			}

			if (Settings.Loaded != null) {
				Settings.Loaded((GDbTabWrapper<TKey, ReadableTuple<TKey>>)(object)this, (GTabSettings<TKey, ReadableTuple<TKey>>)(object)Settings, ProjectDatabase.GetDb<TKey>(Settings.DbData));
			}

			if (Settings.DisplayablePropertyMaker.OnTabVisible != null) {
				Settings.DisplayablePropertyMaker.OnTabVisible(this);
			}

			Loaded += delegate {
				TabControl parent = WpfUtilities.FindParentControl<TabControl>(this);

				if (parent != null) {
					parent.SelectionChanged += new SelectionChangedEventHandler(_parent_SelectionChanged);
				}
			};

			_listView.PreviewMouseDown += delegate { _listView.Focus(); };

			_listView.Loaded += delegate {
				try {
					if (IsVisible) {
						Keyboard.Focus(_listView);
					}
				}
				catch {
				}
			};

			ApplicationShortcut.Link(ApplicationShortcut.Paste, () => ImportFromFile("clipboard"), _listView);
			ApplicationShortcut.Link(ApplicationShortcut.AdvancedPaste, () => ImportFromFile("clipboard", true), this);
			ApplicationShortcut.Link(ApplicationShortcut.AdvancedPaste2, () => ImportFromFile("clipboard", true), this);
			ApplicationShortcut.Link(ApplicationShortcut.Cut, () => _miCut_Click(null, null), _listView);
		}

		private void _table_TableUpdated(object sender) {
			if (SearchEngine.IsLoaded) {
				List<Tuple> tuples = _listView.SelectedItems.OfType<Tuple>().ToList();
				SearchEngine.Filter(this, () => SelectItems(tuples));
			}
		}

		private void _deployTabControls() {
			if (!_isDeployed) {
				Settings.DisplayablePropertyMaker.Deploy(this, Settings);
				_initMenus();
				WpfUtils.DisableContextMenuIfEmpty(_listView);
				_listView.SelectionChanged += _listView_SelectionChanged;

				_isDeployed = true;
			}
		}

		public Table<T, ReadableTuple<T>> GetTable<T>(ServerDbs db) {
			return ProjectDatabase.GetTable<T>(db);
		}

		public MetaTable<T> GetMetaTable<T>(ServerDbs db) {
			return ProjectDatabase.GetMetaTable<T>(db);
		}

		public AbstractDb<T> GetDb<T>(ServerDbs source) {
			return ProjectDatabase.GetDb<T>(source);
		}

		#region Init methods
		private void _initMenus() {
			foreach (GItemCommand<TKey, TValue> commandCopy in Settings.AddedCommands) {
				if (commandCopy.Visibility != Visibility.Visible) continue;

				GItemCommand<TKey, TValue> command = commandCopy;
				MenuItem item = new MenuItem();
				item.Header = command.DisplayName;
				item.Click += (e, a) => _menuItem_Click(command);

				Image image = new Image();
				image.Source = ApplicationManager.PreloadResourceImage(command.ImagePath);
				item.Icon = image;

				if (command.Shortcut != null) {
					ApplicationShortcut.Link(command.Shortcut, () => _menuItem_Click(command), this);
					item.InputGestureText = ApplicationShortcut.FindDislayName(command.Shortcut);
				}

				_listView.ContextMenu.Items.Insert(command.InsertIndex, item);
			}

			_miDelete.Click += _menuItemDeleteItem_Click;
			_miCopyTo.Click += _menuItemCopyItemTo_Click;
			_miShowSelected.Click += _menuItemKeepSelectedItemsOnly_Click;
			_miChangeId.Click += _miChangeId_Click;
			_miSelectInNotepad.Click += _miSelectInNotepad_Click;
			_miCut.Click += _miCut_Click;

			if (!Settings.CanChangeId) {
				_miChangeId.Visibility = Visibility.Collapsed;
			}

			var shortcut = ApplicationShortcut.FromString("Ctrl-W", "Open in Notepad++");
			ApplicationShortcut.Link(shortcut, () => _miSelectInNotepad_Click(null, null), this);
		}

		private void _miCut_Click(object sender, RoutedEventArgs e) {
			try {
				ApplicationShortcut.Execute(ApplicationShortcut.Copy, this);
				_deleteItems();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _initTableEvents() {
			Table.TupleRemoved += _clientItemDatabase_TupleRemoved;
			Table.TupleAdded += _clientItemDatabase_TupleAdded;
			Table.TupleModified += _clientItemDatabase_TupleModified;
			Table.Commands.PreviewCommandRedo += _commands_PreviewCommandRedo;
			Table.Commands.PreviewCommandUndo += _commands_PreviewCommandRedo;
			Table.Commands.CommandRedo += _commands_CommandRedo;
			Table.Commands.CommandUndo += _commands_CommandRedo;
			ProjectDatabase.Reloaded += _database_Reloaded;
		}
		#endregion

		#region UI events
		public TkDictionary<string, object> AttachedProperty = new TkDictionary<string, object>();

		private void _miChangeId_Click(object sender, RoutedEventArgs e) {
			ChangeId();
		}

		private void _miSelectInNotepad_Click(object sender, RoutedEventArgs e) {
			try {
				TValue item = _listView.SelectedItem as TValue;

				if (item != null) {
					string displayId = item.GetValue<string>(Settings.AttId);
					string path;

					//if (Settings.DbData == ServerDbs.ItemGroups) {
					//	displayId = item.GetValue<string>(ServerItemGroupAttributes.Display);
					//}

					if ((path = DbPathLocator.DetectPath(Settings.DbData)) != null) {
						if (!FtpHelper.IsSystemFile(path)) {
							ErrorHandler.HandleException("The file cannot be opened because it is not stored locally.");
							return;
						}

						string[] lines = File.ReadAllLines(path);

						string line = lines.FirstOrDefault(p => p.StartsWith(displayId + ","));

						if (line == null)
							line = lines.FirstOrDefault(p => p.StartsWith(displayId + "\t"));

						if (line == null)
							line = lines.FirstOrDefault(p => p.Contains("Id: " + displayId));

						if (line == null)
							line = lines.FirstOrDefault(p => p.Contains("id: " + displayId));

						if (line == null)
							line = lines.FirstOrDefault(p => p.Contains("[" + displayId + "] ="));

						if (line == null)
							line = lines.FirstOrDefault(p => p.StartsWith(displayId));

						if (line == null) {
							int ival;
							if (!Int32.TryParse(displayId, out ival))
								line = lines.FirstOrDefault(p => p.Contains(displayId + ":"));
						}

						if (line != null)
							GTabsMaker.SelectInNotepadpp(path, (lines.ToList().IndexOf(line) + 1).ToString(CultureInfo.InvariantCulture));
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemCopyItemTo_Click(object sender, RoutedEventArgs e) {
			CopyItemTo();
		}

		private void _menuItem_Click(GItemCommand<TKey, TValue> command) {
			if (command.AddToCommandsStack) {
				if (command.AllowMultipleSelection) {
					List<ITableCommand<TKey, TValue>> commands = new List<ITableCommand<TKey, TValue>>();
					SearchEngine.Collection.Disable();

					for (int index = 0; index < _listView.SelectedItems.Count; index++) {
						TValue rItem = (TValue)_listView.SelectedItems[index];
						var cmd = command.Command(rItem);

						if (cmd != null)
							commands.Add(cmd);
					}

					if (commands.Count > 0)
						Table.Commands.AddGroupedCommands(commands);

					SearchEngine.Collection.UpdateAndEnable();
					Update();
				}
				else {
					try {
						if (_listView.SelectedItem != null) {
							TValue rItem = (TValue)_listView.SelectedItem;
							Table.Commands.StoreAndExecute(command.Command(rItem));
						}
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				}
			}
			else {
				if (command.GenericCommand == null) {
					ErrorHandler.HandleException("The added command in the generic database tab hasn't been compiled properly.");
					return;
				}

				command.GenericCommand(_listView.SelectedItems.OfType<TValue>().ToList());
			}
		}

		private void _menuItemDeleteItem_Click(object sender, RoutedEventArgs e) {
			_deleteItems();
		}

		private void _listView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				TValue item = _listView.SelectedItem as TValue;

				if (item != null) {
					Show(item);
				}
				else {
					_resetFields();
				}

				if (SdeAppConfiguration.BindItemTabs) {
					if (Settings.DbData == ServerDbs.CItems ||
					    Settings.DbData == ServerDbs.Items ||
					    Settings.DbData == ServerDbs.Items2) {
						LastSelectedTuple = item;
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _autoSelect() {
			if (SdeAppConfiguration.BindItemTabs) {
				if (Settings.DbData == ServerDbs.CItems || Settings.DbData == ServerDbs.Items || Settings.DbData == ServerDbs.Items2) {
					_dbSearchPanel._searchTextBox.Text = GSearchEngine<TKey, TValue>.LastSearch;
				}
			}

			if (SdeAppConfiguration.BindItemTabs && LastSelectedTuple != null) {
				if (LastSelectedTuple is ReadableTuple<int>) {
					if (Settings.DbData == ServerDbs.CItems) {
						TabNavigation.SelectQuiet(ServerDbs.CItems, LastSelectedTuple.GetKey<int>());
					}
					else if (Settings.DbData == ServerDbs.Items) {
						TabNavigation.SelectQuiet(ServerDbs.Items, LastSelectedTuple.GetKey<int>());
					}
					else if (Settings.DbData == ServerDbs.Items2) {
						TabNavigation.SelectQuiet(ServerDbs.Items2, LastSelectedTuple.GetKey<int>());
					}
				}
			}
		}

		private void _parent_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (e.AddedItems.Count > 0 && e.AddedItems[0] is GDbTab && ReferenceEquals(e.AddedItems[0], this)) {
				Window wnd = AttachedProperty["AttachedWindow"] as Window;

				if (wnd != null) {
					e.Handled = true;

					wnd.Dispatch(delegate { wnd.Activate(); });
				}

				TabChanged();
			}
		}

		public override void TabChanged() {
			_deployTabControls();

			if (DelayedReload) {
				SearchEngine.Filter(this);
				DelayedReload = false;
			}

			_autoSelect();
		}

		private void _menuItemKeepSelectedItemsOnly_Click(object sender, RoutedEventArgs e) {
			if (typeof(TKey) != typeof(int)) {
				ErrorHandler.HandleException("This method is not supported for this type of database.");
				return;
			}

			List<int> tupleIndexes = _listView.SelectedItems.Cast<TValue>().OrderBy(p => p.GetKey<TKey>()).Select(p => p.GetKey<int>()).ToList();
			SearchEngine.SetRange(tupleIndexes);
		}
		#endregion

		#region Database events
		private void _commands_CommandRedo(object sender, ITableCommand<TKey, TValue> command) {
			_listView.UpdateAndEnable();

			RangeObservableCollection<TValue> coll = SearchEngine.Collection;

			if (coll != null) {
				SearchEngine.Collection.UpdateAndEnable();
			}
		}

		private void _commands_PreviewCommandRedo(object sender, ITableCommand<TKey, TValue> command) {
			RangeObservableCollection<TValue> coll = SearchEngine.Collection;

			if (coll != null) {
				SearchEngine.Collection.Disable();
			}

			_listView.Disable();
		}

		private void _clientItemDatabase_TupleModified(object sender, TKey key, TValue value) {
			if (SearchEngine.SubsetCondition == null) {
				SearchEngine.SetOrder(value);
			}
			else {
				if (SearchEngine.SubsetCondition(value)) {
					SearchEngine.SetOrder(value);
				}
				else {
					_listView.Items.Delete(value);
					Table.FastItems.Remove(value);
				}
			}
		}

		private void _clientItemDatabase_TupleAdded(object sender, TKey key, TValue copiedElement) {
			if (Settings.SearchEngine.SubsetCondition != null) {
				if (!Settings.SearchEngine.SubsetCondition(copiedElement)) {
					return;
				}
			}

			if (!_listView.Items.Contains(copiedElement)) {
				if (_listView.ItemsSource != null) {
					SearchEngine.AddTuple(copiedElement);

					if (_isCurrentTabSelected()) {
						this.Dispatch(p => _listView.SelectedItem = copiedElement);
					}

					if (Settings.SearchEngine.SetupImageDataGetter != null) {
						Settings.SearchEngine.SetupImageDataGetter(copiedElement);
					}
				}
			}
		}

		private void _clientItemDatabase_TupleRemoved(object sender, TKey key, TValue value) {
			_listView.Items.Delete(value);
		}

		private void _database_Reloaded(object sender) {
			this.Dispatch(delegate {
				if (((Grid)Content).IsVisible) {
					SearchEngine.Filter(this);
				}
				else {
					DelayedReload = true;
				}
			});
		}
		#endregion

		#region Public methods (called from the menu)
		public void Show(TValue item) {
			_currentSelectedItem = item;
			_show(item);
		}

		public override void ChangeId() {
			try {
				_changeId(_listView.SelectedItem);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public override void SetRange(List<int> tupleIndexes) {
			SearchEngine.SetRange(tupleIndexes);
		}

		public override void SelectItems(List<Tuple> tuples) {
			_listView.SelectedIndex = -1;

			List<Tuple> toAdd = new List<Tuple>();

			for (int i = 0; i < tuples.Count; i++) {
				if (_listView.Items.Contains(tuples[i]))
					toAdd.Add(tuples[i]);
			}

			_listView.SelectItems(toAdd);

			_listView.ScrollToCenterOfView(_listView.SelectedItem);
			Keyboard.Focus(_listView);
		}

		public List<TValue> GetItems() {
			return _listView.Items.Cast<TValue>().ToList();
		}

		public override void Update() {
			_listView_SelectionChanged(null, null);
		}

		public override void Undo() {
			if (ProjectDatabase != null) {
				ProjectDatabase.Commands.Undo();
			}
			else {
				if (Table.Commands.CanUndo) {
					Table.Commands.Undo();
				}
			}
		}

		public override void Redo() {
			if (ProjectDatabase != null) {
				ProjectDatabase.Commands.Redo();
			}
			else {
				if (Table.Commands.CanRedo) {
					Table.Commands.Redo();
				}
			}
		}

		public override void AddNewItem() {
			try {
				if (Settings.CustomAddItemMethod != null) {
					Settings.CustomAddItemMethod();
					return;
				}

				TKey id = _getNewItemId(default(TKey));

				TValue item = (TValue)Activator.CreateInstance(typeof(TValue), id, Settings.AttributeList);
				item.Added = true;

				if (Settings.NewItemAddedFunction != null) {
					Settings.NewItemAddedFunction(item);
				}

				Table.Commands.AddTuple(id, item, false);
				_listView.ScrollToCenterOfView(item);
			}
			catch (KeyInvalidException) {
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public override void ImportFromFile(string fileDefault = null, bool autoIncrement = false) {
			try {
				string file = fileDefault ?? PathRequest.OpenFileCde("filter", "All db files|*.conf;*.txt");

				if (file == "clipboard") {
					if (!Clipboard.ContainsText())
						return;

					file = TemporaryFilesManager.GetTemporaryFilePath("clipboard_{0:0000}.txt");
					File.WriteAllText(file, Clipboard.GetText());
				}

				if (file != null) {
					try {
						Table.Commands.Begin();
						ProjectDatabase.GetDb<TKey>(Settings.DbData).LoadFromClipboard(file);
					}
					catch {
						Table.Commands.CancelEdit();
					}
					finally {
						Table.Commands.EndEdit();

						if (autoIncrement && typeof(TKey) == typeof(int)) {
							var cmds = Table.Commands.GetUndoCommands();

							if (cmds.Count > 0) {
								var lastCmd = cmds.Last() as GroupCommand<TKey, TValue>;

								if (lastCmd != null) {
									if (lastCmd.Commands.Count > 0 && lastCmd.Commands.OfType<ChangeTupleProperties<TKey, TValue>>().Count() == 1) {
										var firstKey = lastCmd.Commands.First().Key;

										var tuple = new ReadableTuple<TKey>(firstKey, Table.AttributeList);
										var oldTuple = (ReadableTuple<TKey>)(object)Table.TryGetTuple(firstKey);
										tuple.Copy(oldTuple);
										tuple.Added = true;

										ProjectDatabase.Commands.Undo();
										Table.Commands.AddTuple(tuple.GetKey<TKey>(), (TValue)(object)tuple, false, true, null);
									}
								}
							}
						}
					}

					_listView_SelectionChanged(this, null);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public override void AddNewItemRaw() {
			try {
				string defaultValue = Clipboard.ContainsText() ? Clipboard.GetText() : "";

				InputDialog dialog = new InputDialog("Paste the database lines here.", "Add new raw items", defaultValue, false, false);
				dialog.Owner = WpfUtilities.TopWindow;
				dialog.TextBoxInput.Loaded += delegate {
					dialog.TextBoxInput.SelectAll();
					dialog.TextBoxInput.Focus();
				};
				dialog.TextBoxInput.AcceptsReturn = true;
				dialog.TextBoxInput.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
				dialog.TextBoxInput.TextWrapping = TextWrapping.NoWrap;
				dialog.TextBoxInput.Height = 200;
				dialog.TextBoxInput.MinHeight = 200;
				dialog.TextBoxInput.MaxHeight = 200;
				dialog.TextBoxInput.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

				if (dialog.ShowDialog() == true) {
					try {
						Table.Commands.Begin();

						string text = dialog.Input;
						string tempPath = TemporaryFilesManager.GetTemporaryFilePath("db_tmp_{0:0000}.txt");
						File.WriteAllText(tempPath, text);
						ProjectDatabase.GetDb<TKey>(Settings.DbData).LoadFromClipboard(tempPath);
					}
					catch {
						Table.Commands.CancelEdit();
					}
					finally {
						Table.Commands.EndEdit();
					}

					_listView_SelectionChanged(this, null);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public override void ShowSelectedOnly() {
			_menuItemKeepSelectedItemsOnly_Click(this, null);
		}

		public override void DeleteItems() {
			try {
				_deleteItems();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public override void CopyItemTo() {
			try {
				_copyTo(_listView.SelectedItem as TValue);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public override void CopyItemTo(BaseDb db) {
			try {
				if (db.AttributeList.PrimaryAttribute.DataType != typeof(TKey))
					throw new Exception("Key type mismatch.");

				_copyTo(_listView.SelectedItems.OfType<TValue>().ToList(), db.To<TKey>());
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public override void Search() {
			_dbSearchPanel._searchTextBox.SelectAll();
			Keyboard.Focus(_dbSearchPanel._searchTextBox);
		}

		public override void Filter() {
			SearchEngine.Filter(this);
		}

		public override void IgnoreFilterOnce() {
			SearchEngine.IgnoreFilterOnce();
		}

		public override void ReplaceFromFile() {
			try {
				ReplaceTableFields.ReplaceFields(To<TKey>());
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
		#endregion

		#region Private methods (behavior)
		private void _show(TValue item) {
			lock (_lock) {
				if (item != _currentSelectedItem) return;

				try {
					ItemsEventsDisabled = true;

					Settings.DisplayablePropertyMaker.Display(item, () => _currentSelectedItem);
				}
				finally {
					ItemsEventsDisabled = false;
				}
			}
		}

		private void _resetFields() {
			lock (_lock) {
				try {
					ItemsEventsDisabled = true;
					Settings.DisplayablePropertyMaker.Reset();
				}
				finally {
					ItemsEventsDisabled = false;
				}
			}
		}

		private void _changeId(object selectedItem) {
			if (!Settings.CanChangeId) {
				ErrorHandler.HandleException("This type of database does not support identifiers edit.");
				return;
			}

			TValue item = (TValue)selectedItem;

			if (item == null) {
				ErrorHandler.HandleException("No item has been selected.", ErrorLevel.NotSpecified);
				return;
			}

			try {
				TKey id = _getNewItemId(item.GetKey<TKey>(), true);

				if (Table.ContainsKey(id)) {
					if (WindowProvider.ShowDialog("An item with this ID already exists. Would you like to replace it?", "ID already exists", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes) {
						try {
							Table.Commands.Begin();
							Table.Commands.Delete(id);
							Table.Commands.ChangeKey(item.GetValue<TKey>(Settings.AttributeList.PrimaryAttribute), id, _changeKeyCallback);
							_listView.ScrollToCenterOfView(_listView.SelectedItem);
						}
						finally {
							Table.Commands.End();
						}
					}

					return;
				}

				Table.Commands.ChangeKey(item.GetValue<TKey>(Settings.AttributeList.PrimaryAttribute), id, _changeKeyCallback);
				_listView.ScrollToCenterOfView(_listView.SelectedItem);
			}
			catch (KeyInvalidException) {
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _deleteItems() {
			try {
				if (_listView.SelectedItems.Count > 0) {
					List<ITableCommand<TKey, TValue>> commands = new List<ITableCommand<TKey, TValue>>();
					SearchEngine.Collection.Disable();

					for (int index = 0; index < _listView.SelectedItems.Count; index++) {
						TValue item = (TValue)_listView.SelectedItems[index];
						commands.Add(new DeleteTuple<TKey, TValue>(item.GetKey<TKey>(), _itemDeletedCallback));
					}

					Table.Commands.AddGroupedCommands(commands, _selectLastSelected);
					SearchEngine.Collection.UpdateAndEnable();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _copyTo(TValue item) {
			try {
				if (item == null)
					return;

				if (typeof(TKey) == typeof(string)) {
					if ((DbComponent.DbSource & ServerDbs.MobSkillsItems) != 0) {
						TKey old = item.GetValue<TKey>(Settings.AttributeList.PrimaryAttribute);
						string newId = (string)(object)old + Methods.RandomString(30);
						Table.Commands.CopyTupleTo(old, (TKey)(object)newId, _copyToCallback);
						return;
					}
				}

				TKey id = _getNewItemId(item.GetKey<TKey>(), true);

				TKey oldId = item.GetValue<TKey>(Settings.AttributeList.PrimaryAttribute);

				if (Table.ContainsKey(id)) {
					if (WindowProvider.ShowDialog("An item with this ID already exists (\"" + Table.TryGetTuple(id)[Settings.AttDisplay.Index].ToString().RemoveBreakLines() + "\"). Do you want to replace it?",
						"Item already exists", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes)
						return;
				}

				if (DbComponent.DbSource == ServerDbs.MobGroups ||
				    DbComponent.DbSource == ServerDbs.ItemGroups) {
					throw new InvalidOperationException("DicoCopyTo is not supported.");
				}

				Table.Commands.CopyTupleTo(oldId, id, _copyToCallback);
			}
			catch (KeyInvalidException) {
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _copyTo(List<TValue> items, AbstractDb<TKey> db) {
			try {
				if (items == null || items.Count == 0)
					return;

				if (typeof(string) == typeof(TKey)) {
					if ((DbComponent.DbSource & ServerDbs.MobSkillsItems) != 0) {
						try {
							db.Table.Commands.Begin();

							for (int i = 0; i < items.Count; i++) {
								var item = items[i];
								TKey old = item.GetValue<TKey>(Settings.AttributeList.PrimaryAttribute);
								string newId = (string)(object)old + Methods.RandomString(30);

								if (i == items.Count - 1)
									db.Table.Commands.CopyTupleTo((Table<TKey, ReadableTuple<TKey>>)(object)Table, old, (TKey)(object)newId, (a, b, c, d, e) => _copyToCallback2(db, c, d, e));
								else
									db.Table.Commands.CopyTupleTo((Table<TKey, ReadableTuple<TKey>>)(object)Table, old, (TKey)(object)newId, (a, b, c, d, e) => _copyToCallback3(c, d, e));
							}
						}
						catch (Exception err) {
							db.Table.Commands.CancelEdit();
							ErrorHandler.HandleException(err);
						}
						finally {
							db.Table.Commands.End();
						}
						return;
					}

					throw new Exception("Operation not supported.");
				}

				if (db.DbSource == Settings.DbData && items.Count == 1) {
					_copyTo(items[0]);
					return;
				}

				CopyToDialog dialog = new CopyToDialog(this, items.OfType<Tuple>().ToList(), DbComponent, db);
				dialog.ShowDialog();
			}
			catch (KeyInvalidException) {
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
		#endregion

		#region Key related...
		private TKey _getNewItemId(TKey oldId, bool ignoreAlreadyExists = false) {
			InputDialog dialog = new InputDialog("Enter the new ID for this item.", "New ID", oldId == null ? "" : oldId.ToString(), false);
			dialog.Owner = WpfUtilities.TopWindow;
			dialog.TextBoxInput.Loaded += delegate {
				dialog.TextBoxInput.SelectAll();
				dialog.TextBoxInput.Focus();
			};

			if (dialog.ShowDialog() == true) {
				try {
					if (typeof(TKey) == typeof(int))
						Int32.Parse(dialog.Input);
				}
				catch (Exception) {
					ErrorHandler.HandleException("Invalid ID format.");
					throw new KeyInvalidException();
				}

				if (typeof(TKey) == typeof(int)) {
					int id = Int32.Parse(dialog.Input);

					if (id < 0) {
						ErrorHandler.HandleException("ID must be greater than 0.");
						throw new KeyInvalidException();
					}
				}

				TKey idKey = _getKey(dialog.Input);

				if (!ignoreAlreadyExists && Table.ContainsKey(idKey)) {
					ErrorHandler.HandleException("An item with this ID already exists.");
					throw new KeyInvalidException();
				}

				return idKey;
			}

			throw new KeyInvalidException();
		}

		private TKey _getKey(string input) {
			if (typeof(TKey) == typeof(int)) {
				return (TKey)(object)Int32.Parse(input);
			}

			return (TKey)(object)input;
		}
		#endregion

		#region Commands callback
		private void _selectLastSelected(bool executed) {
			if (executed) {
			}
			else {
				if (_listView.SelectedItem != null)
					GrfThread.Start(() => _listView.Dispatch(p => p.ScrollToCenterOfView(_listView.SelectedItem)));
			}
		}

		private bool _isCurrentTabSelected() {
			return this.Dispatch(() => {
				try {
					TabControl tabControl = WpfUtilities.FindParentControl<TabControl>(this);
					TabItem selectedTab = tabControl.Items[tabControl.SelectedIndex] as TabItem;

					return selectedTab != null && WpfUtilities.IsTab(this, selectedTab.Header.ToString());
				}
				catch {
					return false;
				}
			});
		}

		private void _copyToCallback(TKey oldkey, TKey newkey, bool executed) {
			if (executed) {
				Table.GetTuple(newkey).Added = true;

				if (_isCurrentTabSelected())
					_listView.SelectedItem = Table.GetTuple(newkey);

				_listView.ScrollToCenterOfView(_listView.SelectedItem);
			}
		}

		private void _copyToCallback2(AbstractDb<TKey> dbDest, Table<TKey, ReadableTuple<TKey>> tableDest, TKey newkey, bool executed) {
			if (executed) {
				tableDest.GetTuple(newkey).Added = true;
				TabNavigation.Select(dbDest.DbSource, newkey);
			}
		}

		private void _copyToCallback3(Table<TKey, ReadableTuple<TKey>> tableDest, TKey newkey, bool executed) {
			if (executed) {
				tableDest.GetTuple(newkey).Added = true;
			}
		}

		private void _changeKeyCallback(TKey oldkey, TKey newkey, bool executed) {
			if (executed) {
				TValue tuple = Table.GetTuple(newkey);
				SearchEngine.SetOrder(tuple);

				if (tuple == _listView.SelectedItem) {
					if (Settings.TextBoxId != null)
						Settings.TextBoxId.Text = newkey.ToString();
				}
			}
			else {
				TValue tuple = Table.GetTuple(oldkey);
				SearchEngine.SetOrder(tuple);

				if (tuple == _listView.SelectedItem) {
					if (Settings.TextBoxId != null)
						Settings.TextBoxId.Text = oldkey.ToString();
				}
			}
		}

		private void _itemDeletedCallback(TKey key, TValue value, bool executed) {
			if (executed) {
				_listView.Items.Delete(value);
			}
		}
		#endregion
	}
}