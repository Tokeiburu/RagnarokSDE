using ErrorManager;
using GRF.IO;
using GRF.Threading;
using SDE.Editor.Engines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;

namespace SDE.View.Dialogs
{
    public class FtpEntry
    {
        public string Name { get; set; }
        public string Changed { get; set; }
        public string Rights { get; set; }

        public object DataImage { get { return ApplicationManager.GetResourceImage("folderClosed.png"); } }

        public bool Default
        {
            get { return true; }
        }
    }

    public class FtpEntrySorter : IComparer<FtpEntry>
    {
        public int Compare(FtpEntry x, FtpEntry y)
        {
            return String.CompareOrdinal(x.Name, y.Name);
        }
    }

    /// <summary>
    /// Interaction logic for ScriptEditDialog.xaml
    /// </summary>
    public partial class ExplorerDialog : TkWindow
    {
        private readonly string _path;
        private readonly SdeEditor _editor;

        public string CurrentPath { get; set; }
        private FtpEntrySorter _sorter = new FtpEntrySorter();

        public ExplorerDialog(string path, SdeEditor editor)
            : base("FTP explorer...", "find.png", SizeToContent.Manual, ResizeMode.CanResize)
        {
            _path = path;
            _editor = editor;

            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = WpfUtilities.TopWindow;

            ListViewDataTemplateHelper.GenerateListViewTemplateNew(_listViewResults, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
                new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "Name", FixedWidth = 20, MaxHeight = 24 },
                new ListViewDataTemplateHelper.RangeColumnInfo { Header = "Name", DisplayExpression = "Name", MinWidth = 100, ToolTipBinding = "Name", TextAlignment = TextAlignment.Left, IsFill = true },
                new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Changed", DisplayExpression = "Changed", FixedWidth = 150, ToolTipBinding = "Changed", TextAlignment = TextAlignment.Left },
                new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Rights", DisplayExpression = "Rights", FixedWidth = 100, ToolTipBinding = "Rights", TextAlignment = TextAlignment.Left }
            }, new DefaultListViewComparer<FtpEntry>(), new string[] { "Default", "{DynamicResource TextForeground}" });

            _validateFileManager();
            _setListing("/");

            _listViewResults.KeyDown += new KeyEventHandler(_listViewResults_KeyDown);

            _listViewResults.MouseDoubleClick += new MouseButtonEventHandler(_listViewResults_MouseDoubleClick);
            _listViewResults.SelectionChanged += new SelectionChangedEventHandler(_listViewResults_SelectionChanged);
        }

        private void _listViewResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _update();
        }

        private void _update()
        {
            string t;
            _label.Dispatch(p => p.Content = "Path: " + GrfPath.Combine(_currentPath, _listViewResults.SelectedItem != null ? ((t = ((FtpEntry)_listViewResults.SelectedItem).Name) == ".." ? "" : t) : "").Replace("\\", "/"));
        }

        private void _listViewResults_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    _listViewResults_MouseDoubleClick(null, null);
                }
                else if (e.Key == Key.Back)
                {
                    e.Handled = true;
                    _setListing(GrfPath.GetDirectoryName(_currentPath));
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private string _currentPath = "";

        private void _listViewResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = _listViewResults.SelectedItem as FtpEntry;

            if (item != null)
            {
                if (item.Name == "..")
                {
                    _setListing(GrfPath.GetDirectoryName(_currentPath));
                    return;
                }

                _setListing(GrfPath.CombineUrl(_currentPath, item.Name));
            }
        }

        private void _validateFileManager()
        {
            _interface = FileManager.Get(_path);
            _interface.Open();
        }

        private bool _eventsEnabled = true;
        private FileManager _interface;

        private void _setListing(string s)
        {
            if (!_eventsEnabled) return;

            if (String.IsNullOrEmpty(s))
                s = "/";

            _eventsEnabled = false;
            GrfThread.Start(() => _setListingSub(s));
        }

        private void _setListingSub(string path)
        {
            try
            {
                var files = _interface.GetDirectories(path).Where(p => p.getFilename() != ".");
                var entries = new List<FtpEntry>();

                foreach (var file in files)
                {
                    entries.Add(new FtpEntry { Name = file.getFilename(), Changed = file.getAttrs().getMtimeString(), Rights = file.getAttrs().getPermissionsString() });
                }

                if (_listViewResults.Dispatch(() => WpfUtils.GetLastSorted(_listViewResults)) == null)
                {
                    entries = entries.OrderBy(p => p, _sorter).ToList();
                }

                _listViewResults.Dispatch(p => p.ItemsSource = entries);
                _currentPath = path.Replace("\\", "/");
                _update();
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
            finally
            {
                _eventsEnabled = true;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _interface.Close();
            base.OnClosing(e);
        }

        private void _buttonOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string t;
                CurrentPath = GrfPath.Combine(_currentPath, _listViewResults.SelectedItem != null ? ((t = ((FtpEntry)_listViewResults.SelectedItem).Name) == ".." ? "" : t) : "").Replace("\\", "/");
                DialogResult = true;
                Close();
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }
    }
}