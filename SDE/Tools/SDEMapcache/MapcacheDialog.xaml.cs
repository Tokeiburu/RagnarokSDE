using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ErrorManager;
using GRF.Core;
using GRF.Threading;
using GrfToWpfBridge.Application;
using SDE.ApplicationConfiguration;
using SDE.Tools.SDEMapcache.Commands;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Extension;
using Utilities.Services;
using AsyncOperation = GrfToWpfBridge.Application.AsyncOperation;
using Extensions = SDE.Core.Extensions;

namespace SDE.Tools.SDEMapcache {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class MapcacheDialog : TkWindow {
		private readonly Mapcache _cache;
		private readonly WpfRecentFiles _recentFilesManager;
		private readonly AsyncOperation _asyncOperation;

		public MapcacheDialog(string text)
			: base("Mapcache", "cache.png", SizeToContent.Manual, ResizeMode.CanResize) {
			
			InitializeComponent();

			ShowInTaskbar = true;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			Owner = WpfUtilities.TopWindow;

			_cache = new Mapcache();
			_cache.Commands.ModifiedStateChanged += _commands_ModifiedStateChanged;
			_asyncOperation = new AsyncOperation(_progressBar);

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_listView, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Name", DisplayExpression = "MapName", SearchGetAccessor = "MapName", ToolTipBinding = "MapName", TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, FixedWidth = 140 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Width", DisplayExpression = "Xs", SearchGetAccessor = "Xs", ToolTipBinding = "Xs", TextAlignment = TextAlignment.Center, FixedWidth = 80 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Height", DisplayExpression = "Ys", SearchGetAccessor = "Ys", ToolTipBinding = "Ys", TextAlignment = TextAlignment.Center, FixedWidth = 80 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Size", DisplayExpression = "DisplaySize", SearchGetAccessor = "Len", ToolTipBinding = "DisplaySize", TextAlignment = TextAlignment.Center, FixedWidth = 150 },
			}, new DefaultListViewComparer<MapInfo>(), new string[] { "Added", "{DynamicResource CellBrushAdded}" });

			_tmbUndo.SetUndo(_cache.Commands);
			_tmbRedo.SetRedo(_cache.Commands);
			_commands_ModifiedStateChanged(null, null);
			_listView.ItemsSource = _cache.ViewMaps;

			ApplicationShortcut.Link(ApplicationShortcut.Save, () => _menuItemSave_Click(null, null), this);
			ApplicationShortcut.Link(ApplicationShortcut.Undo, () => _cache.Commands.Undo(), this);
			ApplicationShortcut.Link(ApplicationShortcut.Redo, () => _cache.Commands.Redo(), this);
			ApplicationShortcut.Link(ApplicationShortcut.New, () => _menuItemNew_Click(null, null), this);
			ApplicationShortcut.Link(ApplicationShortcut.Open, () => _menuItemOpenFrom_Click(null, null), this);
			ApplicationShortcut.Link(ApplicationShortcut.Delete, () => _miDelete_Click(null, null), this);

			_recentFilesManager = new WpfRecentFiles(Configuration.ConfigAsker, 6, _menuItemRecentFiles, "Mapcache");
			_recentFilesManager.FileClicked += _recentFilesManager_FileClicked;

			if (text != null) {
				_recentFilesManager.AddRecentFile(text);
				_load(text);
			}
			else {
				// Open the latest file
				if (_recentFilesManager.Files.Count > 0 && File.Exists(_recentFilesManager.Files[0])) {
					_load(_recentFilesManager.Files[0]);
				}
			}
		}

		private void _commands_ModifiedStateChanged(object sender, IMapcacheCommand command) {
			this.Dispatch(delegate {
				if (_cache.LoadedPath != null) {
					this.Title = "Mapcache - " + Methods.CutFileName(_cache.LoadedPath, 50) + (_cache.Commands.IsModified ? " *" : "");
				}
				else {
					this.Title = "Mapcache - map_cache.dat *";
				}

				_labelMapCount.Content = "Map count: " + _cache.MapCount;
				_labelMapAdded.Content = "Added: " + _cache.Maps.Count(p => p.Added);
				_labelSize.Content = "File Size: " + _cache.FileSize + " (" + Methods.FileSizeToString(_cache.FileSize) + ")";
				_listView.ItemsSource = _cache.ViewMaps;
			});
		}

		private void _miDelete_Click(object sender, RoutedEventArgs e) {
			try {
				if (!_validateState())
					return;
				_cache.Commands.DeleteMaps(_listView.SelectedItems.OfType<MapInfo>().Select(p => p.MapName).ToList());
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private bool _validateState() {
			if (_asyncOperation.IsRunning) {
				ErrorHandler.HandleException("An operation is currently running, wait for it to finish or cancel it.", ErrorLevel.NotSpecified);
				return false;
			}

			return true;
		}

		private void _menuItemSave_Click(object sender, RoutedEventArgs e) {
			if (!_validateState())
				return;

			if (_cache.LoadedPath == null) {
				_menuItemSaveAs_Click(sender, e);
				return;
			}

			_cache.Save(_cache.LoadedPath);
			_cache.LoadedPath = _cache.LoadedPath;
			_cache.Commands.SaveCommandIndex();
			_fakeProgress(0);
		}

		private void _menuItemSaveAs_Click(object sender, RoutedEventArgs e) {
			if (!_validateState())
				return;

			try {
				string file = PathRequest.SaveFileMapcache("filter", "Dat Files (*.dat)|*.dat", "fileName", Path.GetFileName(_cache.LoadedPath ?? "map_cache.dat"));

				if (file != null) {
					_cache.Save(file);
					_cache.LoadedPath = file;
					_cache.Commands.SaveCommandIndex();
					_fakeProgress(0);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _fakeProgress(int mode) {
			if (mode == 1) {
				_asyncOperation.SetAndRunOperation(new GrfThread(_dull, _cache, 200), delegate {
					_progressBar.SetSpecialState(TkProgressBar.ProgressStatus.FileLoaded);
				});
			}
			else
				_asyncOperation.SetAndRunOperation(new GrfThread(_dull, _cache, 200));
		}

		private void _dull() {
			AProgress.Finalize(_cache);
		}

		private void _menuItemAbout_Click(object sender, RoutedEventArgs e) {
			
		}

		private void _menuItemQuit_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		protected override void OnClosing(CancelEventArgs e) {
			try {
				if (_cache != null && _cache.Commands.IsModified) {
					MessageBoxResult res = WindowProvider.ShowDialog("The map cache has been modified, do you want to save it first?", "Modified map cache", MessageBoxButton.YesNoCancel);

					if (res == MessageBoxResult.Yes) {
						_menuItemSaveAs_Click(null, null);
						e.Cancel = true;
						return;
					}

					if (res == MessageBoxResult.Cancel) {
						e.Cancel = true;
						return;
					}
				}

				//ApplicationManager.Shutdown();
				base.OnClosing(e);
			}
			catch (Exception err) {
				try {
					ErrorHandler.HandleException("The application hasn't ended properly. Please report this issue.", err);
				}
				catch {
				}

				//ApplicationManager.Shutdown();
			}
		}

		private void _menuItemOpenFrom_Click(object sender, RoutedEventArgs e) {
			try {
				string file = PathRequest.OpenFileMapcache("filter", "Dat Files (*.dat)|*.dat");

				if (file != null) {
					if (File.Exists(file)) {
						_recentFilesManager.AddRecentFile(file);
						_load(file);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _load(string file) {
			if (file.IsExtension(".dat")) {
				if (_validateNewContainer()) {
					_cache.Load(file);
					_commands_ModifiedStateChanged(null, null);
					_fakeProgress(1);
				}
			}
		}

		private bool _validateNewContainer() {
			try {
				if (!_validateState())
					return false;

				if (_cache.Commands.IsModified) {
					MessageBoxResult res = WindowProvider.ShowDialog("The map cache has been modified, do you want to save it first?", "Modified map cache", MessageBoxButton.YesNoCancel);

					if (res == MessageBoxResult.Yes) {
						_menuItemSaveAs_Click(null, null);
						return false;
					}

					if (res == MessageBoxResult.Cancel) {
						return false;
					}
				}
			}
			catch {
				return true;
			}

			return true;
		}

		private void _recentFilesManager_FileClicked(string fileName) {
			try {
				if (File.Exists(fileName)) {
					_load(fileName);
				}
				else {
					ErrorHandler.HandleException("File not found : " + fileName, ErrorLevel.Low);
					_recentFilesManager.RemoveRecentFile(fileName);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemCloseGrf_Click(object sender, RoutedEventArgs e) {
			try {
				if (_validateNewContainer()) {
					_cache.Reset();
					_progressBar.Progress = 0;
					_commands_ModifiedStateChanged(null, null);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemSelect_Click(object sender, RoutedEventArgs e) {
			try {
				if (_cache.LoadedPath == null)
					throw new Exception("No file has been loaded.");

				OpeningService.FileOrFolder(_cache.LoadedPath);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemNew_Click(object sender, RoutedEventArgs e) {
			_menuItemCloseGrf_Click(null, null);
		}

		private void _listView_DragEnter(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Copy;
		}

		private void _listView_Drop(object sender, DragEventArgs e) {
			try {
				if (!_validateState())
					return;

				if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
					string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

					if (files == null)
						return;

					if (files.Length == 1 && files[0].IsExtension(".dat")) {
						_load(files[0]);
					}
					else {
						_cache.Commands.AddMaps(files.ToList());
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemAdd_Click(object sender, RoutedEventArgs e) {
			try {
				string[] files = PathRequest.OpenFilesCde("filter", "Gat Files (*.gat)|*.gat");

				if (files != null) {
					_cache.Commands.AddMaps(files.ToList());
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemDelete_Click(object sender, RoutedEventArgs e) {
			_miDelete_Click(null, null);
		}

		private void _buttonUndo_Click(object sender, RoutedEventArgs e) {
			if (!_validateState())
				return;
			_cache.Commands.Undo();
		}

		private void _buttonRedo_Click(object sender, RoutedEventArgs e) {
			if (!_validateState())
				return;
			_cache.Commands.Redo();
		}

		private void Border_MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}

		private void _menuItemMerge_Click(object sender, RoutedEventArgs e) {
			try {
				if (!_validateState())
					return;

				string file = PathRequest.OpenFileMapcache("filter", "Grf and Dat Files (*.grf, *.dat)|*.grf;*.dat");

				if (file != null) {
					if (file.IsExtension(".grf")) {
						_asyncOperation.SetAndRunOperation(new GrfThread(delegate {
							AProgress.Init(_cache);

							try {
								_cache.Commands.Begin();

								List<FileEntry> files = new List<FileEntry>();
								int count = 0;

								using (GrfHolder grf = new GrfHolder(file, GrfLoadOptions.Normal)) {
									files.AddRange(grf.FileTable.EntriesInDirectory("data\\", SearchOption.TopDirectoryOnly).Where(entry => entry.RelativePath.IsExtension(".gat")));

									foreach (var entry in files) {
										count++;
										AProgress.IsCancelling(_cache);
										FileEntry rswEntry = grf.FileTable.TryGet(entry.RelativePath.ReplaceExtension(".rsw"));

										if (rswEntry == null)
											continue;

										_cache.Progress = (float)count / files.Count * 100f;
										_cache.Commands.AddMap(Path.GetFileNameWithoutExtension(entry.DisplayRelativePath), entry.GetDecompressedData(), rswEntry.GetDecompressedData());
									}
								}
							}
							catch (OperationCanceledException) {
								_cache.IsCancelled = true;
								_cache.Commands.CancelEdit();
							}
							catch (Exception err) {
								_cache.Commands.CancelEdit();
								ErrorHandler.HandleException(err);
							}
							finally {
								_cache.Commands.End();
								AProgress.Finalize(_cache);
							}
						}, _cache, 200));
					}
					else if (file.IsExtension(".dat")) {
						Mapcache cache = new Mapcache(file);

						try {
							_cache.Commands.Begin();

							foreach (var map in cache.Maps) {
								_cache.Commands.AddMapRaw(map.MapName, map);
							}
						}
						catch {
							_cache.Commands.CancelEdit();
							throw;
						}
						finally {
							_cache.Commands.End();
						}
					}
					else {
						throw new Exception("Unreognized file format.");
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}
