using ErrorManager;
using GRF.Core.GroupedGrf;
using SDE.Editor;
using SDE.Editor.Engines;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Services;

namespace SDE.View.Controls
{
    /// <summary>
    /// Interaction logic for TextViewItem.xaml
    /// </summary>
    public partial class TextViewItem : UserControl, INotifyPropertyChanged
    {
        #region Delegates

        public delegate void TextViewItemEventHandler(object sender, EventArgs e);

        #endregion Delegates

        private readonly GetSetSetting _setting;
        private readonly MultiGrfReader _metaGrf;
        private string _defaultValue = "";
        private readonly ToolTip _toolTip;
        private bool _isSelected;

        public TextViewItem()
        {
            InitializeComponent();
        }

        public TextViewItem(ListView listView, GetSetSetting setting, MultiGrfReader metaGrf)
        {
            _setting = setting;
            _metaGrf = metaGrf;
            ListView = listView;

            InitializeComponent();

            _tblockDescription.PreviewMouseMove += (e, a) => OnMouseOver(e);
            _tbText.TextChanged += _tbText_TextChanged;
            _toolTip = new ToolTip();
            _tRectangleOverlay.ToolTip = _toolTip;

            MouseEnter += new MouseEventHandler(_tkTreeViewItem_MouseEnter);
            MouseLeave += new MouseEventHandler(_tkTreeViewItem_MouseLeave);

            try
            {
                DefaultValue = ProjectConfiguration.ConfigAsker.RetrieveSetting(() => setting.Value).Default;
            }
            catch
            {
                DefaultValue = setting.Value;
            }

            _tbText.SavePathUniqueName = "Server database editor - TVI - " + DefaultValue;
            _tbText.Text = setting.Value;

            PreviewDragEnter += _dragOver;
            PreviewDragOver += _dragOver;
            PreviewDragLeave += _dragLeave;
            PreviewDrop += (e, a) => { _tbText.OnMainGridDrop(e, a); a.Handled = true; };
        }

        public ListView ListView { get; set; }

        public Border TVIHeaderBrush
        {
            get { return _grid; }
        }

        public string DefaultValue
        {
            get
            {
                return _defaultValue;
            }
            set
            {
                _defaultValue = value;
            }
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;

                try
                {
                    if (_isSelected)
                    {
                        SetState(TviState.Selected);
                    }
                    else
                    {
                        SetState(TviState.None);
                    }
                }
                catch (Exception err)
                {
                    ErrorHandler.HandleException(err);
                }
            }
        }

        public string Filepath
        {
            get { return _setting.Value; }
        }

        public string Description
        {
            set
            {
                _tblockDescription.Text = value;
            }
        }

        public TextBox TextBoxItem
        {
            get { return _tbText.TextBox; }
        }

        public PathBrowser Browser
        {
            get { return _tbText; }
        }

        private void _dragLeave(object sender, DragEventArgs e)
        {
            if (IsSelected)
            {
                SetState(TviState.Selected);
            }
            else
            {
                SetState(TviState.None);
            }
            e.Handled = true;
        }

        private void _dragOver(object sender, DragEventArgs e)
        {
            SetState(TviState.DragOver);
            e.Handled = true;
        }

        public void SetState(TviState state)
        {
            switch (state)
            {
                case TviState.Selected:
                    TVIHeaderBrush.Background = Application.Current.Resources["TVISelectBackground"] as Brush;
                    TVIHeaderBrush.BorderBrush = Application.Current.Resources["TVISelectBorder"] as Brush;
                    break;

                case TviState.DragOver:
                    TVIHeaderBrush.Background = Application.Current.Resources["TVIMouseDragOverBackground"] as Brush;
                    TVIHeaderBrush.BorderBrush = Application.Current.Resources["TVIMouseDragOverBorder"] as Brush;
                    break;

                case TviState.MouseOver:
                    TVIHeaderBrush.Background = Application.Current.Resources["TVIMouseOverBackground"] as Brush;
                    TVIHeaderBrush.BorderBrush = Application.Current.Resources["TVIMouseOverBorder"] as Brush;
                    break;

                case TviState.None:
                    TVIHeaderBrush.Background = Application.Current.Resources["TVIDefaultBackground"] as Brush;
                    TVIHeaderBrush.BorderBrush = Application.Current.Resources["TVIDefaultBorder"] as Brush;
                    break;
            }
        }

