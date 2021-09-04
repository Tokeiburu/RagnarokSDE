using ErrorManager;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using TokeiLibrary;

namespace SDE.Editor.Generic.UI.FormatConverters
{
    public class ElementalFormat : FormatConverter<int, ReadableTuple<int>>
    {
        private ComboBox _comboBoxElement;
        private ComboBox _comboBoxLevel;
        private GDbTabWrapper<int, ReadableTuple<int>> _tab;

        public int Get(ReadableTuple<int> tuple)
        {
            object val = tuple.GetValue(_attribute);
            string sval = val as string;

            if (String.IsNullOrEmpty(sval))
                return -1;

            return Int32.Parse((string)val);
        }

        public override void Init(GDbTabWrapper<int, ReadableTuple<int>> tab, DisplayableProperty<int, ReadableTuple<int>> dp)
        {
            _parent = _parent ?? tab.PropertiesGrid;

            _comboBoxLevel = new ComboBox { Margin = new Thickness(3), ItemsSource = new List<int> { 1, 2, 3, 4 } };
            _comboBoxElement = new ComboBox { Margin = new Thickness(0, 3, 3, 3), ItemsSource = Enum.GetValues(typeof(MobElementType)) };

            _comboBoxLevel.SelectionChanged += _comboBox_SelectionChanged;
            _comboBoxElement.SelectionChanged += _comboBox_SelectionChanged;

            _tab = tab;

            dp.AddResetField(_comboBoxLevel);
            dp.AddResetField(_comboBoxElement);

            Grid grid = new Grid();
            grid.SetValue(Grid.RowProperty, _row);
            grid.SetValue(Grid.ColumnProperty, _column);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            _comboBoxLevel.SetValue(Grid.ColumnProperty, 0);
            _comboBoxElement.SetValue(Grid.ColumnProperty, 1);

            grid.Children.Add(_comboBoxLevel);
            grid.Children.Add(_comboBoxElement);

            _parent.Children.Add(grid);

            dp.AddUpdateAction(new Action<ReadableTuple<int>>(item => grid.Dispatch(delegate
            {
                try
                {
                    int value = Get(item);

                    if (value < 0)
                    {
                        _comboBoxLevel.SelectedIndex = -1;
                        _comboBoxElement.SelectedIndex = -1;
                    }
                    else
                    {
                        int level = value / 10;
                        int property = value - level * 10;
                        level = level / 2 - 1;

                        _comboBoxLevel.SelectedIndex = level;
                        _comboBoxElement.SelectedIndex = property;
                    }
                }
                catch
                {
                }
            })));
        }

        private void _comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_tab.ItemsEventsDisabled) return;

            try
            {
                if (_tab.List.SelectedItem != null)
                {
                    int value = (_comboBoxLevel.SelectedIndex + 1) * 20 + _comboBoxElement.SelectedIndex;
                    _tab.Table.Commands.Set((ReadableTuple<int>)_tab.List.SelectedItem, _attribute, value.ToString(CultureInfo.InvariantCulture));
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }
    }
}