using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Database;
using Database.Commands;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Engines.DatabaseEngine;
using SDE.Tools.DatabaseEditor.Engines.TabNavigationEngine;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using SDE.Tools.DatabaseEditor.WPF;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Extensions = SDE.Core.Extensions;

namespace SDE.Tools.DatabaseEditor.Generic.UI.FormatConverters {
	public partial class CustomItemGroupDisplay<TKey> : FormatConverter<TKey, ReadableTuple<TKey>> {
		private Table<int, ReadableTuple<int>> __itemGroupsTable;
		protected Button _button;
		private DisplayableProperty<TKey, ReadableTuple<TKey>> _dp;
		protected Grid _grid;
		private RangeListView _lv;

		private object _selectedItem;
		protected GDbTabWrapper<TKey, ReadableTuple<TKey>> _tab;
		protected TextBox _textBox;

		private Table<int, ReadableTuple<int>> _itemGroupsTable {
			get { return __itemGroupsTable ?? (__itemGroupsTable = ((GenericDatabase) _tab.Database).GetTable<int>(ServerDbs.ItemGroups)); }
		}

		private void _previewCommandChanged(object sender, ITableCommand<int, ReadableTuple<int>> command) {
			_previewCommandChanged2(null, null);
		}
		private void _commandChanged(object sender, ITableCommand<int, ReadableTuple<int>> command) {
			_commandChanged2(null, null);
		}

		private void _previewCommandChanged2(object sender, IGenericDbCommand command) {
			if (_lv.SelectedItem != null)
				_selectedItem = _lv.SelectedItem;
		}
		private void _commandChanged2(object sender, IGenericDbCommand command) {
			if (_tab.List.SelectedItem != null && _selectedItem != null) {
				ItemView itemView = (ItemView)_selectedItem;

				if (_lv.ItemsSource != null)
					_lv.Dispatch(p => p.SelectedItem = ((RangeObservableCollection<ItemView>)_lv.ItemsSource).FirstOrDefault(q => q.ID == itemView.ID));

				if (_lv.SelectedItem != null) {
					if (!((ItemView)_lv.SelectedItem).Exists()) {
						return;
					}

					((ItemView)_lv.SelectedItem).VisualUpdate();
					_lv.ScrollToCenterOfView(_lv.SelectedItem);
				}

				_lv_SelectionChanged(null, null);
			}
		}

