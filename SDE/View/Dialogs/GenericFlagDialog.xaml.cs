using Database;
using SDE.ApplicationConfiguration;
using SDE.Editor;
using SDE.Editor.Engines;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Extension;
using Utilities.IndexProviders;

namespace SDE.View.Dialogs
{
    public class GridIndexProvider
    {
        private readonly int _row;
        private readonly int _col;
        private int _current;

        public int Current
        {
            get { return _current - 1; }
        }

        public GridIndexProvider(int row, int col)
        {
            _row = row;
            _col = col;
        }

        public bool Next(out int row, out int col)
        {
            row = _current % _row;
            col = (_current / _row) % _col;
            _current++;

            return _current <= _row * _col;
        }
    }

    /// <summary>
    /// Interaction logic for ScriptEditDialog.xaml
    /// </summary>
    public partial class GenericFlagDialog : TkWindow, IInputWindow
    {
        private readonly List<CheckBox> _boxes = new List<CheckBox>();
        private long _value;
        private readonly int _maxColWidth = 400;

        public GenericFlagDialog(DbAttribute attribute, string text, Type enumType) : this(attribute, text, enumType, null, _getDisplay(Description.GetAnyDescription(enumType)))
        {
        }

        public GenericFlagDialog(DbAttribute attribute, string text, Type enumType, FlagTypeData flagTypeData) : this(attribute, text, enumType, flagTypeData, enumType == null ? "Flag edit" : _getDisplay(Description.GetAnyDescription(enumType)))
        {
        }

        public GenericFlagDialog(DbAttribute attribute, string text, Type enumType, FlagTypeData flagTypeData, string description) : base(description, "cde.ico", SizeToContent.WidthAndHeight, ResizeMode.CanResize)
        {
            InitializeComponent();

            _value = text.ToLong();

            if (flagTypeData != null)
            {
                List<long> valuesEnum = flagTypeData.Values.Where(p => (p.DataFlag & FlagDataProperty.Hide) == 0).Select(p => p.Value).ToList();
                var values = flagTypeData.Values.Where(p => (p.DataFlag & FlagDataProperty.Hide) == 0).ToList();

                GridIndexProvider provider = _findGrid(values);

                var toolTips = new string[values.Count];

                for (int i = 0; i < values.Count; i++)
                    toolTips[i] = _getTooltip(values[i].Description);

                AbstractProvider iProvider = new DefaultIndexProvider(0, values.Count);
                ToolTipsBuilder.Initialize(toolTips, this);

                int row;
                int col;

                for (int i = 0; i < values.Count; i++)
                {
                    provider.Next(out row, out col);

                    int index = (int)iProvider.Next();
                    CheckBox box = new CheckBox { Content = values[index].Name, Margin = new Thickness(3, 6, 3, 6), VerticalAlignment = VerticalAlignment.Center };

                    var menu = new ContextMenu();
                    MenuItem item = new MenuItem();
                    item.Header = "Restrict search to [" + values[index].Name + "]";
                    box.ContextMenu = menu;
                    menu.Items.Add(item);
                    item.Click += delegate
                    {
                        var selected = SdeEditor.Instance.Tabs.FirstOrDefault(p => p.IsSelected);

                        if (selected != null)
                        {
                            selected._dbSearchPanel._searchTextBox.Text = "([" + attribute.AttributeName + "] & " + "Flags." + values[index].Name + ") != 0";
                        }
                    };

                    box.Tag = valuesEnum[index];
                    WpfUtils.AddMouseInOutEffectsBox(box);
                    _boxes.Add(box);
                    _upperGrid.Children.Add(box);
                    WpfUtilities.SetGridPosition(box, row, 2 * col);
                }

                _boxes.ForEach(_addEvents);
            }
            else
            {
                if (enumType.BaseType != typeof(Enum)) throw new Exception("Invalid argument type, excepted an enum.");

                if (enumType == typeof(MobModeType))
                {
                    if (DbPathLocator.GetServerType() == ServerType.RAthena && !ProjectConfiguration.UseOldRAthenaMode)
                    {
                        enumType = typeof(MobModeTypeNew);
                    }
                }

                List<long> valuesEnum = Enum.GetValues(enumType).Cast<int>().Select(p => (long)p).ToList();
                var values = Enum.GetValues(enumType).Cast<Enum>().ToList();

                string[] commands = Description.GetAnyDescription(enumType).Split('#');

                if (commands.Any(p => p.StartsWith("max_col_width:")))
                {
                    _maxColWidth = Int32.Parse(commands.First(p => p.StartsWith("max_col_width")).Split(':')[1]);
                }

                GridIndexProvider provider = _findGrid(values);

                var toolTips = new string[values.Count];

                if (!commands.Contains("disable_tooltips"))
                {
                    for (int i = 0; i < values.Count; i++)
                        toolTips[i] = _getTooltip(Description.GetDescription(values[i]));
                }

                AbstractProvider iProvider = new DefaultIndexProvider(0, values.Count);

                if (commands.Any(p => p.StartsWith("order:")))
                {
                    List<int> order = commands.First(p => p.StartsWith("order:")).Split(':')[1].Split(',').Select(Int32.Parse).ToList();

                    for (int i = 0; i < values.Count; i++)
                    {
                        if (!order.Contains(i))
                        {
                            order.Add(i);
                        }
                    }

                    iProvider = new SpecifiedIndexProvider(order);
                }

                ToolTipsBuilder.Initialize(toolTips, this);

                int row;
                int col;
                ServerType currentType = DbPathLocator.GetServerType();

                for (int i = 0; i < values.Count; i++)
                {
                    provider.Next(out row, out col);

                    int index = (int)iProvider.Next();
                    CheckBox box = new CheckBox { Content = _getDisplay(Description.GetDescription(values[index])), Margin = new Thickness(3, 6, 3, 6), VerticalAlignment = VerticalAlignment.Center };
                    ServerType type = _getEmuRestrition(Description.GetDescription(values[index]));

                    if ((type & currentType) != currentType)
                    {
                        box.IsEnabled = false;
                    }

                    var menu = new ContextMenu();
                    MenuItem item = new MenuItem();
                    item.Header = "Restrict search to [" + _getDisplay(Description.GetDescription(values[index])) + "]";
                    box.ContextMenu = menu;
                    menu.Items.Add(item);
                    item.Click += delegate
                    {
                        var selected = SdeEditor.Instance.Tabs.FirstOrDefault(p => p.IsSelected);

                        if (selected != null)
                        {
                            selected._dbSearchPanel._searchTextBox.Text = "([" + attribute.AttributeName + "] & " + valuesEnum[index] + ") != 0";
                        }
                    };

                    box.Tag = valuesEnum[index];
                    WpfUtils.AddMouseInOutEffectsBox(box);
                    _boxes.Add(box);
                    _upperGrid.Children.Add(box);
                    WpfUtilities.SetGridPosition(box, row, 2 * col);
                }

                _boxes.ForEach(_addEvents);
            }
        }

