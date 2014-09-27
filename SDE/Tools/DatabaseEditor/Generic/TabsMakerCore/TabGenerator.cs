using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Database;
using SDE.Tools.DatabaseEditor.Engines.DatabaseEngine;
using SDE.Tools.DatabaseEditor.Generic.Core;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using SDE.Tools.DatabaseEditor.Generic.IndexProviders;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;

namespace SDE.Tools.DatabaseEditor.Generic.TabsMakerCore {
	public class TabGenerator<TKey> {
		#region Delegates

		public delegate GDbTab GDbTabMakerDelegate(GenericDatabase database, TabControl control, BaseDb gdb);

		public delegate void GenerateGridDelegate(ref int line, GenericDatabase database, TabControl control, DisplayableProperty<TKey, ReadableTuple<TKey>> generalProperties, BaseDb gdb);

		public delegate void TabGeneratorDelegate(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb);

		#endregion

		public TabGenerator() {
			GDbTabMaker = _gDbTabMaker;
			SetSettings = _setSettings;
			OnSetCustomCommands = _onSetCustomCommands;
			OnPreviewTabInitialize = _onPreviewTabInitialize;
			OnPreviewGenerateGrid = _onPreviewGenerateGrid;
			GenerateGrid = _generateGrid;
			OnGenerateGrid = _onGenerateGrid;
			OnTabVisualUpdate = TgOnTabVisualUpdate;
			OnDatabaseReloaded = TgOnTabVisualUpdate;
			MaxElementsToCopyInCustomMethods = -1;
		}

		public int MaxElementsToCopyInCustomMethods { get; set; }
		public int StartIndexInCustomMethods { get; set; }
		public int DefaultSpacing { get; set; }

		public GDbTabMakerDelegate GDbTabMaker { get; set; }

		public TabGeneratorDelegate OnInitSettings { get; set; }
		public TabGeneratorDelegate SetSettings { get; set; }
		public TabGeneratorDelegate OnSetCustomCommands { get; set; }
		public TabGeneratorDelegate OnPreviewTabInitialize { get; set; }

		public GenerateGridDelegate OnPreviewGenerateGrid { get; set; }
		public GenerateGridDelegate GenerateGrid { get; set; }
		public GenerateGridDelegate OnGenerateGrid { get; set; }
		public TabGeneratorDelegate OnPreviewTabVisualUpdate { get; set; }
		public TabGeneratorDelegate OnTabVisualUpdate { get; set; }
		public TabGeneratorDelegate OnDatabaseReloaded { get; set; }

		public static bool IsTabEnabled(GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb) {
			if (AllLoaders.DetectPath(settings.DbData) == null) {
				return false;
			}

			if (gdb.Attached["IsEnabled"] != null && !(bool)gdb.Attached["IsEnabled"]) {
				return false;
			}

			if (!Boolean.Parse(SDEConfiguration.ConfigAsker["[Server database editor - Enabled state - " + settings.DbData.DisplayName + "]", true.ToString()])) {
				return false;
			}

			return true;
		}

		public static void TgOnTabVisualUpdate(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb) {
			tab.Dispatch(delegate {
				((UIElement)tab.Content).IsEnabled = IsTabEnabled(settings, gdb);
			});

			List<DbAttribute> attributes = settings.AttributeList.Attributes;

			if (gdb.LayoutIndexes != null) {
				AbstractProvider provider = AbstractProvider.GetProvider(gdb.LayoutIndexes);

				if (provider is GroupIndexProvider) return;

				List<int> indexes = provider.GetIndexes();

				int row = 0;
				int column;

				for (int i = 0; i < indexes.Count; i += 2) {
					column = 0;

					if (indexes[i] > -1 && indexes[i] < attributes.Count) {
						var attribute = attributes[indexes[i]];
						if (attribute.IsSkippable) {
							var attached = gdb.Attached[attribute.DisplayName];
							bool isSet = attached == null || (bool) gdb.Attached[attribute.DisplayName];

							tab.Dispatch(delegate {
								var label = tab._displayGrid.Children.Cast<UIElement>().FirstOrDefault(p => (int) p.GetValue(Grid.RowProperty) == row && (int) p.GetValue(Grid.ColumnProperty) == column);
								var content = tab._displayGrid.Children.Cast<UIElement>().FirstOrDefault(p => (int) p.GetValue(Grid.RowProperty) == row && (int) p.GetValue(Grid.ColumnProperty) == column + 1);

								if (label != null) {
									label.Visibility = isSet ? Visibility.Visible : Visibility.Collapsed;
									label.IsEnabled = isSet;
								}

								if (content != null) {
									content.Visibility = isSet ? Visibility.Visible : Visibility.Collapsed;
									content.IsEnabled = isSet;
								}
							});
						}
					}

					column += 3;

					if (i + 1 < indexes.Count) {
						if (indexes[i + 1] > -1 && indexes[i + 1] < attributes.Count) {
							var attribute = attributes[indexes[i + 1]];
							if (attribute.IsSkippable) {
								var attached = gdb.Attached[attribute.DisplayName];
								bool isSet = attached == null || (bool)gdb.Attached[attribute.DisplayName];

								tab.Dispatch(delegate {
									var label = tab._displayGrid.Children.Cast<UIElement>().FirstOrDefault(p => (int)p.GetValue(Grid.RowProperty) == row && (int)p.GetValue(Grid.ColumnProperty) == column);
									var content = tab._displayGrid.Children.Cast<UIElement>().FirstOrDefault(p => (int)p.GetValue(Grid.RowProperty) == row && (int)p.GetValue(Grid.ColumnProperty) == column + 1);

									if (label != null) {
										label.Visibility = isSet ? Visibility.Visible : Visibility.Collapsed;
										label.IsEnabled = isSet;
									}

									if (content != null) {
										content.Visibility = isSet ? Visibility.Visible : Visibility.Collapsed;
										content.IsEnabled = isSet;
									}
								});
							}
						}
					}

					row += 2;
				}
			}
		}

