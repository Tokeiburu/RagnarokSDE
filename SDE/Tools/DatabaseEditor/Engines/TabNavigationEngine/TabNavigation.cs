using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using Database;
using ErrorManager;
using GRF.Threading;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using TokeiLibrary;
using Utilities;

namespace SDE.Tools.DatabaseEditor.Engines.TabNavigationEngine {
	/// <summary>
	/// This class keeps track of the selected tabs.
	/// It also selects items in the various tables.
	/// </summary>
	public class TabNavigation : AbstractCommand<INagivationCommand> {
		private readonly TabControl _tab;
		private SelectionChanged _firstSelection;
		private DateTime _now;
		private INagivationCommand _pendingCommand;

		public TabNavigation(TabControl tab) {
			_tab = tab;
			Instance = this;
			_now = DateTime.Now;
		}

		public static TabNavigation Instance { get; set; }

		public bool Disabled { get; set; }

		protected override void _execute(INagivationCommand command) {
			command.Execute(this);
		}

		protected override void _undo(INagivationCommand command) {
			if (_pendingCommand != null) {
				_storeAndExecute(_pendingCommand, true);
			}

			command.Undo(this);
		}

		protected override void _redo(INagivationCommand command) {
			command.Execute(this);
		}

		public override void StoreAndExecute(INagivationCommand command) {
			_storeAndExecute(command, false);
		}

		public static void Select(ServerDbs db, Tuple tuple) {
			Instance.Select2(db, tuple);
		}

		public static void Select<TKey>(ServerDbs db, TKey id) {
			Instance.SelectInternal(db, new List<TKey> { id });
		}

		public static void SelectList<TKey>(ServerDbs db, IEnumerable<TKey> id) {
			try {
				List<TKey> result = id.ToList();

				if (result.Count == 0) {
					ErrorHandler.HandleException("No items match the query in [" + db.DisplayName + "].", ErrorLevel.NotSpecified);
					return;
				}

				Instance.SelectInternal(db, result);
			}
			catch (Exception err) {
				ErrorHandler.HandleException("Failed to parse the search query.\r\n\r\n" + err.Message);
			}
		}

		public static void SelectList<TKey>(ServerDbs db, List<TKey> id) {
			if (id.Count == 0)
				return;

			Instance.SelectInternal(db, id);
		}

		public void Select2(ServerDbs tabName, Tuple tuple) {
			if (tuple.Attributes.PrimaryAttribute.DataType == typeof(int)) {
				SelectInternal(tabName, new List<int> { tuple.GetKey<int>() });
			}
			else if (tuple.Attributes.PrimaryAttribute.DataType == typeof(string)) {
				SelectInternal(tabName, new List<string> { tuple.GetKey<string>() });
			}
		}

		public void SelectInternal<TKey>(ServerDbs tabName, List<TKey> tuplesGen) {
			GrfThread.Start(() => _selectInternal(tabName, tuplesGen), "TabNavigationEngine - Select tuple");
		}

