using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Database;
using ErrorManager;
using GRF.Image;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.TabNavigationEngine;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.View;
using SDE.View.Dialogs;
using SDE.View.ObjectView;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using TokeiLibrary.WpfBugFix;
using Utilities;
using Utilities.Extension;
using Utilities.Services;
using Extensions = SDE.Core.Extensions;

namespace SDE.Editor.Generic.UI.CustomControls {
	public class CustomEvolutionProperty<TKey, TValue> : ICustomControl<TKey, TValue> where TValue : Database.Tuple {
		private readonly int _rSpan;
		private readonly int _row;
		private RangeListView _lv;
		private RangeListView _lvRequirements;
		private GDbTabWrapper<TKey, TValue> _tab;
		private Action<TValue> _updateAction;
		private Evolution _evolution;
		private int _lastSelectedIndex1 = 0;
		private int _lastSelectedIndex2 = -1;
		private DefaultListViewComparer<PetEvolutionView> _evolutionSorter;
		private DefaultListViewComparer<PetEvolutionTargetView> _evolutionTargetSorter;
		private TValue _item;

		public CustomEvolutionProperty(int row, int rSpan) {
			_row = row;
			_rSpan = rSpan;
		}

		#region ICustomControl<TKey,TValue> Members
		public void Init(GDbTabWrapper<TKey, TValue> tab, DisplayableProperty<TKey, TValue> dp) {
			_tab = tab;
			Grid grid = new Grid();

			grid.SetValue(Grid.RowProperty, _row);
			grid.SetValue(Grid.RowSpanProperty, _rSpan);
			grid.SetValue(Grid.ColumnProperty, 0);
			grid.SetValue(Grid.ColumnSpanProperty, 5);

			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto) });
			grid.RowDefinitions.Add(new RowDefinition());

			grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star)});
			grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(6, GridUnitType.Star)});

			Label label = new Label();
			label.Content = "Evolutions";
			label.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextForeground"] as Brush);
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
			_lv.PreviewMouseRightButtonUp += _lv_PreviewMouseRightButtonUp;

			_evolutionSorter = new DefaultListViewComparer<PetEvolutionView>(true);

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_lv, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = ServerMobAttributes.Id.DisplayName, DisplayExpression = "ID", SearchGetAccessor = "ID", FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "ID" },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "Name", DisplayExpression = "EvolutionName", SearchGetAccessor = "EvolutionName", IsFill = true, ToolTipBinding = "EvolutionName", TextWrapping = TextWrapping.Wrap, MinWidth = 40 },
			}, _evolutionSorter, new string[] { "Default", "{DynamicResource TextForeground}" });

			_lv.ContextMenu = new ContextMenu();
			_lv.MouseDoubleClick += new MouseButtonEventHandler(_lv_MouseDoubleClick);

			MenuItem miSelect = new MenuItem { Header = "Select", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("arrowdown.png") } };
			MenuItem miEditDrop = new MenuItem { Header = "Edit", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("properties.png") } };
			MenuItem miRemoveDrop = new MenuItem { Header = "Remove", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("delete.png") }, InputGestureText = "Del" };
			MenuItem miCopy = new MenuItem { Header = "Copy", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("copy.png") }, InputGestureText = "Ctrl-C" };
			MenuItem miPaste = new MenuItem { Header = "Paste", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("paste.png") }, InputGestureText = "Ctrl-V" };
			MenuItem miAddEvolution = new MenuItem { Header = "Add evolution", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("add.png") } };

			ApplicationShortcut.Link(ApplicationShortcut.Copy, () => _miCopy_Click(null, null, _lv), _lv);
			ApplicationShortcut.Link(ApplicationShortcut.Paste, () => _miPaste_Click(null, null), _lv);
			ApplicationShortcut.Link(ApplicationShortcut.Delete, () => _miRemoveDrop_Click<PetEvolutionView>(null, null, _lv), _lv);

			_lv.ContextMenu.Items.Add(miSelect);
			_lv.ContextMenu.Items.Add(miEditDrop);
			_lv.ContextMenu.Items.Add(miRemoveDrop);
			_lv.ContextMenu.Items.Add(new Separator());
			_lv.ContextMenu.Items.Add(miCopy);
			_lv.ContextMenu.Items.Add(miPaste);
			_lv.ContextMenu.Items.Add(miAddEvolution);
			_lv.SelectionChanged += new SelectionChangedEventHandler(_lv_SelectionChanged);

			miSelect.Click += (sender, e) => _miSelect_Click(sender, e, _lv, ServerDbs.Pet);
			miEditDrop.Click += new RoutedEventHandler(_miEditDrop_Click);
			miRemoveDrop.Click += (sender, e) => _miRemoveDrop_Click<PetEvolutionView>(sender, e, _lv);
			miAddEvolution.Click += new RoutedEventHandler(_miAddDrop_Click);
			miCopy.Click += (sender, e) => _miCopy_Click(sender, e, _lv);
			miPaste.Click += new RoutedEventHandler(_miPaste_Click);

			_updateAction = new Action<TValue>(_update);

			_lv.PreviewMouseDown += delegate { Keyboard.Focus(_lv); };

			dp.AddUpdateAction(_updateAction);
			dp.AddResetField(_lv);

			grid.Children.Add(label);
			grid.Children.Add(_lv);
			tab.PropertiesGrid.Children.Add(grid);

			_initRequirementGrid();
		}
		#endregion

		private void _lv_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_lv.SelectedItem == null) {
				_lvRequirements.ItemsSource = null;
				return;
			}

			try {
				_lastSelectedIndex1 = _lv.SelectedIndex;
				_lastSelectedIndex2 = -1;
				PetEvolutionView evolution = (PetEvolutionView)_lv.SelectedItem;
				_updateRequirements(evolution);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _updateRequirements(PetEvolutionView evolution) {
			List<PetEvolutionTargetView> result = new List<PetEvolutionTargetView>();
			Table<int, ReadableTuple<int>> btable = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);
			Table<int, ReadableTuple<int>> ctable = _tab.ProjectDatabase.GetTable<int>(ServerDbs.CItems);

			try {
				result.AddRange(evolution.EvolutionTarget.ItemRequirements.Select(t => new PetEvolutionTargetView(t, btable, ctable)));
			}
			catch {
			}

			_lvRequirements.ItemsSource = new RangeObservableCollection<PetEvolutionTargetView>(result.OrderBy(p => p, Extensions.BindDefaultSearch<PetEvolutionTargetView>(_lvRequirements, "ID", true)));
		}

		private void _initRequirementGrid() {
			Grid grid = _tab.PropertiesGrid.Children.OfType<Grid>().Last();
			grid.Background = Application.Current.Resources["TabItemBackground"] as Brush;

			Label label = new Label();
			label.Content = "Requirements";
			label.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextForeground"] as Brush);
			label.FontStyle = FontStyles.Italic;
			label.Padding = new Thickness(0);
			label.Margin = new Thickness(3);
			label.SetValue(Grid.ColumnProperty, 1);

			_lvRequirements = new RangeListView();
			_lvRequirements.SetValue(TextSearch.TextPathProperty, "ID");
			_lvRequirements.SetValue(WpfUtils.IsGridSortableProperty, true);
			_lvRequirements.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
			_lvRequirements.SetValue(Grid.RowProperty, _row);
			_lvRequirements.SetValue(Grid.ColumnProperty, 1);
			_lvRequirements.FocusVisualStyle = null;
			_lvRequirements.Margin = new Thickness(3);
			_lvRequirements.BorderThickness = new Thickness(1);
			_lvRequirements.PreviewMouseRightButtonUp += _lvRequirements_PreviewMouseRightButtonUp;

			_evolutionTargetSorter = new DefaultListViewComparer<PetEvolutionTargetView>(true);

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_lvRequirements, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Item ID", DisplayExpression = "ID", SearchGetAccessor = "ID", FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "ID" },
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "ID", FixedWidth = 26, MaxHeight = 24 },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "Name", DisplayExpression = "Name", SearchGetAccessor = "Name", IsFill = true, ToolTipBinding = "Name", TextWrapping = TextWrapping.Wrap, MinWidth = 40 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Amount", DisplayExpression = "Amount", SearchGetAccessor = "Amount", ToolTipBinding = "Amount", FixedWidth = 60, TextAlignment = TextAlignment.Right },
			}, _evolutionTargetSorter, new string[] { "Default", "{DynamicResource TextForeground}" });

			_lvRequirements.ContextMenu = new ContextMenu();
			_lvRequirements.MouseDoubleClick += new MouseButtonEventHandler(_lvRequirements_MouseDoubleClick);

			MenuItem miSelect = new MenuItem { Header = "Select", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("arrowdown.png") } };
			MenuItem miEditDrop = new MenuItem { Header = "Edit", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("properties.png") } };
			MenuItem miRemoveDrop = new MenuItem { Header = "Remove", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("delete.png") }, InputGestureText = "Del" };
			MenuItem miCopy = new MenuItem { Header = "Copy", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("copy.png") }, InputGestureText = "Ctrl-C" };
			MenuItem miPaste = new MenuItem { Header = "Paste", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("paste.png") }, InputGestureText = "Ctrl-V" };
			MenuItem miAddTarget = new MenuItem { Header = "Add requirement", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("add.png") } };

			ApplicationShortcut.Link(ApplicationShortcut.Copy, () => _miCopy_Click(null, null, _lvRequirements), _lvRequirements);
			ApplicationShortcut.Link(ApplicationShortcut.Paste, () => _miPasteRequirement_Click(null, null), _lvRequirements);
			ApplicationShortcut.Link(ApplicationShortcut.Delete, () => _miRemoveDrop_Click<PetEvolutionTargetView>(null, null, _lvRequirements), _lvRequirements);

			_lvRequirements.ContextMenu.Items.Add(miSelect);
			_lvRequirements.ContextMenu.Items.Add(miEditDrop);
			_lvRequirements.ContextMenu.Items.Add(miRemoveDrop);
			_lvRequirements.ContextMenu.Items.Add(new Separator());
			_lvRequirements.ContextMenu.Items.Add(miCopy);
			_lvRequirements.ContextMenu.Items.Add(miPaste);
			_lvRequirements.ContextMenu.Items.Add(miAddTarget);

			miSelect.Click += (sender, e) => _miSelect_Click(sender, e, _lvRequirements, ServerDbs.Items);
			miEditDrop.Click += new RoutedEventHandler(_miEditRequirement_Click);
			miRemoveDrop.Click += (sender, e) => _miRemoveDrop_Click<PetEvolutionTargetView>(sender, e, _lvRequirements);
			miAddTarget.Click += new RoutedEventHandler(_miAddRequirement_Click);
			miCopy.Click += (sender, e) => _miCopy_Click(sender, e, _lvRequirements);
			miPaste.Click += new RoutedEventHandler(_miPasteRequirement_Click);

			_lvRequirements.PreviewMouseDown += delegate { Keyboard.Focus(_lvRequirements); };
			_lvRequirements.SelectionChanged += new SelectionChangedEventHandler(_lvRequirements_SelectionChanged);

			grid.Children.Add(label);
			grid.Children.Add(_lvRequirements);
		}

		private void _lvRequirements_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_lvRequirements.SelectedIndex > -1) {
				_lastSelectedIndex2 = _lvRequirements.SelectedIndex;
			}
		}

		private void _miPaste_Click(object sender, RoutedEventArgs e) {
			try {
				if (_tab.List.SelectedItem == null) return;
				if (!Clipboard.ContainsText()) return;

				string text = Clipboard.GetText();

				var itemDb = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);
				var mobDb = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);

				Evolution evolution = new Evolution(text, itemDb, mobDb);

				var btable = _tab.Table;

				btable.Commands.Begin();

				TValue item = (TValue)_tab.List.SelectedItem;

				_evolution = new Evolution();

				try {
					foreach (var targetEv in _lv.Items.OfType<PetEvolutionView>()) {
						_evolution.Targets.Add(targetEv.EvolutionTarget);
					}

					for (int i = 0; i < evolution.Targets.Count; i++) {
						var target = evolution.Targets[i];

						if (_lv.Items.OfType<PetEvolutionView>().Any(p => p.EvolutionTarget.Target == target.Target)) {
							evolution.Targets.RemoveAt(i);
							i--;
						}
					}

					if (evolution.Targets.Count > 0) {
						_evolution.Targets.AddRange(evolution.Targets);
						btable.Commands.Set(item, ServerPetAttributes.Evolution, _evolution.ToString());
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}

				btable.Commands.EndEdit();

				_updateAction(item);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _miPasteRequirement_Click(object sender, RoutedEventArgs e) {
			try {
				if (_tab.List.SelectedItem == null) return;
				if (!Clipboard.ContainsText()) return;

				string text = Clipboard.GetText();
				string[] elementsToAdd = text.Trim(',').Split(',');

				if (elementsToAdd.Length % 2 != 0) throw new Exception("The number of arguments must be even.");

				List<Utilities.Extension.Tuple<object, int>> copy = new List<Utilities.Extension.Tuple<object, int>>();
				var itemDb = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

				for (int i = 0; i < elementsToAdd.Length; i += 2) {
					copy.Add(new Utilities.Extension.Tuple<object, int>(DbIOUtils.Name2Id(itemDb, ServerItemAttributes.AegisName, elementsToAdd[i], "item_db", true).ToString(), Int32.Parse(elementsToAdd[i + 1])));
				}

				var btable = _tab.Table;

				btable.Commands.Begin();

				TValue item = (TValue)_tab.List.SelectedItem;
				bool changed = false;

				try {
					_evolution = new Evolution();

					foreach (var targetEv in _lv.Items.OfType<PetEvolutionView>()) {
						if (targetEv == _lv.SelectedItem) {
							List<Utilities.Extension.Tuple<object, int>> requirements = new List<Utilities.Extension.Tuple<object, int>>();

							foreach (var requirement in _lvRequirements.Items.OfType<PetEvolutionTargetView>()) {
								if (copy.Any(p => p.Item1.ToString() == requirement.ID.ToString(CultureInfo.InvariantCulture) && p.Item2 == requirement.Amount)) {
									var cpyEntry = copy.Where(p => p.Item1.ToString() == requirement.ID.ToString(CultureInfo.InvariantCulture) && p.Item2 == requirement.Amount).ToList();

									foreach (var ea in cpyEntry) {
										copy.Remove(ea);
									}
								}
								else if (copy.Any(p => p.Item1.ToString() == requirement.ID.ToString(CultureInfo.InvariantCulture) && p.Item2 != requirement.Amount)) {
									var cpyEntry = copy.First(p => p.Item1.ToString() == requirement.ID.ToString(CultureInfo.InvariantCulture));

									requirements.Add(cpyEntry);
									copy.Remove(cpyEntry);
									changed = true;
								}
								else {
									requirements.Add(requirement.Requirement);
								}
							}

							if (copy.Count > 0) {
								requirements.AddRange(copy.Select(p => new Utilities.Extension.Tuple<object, int>(DbIOUtils.Id2Name(itemDb, ServerItemAttributes.AegisName, p.Item1.ToString()), p.Item2)));
								changed = true;
							}

							targetEv.EvolutionTarget.ItemRequirements = requirements;
						}

						_evolution.Targets.Add(targetEv.EvolutionTarget);
					}

					if (changed) {
						btable.Commands.Set(item, ServerPetAttributes.Evolution, _evolution.ToString());
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}

				btable.Commands.EndEdit();
				
				if (changed) {
					_updateAction(item);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _miCopy_Click(object sender, RoutedEventArgs e, RangeListView lv) {
			if (lv.SelectedItems.Count > 0) {
				StringBuilder b = new StringBuilder();
				Dictionary<string, MetaTable<int>> dbs = new Dictionary<string, MetaTable<int>>();
				dbs[ServerDbs.Items] = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);
				dbs[ServerDbs.Mobs] = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);

				foreach (ICustomEditableView item in lv.SelectedItems) {
					b.Append(item.GetStringFormat(lv, dbs));
				}

				string res = b.ToString().Trim(',');
				Clipboard.SetDataObject(res);
			}
		}

		private void _update(TValue item) {
			if (_item != item) {
				_lastSelectedIndex1 = -1;
				_lastSelectedIndex2 = -1;
			}

			_item = item;
			List<PetEvolutionView> result = new List<PetEvolutionView>();
			Table<int, ReadableTuple<int>> btable = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);

			try {
				_evolution = new Evolution((string)item.GetRawValue(ServerPetAttributes.Evolution.Index));
				result.AddRange(_evolution.Targets.Select(t => new PetEvolutionView((ReadableTuple<int>)(object)item, t, btable)));
			}
			catch {
			}

			_lv.ItemsSource = new RangeObservableCollection<PetEvolutionView>(result.OrderBy(p => p, Extensions.BindDefaultSearch<PetEvolutionView>(_lv, "ID", true)));

			if (_lv.Items.Count > 0) {
				if (_lastSelectedIndex1 == -1) {
					_lv.SelectedIndex = 0;
				}
				else {
					//if (_lastSelectedIndex1 >= _lv.Items.Count) {
					//	_lastSelectedIndex1 = 0;
					//}
				
					int index2 = _lastSelectedIndex2;
				
					_lv.SelectedIndex = _lastSelectedIndex1;
				
					if (index2 >= _lvRequirements.Items.Count) {
						_lastSelectedIndex2 = -1;
					}
					else {
						_lastSelectedIndex2 = index2;
					}
				
					_lvRequirements.SelectedIndex = _lastSelectedIndex2;
				}
			}
		}

		private void _miAddDrop_Click(object sender, RoutedEventArgs e) {
			if (_tab.List.SelectedItem == null)
				return;

			try {
				Table<int, ReadableTuple<int>> query = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Pet);

				SelectFromDialog select = new SelectFromDialog(query, ServerDbs.Pet, "");
				select.Owner = WpfUtilities.TopWindow;

				if (select.ShowDialog() == true && select.Tuple != null) {
					if (_lv.SelectedItems.Count <= 0 || _tab.List.SelectedItem == null)
						return;

					TValue item = (TValue)_tab.List.SelectedItem;
					var btable = _tab.Table;

					_evolution = new Evolution();

					//if (_lv.Items.OfType<PetEvolutionView>().Any(p => p.ID == select.Id)) {
					//	ErrorHandler.HandleException("An evolution with this ID already exists.");
					//	return;
					//}

					try {
						btable.Commands.Begin();

						var target = new EvolutionTarget();
						target.Target = select.Id;

						foreach (var targetEv in _lv.Items.OfType<PetEvolutionView>()) {
							_evolution.Targets.Add(targetEv.EvolutionTarget);
						}

						_evolution.Targets.Add(target);
						btable.Commands.Set(item, ServerPetAttributes.Evolution, _evolution.ToString());
					}
					finally {
						btable.Commands.EndEdit();
					}

					_update(item);

					_lv.SelectedItem = _lv.Items.OfType<PetEvolutionView>().FirstOrDefault(p => p.ID == select.Id) ?? _lv.SelectedItem;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _miAddRequirement_Click(object sender, RoutedEventArgs e) {
			if (_tab.List.SelectedItem == null)
				return;

			TValue item = (TValue)_tab.List.SelectedItem;
			var btable = _tab.Table;

			try {
				DropEditDialog dialog = new DropEditDialog("", "1", ServerDbs.Items, _tab.ProjectDatabase);
				dialog.Owner = WpfUtilities.TopWindow;
				dialog.Element2 = "Amount";

				if (dialog.ShowDialog() == true) {
					string sid = dialog.Id;
					string svalue = dialog.DropChance;
					int value;
					int id;

					Int32.TryParse(sid, out id);

					if (!Extensions.GetIntFromFloatValue(svalue, out value)) {
						ErrorHandler.HandleException("Invalid format (integer or float value only)");
						return;
					}

					if (id <= 0) {
						return;
					}

					bool replaced = false;

					try {
						btable.Commands.Begin();

						_evolution = new Evolution();

						foreach (var targetEv in _lv.Items.OfType<PetEvolutionView>()) {
							if (targetEv == _lv.SelectedItem) {
								List<Utilities.Extension.Tuple<object, int>> requirements = new List<Utilities.Extension.Tuple<object, int>>();

								foreach (var requirement in _lvRequirements.Items.OfType<PetEvolutionTargetView>()) {
									if (requirement.ID == sid) {
										requirements.Add(new Utilities.Extension.Tuple<object, int>(dialog.Id, value));
										replaced = true;
									}
									else {
										requirements.Add(requirement.Requirement);
									}
								}

								if (!replaced) {
									requirements.Add(new Utilities.Extension.Tuple<object, int>(dialog.Id, value));
									replaced = true;
								}

								targetEv.EvolutionTarget.ItemRequirements = requirements;
							}

							_evolution.Targets.Add(targetEv.EvolutionTarget);
						}

						btable.Commands.Set(item, ServerPetAttributes.Evolution, _evolution.ToString());
					}
					finally {
						btable.Commands.EndEdit();
					}

					if (replaced) {
						_update(item);

						foreach (PetEvolutionTargetView titem in _lvRequirements.Items) {
							if (titem.ID == dialog.Id) {
								_lvRequirements.SelectedItem = titem;
								break;
							}
						}
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			btable.Commands.EndEdit();
		}

		private void _miEditDrop_Click(object sender, RoutedEventArgs e) {
			if (_lv.SelectedItems.Count <= 0 || _tab.List.SelectedItem == null)
				return;

			try {
				Table<int, ReadableTuple<int>> query = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Pet);

				SelectFromDialog select = new SelectFromDialog(query, ServerDbs.Pet, "");
				select.Owner = WpfUtilities.TopWindow;

				if (select.ShowDialog() == true && select.Tuple != null) {
					if (_lv.SelectedItems.Count <= 0 || _tab.List.SelectedItem == null)
						return;

					TValue item = (TValue)_tab.List.SelectedItem;
					var btable = _tab.Table;

					_evolution = new Evolution();

					//if (_lv.Items.OfType<PetEvolutionView>().Any(p => p.ID == select.Id)) {
					//	ErrorHandler.HandleException("An evolution with this ID already exists.");
					//	return;
					//}

					try {
						btable.Commands.Begin();

						foreach (var targetEv in _lv.Items.OfType<PetEvolutionView>()) {
							if (targetEv == _lv.SelectedItem) {
								targetEv.EvolutionTarget.Target = select.Id;
							}

							_evolution.Targets.Add(targetEv.EvolutionTarget);
						}

						btable.Commands.Set(item, ServerPetAttributes.Evolution, _evolution.ToString());
					}
					finally {
						btable.Commands.EndEdit();
					}

					_update(item);

					_lv.SelectedItem = _lv.Items.OfType<PetEvolutionView>().FirstOrDefault(p => p.ID == select.Id) ?? _lv.SelectedItem;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _miEditRequirement_Click(object sender, RoutedEventArgs e) {
			if (_lvRequirements.SelectedItems.Count <= 0 || _tab.List.SelectedItem == null)
				return;

			TValue item = (TValue)_tab.List.SelectedItem;
			var btable = _tab.Table;

			try {
				var selectedItem = (PetEvolutionTargetView)_lvRequirements.SelectedItem;
				DropEditDialog dialog = new DropEditDialog(selectedItem.ID.ToString(CultureInfo.InvariantCulture), selectedItem.Amount.ToString(CultureInfo.InvariantCulture), ServerDbs.Items, _tab.ProjectDatabase);
				dialog.Owner = WpfUtilities.TopWindow;
				dialog.Element2 = "Amount";

				if (dialog.ShowDialog() == true) {
					string sid = dialog.Id;
					string svalue = dialog.DropChance;
					int value;
					int id;

					Int32.TryParse(sid, out id);

					if (!Extensions.GetIntFromFloatValue(svalue, out value)) {
						ErrorHandler.HandleException("Invalid format (integer or float value only)");
						return;
					}

					if (id <= 0) {
						return;
					}

					try {
						btable.Commands.Begin();

						_evolution = new Evolution();

						foreach (var targetEv in _lv.Items.OfType<PetEvolutionView>()) {
							if (targetEv == _lv.SelectedItem) {
								List<Utilities.Extension.Tuple<object, int>> requirements = new List<Utilities.Extension.Tuple<object, int>>();

								foreach (var requirement in _lvRequirements.Items.OfType<PetEvolutionTargetView>()) {
									if (requirement == _lvRequirements.SelectedItem) {
										requirements.Add(new Utilities.Extension.Tuple<object, int>(dialog.Id, value));
									}
									else {
										requirements.Add(requirement.Requirement);
									}
								}

								targetEv.EvolutionTarget.ItemRequirements = requirements;
							}

							_evolution.Targets.Add(targetEv.EvolutionTarget);
						}

						btable.Commands.Set(item, ServerPetAttributes.Evolution, _evolution.ToString());
					}
					finally {
						btable.Commands.EndEdit();
					}

					_update(item);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			btable.Commands.EndEdit();
		}

		private void _removeSelected<TCollection>(RangeListView lv) {
			for (int i = 0; i < lv.SelectedItems.Count; i++) {
				TCollection selectedItem = (TCollection)lv.SelectedItems[i];
				((RangeObservableCollection<TCollection>)lv.ItemsSource).Remove(selectedItem);
				i--;
			}
		}

		private void _miRemoveDrop_Click<TCollection>(object sender, RoutedEventArgs e, RangeListView lv) {
			if (lv.SelectedItems.Count <= 0 || _tab.List.SelectedItem == null)
				return;

			TValue item = (TValue)_tab.List.SelectedItem;
			var btable = _tab.Table;
			
			btable.Commands.Begin();
			
			try {
				_removeSelected<TCollection>(lv);
				_evolution = new Evolution();

				foreach (var targetEv in _lv.Items.OfType<PetEvolutionView>()) {
					if (targetEv == _lv.SelectedItem) {
						targetEv.EvolutionTarget.ItemRequirements = _lvRequirements.Items.OfType<PetEvolutionTargetView>().Select(requirement => requirement.Requirement).ToList();
					}

					_evolution.Targets.Add(targetEv.EvolutionTarget);
				}

				btable.Commands.Set(item, ServerPetAttributes.Evolution, _evolution.ToString());
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			
			btable.Commands.EndEdit();
		}

		private void _lv_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			_miEditDrop_Click(sender, null);
		}

		private void _lvRequirements_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			_miEditRequirement_Click(sender, null);
		}

		private void _lv_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			try {
				bool hasItems = _lv.GetObjectAtPoint<ListViewItem>(e.GetPosition(_lv)) != null;
				_lv.ContextMenu.Items.Cast<UIElement>().Take(5).ToList().ForEach(p => p.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _lvRequirements_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			try {
				bool hasItems = _lvRequirements.GetObjectAtPoint<ListViewItem>(e.GetPosition(_lvRequirements)) != null;
				_lvRequirements.ContextMenu.Items.Cast<UIElement>().Take(5).ToList().ForEach(p => p.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _miSelect_Click(object sender, RoutedEventArgs e, RangeListView lv, ServerDbs db) {
			if (lv.SelectedItems.Count > 0) {
				TabNavigation.SelectList(db, lv.SelectedItems.Cast<ICustomEditableView>().Select(p => p.SelectId));
			}
		}

		#region Nested type: PetEvolutionView
		public class PetEvolutionView : ICustomEditableView, INotifyPropertyChanged {
			private readonly Table<int, ReadableTuple<int>> _mobsTable;
			private readonly ReadableTuple<int> _tuple;
			private EvolutionTarget _evolutionTarget;

			public EvolutionTarget EvolutionTarget {
				get { return _evolutionTarget; }
			}

			public PetEvolutionView(ReadableTuple<int> tuple, EvolutionTarget evolutionTarget, Table<int, ReadableTuple<int>> mobsTable) {
				_tuple = tuple;
				_evolutionTarget = evolutionTarget;
				_mobsTable = mobsTable;
				//_tuple.TupleModified += _tuple_TupleModified;

				_reload();
			}

			public string ID { get; private set; }
			public string EvolutionName { get; private set; }

			public bool Default {
				get { return true; }
			}

			#region INotifyPropertyChanged Members
			public event PropertyChangedEventHandler PropertyChanged;
			#endregion

			//private void _tuple_TupleModified(object sender, bool value) {
			//	OnPropertyChanged();
			//}

			private void _reload() {
				object res = DbIOUtils.Name2Id(_mobsTable, ServerMobAttributes.AegisName, _evolutionTarget.Target, "mob_db", true);

				EvolutionName = "";

				if (res is int) {
					int key = (int)res;
					ID = res.ToString();

					if (_mobsTable.ContainsKey(key)) {
						EvolutionName = (string)_mobsTable.GetTuple(key).GetRawValue(ServerMobAttributes.KRoName.Index);
					}
				}
				else {
					ID = res.ToString();
				}
			}

			protected virtual void OnPropertyChanged() {
				_reload();

				PropertyChangedEventHandler handler = PropertyChanged;
				if (handler != null) handler(this, new PropertyChangedEventArgs(""));
			}

			public void Update() {
				_reload();
			}

			// ICustomEditableView
			public int SelectId {
				get {
					int v;
					return Int32.TryParse(ID, out v) ? v : 0;
				}
			}

			public string GetStringFormat(RangeListView lv, Dictionary<string, MetaTable<int>> dbs) {
				var itemDb = dbs[ServerDbs.Items];
				var mobDb = dbs[ServerDbs.Mobs];
				StringBuilder b = new StringBuilder();

				b.Append("#");
				b.Append(DbIOUtils.Name2Id(mobDb, ServerMobAttributes.AegisName, EvolutionTarget.Target, "item_db", true));
				b.Append(",");

				foreach (Utilities.Extension.Tuple<object, int> item in EvolutionTarget.ItemRequirements) {
					b.Append(DbIOUtils.Name2Id(itemDb, ServerItemAttributes.AegisName, item.Item1.ToString(), "item_db", true));
					b.Append(",");
					b.Append(item.Item2);
					b.Append(",");
				}

				return b.ToString();
			}
		}
		#endregion

		#region Nested type: PetEvolutionView
		public class PetEvolutionTargetView : ICustomEditableView, INotifyPropertyChanged {
			private readonly Utilities.Extension.Tuple<object, int> _requirement;
			private readonly Table<int, ReadableTuple<int>> _itemsTable;
			private readonly Table<int, ReadableTuple<int>> _clientTable;

			public PetEvolutionTargetView(Utilities.Extension.Tuple<object, int> requirement, Table<int, ReadableTuple<int>> itemsTable, Table<int, ReadableTuple<int>> clientTable) {
				_requirement = requirement;
				_itemsTable = itemsTable;
				_clientTable = clientTable;

				_reload();
			}

			public Utilities.Extension.Tuple<object, int> Requirement {
				get { return _requirement; }
			}

			public string ID { get; private set; }
			public string Name { get; private set; }
			public int Amount { get; private set; }

			public BitmapSource DataImage {
				get {
					try {
						if (_clientTable != null) {
							int v;

							if (Int32.TryParse(ID, out v)) {
								var entry = _clientTable.TryGetTuple(v);

								if (entry != null) {
									byte[] data = SdeEditor.Instance.ProjectDatabase.MetaGrf.GetData(EncodingService.FromAnyToDisplayEncoding(@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\item\" + entry.GetValue<string>(ClientItemAttributes.IdentifiedResourceName.Index).ExpandString() + ".bmp"));

									if (data != null) {
										GrfImage gimage = new GrfImage(ref data);
										gimage.MakePinkTransparent();

										if (gimage.GrfImageType == GrfImageType.Bgr24) {
											gimage.Convert(GrfImageType.Bgra32);
										}

										return gimage.Cast<BitmapSource>();
									}
								}
							}
						}
						return null;
					}
					catch {
						return null;
					}
				}
			}

			public bool Default {
				get { return true; }
			}

			#region INotifyPropertyChanged Members
			public event PropertyChangedEventHandler PropertyChanged;
			#endregion

			private void _reload() {
				object res = DbIOUtils.Name2Id(_itemsTable, ServerItemAttributes.AegisName, _requirement.Item1 as string, "mob_db", true);

				Name = "";

				if (res is int) {
					int key = (int)res;
					ID = res.ToString();

					if (_itemsTable.ContainsKey(key)) {
						Name = (string)_itemsTable.GetTuple(key).GetRawValue(ServerItemAttributes.Name.Index);
					}
				}
				else {
					ID = res.ToString();
				}

				Amount = _requirement.Item2;
			}

			protected virtual void OnPropertyChanged() {
				_reload();

				PropertyChangedEventHandler handler = PropertyChanged;
				if (handler != null) handler(this, new PropertyChangedEventArgs(""));
			}

			public void Update() {
				_reload();
			}

			// ICustomEditableView
			public int SelectId {
				get {
					int v;
					return Int32.TryParse(ID, out v) ? v : 0;
				}
			}

			public string GetStringFormat(RangeListView lv, Dictionary<string, MetaTable<int>> dbs) {
				var itemDb = dbs[ServerDbs.Items];
				StringBuilder b = new StringBuilder();

				b.Append(DbIOUtils.Name2Id(itemDb, ServerItemAttributes.AegisName, _requirement.Item1.ToString(), "item_db", true));
				b.Append(",");
				b.Append(_requirement.Item2);
				b.Append(",");

				return b.ToString();
			}
		}
		#endregion
	}
}