		private GDbTab _gDbTabMaker(GenericDatabase database, TabControl control, BaseDb gdb) {
			GTabSettings<TKey, ReadableTuple<TKey>> settings = new GTabSettings<TKey, ReadableTuple<TKey>>(gdb);
			GDbTabWrapper<TKey, ReadableTuple<TKey>> tab = new GDbTabWrapper<TKey, ReadableTuple<TKey>>();
			Table<TKey, ReadableTuple<TKey>> table = gdb.To<TKey>().Table;
			settings.Table = table;
			settings.Control = control;

			InitStyle(tab, settings, gdb);
			InitAttributes(tab, settings, gdb);
			if (OnInitSettings != null) OnInitSettings(tab, settings, gdb);

			DisplayableProperty<TKey, ReadableTuple<TKey>> generalProperties = new DisplayableProperty<TKey, ReadableTuple<TKey>>();
			generalProperties.Spacing = DefaultSpacing;

			control.SelectionChanged += delegate(object sender, SelectionChangedEventArgs e) {
				if (e == null || e.RemovedItems.Count <= 0 || e.RemovedItems[0] as TabItem == null || (e.AddedItems.Count > 0 && e.AddedItems[0] as TabItem == null))
					return;

				if (e.AddedItems.Count <= 0) return;

				TabItem item = e.AddedItems[0] as TabItem;

				if (gdb.DbSource.AlternativeName != null) {
					if (WpfUtilities.IsTab(item, gdb.DbSource.Filename) || WpfUtilities.IsTab(item, gdb.DbSource.AlternativeName)) {
						if (generalProperties.OnTabVisible != null) generalProperties.OnTabVisible(this);
						if (OnPreviewTabVisualUpdate != null) OnPreviewTabVisualUpdate(tab, settings, gdb);
						if (OnTabVisualUpdate != null) OnTabVisualUpdate(tab, settings, gdb);
					}
				}
				else {
					if (WpfUtilities.IsTab(item, gdb.DbSource)) {
						if (generalProperties.OnTabVisible != null) generalProperties.OnTabVisible(this);
						if (OnPreviewTabVisualUpdate != null) OnPreviewTabVisualUpdate(tab, settings, gdb);
						if (OnTabVisualUpdate != null) OnTabVisualUpdate(tab, settings, gdb);
					}
				}
			};

			database.Reloaded += delegate {
				if (OnDatabaseReloaded != null)
					OnDatabaseReloaded(tab, settings, gdb);
			};

			int line = 0;
			if (OnPreviewGenerateGrid != null) OnPreviewGenerateGrid(ref line, database, control, generalProperties, gdb);
			if (GenerateGrid != null) GenerateGrid(ref line, database, control, generalProperties, gdb);
			if (OnGenerateGrid != null) OnGenerateGrid(ref line, database, control, generalProperties, gdb);

			settings.DisplayablePropertyMaker = generalProperties;
			settings.ClientDatabase = database;

			if (SetSettings != null) SetSettings(tab, settings, gdb);
			if (OnSetCustomCommands != null) OnSetCustomCommands(tab, settings, gdb);
			if (OnPreviewTabInitialize != null) OnPreviewTabInitialize(tab, settings, gdb);
			tab.Initialize(settings);
			return tab;
		}

		private void _onPreviewTabInitialize(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb) {
		}

