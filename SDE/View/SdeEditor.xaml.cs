using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Database;
using ErrorManager;
using GRF.IO;
using GRF.Threading;
using GrfToWpfBridge.Application;
using SDE.ApplicationConfiguration;
using SDE.Core.ViewItems;
using SDE.Editor;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.TabNavigationEngine;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.View.Controls;
using SDE.View.Dialogs;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.CommandLine;
using Utilities.Commands;
using Extensions = SDE.Core.Extensions;

namespace SDE.View {
	/// <summary>
	/// Interaction logic for CDEditor.xaml
	/// </summary>
	public partial class SdeEditor : TkWindow, IProgress, Editor.Generic.Parsers.Generic.IErrorListener {
		public readonly List<GDbTab> GdTabs = new List<GDbTab>();
		internal readonly AsyncOperation _asyncOperation;
		private readonly SdeDatabase _clientDatabase;
		private readonly ObservableCollection<DebugItemView> _debugItems;
		private DbHolder _holder;
		private TabNavigation _tabEngine;
		public static SdeEditor Instance;
		public bool NoErrorsFound { get; set; }

		public SdeDatabase ProjectDatabase {
			get { return _clientDatabase; }
		}

		public SdeEditor() : base("Server database editor", "cde.ico", SizeToContent.Manual, ResizeMode.CanResize) {
			SplashDialog loading = new SplashDialog();
			loading.Show();
			Loaded += delegate {
				loading.Terminate();
			};

			try {
				ApplicationShortcut.OverrideBindings(SdeAppConfiguration.Remapper);
			}
			catch (Exception err) {
				SdeAppConfiguration.Remapper.Clear();
				ApplicationShortcut.OverrideBindings(SdeAppConfiguration.Remapper);
				ErrorHandler.HandleException("Failed to load the custom key bindings. The bindings will be reset to their default values.", err);
			}

			string configFile = _parseCommandLineArguments();
			GrfPath.Delete(ProjectConfiguration.DefaultFileName);

			InitializeComponent();
			Instance = this;
			ShowInTaskbar = true;

			_asyncOperation = new AsyncOperation(_progressBar);
			_clientDatabase = new SdeDatabase(_metaGrf);
			_loadMenu();
			
			if (configFile == null) {
				ProjectConfiguration.ConfigAsker = new ConfigAsker(ProjectConfiguration.DefaultFileName);

				if (SdeAppConfiguration.AlwaysReopenLatestProject) {
					if (_recentFilesManager.Files.Count > 0 && File.Exists(_recentFilesManager.Files[0])) {
						ProjectConfiguration.ConfigAsker = new ConfigAsker(configFile = _recentFilesManager.Files[0]);
					}
				}
			}
			else if (File.Exists(configFile)) {
				ProjectConfiguration.ConfigAsker = new ConfigAsker(configFile);
			}

			_loadSettingsTab();
			if (configFile != null) { ReloadSettings(configFile); }
			_loadGenericTab();

			_clientDatabase.Commands.ModifiedStateChanged += new AbstractCommand<IGenericDbCommand>.AbstractCommandsEventHandler(_commands_ModifiedStateChanged);

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
			ApplicationShortcut.Link(ApplicationShortcut.Replace, () => { if (_menuItemReplaceAll.IsEnabled) _menuItemReplaceAll_Click(this, null); }, this);
			ApplicationShortcut.Link(ApplicationShortcut.FromString("Ctrl-Enter", "Select next entry"), () => _execute(v => v.SelectNext()), this);
			ApplicationShortcut.Link(ApplicationShortcut.FromString("Ctrl-Shift-Enter", "Select previous entry"), () => _execute(v => v.SelectPrevious()), this);
			Configuration.EnableDebuggerTrace = false;
			
			_tnbUndo.SetUndo(_tabEngine);
			_tnbRedo.SetRedo(_tabEngine);
			
			_tmbUndo.SetUndo(_clientDatabase.Commands);
			_tmbRedo.SetRedo(_clientDatabase.Commands);

			Extensions.GenerateListViewTemplate(_debugList, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "#", DisplayExpression = "ErrorNumber", SearchGetAccessor = "ErrorNumber", FixedWidth = 35, ToolTipBinding = "ErrorNumber", TextAlignment = TextAlignment.Right },
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "Exception", FixedWidth = 20, MaxHeight = 24 },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "Exception", DisplayExpression = "Exception", SearchGetAccessor = "Exception", IsFill = true, TextAlignment = TextAlignment.Left, ToolTipBinding="OriginalException", TextWrapping = TextWrapping.Wrap, MinWidth = 120 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Id", DisplayExpression = "Id", SearchGetAccessor = "Id", FixedWidth = 90, TextAlignment = TextAlignment.Left, ToolTipBinding="Id", TextWrapping = TextWrapping.Wrap },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "File", DisplayExpression = "FileName", SearchGetAccessor = "FilePath", FixedWidth = 145, TextAlignment = TextAlignment.Left, ToolTipBinding="FilePath", TextWrapping = TextWrapping.Wrap },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Line", DisplayExpression = "Line", SearchGetAccessor = "Line", FixedWidth = 40, TextAlignment = TextAlignment.Left, ToolTipBinding="Line" },
			}, null, new string[] { "Added", "Blue", "Default", "Black" });

			ApplicationShortcut.Link(ApplicationShortcut.Copy, () => WpfUtils.CopyContent(_debugList), _debugList);

			_debugItems = new ObservableCollection<DebugItemView>();
			_debugList.ItemsSource = _debugItems;

			DbIOErrorHandler.ClearListeners();
			DbIOErrorHandler.AddListener(this);

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
				ServerType serverType = DbPathLocator.GetServerType();
				bool renewal = DbPathLocator.GetIsRenewal();
				string header = String.Format("Current ({0} - {1})", serverType == ServerType.RAthena ? "rA" : "Herc", renewal ? "Renewal" : "Pre-Renewal");

				this.BeginDispatch(delegate {
					_menuItemExportDbCurrent.IsEnabled = true;
					_menuItemExportDbCurrent.Header = header;

					_menuItemExportSqlCurrent.IsEnabled = true;
					_menuItemExportSqlCurrent.Header = header;
				});
			};

			SelectionChanged += _sdeEditor_SelectionChanged;
		}

		public AsyncOperation AsyncOperation {
			get { return _asyncOperation; }
		}

		public List<GDbTab> Tabs {
			get { return GdTabs; }
		}

		#region IErrorListener Members

		public void Handle(Exception err, string exception) {
			Handle(err, exception, ErrorLevel.Warning);
		}

		public void Handle(Exception err, string exception, ErrorLevel errorLevel) {
			Dispatcher.Invoke(new Action(delegate {
				if (_mainTabControl.SelectedIndex != 1 && ((TabItem)_mainTabControl.Items[1]).Header.ToString() != "Error console *")
					((TabItem)_mainTabControl.Items[1]).Header = new DisplayLabel { DisplayText = "Error console *", FontWeight = FontWeights.Bold };

				_debugItems.Add(new DebugItemView(err, _debugItems.Count + 1, exception, errorLevel));
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

		private void _commands_ModifiedStateChanged(object sender, IGenericDbCommand command) {
			_setTitle(Methods.CutFileName(ProjectConfiguration.ConfigAsker.ConfigFile), _clientDatabase.IsModified);
		}

		private void _setTitle(string name, bool isModified) {
			this.BeginDispatch(() => {
				Title = "Server database editor" + (String.IsNullOrEmpty(name) ? "" : " - " + name) + (isModified ? " *" : "");
			});
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
						db.Table.Commands.ModifiedStateChanged += (e, a) => UpdateTabHeader(db);
					}
					else if (copy is AbstractDb<string>) {
						AbstractDb<string> db = (AbstractDb<string>)copy;
						db.Table.Commands.CommandIndexChanged += (e, a) => UpdateTabHeader(db);
						db.Table.Commands.ModifiedStateChanged += (e, a) => UpdateTabHeader(db);
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
				string header = db.DbSource.IsImport ? "imp" : db.DbSource.DisplayName;

				if (table.Commands.IsModified) {
					header += " *";
				}

				this.BeginDispatch(delegate {
					var gdt = _mainTabControl.Items.OfType<GDbTabWrapper<TKey, ReadableTuple<TKey>>>().FirstOrDefault(p => p.Header.ToString() == db.DbSource.Filename);

					if (gdt != null) {
						((DisplayLabel) gdt.Header).Content = header;
					}
				});
			}
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) { }

		public bool DisableSelectionChangedEvents { get; set; }

		private void _sdeEditor_SelectionChanged(object sender, TabItem olditem, TabItem newitem) {
			if (DisableSelectionChangedEvents)
				return;

			if (newitem == null) {
				NoErrorsFound = true;
				return;
			}

			if (newitem == olditem) {
				NoErrorsFound = true;
				return;
			}

			bool isOldErrorConsole = WpfUtilities.IsTab(olditem, "Error console *") || WpfUtilities.IsTab(olditem, "Error console");
			bool isCurrentErrorConsole = WpfUtilities.IsTab(olditem, "Error console *") || WpfUtilities.IsTab(newitem, "Error console");

			if (_delayedReloadDatabase && (WpfUtilities.IsTab(olditem, "Settings") || isOldErrorConsole) &&
				(!isCurrentErrorConsole && !WpfUtilities.IsTab(newitem, "Settings"))) {
				if (!ReloadDatabase()) {
					_mainTabControl.SelectedIndex = 0;
				}

				NoErrorsFound = false;
				return;
			}

			if (WpfUtilities.IsTab(newitem, "Error console *")) {
				_mainTabControl.Dispatch(p => ((TabItem)_mainTabControl.Items[1]).Header = new DisplayLabel { DisplayText = "Error console", FontWeight = FontWeights.Bold });
				NoErrorsFound = false;
				return;
			}

			NoErrorsFound = true;
		}

		private void _mainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (e == null || e.RemovedItems.Count <= 0 || e.RemovedItems[0] as TabItem == null || (e.AddedItems.Count > 0 && e.AddedItems[0] as TabItem == null))
				return;

			OnSelectionChanged(e.RemovedItems[0] as TabItem, _mainTabControl.SelectedItem as TabItem);
		}

		public delegate void SdeSelectionChangedEventHandler(object sender, TabItem oldItem, TabItem newItem);
		public event SdeSelectionChangedEventHandler SelectionChanged;

		public void OnSelectionChanged() {
			SdeSelectionChangedEventHandler handler = SelectionChanged;
			TabItem olditem = _mainTabControl.SelectedItem as TabItem;
			TabItem newitem = _mainTabControl.SelectedItem as TabItem;
			if (handler != null) handler(this, olditem, newitem);
		}

		public void OnSelectionChanged(TabItem olditem, TabItem newitem) {
			SdeSelectionChangedEventHandler handler = SelectionChanged;
			olditem = olditem ?? _mainTabControl.SelectedItem as TabItem;
			newitem = newitem ?? _mainTabControl.SelectedItem as TabItem;
			if (ReferenceEquals(olditem, newitem)) return;
			if (handler != null) handler(this, olditem, newitem);
		}

		public void Update() {
			_execute(v => v.Update());
		}

		public GDbTab FindTopmostTab() {
			var window = WpfUtilities.TopWindow;
			if (window == null) return null;

			GDbTab tab = null;

			if (window.Tag is GDbTab) {
				return window.Tag as GDbTab;
			}

			if ((_mainTabControl.SelectedIndex >= 0 && _mainTabControl.Items[_mainTabControl.SelectedIndex] is GDbTab) || (tab != null)) {
				return (GDbTab)_mainTabControl.Items[_mainTabControl.SelectedIndex];
			}

			return null;
		}

		private void _execute(Action<GDbTab> func) {
			var window = WpfUtilities.TopWindow;
			if (window == null) return;

			GDbTab tab = null;

			if (window.Tag is GDbTab) {
				tab = window.Tag as GDbTab;
			}

			if ((_mainTabControl.SelectedIndex >= 0 && _mainTabControl.Items[_mainTabControl.SelectedIndex] is GDbTab) || (tab != null)) {
				tab = tab ?? (GDbTab)_mainTabControl.Items[_mainTabControl.SelectedIndex];

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

		public void SelectItems(List<Tuple> items) {
			_execute(v => v.SelectItems(items));
		}

		private bool _isClientSyncConvert() {
			return ProjectConfiguration.SynchronizeWithClientDatabases;
		}

	}
}
