using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.Core;
using GRF.Core.GroupedGrf;
using GRF.Image;
using GrfToWpfBridge;
using SDE.ApplicationConfiguration;
using SDE.Core;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace SDE.View {
	/// <summary>
	/// Interaction logic for MetaGrfEplorer.xaml
	/// </summary>
	public partial class MultiGrfExplorer : TkWindow {
		private readonly string _explorerPath = "";
		private readonly object _filterLock = new object();

		private readonly MultiGrfReader _metaGrf;
		private readonly GrfImageWrapper _wrapper = new GrfImageWrapper();
		private RangeObservableCollection<FileEntry> _entries = new RangeObservableCollection<FileEntry>();
		private readonly List<FileEntry> _entriesAll = new List<FileEntry>();
		private string _searchFilter = "";

		public MultiGrfExplorer(MultiGrfReader metaGrf, string explorerPath, string filter, string selected)
			: base("Meta GRF explorer", "cde.ico", SizeToContent.Manual, ResizeMode.CanResize) {
			InitializeComponent();

			ShowInTaskbar = true;
			_explorerPath = explorerPath.ToLower();
			_metaGrf = metaGrf;
			//metaGrf.Lock();

			if (filter != "") {
				_entries.AddRange(metaGrf.FilesInDirectory(_explorerPath).Select(metaGrf.GetEntry).Where(p => p.RelativePath.IndexOf(filter, StringComparison.OrdinalIgnoreCase) > -1));
			}
			else {
				_entries.AddRange(metaGrf.FilesInDirectory(_explorerPath).Select(metaGrf.GetEntry));
			}

			_entriesAll.AddRange(_entries);

			foreach (FileEntry entry in _entries) {
				entry.DataImage = entry.DisplayRelativePath;
			}

			_loadEncoding();

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_items, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "File name", DisplayExpression = "DataImage", ToolTipBinding="RelativePath", IsFill = true, MinWidth = 60, TextWrapping = TextWrapping.Wrap },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Type", DisplayExpression = "FileType", FixedWidth = 40, TextAlignment = TextAlignment.Right, ToolTipBinding="FileType" },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Size", DisplayExpression = "DisplaySize", FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding="DisplaySize" },
			}, new MetaGrfSorter(), new string[] { });

			_items.MouseDoubleClick += new MouseButtonEventHandler(_items_MouseDoubleClick);

			_items.ItemsSource = _entries;
			VirtualFileDataObject.SetDraggable(_imagePreview, _wrapper);

			ApplicationShortcut.Link(ApplicationShortcut.Copy, _copyItems, _items);
			ApplicationShortcut.Link(ApplicationShortcut.Confirm, () => _buttonSelect_Click(null, null), _items);

			this.Loaded += delegate {
				_items.SelectedItem = _entries.FirstOrDefault(p => String.CompareOrdinal(p.DisplayRelativePath, selected.ToDisplayEncoding(true) + ".bmp") == 0);
				_items.ScrollToCenterOfView(_items.SelectedItem);
			};

			this.Owner = WpfUtilities.TopWindow;
		}

		private void _copyItems() {
			try {
				StringBuilder builder = new StringBuilder();

				for (int index = 0; index < _items.SelectedItems.Count; index++) {
					FileEntry item = (FileEntry) _items.SelectedItems[index];

					if (index != _items.SelectedItems.Count - 1)
						builder.AppendLine((string) item.DataImage);
					else
						builder.Append((string) item.DataImage);
				}

				Clipboard.SetDataObject(builder.ToString());
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _loadEncoding() {
			_comboBoxEncoding.Init(new[] {
				new EncodingView { FriendlyName = "Client encoding (" + SdeAppConfiguration.EncodingCodepageClient + ")", Encoding = EncodingService.DisplayEncoding },
				new EncodingView { FriendlyName = "Server encoding (" + SdeAppConfiguration.EncodingCodepageServer + ")", Encoding = SdeAppConfiguration.EncodingServer }
			}.Concat(EncodingService.GetKnownEncodings()).ToList(),
			new TypeSetting<int>(v => SdeAppConfiguration.EncodingCodepageView = v, () => SdeAppConfiguration.EncodingCodepageView),
			new TypeSetting<Encoding>(v => SdeAppConfiguration.EncodingMetaGrfView = v, () => SdeAppConfiguration.EncodingMetaGrfView));

			_comboBoxEncoding.EncodingChanged += delegate {
				_reloadView(true);
			};

			_reloadView(true);
		}

		private void _reloadView(bool forceReload = false) {
			if (SdeAppConfiguration.EncodingCodepageView == EncodingService.DisplayEncoding.CodePage && !forceReload) return;

			try {
				var oldEntry = _items.SelectedItem;
				_items.ItemsSource = null;
				var encoding = Encoding.GetEncoding(SdeAppConfiguration.EncodingCodepageView);

				foreach (FileEntry entry in _entries) {
					entry.DataImage = encoding.GetString(EncodingService.DisplayEncoding.GetBytes(entry.DisplayRelativePath));
				}

				_items.ItemsSource = _entries;

				_items.SelectedItem = oldEntry;
				_items.ScrollToCenterOfView(_items.SelectedItem);
			}
			catch (Exception err) {
				Debug.Ignore(() => _items.ItemsSource = _entries);
				ErrorHandler.HandleException(err);
			}
		}

		private void _items_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			var item = _items.GetObjectAtPoint<ListViewItem>(e.GetPosition(_items));

			if (item != null) {
				_buttonSelect_Click(null, null);
			}
		}

		public TkPath SelectedPath { get; set; }

		private void _menuItemImageExport_Click(object sender, RoutedEventArgs e) {
			if (_wrapper.Image != null) {
				try {
					object selectedItem = _items.SelectedItem;
					string displayFileName = ((FileEntry) selectedItem).DisplayRelativePath;

					_wrapper.Image.SaveTo(displayFileName, PathRequest.ExtractSetting);
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}

		private void _buttonSelect_Click(object sender, RoutedEventArgs e) {
			object selectedItem = _items.SelectedItem;

			if (selectedItem != null) {
				DialogResult = true;
				Close();
			}
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			DialogResult = false;
			Close();
		}

		protected override void OnClosing(CancelEventArgs e) {
			//_metaGrf.Unlock();
		}

		private void _textBoxSearch_TextChanged(object sender, TextChangedEventArgs e) {
			_searchFilter = _textBoxSearch.Text;
			_filter();
		}

		private void _items_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				if (_items.SelectedItem != null) {
					object selectedItem = _items.SelectedItem;

					string displayFileName = ((FileEntry) selectedItem).DisplayRelativePath;
					SelectedPath = _metaGrf.FindTkPath(Path.Combine(_explorerPath, displayFileName));
					_wrapper.Image = ImageProvider.GetImage(_metaGrf.GetData(Path.Combine(_explorerPath, displayFileName)), Path.GetExtension(displayFileName));
					_imagePreview.Tag = displayFileName;
					_imagePreview.Source = _wrapper.Image.Cast<BitmapSource>();
				}
			}
			catch {
				_wrapper.Image = null;
				_imagePreview.Source = null;
			}
		}

		private void _filter() {
			string currentSearch = _searchFilter;

			new Thread(new ThreadStart(delegate {
				lock (_filterLock) {
					try {
						if (currentSearch != _searchFilter)
							return;

						if (_items == null) return;

						List<string> search = currentSearch.Split(' ').ToList();
						string displayFileName;
						string displayFileName2;
						//FileEntry entry;

						List<FileEntry> objectsToAdd = new List<FileEntry>();

						foreach (var entry in _entriesAll) {
							//entry = _metaGrf.GetEntry(path);

							displayFileName = ((string)entry.DataImage) ?? "";
							displayFileName2 = entry.DisplayRelativePath;

							if (search.All(q => displayFileName.IndexOf(q, StringComparison.InvariantCultureIgnoreCase) != -1) ||
								search.All(q => displayFileName2.IndexOf(q, StringComparison.InvariantCultureIgnoreCase) != -1))
								objectsToAdd.Add(entry);
						}

						_entries = new RangeObservableCollection<FileEntry>(objectsToAdd);
						_items.Dispatch(p => p.ItemsSource = _entries);
					}
					catch {
					}
				}
			})) { Name = "CDEditor - MetaGrfExplorer search filter thread" }.Start();
		}

		#region Nested type: MetaGrfSorter

		internal class MetaGrfSorter : ListViewCustomComparer {
			public override int Compare(object x, object y) {
				try {
					string valx = String.Empty, valy = String.Empty;

					if (_sortColumn == null)
						return 0;

					switch (_sortColumn) {
						case "DisplayRelativePath":
							valx = ((FileEntry) x).DisplayRelativePath;
							valy = ((FileEntry) y).DisplayRelativePath;
							break;
						case "DisplaySize":
							int x1 = ((FileEntry) x).NewSizeDecompressed;
							int y1 = ((FileEntry) y).NewSizeDecompressed;

							return _direction == ListSortDirection.Ascending ? (y1 - x1) : (x1 - y1);
						case "FileType":
							valx = ((FileEntry) x).FileType;
							valy = ((FileEntry) y).FileType;
							break;
					}

					if (_direction == ListSortDirection.Ascending)
						return String.CompareOrdinal(valx, valy);

					return (-1) * String.CompareOrdinal(valx, valy);
				}
				catch (Exception) {
					return 0;
				}
			}
		}

		#endregion
	}
}