		private void _selectInternal<TKey>(ServerDbs tabName, List<TKey> tuplesGen) {
			if (tabName.AdditionalTable == null) {
				TabItem item = _tab.Dispatch(() => _tab.Items.Cast<TabItem>().FirstOrDefault(p => p.Header.ToString() == tabName));

				if (item is GDbTab) {
					GDbTab tab = (GDbTab)item;

					var table = tab.To<TKey>().Table;
					List<Tuple> tuples = tuplesGen.Select(table.TryGetTuple).Where(p => p != null).Select(p => (Tuple)p).ToList();

					if (tuples.Count == 0) {
						ErrorHandler.HandleException((tuplesGen.Count > 1 ? "Items do" : "Item does") + " not exist in [" + tabName.DisplayName + "].", ErrorLevel.NotSpecified);
						return;
					}

					if (!_containsAny(tab, tuples)) {
						tab.Filter();
						_waitForFilter(tab);

						if (!_containsAny(tab, tuples)) {
							ErrorHandler.HandleException((tuplesGen.Count > 1 ? "Items" : "Item") + " not found in [" + tabName.DisplayName + "]. Try clearing the search filter on the specified table.", ErrorLevel.NotSpecified);
							return;
						}
					}

					tab.Dispatch(p => p.IsSelected = true);
					_waitForFilter(tab);
					tab.Dispatch(p => p.SelectItems(tuples));
				}
				else {
					item.Dispatch(p => p.IsSelected = true);
				}
			}
			else {
				TabItem item = _tab.Dispatch(() => _tab.Items.Cast<TabItem>().FirstOrDefault(p => p.Header.ToString() == tabName));
				TabItem item2 = _tab.Dispatch(() => _tab.Items.Cast<TabItem>().FirstOrDefault(p => p.Header.ToString() == tabName.AdditionalTable));

				if (item is GDbTab && item2 is GDbTab) {
					GDbTab tab = (GDbTab)item;
					GDbTab tab2 = (GDbTab)item2;

					var table = tab.To<TKey>().Table;
					var table2 = tab2.To<TKey>().Table;
					List<Tuple> tuples = tuplesGen.Select(table.TryGetTuple).Where(p => p != null).Select(p => (Tuple)p).ToList();
					List<Tuple> tuples2 = tuplesGen.Select(table2.TryGetTuple).Where(p => p != null).Select(p => (Tuple)p).ToList();

					if (tuples.Count == 0 && tuples2.Count == 0) {
						ErrorHandler.HandleException((tuplesGen.Count > 1 ? "Items do" : "Item does") + " not exist in either [" + tabName.DisplayName + "] or [" + tabName.AdditionalTable.DisplayName + "].", ErrorLevel.NotSpecified);
						return;
					}

					if (!_containsAny(tab, tuples) && !_containsAny(tab2, tuples2)) {
						tab.Filter();
						_waitForFilter(tab);

						tab2.Filter();
						_waitForFilter(tab2);

						if (!_containsAny(tab, tuples) && !_containsAny(tab2, tuples2)) {
							ErrorHandler.HandleException((tuplesGen.Count > 1 ? "Items" : "Item") + " not found in either [" + tabName.DisplayName + "] or [" + tabName.AdditionalTable.DisplayName + "]. Try clearing the search filter on the specified table.", ErrorLevel.NotSpecified);
							return;
						}
					}

					GDbTab tabToSelect = _containsAny(tab, tuples) ? tab : tab2;

					tabToSelect.Dispatch(p => p.IsSelected = true);
					_waitForFilter(tabToSelect);
					tabToSelect.Dispatch(p => p.SelectItems(tabToSelect == tab ? tuples : tuples2));
				}
				else {
					item.Dispatch(p => p.IsSelected = true);
				}
			}
		}

		private bool _containsAny(GDbTab tab, List<Tuple> tuples) {
			return tab.Dispatch(new Func<bool>(delegate {

				for (int i = 0; i < tuples.Count; i++) {
					if (tab._listView.Items.Contains(tuples[i]))
						return true;
				}

				return false;
			}));
		}

		private static void _waitForFilter(GDbTab tab) {
			int max = 20;
			while (tab.IsFiltering && max > 0) {
				Thread.Sleep(200);
				max--;
			}
		}

		public void Select(string tabName, object tuple, ListView view) {
			Disabled = true;
			try {
				foreach (TabItem item in _tab.Items) {
					if (item.Header.ToString() == tabName) {
						item.IsSelected = true;
						view.SelectedItem = tuple;
						view.ScrollIntoView(tuple);
						break;
					}
				}
			}
			finally {
				Disabled = false;
			}
		}

		public override List<INagivationCommand> GetUndoCommands() {
			if (_pendingCommand != null) {
				_storeAndExecute(_pendingCommand, true);
			}

			List<INagivationCommand> commands = _commands.Take(_commandIndexCurrent).ToList();
			commands.Insert(0, _firstSelection);
			return commands;
		}

		private void _storeAndExecute(INagivationCommand command, bool forceSet = false) {
			if (Disabled)
				return;

			SelectionChanged sc = command as SelectionChanged;

			if (sc == null || sc.View == null || sc.Tuple == null)
				return;

			if ((DateTime.Now - _now).TotalMilliseconds < 200 && !forceSet) {
				_pendingCommand = command;
				_now = DateTime.Now;
				return;
			}

			if (_pendingCommand != null && _pendingCommand != command) {
				_storeAndExecute(_pendingCommand, true);
			}

			_pendingCommand = null;

			if (_firstSelection == null) {
				_firstSelection = (SelectionChanged) command;
				return;
			}

			sc.PreviousPosition = GetLastCommand();

			if (sc.PreviousPosition != null && sc.PreviousPosition.Tuple == sc.Tuple && ReferenceEquals(sc.PreviousPosition.View, sc.View))
				return;

			base.Store(command);

			lock (_thisLock) {
				while (_commands.Count > 30) {
					_firstSelection = (SelectionChanged) _commands[0];
					_commands.RemoveAt(0);
					_commandIndexCurrent--;
				}
			}

			_now = new DateTime(DateTime.Now.Ticks);
		}

		public SelectionChanged GetLastCommand() {
			if (_commandIndexCurrent == -1)
				return _firstSelection;

			return _commands[_commandIndexCurrent] as SelectionChanged;
		}
	}
}
