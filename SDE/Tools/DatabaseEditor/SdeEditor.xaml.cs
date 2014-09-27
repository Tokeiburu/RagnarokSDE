using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Database;
using ErrorManager;
using GRF.FileFormats;
using GRF.IO;
using GRF.System;
using GRF.Threading;
using GrfToWpfBridge.Application;
using SDE.ApplicationConfiguration;
using SDE.Core.ViewItems;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Engines.DatabaseEngine;
using SDE.Tools.DatabaseEditor.Engines.TabNavigationEngine;
using SDE.Tools.DatabaseEditor.Generic;
using SDE.Tools.DatabaseEditor.Generic.Core;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using SDE.Tools.DatabaseEditor.WPF;
using SDE.WPF;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.CommandLine;
using Extensions = SDE.Core.Extensions;

namespace SDE.Tools.DatabaseEditor {
	/// <summary>
	/// Interaction logic for CDEditor.xaml
	/// </summary>
	public partial class SdeEditor : TkWindow, IProgress, IErrorListener {
		public readonly List<GDbTab> GdTabs = new List<GDbTab>();
		private readonly AsyncOperation _asyncOperation;
		private readonly GenericDatabase _clientDatabase;
		private readonly ObservableCollection<DebugItemView> _debugItems;
		private DbHolder _holder;
		private TabNavigation _tabEngine;

