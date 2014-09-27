using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Database;
using ErrorManager;
using GRF.Image;
using GRF.Threading;
using SDE.Tools.DatabaseEditor.Engines.DatabaseEngine;
using SDE.Tools.DatabaseEditor.Engines.TabNavigationEngine;
using SDE.Tools.DatabaseEditor.Generic.Core;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using SDE.Tools.DatabaseEditor.Generic.DbWriters;
using SDE.Tools.DatabaseEditor.Generic.IndexProviders;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.UI.CustomControls;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using Utilities.Services;
using ServerItemProperties = SDE.Tools.DatabaseEditor.Generic.Lists.ServerItemAttributes;

namespace SDE.Tools.DatabaseEditor.Generic.TabsMakerCore {
	/// <summary>
	/// Utility class to help generate tabs
	/// </summary>
	public static class GTabsMaker {
		public static readonly GItemCommand<int, ReadableTuple<int>> CopyEntriesToClipboardFunctionInt;
		public static readonly GItemCommand<string, ReadableTuple<string>> CopyEntriesToClipboardFunctionString;
		public static readonly TabGenerator<int>.TabGeneratorDelegate SelectFromMobDb;
		public static readonly TabGenerator<string>.TabGeneratorDelegate SelectFromMobDbString;
		public static readonly TabGenerator<string>.TabGeneratorDelegate SelectFromSkillDbString;
		public static readonly TabGenerator<int>.TabGeneratorDelegate SelectFromItemDb;
		public static readonly TabGenerator<int>.TabGeneratorDelegate SelectFromSkillDb;
		public static readonly TabGenerator<int>.TabGeneratorDelegate SelectFromSkillRequirementsDb;

		static GTabsMaker() {
			CopyEntriesToClipboardFunctionInt = new GItemCommand<int, ReadableTuple<int>> {
				AllowMultipleSelection = true,
				DisplayName = "Copy entries to clipboard",
				ImagePath = "export.png",
				InsertIndex = 3,
				Shortcut = ApplicationShortcut.Copy,
				AddToCommandsStack = false,
				GenericCommand = delegate(List<ReadableTuple<int>> items) {
					StringBuilder builder = new StringBuilder();

					for (int i = 0; i < items.Count; i++) {
						ReadableTuple<int> item = items[i];
						builder.AppendLine(string.Join(",", item.GetRawElements().Select(p => (p ?? "").ToString()).ToArray()));
					}

					Clipboard.SetText(builder.ToString());
				}
			};

			CopyEntriesToClipboardFunctionString = new GItemCommand<string, ReadableTuple<string>> {
				AllowMultipleSelection = true,
				DisplayName = "Copy entries to clipboard",
				ImagePath = "export.png",
				Shortcut = ApplicationShortcut.Copy,
				InsertIndex = 3,
				AddToCommandsStack = false,
				GenericCommand = delegate(List<ReadableTuple<string>> items) {
					StringBuilder builder = new StringBuilder();

					for (int i = 0; i < items.Count; i++) {
						ReadableTuple<string> item = items[i];
						builder.AppendLine(string.Join(",", item.GetRawElements().Select(p => (p ?? "").ToString()).ToArray()));
					}

					Clipboard.SetText(builder.ToString());
				}
			};

			SelectFromMobDb = (tab, settings, gdb) => settings.AddedCommands.Add(GenerateSelectFrom(ServerDbs.Mobs, tab));
			SelectFromItemDb = (tab, settings, gdb) => settings.AddedCommands.Add(GenerateSelectFrom(ServerDbs.Items, tab));
			SelectFromSkillDb = (tab, settings, gdb) => settings.AddedCommands.Add(GenerateSelectFrom(ServerDbs.Skills, tab));
			SelectFromMobDbString = (tab, settings, gdb) => settings.AddedCommands.Add(GenerateSelectFrom(ServerDbs.Mobs, tab, ServerMobSkillAttributes.MobId));
			SelectFromSkillDbString = (tab, settings, gdb) => settings.AddedCommands.Add(GenerateSelectFrom(ServerDbs.Skills, tab, ServerMobSkillAttributes.SkillId));
			SelectFromSkillRequirementsDb = (tab, settings, gdb) => settings.AddedCommands.Add(GenerateSelectFrom(ServerDbs.SkillsRequirement, tab));
		}

