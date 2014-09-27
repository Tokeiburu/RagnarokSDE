using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using SDE.Others;
using SDE.Others.ViewItems;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Services;

namespace SDE.WPF {
	/// <summary>
	/// Interaction logic for MetaGrfResourcesViewer.xaml
	/// </summary>
	public partial class MetaGrfResourcesViewer : UserControl {
		private readonly ObservableCollection<TkPathView> _itemsResourcesSource = new ObservableCollection<TkPathView>();

		public MetaGrfResourcesViewer() {
			InitializeComponent();

			Extensions.GenerateListViewTemplate(_itemsResources, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", FixedWidth = 30, MaxHeight = 60 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "TK Path", DisplayExpression = "DisplayFileName", FixedWidth = 100, ToolTipBinding="DisplayFileName", TextAlignment = TextAlignment.Left, IsFill = true }
			}, null, new string[] { "FileNotFound", "Red" });

			_itemsResources.ItemsSource = _itemsResourcesSource;
			_loadResourcesInfo();
			WpfUtils.DisableContextMenuIfEmpty(_itemsResources);
		}

		public List<TkPath> Paths {
			get { return _itemsResources.Items.Cast<TkPathView>().Select(p => p.Path).ToList(); }
		}

		public Action<string> SaveResourceMethod { get; set; }
		public Func<List<string>> LoadResourceMethod { get; set; }

		private void _itemsResources_DragEnter(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Copy;
		}

		private void _itemsResources_Drop(object sender, DragEventArgs e) {
			try {
				if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
					string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

					if (files != null) {
						foreach (string file in files) {
							_itemsResourcesSource.Add(new TkPathView(new TkPath { FilePath = file }));
						}

						_saveResourcesInfo();
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemsMoveDown_Click(object sender, RoutedEventArgs e) {
			try {
				if (_itemsResources.SelectedItem != null) {
					TkPathView rme = (TkPathView)_itemsResources.SelectedItem;

					if (_itemsResourcesSource.Count <= 1)
						return;

					int index = _getIndex(rme);

					if (index < _itemsResourcesSource.Count - 1) {
						TkPathView old = _itemsResourcesSource[index + 1];
						_itemsResourcesSource.RemoveAt(index + 1);
						_itemsResourcesSource.Insert(index, old);

						_saveResourcesInfo();
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemsMoveUp_Click(object sender, RoutedEventArgs e) {
			try {
				if (_itemsResources.SelectedItem != null) {
					TkPathView rme = (TkPathView)_itemsResources.SelectedItem;

					if (_itemsResourcesSource.Count <= 1)
						return;

					int index = _getIndex(rme);

					if (index > 0) {
						TkPathView old = _itemsResourcesSource[index - 1];
						_itemsResourcesSource.RemoveAt(index - 1);
						_itemsResourcesSource.Insert(index, old);

						_saveResourcesInfo();
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemsSelectInExplorer_Click(object sender, RoutedEventArgs e) {
			try {
				if (_itemsResources.SelectedItem != null) {
					TkPathView rme = (TkPathView)_itemsResources.SelectedItem;

					OpeningService.FilesOrFolders(new string[] { rme.Path.FilePath });
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemsDelete_Click(object sender, RoutedEventArgs e) {
			try {
				for (int index = 0; index < _itemsResources.SelectedItems.Count; index++) {
					TkPathView rme = (TkPathView)_itemsResources.SelectedItems[index];
					_itemsResourcesSource.Remove(rme);
					index--;
				}

				_saveResourcesInfo();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _saveResourcesInfo() {
			if (SaveResourceMethod != null)
				SaveResourceMethod(Methods.ListToString(_itemsResourcesSource.Select(p => p.Path.GetFullPath()).ToList()));
		}

		private int _getIndex(TkPathView rme) {
			for (int i = 0; i < _itemsResourcesSource.Count; i++) {
				if (_itemsResourcesSource[i] == rme)
					return i;
			}

			return -1;
		}

		public void LoadResourcesInfo() {
			_loadResourcesInfo();
		}

		private void _loadResourcesInfo() {
			try {
				if (LoadResourceMethod == null) return;

				bool needsVisualReload = false;

				List<string> resources = LoadResourceMethod();

				if (resources.Count == _itemsResourcesSource.Count) {
					for (int index = 0; index < resources.Count; index++) {
						string resourcePath = resources[index];
						if (_itemsResourcesSource[index].Path.GetFullPath() != resourcePath) {
							needsVisualReload = true;
							break;
						}
					}
				}
				else {
					needsVisualReload = true;
				}

				if (needsVisualReload) {
					_itemsResourcesSource.Clear();

					foreach (string resourcePath in LoadResourceMethod()) {
						_itemsResourcesSource.Add(new TkPathView(new TkPath(resourcePath)));
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}
