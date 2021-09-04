using ErrorManager;
using SDE.Core.Avalon;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using TokeiLibrary.WPF.Styles;

namespace SDE.View.Dialogs
{
    /// <summary>
    /// Interaction logic for ConvertItemIdsDialog.xaml
    /// </summary>
    public partial class ConvertItemIdsDialog : TkWindow
    {
        private MetaTable<int> _citemsDb;

        public ConvertItemIdsDialog()
            : base("Convert item IDs in text", "convert.png", SizeToContent.Manual, ResizeMode.CanResize)
        {
            InitializeComponent();

            ShowInTaskbar = true;

            _tbSource.TextChanged += (e, a) => _updateDestination();

            _tbSource.PreviewKeyUp += _onCloseKey;
            _tbDest.PreviewKeyUp += _onCloseKey;

            AvalonLoader.Load(_tbSource);
            AvalonLoader.SetSyntax(_tbSource, "Script");

            AvalonLoader.Load(_tbDest);
            AvalonLoader.SetSyntax(_tbDest, "Script");

            _tbSource.Text = Clipboard.GetText();

            try
            {
                _citemsDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.CItems);
            }
            catch
            {
                _citemsDb = null;
            }
        }

        private void _onCloseKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void _updateDestination()
        {
            try
            {
                if (_citemsDb == null || _citemsDb.Count == 0)
                {
                    try
                    {
                        _citemsDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.CItems);
                    }
                    catch
                    {
                        _citemsDb = null;
                    }
                }

                if (_citemsDb == null || _citemsDb.Count == 0)
                {
                    _tbDest.Text = "No tables were loaded...";
                    return;
                }

                Regex regex = new Regex(@"(\d+)");
                string copy = _tbSource.Text;

                foreach (var match in regex.Matches(_tbSource.Text).OfType<Match>().Reverse())
                {
                    int id;

                    if (Int32.TryParse(match.ToString(), out id) && (id < 2020 || id > 2023))
                    {
                        var tuple = _citemsDb.TryGetTuple(id);

                        if (tuple != null)
                        {
                            copy = copy.Remove(match.Index, match.Length);
                            copy = copy.Insert(match.Index, tuple.GetValue<string>(ClientItemAttributes.IdentifiedDisplayName));
                        }
                    }
                }

                _tbDest.Text = copy;
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }
    }
}