using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Database;
using Database.Commands;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Engines.DatabaseEngine;
using SDE.Tools.DatabaseEditor.Engines.TabNavigationEngine;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using Extensions = SDE.Others.Extensions;

namespace SDE.Tools.DatabaseEditor.Generic.UI.CustomControls {
	public class CustomQueryViewerMobDroppedBy<TKey, TValue> : ICustomControl<TKey, TValue> where TValue : Tuple {
		private readonly int _cSpan;
		private readonly int _col;
		private readonly int _rSpan;
		private readonly int _row;
		private RangeListView _lv;
		private GDbTabWrapper<TKey, TValue> _tab;

		public CustomQueryViewerMobDroppedBy(int row, int col, int rSpan, int cSpan) {
			_row = row;
			_col = col;
			_rSpan = rSpan;
			_cSpan = cSpan;
		}

		#region ICustomControl<TKey,TValue> Members

		public void Init(GDbTabWrapper<TKey, TValue> tab, DisplayableProperty<TKey, TValue> dp) {
			_tab = tab;
			Grid grid = new Grid();
			WpfUtilities.SetGridPosition(grid, _row, _rSpan, _col, _cSpan);

			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto)});
			grid.RowDefinitions.Add(new RowDefinition());

			Label label = new Label();
			label.Content = "Dropped by";
			label.FontStyle = FontStyles.Italic;
			label.Padding = new Thickness(0);
			label.Margin = new Thickness(3);

			_lv = new RangeListView();
			_lv.SetValue(TextSearch.TextPathProperty, "ID");
			_lv.SetValue(WpfUtils.IsGridSortableProperty, true);
			_lv.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
			_lv.SetValue(Grid.RowProperty, _row);
			_lv.FocusVisualStyle = null;
			_lv.Margin = new Thickness(3);
			_lv.BorderThickness = new Thickness(1);
			WpfUtils.DisableContextMenuIfEmpty(_lv);

			Extensions.GenerateListViewTemplate(_lv, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
					new ListViewDataTemplateHelper.GeneralColumnInfo {Header = ServerMobAttributes.Id.DisplayName, DisplayExpression = "ID", SearchGetAccessor = "ID", FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "ID"},
					new ListViewDataTemplateHelper.RangeColumnInfo {Header = ServerMobAttributes.KRoName.DisplayName, DisplayExpression = "Name", SearchGetAccessor = "Name", IsFill = true, ToolTipBinding = "Name", TextWrapping = TextWrapping.Wrap, MinWidth = 40 },
					new ListViewDataTemplateHelper.GeneralColumnInfo {Header = "Drop %", DisplayExpression = "Drop", SearchGetAccessor = "DropOriginal", ToolTipBinding = "DropOriginal", FixedWidth = 60, TextAlignment = TextAlignment.Right},
					new ListViewDataTemplateHelper.GeneralColumnInfo {Header = "Type", DisplayExpression = "MVP", SearchGetAccessor = "MVP", FixedWidth = 45, TextAlignment = TextAlignment.Center},
				}, new DefaultListViewComparer<MobDropView>(), new string[] { "Modified", "Green", "Added", "Blue", "Default", "Black", "IsMvp", "#FFBA6200" });

			_lv.ContextMenu = new ContextMenu();
			_lv.MouseDoubleClick += new MouseButtonEventHandler(_lv_MouseDoubleClick);

			MenuItem miSelect = new MenuItem { Header = "Select", Icon = new Image { Source = (BitmapSource) ApplicationManager.PreloadResourceImage("arrowdown.png") } };
			MenuItem miEditDrop = new MenuItem { Header = "Edit drop chance", Icon = new Image { Source = (BitmapSource) ApplicationManager.PreloadResourceImage("properties.png") } };
			MenuItem miRemoveDrop = new MenuItem { Header = "Remove", Icon = new Image { Source = (BitmapSource) ApplicationManager.PreloadResourceImage("delete.png") } };

			_lv.ContextMenu.Items.Add(miSelect);
			_lv.ContextMenu.Items.Add(miEditDrop);
			_lv.ContextMenu.Items.Add(miRemoveDrop);

			miSelect.Click += new RoutedEventHandler(_miSelect_Click);
			miEditDrop.Click += new RoutedEventHandler(_miEditDrop_Click);
			miRemoveDrop.Click += new RoutedEventHandler(_miRemoveDrop_Click);

			dp.AddUpdateAction(new Action<TValue>(_update));

			grid.Children.Add(label);
			grid.Children.Add(_lv);

			((GenericDatabase) tab.Database).Commands.CommandIndexChanged += _commands_CommandIndexChanged;
			tab.PropertiesGrid.Children.Add(grid);
			dp.AddResetField(_lv);
		}

		#endregion

		private void _update(TValue item) {
			Table<int, ReadableTuple<int>> btable = _tab.GetMetaTable<int>(ServerDbs.Mobs);
			string id = item.GetKey<int>().ToString(CultureInfo.InvariantCulture);

			if (item.GetKey<int>() == 0) {
				_lv.ItemsSource = null;
				return;
			}

			List<ReadableTuple<int>> tuples = btable.FastItems;
			List<MobDropView> result = new List<MobDropView>();

			try {
				int startIndex;
				bool found;

				for (int i = 0; i < tuples.Count; i++) {
					var p = tuples[i];

					found = false;
					startIndex = ServerMobAttributes.Mvp1ID.Index;

					for (int j = 0; j < 6; j += 2) {
						if ((string) p.GetRawValue(startIndex + j) == id) {
							result.Add(new MobDropView(p, startIndex + j));
							found = true;
							break;
						}
					}

					if (found)
						continue;

					startIndex = ServerMobAttributes.Drop1ID.Index;

					for (int j = 0; j < 20; j += 2) {
						if ((string) p.GetRawValue(startIndex + j) == id) {
							result.Add(new MobDropView(p, startIndex + j));
							break;
						}
					}
				}
			}
			catch {
			}

			_lv.ItemsSource = new RangeObservableCollection<MobDropView>(result.OrderBy(p => p, Extensions.BindDefaultSearch<MobDropView>(_lv, "ID")));
		}

		private void _commands_CommandIndexChanged(object sender, IGenericDbCommand command) {
			_tab.BeginDispatch(delegate {
				if (_tab.List.SelectedItem != null)
					_update(_tab.List.SelectedItem as TValue);
			});
		}

		private void _miEditDrop_Click(object sender, RoutedEventArgs e) {
			if (_lv.SelectedItems.Count <= 0)
				return;

			Table<int, ReadableTuple<int>> btable = _tab.GenericDatabase.GetMetaTable<int>(ServerDbs.Mobs);
			int startIndex;

			try {
				var selectedItem = (MobDropView) _lv.SelectedItem;
				var p = btable.GetTuple(selectedItem.ID);
				string id = ((ReadableTuple<int>) _tab.List.SelectedItem).GetKey<int>().ToString(CultureInfo.InvariantCulture);
				InputDialog dialog = new InputDialog("Enter the new drop rate (integer or float)", "Drop rate", selectedItem.DropOriginal.ToString(CultureInfo.InvariantCulture));
				dialog.Owner = WpfUtilities.TopWindow;
				dialog.TextBoxInput.Loaded += delegate {
					dialog.TextBoxInput.SelectAll();
					dialog.TextBoxInput.Focus();
				};
				dialog.ShowDialog();

				if (dialog.Result == MessageBoxResult.OK) {
					string dResult = dialog.Input;
					int value;

					if (!Extensions.GetIntFromFloatValue(dResult, out value)) {
						ErrorHandler.HandleException("Invalid format (integer or float value only)");
						return;
					}

					btable.Commands.BeginEdit(new GroupCommand<int, ReadableTuple<int>>());
					startIndex = ServerMobAttributes.Mvp1ID.Index;

					for (int j = 0; j < 6; j += 2) {
						if ((string) p.GetRawValue(startIndex + j) == id) {
							btable.Commands.Set(p, startIndex + j + 1, value);
						}
					}

					startIndex = ServerMobAttributes.Drop1ID.Index;

					for (int j = 0; j < 20; j += 2) {
						if ((string) p.GetRawValue(startIndex + j) == id) {
							btable.Commands.Set(p, startIndex + j + 1, value);
							break;
						}
					}
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

			Table<int, ReadableTuple<int>> btable = _tab.GenericDatabase.GetMetaTable<int>(ServerDbs.Mobs);
			int startIndex;

			btable.Commands.BeginEdit(new GroupCommand<int, ReadableTuple<int>>());

			try {
				for (int i = 0; i < _lv.SelectedItems.Count; i++) {
					var selectedItem = (MobDropView) _lv.SelectedItems[i];
					var p = btable.GetTuple(selectedItem.ID);
					string id = ((ReadableTuple<int>) _tab.List.SelectedItem).GetKey<int>().ToString(CultureInfo.InvariantCulture);

					startIndex = ServerMobAttributes.Mvp1ID.Index;

					for (int j = 0; j < 6; j += 2) {
						if ((string) p.GetRawValue(startIndex + j) == id) {
							btable.Commands.Set(p, startIndex + j, 0);
							btable.Commands.Set(p, startIndex + j + 1, 0);
						}
					}

					startIndex = ServerMobAttributes.Drop1ID.Index;

					for (int j = 0; j < 20; j += 2) {
						if ((string) p.GetRawValue(startIndex + j) == id) {
							btable.Commands.Set(p, startIndex + j, 0);
							btable.Commands.Set(p, startIndex + j + 1, 0);
							break;
						}
					}

					((RangeObservableCollection<MobDropView>) _lv.ItemsSource).Remove(selectedItem);
					i--;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			btable.Commands.EndEdit();
		}

		private void _lv_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			ListViewItem item = _lv.GetObjectAtPoint<ListViewItem>(e.GetPosition(_lv)) as ListViewItem;

			if (item != null) {
				TabNavigation.Select(ServerDbs.Mobs, ((MobDropView)item.Content).ID);
			}
		}

		private void _miSelect_Click(object sender, RoutedEventArgs e) {
			if (_lv.SelectedItems.Count > 0) {
				TabNavigation.SelectList(ServerDbs.Mobs, _lv.SelectedItems.Cast<MobDropView>().Select(p => p.ID));
			}
		}

		#region Nested type: MobDropView

		public class MobDropView : INotifyPropertyChanged {
			private readonly int _index;
			private readonly ReadableTuple<int> _tuple;

			public MobDropView(ReadableTuple<int> tuple, int index) {
				_tuple = tuple;
				_index = index;
				_tuple.PropertyChanged += (s, e) => OnPropertyChanged();

				_reload();
			}

			public string Drop { get; private set; }

			public int ID { get; private set; }
			public string Name { get; private set; }
			public string MVP { get; private set; }
			public int DropOriginal { get; private set; }
			public bool IsMvp {
				get { return MVP != ""; }
			}

			public bool Default {
				get { return true; }
			}

			#region INotifyPropertyChanged Members

			public event PropertyChangedEventHandler PropertyChanged;

			#endregion

			private void _reload() {
				ID = _tuple.GetKey<int>();
				Name = (string) _tuple.GetRawValue(ServerMobAttributes.KRoName.Index);
				DropOriginal = Int32.Parse(((string)_tuple.GetRawValue(_index + 1)));
				Drop = String.Format("{0:0.00} %", DropOriginal / 100f);
				MVP = _index < ServerMobAttributes.Drop1ID.Index ? "MVP" : "";
			}

			protected virtual void OnPropertyChanged() {
				_reload();

				PropertyChangedEventHandler handler = PropertyChanged;
				if (handler != null) handler(this, new PropertyChangedEventArgs(""));
			}
		}

		#endregion
	}
}
