using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Database;
using Database.Commands;
using ErrorManager;
using GRF.System;
using GRF.Threading;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.CommandLine;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Generic.TabsMakerCore {
	public class GDbTabWrapper<TKey, TValue> : GDbTab where TValue : Tuple {
		private readonly object _lock = new object();
		public bool ItemsEventsDisabled;
		private TValue _currentSelectedItem;

		public GTabSettings<TKey, TValue> Settings { get; private set; }
		public GSearchEngine<TKey, TValue> SearchEngine { get; private set; }

		public Grid PropertiesGrid {
			get { return _displayGrid; }
		}
		public ListView List {
			get { return _listView; }
		}

		public override bool IsFiltering {
			get { return SearchEngine.IsFiltering; }
			set { base.IsFiltering = value; }
		}
		public override DbAttribute DisplayAttribute {
			get { return Settings.AttDisplay; }
		}
		public override DbAttribute IdAttribute {
			get { return Settings.AttId; }
		}

		public Table<TKey, TValue> Table { get; set; }

		public void Initialize(GTabSettings<TKey, TValue> settings) {
			var res = WpfUtilities.FindDirectParentControl<TabControl>(this);
			Z.F(res);
			Settings = settings;
			_unclickableBorder.Init(_cbSubMenu);
			Database = settings.ClientDatabase;
			Table = Settings.Table;
			Header = Settings.TabName;
			Style = TryFindResource(settings.Style) as Style ?? Style;
			SearchEngine = settings.SearchEngine;
			SearchEngine.Init(_gridSearchContent, _searchTextBox, this);

			if (Settings.SearchEngine.SetupImageDataGetter != null) {
				Others.Extensions.GenerateListViewTemplate(_listView, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
			        new ListViewDataTemplateHelper.GeneralColumnInfo {Header = Settings.AttId.DisplayName, DisplayExpression = "[" + Settings.AttId.Index + "]", SearchGetAccessor = Settings.AttId.AttributeName, FixedWidth = Settings.AttIdWidth, TextAlignment = TextAlignment.Right, ToolTipBinding = "[" + Settings.AttId.Index + "]"},
			        new ListViewDataTemplateHelper.ImageColumnInfo {Header = "", DisplayExpression = "DataImage", SearchGetAccessor = Settings.AttId.AttributeName, FixedWidth = 26, MaxHeight = 24},
			        new ListViewDataTemplateHelper.RangeColumnInfo {Header = Settings.AttDisplay.DisplayName, DisplayExpression = "[" + Settings.AttDisplay.Index + "]", SearchGetAccessor = Settings.AttDisplay.AttributeName, IsFill = true, ToolTipBinding = "[" + Settings.AttDisplay.Index + "]", MinWidth = 100, TextWrapping = TextWrapping.Wrap }
			    }, new DatabaseItemSorter(Settings.AttributeList), new string[] { "Deleted", "Red", "Modified", "Green", "Added", "Blue", "Normal", "Black" }, "generateStyle", "false");
			}
			else {
				Others.Extensions.GenerateListViewTemplate(_listView, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
			        new ListViewDataTemplateHelper.GeneralColumnInfo {Header = Settings.AttId.DisplayName, DisplayExpression = "[" + Settings.AttId.Index + "]", SearchGetAccessor = Settings.AttId.AttributeName, FixedWidth = Settings.AttIdWidth, TextAlignment = TextAlignment.Right, ToolTipBinding = "[" + Settings.AttId.Index + "]"},
			        new ListViewDataTemplateHelper.RangeColumnInfo {Header = Settings.AttDisplay.DisplayName, DisplayExpression = "[" + Settings.AttDisplay.Index + "]", SearchGetAccessor = Settings.AttDisplay.AttributeName, IsFill = true, ToolTipBinding = "[" + Settings.AttDisplay.Index + "]", MinWidth = 100, TextWrapping = TextWrapping.Wrap }
			    }, new DatabaseItemSorter(Settings.AttributeList), new string[] { "Deleted", "Red", "Modified", "Green", "Added", "Blue", "Normal", "Black" }, "generateStyle", "false");
			}

#if SDE_DEBUG
			CLHelper.WA = CLHelper.CP(-10);
#endif
			Settings.DisplayablePropertyMaker.Deploy(this, Settings);
#if SDE_DEBUG
			CLHelper.WA = ", deploy time : " + CLHelper.CS(-10) + CLHelper.CD(-10) + "ms";
#endif
			_initTableEvents();
			_initMenus();

			_searchTextBox.GotFocus += new RoutedEventHandler(_searchTextBox_GotFocus);
			_searchTextBox.LostFocus += new RoutedEventHandler(_searchTextBox_LostFocus);
			_listView.PreviewMouseRightButtonUp += new MouseButtonEventHandler(_listView_PreviewMouseRightButtonUp);
			_listView.SelectionChanged += _listView_SelectionChanged;

			Loaded += delegate {
				TabControl parent = WpfUtilities.FindParentControl<TabControl>(this);

				if (parent != null) {
					parent.SelectionChanged += new SelectionChangedEventHandler(_parent_SelectionChanged);
				}
			};

			if (Settings.ContextMenu != null) {
				if (Header is Control)
					((Control)Header).ContextMenu = Settings.ContextMenu;
			}

			if (Settings.Loaded != null) {
				Settings.Loaded((GDbTabWrapper<TKey, ReadableTuple<TKey>>)(object)this, (GTabSettings<TKey, ReadableTuple<TKey>>)(object)Settings, ((GenericDatabase)Database).GetDb<TKey>(Settings.DbData));
			}

			if (Settings.DisplayablePropertyMaker.OnTabVisible != null) {
				Settings.DisplayablePropertyMaker.OnTabVisible(this);
			}

		}

		public Table<T, ReadableTuple<T>> GetTable<T>(ServerDBs db) {
			return ((GenericDatabase) Database).GetTable<T>(db);
		}

		#region Init methods
		private void _initMenus() {
			foreach (GItemCommand<TKey, TValue> commandCopy in Settings.AddedCommands) {
				GItemCommand<TKey, TValue> command = commandCopy;
				MenuItem item = new MenuItem();
				item.Header = command.DisplayName;
				item.Click += (e, a) => _menuItem_Click(command);

				Image image = new Image();
				image.Source = (BitmapSource) ApplicationManager.PreloadResourceImage(command.ImagePath);
				item.Icon = image;

				if (command.Shortcut != null) {
					ApplicationShortcut.Link(command.Shortcut, () => _menuItem_Click(command), List);
					item.InputGestureText = command.Shortcut.DisplayString;
				}

				_listView.ContextMenu.Items.Insert(command.InsertIndex, item);
			}

			_miDelete.Click += _menuItemDeleteItem_Click;
			_miCopyTo.Click += new RoutedEventHandler(_menuItemCopyItemTo_Click);
			_miShowSelected.Click += new RoutedEventHandler(_menuItemKeepSelectedItemsOnly_Click);
			_buttonOpenSubMenu.Click += new RoutedEventHandler(_buttonOpenSubMenu_Click);
			_miChangeId.Click += new RoutedEventHandler(_miChangeId_Click);

			if (!Settings.CanChangeId) {
				_miChangeId.Visibility = Visibility.Collapsed;
			}
		}
		private void _initTableEvents() {
			Table.TupleRemoved += new Table<TKey, TValue>.TableEventHandler(_clientItemDatabase_TupleRemoved);
			Table.TupleAdded += new Table<TKey, TValue>.TableEventHandler(_clientItemDatabase_TupleAdded);
			Table.TupleModified += new Table<TKey, TValue>.TableEventHandler(_clientItemDatabase_TupleModified);
			Table.Commands.PreviewCommandRedo += new AbstractCommand<ITableCommand<TKey, TValue>>.AbstractCommandsEventHandler(_commands_PreviewCommandRedo);
			Table.Commands.PreviewCommandUndo += new AbstractCommand<ITableCommand<TKey, TValue>>.AbstractCommandsEventHandler(_commands_PreviewCommandRedo);
			Table.Commands.CommandRedo += new AbstractCommand<ITableCommand<TKey, TValue>>.AbstractCommandsEventHandler(_commands_CommandRedo);
			Table.Commands.CommandUndo += new AbstractCommand<ITableCommand<TKey, TValue>>.AbstractCommandsEventHandler(_commands_CommandRedo);
			Database.Reloaded += new BaseGenericDatabase.ClientDatabaseEventHandler(_database_Reloaded);
		}

		#endregion

		#region UI events
		private void _miChangeId_Click(object sender, RoutedEventArgs e) {
			ChangeId();
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
						commands.Add(command.Command(rItem));
					}

					Table.Commands.StoreAndExecute(new GroupCommand<TKey, TValue>(commands));
					SearchEngine.Collection.UpdateAndEnable();
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
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
		private void _searchTextBox_LostFocus(object sender, RoutedEventArgs e) {
			_border1.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 132, 144, 161));

			if (_searchTextBox.Text == "") {
				_labelFind.Visibility = Visibility.Visible;
			}
			else {
				_labelFind.Visibility = Visibility.Hidden;
				_searchTextBox.Foreground = new SolidColorBrush(Colors.Gray);
			}
		}
		private void _searchTextBox_GotFocus(object sender, RoutedEventArgs e) {
			_labelFind.Visibility = Visibility.Hidden;
			_border1.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 5, 122, 0));
			_searchTextBox.Foreground = new SolidColorBrush(Colors.Black);
		}
		private void _parent_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (e.AddedItems.Count > 0 && e.AddedItems[0] is GDbTab && ReferenceEquals(e.AddedItems[0], this)) {
				if (DelayedReload) {
					SearchEngine.Filter(this);
					DelayedReload = false;
				}
			}
		}
		private void _menuItemKeepSelectedItemsOnly_Click(object sender, RoutedEventArgs e) {
			if (typeof(TKey) != typeof(int)) {
				ErrorHandler.HandleException("This method is not supported for this type of database.");
				return;
			}

			List<int> tupleIndexes = _listView.SelectedItems.Cast<TValue>().OrderBy(p => p.GetKey<TKey>()).Select(p => p.GetKey<int>()).ToList();
			SearchEngine.SetRange(tupleIndexes);
		}
		private void _buttonOpenSubMenu_Click(object sender, RoutedEventArgs e) {
			_cbSubMenu.IsDropDownOpen = true;
		}
		private void _listView_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			try {
				object item = _listView.InputHitTest(e.GetPosition(_listView));

				if (item is ScrollViewer) {
					e.Handled = true;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
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

					if (_isCurrentTabSelected())
						_listView.SelectedItem = copiedElement;

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
				if (((Grid)Content).IsVisible)
					SearchEngine.Filter(this);
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

			for (int i = 0; i < tuples.Count; i++) {
				if (_listView.Items.Contains(tuples[i]))
					_listView.SelectedItems.Add(tuples[i]);
			}

			_listView.ScrollToCenterOfView(_listView.SelectedItem);
		}
		public List<TValue> GetItems() {
			return _listView.Items.Cast<TValue>().ToList();
		}
		public override void Update() {
			_listView_SelectionChanged(null, null);
		}
		public override void Undo() {
			if (Database is GenericDatabase) {
				GenericDatabase gDb = (GenericDatabase) Database;
				gDb.Commands.Undo();
			}
			else {
				if (Table.Commands.CanUndo) {
					Table.Commands.Undo();
				}
			}
		}
		public override void Redo() {
			if (Database is GenericDatabase) {
				GenericDatabase gDb = (GenericDatabase)Database;
				gDb.Commands.Redo();
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

				Table.Commands.StoreAndExecute(new AddTuple<TKey, TValue>(id, item));
				_listView.ScrollToCenterOfView(item);
			}
			catch (KeyInvalidException) { }
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

				dialog.ShowDialog();

				if (dialog.Result == MessageBoxResult.OK) {
					try {
						Table.Commands.BeginEdit(new GroupCommand<TKey, TValue>());

						string text = dialog.Input;
						string tempPath = TemporaryFilesManager.GetTemporaryFilePath("db_tmp_{0:0000}.txt");
						File.WriteAllText(tempPath, text);

						GenericDatabase gdb = (GenericDatabase)Database;
						gdb.GetDb<TKey>(Settings.DbData).LoadDb(tempPath);
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
		public override void Search() {
			_cbSubMenu.IsDropDownOpen = true;
		}
		public override void Filter() {
			SearchEngine.Filter(this);
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
				Settings.DisplayablePropertyMaker.Reset();
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
							//Table.Commands.BeginEdit(new GroupCommand<TKey, TValue>());
							Table.Commands.StoreAndExecute(new ChangeTupleKey<TKey, TValue>(item.GetValue<TKey>(Settings.AttributeList.PrimaryAttribute), id, _changeKeyCallback));
							_listView.ScrollToCenterOfView(_listView.SelectedItem);
						}
						finally {
							//Table.Commands.EndEdit();
						}
					}

					return;
				}

				Table.Commands.StoreAndExecute(new ChangeTupleKey<TKey, TValue>(item.GetValue<TKey>(Settings.AttributeList.PrimaryAttribute), id, _changeKeyCallback));
				_listView.ScrollToCenterOfView(_listView.SelectedItem);
			}
			catch (KeyInvalidException) { }
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

					Table.Commands.StoreAndExecute(new GroupCommand<TKey, TValue>(commands, _selectLastSelected));
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

				TKey id = _getNewItemId(item.GetKey<TKey>(), true);

				TKey oldId = item.GetValue<TKey>(Settings.AttributeList.PrimaryAttribute);

				if (Table.ContainsKey(id)) {
					if (WindowProvider.ShowDialog("An item with this ID already exists (\"" + Table.TryGetTuple(id)[Settings.AttDisplay.Index].ToString().RemoveBreakLines() + "\"). Do you want to replace it?",
					                              "Item already exists", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes)
						return;
				}

				Table.Commands.StoreAndExecute(new CopyTupleTo<TKey, TValue>(oldId, id, _copyToCallback));
			}
			catch (KeyInvalidException) { }
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
			dialog.ShowDialog();

			if (dialog.Result == MessageBoxResult.OK) {
				try {
					if (typeof (TKey) == typeof (int))
						Int32.Parse(dialog.Input);
				}
				catch (Exception) {
					ErrorHandler.HandleException("Invalid ID format.");
					throw new KeyInvalidException();
				}

				if (typeof (TKey) == typeof (int)) {
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
				return (TKey) (object) Int32.Parse(input);
			}

			return (TKey) (object) input;
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
			TabControl tabControl = WpfUtilities.FindParentControl<TabControl>(this);
			TabItem selectedTab = tabControl.Items[tabControl.SelectedIndex] as TabItem;

			return selectedTab != null && WpfUtilities.IsTab(this, selectedTab.Header.ToString());
		}
		private void _copyToCallback(TKey oldkey, TKey newkey, bool executed) {
			if (executed) {
				Table.GetTuple(newkey).Added = true;

				TabControl tabControl = WpfUtilities.FindParentControl<TabControl>(this);
				TabItem selectedTab = tabControl.Items[tabControl.SelectedIndex] as TabItem;

				if (_isCurrentTabSelected())
					_listView.SelectedItem = Table.GetTuple(newkey);

				_listView.ScrollToCenterOfView(_listView.SelectedItem);
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

		public AbstractDb<T> GetDb<T>(ServerDBs source) {
			return ((GenericDatabase) Database).GetDb<T>(source);
		}
	}
}
