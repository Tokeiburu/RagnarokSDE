using Database;
using ErrorManager;
using SDE.Core;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.TabsMakerCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;

namespace SDE.View.Dialogs
{
    /// <summary>
    /// Interaction logic for ScriptEditDialog.xaml
    /// </summary>
    public partial class ReplaceDialog : TkWindow
    {
        private readonly SdeEditor _editor;

        public ReplaceDialog(SdeEditor editor)
            : base("Replace all...", "convert.png", SizeToContent.Height, ResizeMode.NoResize)
        {
            _editor = editor;
            _editor.SelectionChanged += new SdeEditor.SdeSelectionChangedEventHandler(_editor_SelectionChanged);

            InitializeComponent();
            Extensions.SetMinimalSize(this);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = WpfUtilities.TopWindow;

            _update();
        }

        private void _editor_SelectionChanged(object sender, TabItem olditem, TabItem newitem)
        {
            _update();
        }

        public class DbAttributeWrapper
        {
            public DbAttribute Attribute { get; private set; }

            public DbAttributeWrapper(DbAttribute attribute)
            {
                Attribute = attribute;
            }

            public override string ToString()
            {
                return Attribute.DisplayName ?? Attribute.AttributeName;
            }
        }

        private GDbTab _tab;

        private void _update()
        {
            GDbTab tab = _editor.FindTopmostTab();

            if (tab != null)
            {
                try
                {
                    if (_cbAttribute.ItemsSource != null && _tab == tab)
                    {
                        _buttonOk.IsEnabled = true;
                        _cbAttribute.IsEnabled = true;
                        return;
                    }

                    _tab = tab;
                    _cbAttribute.ItemsSource = _tab.DbComponent.AttributeList.Attributes.Skip(1).Where(p => (p.Visibility & VisibleState.VisibleAndForceShow) != 0).Select(p => new DbAttributeWrapper(p));
                    _buttonOk.IsEnabled = true;
                    _cbAttribute.IsEnabled = true;
                    return;
                }
                catch (Exception err)
                {
                    ErrorHandler.HandleException(err);
                }
            }

            _cbAttribute.ItemsSource = null;
            _buttonOk.IsEnabled = false;
            _cbAttribute.IsEnabled = false;
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

        private void _replace<T>(GDbTab tab, DbAttribute attribute)
        {
            var aDb = tab.DbComponent.To<T>();

            try
            {
                if (attribute.DataType == typeof(bool) && attribute.DataConverter.GetType() == typeof(DefaultValueConverter))
                {
                    aDb.Table.Commands.Set(tab._listView.SelectedItems.Cast<ReadableTuple<T>>().ToList(), attribute, Boolean.Parse(_tbNewValue.Text));
                }
                else
                {
                    aDb.Table.Commands.Set(tab._listView.SelectedItems.Cast<ReadableTuple<T>>().ToList(), attribute, _tbNewValue.Text);
                }
            }
            finally
            {
                tab.Update();
            }
        }

        private void _replace()
        {
            try
            {
                if (_cbAttribute.SelectedIndex < 0)
                    throw new Exception("No attribute selected.");

                DbAttribute attribute = ((DbAttributeWrapper)_cbAttribute.SelectedItem).Attribute;
                GDbTab tab = _tab;

                if (tab == null)
                    throw new Exception("No tab selected.");

                if (tab._listView.SelectedItems.Count == 0)
                    throw new Exception("No items selected (select the items to replace in the list).");

                if (tab.DbComponent is AbstractDb<int>)
                {
                    _replace<int>(tab, attribute);
                }
                else if (tab.DbComponent is AbstractDb<string>)
                {
                    _replace<string>(tab, attribute);
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }
    }
}