		public static GItemCommand<TKey, ReadableTuple<TKey>> GenerateSelectFrom<TKey>(ServerDbs serverDb, GDbTabWrapper<TKey, ReadableTuple<TKey>> tab) {
			return new GItemCommand<TKey, ReadableTuple<TKey>> {
				AllowMultipleSelection = false,
				DisplayName = String.Format("Select in [{0}]", serverDb.DisplayName),
				ImagePath = "arrowdown.png",
				InsertIndex = 4,
				AddToCommandsStack = false,
				GenericCommand = items => TabNavigation.SelectList(serverDb, items.Select(p => p.GetKey<TKey>()))
			};
		}

		public static GItemCommand<TKey, ReadableTuple<TKey>> GenerateSelectFrom<TKey>(ServerDbs serverDb, GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, DbAttribute attribute) {
			return new GItemCommand<TKey, ReadableTuple<TKey>> {
				AllowMultipleSelection = false,
				DisplayName = String.Format("Select in [{0}]", serverDb.DisplayName),
				ImagePath = "arrowdown.png",
				InsertIndex = 4,
				AddToCommandsStack = false,
				GenericCommand = items => TabNavigation.SelectList(serverDb, items.Select(p => p.GetValue<int>(attribute)).Distinct())
			};
		}

		public static void InPairsLabel<T>(DbAttribute att1, DbAttribute att2, ref int line, DisplayableProperty<T, ReadableTuple<T>> generalProperties) {
			if (att1 != null && att1.Visibility == VisibleState.Visible)
				generalProperties.AddLabel(att1, line, 0);

			if (att2 != null && att2.Visibility == VisibleState.Visible)
				generalProperties.AddLabel(att2, line, 3);

			line += 2;
		}
		public static void InPairsPropety<T>(DbAttribute att1, DbAttribute att2, ref int line, DisplayableProperty<T, ReadableTuple<T>> generalProperties) {
			if (att1 != null && att1.Visibility == VisibleState.Visible)
				generalProperties.AddProperty(att1, line, 1);

			if (att2 != null && att2.Visibility == VisibleState.Visible)
				generalProperties.AddProperty(att2, line, 4);

			line += 2;
		}
		public static void Print<TKey>(ref int line, AttributeList list, DisplayableProperty<TKey, ReadableTuple<TKey>> generalProperties, int from, int to) {
			for (int i = from, size = Math.Min(to, list.Attributes.Count); i < size; i += 2) {
				DbAttribute att1 = list.Attributes[i];
				DbAttribute att2 = (i + 1 < size) ? list.Attributes[i + 1] : null;

				InPairsLabel(att1, att2, ref line, generalProperties);
				line -= 2;
				InPairsPropety(att1, att2, ref line, generalProperties);
			}
		}
		public static void Print<TKey, TValue>(ref int line, IIndexProvider provider, DisplayableProperty<TKey, TValue> generalProperties, AttributeList list) where TValue : Tuple {
			int lineOffset = -1;
			List<int> indexes = provider.GetIndexes();

			for (int i = 0; i < indexes.Count; i += 2) {
				if (indexes[i] > -1 && indexes[i] < list.Attributes.Count && list.Attributes[indexes[i]].Visibility == VisibleState.Visible) {
					generalProperties.AddLabel(list.Attributes[indexes[i]], ++lineOffset, 0, false);
					generalProperties.AddProperty(list.Attributes[indexes[i]], lineOffset, 1);
				}

				if (i + 1 < indexes.Count) {
					if (indexes[i + 1] > -1 && indexes[i + 1] < list.Attributes.Count && list.Attributes[indexes[i + 1]].Visibility == VisibleState.Visible) {
						generalProperties.AddLabel(list.Attributes[indexes[i + 1]], lineOffset, 3, false);
						generalProperties.AddProperty(list.Attributes[indexes[i + 1]], lineOffset, 4);
					}
				}

				lineOffset++;
				line += 2;
			}
		}
		public static Grid PrintGrid<TKey, TValue>(ref int line, int col, int rowSpan, int colSpan, IIndexProvider provider, int c0, int c1, int c2, int c3, DisplayableProperty<TKey, TValue> generalProperties, AttributeList list) where TValue : Tuple {
			Grid grid = generalProperties.AddGrid(line, col, rowSpan, colSpan);

			grid.ColumnDefinitions.Add(_getColumnDef(c0));
			grid.ColumnDefinitions.Add(_getColumnDef(c1));
			grid.ColumnDefinitions.Add(_getColumnDef(c2));
			grid.ColumnDefinitions.Add(_getColumnDef(c3));

			int lineOffset = -1;
			List<int> indexes = provider.GetIndexes();
			
			for (int i = 0; i < indexes.Count; i += 2) {
				if (indexes[i] > -1 && indexes[i] < list.Attributes.Count && list.Attributes[indexes[i]].Visibility == VisibleState.Visible) {
					generalProperties.AddLabel(list.Attributes[indexes[i]], ++lineOffset, 0, false, grid);
					generalProperties.AddProperty(list.Attributes[indexes[i]], lineOffset, 1, grid);
				}

				if (i + 1 < indexes.Count) {
					if (indexes[i + 1] > -1 && indexes[i + 1] < list.Attributes.Count && list.Attributes[indexes[i + 1]].Visibility == VisibleState.Visible) {
						generalProperties.AddLabel(list.Attributes[indexes[i + 1]], lineOffset, 2, false, grid);
						generalProperties.AddProperty(list.Attributes[indexes[i + 1]], lineOffset, 3, grid);
					}
				}

				lineOffset++;
			}

			line++;
			return grid;
		}
		public static Grid PrintGrid<TKey, TValue>(ref int line, int col, int rowSpan, int colSpan, IIndexProvider provider, AbstractProvider gridProvider, DisplayableProperty<TKey, TValue> generalProperties, AttributeList list) where TValue : Tuple {
			if (gridProvider is NullIndexProvider && provider is NullIndexProvider) {
				line++;
				return null;
			}

			if (gridProvider is NullIndexProvider) {
				// No grid is being printed, but the provider is not null
				Print(ref line, provider, generalProperties, list);
				return null;
			}

			if (gridProvider == null) {
				return PrintGrid(ref line, col, rowSpan, colSpan, provider, -1, 0, -1, 0, generalProperties, list);
			}

			return PrintGrid(ref line, col, rowSpan, colSpan, provider, gridProvider[0], gridProvider[1], gridProvider[2], gridProvider[3], generalProperties, list);
		}
		public static GDbTab Instantiate<TKey>(GTabSettings<TKey, ReadableTuple<TKey>> settings, AbstractDb<TKey> db) {
			Table<TKey, ReadableTuple<TKey>> table = db.Table;
			GDbTabWrapper<TKey, ReadableTuple<TKey>> tab = new GDbTabWrapper<TKey, ReadableTuple<TKey>>();

			settings.Table = table;
			tab.Initialize(settings);
			return tab;
		}
		private static ColumnDefinition _getColumnDef(int c) {
			if (c == 0)
				return new ColumnDefinition();
			if (c == -1)
				return new ColumnDefinition {Width = new GridLength(-1, GridUnitType.Auto)};
			if (c < 0)
				return new ColumnDefinition {MinWidth = c * -1d, Width = new GridLength(-1, GridUnitType.Auto)};

			return new ColumnDefinition {Width = new GridLength(c)};
		}

