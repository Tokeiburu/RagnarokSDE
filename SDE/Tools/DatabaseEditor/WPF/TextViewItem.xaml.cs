using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ErrorManager;
using SDE.Core;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Services;

namespace SDE.Tools.DatabaseEditor.WPF {
	/// <summary>
	/// Interaction logic for TextViewItem.xaml
	/// </summary>
	public partial class TextViewItem : UserControl {
		#region Delegates

		public delegate void TextViewItemEventHandler(object sender, EventArgs e);

		#endregion

		#region TviState enum

		public enum TviState {
			MouseOver,
			Selected,
			DragOver,
			None
		}

		#endregion

		private readonly MetaGrfHolder _metaGrf;
		private readonly Brush _mouseDragOverBackgroundBrush = new LinearGradientBrush(Color.FromArgb(80, 147, 255, 141), Color.FromArgb(80, 64, 255, 70), 90);
		private readonly Brush _mouseDragOverBorderBrush = new SolidColorBrush(Color.FromArgb(255, 92, 191, 92));
		private readonly ToolTip _toolTip;
		private Brush _defaultBackgroundBrush = new SolidColorBrush(Colors.Transparent);
		private Brush _defaultBorderBrush = new SolidColorBrush(Colors.Transparent);
		private string _defaultValue = "";
		private bool _isSelected;
		private Brush _mouseOverBackgroundBrush = new LinearGradientBrush(Color.FromArgb(80, 214, 228, 255), Color.FromArgb(80, 111, 176, 255), 90);
		private Brush _mouseOverBorderBrush = new SolidColorBrush(Color.FromArgb(255, 181, 178, 244));
		private Brush _selectBackgroundBrush = new LinearGradientBrush(Color.FromArgb(140, 214, 228, 255), Color.FromArgb(140, 111, 176, 255), 90);
		private Brush _selectBorderBrush = new SolidColorBrush(Color.FromArgb(255, 153, 150, 227));

		public TextViewItem() {
			InitializeComponent();
		}

		public TextViewItem(ListView listView, string defaultName, MetaGrfHolder metaGrf, object itemFile) {
			_metaGrf = metaGrf;
			ListView = listView;
			ItemFile = itemFile;

			InitializeComponent();

			_tblockDescription.PreviewMouseMove += (e, a) => OnMouseOver(e);
			_tbText.TextChanged += _tbText_TextChanged;
			_toolTip = new ToolTip();
			_tRectangleOverlay.ToolTip = _toolTip;

			MouseEnter += new MouseEventHandler(_tkTreeViewItem_MouseEnter);
			MouseLeave += new MouseEventHandler(_tkTreeViewItem_MouseLeave);

			DefaultValue = defaultName;

			PreviewDragEnter += _dragOver;
			PreviewDragOver += _dragOver;
			PreviewDragLeave += _dragLeave;
			PreviewDrop += (e, a) => { _tbText.OnMainGridDrop(e, a); a.Handled = true; };
			//CheckValid(defaultName);
		}

		public ListView ListView { get; set; }

		public Brush MouseOverBorderBrush {
			get { return _mouseOverBorderBrush; }
			set { _mouseOverBorderBrush = value; }
		}
		public Brush MouseOverBackgroundBrush {
			get { return _mouseOverBackgroundBrush; }
			set { _mouseOverBackgroundBrush = value; }
		}
		public Brush DefaultBackgroundBrush {
			get { return _defaultBackgroundBrush; }
			set { _defaultBackgroundBrush = value; }
		}
		public Brush DefaultBorderBrush {
			get { return _defaultBorderBrush; }
			set { _defaultBorderBrush = value; }
		}
		public Brush SelectBackgroundBrush {
			get { return _selectBackgroundBrush; }
			set { _selectBackgroundBrush = value; }
		}
		public Brush SelectBorderBrush {
			get { return _selectBorderBrush; }
			set { _selectBorderBrush = value; }
		}

		public Border TVIHeaderBrush {
			get { return _grid; }
		}

		public string DefaultValue {
			get {
				return _defaultValue;
			}
			set {
				_defaultValue = value;
				_tbText.Text = value;
			}
		}

		public object ItemFile { get; set; }

