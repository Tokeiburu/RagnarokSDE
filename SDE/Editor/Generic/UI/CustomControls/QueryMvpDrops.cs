using Database;
using ErrorManager;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.TabNavigationEngine;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.View.Dialogs;
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
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using TokeiLibrary.WpfBugFix;
using Extensions = SDE.Core.Extensions;

namespace SDE.Editor.Generic.UI.CustomControls
{
    public class QueryMvpDrops<TKey, TValue> : ICustomControl<TKey, TValue> where TValue : Database.Tuple
    {
        private readonly int _row;
        private RangeListView _lv;
        private GDbTabWrapper<TKey, TValue> _tab;
        private Action<TValue> _updateAction;

        public QueryMvpDrops(int row)
        {
            _row = row;
        }

        #region ICustomControl<TKey,TValue> Members

        public void Init(GDbTabWrapper<TKey, TValue> tab, DisplayableProperty<TKey, TValue> dp)
        {
            _tab = tab;
            Grid grid = tab.PropertiesGrid.Children.OfType<Grid>().Last();

            Label label = new Label();
            label.Content = "MVP drops";
            label.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextForeground"] as Brush);
            label.FontStyle = FontStyles.Italic;
            label.Padding = new Thickness(0);
            label.Margin = new Thickness(3);
            label.SetValue(Grid.ColumnProperty, 1);

            _lv = new RangeListView();
            _lv.SetValue(TextSearch.TextPathProperty, "ID");
            _lv.SetValue(WpfUtils.IsGridSortableProperty, true);
            _lv.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            _lv.SetValue(Grid.RowProperty, _row);
            _lv.SetValue(Grid.ColumnProperty, 1);
            _lv.FocusVisualStyle = null;
            _lv.Margin = new Thickness(3);
            _lv.BorderThickness = new Thickness(1);
            _lv.PreviewMouseRightButtonUp += _lv_PreviewMouseRightButtonUp;
            _lv.Background = Application.Current.Resources["TabItemBackground"] as Brush;

            ListViewDataTemplateHelper.GenerateListViewTemplateNew(_lv, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
                new ListViewDataTemplateHelper.GeneralColumnInfo { Header = ServerItemAttributes.Id.DisplayName, DisplayExpression = "ID", SearchGetAccessor = "ID", FixedWidth = 45, TextAlignment = TextAlignment.Right, ToolTipBinding = "ID" },
                new ListViewDataTemplateHelper.RangeColumnInfo { Header = ServerItemAttributes.Name.DisplayName, DisplayExpression = "Name", SearchGetAccessor = "Name", IsFill = true, ToolTipBinding = "Name", TextWrapping = TextWrapping.Wrap, MinWidth = 40 },
                new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Drop %", DisplayExpression = "Drop", SearchGetAccessor = "DropOriginal", ToolTipBinding = "DropOriginal", FixedWidth = 60, TextAlignment = TextAlignment.Right },
            }, new DefaultListViewComparer<MobDropView>(), new string[] { "Default", "{DynamicResource TextForeground}", "IsMvp", "{DynamicResource CellBrushMvp}", "IsRandomGroup", "{DynamicResource CellBrushLzma}" });

            _lv.ContextMenu = new ContextMenu();
            _lv.MouseDoubleClick += new MouseButtonEventHandler(_lv_MouseDoubleClick);