		private void _validateUndo(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, string text, DbAttribute attribute) {
			try {
				if (tab.List.SelectedItem != null && !tab.ItemsEventsDisabled && _lv.SelectedItem != null) {
					ReadableTuple<TKey> tuple = (ReadableTuple<TKey>)tab.List.SelectedItem;
					int tupleKey = ((ItemView) _lv.SelectedItem).ID;

					ITableCommand<TKey, ReadableTuple<TKey>> command = tab.Table.Commands.Last();

					if (command is ChangeTupleDicoProperty<TKey, ReadableTuple<TKey>, TKey, ReadableTuple<TKey>>) {
						ChangeTupleDicoProperty<TKey, ReadableTuple<TKey>, TKey, ReadableTuple<TKey>> changeCommand = (ChangeTupleDicoProperty<TKey, ReadableTuple<TKey>, TKey, ReadableTuple<TKey>>)command;
						IGenericDbCommand last = tab.GenericDatabase.Commands.Last();

						if (last != null) {
							if (last is GenericDbCommand<TKey>) {
								GenericDbCommand<TKey> nLast = (GenericDbCommand<TKey>)last;

								if (ReferenceEquals(nLast.Table, tab.Table)) {
									// The last command of the table is being edited

									if (changeCommand.Tuple != tuple || tupleKey != (int) (object) changeCommand.DicoKey || changeCommand.Attribute.Index != attribute.Index) {
										//tab.Table.Commands.Set(tuple, attribute, text);
									}
									else {
										ItemView itemView = ((ItemView) _lv.SelectedItem);

										changeCommand.NewValue.SetValue(attribute, text);
										changeCommand.Execute(tab.Table);

										if (changeCommand.NewValue.CompareWith(changeCommand.InitialValue)) {
											nLast.Undo();
											tab.GenericDatabase.Commands.RemoveCommands(1);
											tab.Table.Commands.RemoveCommands(1);
											//changeCommand.InitialValue.Modified = changeCommand.SubModified;
											_lv.Dispatch(p => p.SelectedItem = ((RangeObservableCollection<ItemView>) _lv.ItemsSource).FirstOrDefault(q => q.ID == itemView.ID));
										}

										itemView.VisualUpdate();
										return;
									}
								}
							}
						}

						_setSelectedItem(attribute, text);
					}
					else {
						_setSelectedItem(attribute, text);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _lv_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var itemView = _lv.SelectedItem as ItemView;
			_tab.ItemsEventsDisabled = true;

			try {
				if (itemView == null) {
					_dp.Reset();
				}
				else {
					_selectedItem = itemView;
					_dp.Display((ReadableTuple<TKey>)(object)itemView.SubTuple, null);
				}
			}
			finally {
				_tab.ItemsEventsDisabled = false;
			}
		}
		private void _lv_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			_miEditDrop_Click(sender, null);
		}

		private void _miAddDrop_Click(object sender, RoutedEventArgs e) {
			Table<int, ReadableTuple<int>> btable = _itemGroupsTable;

			try {
				DropEdit dialog = new DropEdit("", "1", ServerDbs.Items, _tab.GenericDatabase);
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

					_setSelectedItem(id, value, DicoModifs.Add);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			btable.Commands.EndEdit();
		}

		private void _setSelectedItem(int id, object rate, DicoModifs modif) {
			if (modif == DicoModifs.Delete) {
				var p = (ReadableTuple<int>)_tab.List.SelectedItem;
				_itemGroupsTable.Commands.StoreAndExecute(new ChangeTupleDicoProperty<int, ReadableTuple<int>, int, ReadableTuple<int>>(
					p, ServerItemGroupAttributes.Table, ((ItemView)_lv.SelectedItem).ID));
				return;
			}

			ItemView selectedItem = null;
			ReadableTuple<int> newValue = new ReadableTuple<int>(id, ServerItemGroupSubAttributes.AttributeList);

			if (modif != DicoModifs.Add) {
				selectedItem = (ItemView)_lv.SelectedItem;
				newValue.Copy(selectedItem.SubTuple);
			}

			newValue.SetValue(ServerItemGroupSubAttributes.Rate, rate);
			newValue.SetValue(ServerItemGroupSubAttributes.Id, id);

			Dictionary<int, ReadableTuple<int>> dico = ((ReadableTuple<int>)_tab.List.SelectedItem).GetRawValue<Dictionary<int, ReadableTuple<int>>>(ServerItemGroupAttributes.Table);

			if (dico.ContainsKey(id)) {
				if (selectedItem != null) {
					int selectedId = selectedItem.ID;

					if (selectedId != id) {
						var coll = ((RangeObservableCollection<ItemView>) _lv.ItemsSource);
						coll.Remove(coll.FirstOrDefault(p => p.ID == id));
					}
				}
			}

			if (modif == DicoModifs.Add) {
				newValue.Added = true;

				if (dico.ContainsKey(id)) {
					ErrorHandler.HandleException("The item ID already exists.");
					return;
				}

				_itemGroupsTable.Commands.StoreAndExecute(new ChangeTupleDicoProperty<int, ReadableTuple<int>, int, ReadableTuple<int>>(
					_tab.List.SelectedItem as ReadableTuple<int>, ServerItemGroupAttributes.Table, id, newValue, id, _addedItem));
			}
			else if (modif == DicoModifs.Edit) {
				newValue.Modified = true;
				int oldId = selectedItem.ID;
				selectedItem.ID = id;

				_itemGroupsTable.Commands.StoreAndExecute(new ChangeTupleDicoProperty<int, ReadableTuple<int>, int, ReadableTuple<int>>(
					_tab.List.SelectedItem as ReadableTuple<int>, ServerItemGroupAttributes.Table, oldId, newValue, id));

				selectedItem.VisualUpdate();
			}

			((RangeObservableCollection<ItemView>) _lv.ItemsSource).ToList().ForEach(p => p.VisualUpdate());
		}
		private void _setSelectedItem(DbAttribute attribute, object value) {
			var item = (ItemView)_lv.SelectedItem;
			ReadableTuple<int> newValue = new ReadableTuple<int>(item.ID, ServerItemGroupSubAttributes.AttributeList);
			newValue.Copy(item.SubTuple);
			newValue.SetValue(attribute, value);
			newValue.Modified = true;

			_itemGroupsTable.Commands.StoreAndExecute(new ChangeTupleDicoProperty<int, ReadableTuple<int>, int, ReadableTuple<int>>(
				_tab.List.SelectedItem as ReadableTuple<int>, ServerItemGroupAttributes.Table, item.ID, newValue, item.ID));

			item.VisualUpdate();
		}

		private void _addedItem(ReadableTuple<int> tuple, DbAttribute attribute, int dkey, ReadableTuple<int> dvalue, int newdkey, bool executed) {
			RangeObservableCollection<ItemView> result = (RangeObservableCollection<ItemView>)_lv.ItemsSource;

			if (result == null) {
				result = new RangeObservableCollection<ItemView>();
				_lv.ItemsSource = result;
			}

			if (executed) {
				Dictionary<int, ReadableTuple<int>> dico = (Dictionary<int, ReadableTuple<int>>)tuple.GetRawValue(1);
				var itemView = new ItemView(((GenericDatabase) _tab.Database).GetMetaTable<int>(ServerDbs.Items), dico, dkey);
				Extensions.InsertIntoList(_lv, itemView, result);
				_lv.SelectedItem = itemView;
				_lv.ScrollToCenterOfView(itemView);
			}
			else {
				result.Remove(result.FirstOrDefault(p => p.ID == dkey));
			}
		}
		private void _miSelect_Click(object sender, RoutedEventArgs e) {
			if (_lv.SelectedItems.Count > 0) {
				TabNavigation.SelectList(ServerDbs.Items, _lv.SelectedItems.Cast<ItemView>().ToList().Select(p => p.ID).ToList());
			}
		}
		private void _miEditDrop_Click(object sender, RoutedEventArgs e) {
			if (_lv.SelectedItems.Count <= 0)
				return;

			Table<int, ReadableTuple<int>> btable = _tab.GenericDatabase.GetTable<int>(ServerDbs.ItemGroups);

			try {
				var selectedItem = (ItemView)_lv.SelectedItem;
				DropEdit dialog = new DropEdit(selectedItem.ID.ToString(CultureInfo.InvariantCulture), selectedItem.Rate.ToString(CultureInfo.InvariantCulture), ServerDbs.Items, _tab.GenericDatabase) { Element2 = "Frequency" };
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

					_setSelectedItem(id, value, DicoModifs.Edit);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			btable.Commands.EndEdit();
		}
		private void _miRemoveDrop_Click(object sender, RoutedEventArgs e) {
			if (_lv.SelectedItems.Count <= 0)
				return;

			Table<int, ReadableTuple<int>> btable = _itemGroupsTable;

			btable.Commands.BeginEdit(new GroupCommand<int, ReadableTuple<int>>());

			try {
				for (int i = 0; i < _lv.SelectedItems.Count; i++) {
					var selectedItem = (ItemView)_lv.SelectedItems[i];
					_setSelectedItem(selectedItem.ID, null, DicoModifs.Delete);
					((RangeObservableCollection<ItemView>)_lv.ItemsSource).Remove(selectedItem);
					i--;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			btable.Commands.EndEdit();
			((RangeObservableCollection<ItemView>)_lv.ItemsSource).ToList().ForEach(p => p.VisualUpdate());
		}

		#region Nested type: ItemView

		public class ItemView : INotifyPropertyChanged {
			private readonly Dictionary<int, ReadableTuple<int>> _chances;
			private readonly Table<int, ReadableTuple<int>> _itemTable;
			private ReadableTuple<int> _subTuple;
			private int _totalItems = -1;

			private ReadableTuple<int> _tuple;

			public ItemView(Table<int, ReadableTuple<int>> itemTable, Dictionary<int, ReadableTuple<int>> chances, int id, int totalLength) {
				_itemTable = itemTable;
				_chances = chances;
				_totalItems = totalLength;
				ID = id;

				_reload();
			}

			public ItemView(Table<int, ReadableTuple<int>> itemTable, Dictionary<int, ReadableTuple<int>> chances, int id) {
				_itemTable = itemTable;
				_chances = chances;
				ID = id;

				_reload();
			}

			public ReadableTuple<int> SubTuple {
				get { return _subTuple ?? (_subTuple = _chances[ID]); }
			}

			public int ID { get; set; }
			public int Rate {
				get {
					return SubTuple.GetValue<int>(ServerItemGroupSubAttributes.Rate);
				}
			}
			public int ChanceInt {
				get {
					if (_totalItems < 0) {
						_totalItems = _chances.Where(p => p.Key != 0).Sum(p => p.Value.GetValue<int>(ServerItemGroupSubAttributes.Rate));
					}

					return (int)((Rate / (float)_totalItems) * 100f);
				}
			}

			public bool Modified {
				get { return SubTuple.Modified; }
			}

			public bool Added {
				get { return SubTuple.Added; }
			}

			public bool Normal {
				get { return SubTuple.Normal; }
			}

			public string Drop {
				get { return String.Format("{0}", Rate); }
			}
			public string Name {
				get {
					if (_tuple != null) {
						return (string)_tuple.GetRawValue(ServerItemAttributes.Name.Index);
					}
					return null;
				}
			}
			public string Chance {
				get {
					if (_totalItems < 0) {
						_totalItems = _chances.Where(p => p.Key != 0).Sum(p => p.Value.GetValue<int>(ServerItemGroupSubAttributes.Rate));
					}

					return String.Format("{0:0.00} %", (Rate / (float)_totalItems) * 100f);
				}
			}

			#region INotifyPropertyChanged Members

			public event PropertyChangedEventHandler PropertyChanged;

			#endregion

			private void _reload() {
				if (_tuple != null) {
					_tuple.PropertyChanged -= _propertyChanged;
				}

				_tuple = _itemTable.TryGetTuple(ID);

				if (_tuple != null) {
					_tuple.PropertyChanged += _propertyChanged;
				}
			}

			private void _propertyChanged(object sender, PropertyChangedEventArgs e) {
				OnPropertyChanged();
			}

			public void Detach() {
				if (_tuple != null)
					_tuple.PropertyChanged -= _propertyChanged;
			}

			protected virtual void OnPropertyChanged() {
				_reload();

				PropertyChangedEventHandler handler = PropertyChanged;
				if (handler != null) handler(this, new PropertyChangedEventArgs(""));
			}

			public void VisualUpdate() {
				_totalItems = -1;
				_subTuple = null;
				OnPropertyChanged();
			}

			public bool Exists() {
				return _chances.ContainsKey(ID);
			}
		}

		#endregion
	}
}