		public static void SInit<TKey>(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb) {
			settings.Style = "TabItemStyledLess";
			settings.ContextMenu = new ContextMenu();
			var menuItem = new MenuItem { Header = "Select '" + settings.DbData.Filename.Replace("_", "__") + "' in explorer", Icon = new Image { Source = (BitmapSource) ApplicationManager.PreloadResourceImage("arrowdown.png") } };

			menuItem.Click += delegate {
				if (settings.DbData != null) {
					try {
						string path = AllLoaders.DetectPath(settings.DbData);

						if (path != null)
							OpeningService.FilesOrFolders(path);
						else
							ErrorHandler.HandleException("File not found.");
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				}
				else {
					ErrorHandler.HandleException("File not found.");
				}
			};

			settings.ContextMenu.Items.Add(menuItem);

			settings.Loaded += _loaded;

			if (tab == null || gdb == null)
				return;

			if (gdb.AttributeList.Attributes.Any(p => p.IsSkippable)) {
				foreach (var attributeIntern in gdb.AttributeList.Attributes.Where(p => p.IsSkippable)) {
					var attribute = attributeIntern;
					var menuItemSkippable = new MenuItem {Header = attribute.DisplayName + " [" + attribute.AttributeName + ", " + attribute.Index + "]", Icon = new Image {Source = (BitmapSource) ApplicationManager.PreloadResourceImage("add.png")}};
					menuItemSkippable.IsEnabled = false;
					menuItemSkippable.Click += delegate {
						gdb.Attached["EntireRewrite"] = true;
						gdb.Attached[attribute.DisplayName] = gdb.Attached[attribute.DisplayName] != null && !(bool) gdb.Attached[attribute.DisplayName];
						gdb.To<TKey>().TabGenerator.OnTabVisualUpdate(tab, settings, gdb);
					};
					settings.ContextMenu.Items.Add(menuItemSkippable);
				}

				gdb.Attached.CollectionChanged += delegate {
					int index = 2;

					foreach (var attributeIntern in gdb.AttributeList.Attributes.Where(p => p.IsSkippable)) {
						var attribute = attributeIntern;
						int index1 = index;
						settings.ContextMenu.Dispatch(delegate {
							var menuItemSkippable = (MenuItem) settings.ContextMenu.Items[index1];
							menuItemSkippable.IsEnabled = true;
							bool isSet = gdb.Attached[attribute.DisplayName] == null || (bool) gdb.Attached[attribute.DisplayName];

							menuItemSkippable.Icon = new Image {Source = (BitmapSource) ApplicationManager.PreloadResourceImage(isSet ? "delete.png" : "add.png")};
						});

						index++;
					}
				};
			}
		}

		private static void _loaded<TKey>(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb) {
			string property = "[Server database editor - Enabled state - " + settings.DbData.DisplayName + "]";

			Func<bool> getConfig = () => Boolean.Parse(ProjectConfiguration.ConfigAsker[property, true.ToString()]);
			Action<bool> setConfig = v => ProjectConfiguration.ConfigAsker[property] = v.ToString();
			Func<string> getHeader = () => getConfig() ? "Disable" : "Enable";
			Func<string> getFullHeader = () => String.Format("{0} '{1}'", getHeader(), settings.DbData.Filename.Replace("_", "__"));
			Func<Image> getIcon = () => getConfig() ? new Image {Source = (BitmapSource) ApplicationManager.PreloadResourceImage("error16.png")} : new Image {Source = (BitmapSource) ApplicationManager.PreloadResourceImage("validity.png")};

			var menuItem = new MenuItem {Header = getFullHeader(), Icon = getIcon()};
			menuItem.IsEnabled = false;

			menuItem.Click += delegate {
				if (settings.DbData != null) {
					try {
						setConfig(!getConfig());
						gdb.Attached["IsEnabled"] = getConfig();
						TabGenerator<TKey>.TgOnTabVisualUpdate(tab, settings, gdb);

						menuItem.Dispatch(delegate {
							menuItem.Header = getFullHeader();
							menuItem.Icon = getIcon();
						});
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				}
				else {
					ErrorHandler.HandleException("File not found.");
				}
			};

			tab.Database.Reloaded += delegate {
				menuItem.Dispatch(delegate {
					menuItem.IsEnabled = true;

					if (!getConfig()) {
						gdb.Attached["IsEnabled"] = false;
					}

					menuItem.Header = getFullHeader();
					menuItem.Icon = getIcon();
				});
			};

			settings.ContextMenu.Items.Insert(1, menuItem);
		}

		public static void SInit<TKey>(GTabSettings<TKey, ReadableTuple<TKey>> settings) {
			SInit(null, settings, null);
		}

		// TODO: The TabGenerator class works better than these custom generators
		public static GDbTab LoadSItemsTab<TKey>(GenericDatabase database, TabControl control, BaseDb gdb) {
			AbstractDb<TKey> db = gdb.To<TKey>();
			AttributeList list = ServerItemProperties.AttributeList;

			GDbTabWrapper<TKey, ReadableTuple<TKey>> tab = new GDbTabWrapper<TKey, ReadableTuple<TKey>>();
			GTabSettings<TKey, ReadableTuple<TKey>> settings = new GTabSettings<TKey, ReadableTuple<TKey>>(db);

			SInit(settings);
			settings.AttributeList = list;
			settings.AttId = list.PrimaryAttribute;
			settings.AttDisplay = ServerItemProperties.Name;

			DisplayableProperty<TKey, ReadableTuple<TKey>> generalProperties = new DisplayableProperty<TKey, ReadableTuple<TKey>>();
			generalProperties.Spacing = 0;

			int line = 0;
			
			Print(ref line, new SpecifiedRangeIndexProvider(new int[] {
				ServerItemProperties.Id.Index, 1,
				ServerItemProperties.Type.Index, 1,
				ServerItemProperties.AegisName.Index, 2,
				ServerItemProperties.Buy.Index, 2,
				ServerItemProperties.Weight.Index, 16
			}), generalProperties, list);
			
			generalProperties.AddCustomProperty(new CustomQueryViewerMobDroppedBy<TKey, ReadableTuple<TKey>>(line, 0, 1, 2));
			generalProperties.SetRow(line, new GridLength(1, GridUnitType.Star));

			Grid grid = PrintGrid(ref line, 3, 1, 2, new DefaultIndexProvider(ServerItemProperties.BindOnEquip.Index, 8), -1, 0, -1, 0, generalProperties, list);

			generalProperties.AddDeployAction(delegate {
				grid.Children[0].IsEnabled = false;
				grid.Children[1].IsEnabled = false;
				grid.Children[4].IsEnabled = false;
				grid.Children[5].IsEnabled = false;
			});

			settings.DisplayablePropertyMaker = generalProperties;
			settings.ClientDatabase = database;
			settings.SearchEngine.SetAttributes(
				settings.AttId, settings.AttDisplay,
				ServerItemProperties.AegisName, null,
				ServerItemProperties.ApplicableJob, ServerItemProperties.Script, 
				ServerItemProperties.OnEquipScript, ServerItemProperties.OnUnequipScript,
				ServerItemProperties.Type, ServerItemProperties.Gender
			);

			settings.SearchEngine.SetSettings(ServerItemProperties.Id, true);
			settings.SearchEngine.SetSettings(ServerItemProperties.Name, true);
			settings.SearchEngine.SetSettings(ServerItemProperties.AegisName, true);

			settings.SearchEngine.SetupImageDataGetter = delegate(ReadableTuple<TKey> tuple) {
				tuple.GetImageData = delegate {
					try {
						var cDb = database.GetTable<int>(ServerDbs.ClientResourceDb);

						if (cDb == null)
							return null;

						int id = tuple.GetKey<int>();

						if (!cDb.ContainsKey(id))
							return null;

						byte[] data = database.MetaGrf.GetData(EncodingService.FromAnyToDisplayEncoding(@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\item\" + cDb.GetTuple(id).GetValue<string>(ClientResourceAttributes.ResourceName) + ".bmp"));

						if (data != null) {
							GrfImage gimage = new GrfImage(ref data);
							gimage.MakePinkTransparent();
							return gimage.Cast<BitmapSource>();
						}

						return null;
					}
					catch {
						return null;
					}
				};
			};

			settings.AddedCommands.Add(new GItemCommand<TKey, ReadableTuple<TKey>> {
				AllowMultipleSelection = true,
				DisplayName = "Copy entries to clipboard (rAthena)",
				ImagePath = "export.png",
				InsertIndex = 3,
				Shortcut = ApplicationShortcut.Copy,
				AddToCommandsStack = false,
				GenericCommand = delegate(List<ReadableTuple<TKey>> items) {
					StringBuilder builder = new StringBuilder();
					DbWriterMethods.DbItemsWriterSub(builder, db, items.OrderBy(p => p.GetKey<TKey>()), ServerType.RAthena);
					Clipboard.SetText(builder.ToString());
				}
			});

			settings.AddedCommands.Add(new GItemCommand<TKey, ReadableTuple<TKey>> {
				AllowMultipleSelection = true,
				DisplayName = "Copy entries to clipboard (Hercules)",
				ImagePath = "export.png",
				InsertIndex = 4,
				Shortcut = ApplicationShortcut.Copy2,
				AddToCommandsStack = false,
				GenericCommand = delegate(List<ReadableTuple<TKey>> items) {
					StringBuilder builder = new StringBuilder();
					DbWriterMethods.DbItemsWriterSub(builder, db, items, ServerType.Hercules);
					Clipboard.SetText(builder.ToString(), TextDataFormat.UnicodeText);
				}
			});

			settings.Table = db.Table;
			tab.Initialize(settings);
			AddTagGeneratorTabChangedEvent(control, tab, settings, gdb);
			return tab;
		}
		public static void ItemGroupCopyEntries(List<ReadableTuple<int>> items, BaseDb gdb, TabControl control, ServerType serverType) {
			var parent = WpfUtilities.FindDirectParentControl<SdeEditor>(control);
			parent.AsyncOperation.SetAndRunOperation(new GrfThread(delegate {
				items = items.OrderBy(p => p.GetKey<int>()).ToList();

				StringBuilder builder = new StringBuilder();

				try {
					AProgress.Init(parent);
					DbLoaderErrorHandler.Start();

					var dbItems = gdb.GetMeta<int>(ServerDbs.Items);

					List<string> aegisNames = dbItems.FastItems.Select(p => p.GetStringValue(ServerItemAttributes.AegisName.Index)).ToList();
					List<string> names = dbItems.FastItems.Select(p => p.GetStringValue(ServerItemAttributes.Name.Index)).ToList();

					for (int i = 0; i < items.Count; i++) {
						AProgress.IsCancelling(parent);
						DbWriterMethods.DbItemGroupWriter2(items[i], serverType, builder, gdb, aegisNames, names);
						parent.Progress = (i + 1f) / items.Count * 100f;
					}
				}
				catch (OperationCanceledException) { }
				finally {
					AProgress.Finalize(parent);
					DbLoaderErrorHandler.Stop();
				}

				Clipboard.SetText(builder.ToString());
			}, parent, 200, null, true, true));
		}
		public static void AddTagGeneratorTabChangedEvent<TKey>(TabControl control, GDbTab tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb) {
			control.SelectionChanged += delegate(object sender, SelectionChangedEventArgs e) {
				if (e == null || e.RemovedItems.Count <= 0 || e.RemovedItems[0] as TabItem == null || (e.AddedItems.Count > 0 && e.AddedItems[0] as TabItem == null))
					return;

				if (e.AddedItems.Count <= 0) return;

				TabItem item = e.AddedItems[0] as TabItem;

				if (gdb.DbSource.AlternativeName != null) {
					if (WpfUtilities.IsTab(item, gdb.DbSource.Filename) || WpfUtilities.IsTab(item, gdb.DbSource.AlternativeName)) {
						gdb.To<TKey>().TabGenerator.OnTabVisualUpdate((GDbTabWrapper<TKey, ReadableTuple<TKey>>)tab, settings, gdb);
					}
				}
				else {
					if (WpfUtilities.IsTab(item, gdb.DbSource)) {
						gdb.To<TKey>().TabGenerator.OnTabVisualUpdate((GDbTabWrapper<TKey, ReadableTuple<TKey>>)tab, settings, gdb);
					}
				}
			};
		}

		public static void AdvancedCustomCommands(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
			settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
				AllowMultipleSelection = true,
				DisplayName = "Copy entries to clipboard",
				ImagePath = "export.png",
				InsertIndex = 3,
				AddToCommandsStack = false,
				Shortcut = ApplicationShortcut.Copy,
				GenericCommand = delegate(List<ReadableTuple<int>> items) {
					StringBuilder builder = new StringBuilder();
					var db = gdb.To<int>();

					List<DbAttribute> attributesToRemove = new List<DbAttribute>();
					List<DbAttribute> attributes = new List<DbAttribute>(gdb.AttributeList.Attributes);
					attributes.Reverse();

					foreach (DbAttribute attribute in attributes) {
						if (db.Attached[attribute.DisplayName] != null) {
							bool isLoaded = (bool) db.Attached[attribute.DisplayName];

							if (!isLoaded) {
								attributesToRemove.Add(attribute);
							}
						}
					}

					for (int i = 0; i < items.Count; i++) {
						ReadableTuple<int> item = items[i];
						List<object> rawElements = item.GetRawElements().Skip(db.TabGenerator.StartIndexInCustomMethods).Take(db.TabGenerator.MaxElementsToCopyInCustomMethods).ToList();

						foreach (var attribute in attributesToRemove) {
							rawElements.RemoveAt(attribute.Index);
						}

						builder.AppendLine(string.Join(",", rawElements.Select(p => (p ?? "").ToString()).ToArray()));
					}

					Clipboard.SetText(builder.ToString());
				}
			});
		}

		public static void LoadSItemGroupVisualUpdate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
			List<DbAttribute> attributes = ServerItemGroupSubAttributes.AttributeList.Attributes;
			List<int> indexes = new SpecifiedIndexProvider(new int[] { 2, 3, 4, 5, 6, 7 }).GetIndexes();
			Grid grid = tab._displayGrid.Children.Cast<UIElement>().FirstOrDefault(p => (int)p.GetValue(Grid.RowProperty) == 0 && (int)p.GetValue(Grid.ColumnProperty) == 2) as Grid;

			if (grid == null) return;

			int row = 2;
			int column = 0;

			for (int i = 0; i < indexes.Count; i++) {
				if (indexes[i] > -1 && indexes[i] < attributes.Count) {
					var attribute = attributes[indexes[i]];

					if (attribute.IsSkippable) {
						var attached = gdb.Attached[attribute.DisplayName];
						bool isSet = attached != null && (bool)gdb.Attached[attribute.DisplayName];

						tab.Dispatch(delegate {
							var label = grid.Children.Cast<UIElement>().FirstOrDefault(p => (int)p.GetValue(Grid.RowProperty) == row && (int)p.GetValue(Grid.ColumnProperty) == column);
							var content = grid.Children.Cast<UIElement>().FirstOrDefault(p => (int)p.GetValue(Grid.RowProperty) == row && (int)p.GetValue(Grid.ColumnProperty) == column + 1);

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

				row += 2;
			}
		}

		public static void SelectInNotepadpp(string filePath, string line) {
			Process.Start("notepad++.exe", "\"" + filePath + "\" -n" + line);
		}
	}
}
