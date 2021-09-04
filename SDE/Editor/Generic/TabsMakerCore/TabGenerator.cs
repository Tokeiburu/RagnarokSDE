using Database;
using ErrorManager;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Generic.Core;
using SDE.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using Utilities.IndexProviders;

namespace SDE.Editor.Generic.TabsMakerCore
{
    public class TabSettings<TKey>
    {
        public SdeDatabase Database { get; set; }
        public TabControl Control { get; set; }
        public BaseDb Gdb { get; set; }
        public DisplayableProperty<TKey, ReadableTuple<TKey>> GeneralProperties { get; set; }
        public GDbTabWrapper<TKey, ReadableTuple<TKey>> Tab { get; set; }
        public Table<TKey, ReadableTuple<TKey>> Table { get; set; }
        public TabGenerator<TKey> TabGenerator { get; set; }
    }

    public class TabGenerator<TKey>
    {
        #region Delegates

        public delegate GDbTab GDbTabMakerDelegate(SdeDatabase database, TabControl control, BaseDb gdb);

        public delegate void GenerateGridDelegate(ref int line, TabSettings<TKey> tabSettings);

        public delegate bool TabEnabledDelegate(GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb);

        public delegate void TabGeneratorDelegate(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb);

        public delegate void TabEvent(TabSettings<TKey> tabSettings);

        #endregion Delegates

        public TabSettings<TKey> Settings { get; set; }

        public TabGenerator()
        {
            GDbTabMaker = _gDbTabMaker;
            SetSettings = _setSettings;
            OnSetCustomCommands = _onSetCustomCommands;
            OnPreviewTabInitialize = _onPreviewTabInitialize;
            OnPreviewGenerateGrid = _onPreviewGenerateGrid;
            GenerateGrid = GenerateGridDefault;
            OnGenerateGrid = _onGenerateGrid;
            OnTabVisualUpdate = TgOnTabVisualUpdate;
            OnPreviewDatabaseReloaded = null;
            OnDatabaseReloaded = null;
            OnTabRefreshed = null;
            IsTabEnabledMethod = IsTabEnabled;
            MaxElementsToCopyInCustomMethods = -1;
            Settings = new TabSettings<TKey>();
            Settings.TabGenerator = this;
        }

        public int MaxElementsToCopyInCustomMethods { get; set; }
        public int StartIndexInCustomMethods { get; set; }
        public int DefaultSpacing { get; set; }

        public GDbTabMakerDelegate GDbTabMaker { get; set; }

        public TabGeneratorDelegate OnInitSettings { get; set; }
        public TabGeneratorDelegate SetSettings { get; set; }
        public TabGeneratorDelegate OnSetCustomCommands { get; set; }
        public TabGeneratorDelegate OnPreviewTabInitialize { get; set; }
        public TabGeneratorDelegate OnAfterTabInitialize { get; set; }

        public GenerateGridDelegate OnPreviewGenerateGrid { get; set; }
        public GenerateGridDelegate GenerateGrid { get; set; }
        public GenerateGridDelegate OnGenerateGrid { get; set; }
        public TabGeneratorDelegate OnPreviewTabVisualUpdate { get; set; }
        public TabGeneratorDelegate OnTabVisualUpdate { get; set; }
        public TabGeneratorDelegate OnPreviewDatabaseReloaded { get; set; }
        public TabGeneratorDelegate OnDatabaseReloaded { get; set; }
        public TabEnabledDelegate IsTabEnabledMethod { get; set; }
        public TabEvent OnTabRefreshed { get; set; }

        public static bool IsTabEnabled(GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb)
        {
            if (DbPathLocator.DetectPath(settings.DbData) == null)
            {
                return false;
            }

            if (!gdb.IsEnabled)
            {
                return false;
            }

            if (!Boolean.Parse(ProjectConfiguration.ConfigAsker["[Server database editor - Enabled state - " + settings.DbData.DisplayName + "]", true.ToString()]))
            {
                return false;
            }

            return true;
        }

