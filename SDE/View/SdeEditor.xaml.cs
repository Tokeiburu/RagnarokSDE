using Database;
using ErrorManager;
using GRF.Image;
using GRF.IO;
using GRF.Threading;
using GrfToWpfBridge;
using GrfToWpfBridge.Application;
using SDE.ApplicationConfiguration;
using SDE.Editor;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.TabNavigationEngine;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.Tools.SDEMapcache;
using SDE.View.Controls;
using SDE.View.Dialogs;
using SDE.View.ObjectView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using TokeiLibrary.WpfBugFix;
using Utilities;
using Utilities.CommandLine;
using Utilities.Commands;

namespace SDE.View
{
    /// <summary>
    /// Interaction logic for CDEditor.xaml
    /// </summary>
    public partial class SdeEditor : TkWindow, IProgress, Editor.Generic.Parsers.Generic.IErrorListener
    {
        public readonly List<GDbTab> GdTabs = new List<GDbTab>();
        internal readonly AsyncOperation _asyncOperation;
        private readonly SdeDatabase _clientDatabase;
        private readonly ObservableCollection<DebugItemView> _debugItems;
        private DbHolder _holder;
        private TabNavigation _tabEngine;
        public static SdeEditor Instance;
        public bool NoErrorsFound { get; set; }

        public SdeDatabase ProjectDatabase
        {
            get { return _clientDatabase; }
        }

