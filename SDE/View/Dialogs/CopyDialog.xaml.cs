using Database;
using ErrorManager;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.TabsMakerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Tuple = Database.Tuple;

namespace SDE.View.Dialogs
{
    /// <summary>
    /// Interaction logic for ScriptEditDialog.xaml
    /// </summary>
    public partial class CopyDialog : TkWindow
    {
        private readonly SdeEditor _editor;

        public CopyDialog(SdeEditor editor)
            : base("Copy all...", "imconvert.png", SizeToContent.WidthAndHeight, ResizeMode.NoResize)
        {
            _editor = editor;
            _editor.SelectionChanged += new SdeEditor.SdeSelectionChangedEventHandler(_editor_SelectionChanged);

            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = WpfUtilities.TopWindow;

            _cbSelectAll.Checked += delegate
            {
                _boxes.ForEach(p => p.IsChecked = true);
            };

            _cbSelectAll.Unchecked += delegate
            {
                _boxes.ForEach(p => p.IsChecked = false);
            };

            _update();
        }

        private void _editor_SelectionChanged(object sender, TabItem olditem, TabItem newitem)
        {
            _update();
        }

        private GDbTab _tab;

        private readonly List<CheckBox> _boxes = new List<CheckBox>();

        private void _update()
        {
            GDbTab tab = _editor.FindTopmostTab();

            if (tab != null)
            {
                try
                {
                    if (_boxes.Count > 0 && _tab == tab)
                    {
                        _buttonOk.IsEnabled = true;
                        return;
                    }

                    _tab = tab;

                    _gridCopy.Children.Clear();
                    _boxes.Clear();

                    int index = 0;

                    foreach (DbAttribute attribute in _tab.DbComponent.AttributeList.Attributes)
                    {
                        if (attribute.Index == 0) continue;

                        if ((attribute.Visibility & VisibleState.VisibleAndForceShow) != 0)
                        {
                            CheckBox box = new CheckBox { Margin = new Thickness(3, 3, 10, 3) };
                            box.Content = attribute.DisplayName ?? attribute.AttributeName;
                            box.Tag = attribute;
                            box.SetValue(Grid.RowProperty, index / _gridCopy.ColumnDefinitions.Count);
                            box.SetValue(Grid.ColumnProperty, index % _gridCopy.ColumnDefinitions.Count);
                            box.IsChecked = _cbSelectAll.IsChecked;
                            WpfUtils.AddMouseInOutEffectsBox(box);
                            _gridCopy.Children.Add(box);
                            _boxes.Add(box);
                            index++;
                        }
                    }

                    _buttonOk.IsEnabled = true;
                    return;
                }
                catch (Exception err)
                {
                    ErrorHandler.HandleException(err);
                }
            }

            _gridCopy.Children.Clear();
            _boxes.Clear();
            _buttonOk.IsEnabled = false;
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
            _replace();
        }

        private void _replace<T>(GDbTab tab, Database.Tuple tuple)
        {
            var aDb = tab.DbComponent.To<T>();
            aDb.Table.Commands.Begin();

            try
            {
                List<DbAttribute> attributes = _boxes.Where(p => p.IsChecked == true).Select(p => (DbAttribute)p.Tag).ToList();
                //List<ITableCommand<T, ReadableTuple<T>>> commands = new List<ITableCommand<T, ReadableTuple<T>>>();

                foreach (ReadableTuple<T> item in tab._listView.SelectedItems)
                {
                    for (int index = 0; index < attributes.Count; index++)
                    {
                        aDb.Table.Commands.Set(item, attributes[index], tuple.GetValue(attributes[index]));
                    }
                }

                //aDb.Table.Commands.StoreAndExecute(new GroupCommand<T, ReadableTuple<T>>(commands));
            }
            catch
            {
                aDb.Table.Commands.CancelEdit();
            }
            finally
            {
                aDb.Table.Commands.End();
                tab.Update();
            }
        }

        private void _replace()
        {
            try
            {
                if (_boxes.TrueForAll(p => p.IsChecked == false))
                    throw new Exception("No attribute selected.");

                GDbTab tab = _tab;

                if (tab == null)
                    throw new Exception("No tab selected.");

                if (tab._listView.SelectedItems.Count == 0)
                    throw new Exception("No items selected (select the items to replace in the list).");

                if (tab._listView.SelectedItems.Count == 1)
                    throw new Exception("You must select more than one item to copy (the currently selected one is the source).");

                var tuple = tab._listView.SelectedItem as Tuple;

                if (tab.DbComponent is AbstractDb<int>)
                {
                    _replace<int>(tab, tuple);
                }
                else if (tab.DbComponent is AbstractDb<string>)
                {
                    _replace<string>(tab, tuple);
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }
    }
}