		private void _onSetCustomCommands(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb) {
			int max = MaxElementsToCopyInCustomMethods < 0 ? settings.AttributeList.Attributes.Count - StartIndexInCustomMethods : MaxElementsToCopyInCustomMethods;

			settings.AddedCommands.Add(new GItemCommand<TKey, ReadableTuple<TKey>> {
				AllowMultipleSelection = true,
				DisplayName = "Copy entries to clipboard",
				ImagePath = "export.png",
				InsertIndex = 3,
				AddToCommandsStack = false,
				Shortcut = ApplicationShortcut.Copy,
				GenericCommand = delegate(List<ReadableTuple<TKey>> items) {
					StringBuilder builder = new StringBuilder();

					for (int i = 0; i < items.Count; i++) {
						ReadableTuple<TKey> item = items[i];
						builder.AppendLine(string.Join(",", item.GetRawElements().Skip(StartIndexInCustomMethods).Take(max).Select(p => (p ?? "").ToString()).ToArray()));
					}

					Clipboard.SetText(builder.ToString());
				}
			});
		}

		private void _setSettings(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb) {
			List<DbAttribute> attributes;

			if (gdb.LayoutSearch != null) {
				attributes = gdb.LayoutSearch.ToList();
			}
			else {
				attributes = new DbAttribute[] {settings.AttId, settings.AttDisplay}.Concat(gdb.AttributeList.Attributes.Skip(2).Where(p => p.IsSearchable != null)).ToList();
			}

			if (attributes.Count % 2 != 0) {
				attributes.Add(null);
			}

			settings.SearchEngine.SetAttributes(attributes);
			settings.SearchEngine.SetSettings(settings.AttId, true);
			settings.SearchEngine.SetSettings(settings.AttDisplay, true);

			foreach (DbAttribute attribute in attributes) {
				if (attribute != null && attribute.IsSearchable == true) {
					settings.SearchEngine.SetSettings(attribute, true);
				}
			}
		}

		private void _onGenerateGrid(ref int line, GenericDatabase database, TabControl control, DisplayableProperty<TKey, ReadableTuple<TKey>> generalProperties, BaseDb gdb) {
		}

		private void _onPreviewGenerateGrid(ref int line, GenericDatabase database, TabControl control, DisplayableProperty<TKey, ReadableTuple<TKey>> generalProperties, BaseDb gdb) {
		}

		private void _generateGrid(ref int line, GenericDatabase database, TabControl control, DisplayableProperty<TKey, ReadableTuple<TKey>> generalProperties, BaseDb gdb) {
			if (gdb.LayoutIndexes != null) {
				AbstractProvider metaProvider = AbstractProvider.GetProvider(gdb.LayoutIndexes);

				if (metaProvider is GroupIndexProvider) {
					AbstractProvider gridProvider = AbstractProvider.GetProvider(gdb.GridIndexes);
					gridProvider.GroupAs = typeof(SpecifiedIndexProvider);
					bool col = false;

					foreach (IIndexProvider provider in metaProvider.Providers) {
						AbstractProvider gridLayout = gridProvider.Next<AbstractProvider>();
						GTabsMaker.PrintGrid(ref line, (col = !col) ? 0 : 3, 1, 2, provider, gridLayout, generalProperties, gdb.AttributeList);
						if (col) line--;
					}
				}
				else {
					GTabsMaker.Print(ref line, metaProvider, generalProperties, gdb.AttributeList);
				}
			}
			else {
				GTabsMaker.Print(ref line, new SpecifiedRangeIndexProvider(new int[] { 0, gdb.AttributeList.Attributes.Count }), generalProperties, gdb.AttributeList);
			}
		}

		public void InitAttributes(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb) {
			settings.AttributeList = gdb.AttributeList;
			settings.AttId = gdb.AttributeList.PrimaryAttribute;
			settings.AttDisplay = gdb.AttributeList.Attributes.FirstOrDefault(p => p.IsDisplayAttribute) ?? gdb.AttributeList.Attributes[1];

			if (typeof(TKey) == typeof(string)) {
				settings.AttIdWidth = 120;
			}
		}

		public void InitStyle(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb) {
			GTabsMaker.SInit(tab, settings, gdb);
		}

		public GDbTab GenerateTab(GenericDatabase database, TabControl control, BaseDb baseDb) {
#if SDE_DEBUG
			CLHelper.WA = "_CPTAB loading " + baseDb.DbSource.Filename;
			var tab = GDbTabMaker(database, control, baseDb);
			CLHelper.WL = ", generating time : _CS_CDms";
			return tab;
#else
			return GDbTabMaker(database, control, baseDb);
#endif
		}
	}
}