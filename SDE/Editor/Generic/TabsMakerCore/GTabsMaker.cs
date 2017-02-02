using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Database;
using ErrorManager;
using GRF.Image;
using GRF.Threading;
using ICSharpCode.AvalonEdit;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.TabNavigationEngine;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.Editor.Generic.UI.CustomControls;
using SDE.Editor.Items;
using SDE.View;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Extension;
using Utilities.IndexProviders;
using Utilities.Services;

namespace SDE.Editor.Generic.TabsMakerCore {
	/// <summary>
	/// Utility class to help generate tabs
	/// </summary>
	public static class GTabsMaker {
		public const int MinOptions = 3;
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

					Clipboard.SetDataObject(builder.ToString());
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
						Tuple item = items[i];
						builder.AppendLine(string.Join(",", item.GetRawElements().Select(p => (p ?? "").ToString()).ToArray()));
					}

					Clipboard.SetDataObject(builder.ToString());
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
			if (att1 != null && (att1.Visibility & VisibleState.VisibleAndForceShow) != 0)
				generalProperties.AddLabel(att1, line, 0);

			if (att2 != null && (att2.Visibility & VisibleState.VisibleAndForceShow) != 0)
				generalProperties.AddLabel(att2, line, 3);

			line += 2;
		}

		public static void InPairsPropety<T>(DbAttribute att1, DbAttribute att2, ref int line, DisplayableProperty<T, ReadableTuple<T>> generalProperties) {
			if (att1 != null && (att1.Visibility & VisibleState.VisibleAndForceShow) != 0)
				generalProperties.AddProperty(att1, line, 1);

			if (att2 != null && (att2.Visibility & VisibleState.VisibleAndForceShow) != 0)
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
				if (indexes[i] > -1 && indexes[i] < list.Attributes.Count && (list.Attributes[indexes[i]].Visibility & VisibleState.VisibleAndForceShow) != 0) {
					generalProperties.AddLabel(list.Attributes[indexes[i]], ++lineOffset, 0, false);
					generalProperties.AddProperty(list.Attributes[indexes[i]], lineOffset, 1);
				}

				if (i + 1 < indexes.Count) {
					if (indexes[i + 1] > -1 && indexes[i + 1] < list.Attributes.Count && (list.Attributes[indexes[i + 1]].Visibility & VisibleState.VisibleAndForceShow) != 0) {
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
				if (indexes[i] > -1 && indexes[i] < list.Attributes.Count && (list.Attributes[indexes[i]].Visibility & VisibleState.VisibleAndForceShow) != 0) {
					generalProperties.AddLabel(list.Attributes[indexes[i]], ++lineOffset, 0, false, grid);
					generalProperties.AddProperty(list.Attributes[indexes[i]], lineOffset, 1, grid);
				}

				if (i + 1 < indexes.Count) {
					if (indexes[i + 1] > -1 && indexes[i + 1] < list.Attributes.Count && (list.Attributes[indexes[i + 1]].Visibility & VisibleState.VisibleAndForceShow) != 0) {
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
				return new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) };
			if (c < 0)
				return new ColumnDefinition { MinWidth = c * -1d, Width = new GridLength(-1, GridUnitType.Auto) };

			return new ColumnDefinition { Width = new GridLength(c) };
		}

		public static void SInit<TKey>(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb) {
			//settings.Style = "TabItemStyledLess";

			if (settings.DbData.IsImport) {
				settings.Style = "TabItemStyledLess2";
			}
			else {
				if (settings.DbData.AdditionalTable != null && settings.DbData.AdditionalTable.IsImport)
					settings.Style = "TabItemStyledLess1";
				else
					settings.Style = "TabItemStyledLess";
			}

			settings.ContextMenu = new ContextMenu();
			var menuItem = new MenuItem { Header = "Select '" + settings.DbData.Filename.Replace("_", "__") + "' in explorer", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("arrowdown.png") } };

			menuItem.Click += delegate {
				if (settings.DbData != null) {
					try {
						string path = DbPathLocator.DetectPath(settings.DbData);

						if (path != null) {
							if (FtpHelper.IsSystemFile(path))
								OpeningService.FilesOrFolders(path);
							else
								ErrorHandler.HandleException("The file cannot be opened because it is not stored locally.");
						}
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

			tab.Visibility = settings.Visibility;

			if (gdb.AttributeList.Attributes.Any(p => p.IsSkippable)) {
				foreach (var attributeIntern in gdb.AttributeList.Attributes.Where(p => p.IsSkippable)) {
					var attribute = attributeIntern;
					var menuItemSkippable = new MenuItem { Header = attribute.DisplayName + " [" + attribute.AttributeName + ", " + attribute.Index + "]", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("add.png") } };
					menuItemSkippable.IsEnabled = false;
					menuItemSkippable.Click += delegate {
						gdb.Attached["EntireRewrite"] = true;
						gdb.Attached[attribute.DisplayName] = gdb.Attached[attribute.DisplayName] != null && !(bool)gdb.Attached[attribute.DisplayName];
						gdb.To<TKey>().TabGenerator.OnTabVisualUpdate(tab, settings, gdb);
					};
					settings.ContextMenu.Items.Add(menuItemSkippable);
				}

				gdb.Attached.CollectionChanged += delegate {
					int index = MinOptions;

					foreach (var attributeIntern in gdb.AttributeList.Attributes.Where(p => p.IsSkippable)) {
						var attribute = attributeIntern;
						int index1 = index;
						settings.ContextMenu.Dispatch(delegate {
							var menuItemSkippable = (MenuItem)settings.ContextMenu.Items[index1];
							menuItemSkippable.IsEnabled = true;
							bool isSet = gdb.Attached[attribute.DisplayName] == null || (bool)gdb.Attached[attribute.DisplayName];

							menuItemSkippable.Icon = new Image { Source = ApplicationManager.PreloadResourceImage(isSet ? "delete.png" : "add.png") };
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
			Func<Image> getIcon = () => getConfig() ? new Image { Source = ApplicationManager.PreloadResourceImage("error16.png") } : new Image { Source = ApplicationManager.PreloadResourceImage("validity.png") };

			var menuItem = new MenuItem { Header = getFullHeader(), Icon = getIcon() };
			menuItem.IsEnabled = false;

			menuItem.Click += delegate {
				if (settings.DbData != null) {
					try {
						setConfig(!getConfig());
						gdb.IsEnabled = getConfig();
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

			tab.ProjectDatabase.Reloaded += delegate {
				menuItem.Dispatch(delegate {
					menuItem.IsEnabled = true;

					if (!getConfig()) {
						gdb.IsEnabled = false;
					}

					menuItem.Header = getFullHeader();
					menuItem.Icon = getIcon();
				});
			};

			settings.ContextMenu.Items.Insert(1, menuItem);

			var menuItem2 = new MenuItem { Header = "Detach", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("convert.png") } };
			menuItem2.Click += delegate {
				try {
					if (menuItem2.Header.ToString() != "Detach") {
						menuItem2.Header = "Detach";
						((TkWindow)tab.AttachedProperty["AttachedWindow"]).Close();
						tab.AttachedProperty["AttachedWindow"] = null;
						return;
					}

					menuItem2.Header = "Reattach";

					TkWindow window = new TkWindow(gdb.DbSource.DisplayName, "properties.png", SizeToContent.Manual, ResizeMode.CanResize);
					window.Tag = tab;
					window.ShowInTaskbar = true;
					//window.Owner = WpfUtilities.TopWindow;

					tab.AttachedProperty["AttachedWindow"] = window;

					window.Content = tab.Content;
					window.KeyDown += delegate(object sender, KeyEventArgs e) { tab.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(tab), 0, e.Key) { RoutedEvent = UIElement.KeyDownEvent }); };

					var sde = WpfUtilities.FindParentControl<SdeEditor>(tab);

					EventHandler handler = delegate {
						var oldTab = sde._mainTabControl.SelectedItem;
						sde.OnSelectionChanged(tab, null);
						var tab2 = sde.FindTopmostTab();
						if (tab2 != null) {
							if (oldTab != tab2)
								tab2.TabChanged();
						}
					};
					sde.Activated += handler;

					window.Activated += delegate {
						sde.OnSelectionChanged(null, tab);

						if (sde.NoErrorsFound) {
							//sde.DisableSelectionChangedEvents = true;
							tab.TabChanged();
							//sde.DisableSelectionChangedEvents = false;
						}
						else {
							if (sde.FindTopmostTab() == tab) {
								return;
							}

							sde.Activate();
						}
					};

					tab.Content = null;
					window.Closed += delegate {
						tab.Content = ((TkWindow)tab.AttachedProperty["AttachedWindow"]).Content;
						tab.AttachedProperty["AttachedWindow"] = null;
						menuItem2.Header = "Detach";
						sde.Activated -= handler;
					};
					window.Show();
					window.Activate();
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			};

			settings.ContextMenu.Items.Insert(2, menuItem2);
		}

		public static void SInit<TKey>(GTabSettings<TKey, ReadableTuple<TKey>> settings) {
			SInit(null, settings, null);
		}

		public static GDbTab LoadCItemsTab<TKey>(SdeDatabase database, TabControl control, BaseDb gdb) {
			AbstractDb<TKey> db = gdb.To<TKey>();
			AttributeList list = db.AttributeList;

			GDbTabWrapper<TKey, ReadableTuple<TKey>> tab = new GDbTabWrapper<TKey, ReadableTuple<TKey>>();
			GTabSettings<TKey, ReadableTuple<TKey>> settings = new GTabSettings<TKey, ReadableTuple<TKey>>(db);

			SInit(settings);
			settings.AttributeList = list;
			settings.AttId = list.PrimaryAttribute;
			settings.AttDisplay = ClientItemAttributes.IdentifiedDisplayName;

			DisplayableProperty<TKey, ReadableTuple<TKey>> generalProperties = new DisplayableProperty<TKey, ReadableTuple<TKey>>();
			generalProperties.Spacing = 0;

			int line = 0;

			Print(ref line, new SpecifiedRangeIndexProvider(new int[] {
				ClientItemAttributes.Id.Index, 1,
				ClientItemAttributes.IsCard.Index, 1,
				ClientItemAttributes.ClassNumber.Index, 1,
				ClientItemAttributes.Illustration.Index, 1,
				ClientItemAttributes.NumberOfSlots.Index, 1,
				ClientItemAttributes.Affix.Index, 1,
				-1, 1,
				ClientItemAttributes.Postfix.Index, 1,
			}), generalProperties, list);

			line = 10;
			generalProperties.AddLabel("Identified", line, 0, true);
			generalProperties.AddLabel("Unidentified", line, 3, true);

			line += 2;
			generalProperties.AddLabel("Resource name", line, 0);
			generalProperties.AddLabel("Resource name", line, 3);

			line += 2;
			generalProperties.AddLabel("Display name", line, 0);
			generalProperties.AddLabel("Display name", line, 3);

			generalProperties.SetRow(line + 2, new GridLength(1, GridUnitType.Star));

			generalProperties.AddProperty(ClientItemAttributes.IdentifiedDisplayName, line, 1);
			generalProperties.AddProperty(ClientItemAttributes.UnidentifiedDisplayName, line, 4);

			generalProperties.AddDeployAction(delegate(Grid obj) {
				var checkBox = obj.Children.OfType<CheckBox>().FirstOrDefault();

				if (checkBox != null) {
					checkBox.Checked += delegate {
						obj.Children.OfType<UIElement>().Where(p => (int)p.GetValue(Grid.RowProperty) < 10 && (int)p.GetValue(Grid.RowProperty) > 0 && (int)p.GetValue(Grid.ColumnProperty) == 4)
							.ToList().ForEach(p => p.IsEnabled = true);
					};

					checkBox.Unchecked += delegate {
						obj.Children.OfType<UIElement>().Where(p => (int)p.GetValue(Grid.RowProperty) < 10 && (int)p.GetValue(Grid.RowProperty) > 0 && (int)p.GetValue(Grid.ColumnProperty) == 4)
							.ToList().ForEach(p => p.IsEnabled = false);
					};

					obj.Children.OfType<UIElement>().Where(p => (int)p.GetValue(Grid.RowProperty) < 10 && (int)p.GetValue(Grid.RowProperty) > 0 && (int)p.GetValue(Grid.ColumnProperty) == 4)
						.ToList().ForEach(p => p.IsEnabled = false);
				}
			});

			PreviewItemInGame idPvig = new PreviewItemInGame(generalProperties.GetComponent<TextBox>(line, 1)) { HorizontalAlignment = HorizontalAlignment.Center };
			idPvig.SetValue(Grid.RowProperty, line + 4);
			idPvig.SetValue(Grid.ColumnProperty, 0);
			idPvig.SetValue(Grid.ColumnSpanProperty, 2);
			generalProperties.AddElement(idPvig);
			CustomResourceProperty<TKey, ReadableTuple<TKey>> idResourceProp = new CustomResourceProperty<TKey, ReadableTuple<TKey>>(idPvig.PreviewImage, @"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\item", @"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\collection", ".bmp", line - 2, 1, ClientItemAttributes.IdentifiedResourceName);
			generalProperties.AddCustomProperty(idResourceProp);

			PreviewItemInGame unPvig = new PreviewItemInGame(generalProperties.GetComponent<TextBox>(line, 4)) { HorizontalAlignment = HorizontalAlignment.Center };
			unPvig.SetValue(Grid.RowProperty, line + 4);
			unPvig.SetValue(Grid.ColumnProperty, 3);
			unPvig.SetValue(Grid.ColumnSpanProperty, 2);
			generalProperties.AddElement(unPvig);
			CustomResourceProperty<TKey, ReadableTuple<TKey>> unResourceProp = new CustomResourceProperty<TKey, ReadableTuple<TKey>>(unPvig.PreviewImage, @"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\item", @"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\collection", ".bmp", line - 2, 4, ClientItemAttributes.UnidentifiedResourceName);
			generalProperties.AddCustomProperty(unResourceProp);

			TextEditor tbIdDesc = new TextEditor();
			TextEditor tbUnDesc = new TextEditor();

			generalProperties.AddCustomProperty(new CustomDescriptionProperty<TKey, ReadableTuple<TKey>>(
				line + 2, 0, idPvig.PreviewDescription, ClientItemAttributes.IdentifiedDescription, true,
				tbIdDesc, tbUnDesc
				));

			generalProperties.AddCustomProperty(new CustomDescriptionProperty<TKey, ReadableTuple<TKey>>(
				line + 2, 3, unPvig.PreviewDescription, ClientItemAttributes.UnidentifiedDescription, false,
				tbIdDesc, tbUnDesc
				));

			settings.DisplayablePropertyMaker = generalProperties;
			settings.ClientDatabase = database;
			settings.SearchEngine.SetAttributes(
				ClientItemAttributes.Id, null,
				ClientItemAttributes.IdentifiedDisplayName, ClientItemAttributes.UnidentifiedDisplayName,
				ClientItemAttributes.IdentifiedResourceName, ClientItemAttributes.UnidentifiedResourceName,
				ClientItemAttributes.IdentifiedDescription, ClientItemAttributes.UnidentifiedDescription);
			settings.SearchEngine.SetSettings(ClientItemAttributes.Id, true);
			settings.SearchEngine.SetSettings(ClientItemAttributes.IdentifiedDisplayName, true);

			// Delayed items!
			AbstractDb<int> itemDb1 = null;
			AbstractDb<int> itemDb2 = null;
			AbstractDb<int> petDb1 = null;
			AbstractDb<int> petDb2 = null;
			AbstractDb<int> mobDb1 = null;
			AbstractDb<int> mobDb2 = null;
			//AbstractDb<int> citemDb = null;

			ItemGeneratorEngine itemGen = new ItemGeneratorEngine();

			settings.AddedCommands.Add(new GItemCommand<TKey, ReadableTuple<TKey>> {
				AllowMultipleSelection = true,
				DisplayName = "Copy entries to clipboard (conf)",
				ImagePath = "export.png",
				InsertIndex = 4,
				Shortcut = ApplicationShortcut.Copy2,
				AddToCommandsStack = false,
				GenericCommand = delegate(List<ReadableTuple<TKey>> items) {
					try {
						StringBuilder builder = new StringBuilder();
						foreach (var item in items) {
							DbIOClientItems.WriteEntry(builder, (ReadableTuple<int>)(object)item);
						}
						Clipboard.SetDataObject(builder.ToString());
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				}
			});

			settings.AddedCommands.Add(new GItemCommand<TKey, ReadableTuple<TKey>> {
				AllowMultipleSelection = true,
				DisplayName = "Autocomplete (from Server data)",
				ImagePath = "imconvert.png",
				InsertIndex = 5,
				Shortcut = ApplicationShortcut.FromString("Ctrl-G", "Autocomplete"),
				AddToCommandsStack = true,
				Command = delegate(ReadableTuple<TKey> item) {
					try {
						itemDb1 = tab.GetDb<int>(ServerDbs.Items);
						itemDb2 = tab.GetDb<int>(ServerDbs.Items2);
						petDb1 = tab.GetDb<int>(ServerDbs.Pet);
						petDb2 = tab.GetDb<int>(ServerDbs.Pet2);
						mobDb1 = tab.GetDb<int>(ServerDbs.Mobs);
						mobDb2 = tab.GetDb<int>(ServerDbs.Mobs2);

						int id = item.GetKey<int>();

						ReadableTuple<int> tupleSource = itemDb2.Table.TryGetTuple(id) ?? itemDb1.Table.TryGetTuple(id);

						if (tupleSource != null) {
							return itemGen.Generate(item, tupleSource, mobDb1, mobDb2, petDb1, petDb2);
						}

						return null;
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
						return null;
					}
				}
			});

			var select = GenerateSelectFrom(ServerDbs.Items, tab);
			select.Shortcut = ApplicationShortcut.Select;
			settings.AddedCommands.Add(select);

			settings.SearchEngine.SetupImageDataGetter = delegate(ReadableTuple<TKey> tuple) {
				tuple.GetImageData = delegate {
					try {
						byte[] data = database.MetaGrf.GetData(EncodingService.FromAnyToDisplayEncoding(@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\item\" + tuple.GetValue<string>(ClientItemAttributes.IdentifiedResourceName.Index).ExpandString() + ".bmp"));

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

			settings.Table = db.Table;
			tab.Initialize(settings);
			AddTagGeneratorTabChangedEvent(control, tab, settings, gdb);
			return tab;
		}

		// TODO: The TabGenerator class works better than these custom generators
		public static GDbTab LoadSItemsTab<TKey>(SdeDatabase database, TabControl control, BaseDb gdb) {
			AbstractDb<TKey> db = gdb.To<TKey>();
			AttributeList list = ServerItemAttributes.AttributeList;

			GDbTabWrapper<TKey, ReadableTuple<TKey>> tab = new GDbTabWrapper<TKey, ReadableTuple<TKey>>();
			GTabSettings<TKey, ReadableTuple<TKey>> settings = new GTabSettings<TKey, ReadableTuple<TKey>>(db);

			SInit(settings);
			settings.AttributeList = list;
			settings.AttId = list.PrimaryAttribute;
			settings.AttDisplay = ServerItemAttributes.Name;

			DisplayableProperty<TKey, ReadableTuple<TKey>> generalProperties = new DisplayableProperty<TKey, ReadableTuple<TKey>>();
			generalProperties.Spacing = 0;

			int line = 0;

			Print(ref line, new SpecifiedRangeIndexProvider(new int[] {
				ServerItemAttributes.Id.Index, 1,
				ServerItemAttributes.Type.Index, 1,
				ServerItemAttributes.AegisName.Index, 2,
				ServerItemAttributes.Buy.Index, 2,
				ServerItemAttributes.Weight.Index, 16
			}), generalProperties, list);

			generalProperties.AddCustomProperty(new QueryItemDroppedBy<TKey, ReadableTuple<TKey>>(line, 0, 2, 2));

			Grid grid = PrintGrid(ref line, 3, 1, 2, new SpecifiedIndexProvider(new[] {
				ServerItemAttributes.TradeFlag.Index, ServerItemAttributes.NoUseFlag.Index,
				ServerItemAttributes.Stack.Index, ServerItemAttributes.Sprite.Index,
				ServerItemAttributes.Delay.Index, -1,
			}), -1, 0, -1, 0, generalProperties, list);

			generalProperties.SetRow(line, new GridLength(1, GridUnitType.Star));

			Grid gridCheckoxes = PrintGrid(ref line, 3, 1, 2, new SpecifiedIndexProvider(new[] {
				ServerItemAttributes.KeepAfterUse.Index, -1,
				ServerItemAttributes.BindOnEquip.Index, -1,
				ServerItemAttributes.BuyingStore.Index, -1,
				ServerItemAttributes.ForceSerial.Index, -1
			}), -1, 0, 0, 0, generalProperties, list);

			database.Reloaded += delegate {
				bool herc = DbPathLocator.GetServerType() == ServerType.Hercules;
				bool isRenewal = DbPathLocator.GetIsRenewal();

				grid.Dispatch(delegate {
					try {
						var bindOnEquip = DisplayablePropertyHelper.GetAll(grid, ServerItemAttributes.BindOnEquip);
						var matk = DisplayablePropertyHelper.GetAll(grid, ServerItemAttributes.Matk);
						var keepAfterUse = DisplayablePropertyHelper.GetAll(gridCheckoxes, ServerItemAttributes.KeepAfterUse);
						var forceSerial = DisplayablePropertyHelper.GetAll(gridCheckoxes, ServerItemAttributes.ForceSerial);

						bindOnEquip.ForEach(p => p.IsEnabled = false);
						matk.ForEach(p => p.IsEnabled = false);
						keepAfterUse.ForEach(p => p.IsEnabled = false);
						forceSerial.ForEach(p => p.IsEnabled = false);

						if (herc) {
							bindOnEquip.ForEach(p => p.IsEnabled = true);
							keepAfterUse.ForEach(p => p.IsEnabled = true);
							forceSerial.ForEach(p => p.IsEnabled = true);
						}

						if (isRenewal) {
							matk.ForEach(p => p.IsEnabled = true);
						}
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				});
			};

			//generalProperties.AddDeployAction(delegate {
			//	grid.Children[0].IsEnabled = false;
			//	grid.Children[1].IsEnabled = false;
			//	grid.Children[4].IsEnabled = false;
			//	grid.Children[5].IsEnabled = false;
			//});

			settings.DisplayablePropertyMaker = generalProperties;
			settings.ClientDatabase = database;
			settings.SearchEngine.SetAttributes(
				settings.AttId, settings.AttDisplay,
				ServerItemAttributes.AegisName, null,
				ServerItemAttributes.ApplicableJob, ServerItemAttributes.Script,
				ServerItemAttributes.OnEquipScript, ServerItemAttributes.OnUnequipScript,
				ServerItemAttributes.Type, ServerItemAttributes.Gender
				);

			settings.SearchEngine.SetSettings(ServerItemAttributes.Id, true);
			settings.SearchEngine.SetSettings(ServerItemAttributes.Name, true);
			settings.SearchEngine.SetSettings(ServerItemAttributes.AegisName, true);

			Table<int, ReadableTuple<int>> cDb = null;
			Table<int, ReadableTuple<int>> citemsDb = null;

			settings.SearchEngine.SetupImageDataGetter = delegate(ReadableTuple<TKey> tuple) {
				tuple.GetImageData = delegate {
					try {
						if (cDb == null) {
							cDb = database.GetTable<int>(ServerDbs.ClientResourceDb);
							citemsDb = database.GetTable<int>(ServerDbs.CItems);
						}

						if (cDb == null) {
							return null;
						}

						int id = tuple.GetKey<int>();
						int val;

						if ((val = tuple.GetIntNoThrow(ServerItemAttributes.Sprite)) > 0) {
							id = val;
						}

						if (citemsDb.ContainsKey(id)) {
							byte[] data = database.MetaGrf.GetData(EncodingService.FromAnyToDisplayEncoding(@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\item\" + citemsDb.GetTuple(id).GetValue<string>(ClientItemAttributes.IdentifiedResourceName) + ".bmp"));

							if (data != null) {
								GrfImage gimage = new GrfImage(ref data);
								gimage.MakePinkTransparent();
								return gimage.Cast<BitmapSource>();
							}
						}

						if (!cDb.ContainsKey(id))
							return null;

						{
							byte[] data = database.MetaGrf.GetData(EncodingService.FromAnyToDisplayEncoding(@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\item\" + cDb.GetTuple(id).GetValue<string>(ClientResourceAttributes.ResourceName) + ".bmp"));

							if (data != null) {
								GrfImage gimage = new GrfImage(ref data);
								gimage.MakePinkTransparent();
								return gimage.Cast<BitmapSource>();
							}
						}

						return null;
					}
					catch {
						return null;
					}
				};
			};

			settings.AddedCommands.Add(new GItemCommand<TKey, ReadableTuple<TKey>> {
				Visibility = gdb.DbSource == ServerDbs.Items ? Visibility.Visible : Visibility.Collapsed,
				AllowMultipleSelection = false,
				DisplayName = "Copy to [" + ServerDbs.Items2.DisplayName + "]...",
				ImagePath = "convert.png",
				InsertIndex = 3,
				Shortcut = ApplicationShortcut.CopyTo2,
				AddToCommandsStack = false,
				GenericCommand = tuple => tab.CopyItemTo(db.GetDb<TKey>(ServerDbs.Items2))
			});

			settings.AddedCommands.Add(new GItemCommand<TKey, ReadableTuple<TKey>> {
				AllowMultipleSelection = true,
				DisplayName = "Copy entries to clipboard (txt)",
				ImagePath = "export.png",
				InsertIndex = 4,
				Shortcut = ApplicationShortcut.Copy,
				AddToCommandsStack = false,
				GenericCommand = delegate(List<ReadableTuple<TKey>> items) {
					StringBuilder builder = new StringBuilder();
					DbIOItems.DbItemsWriterSub(builder, db, items.OrderBy(p => p.GetKey<TKey>()), ServerType.RAthena);
					Clipboard.SetDataObject(builder.ToString());
				}
			});

			settings.AddedCommands.Add(new GItemCommand<TKey, ReadableTuple<TKey>> {
				AllowMultipleSelection = true,
				DisplayName = "Copy entries to clipboard (conf)",
				ImagePath = "export.png",
				InsertIndex = 5,
				Shortcut = ApplicationShortcut.Copy2,
				AddToCommandsStack = false,
				GenericCommand = delegate(List<ReadableTuple<TKey>> items) {
					StringBuilder builder = new StringBuilder();
					DbIOItems.DbItemsWriterSub(builder, db, items, ServerType.Hercules);
					Clipboard.SetDataObject(builder.ToString());
				}
			});

			var select = GenerateSelectFrom(ServerDbs.CItems, tab);
			select.Shortcut = ApplicationShortcut.Select;
			settings.AddedCommands.Add(select);
			settings.AddedCommands.Last().InsertIndex = 6;

			AbstractDb<int> citemDb = null;
			AbstractDb<int> petDb1 = null;
			AbstractDb<int> petDb2 = null;
			AbstractDb<int> mobDb1 = null;
			AbstractDb<int> mobDb2 = null;
			ItemGeneratorEngine itemGen = new ItemGeneratorEngine();

			settings.AddedCommands.Add(new GItemCommand<TKey, ReadableTuple<TKey>> {
				AllowMultipleSelection = true,
				DisplayName = String.Format("Add in [{0}]", ServerDbs.CItems.DisplayName),
				ImagePath = "add.png",
				InsertIndex = 7,
				Shortcut = ApplicationShortcut.FromString("Ctrl-Alt-E", String.Format("Add in [{0}]", ServerDbs.CItems.DisplayName)),
				AddToCommandsStack = false,
				GenericCommand = delegate(List<ReadableTuple<TKey>> items) {
					try {
						citemDb = tab.GetDb<int>(ServerDbs.CItems);
						petDb1 = tab.GetDb<int>(ServerDbs.Pet);
						petDb2 = tab.GetDb<int>(ServerDbs.Pet2);
						mobDb1 = tab.GetDb<int>(ServerDbs.Mobs);
						mobDb2 = tab.GetDb<int>(ServerDbs.Mobs2);

						citemDb.Table.Commands.Begin();

						foreach (var item in items) {
							int key = item.GetKey<int>();

							if (!citemDb.Table.ContainsKey(key)) {
								ReadableTuple<int> tuple = new ReadableTuple<int>(key, ClientItemAttributes.AttributeList);
								tuple.Added = true;
								citemDb.Table.Commands.AddTuple(key, tuple, false);

								var cmds = itemGen.Generate(tuple, (ReadableTuple<int>)(object)item, mobDb1, mobDb2, petDb1, petDb2);

								if (cmds != null)
									citemDb.Table.Commands.StoreAndExecute(cmds);
							}
						}
					}
					finally {
						citemDb.Table.Commands.EndEdit();
					}
				},
			});
			settings.AddedCommands.Last().InsertIndex = 7;

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
					DbIOErrorHandler.Start();

					var dbItems = gdb.GetMeta<int>(ServerDbs.Items);

					List<string> aegisNames = dbItems.FastItems.Select(p => p.GetStringValue(ServerItemAttributes.AegisName.Index)).ToList();
					List<string> names = dbItems.FastItems.Select(p => p.GetStringValue(ServerItemAttributes.Name.Index)).ToList();

					for (int i = 0; i < items.Count; i++) {
						AProgress.IsCancelling(parent);
						DbIOItemGroups.Writer2(items[i], serverType, builder, gdb, aegisNames, names);
						parent.Progress = (i + 1f) / items.Count * 100f;
					}
				}
				catch (OperationCanceledException) {
				}
				finally {
					AProgress.Finalize(parent);
					DbIOErrorHandler.Stop();
				}

				try {
					Clipboard.SetDataObject(builder.ToString());
					Clipboard.SetText(builder.ToString());
				}
				catch {
				}
			}, parent, 200, null, true, true));
		}

		public static void AddTagGeneratorTabChangedEvent<TKey>(TabControl control, GDbTab tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb) {
			SdeEditor.Instance.SelectionChanged += new SdeEditor.SdeSelectionChangedEventHandler((sender, oldItem, newItem) => {
				try {
					TabItem item = newItem;

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
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			});
		}

		public static void LoadSItemGroupVisualUpdate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
			List<DbAttribute> attributes = settings.AttributeList.Attributes;
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
			try {
				if (String.IsNullOrEmpty(SdeAppConfiguration.NotepadPath))
					Process.Start("notepad++.exe", "\"" + filePath + "\" -n" + line);
				else {
					Process.Start(String.Format("\"{0}\"", SdeAppConfiguration.NotepadPath), "\"" + filePath + "\" -n" + line);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}