        public SdeEditor() : base("Server database editor", "cde.ico", SizeToContent.Manual, ResizeMode.CanResize)
        {
            _parseCommandLineArguments(true);

            SplashDialog loading = new SplashDialog();
            loading.Show();
            Loaded += delegate
            {
                loading.Terminate();
            };

            try
            {
                ApplicationShortcut.OverrideBindings(SdeAppConfiguration.Remapper);
            }
            catch (Exception err)
            {
                SdeAppConfiguration.Remapper.Clear();
                ApplicationShortcut.OverrideBindings(SdeAppConfiguration.Remapper);
                ErrorHandler.HandleException("Failed to load the custom key bindings. The bindings will be reset to their default values.", err);
            }

            string configFile = _parseCommandLineArguments();
            GrfPath.Delete(ProjectConfiguration.DefaultFileName);

            if (SdeAppConfiguration.ThemeIndex == 1)
            {
                UIElement.IsEnabledProperty.OverrideMetadata(typeof(RangeListView), new UIPropertyMetadata(true, List_IsEnabledChanged, CoerceIsEnabled));
                UIElement.IsEnabledProperty.OverrideMetadata(typeof(ListView), new UIPropertyMetadata(true, List_IsEnabledChanged, CoerceIsEnabled));
            }

            InitializeComponent();
            Instance = this;
            ShowInTaskbar = true;

            _asyncOperation = new AsyncOperation(_progressBar);
            _clientDatabase = new SdeDatabase(_metaGrf);
            _loadMenu();

            if (configFile == null)
            {
                ProjectConfiguration.ConfigAsker = new ConfigAsker(ProjectConfiguration.DefaultFileName);

                if (SdeAppConfiguration.AlwaysReopenLatestProject)
                {
                    if (_recentFilesManager.Files.Count > 0 && File.Exists(_recentFilesManager.Files[0]))
                    {
                        ProjectConfiguration.ConfigAsker = new ConfigAsker(configFile = _recentFilesManager.Files[0]);
                    }
                }
            }
            else if (File.Exists(configFile))
            {
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

            ListViewDataTemplateHelper.GenerateListViewTemplateNew(_debugList, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
                new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "#", DisplayExpression = "ErrorNumber", SearchGetAccessor = "ErrorNumber", FixedWidth = 35, ToolTipBinding = "ErrorNumber", TextAlignment = TextAlignment.Right },
                new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "Exception", FixedWidth = 20, MaxHeight = 24 },
                new ListViewDataTemplateHelper.RangeColumnInfo { Header = "Exception", DisplayExpression = "Exception", SearchGetAccessor = "Exception", IsFill = true, TextAlignment = TextAlignment.Left, ToolTipBinding="OriginalException", TextWrapping = TextWrapping.Wrap, MinWidth = 120 },
                new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Id", DisplayExpression = "Id", SearchGetAccessor = "Id", FixedWidth = 90, TextAlignment = TextAlignment.Left, ToolTipBinding="Id", TextWrapping = TextWrapping.Wrap },
                new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "File", DisplayExpression = "FileName", SearchGetAccessor = "FilePath", FixedWidth = 145, TextAlignment = TextAlignment.Left, ToolTipBinding="FilePath", TextWrapping = TextWrapping.Wrap },
                new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Line", DisplayExpression = "Line", SearchGetAccessor = "Line", FixedWidth = 40, TextAlignment = TextAlignment.Left, ToolTipBinding="Line" },
            }, null, new string[] { "Default", "{DynamicResource TextForeground}" });

            ApplicationShortcut.Link(ApplicationShortcut.Copy, () => WpfUtils.CopyContent(_debugList), _debugList);

            _debugItems = new ObservableCollection<DebugItemView>();
            _debugList.ItemsSource = _debugItems;

            DbIOErrorHandler.ClearListeners();
            DbIOErrorHandler.AddListener(this);

            _clientDatabase.PreviewReloaded += delegate
            {
                this.BeginDispatch(delegate
                {
                    foreach (TabItem tabItem in _mainTabControl.Items)
                    {
                        tabItem.IsEnabled = true;

                        var tabItemHeader = tabItem.Header as DisplayLabel;

                        if (tabItemHeader != null)
                            tabItemHeader.ResetEnabled();
                    }
                });
            };

            _clientDatabase.Reloaded += delegate
            {
                _mainTabControl.Dispatch(p => p.RaiseEvent(new SelectionChangedEventArgs(Selector.SelectionChangedEvent, new List<object>(), _mainTabControl.SelectedItem == null ? new List<object>() : new List<object> { _mainTabControl.SelectedItem })));
                ServerType serverType = DbPathLocator.GetServerType();
                bool renewal = DbPathLocator.GetIsRenewal();
                string header = String.Format("Current ({0} - {1})", serverType == ServerType.RAthena ? "rA" : "Herc", renewal ? "Renewal" : "Pre-Renewal");

                this.BeginDispatch(delegate
                {
                    _menuItemExportDbCurrent.IsEnabled = true;
                    _menuItemExportDbCurrent.Header = header;

                    _menuItemExportSqlCurrent.IsEnabled = true;
                    _menuItemExportSqlCurrent.Header = header;
                });
            };

            SelectionChanged += _sdeEditor_SelectionChanged;
        }

        private static void List_IsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(d);
            for (int i = 0; i < childrenCount; ++i)
            {
                var child = VisualTreeHelper.GetChild(d, i);
                child.CoerceValue(UIElement.IsEnabledProperty);
            }
        }

        private static object CoerceIsEnabled(DependencyObject d, object basevalue)
        {
            var parent = VisualTreeHelper.GetParent(d) as FrameworkElement;
            if (parent != null && parent.IsEnabled == false)
            {
                if (d.ReadLocalValue(UIElement.IsEnabledProperty) == DependencyProperty.UnsetValue)
                {
                    return true;
                }
            }
            return true;
        }

        //private void watcher_Created(object sender, FileSystemEventArgs e) {
        //	if (e.FullPath.Contains("CETRAINER")) {
        //		File.Copy(e.FullPath, "C:\\test.cetrainer");
        //	}
        //	Console.WriteLine(e.FullPath);
        //}

        public AsyncOperation AsyncOperation
        {
            get { return _asyncOperation; }
        }

        public List<GDbTab> Tabs
        {
            get { return GdTabs; }
        }

        #region IErrorListener Members

        public void Handle(Exception err, string exception)
        {
            Handle(err, exception, ErrorLevel.Warning);
        }

        public void Handle(Exception err, string exception, ErrorLevel errorLevel)
        {
            Dispatcher.Invoke(new Action(delegate
            {
                if (_mainTabControl.SelectedIndex != 1 && ((TabItem)_mainTabControl.Items[1]).Header.ToString() != "Error console *")
                    ((TabItem)_mainTabControl.Items[1]).Header = new DisplayLabel { DisplayText = "Error console *", FontWeight = FontWeights.Bold };

                _debugItems.Add(new DebugItemView(err, _debugItems.Count + 1, exception, errorLevel));
            }), DispatcherPriority.Background);
        }

        #endregion IErrorListener Members

        #region IProgress Members

        public float Progress { get; set; }
        public bool IsCancelling { get; set; }
        public bool IsCancelled { get; set; }

        public void CancelOperation()
        {
            IsCancelling = true;
        }

        #endregion IProgress Members

        private string _parseCommandLineArguments(bool load = false)
        {
            List<GenericCLOption> options = CommandLineParser.GetOptions(Environment.CommandLine, false);

            foreach (GenericCLOption option in options)
            {
                if (!load)
                {
                    if (option.CommandName == "-REM" || option.CommandName == "REM")
                    {
                        break;
                    }
                    if (option.Args.Count <= 0)
                    {
                        continue;
                    }
                    else if (option.Args[0].EndsWith(".sde"))
                    {
                        return options[0].Args[0];
                    }
                }
                else
                {
                    if (option.CommandName == "-mapcache" || option.CommandName == "mapcache")
                    {
                        new MapcacheDialog(option.Args.Count > 0 ? option.Args[0] : null).ShowDialog();
                        ApplicationManager.Shutdown();
                        break;
                    }
                }
            }

            return null;
        }

        private void _commands_ModifiedStateChanged(object sender, IGenericDbCommand command)
        {
            _setTitle(Methods.CutFileName(ProjectConfiguration.ConfigAsker.ConfigFile), _clientDatabase.IsModified);
        }

        private void _setTitle(string name, bool isModified)
        {
            this.BeginDispatch(() =>
            {
                Title = "Server database editor" + (String.IsNullOrEmpty(name) ? "" : " - " + name) + (isModified ? " *" : "");
            });
        }

        private void _loadGenericTab()
        {
            try
            {
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

                foreach (var tab in _clientDatabase.AllTables)
                {
                    var copy = tab.Value;

                    if (copy is AbstractDb<int>)
                    {
                        AbstractDb<int> db = (AbstractDb<int>)copy;
                        db.Table.Commands.CommandIndexChanged += (e, a) => UpdateTabHeader(db);
                        db.Table.Commands.ModifiedStateChanged += (e, a) => UpdateTabHeader(db);
                    }
                    else if (copy is AbstractDb<string>)
                    {
                        AbstractDb<string> db = (AbstractDb<string>)copy;
                        db.Table.Commands.CommandIndexChanged += (e, a) => UpdateTabHeader(db);
                        db.Table.Commands.ModifiedStateChanged += (e, a) => UpdateTabHeader(db);
                    }
                }

                foreach (var tab in GdTabs)
                {
                    var copy = tab;
                    copy._listView.SelectionChanged += delegate (object sender, SelectionChangedEventArgs args)
                    {
                        if (sender is ListView)
                        {
                            ListView view = (ListView)sender;
                            _tabEngine.StoreAndExecute(new SelectionChanged(copy.Header.ToString(), view.SelectedItem, view, copy));
                        }
                    };
                }

                foreach (GDbTab tab in GdTabs)
                {
                    GDbTab tabCopy = tab;
                    _mainTabControl.Items.Insert(_mainTabControl.Items.Count, tabCopy);
                }
            }
            finally
            {
                ProjectConfiguration.ConfigAsker.IsAutomaticSaveEnabled = true;
            }
        }

        public void UpdateTabHeader<TKey>(AbstractDb<TKey> db)
        {
            Table<TKey, ReadableTuple<TKey>> table = db.Table;

            if (table != null)
            {
                string header = db.DbSource.IsImport ? "imp" : db.DbSource.DisplayName;

                if (table.Commands.IsModified)
                {
                    header += " *";
                }

                this.BeginDispatch(delegate
                {
                    var gdt = _mainTabControl.Items.OfType<GDbTabWrapper<TKey, ReadableTuple<TKey>>>().FirstOrDefault(p => p.Header.ToString() == db.DbSource.Filename);

                    if (gdt != null)
                    {
                        ((DisplayLabel)gdt.Header).Content = header;
                    }
                });
            }
        }

        protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e)
        {
        }

        public bool DisableSelectionChangedEvents { get; set; }

        private void _sdeEditor_SelectionChanged(object sender, TabItem olditem, TabItem newitem)
        {
            if (DisableSelectionChangedEvents)
                return;

            if (newitem == null)
            {
                NoErrorsFound = true;
                return;
            }

            if (newitem == olditem)
            {
                NoErrorsFound = true;
                return;
            }

            bool isOldErrorConsole = WpfUtilities.IsTab(olditem, "Error console *") || WpfUtilities.IsTab(olditem, "Error console");
            bool isCurrentErrorConsole = WpfUtilities.IsTab(newitem, "Error console *") || WpfUtilities.IsTab(newitem, "Error console");

            if (_delayedReloadDatabase && (WpfUtilities.IsTab(olditem, "Settings") || isOldErrorConsole) &&
                (!isCurrentErrorConsole && !WpfUtilities.IsTab(newitem, "Settings")))
            {
                if (!ReloadDatabase())
                {
                    _mainTabControl.SelectedIndex = 0;
                }

                NoErrorsFound = false;
                return;
            }

            if (WpfUtilities.IsTab(newitem, "Error console *"))
            {
                _mainTabControl.Dispatch(p => ((TabItem)_mainTabControl.Items[1]).Header = new DisplayLabel { DisplayText = "Error console", FontWeight = FontWeights.Bold });
                NoErrorsFound = false;
                return;
            }

            NoErrorsFound = true;
        }

        private void _mainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e == null || e.RemovedItems.Count <= 0 || e.RemovedItems[0] as TabItem == null || (e.AddedItems.Count > 0 && e.AddedItems[0] as TabItem == null))
                return;

            OnSelectionChanged(e.RemovedItems[0] as TabItem, _mainTabControl.SelectedItem as TabItem);
        }

        public delegate void SdeSelectionChangedEventHandler(object sender, TabItem oldItem, TabItem newItem);

        public event SdeSelectionChangedEventHandler SelectionChanged;

        public void OnSelectionChanged()
        {
            SdeSelectionChangedEventHandler handler = SelectionChanged;
            TabItem olditem = _mainTabControl.SelectedItem as TabItem;
            TabItem newitem = _mainTabControl.SelectedItem as TabItem;
            if (handler != null) handler(this, olditem, newitem);
        }

        public void OnSelectionChanged(TabItem olditem, TabItem newitem)
        {
            SdeSelectionChangedEventHandler handler = SelectionChanged;
            olditem = olditem ?? _mainTabControl.SelectedItem as TabItem;
            newitem = newitem ?? _mainTabControl.SelectedItem as TabItem;
            if (ReferenceEquals(olditem, newitem)) return;
            if (handler != null) handler(this, olditem, newitem);
        }

        public void Update()
        {
            _execute(v => v.Update());
        }

        public GDbTab FindTopmostTab()
        {
            var window = WpfUtilities.TopWindow;
            if (window == null) return null;

            GDbTab tab = null;

            if (window.Tag is GDbTab)
            {
                return window.Tag as GDbTab;
            }

            if ((_mainTabControl.SelectedIndex >= 0 && _mainTabControl.Items[_mainTabControl.SelectedIndex] is GDbTab) || (tab != null))
            {
                return (GDbTab)_mainTabControl.Items[_mainTabControl.SelectedIndex];
            }

            return null;
        }

        private void _execute(Action<GDbTab> func)
        {
            var window = WpfUtilities.TopWindow;
            if (window == null) return;

            GDbTab tab = null;

            if (window.Tag is GDbTab)
            {
                tab = window.Tag as GDbTab;
            }

            if ((_mainTabControl.SelectedIndex >= 0 && _mainTabControl.Items[_mainTabControl.SelectedIndex] is GDbTab) || (tab != null))
            {
                tab = tab ?? (GDbTab)_mainTabControl.Items[_mainTabControl.SelectedIndex];

                try
                {
                    func(tab);
                }
                catch (Exception err)
                {
                    ErrorHandler.HandleException(err);
                }
            }
        }

        public void SetRange(List<int> selectedIds)
        {
            _execute(v => v.SetRange(selectedIds));
        }

        public void SelectItems(List<Database.Tuple> items)
        {
            _execute(v => v.SelectItems(items));
        }

        private bool _isClientSyncConvert()
        {
            return ProjectConfiguration.SynchronizeWithClientDatabases;
        }

        private void _exportImages(string grfpath, int mode)
        {
            _execute(delegate (GDbTab tab)
            {
                if (tab.DbComponent.DbSource != ServerDbs.ClientItems)
                    throw new Exception("This feature can only be used on the Client Items tab.");

                string extractionPath = PathRequest.FolderExtractDb();

                if (extractionPath == null)
                    return;

                var selector = tab._listView.SelectedItems.Count > 0 ? tab._listView.SelectedItems : tab._listView.Items;
                Exception exception = null;

                foreach (var tuple in selector.Cast<ReadableTuple<int>>())
                {
                    var resourceName = tuple.GetValue<string>(ClientItemAttributes.IdentifiedResourceName);
                    var resourcePath = GrfPath.Combine(grfpath, resourceName + ".bmp");
                    var data = ProjectDatabase.MetaGrf.GetData(resourcePath);

                    if (data != null)
                    {
                        try
                        {
                            int id = tuple.Key;

                            GrfImage image = new GrfImage(data);
                            image.MakeFirstPixelTransparent();

                            if (mode == 0)
                            {
                                image.MakePinkTransparent();
                            }

                            image.Convert(GrfImageType.Bgra32);
                            image.Save(GrfPath.Combine(extractionPath, id + ".png"));
                            image.Close();
                        }
                        catch (Exception err)
                        {
                            exception = new Exception("Failed to decompress image for item id: " + tuple.Key, err);
                        }
                    }
                }

                GC.Collect();

                if (exception != null)
                    throw exception;
            });
        }

        private void _menuItemInventoryExport_Click(object sender, RoutedEventArgs e)
        {
            //try {
            //	LuaReader reader = new LuaReader(@"C:\skillinfolist.lub");
            //	StringBuilder output = new StringBuilder();
            //
            //	var dico = reader.ReadAll();
            //	var list = (LuaList)(((LuaKeyValue)dico.Variables[0]).Value);
            //	var dicoSp = new Dictionary<string, string>();
            //	var dicoRange = new Dictionary<string, string>();
            //
            //	foreach (var entry in list.Variables) {
            //		var keyValue = (LuaKeyValue)entry;
            //		var key = keyValue.Key.Trim('[', ']').Replace("SKID.", "");
            //
            //		var skillInfo = (LuaList)keyValue.Value;
            //		dicoSp[key] = "";
            //		dicoRange[key] = "";
            //
            //		foreach (var skillEntryInfo in skillInfo.Variables.OfType<LuaKeyValue>()) {
            //			if (skillEntryInfo.Key == "SpAmount") {
            //				dicoSp[key] = Methods.Aggregate(((LuaList)skillEntryInfo.Value).Variables.OfType<LuaValue>().Select(p => p.Value).ToList(), ":");
            //			}
            //
            //			if (skillEntryInfo.Key == "AttackRange") {
            //				dicoRange[key] = Methods.Aggregate(((LuaList)skillEntryInfo.Value).Variables.OfType<LuaValue>().Select(p => p.Value).ToList(), ":");
            //			}
            //		}
            //	}
            //
            //	var skillRequirementDb = ProjectDatabase.GetTable<int>(ServerDbs.SkillsRequirement);
            //	var skillDb = ProjectDatabase.GetTable<int>(ServerDbs.Skills);
            //
            //	foreach (var entry in skillRequirementDb) {
            //		var spCost = entry.GetValue<string>(ServerSkillRequirementsAttributes.SpCost) ?? "";
            //
            //		int skillId = entry.Key;
            //
            //		var entrySkillDb = skillDb.TryGetTuple(skillId);
            //
            //		if (entrySkillDb == null)
            //			continue;
            //
            //		int maxLevel = entrySkillDb.GetValue<int>(ServerSkillAttributes.MaxLevel);
            //		string skillKeyName = entrySkillDb.GetValue<string>(ServerSkillAttributes.Name).Trim('\t', ' ');
            //
            //		if (!dicoSp.ContainsKey(skillKeyName))
            //			continue;
            //
            //		var valueskRO = dicoSp[skillKeyName].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            //
            //		string[] values = spCost.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            //
            //		if (valueskRO.Length > maxLevel) {
            //			output.AppendLine(skillKeyName + ": Skill level mismatch, rA: " + maxLevel + ", kRO: " + valueskRO.Length);
            //			maxLevel = valueskRO.Length;
            //		}
            //
            //		int[] spCostValuesrA = new int[maxLevel];
            //		int[] spCostValueskRO = new int[maxLevel];
            //
            //		for (int i = 0; i < maxLevel; i++) {
            //			if (i < values.Length) {
            //				spCostValuesrA[i] = Int32.Parse(values[i]);
            //			}
            //			else {
            //				if (i == 0)
            //					spCostValuesrA[i] = 0;
            //				else
            //					spCostValuesrA[i] = spCostValuesrA[i - 1];
            //			}
            //
            //			if (i < valueskRO.Length) {
            //				spCostValueskRO[i] = Int32.Parse(valueskRO[i]);
            //			}
            //			else {
            //				if (i == 0)
            //					spCostValueskRO[i] = 0;
            //				else
            //					spCostValueskRO[i] = spCostValueskRO[i - 1];
            //			}
            //		}
            //
            //		for (int i = 0; i < maxLevel; i++) {
            //			if (spCostValueskRO[i] != spCostValuesrA[i]) {
            //				output.AppendLine(skillKeyName + ": SP cost mismatch, rA:\t" + spCost + ", kRO:\t" + dicoSp[skillKeyName]);
            //				break;
            //			}
            //		}
            //	}
            //
            //	Z.F();
            //	//foreach (var entry in res) {
            //	//	string key = entry.Key;
            //	//	LuaList list = entry.Value;
            //	//}
            //}
            //catch (Exception err) {
            //	ErrorHandler.HandleException(err);
            //}

            _exportImages(@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\item\", 0);
        }

        private void _menuItemIllustrationExport_Click(object sender, RoutedEventArgs e)
        {
            _exportImages(@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\collection\", 1);
        }
    }
}