		public SdeEditor() : base("Server database editor", "cde.ico", SizeToContent.Manual, ResizeMode.CanResize) {
			SplashDialog loading = new SplashDialog();
			loading.Show();
			Loaded += delegate {
				loading.Terminate();
			};

			string configFile = _parseCommandLineArguments();

			GrfPath.Delete(ProjectConfiguration.DefaultFileName);

			if (configFile == null) {
				ProjectConfiguration.ConfigAsker = new ConfigAsker(ProjectConfiguration.DefaultFileName);
			}
			else if (File.Exists(configFile)) {
				ProjectConfiguration.ConfigAsker = new ConfigAsker(configFile);
			}

			InitializeComponent();
			ShowInTaskbar = true;

			_asyncOperation = new AsyncOperation(_progressBar);
			_clientDatabase = new GenericDatabase(_metaGrf);

			_loadMenu();
			_loadSettingsTab();
			if (configFile != null) { ReloadSettings(configFile); }
			_loadGenericTab();

			_clientDatabase.Modified += new BaseGenericDatabase.ClientDatabaseEventHandler(_clientDatabase_Modified);

			_cbAssociate.Checked -= new RoutedEventHandler(_cbAssociate_Checked);
			_cbAssociate.IsChecked = (SdeAppConfiguration.FileShellAssociated & FileAssociation.Sde) == FileAssociation.Sde;
			_cbAssociate.Checked += new RoutedEventHandler(_cbAssociate_Checked);

			ApplicationShortcut.Link(ApplicationShortcut.Undo, () => _clientDatabase.Commands.Undo(), this);
			ApplicationShortcut.Link(ApplicationShortcut.UndoGlobal, () => _clientDatabase.Commands.Undo(), this);
			ApplicationShortcut.Link(ApplicationShortcut.Redo, () => _clientDatabase.Commands.Redo(), this);
			ApplicationShortcut.Link(ApplicationShortcut.RedoGlobal, () => _clientDatabase.Commands.Redo(), this);
			ApplicationShortcut.Link(ApplicationShortcut.Search, () => _execute(v => v.Search()), this);
			ApplicationShortcut.Link(ApplicationShortcut.Delete, () => _execute(v => v.DeleteItems()), this);
			ApplicationShortcut.Link(ApplicationShortcut.Rename, () => _execute(v => v.ChangeId()), this);
			ApplicationShortcut.Link(ApplicationShortcut.NavigationBackward, () => _tabEngine.Undo(), this);
			ApplicationShortcut.Link(ApplicationShortcut.NavigationBackward2, () => _tabEngine.Redo(), this);
			ApplicationShortcut.Link(ApplicationShortcut.NavigationForward, () => _tabEngine.Redo(), this);
			ApplicationShortcut.Link(ApplicationShortcut.Change, () => _execute(v => v.ChangeId()), this);
			ApplicationShortcut.Link(ApplicationShortcut.Restrict, () => _execute(v => v.ShowSelectedOnly()), this);
			ApplicationShortcut.Link(ApplicationShortcut.CopyTo, () => _execute(v => v.CopyItemTo()), this);
			ApplicationShortcut.Link(ApplicationShortcut.New, () => _execute(v => v.AddNewItem()), this);
			ApplicationShortcut.Link(ApplicationShortcut.Save, () => _menuItemDatabaseSave_Click(this, null), this);
			ApplicationShortcut.Link(ApplicationShortcut.SaveAll, () => _menuItemDatabaseSaveAll_Click(this, null), this);
			
			SdeAppConfiguration.Bind(_cbStackTrace, () => Configuration.EnableDebuggerTrace, v => {
				Configuration.EnableDebuggerTrace = v;
				SdeErrorHandler.ShowStackTraceViewer();
			});

			_tnbUndo.SetUndo(_tabEngine);
			_tnbRedo.SetRedo(_tabEngine);

			_tmbUndo.SetUndo(_clientDatabase.Commands);
			_tmbRedo.SetRedo(_clientDatabase.Commands);

			Extensions.GenerateListViewTemplate(_debugList, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "#", DisplayExpression = "ErrorNumber", SearchGetAccessor = "ErrorNumber", FixedWidth = 35, ToolTipBinding = "ErrorNumber", TextAlignment = TextAlignment.Right },
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "Exception", FixedWidth = 20, MaxHeight = 24 },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "Exception", DisplayExpression = "Exception", SearchGetAccessor = "Exception", IsFill = true, TextAlignment = TextAlignment.Left, ToolTipBinding="OriginalException", TextWrapping = TextWrapping.Wrap, MinWidth = 120 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Id", DisplayExpression = "Id", SearchGetAccessor = "Id", FixedWidth = 90, TextAlignment = TextAlignment.Left, ToolTipBinding="Id", TextWrapping = TextWrapping.Wrap },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "File", DisplayExpression = "FileName", SearchGetAccessor = "FilePath", FixedWidth = 130, TextAlignment = TextAlignment.Left, ToolTipBinding="FilePath", TextWrapping = TextWrapping.Wrap },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Line", DisplayExpression = "Line", SearchGetAccessor = "Line", FixedWidth = 40, TextAlignment = TextAlignment.Left, ToolTipBinding="Line" },
			}, null, new string[] { "Added", "Blue", "Default", "Black" });

			ApplicationShortcut.Link(ApplicationShortcut.Copy, () => WpfUtils.CopyContent(_debugList), _debugList);

			_debugItems = new ObservableCollection<DebugItemView>();
			_debugList.ItemsSource = _debugItems;

			DbLoaderErrorHandler.ClearListeners();
			DbLoaderErrorHandler.AddListener(this);

			_clientDatabase.PreviewReloaded += delegate {
				this.BeginDispatch(delegate {
					foreach (TabItem tabItem in _mainTabControl.Items) {
						tabItem.IsEnabled = true;

						var tabItemHeader = tabItem.Header as DisplayLabel;

						if (tabItemHeader != null)
							tabItemHeader.ResetEnabled();
					}
				});
			};

			_clientDatabase.Reloaded += delegate {
				_mainTabControl.Dispatch(p => p.RaiseEvent(new SelectionChangedEventArgs(Selector.SelectionChangedEvent, new List<object>(), _mainTabControl.SelectedItem == null ? new List<object>() : new List<object> { _mainTabControl.SelectedItem })));
				ServerType serverType = AllLoaders.GetServerType();
				bool renewal = AllLoaders.GetIsRenewal();

				string header = String.Format("Current ({0} - {1})", serverType == ServerType.RAthena ? "rA" : "Herc", renewal ? "Renewal" : "Pre-Renewal");

				this.BeginDispatch(delegate {
					_menuItemExportDbCurrent.IsEnabled = true;
					_menuItemExportDbCurrent.Header = header;

					_menuItemExportSqlCurrent.IsEnabled = true;
					_menuItemExportSqlCurrent.Header = header;
				});
			};
		}

		public AsyncOperation AsyncOperation {
			get { return _asyncOperation; }
		}

		public List<GDbTab> Tabs {
			get { return GdTabs; }
		}

		#region IErrorListener Members

		public void Handle(string exception) {
			Handle(exception, ErrorLevel.Warning);
		}

		public void Handle(string exception, ErrorLevel errorLevel) {
			Dispatcher.Invoke(new Action(delegate {
				if (_mainTabControl.SelectedIndex != 1 && ((TabItem)_mainTabControl.Items[1]).Header.ToString() != "Error console *")
					((TabItem)_mainTabControl.Items[1]).Header = new DisplayLabel { DisplayText = "Error console *", FontWeight = FontWeights.Bold };

				_debugItems.Add(new DebugItemView(_debugItems.Count + 1, exception, errorLevel));
			}), DispatcherPriority.Background);
		}

		#endregion

		#region IProgress Members

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }
		public void CancelOperation() {
			IsCancelling = true;
		}

		#endregion

		private string _parseCommandLineArguments() {
			List<GenericCLOption> options = CommandLineParser.GetOptions(Environment.CommandLine, false);

			foreach (GenericCLOption option in options) {
				if (option.CommandName == "-REM" || option.CommandName == "REM") {
					break;
				}
				if (option.Args.Count <= 0) {
					continue;
				}
				else if (option.Args[0].EndsWith(".sde")) {
					return options[0].Args[0];
				}
			}

			return null;
		}

		private void _clientDatabase_Modified(object sender) {
			Dispatcher.BeginInvoke(new Action(() => Title = "Server database editor - " + Methods.CutFileName(ProjectConfiguration.ConfigAsker.ConfigFile) + (_clientDatabase.IsModified ? " *" : "")));
		}

		private void _loadGenericTab() {
			try {
				ProjectConfiguration.ConfigAsker.IsAutomaticSaveEnabled = false;

				_holder = new DbHolder();
#if SDE_DEBUG
				CLHelper.WA = "_CPInstantiate database";
#endif
				_holder.Instantiate(_clientDatabase);
#if SDE_DEBUG
				CLHelper.WL = " : _CS_CDms";
#endif
				GdTabs.AddRange(_holder.GetTabs(_mainTabControl));

				foreach (var tab in _clientDatabase.AllTables) {
					var copy = tab.Value;

					if (copy is AbstractDb<int>) {
						AbstractDb<int> db = (AbstractDb<int>)copy;
						db.Table.Commands.CommandIndexChanged += (e, a) => UpdateTabHeader(db);
					}
					else if (copy is AbstractDb<string>) {
						AbstractDb<string> db = (AbstractDb<string>)copy;
						db.Table.Commands.CommandIndexChanged += (e, a) => UpdateTabHeader(db);
					}
				}

				foreach (var tab in GdTabs) {
					var copy = tab;
					copy._listView.SelectionChanged += delegate(object sender, SelectionChangedEventArgs args) {
						if (sender is ListView) {
							ListView view = (ListView)sender;
							_tabEngine.StoreAndExecute(new SelectionChanged(copy.Header.ToString(), view.SelectedItem, view, copy));
						}
					};
				}

				foreach (GDbTab tab in GdTabs) {
					GDbTab tabCopy = tab;
					_mainTabControl.Items.Insert(_mainTabControl.Items.Count, tabCopy);
				}
			}
			finally {
				ProjectConfiguration.ConfigAsker.IsAutomaticSaveEnabled = true;
			}
		}

		public void UpdateTabHeader<TKey>(AbstractDb<TKey> db) {
			Table<TKey, ReadableTuple<TKey>> table = db.Table;

			if (table != null) {
				string header = db.DbSource.DisplayName;

				if (table.Commands.IsModified) {
					header += " *";
				}

				Dispatcher.BeginInvoke(new Action(delegate {
					var gdt = _mainTabControl.Items.OfType<GDbTabWrapper<TKey, ReadableTuple<TKey>>>().FirstOrDefault(p => p.Header.ToString() == db.DbSource.Filename);

					if (gdt != null) {
						((DisplayLabel) gdt.Header).Content = header;
					}
				}));
			}
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) { }

		private void _tabChanged(TabItem oldItem) {
			TabItem item = _mainTabControl.SelectedItem as TabItem;

			if (item == null)
				return;

			if (_delayedReloadDatabase && WpfUtilities.IsTab(oldItem, "Settings")) {
				if (!ReloadDatabase()) {
					_mainTabControl.SelectedIndex = 0;
				}

				return;
			}

			if (WpfUtilities.IsTab(item, "Error console *")) {
				_mainTabControl.Dispatch(p => ((TabItem)_mainTabControl.Items[1]).Header = new DisplayLabel { DisplayText = "Error console", FontWeight = FontWeights.Bold });
			}
		}

		private void _menuItemReloadDatabase_Click(object sender, RoutedEventArgs e) {
			ReloadDatabase();
		}

		private void _mainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (e != null && (e.AddedItems.Count > 0 && e.AddedItems[0] as TabItem != null)) {
				var tabItem = (TabItem) e.AddedItems[0];

				GDbTab tab = tabItem as GDbTab;
				if (tab != null) {
					tab.TabSelected();
				}
			}

			if (e == null || e.RemovedItems.Count <= 0 || e.RemovedItems[0] as TabItem == null || (e.AddedItems.Count > 0 && e.AddedItems[0] as TabItem == null))
				return;

			_tabChanged(e.RemovedItems[0] as TabItem);
		}

		public void Update() {
			_execute(v => v.Update());
		}

		private void _execute(Action<GDbTab> func) {
			if (_mainTabControl.SelectedIndex >= 0 && _mainTabControl.Items[_mainTabControl.SelectedIndex] is GDbTab) {
				GDbTab tab = (GDbTab) _mainTabControl.Items[_mainTabControl.SelectedIndex];

				try {
					func(tab);
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}

		public void SetRange(List<int> selectedIds) {
			_execute(v => v.SetRange(selectedIds));
		}

		private void _menuItemCopyItemTo_Click(object sender, RoutedEventArgs e) {
			_execute(v => v.CopyItemTo());
		}

		private void _menuItemDeleteItem_Click(object sender, RoutedEventArgs e) {
			_execute(v => v.DeleteItems());
		}

		private void _menuItemUndoAll_Click(object sender, RoutedEventArgs e) {
			ReloadDatabase();
		}

		private void _cbAssociate_Checked(object sender, RoutedEventArgs e) {
			SdeAppConfiguration.FileShellAssociated |= FileAssociation.Sde;
			ApplicationManager.AddExtension(Methods.ApplicationFullPath, "Server database editor", ".sde", true);
		}

		private void _cbAssociate_Unchecked(object sender, RoutedEventArgs e) {
			GrfPath.Delete(GrfPath.Combine(SdeAppConfiguration.ProgramDataPath, "sde.ico"));
			ApplicationManager.RemoveExtension(Methods.ApplicationFullPath, ".sde");
		}

		public void SelectItems(List<Tuple> items) {
			_execute(v => v.SelectItems(items));
		}

		private void _tbmUndo_Click(object sender, RoutedEventArgs e) {
			_execute(v => v.Undo());
		}

		private void _tbmRedo_Click(object sender, RoutedEventArgs e) {
			_execute(v => v.Redo());
		}

		private void _tnbUndo_Click(object sender, RoutedEventArgs e) {
			_tabEngine.Undo();
		}

		private void _tnbRedo_Click(object sender, RoutedEventArgs e) {
			_tabEngine.Redo();
		}

		private void _menuItemChangeId_Click(object sender, RoutedEventArgs e) {
			_execute(v => v.ChangeId());
		}

		private void _menuItemBackups_Click(object sender, RoutedEventArgs e) {
			WindowProvider.Show(new BackupDialog(), _menuItemBackups, this);
		}

		private void _menuItemAddTable_Click(object sender2, RoutedEventArgs er) {
			//string file = @"C:\Users\Sylvain\Desktop\SVN\Hercules\Hercules\trunk\db\homun_skill_tree.txt";// PathRequest.OpenFileCde("filter", FileFormat.MergeFilters(Format.Txt));
			string file = PathRequest.OpenFileCde("filter", FileFormat.MergeFilters(Format.Txt));

			if (file == null) return;

			DbMaker maker = new DbMaker(file);

			if (maker.Init(_holder)) {
				maker.Add(_mainTabControl, _holder, _tabEngine, this);
				List<string> files = ProjectConfiguration.CustomTabs;
				files.Add(file);
				ProjectConfiguration.CustomTabs = files;
			}
			else {
				ErrorHandler.HandleException("Failed to parse the file to a database format.");
			}
		}

		private void _miOpen_Click(object sender, RoutedEventArgs e) {
			try {
				DebugItemView view = (DebugItemView)_debugList.SelectedItem;
				Process.Start(view.FilePath);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _miOpenNotepad_Click(object sender, RoutedEventArgs e) {
			try {
				DebugItemView view = (DebugItemView) _debugList.SelectedItem;
				GTabsMaker.SelectInNotepadpp(view.FilePath, view.Line);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemImportFromFile_Click(object sender, RoutedEventArgs e) {
			_execute(v => v.ImportFromFile());
		}
	}
}