            MenuItem miSelect = new MenuItem { Header = "Select", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("arrowdown.png") } };
            MenuItem miEditDrop = new MenuItem { Header = "Edit", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("properties.png") } };
            MenuItem miRemoveDrop = new MenuItem { Header = "Remove", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("delete.png") }, InputGestureText = "Del" };
            MenuItem miCopy = new MenuItem { Header = "Copy", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("copy.png") }, InputGestureText = "Ctrl-C" };
            MenuItem miPaste = new MenuItem { Header = "Paste", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("paste.png") }, InputGestureText = "Ctrl-V" };
            MenuItem miAddDrop = new MenuItem { Header = "Add", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("add.png") } };

            ApplicationShortcut.Link(ApplicationShortcut.Copy, () => _miCopy_Click(null, null), _lv);
            ApplicationShortcut.Link(ApplicationShortcut.Paste, () => _miPaste_Click(null, null), _lv);
            ApplicationShortcut.Link(ApplicationShortcut.Delete, () => _miRemoveDrop_Click(null, null), _lv);

            _lv.ContextMenu.Items.Add(miSelect);
            _lv.ContextMenu.Items.Add(miEditDrop);
            _lv.ContextMenu.Items.Add(miRemoveDrop);
            _lv.ContextMenu.Items.Add(new Separator());
            _lv.ContextMenu.Items.Add(miCopy);
            _lv.ContextMenu.Items.Add(miPaste);
            _lv.ContextMenu.Items.Add(miAddDrop);

            miSelect.Click += _miSelect_Click;
            miEditDrop.Click += _miEditDrop_Click;
            miRemoveDrop.Click += _miRemoveDrop_Click;
            miAddDrop.Click += _miAddDrop_Click;
            miCopy.Click += new RoutedEventHandler(_miCopy_Click);
            miPaste.Click += new RoutedEventHandler(_miPaste_Click);

            _updateAction = new Action<TValue>(_update);

            _lv.PreviewMouseDown += delegate { Keyboard.Focus(_lv); };

            tab.ProjectDatabase.Commands.CommandIndexChanged += _commands_CommandIndexChanged;
            dp.AddUpdateAction(_updateAction);
            dp.AddResetField(_lv);

            grid.Children.Add(label);
            grid.Children.Add(_lv);
        }

        #endregion ICustomControl<TKey,TValue> Members

        private void _miPaste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_lv.Items.Count >= 3)
                {
                    ErrorHandler.HandleException("You cannot add more than 3 drops. Delete an item and then add a new one.");
                    return;
                }

                if (_tab.List.SelectedItem == null) return;
                if (!Clipboard.ContainsText()) return;

                string text = Clipboard.GetText();
                string[] elementsToAdd = text.Split(',');

                if (DbPathLocator.IsYamlMob())
                {
                    if (elementsToAdd.Length % 3 != 0) throw new Exception("The number of arguments must be even.");

                    Table<int, ReadableTuple<int>> btable = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);

                    try
                    {
                        TValue item = (TValue)_tab.List.SelectedItem;

                        try
                        {
                            btable.Commands.Begin();

                            int startIndex = ServerMobAttributes.Mvp1ID.Index;
                            int i = 0;

                            for (int j = 0; j < elementsToAdd.Length; j += 3)
                            {
                                string sid = elementsToAdd[j];
                                string svalue = elementsToAdd[j + 1];
                                string randGroup = elementsToAdd[j + 2];
                                int value;
                                int id;

                                Int32.TryParse(sid, out id);

                                if (id <= 0)
                                    return;

                                if (!Extensions.GetIntFromFloatValue(svalue, out value))
                                {
                                    ErrorHandler.HandleException("Invalid format (integer or float value only)");
                                    return;
                                }

                                for (; i < 6; i += 2)
                                {
                                    if (item.GetValue<int>(startIndex + i) == 0)
                                    {
                                        btable.Commands.Set((ReadableTuple<int>)(object)item, startIndex + i, id);
                                        btable.Commands.Set((ReadableTuple<int>)(object)item, startIndex + i + 1, value);
                                        btable.Commands.Set((ReadableTuple<int>)(object)item, ServerMobAttributes.Mvp1RandomOptionGroup.Index + (i / 2), randGroup);
                                        i += 2;
                                        break;
                                    }
                                }
                            }
                        }
                        finally
                        {
                            btable.Commands.EndEdit();
                        }

                        _lv.ItemsSource = null;
                        _updateAction(item);
                    }
                    catch (Exception err)
                    {
                        ErrorHandler.HandleException(err);
                    }
                }
                else
                {
                    if (elementsToAdd.Length % 2 != 0) throw new Exception("The number of arguments must be even.");

                    Table<int, ReadableTuple<int>> btable = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);

                    try
                    {
                        TValue item = (TValue)_tab.List.SelectedItem;

                        try
                        {
                            btable.Commands.Begin();

                            int startIndex = ServerMobAttributes.Mvp1ID.Index;
                            int i = 0;

                            for (int j = 0; j < elementsToAdd.Length; j += 2)
                            {
                                string sid = elementsToAdd[j];
                                string svalue = elementsToAdd[j + 1];
                                int value;
                                int id;

                                Int32.TryParse(sid, out id);

                                if (id <= 0)
                                    return;

                                if (!Extensions.GetIntFromFloatValue(svalue, out value))
                                {
                                    ErrorHandler.HandleException("Invalid format (integer or float value only)");
                                    return;
                                }

                                for (; i < 6; i += 2)
                                {
                                    if (item.GetValue<int>(startIndex + i) == 0)
                                    {
                                        btable.Commands.Set((ReadableTuple<int>)(object)item, startIndex + i, id);
                                        btable.Commands.Set((ReadableTuple<int>)(object)item, startIndex + i + 1, value);
                                        i += 2;
                                        break;
                                    }
                                }
                            }
                        }
                        finally
                        {
                            btable.Commands.EndEdit();
                        }

                        _lv.ItemsSource = null;
                        _updateAction(item);
                    }
                    catch (Exception err)
                    {
                        ErrorHandler.HandleException(err);
                    }
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _miCopy_Click(object sender, RoutedEventArgs e)
        {
            if (_lv.SelectedItems.Count > 0)
            {
                StringBuilder builder = new StringBuilder();

                foreach (MobDropView item in _lv.SelectedItems)
                {
                    builder.Append(item.ID);
                    builder.Append(",");
                    builder.Append(item.DropOriginal);
                    builder.Append(",");
                    builder.Append(item.RandomOptionGroup);
                    builder.Append(",");
                }

                string res = builder.ToString().Trim(',');
                Clipboard.SetDataObject(res);
            }
        }

        private void _commands_CommandIndexChanged(object sender, IGenericDbCommand command)
        {
            _tab.BeginDispatch(delegate
            {
                if (_tab.List.SelectedItem != null)
                    _update(_tab.List.SelectedItem as TValue);
            });
        }

        private void _update(TValue item)
        {
            List<MobDropView> result = new List<MobDropView>();
            Table<int, ReadableTuple<int>> btable = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

            try
            {
                int startIndex = ServerMobAttributes.Mvp1ID.Index;

                for (int j = 0; j < 6; j += 2)
                {
                    string value = (string)item.GetRawValue(startIndex + j);

                    if (string.IsNullOrEmpty(value) || value == "0")
                        continue;

                    ReadableTuple<int> tuple = (ReadableTuple<int>)(object)item;
                    result.Add(new MobDropView(tuple, startIndex + j, btable));
                }
            }
            catch
            {
            }

            _lv.ItemsSource = new RangeObservableCollection<MobDropView>(result.OrderBy(p => p, Extensions.BindDefaultSearch<MobDropView>(_lv, "ID")));
        }

        private void _miAddDrop_Click(object sender, RoutedEventArgs e)
        {
            if (_lv.Items.Count >= 3)
            {
                ErrorHandler.HandleException("You cannot add more than 3 MVP drops. Delete an item and then add a new one.");
                return;
            }

            Table<int, ReadableTuple<int>> btable = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);

            try
            {
                DropEditDialog dialog = new DropEditDialog("", "", ServerDbs.Items, _tab.ProjectDatabase, false, (DbPathLocator.IsYamlMob() ? 2 : 0));
                dialog.Owner = WpfUtilities.TopWindow;

                if (dialog.ShowDialog() == true)
                {
                    string sid = dialog.Id;
                    string svalue = dialog.DropChance;
                    string randGroup = "";
                    int value;
                    int id;

                    if (DbPathLocator.IsYamlMob())
                    {
                        randGroup = dialog.RandGroup;
                    }

                    Int32.TryParse(sid, out id);

                    if (id <= 0)
                        return;

                    if (!Extensions.GetIntFromFloatValue(svalue, out value))
                    {
                        ErrorHandler.HandleException("Invalid format (integer or float value only)");
                        return;
                    }

                    TValue item = (TValue)_tab.List.SelectedItem;

                    try
                    {
                        btable.Commands.Begin();

                        int startIndex = ServerMobAttributes.Mvp1ID.Index;

                        for (int i = 0; i < 6; i += 2)
                        {
                            if (item.GetValue<int>(startIndex + i) == 0)
                            {
                                btable.Commands.Set((ReadableTuple<int>)(object)item, startIndex + i, id);
                                btable.Commands.Set((ReadableTuple<int>)(object)item, startIndex + i + 1, value);

                                if (DbPathLocator.IsYamlMob())
                                {
                                    btable.Commands.Set((ReadableTuple<int>)(object)item, ServerMobAttributes.Mvp1RandomOptionGroup.Index + (i / 2), randGroup);
                                }
                                break;
                            }
                        }
                    }
                    finally
                    {
                        btable.Commands.EndEdit();
                    }

                    _lv.ItemsSource = null;
                    _updateAction(item);
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _miEditDrop_Click(object sender, RoutedEventArgs e)
        {
            if (_lv.SelectedItems.Count <= 0)
                return;

            Table<int, ReadableTuple<int>> btable = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);

            try
            {
                var selectedItem = (MobDropView)_lv.SelectedItem;
                DropEditDialog dialog = new DropEditDialog(selectedItem.ID.ToString(CultureInfo.InvariantCulture), selectedItem.DropOriginal.ToString(CultureInfo.InvariantCulture), ServerDbs.Items, _tab.ProjectDatabase, false, DbPathLocator.IsYamlMob() ? 2 : 0);

                if (DbPathLocator.IsYamlMob())
                {
                    dialog._tbRandGroup.Text = selectedItem.RandomOptionGroup;
                }

                dialog.Owner = WpfUtilities.TopWindow;

                if (dialog.ShowDialog() == true)
                {
                    string sid = dialog.Id;
                    string svalue = dialog.DropChance;
                    string randGroup = "";
                    int value;
                    int id;

                    if (DbPathLocator.IsYamlMob())
                    {
                        randGroup = dialog.RandGroup;
                    }

                    Int32.TryParse(sid, out id);

                    if (id <= 0)
                    {
                        return;
                    }

                    if (!Extensions.GetIntFromFloatValue(svalue, out value))
                    {
                        ErrorHandler.HandleException("Invalid format (integer or float value only)");
                        return;
                    }

                    try
                    {
                        btable.Commands.Begin();
                        btable.Commands.Set(selectedItem.Tuple, selectedItem.AttributeIndex, id);
                        btable.Commands.Set(selectedItem.Tuple, selectedItem.AttributeIndex + 1, value);

                        if (DbPathLocator.IsYamlMob())
                        {
                            int b = (selectedItem.AttributeIndex - ServerMobAttributes.Mvp1ID.Index) / 2;
                            int distRandGroup = ServerMobAttributes.Mvp1RandomOptionGroup.Index + b;

                            btable.Commands.Set(selectedItem.Tuple, distRandGroup, randGroup);
                        }
                    }
                    finally
                    {
                        btable.Commands.EndEdit();
                    }

                    selectedItem.Update();
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }

            btable.Commands.EndEdit();
        }

        private void _miRemoveDrop_Click(object sender, RoutedEventArgs e)
        {
            if (_lv.SelectedItems.Count <= 0)
                return;

            Table<int, ReadableTuple<int>> btable = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);

            btable.Commands.Begin();

            try
            {
                for (int i = 0; i < _lv.SelectedItems.Count; i++)
                {
                    var selectedItem = (MobDropView)_lv.SelectedItems[i];
                    var p = (ReadableTuple<int>)_tab.List.SelectedItem;

                    btable.Commands.Set(p, selectedItem.AttributeIndex, 0);
                    btable.Commands.Set(p, selectedItem.AttributeIndex + 1, 0);

                    if (DbPathLocator.IsYamlMob())
                    {
                        int b = (selectedItem.AttributeIndex - ServerMobAttributes.Mvp1ID.Index) / 2;
                        int distRandGroup = ServerMobAttributes.Mvp1RandomOptionGroup.Index + b;

                        btable.Commands.Set(p, distRandGroup, 0);
                    }

                    ((RangeObservableCollection<MobDropView>)_lv.ItemsSource).Remove(selectedItem);
                    i--;
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }

            btable.Commands.EndEdit();
        }

        private void _lv_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _miEditDrop_Click(sender, null);
        }

        private void _lv_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                bool hasItems = _lv.GetObjectAtPoint<ListViewItem>(e.GetPosition(_lv)) != null;
                _lv.ContextMenu.Items.Cast<UIElement>().Take(5).ToList().ForEach(p => p.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed);
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _miSelect_Click(object sender, RoutedEventArgs e)
        {
            if (_lv.SelectedItems.Count > 0)
            {
                TabNavigation.SelectList(ServerDbs.Items, _lv.SelectedItems.Cast<MobDropView>().Select(p => p.ID));
            }
        }

        #region Nested type: MobDropView

        public class MobDropView : INotifyPropertyChanged
        {
            private readonly int _index;
            private readonly Table<int, ReadableTuple<int>> _itemsTable;
            private readonly ReadableTuple<int> _tuple;

            public MobDropView(ReadableTuple<int> tuple, int index, Table<int, ReadableTuple<int>> itemsTable)
            {
                _tuple = tuple;
                _index = index;
                _itemsTable = itemsTable;
                _tuple.PropertyChanged += (s, e) => OnPropertyChanged();

                _reload();
            }

            public int AttributeIndex
            {
                get { return _index; }
            }

            public ReadableTuple<int> Tuple
            {
                get { return _tuple; }
            }

            public string Drop { get; private set; }

            public int ID { get; private set; }
            public string Name { get; private set; }
            public string MVP { get; private set; }
            public int DropOriginal { get; private set; }
            public string RandomOptionGroup { get; private set; }

            public bool IsMvp
            {
                get { return MVP != ""; }
            }

            public bool Default
            {
                get { return true; }
            }

            public bool IsRandomGroup
            {
                get
                {
                    if (DbPathLocator.IsYamlMob())
                    {
                        return !String.IsNullOrEmpty(RandomOptionGroup);
                    }

                    return false;
                }
            }

            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion INotifyPropertyChanged Members

            private void _reload()
            {
                ID = _tuple.GetValue<int>(_index);

                Name = "";

                if (_itemsTable.ContainsKey(ID))
                {
                    Name = (string)_itemsTable.GetTuple(ID).GetRawValue(ServerItemAttributes.Name.Index);
                }

                DropOriginal = Int32.Parse(((string)_tuple.GetRawValue(_index + 1)));
                Drop = String.Format("{0:0.00} %", DropOriginal / 100f);
                MVP = _index < ServerMobAttributes.Drop1ID.Index ? "MVP" : "";

                if (DbPathLocator.IsYamlMob())
                {
                    int b = (AttributeIndex - ServerMobAttributes.Mvp1ID.Index) / 2;
                    int distRandGroup = ServerMobAttributes.Mvp1RandomOptionGroup.Index + b;

                    RandomOptionGroup = (_tuple.GetRawValue(distRandGroup) ?? "").ToString();
                }
            }

            protected virtual void OnPropertyChanged()
            {
                _reload();

                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(""));
            }

            public void Update()
            {
                _reload();
            }
        }

        #endregion Nested type: MobDropView
    }
}