		public bool IsSelected {
			get {
				return _isSelected;
			}
			set {
				_isSelected = value;

				try {
					if (_isSelected) {
						SetState(TviState.Selected);
					}
					else {
						SetState(TviState.None);
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}

		public string Filepath {
			get {
				if (ItemFile != null) {
					if (ItemFile is SdeFiles) {
						return ((SdeFiles)ItemFile).Filename;
					}
				}

				return null;
			}
		}

		public string Description {
			get { return ""; }
			set {
				_tblockDescription.Text = value;
			}
		}

		public TextBox TextBoxItem {
			get { return _tbText.TextBox; }
		}

		public PathBrowser Browser {
			get { return _tbText; }
		}

		private void _dragLeave(object sender, DragEventArgs e) {
			if (IsSelected) {
				SetState(TviState.Selected);
			}
			else {
				SetState(TviState.None);
			}
			e.Handled = true;
		}

		private void _dragOver(object sender, DragEventArgs e) {
			SetState(TviState.DragOver);
			e.Handled = true;
		}

		public void SetState(TviState state) {
			switch (state) {
				case TviState.Selected:
					TVIHeaderBrush.Background = SelectBackgroundBrush;
					TVIHeaderBrush.BorderBrush = SelectBorderBrush;
					break;
				case TviState.DragOver:
					TVIHeaderBrush.Background = _mouseDragOverBackgroundBrush;
					TVIHeaderBrush.BorderBrush = _mouseDragOverBorderBrush;
					break;
				case TviState.MouseOver:
					TVIHeaderBrush.Background = MouseOverBackgroundBrush;
					TVIHeaderBrush.BorderBrush = MouseOverBorderBrush;
					break;
				case TviState.None:
					TVIHeaderBrush.Background = DefaultBackgroundBrush;
					TVIHeaderBrush.BorderBrush = DefaultBorderBrush;
					break;
			}
		}

		private void _tkTreeViewItem_MouseLeave(object sender, MouseEventArgs e) {
			TextViewItem item = sender as TextViewItem;

			if (item != null) {
				if (!item.IsSelected) {
					item.SetState(TviState.None);
				}
			}
		}
		private void _tkTreeViewItem_MouseEnter(object sender, MouseEventArgs e) {
			TextViewItem item = sender as TextViewItem;

			if (item != null) {
				if (!item.IsSelected) {
					item.SetState(TviState.MouseOver);
				}
			}
		}

		private void _tbText_TextChanged(object sender, EventArgs e) {
			if (ItemFile != null) {
				if (ItemFile is SdeFiles) {
					((SdeFiles) ItemFile).Filename = _tbText.Text;
				}
			}

			_generateToolTip();
			CheckValid();
		}

		private void _generateToolTip() {
			string toolTip = "File not found.";

			if (Browser.BrowseMode == PathBrowser.BrowseModeType.Folder) {
				if (Filepath != null && _metaGrf != null && Directory.Exists(Filepath)) {
					toolTip = Filepath;
				}
			}
			else {
				if (Filepath != null && _metaGrf != null && _metaGrf.GetData(Filepath) != null) {
					if (File.Exists(Filepath)) {
						toolTip = Filepath;
					}
					else {
						TkPath path = new TkPath(_metaGrf[Filepath]);

						if (String.IsNullOrEmpty(path.RelativePath)) {
							toolTip = path.FilePath;
						}
						else {
							toolTip = path.FilePath + "\r\n" + path.RelativePath;
						}
					}
				}
			}

			_toolTip.Content = toolTip;
		}

		public event TextViewItemEventHandler MouseOver;

		public void OnMouseOver(object obj) {
			TextViewItemEventHandler handler = MouseOver;
			if (handler != null) handler(obj, null);
		}

		private void _buttonReset_Click(object sender, RoutedEventArgs e) {
			_tbText.Text = DefaultValue;
		}

		public void CheckValid(string path = null) {
			try {
				path = path ?? Filepath;

				_generateToolTip();

				if (Browser.BrowseMode == PathBrowser.BrowseModeType.Folder) {
					if (Directory.Exists(path)) {
						_tblockDescription.Foreground = Brushes.Black;
						_imgError.Visibility = Visibility.Collapsed;
					}
					else {
						_tblockDescription.Foreground = Brushes.Red;
						_imgError.Visibility = Visibility.Visible;
					}
				}
				else {
					if (_metaGrf.GetData(path) != null) {
						_tblockDescription.Foreground = Brushes.Black;
						_imgError.Visibility = Visibility.Collapsed;
					}
					else {
						_tblockDescription.Foreground = Brushes.Red;
						_imgError.Visibility = Visibility.Visible;
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemsSelect_Click(object sender, RoutedEventArgs e) {
			try {
				if (Filepath != null) {
					if (File.Exists(Filepath) || Directory.Exists(Filepath)) {
						OpeningService.FilesOrFolders(Filepath);
						return;
					}
				}

				if (Filepath == null || _metaGrf[Filepath] == null) {
					ErrorHandler.HandleException("File path not found.");
				}
				else {
					OpeningService.FilesOrFolders(new TkPath(_metaGrf[Filepath]).FilePath);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemsReset_Click(object sender, RoutedEventArgs e) {
			try {
				_tbText.Text = DefaultValue;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}