        public static void TgOnTabVisualUpdate(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb)
        {
            Exception exception = null;

            bool success = tab.Dispatch(delegate
            {
                try
                {
                    UIElement content = (UIElement)tab.Content; // (UIElement)(tab.Content ?? ((Window)tab.AttachedProperty["AttachedWindow"]).Content);

                    if (gdb.To<TKey>().TabGenerator == null || gdb.To<TKey>().TabGenerator.IsTabEnabledMethod == null)
                        content.IsEnabled = IsTabEnabled(settings, gdb);
                    else
                    {
                        content.IsEnabled = gdb.To<TKey>().TabGenerator.IsTabEnabledMethod(settings, gdb);
                    }
                    return true;
                }
                catch (Exception err)
                {
                    exception = err;
                    return false;
                }
            });

            if (!success)
                throw exception;

            List<DbAttribute> attributes = settings.AttributeList.Attributes;

            if (gdb.LayoutIndexes != null)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.IsSkippable)
                    {
                        bool isSet = _isAttributeEnabled(attribute, gdb);

                        tab.Dispatch(delegate
                        {
                            var elements = DisplayablePropertyHelper.GetAll(tab.PropertiesGrid, attribute.DisplayName);

                            foreach (var element in elements)
                            {
                                element.Visibility = isSet ? Visibility.Visible : Visibility.Collapsed;
                                element.IsEnabled = isSet;
                            }
                        });
                    }
                }
            }
        }

        private static bool _isAttributeEnabled(DbAttribute attribute, BaseDb gdb)
        {
            var attached = gdb.Attached[attribute.DisplayName];
            return attached == null || (bool)gdb.Attached[attribute.DisplayName];
        }

        private GDbTab _gDbTabMaker(SdeDatabase database, TabControl control, BaseDb gdb)
        {
            GTabSettings<TKey, ReadableTuple<TKey>> settings = new GTabSettings<TKey, ReadableTuple<TKey>>(gdb);
            GDbTabWrapper<TKey, ReadableTuple<TKey>> tab = new GDbTabWrapper<TKey, ReadableTuple<TKey>>();
            Table<TKey, ReadableTuple<TKey>> table = gdb.To<TKey>().Table;
            settings.Table = table;
            settings.Control = control;

            Settings.Control = control;
            Settings.Gdb = gdb;
            Settings.Tab = tab;
            Settings.Table = table;
            Settings.Database = database;

            InitStyle(tab, settings, gdb);
            InitAttributes(tab, settings, gdb);
            if (OnInitSettings != null) OnInitSettings(tab, settings, gdb);

            DisplayableProperty<TKey, ReadableTuple<TKey>> generalProperties = new DisplayableProperty<TKey, ReadableTuple<TKey>>();
            generalProperties.Spacing = DefaultSpacing;
            Settings.GeneralProperties = generalProperties;

            SdeEditor.Instance.SelectionChanged += new SdeEditor.SdeSelectionChangedEventHandler((sender, oldTab, newTab) =>
            {
                try
                {
                    TabItem item = newTab;

                    if (gdb.DbSource.AlternativeName != null)
                    {
                        if (WpfUtilities.IsTab(item, gdb.DbSource.Filename) || WpfUtilities.IsTab(item, gdb.DbSource.AlternativeName))
                        {
                            if (generalProperties.OnTabVisible != null) generalProperties.OnTabVisible(this);
                            if (OnPreviewTabVisualUpdate != null) OnPreviewTabVisualUpdate(tab, settings, gdb);
                            if (OnTabVisualUpdate != null) OnTabVisualUpdate(tab, settings, gdb);
                            if (OnTabRefreshed != null) OnTabRefreshed(Settings);
                        }
                    }
                    else
                    {
                        if (WpfUtilities.IsTab(item, gdb.DbSource))
                        {
                            if (generalProperties.OnTabVisible != null) generalProperties.OnTabVisible(this);
                            if (OnPreviewTabVisualUpdate != null) OnPreviewTabVisualUpdate(tab, settings, gdb);
                            if (OnTabVisualUpdate != null) OnTabVisualUpdate(tab, settings, gdb);
                            if (OnTabRefreshed != null) OnTabRefreshed(Settings);
                        }
                    }
                }
                catch (Exception err)
                {
                    ErrorHandler.HandleException(err);
                }
            });

            database.PreviewReloaded += delegate
            {
                if (OnPreviewDatabaseReloaded != null)
                    OnPreviewDatabaseReloaded(tab, settings, gdb);
            };

            database.Reloaded += delegate
            {
                //if (OnPreviewTabVisualUpdate != null) OnPreviewTabVisualUpdate(tab, settings, gdb);
                //if (OnTabVisualUpdate != null) OnTabVisualUpdate(tab, settings, gdb);

                DisplayablePropertyHelper.CheckAttributeRestrictions(tab, settings, gdb);

                if (OnDatabaseReloaded != null)
                    OnDatabaseReloaded(tab, settings, gdb);

                if (OnTabRefreshed != null)
                    OnTabRefreshed(Settings);
            };

            int line = 0;
            if (OnPreviewGenerateGrid != null) OnPreviewGenerateGrid(ref line, Settings);
            if (GenerateGrid != null) GenerateGrid(ref line, Settings);
            if (OnGenerateGrid != null) OnGenerateGrid(ref line, Settings);

            settings.DisplayablePropertyMaker = generalProperties;
            settings.ClientDatabase = database;

            if (SetSettings != null) SetSettings(tab, settings, gdb);
            if (OnSetCustomCommands != null) OnSetCustomCommands(tab, settings, gdb);
            if (OnPreviewTabInitialize != null) OnPreviewTabInitialize(tab, settings, gdb);
            tab.Initialize(settings);
            if (OnAfterTabInitialize != null) OnAfterTabInitialize(tab, settings, gdb);
            return tab;
        }

        private void _onPreviewTabInitialize(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb)
        {
        }

        private void _onSetCustomCommands(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb)
        {
            int max = MaxElementsToCopyInCustomMethods < 0 ? settings.AttributeList.Attributes.Count - StartIndexInCustomMethods : MaxElementsToCopyInCustomMethods;

            settings.AddedCommands.Add(new GItemCommand<TKey, ReadableTuple<TKey>>
            {
                AllowMultipleSelection = true,
                DisplayName = "Copy entries to clipboard",
                ImagePath = "export.png",
                InsertIndex = 3,
                AddToCommandsStack = false,
                Shortcut = ApplicationShortcut.Copy,
                GenericCommand = delegate (List<ReadableTuple<TKey>> items)
                {
                    StringBuilder builder = new StringBuilder();
                    List<int> toRemove =
                        (from attribute in gdb.AttributeList.Attributes.OrderByDescending(p => p.Index)
                         where attribute.IsSkippable && !_isAttributeEnabled(attribute, gdb)
                         select attribute.Index).ToList();

                    for (int i = 0; i < items.Count; i++)
                    {
                        ReadableTuple<TKey> item = items[i];

                        List<string> objs = item.GetRawElements().Skip(StartIndexInCustomMethods).Take(max).Select(p => (p ?? "").ToString()).ToList();

                        foreach (var index in toRemove)
                        {
                            objs.RemoveAt(index);
                        }

                        builder.AppendLine(string.Join(",", objs.ToArray()));
                    }

                    Clipboard.SetDataObject(builder.ToString());
                }
            });
        }

        public static void CopyTuplesDefault(TabGenerator<TKey> tabGenerator, List<ReadableTuple<TKey>> items, BaseDb gdb)
        {
            int max = tabGenerator.MaxElementsToCopyInCustomMethods < 0 ? gdb.AttributeList.Attributes.Count - tabGenerator.StartIndexInCustomMethods : tabGenerator.MaxElementsToCopyInCustomMethods;

            StringBuilder builder = new StringBuilder();
            List<int> toRemove =
                (from attribute in gdb.AttributeList.Attributes.OrderByDescending(p => p.Index)
                 where attribute.IsSkippable && !_isAttributeEnabled(attribute, gdb)
                 select attribute.Index).ToList();

            for (int i = 0; i < items.Count; i++)
            {
                ReadableTuple<TKey> item = items[i];

                List<string> objs = item.GetRawElements().Skip(tabGenerator.StartIndexInCustomMethods).Take(max).Select(p => (p ?? "").ToString()).ToList();

                foreach (var index in toRemove)
                {
                    if (index < objs.Count)
                    {
                        objs.RemoveAt(index);
                    }
                }

                builder.AppendLine(string.Join(",", objs.ToArray()));
            }

            Clipboard.SetDataObject(builder.ToString());
        }

        public static void CopyTuplesDefault(TabGenerator<TKey> tabGenerator, List<ReadableTuple<TKey>> items, DbAttribute[] attributes)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < items.Count; i++)
            {
                ReadableTuple<TKey> item = items[i];
                List<string> objs = new List<string>();

                for (int j = 0; j < attributes.Length; j++)
                {
                    objs.Add((item.GetRawValue(attributes[j].Index) ?? "").ToString());
                }

                builder.AppendLine(string.Join(",", objs.ToArray()));
            }

            Clipboard.SetDataObject(builder.ToString());
        }

        public static void CopyTuples(List<ReadableTuple<TKey>> items, BaseDb gdb)
        {
            StringBuilder builder = new StringBuilder();
            List<int> toRemove =
                (from attribute in gdb.AttributeList.Attributes.OrderByDescending(p => p.Index)
                 where attribute.IsSkippable && !_isAttributeEnabled(attribute, gdb)
                 select attribute.Index).ToList();

            for (int i = 0; i < items.Count; i++)
            {
                ReadableTuple<TKey> item = items[i];

                List<string> objs = item.GetRawElements().Select(p => (p ?? "").ToString()).ToList();

                foreach (var index in toRemove)
                {
                    objs.RemoveAt(index);
                }

                builder.AppendLine(string.Join(",", objs.ToArray()));
            }

            Clipboard.SetDataObject(builder.ToString());
        }

        private void _setSettings(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb)
        {
            List<DbAttribute> attributes;

            if (gdb.LayoutSearch != null)
            {
                attributes = gdb.LayoutSearch.ToList();
            }
            else
            {
                attributes = new DbAttribute[] { settings.AttId, settings.AttDisplay }.Concat(gdb.AttributeList.Attributes.Skip(1).Where(p => p.IsSearchable != null && p != settings.AttId && p != settings.AttDisplay)).ToList();
            }

            if (attributes.Count % 2 != 0)
            {
                attributes.Add(null);
            }

            settings.SearchEngine.SetAttributes(attributes);
            settings.SearchEngine.SetSettings(settings.AttId, true);
            settings.SearchEngine.SetSettings(settings.AttDisplay, true);

            foreach (DbAttribute attribute in attributes)
            {
                if (attribute != null && attribute.IsSearchable == true)
                {
                    settings.SearchEngine.SetSettings(attribute, true);
                }
            }
        }

        private void _onGenerateGrid(ref int line, TabSettings<TKey> settings)
        {
        }

        private void _onPreviewGenerateGrid(ref int line, TabSettings<TKey> settings)
        {
        }

        public void GenerateGridDefault(ref int line, TabSettings<TKey> settings)
        {
            if (settings.Gdb.LayoutIndexes != null)
            {
                AbstractProvider metaProvider = AbstractProvider.GetProvider(settings.Gdb.LayoutIndexes);

                if (metaProvider is GroupIndexProvider)
                {
                    AbstractProvider gridProvider = AbstractProvider.GetProvider(settings.Gdb.GridIndexes);
                    gridProvider.GroupAs = typeof(SpecifiedIndexProvider);
                    bool col = false;

                    foreach (IIndexProvider provider in metaProvider.Providers)
                    {
                        AbstractProvider gridLayout = gridProvider.Next<AbstractProvider>();
                        GTabsMaker.PrintGrid(ref line, (col = !col) ? 0 : 3, 1, 2, provider, gridLayout, settings.GeneralProperties, settings.Gdb.AttributeList);
                        if (col) line--;
                    }
                }
                else
                {
                    GTabsMaker.Print(ref line, metaProvider, settings.GeneralProperties, settings.Gdb.AttributeList);
                }
            }
            else
            {
                GTabsMaker.Print(ref line, new SpecifiedRangeIndexProvider(new int[] { 0, settings.Gdb.AttributeList.Attributes.Count }), settings.GeneralProperties, settings.Gdb.AttributeList);
            }
        }

        public void InitAttributes(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb)
        {
            settings.AttributeList = gdb.AttributeList;
            settings.AttId = gdb.AttributeList.PrimaryAttribute;
            settings.AttDisplay = gdb.AttributeList.Attributes.FirstOrDefault(p => p.IsDisplayAttribute) ?? gdb.AttributeList.Attributes[1];

            if (typeof(TKey) == typeof(string))
            {
                settings.AttIdWidth = 120;
            }
        }

        public void InitStyle(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb)
        {
            GTabsMaker.SInit(tab, settings, gdb);
        }

        public GDbTab GenerateTab(SdeDatabase database, TabControl control, BaseDb baseDb)
        {
            return GDbTabMaker(database, control, baseDb);
        }

        public void Show(bool enable, params DbAttribute[] attributes)
        {
            Settings.Control.Dispatch(delegate
            {
                try
                {
                    foreach (var attribute in attributes)
                    {
                        var uiElements = DisplayablePropertyHelper.GetAll(Settings.Tab.PropertiesGrid, attribute);
                        uiElements.ForEach(p => p.IsEnabled = enable);
                    }
                }
                catch (Exception err)
                {
                    ErrorHandler.HandleException(err);
                }
            });
        }

        public void Show(ServerType serverType, bool? renewal, params DbAttribute[] attributes)
        {
            bool enable = DbPathLocator.GetServerType() == serverType;

            if (renewal == null || renewal.Value == DbPathLocator.GetIsRenewal())
            {
                Show(enable, attributes);
            }
        }
    }
}