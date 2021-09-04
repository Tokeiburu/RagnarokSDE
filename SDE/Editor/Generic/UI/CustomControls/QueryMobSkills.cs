using Database;
using ErrorManager;
using GRF.System;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.TabNavigationEngine;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
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
using Utilities;
using Extensions = SDE.Core.Extensions;

namespace SDE.Editor.Generic.UI.CustomControls
{
    public class QueryMobSkills<TKey, TValue> : ICustomControl<TKey, TValue> where TValue : Database.Tuple
    {
        private readonly int _row;
        private Table<string, ReadableTuple<string>> _iSkillMobsTable;
        private Table<int, ReadableTuple<int>> _iSkillsTable;
        private RangeListView _lv;
        private GDbTabWrapper<TKey, TValue> _tab;

        public QueryMobSkills(int row)
        {
            _row = row;
        }

        private Table<string, ReadableTuple<string>> _skillMobsTable
        {
            get { return _iSkillMobsTable ?? (_iSkillMobsTable = _tab.GetMetaTable<string>(ServerDbs.MobSkills)); }
        }

        private Table<int, ReadableTuple<int>> _skillsTable
        {
            get { return _iSkillsTable ?? (_iSkillsTable = _tab.GetTable<int>(ServerDbs.Skills)); }
        }

        #region ICustomControl<TKey,TValue> Members

        public void Init(GDbTabWrapper<TKey, TValue> tab, DisplayableProperty<TKey, TValue> dp)
        {
            _tab = tab;
            Grid grid = tab.PropertiesGrid.Children.OfType<Grid>().Last();

            Label label = new Label();
            label.Content = "Mob skills";
            label.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextForeground"] as Brush);
            label.FontStyle = FontStyles.Italic;
            label.Padding = new Thickness(0);
            label.Margin = new Thickness(3);
            label.SetValue(Grid.ColumnProperty, 2);

            _lv = new RangeListView();
            _lv.SetValue(TextSearch.TextPathProperty, "ID");
            _lv.SetValue(WpfUtils.IsGridSortableProperty, true);
            _lv.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            _lv.SetValue(Grid.RowProperty, _row);
            _lv.SetValue(Grid.ColumnProperty, 2);
            _lv.FocusVisualStyle = null;
            _lv.Margin = new Thickness(3);
            _lv.BorderThickness = new Thickness(1);
            _lv.Background = Application.Current.Resources["TabItemBackground"] as Brush;

            ListViewDataTemplateHelper.GenerateListViewTemplateNew(_lv, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
                new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Skill", DisplayExpression = "Name", SearchGetAccessor = "Name", ToolTipBinding = "SkillId", FixedWidth = 60, TextWrapping = TextWrapping.Wrap },
                new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Condition", DisplayExpression = "Condition", SearchGetAccessor = "Condition", ToolTipBinding = "Condition", IsFill = true, TextAlignment = TextAlignment.Left, TextWrapping = TextWrapping.Wrap }
            }, new DefaultListViewComparer<MobSkillView>(), new string[] { "Default", "{DynamicResource TextForeground}" });

            _lv.ContextMenu = new ContextMenu();
            _lv.MouseDoubleClick += new MouseButtonEventHandler(_lv_MouseDoubleClick);