        private void _tkTreeViewItem_MouseLeave(object sender, MouseEventArgs e)
        {
            TextViewItem item = sender as TextViewItem;

            if (item != null)
            {
                if (!item.IsSelected)
                {
                    item.SetState(TviState.None);
                }
            }
        }

        private void _tkTreeViewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            TextViewItem item = sender as TextViewItem;

            if (item != null)
            {
                if (!item.IsSelected)
                {
                    item.SetState(TviState.MouseOver);
                }
            }
        }

        private void _tbText_TextChanged(object sender, EventArgs e)
        {
            if (_tbText.Text.StartsWith("find:"))
            {
                var first = _metaGrf.FileTable.FindEntriesFromFileName(_tbText.Text.Split(new char[] { ':' }, 2)[1]).FirstOrDefault();

                if (first != null)
                {
                    _tbText.Text = first.RelativePath;
                    return;
                }
            }
            _setting.Value = _tbText.Text;
            CheckValid();
        }

        private void _generateToolTip()
        {
            string toolTip = "File not found.";

            if (Browser.BrowseMode == PathBrowser.BrowseModeType.Folder)
            {
                if (Filepath != null && _metaGrf != null && Directory.Exists(Filepath))
                {
                    toolTip = Filepath;
                }

                if (Filepath != null && !IOHelper.IsSystemFile(Filepath))
                    toolTip = Filepath;
            }
            else
            {
                if (Filepath != null && _metaGrf != null && _metaGrf.GetData(Filepath) != null)
                {
                    if (File.Exists(Filepath))
                    {
                        toolTip = Filepath;
                    }
                    else
                    {
                        TkPath path = _metaGrf.FindTkPath(Filepath);

                        if (String.IsNullOrEmpty(path.RelativePath))
                        {
                            toolTip = path.FilePath;
                        }
                        else
                        {
                            toolTip = path.FilePath + "\r\n" + path.RelativePath;
                        }
                    }
                }
            }

            _toolTip.Content = toolTip;
        }

        public event TextViewItemEventHandler MouseOver;

        public void OnMouseOver(object obj)
        {
            TextViewItemEventHandler handler = MouseOver;
            if (handler != null) handler(obj, null);
        }

        private void _buttonReset_Click(object sender, RoutedEventArgs e)
        {
            _tbText.Text = DefaultValue;
        }

        public void CheckValid(string path = null)
        {
            try
            {
                path = path ?? Filepath;

                _generateToolTip();

                if (Browser.BrowseMode == PathBrowser.BrowseModeType.Folder)
                {
                    if (Directory.Exists(path) || !IOHelper.IsSystemFile(path))
                    {
                        if (_tbText != null && _tbText.RecentFiles != null)
                            _tbText.RecentFiles.AddRecentFile(path);
                        _tblockDescription.Foreground = Application.Current.Resources["TextForeground"] as Brush;
                        _imgError.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        _tblockDescription.Foreground = Application.Current.Resources["CellBrushRemoved"] as Brush;
                        _imgError.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    if (_metaGrf.GetData(path) != null)
                    {
                        if (_tbText != null && _tbText.RecentFiles != null)
                            _tbText.RecentFiles.AddRecentFile(path);
                        _tblockDescription.Foreground = Application.Current.Resources["TextForeground"] as Brush;
                        _imgError.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        _tblockDescription.Foreground = Application.Current.Resources["CellBrushRemoved"] as Brush;
                        _imgError.Visibility = Visibility.Visible;
                    }
                }

                OnPropertyChanged(null);
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _menuItemsSelect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Filepath != null)
                {
                    if (File.Exists(Filepath) || Directory.Exists(Filepath))
                    {
                        OpeningService.FilesOrFolders(Filepath);
                        return;
                    }
                }

                if (Filepath == null || !_metaGrf.FileTable.ContainsFile(Filepath))
                {
                    ErrorHandler.HandleException("File path not found.");
                }
                else
                {
                    OpeningService.FilesOrFolders(_metaGrf.FindTkPath(Filepath).FilePath);
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _menuItemsReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _tbText.Text = DefaultValue;
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        public void ForceSet()
        {
            TextBoxItem.Text = _setting.Value;
            CheckValid();
        }

        public void ForceSetSetting()
        {
            _setting.Value = TextBoxItem.Text;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum TviState
    {
        MouseOver,
        Selected,
        DragOver,
        None
    }
}