        private static ServerType _getEmuRestrition(string desc)
        {
            if (desc.Contains("#rAthena"))
            {
                return ServerType.RAthena;
            }
            if (desc.Contains("#Hercules"))
            {
                return ServerType.Hercules;
            }
            return ServerType.Both;
        }

        private static string _getDisplay(string desc)
        {
            if (desc.Contains("#"))
            {
                return desc.Split(new char[] { '#' }, 2)[0].TrimEnd('.');
            }
            return desc.TrimEnd('.');
        }

        private static string _getTooltip(string desc)
        {
            if (desc == null)
                return null;

            if (desc.Contains("#"))
            {
                return desc.Split(new char[] { '#' }, 2)[1];
            }
            return desc;
        }

        private GridIndexProvider _findGrid(ICollection values)
        {
            int maxRow;
            int maxCol;

            if (values.Count < 10)
            {
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto), MaxWidth = _maxColWidth });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = 20 });
                maxRow = values.Count;
                maxCol = 1;
            }
            else if (values.Count < 20)
            {
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto), MaxWidth = _maxColWidth });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = 20 });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto), MaxWidth = _maxColWidth });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = 20 });

                maxRow = (values.Count + 1) / 2;
                maxCol = 2;
            }
            else if (values.Count < 30)
            {
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto), MaxWidth = _maxColWidth });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = 20 });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto), MaxWidth = _maxColWidth });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = 20 });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto), MaxWidth = _maxColWidth });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = 20 });

                maxRow = (values.Count + 1) / 3;
                maxCol = 3;
            }
            else
            {
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto), MaxWidth = _maxColWidth });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = 20 });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto), MaxWidth = _maxColWidth });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = 20 });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto), MaxWidth = _maxColWidth });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = 20 });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto), MaxWidth = _maxColWidth });
                _upperGrid.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = 20 });

                maxRow = (values.Count + 1) / 4;
                maxCol = 4;
            }

            for (int i = 0; i < maxRow; i++)
            {
                _upperGrid.RowDefinitions.Add(new RowDefinition());
            }

            GridIndexProvider provider = new GridIndexProvider(maxRow, maxCol);
            return provider;
        }

        public string Text
        {
            get { return _value.ToString(CultureInfo.InvariantCulture); }
        }

        public Grid Footer
        {
            get { return _footerGrid; }
        }

        private void _addEvents(CheckBox cb)
        {
            ToolTipsBuilder.SetupNextToolTip(cb, this);
            cb.IsChecked = ((long)cb.Tag & _value) == (long)cb.Tag;

            cb.Checked += (e, a) => _update();
            cb.Unchecked += (e, a) => _update();
        }

        private void _update()
        {
            _value = 0;

            foreach (var box in _boxes)
            {
                if (box.IsChecked == true)
                {
                    _value |= (long)box.Tag;
                }
            }

            OnValueChanged();
        }

        protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void _buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void _buttonOk_Click(object sender, RoutedEventArgs e)
        {
            if (!SdeAppConfiguration.UseIntegratedDialogsForFlags)
                DialogResult = true;
            Close();
        }

        public event Action ValueChanged;

        public void OnValueChanged()
        {
            Action handler = ValueChanged;
            if (handler != null) handler();
        }
    }
}