            MenuItem miSelectSkills = new MenuItem { Header = "Select skill", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("arrowdown.png") } };
            MenuItem miSelectMobSkills = new MenuItem { Header = "Select mob skill", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("arrowdown.png") } };
            MenuItem miRemoveDrop = new MenuItem { Header = "Remove mob skill", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("delete.png") }, InputGestureText = "Del" };
            MenuItem miCopy = new MenuItem { Header = "Copy", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("copy.png") }, InputGestureText = "Ctrl-C" };
            MenuItem miPaste = new MenuItem { Header = "Paste", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("paste.png") }, InputGestureText = "Ctrl-V" };

            ApplicationShortcut.Link(ApplicationShortcut.Copy, () => _miCopy_Click(null, null), _lv);
            ApplicationShortcut.Link(ApplicationShortcut.Paste, () => _miPaste_Click(null, null), _lv);
            ApplicationShortcut.Link(ApplicationShortcut.Delete, () => _miRemoveDrop_Click(null, null), _lv);

            _lv.ContextMenu.Items.Add(miSelectSkills);
            _lv.ContextMenu.Items.Add(miSelectMobSkills);
            _lv.ContextMenu.Items.Add(miRemoveDrop);
            _lv.ContextMenu.Items.Add(new Separator());
            _lv.ContextMenu.Items.Add(miCopy);
            _lv.ContextMenu.Items.Add(miPaste);

            miSelectSkills.Click += new RoutedEventHandler(_miSelect_Click);
            miSelectMobSkills.Click += new RoutedEventHandler(_miSelect2_Click);
            miRemoveDrop.Click += new RoutedEventHandler(_miRemoveDrop_Click);
            miCopy.Click += new RoutedEventHandler(_miCopy_Click);
            miPaste.Click += new RoutedEventHandler(_miPaste_Click);

            _lv.MouseRightButtonUp += (MouseButtonEventHandler)((sender, e) =>
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
            });

            _lv.PreviewMouseDown += delegate
            {
                Keyboard.Focus(_lv);
                //_lv.Focus();
            };

            dp.AddUpdateAction(new Action<TValue>(_update));

            tab.ProjectDatabase.Commands.CommandIndexChanged += _commands_CommandIndexChanged;
            grid.Children.Add(label);
            grid.Children.Add(_lv);

            dp.AddResetField(_lv);
        }

        #endregion ICustomControl<TKey,TValue> Members

        private void _miPaste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_tab.List.SelectedItem == null) return;
                if (!Clipboard.ContainsText()) return;

                ReadableTuple<int> tuple = (ReadableTuple<int>)_tab.List.SelectedItem;
                string text = Clipboard.GetText();
                StringBuilder builder = new StringBuilder();
                string sid = tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture);
                string name = tuple.GetStringValue(ServerMobAttributes.KRoName.Index);

                foreach (string line in text.Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] args = line.Split(',');

                    if (args.Length < 5)
                        throw new Exception("Not enough arguments to parse the lines (invalid clipboard data).");

                    builder.AppendLine(String.Join(",", new string[] { sid, name + "@" + args[1].Split('@')[1] }.Concat(args.Skip(2)).ToArray()));
                }

                string tempPath = TemporaryFilesManager.GetTemporaryFilePath("db_tmp_{0:0000}.txt");
                File.WriteAllText(tempPath, builder.ToString());

                AbstractDb<string> db;
                var isEnabled = _tab.GetDb<string>(ServerDbs.MobSkills2).IsEnabled;

                if (_tab.Settings.DbData == ServerDbs.Mobs || !isEnabled)
                    db = _tab.GetDb<string>(ServerDbs.MobSkills);
                else
                    db = _tab.GetDb<string>(ServerDbs.MobSkills2);

                var table = db.Table;

                try
                {
                    table.Commands.Begin();
                    db.LoadFromClipboard(tempPath);
                }
                catch
                {
                    table.Commands.CancelEdit();
                }
                finally
                {
                    table.Commands.EndEdit();
                    _tab.Update();
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

                foreach (MobSkillView item in _lv.SelectedItems)
                {
                    builder.AppendLine(String.Join(",", item.MobSkillTuple.GetRawElements().Skip(1).Select(p => p.ToString()).ToArray()));
                }

                string res = builder.ToString().Trim('\r', '\n');
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
            int id = item.GetKey<int>();
            string sid = id.ToString(CultureInfo.InvariantCulture);

            if (id == 0)
            {
                _lv.ItemsSource = null;
                return;
            }

            List<MobSkillView> result = new List<MobSkillView>();

            try
            {
                result.AddRange(_skillMobsTable.FastItems.Where(p => p.GetStringValue(ServerMobSkillAttributes.MobId.Index) == sid).Select(p => new MobSkillView(_skillsTable.TryGetTuple(p.GetValue<int>(ServerMobSkillAttributes.SkillId)), p, id)));
            }
            catch
            {
            }

            _lv.ItemsSource = new RangeObservableCollection<MobSkillView>(result.OrderBy(p => p, Extensions.BindDefaultSearch<MobSkillView>(_lv, "Name")));
        }

        private void _miRemoveDrop_Click(object sender, RoutedEventArgs e)
        {
            if (_lv.SelectedItems.Count <= 0)
                return;

            Table<string, ReadableTuple<string>> btable = _tab.GetMetaTable<string>(ServerDbs.MobSkills);

            btable.Commands.Begin();

            try
            {
                for (int i = 0; i < _lv.SelectedItems.Count; i++)
                {
                    var selectedItem = (MobSkillView)_lv.SelectedItems[i];

                    btable.Commands.Delete(selectedItem.MobSkillTuple.Key);

                    ((RangeObservableCollection<MobSkillView>)_lv.ItemsSource).Remove(selectedItem);
                    i--;
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
            finally
            {
                btable.Commands.EndEdit();
            }
        }

        private void _lv_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = _lv.GetObjectAtPoint<ListViewItem>(e.GetPosition(_lv));

            if (item != null)
            {
                TabNavigation.Select(ServerDbs.MobSkills, ((MobSkillView)item.Content).MobSkillTuple.GetKey<string>());
            }
        }

        private void _miSelect_Click(object sender, RoutedEventArgs e)
        {
            if (_lv.SelectedItems.Count > 0)
            {
                TabNavigation.SelectList(ServerDbs.Skills, _lv.SelectedItems.Cast<MobSkillView>().Where(p => p.SkillTuple != null).Select(p => p.SkillTuple.GetKey<int>()));
            }
        }

        private void _miSelect2_Click(object sender, RoutedEventArgs e)
        {
            if (_lv.SelectedItems.Count > 0)
            {
                TabNavigation.SelectList(ServerDbs.MobSkills, _lv.SelectedItems.Cast<MobSkillView>().Where(p => p.MobSkillTuple != null).Select(p => p.MobSkillTuple.GetKey<string>()));
            }
        }

        #region Nested type: MobSkillView

        public class MobSkillView : INotifyPropertyChanged
        {
            private readonly int _mobId;
            private readonly ReadableTuple<string> _mobSkillDbTuple;
            private readonly ReadableTuple<int> _skillDbTuple;

            public MobSkillView(ReadableTuple<int> skillDbTuple, ReadableTuple<string> mobSkillDbTuple, int mobId)
            {
                _skillDbTuple = skillDbTuple;
                _mobSkillDbTuple = mobSkillDbTuple;
                _mobId = mobId;

                if (_skillDbTuple != null)
                    _skillDbTuple.PropertyChanged += (s, e) => OnPropertyChanged();

                if (_mobSkillDbTuple != null)
                    _mobSkillDbTuple.PropertyChanged += (s, e) => OnPropertyChanged();

                _reload();
            }

            public int MobId
            {
                get { return _mobId; }
            }

            public string SkillId
            {
                get
                {
                    if (_skillDbTuple != null)
                        return _skillDbTuple.GetKey<int>().ToString(CultureInfo.InvariantCulture);
                    return Name;
                }
            }

            public ReadableTuple<string> MobSkillTuple
            {
                get { return _mobSkillDbTuple; }
            }

            public ReadableTuple<int> SkillTuple
            {
                get { return _skillDbTuple; }
            }

            public string Name { get; private set; }
            public string Condition { get; private set; }

            public bool Default
            {
                get { return true; }
            }

            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion INotifyPropertyChanged Members

            private void _reload()
            {
                if (_skillDbTuple != null)
                {
                    Name = _skillDbTuple.GetStringValue(ServerSkillAttributes.Desc.Index);

                    if (String.IsNullOrEmpty(Name)) Name = _skillDbTuple.Key.ToString(CultureInfo.InvariantCulture);
                }

                if (_mobSkillDbTuple != null)
                {
                    int icondition = _mobSkillDbTuple.GetValue<int>(ServerMobSkillAttributes.ConditionType);
                    string condition = Enum.GetValues(typeof(ConditionType)).Cast<Enum>().Select(Description.GetDescription).ToList()[icondition];

                    Condition = condition.
                        Replace("[CValue]", _mobSkillDbTuple.GetStringValue(ServerMobSkillAttributes.ConditionValue.Index)).
                        Replace("[Val1]", _mobSkillDbTuple.GetStringValue(ServerMobSkillAttributes.Val1.Index));

                    if (String.IsNullOrEmpty(Name)) Name = "#Not found - " + _mobSkillDbTuple.GetIntNoThrow(ServerMobSkillAttributes.SkillId);
                }

                if (String.IsNullOrEmpty(Name)) Name = "#Invalid ID";
            }

            protected virtual void OnPropertyChanged()
            {
                _reload();

                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(""));
            }
        }

        #endregion Nested type: MobSkillView
    }
}