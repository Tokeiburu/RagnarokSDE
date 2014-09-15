using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Database;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Generic.IndexProviders;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.Lists.FormatConverter;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Generic.CustomControls {
	public partial class CustomItemGroupDisplay<TKey> : FormatConverter<TKey, ReadableTuple<TKey>> {
		#region DicoModifs enum

		public enum DicoModifs {
			Edit,
			Delete,
			Add
		}

		#endregion

		public override void Init(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, DisplayableProperty<TKey, ReadableTuple<TKey>> dp) {
			_tab = tab;
			_initSettings(tab, dp);

			GenericDatabase gdb = ((GenericDatabase)_tab.Database);
			_itemGroupsTable.Commands.PreviewCommandUndo		+= _previewCommandChanged;
			_itemGroupsTable.Commands.PreviewCommandRedo		+= _previewCommandChanged;
			_itemGroupsTable.Commands.PreviewCommandExecuted	+= _previewCommandChanged;
			_itemGroupsTable.Commands.CommandUndo				+= _commandChanged;
			_itemGroupsTable.Commands.CommandRedo				+= _commandChanged;
			_itemGroupsTable.Commands.CommandExecuted			+= _commandChanged;

			gdb.Commands.PreviewCommandUndo			+= _previewCommandChanged2;
			gdb.Commands.PreviewCommandRedo			+= _previewCommandChanged2;
			gdb.Commands.PreviewCommandExecuted		+= _previewCommandChanged2;
			gdb.Commands.CommandUndo				+= _commandChanged2;
			gdb.Commands.CommandRedo				+= _commandChanged2;
			gdb.Commands.CommandExecuted			+= _commandChanged2;

			Grid grid = new Grid();

			grid.SetValue(Grid.RowProperty, _row);
			grid.SetValue(Grid.ColumnProperty, 0);
			grid.SetValue(Grid.ColumnSpanProperty, 1);

			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto) });
			grid.RowDefinitions.Add(new RowDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition());

			Label label = new Label();
			label.Content = "Item IDs";
			label.FontStyle = FontStyles.Italic;
			label.Padding = new Thickness(0);
			label.Margin = new Thickness(3);

			_lv = new RangeListView();
			_lv.SetValue(TextSearch.TextPathProperty, "ID");
			_lv.SetValue(WpfUtils.IsGridSortableProperty, true);
			_lv.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
			_lv.SetValue(Grid.RowProperty, 1);
			_lv.FocusVisualStyle = null;
			_lv.Margin = new Thickness(3);
			_lv.BorderThickness = new Thickness(1);
			_lv.HorizontalAlignment = HorizontalAlignment.Left;
			_lv.SelectionChanged += _lv_SelectionChanged;

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_lv, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.GeneralColumnInfo {Header = ServerItemAttributes.Id.DisplayName, DisplayExpression = "ID", SearchGetAccessor = "ID", FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "ID"},
				new ListViewDataTemplateHelper.RangeColumnInfo {Header = ServerItemAttributes.Name.DisplayName, DisplayExpression = "Name", SearchGetAccessor = "Name", IsFill = true, ToolTipBinding = "Name", TextWrapping = TextWrapping.Wrap, MinWidth = 70 },
				new ListViewDataTemplateHelper.GeneralColumnInfo {Header = "Freq", DisplayExpression = "Drop", SearchGetAccessor = "Rate", ToolTipBinding = "Rate", FixedWidth = 40, TextAlignment = TextAlignment.Right},
				new ListViewDataTemplateHelper.GeneralColumnInfo {Header = "Drop %", DisplayExpression = "Chance", SearchGetAccessor = "ChanceInt", ToolTipBinding = "Chance", FixedWidth = 60, TextAlignment = TextAlignment.Right}
			}, new DefaultListViewComparer<ItemView>(), new string[] { "Added", "Blue", "Modified", "Green", "Normal", "Black" });

			_lv.ContextMenu = new ContextMenu();
			_lv.MouseDoubleClick += new MouseButtonEventHandler(_lv_MouseDoubleClick);

			MenuItem miSelect = new MenuItem { Header = "Select", Icon = new Image { Source = ApplicationManager.GetResourceImage("arrowdown.png") } };
			MenuItem miEditDrop = new MenuItem { Header = "Edit", Icon = new Image { Source = ApplicationManager.GetResourceImage("properties.png") } };
			MenuItem miRemoveDrop = new MenuItem { Header = "Remove", Icon = new Image { Source = ApplicationManager.GetResourceImage("delete.png") } };
			MenuItem miAddDrop = new MenuItem { Header = "Add", Icon = new Image { Source = ApplicationManager.GetResourceImage("add.png") } };

			_lv.ContextMenu.Items.Add(miSelect);
			_lv.ContextMenu.Items.Add(miEditDrop);
			_lv.ContextMenu.Items.Add(miRemoveDrop);
			_lv.ContextMenu.Items.Add(miAddDrop);

			miSelect.Click += new RoutedEventHandler(_miSelect_Click);
			miEditDrop.Click += new RoutedEventHandler(_miEditDrop_Click);
			miRemoveDrop.Click += new RoutedEventHandler(_miRemoveDrop_Click);
			miAddDrop.Click += new RoutedEventHandler(_miAddDrop_Click);

			dp.AddUpdateAction(new Action<ReadableTuple<TKey>>(delegate(ReadableTuple<TKey> item) {
				Dictionary<int, ReadableTuple<int>> groups = (Dictionary<int, ReadableTuple<int>>) item.GetRawValue(1);

				if (groups == null) {
					groups = new Dictionary<int, ReadableTuple<int>>();
					item.SetRawValue(1, groups);
				}

				Table<int, ReadableTuple<int>> btable = ((GenericDatabase)tab.Database).GetTable<int>(ServerDBs.Items);

				if (groups.Count == 0) {
					_lv.ItemsSource = null;
					return;
				}

				List<ItemView> result = new List<ItemView>();

				try {
					int changes = groups.Where(p => p.Key != 0).Sum(p => p.Value.GetValue<int>(ServerItemGroupSubAttributes.Rate));

					foreach (var keypair in groups.OrderBy(p => p.Key)) {
						result.Add(new ItemView(btable, groups, keypair.Key, changes));
					}
				}
				catch {
				}

				_lv.ItemsSource = new RangeObservableCollection<ItemView>(result);
			}));

			_dp = new DisplayableProperty<TKey, ReadableTuple<TKey>>();
			int line = 0;
			Grid subGrid = GTabsMaker.PrintGrid(ref line, 2, 1, 1, new SpecifiedIndexProvider(new int[] {
				//ServerItemGroupSubAttributes.Id.Index, -1,
				ServerItemGroupSubAttributes.Rate.Index, -1,
				ServerItemGroupSubAttributes.Amount.Index, -1,
				ServerItemGroupSubAttributes.Random.Index, -1,
				ServerItemGroupSubAttributes.IsAnnounced.Index, -1,
				ServerItemGroupSubAttributes.Duration.Index, -1,
				ServerItemGroupSubAttributes.IsNamed.Index, -1,
				ServerItemGroupSubAttributes.IsBound.Index, -1
			}), -1, 0, -1, -1, _dp, ServerItemGroupSubAttributes.AttributeList);

			subGrid.VerticalAlignment = VerticalAlignment.Top;

			grid.Children.Add(label);
			grid.Children.Add(_lv);
			tab.PropertiesGrid.RowDefinitions.Clear();
			tab.PropertiesGrid.RowDefinitions.Add(new RowDefinition());
			tab.PropertiesGrid.ColumnDefinitions.Clear();
			tab.PropertiesGrid.ColumnDefinitions.Add(new ColumnDefinition { MaxWidth = 340 });
			tab.PropertiesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
			tab.PropertiesGrid.ColumnDefinitions.Add(new ColumnDefinition());
			tab.PropertiesGrid.Children.Add(grid);
			_dp.Deploy(_tab, null, true);

			foreach (var update in _dp.Updates) {
				Tuple<DbAttribute, FrameworkElement> x = update;

				if (x.Item1.DataType == typeof(int)) {
					TextBox element = (TextBox)x.Item2;
					_dp.AddUpdateAction(new Action<ReadableTuple<TKey>>(item => element.Dispatch(
						delegate {
							Debug.Ignore(() => element.Text = item.GetValue<int>(x.Item1).ToString(CultureInfo.InvariantCulture));
							element.UndoLimit = 0;
							element.UndoLimit = int.MaxValue;
						})));

					element.TextChanged += delegate {
						if (tab.ItemsEventsDisabled) return;

						try {
							if (tab.List.SelectedItem != null && _lv.SelectedItem != null) {
								_setSelectedItem(x.Item1, element.Text);
							}
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					};
				}
				else if (x.Item1.DataType == typeof(bool)) {
					CheckBox element = (CheckBox)x.Item2;
					_dp.AddUpdateAction(new Action<ReadableTuple<TKey>>(item => element.Dispatch(p => Debug.Ignore(() => p.IsChecked = item.GetValue<bool>(x.Item1)))));

					element.Checked += delegate {
						if (tab.ItemsEventsDisabled) return;

						try {
							if (tab.List.SelectedItem != null && _lv.SelectedItem != null) {
								_setSelectedItem(x.Item1, true);
							}
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					};

					element.Unchecked += delegate {
						if (tab.ItemsEventsDisabled) return;

						try {
							if (tab.List.SelectedItem != null && _lv.SelectedItem != null) {
								_setSelectedItem(x.Item1, false);
							}
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					};
				}
				else if (x.Item1.DataType == typeof(string)) {
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
							catch { }
						})));

					element.TextChanged += delegate {
						_validateUndo(tab, element.Text, x.Item1);
					};
				}
			}
		}

		private void _initSettings(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, DisplayableProperty<TKey, ReadableTuple<TKey>> dp) {
			var settings = tab.Settings;
			var gdb = ((GenericDatabase) tab.Database).GetDb<int>(ServerDBs.ItemGroups);

			List<DbAttribute> attributes = ServerItemGroupSubAttributes.AttributeList.Attributes;

			if (attributes.Any(p => p.IsSkippable)) {
				foreach (var attributeIntern in attributes.Where(p => p.IsSkippable)) {
					var attribute = attributeIntern;
					var menuItemSkippable = new MenuItem { Header = attribute.DisplayName + " [" + attribute.AttributeName + ", " + attribute.Index + "]", Icon = new Image { Source = ApplicationManager.GetResourceImage("add.png") } };
					menuItemSkippable.IsEnabled = false;
					menuItemSkippable.Click += delegate {
						gdb.Attached["EntireRewrite"] = true;
						gdb.Attached[attribute.DisplayName] = gdb.Attached[attribute.DisplayName] != null && !(bool)gdb.Attached[attribute.DisplayName];
						gdb.To<TKey>().TabGenerator.OnTabVisualUpdate(tab, settings, gdb);
					};
					settings.ContextMenu.Items.Add(menuItemSkippable);
				}

				gdb.Attached.CollectionChanged += delegate {
					int index = 2;

					foreach (var attributeIntern in attributes.Where(p => p.IsSkippable)) {
						var attribute = attributeIntern;
						int index1 = index;
						settings.ContextMenu.Dispatch(delegate {
							var menuItemSkippable = (MenuItem)settings.ContextMenu.Items[index1];
							menuItemSkippable.IsEnabled = true;
							bool isSet = gdb.Attached[attribute.DisplayName] == null || (bool)gdb.Attached[attribute.DisplayName];

							menuItemSkippable.Icon = new Image { Source = (BitmapSource)ApplicationManager.PreloadResourceImage(isSet ? "delete.png" : "add.png") };
						});

						index++;
					}
				};
			}
		}
	}
}