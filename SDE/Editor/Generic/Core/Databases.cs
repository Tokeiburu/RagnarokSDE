using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Database;
using ErrorManager;
using GRF.Image;
using SDE.ApplicationConfiguration;
using SDE.Editor.Achievement;
using SDE.Editor.Engines;
using SDE.Editor.Engines.LuaEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.Editor.Generic.UI.CustomControls;
using SDE.Editor.Items;
using SDE.View;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Extension;
using Utilities.IndexProviders;
using Utilities.Services;

namespace SDE.Editor.Generic.Core {
	// debug.Load must ALWAYS be called when loading an item in the DB
	// This method creates a backup which will be used when saving the file
	// The same rule applies for debug.Write
	public class DbItems : AbstractDb<int> {
		private int _format = -1;

		public DbItems() {
			DbSource = ServerDbs.Items;
			AttributeList = ServerItemAttributes.AttributeList;
			DbLoader = DbIOItems.Loader;
			//TabGenerator.GDbTabMaker = GTabsMaker.LoadSItemsTab<int>;
			DbWriter = DbIOItems.DbItemsWriter;

			GDbTabWrapper<int, ReadableTuple<int>> mTab = null;

			TabGenerator.SetSettings = delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
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
			};
			TabGenerator.OnSetCustomCommands = delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
				Table<int, ReadableTuple<int>> cDb = null;
				Table<int, ReadableTuple<int>> citemsDb = null;
				var database = SdeEditor.Instance.ProjectDatabase;
				mTab = tab;

				settings.SearchEngine.SetupImageDataGetter = delegate(ReadableTuple<int> tuple) {
					tuple.GetImageData = delegate {
						try {
							if (cDb == null) {
								cDb = database.GetTable<int>(ServerDbs.ClientResourceDb);
								citemsDb = database.GetTable<int>(ServerDbs.CItems);
							}

							if (cDb == null || citemsDb == null) {
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

									if (gimage.GrfImageType == GrfImageType.Bgr24) {
										gimage.Convert(GrfImageType.Bgra32);
									}

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

									if (gimage.GrfImageType == GrfImageType.Bgr24) {
										gimage.Convert(GrfImageType.Bgra32);
									}

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

				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					Visibility = gdb.DbSource == ServerDbs.Items ? Visibility.Visible : Visibility.Collapsed,
					AllowMultipleSelection = false,
					DisplayName = "Copy to [" + ServerDbs.Items2.DisplayName + "]...",
					ImagePath = "convert.png",
					InsertIndex = 3,
					Shortcut = ApplicationShortcut.CopyTo2,
					AddToCommandsStack = false,
					GenericCommand = tuple => tab.CopyItemTo(gdb.GetDb<int>(ServerDbs.Items2))
				});

				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard (txt or yml)",
					ImagePath = "export.png",
					InsertIndex = 4,
					Shortcut = ApplicationShortcut.Copy,
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<int>> items) {
						if (_format == 2) {
							StringBuilder builder = new StringBuilder();
							var clientDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.CItems);

							foreach (var item in items) {
								DbIOItems.WriteEntry2(this, builder, item, clientDb);
							}

							Clipboard.SetDataObject(builder.ToString());
						}
						else if (_format == 1) {
							var itemDb = GetMeta<int>(ServerDbs.Items);
							StringBuilder builder = new StringBuilder();

							foreach (var item in items) {
								DbIOItems.WriteEntryYaml(builder, item, itemDb);
							}

							Clipboard.SetDataObject(builder.ToString());
						}
						else {
							StringBuilder builder = new StringBuilder();
							DbIOItems.DbItemsWriterSub(builder, this, items.OrderBy(p => p.GetKey<int>()), ServerType.RAthena);
							Clipboard.SetDataObject(builder.ToString());
						}
					}
				});

				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard (conf)",
					ImagePath = "export.png",
					InsertIndex = 5,
					Shortcut = ApplicationShortcut.Copy2,
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<int>> items) {
						StringBuilder builder = new StringBuilder();
						DbIOItems.DbItemsWriterSub(builder, this, items, ServerType.Hercules);
						Clipboard.SetDataObject(builder.ToString());
					}
				});

				//settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
				//	AllowMultipleSelection = true,
				//	DisplayName = "Copy entries to clipboard (Nova conf)",
				//	ImagePath = "export.png",
				//	InsertIndex = 6,
				//	Shortcut = ApplicationShortcut.Copy2,
				//	AddToCommandsStack = false,
				//	GenericCommand = delegate(List<ReadableTuple<int>> items) {
				//		StringBuilder builder = new StringBuilder();
				//		DbIOItems.DbItemsWriterSub2(builder, this, items, ServerType.Hercules);
				//		Clipboard.SetDataObject(builder.ToString());
				//	}
				//});

				var select = GTabsMaker.GenerateSelectFrom(ServerDbs.CItems, tab);
				select.Shortcut = ApplicationShortcut.Select;
				settings.AddedCommands.Add(select);
				settings.AddedCommands.Last().InsertIndex = 6;

				AbstractDb<int> citemDb;
				AbstractDb<int> petDb1;
				AbstractDb<int> petDb2;
				AbstractDb<int> mobDb1;
				AbstractDb<int> mobDb2;
				ItemGeneratorEngine itemGen = new ItemGeneratorEngine();

				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = String.Format("Add in [{0}]", ServerDbs.CItems.DisplayName),
					ImagePath = "add.png",
					InsertIndex = 7,
					Shortcut = ApplicationShortcut.FromString("Ctrl-Alt-E", String.Format("Add in [{0}]", ServerDbs.CItems.DisplayName)),
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<int>> items) {
						citemDb = tab.GetDb<int>(ServerDbs.CItems);
						petDb1 = tab.GetDb<int>(ServerDbs.Pet);
						petDb2 = tab.GetDb<int>(ServerDbs.Pet2);
						mobDb1 = tab.GetDb<int>(ServerDbs.Mobs);
						mobDb2 = tab.GetDb<int>(ServerDbs.Mobs2);

						try {
							citemDb.Table.Commands.Begin();

							foreach (var item in items) {
								int key = item.GetKey<int>();

								if (!citemDb.Table.ContainsKey(key)) {
									ReadableTuple<int> tuple = new ReadableTuple<int>(key, ClientItemAttributes.AttributeList);
									tuple.Added = true;
									citemDb.Table.Commands.AddTuple(key, tuple, false);

									var cmds = itemGen.Generate(tuple, item, mobDb1, mobDb2, petDb1, petDb2);

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
			};
			SdeEditor.Instance.ProjectDatabase.Reloaded += delegate {
				if (DbPathLocator.DetectPath(this.DbSource).IsExtension(".yml"))
					return;

				bool herc = DbPathLocator.GetServerType() == ServerType.Hercules;
				bool isRenewal = DbPathLocator.GetIsRenewal();

				SdeEditor.Instance.Dispatch(delegate {
					try {
						if (mTab == null)
							return;

						var grid = mTab.Settings.DisplayablePropertyMaker.GetComponent<Grid>(22, 3);
						var gridCheckoxes = mTab.Settings.DisplayablePropertyMaker.GetComponent<Grid>(23, 3);

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
			TabGenerator.GenerateGrid = null;
			TabGenerator.OnPreviewTabVisualUpdate = _generateTab;
		}

		private void _generateTab(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
			string path = DbPathLocator.DetectPath(settings.DbData);
			int format;

			if (path == null)
				return;

			if (path.EndsWith(".conf") && ProjectDatabase.IsNova) {
				format = 2;
			}
			else if (path.EndsWith(".yml")) {
				format = 1;
			}
			else {
				format = 0;
			}

			if (tab.IsDeployed && _format == format)
				return;

			_format = format;

			if (format == 2) {
				tab.Clear();
				TabGenerator.Settings.GeneralProperties.Clear();

				DisplayableProperty<int, ReadableTuple<int>> generalProperties = TabGenerator.Settings.GeneralProperties;
				generalProperties.Spacing = 0;

				int line = 0;
				AttributeList list = ServerItemAttributes.AttributeList;

				GTabsMaker.Print(ref line, new SpecifiedRangeIndexProvider(new int[] {
					ServerItemAttributes.Id.Index, 1, ServerItemAttributes.Type.Index, 1,
					ServerItemAttributes.AegisName.Index, 2,
					ServerItemAttributes.TempClientName.Index, 1, ServerItemAttributes.SubType.Index, 1,
					ServerItemAttributes.Buy.Index, 2,
					ServerItemAttributes.Weight.Index, 1, ServerItemAttributes.Attack.Index, 1,
					ServerItemAttributes.Defense.Index, 1, ServerItemAttributes.Range.Index, 1,
					ServerItemAttributes.NumberOfSlots.Index, 1, ServerItemAttributes.ApplicableJob.Index, 1,
					ServerItemAttributes.Upper.Index, 1, ServerItemAttributes.Gender.Index, 1,
					ServerItemAttributes.Location.Index, 1, ServerItemAttributes.WeaponLevel.Index, 1,
					ServerItemAttributes.EquipLevelMin.Index, 1, ServerItemAttributes.Refineable.Index, 1,
					ServerItemAttributes.ClassNumber.Index, 1, ServerItemAttributes.Script.Index, 1,
					ServerItemAttributes.OnEquipScript.Index, 1, ServerItemAttributes.OnUnequipScript.Index, 1,
					ServerItemAttributes.AliasName.Index, 1, ServerItemAttributes.DropEffect.Index, 1,
					ServerItemAttributes.Flags.Index, 1, ServerItemAttributes.CustomFlags.Index, 1,
					ServerItemAttributes.MHFlags.Index, 1, ServerItemAttributes.MHMaxUses.Index, 1,
				}), generalProperties, list);

				generalProperties.AddCustomProperty(new QueryItemDroppedBy<int, ReadableTuple<int>>(line, 0, 2, 2));

				GTabsMaker.PrintGrid(ref line, 3, 1, 2, new SpecifiedIndexProvider(new[] {
					ServerItemAttributes.TradeFlag.Index, ServerItemAttributes.TradeOverride.Index,
					ServerItemAttributes.NoUseFlag.Index, ServerItemAttributes.NoUseOverride.Index,
					ServerItemAttributes.StackAmount.Index, ServerItemAttributes.StackFlags.Index,
					ServerItemAttributes.Delay.Index, ServerItemAttributes.DelayStatus.Index,
					ServerItemAttributes.TempMvpCategory.Index, ServerItemAttributes.TempExpectedWeight.Index,
					ServerItemAttributes.TempWoEDelay.Index
				}), -1, 0, -1, 0, generalProperties, list);

				generalProperties.SetRow(line, new GridLength(1, GridUnitType.Star));

				tab.TabChanged();
			}
			else if (format == 1) {
				tab.Clear();
				TabGenerator.Settings.GeneralProperties.Clear();

				DisplayableProperty<int, ReadableTuple<int>> generalProperties = TabGenerator.Settings.GeneralProperties;
				generalProperties.Spacing = 0;

				int line = 0;
				AttributeList list = ServerItemAttributes.AttributeList;

				GTabsMaker.Print(ref line, new SpecifiedRangeIndexProvider(new int[] {
					ServerItemAttributes.Id.Index, 1, ServerItemAttributes.Type.Index, 1,
					ServerItemAttributes.AegisName.Index, 2,
					ServerItemAttributes.DropEffect.Index, 1, ServerItemAttributes.SubType.Index, 1,
					ServerItemAttributes.Buy.Index, 2,
					ServerItemAttributes.Weight.Index, 1, ServerItemAttributes.Attack.Index, 1,
					ServerItemAttributes.Defense.Index, 1, ServerItemAttributes.Range.Index, 1,
					ServerItemAttributes.NumberOfSlots.Index, 1, ServerItemAttributes.ApplicableJob.Index, 1,
					ServerItemAttributes.Upper.Index, 1, ServerItemAttributes.Gender.Index, 1,
					ServerItemAttributes.Location.Index, 1, ServerItemAttributes.WeaponLevel.Index, 1,
					ServerItemAttributes.EquipLevelMin.Index, 1, ServerItemAttributes.Refineable.Index, 1,
					ServerItemAttributes.ClassNumber.Index, 1, ServerItemAttributes.Script.Index, 1,
					ServerItemAttributes.OnEquipScript.Index, 1, ServerItemAttributes.OnUnequipScript.Index, 1,
					ServerItemAttributes.AliasName.Index, 1, ServerItemAttributes.Flags.Index, 1
				}), generalProperties, list);

				generalProperties.AddCustomProperty(new QueryItemDroppedBy<int, ReadableTuple<int>>(line, 0, 2, 2));

				GTabsMaker.PrintGrid(ref line, 3, 1, 2, new SpecifiedIndexProvider(new[] {
					ServerItemAttributes.TradeFlag.Index, ServerItemAttributes.TradeOverride.Index,
					ServerItemAttributes.NoUseFlag.Index, ServerItemAttributes.NoUseOverride.Index,
					ServerItemAttributes.StackAmount.Index, ServerItemAttributes.StackFlags.Index, 
					ServerItemAttributes.Delay.Index, ServerItemAttributes.DelayStatus.Index,
				}), -1, 0, -1, 0, generalProperties, list);

				generalProperties.SetRow(line, new GridLength(1, GridUnitType.Star));

				tab.TabChanged();
			}
			else if (format == 0) {
				tab.Clear();
				TabGenerator.Settings.GeneralProperties.Clear();

				DisplayableProperty<int, ReadableTuple<int>> generalProperties = TabGenerator.Settings.GeneralProperties;
				generalProperties.Spacing = 0;

				int line = 0;
				AttributeList list = ServerItemAttributes.AttributeList;

				GTabsMaker.Print(ref line, new SpecifiedRangeIndexProvider(new int[] {
					ServerItemAttributes.Id.Index, 1,
					ServerItemAttributes.Type.Index, 1,
					ServerItemAttributes.AegisName.Index, 2,
					ServerItemAttributes.Buy.Index, 2,
					ServerItemAttributes.Weight.Index, 16
				}), generalProperties, list);

				generalProperties.AddCustomProperty(new QueryItemDroppedBy<int, ReadableTuple<int>>(line, 0, 2, 2));

				GTabsMaker.PrintGrid(ref line, 3, 1, 2, new SpecifiedIndexProvider(new[] {
					ServerItemAttributes.TradeFlag.Index, ServerItemAttributes.NoUseFlag.Index,
					ServerItemAttributes.Stack.Index, ServerItemAttributes.Sprite.Index,
					ServerItemAttributes.Delay.Index, ServerItemAttributes.DelayStatus.Index,
				}), -1, 0, -1, 0, generalProperties, list);

				generalProperties.SetRow(line, new GridLength(1, GridUnitType.Star));

				GTabsMaker.PrintGrid(ref line, 3, 1, 2, new SpecifiedIndexProvider(new[] {
					ServerItemAttributes.KeepAfterUse.Index, -1,
					ServerItemAttributes.BindOnEquip.Index, -1,
					ServerItemAttributes.BuyingStore.Index, -1,
					ServerItemAttributes.ForceSerial.Index, -1
				}), -1, 0, 0, 0, generalProperties, list);

				tab.TabChanged();
			}
		}

		protected override void _loadDb() {
			base._loadDb();

			string path = DbPathLocator.DetectPath(DbSource);

			if (path.IsExtension(".yml")) {
				return;
			}

			if (path.IsExtension(".conf") && ProjectDatabase.IsNova) {
				return;
			}

			if (DbPathLocator.GetServerType() == ServerType.RAthena) {
				DbDebugItem<int> debug = new DbDebugItem<int>(this);

				if (debug.Load(DbSource)) {
					DbIOItems.Loader(debug, this);
				}

				if (debug.FilePath.IsExtension(".yml")) {
				}
				else {
					if (debug.Load(ServerDbs.ItemsAvail)) DbIOMethods.DbLoaderCommaRange(debug, AttributeList, ServerItemAttributes.Sprite.Index, 1, false);
					if (debug.Load(ServerDbs.ItemsDelay)) DbIOMethods.DbLoaderCommaRange(debug, AttributeList, ServerItemAttributes.Delay.Index, 2, false);
					if (debug.Load(ServerDbs.ItemsNoUse)) DbIOMethods.DbLoaderCommaRange(debug, AttributeList, ServerItemAttributes.NoUseFlag.Index, 2, false);
					if (debug.Load(ServerDbs.ItemsTrade)) DbIOMethods.DbLoaderCommaRange(debug, AttributeList, ServerItemAttributes.TradeFlag.Index, 2, false);
					if (debug.Load(ServerDbs.ItemsStack)) DbIOMethods.DbLoaderComma(debug, AttributeList, DbIOItems.DbItemsStackFunction, false);
					if (debug.Load(ServerDbs.ItemsBuyingStore)) DbIOMethods.DbLoaderComma(debug, AttributeList, DbIOItems.DbItemsBuyingStoreFunction, false);
					if (debug.Load(ServerDbs.ItemsAvail2)) DbIOMethods.DbLoaderCommaRange(debug, AttributeList, ServerItemAttributes.Sprite.Index, 1, false);
					if (debug.Load(ServerDbs.ItemsDelay2)) DbIOMethods.DbLoaderCommaRange(debug, AttributeList, ServerItemAttributes.Delay.Index, 2, false);
					if (debug.Load(ServerDbs.ItemsNoUse2)) DbIOMethods.DbLoaderCommaRange(debug, AttributeList, ServerItemAttributes.NoUseFlag.Index, 2, false);
					if (debug.Load(ServerDbs.ItemsTrade2)) DbIOMethods.DbLoaderCommaRange(debug, AttributeList, ServerItemAttributes.TradeFlag.Index, 2, false);
					if (debug.Load(ServerDbs.ItemsStack2)) DbIOMethods.DbLoaderComma(debug, AttributeList, DbIOItems.DbItemsStackFunction, false);
					if (debug.Load(ServerDbs.ItemsBuyingStore2)) DbIOMethods.DbLoaderComma(debug, AttributeList, DbIOItems.DbItemsBuyingStoreFunction, false);
				}
			}
		}

		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			LuaHelper.WriteViewIds(DbSource, this);

			DbDebugItem<int> debug = new DbDebugItem<int>(this);

			debug.DbSource = DbSource;

			if (debug.Write(dbPath, subPath, serverType, fileType)) {
				DbIOItems.DbItemsWriter(debug, this);
			}

			if (debug.FilePath.IsExtension(".yml")) {
				return;
			}

			if (debug.FilePath.IsExtension(".conf") && ProjectDatabase.IsNova) {
				return;
			}

			if (serverType == ServerType.RAthena) {
				var db2 = GetDb<int>(ServerDbs.Items2);
				var db1 = GetDb<int>(ServerDbs.Items);
				var itemDbs = GetMeta<int>(ServerDbs.Items);

				// Remove erronous entries
				if (DbSource == ServerDbs.Items) {
					// Items

					this.Table.Commands.Begin();

					try {
						Func<string, bool> isNullEmptyOrZero = delegate(string input) {
							if (input == "")
								return true;
							if (input == null)
								return true;
							if (input == "0")
								return true;
							if (input == "false")
								return true;
							return false;
						};

						foreach (var tuple1 in this.Table.FastItems) {
							var tuple2 = db2.Table.TryGetTuple(tuple1.Key);

							string val1;
							string val2;

							if (tuple2 == null)
								continue;

							// ItemsAvail
							val1 = tuple1.GetValue<string>(ServerItemAttributes.Sprite.Index);
							val2 = tuple2.GetValue<string>(ServerItemAttributes.Sprite.Index);

							if (isNullEmptyOrZero(val2) && val1 != val2) {
								this.Table.Commands.Set(tuple1, ServerItemAttributes.Sprite.Index, "0");
							}

							// ItemsDelay
							val1 = tuple1.GetValue<string>(ServerItemAttributes.Delay.Index);
							val2 = tuple2.GetValue<string>(ServerItemAttributes.Delay.Index);

							if (isNullEmptyOrZero(val2) && val1 != val2) {
								this.Table.Commands.Set(tuple1, ServerItemAttributes.Delay.Index, "0");
							}

							// NoUse
							val1 = tuple1.GetValue<string>(ServerItemAttributes.NoUseFlag.Index);
							val2 = tuple2.GetValue<string>(ServerItemAttributes.NoUseFlag.Index);

							if (isNullEmptyOrZero(val2) && val1 != val2) {
								this.Table.Commands.Set(tuple1, ServerItemAttributes.NoUseFlag.Index, "0");
							}

							// NoUse
							val1 = tuple1.GetValue<string>(ServerItemAttributes.TradeFlag.Index);
							val2 = tuple2.GetValue<string>(ServerItemAttributes.TradeFlag.Index);

							if (isNullEmptyOrZero(val2) && val1 != val2) {
								this.Table.Commands.Set(tuple1, ServerItemAttributes.TradeFlag.Index, "0");
							}

							// Stack
							val1 = tuple1.GetValue<string>(ServerItemAttributes.Stack.Index);
							val2 = tuple2.GetValue<string>(ServerItemAttributes.Stack.Index);

							if (isNullEmptyOrZero(val2) && val1 != val2) {
								this.Table.Commands.Set(tuple1, ServerItemAttributes.Stack.Index, "0");
							}

							// BuyingStore
							val1 = tuple1.GetValue<string>(ServerItemAttributes.BuyingStore.Index);
							val2 = tuple2.GetValue<string>(ServerItemAttributes.BuyingStore.Index);

							if (isNullEmptyOrZero(val2) && val1 != val2) {
								this.Table.Commands.Set(tuple1, ServerItemAttributes.BuyingStore.Index, "false");
							}
						}
					}
					catch (Exception err) {
						this.Table.Commands.CancelEdit();
						ErrorHandler.HandleException(err);
					}
					finally {
						this.Table.Commands.End();
					}
				}

				List<ServerDbs> serverDbs;

				if (ServerDbs.Items == DbSource) {
					serverDbs = new List<ServerDbs> {
						ServerDbs.ItemsAvail,
						ServerDbs.ItemsDelay,
						ServerDbs.ItemsNoUse,
						ServerDbs.ItemsStack,
						ServerDbs.ItemsTrade,
						ServerDbs.ItemsBuyingStore
					};
				}
				else {
					serverDbs = new List<ServerDbs> {
						ServerDbs.ItemsAvail2,
						ServerDbs.ItemsDelay2,
						ServerDbs.ItemsNoUse2,
						ServerDbs.ItemsStack2,
						ServerDbs.ItemsTrade2,
						ServerDbs.ItemsBuyingStore2
					};
				}

				debug = new DbDebugItem<int>(this);
				debug.ForceWrite = true;

				if (db2.Table.Commands.CommandIndex == -1 && db1.Table.Commands.CommandIndex == -1)
					debug.ForceWrite = false;

				debug.DbSource = serverDbs[0];
				if (debug.Write(dbPath, subPath, serverType, fileType, true)) {
					DbIOItems.DbItemsCommaRange(debug, this, ServerItemAttributes.Sprite.Index, 1, "", (t, v, l) => {
						if (SdeAppConfiguration.AddCommentForItemAvail) {
							try {
								l += "\t//" + t.GetValue<string>(ServerItemAttributes.Name);
								int id2;

								if (Int32.TryParse(v[0], out id2)) {
									var t2 = itemDbs.TryGetTuple(id2);

									if (t2 != null) {
										l += " - " + t2.GetValue<string>(ServerItemAttributes.Name);
									}
								}
							}
							catch {
							}
						}

						return l;
					});
				}

				debug.DbSource = serverDbs[1];
				if (debug.Write(dbPath, subPath, serverType, fileType, true)) {
					DbIOItems.DbItemsCommaRange(debug, this, ServerItemAttributes.Delay.Index, 1, "", (t, v, l) => {
						try {
							string valS;

							if ((valS = t.GetValue<string>(ServerItemAttributes.DelayStatus)) != "") {
								l += "," + valS;
							}
						}
						catch {
						}

						return l;
					});
				}

				debug.DbSource = serverDbs[2];
				if (debug.Write(dbPath, subPath, serverType, fileType, true)) {
					DbIOItems.DbItemsNouse(debug, this);
				}

				debug.DbSource = serverDbs[3];
				if (debug.Write(dbPath, subPath, serverType, fileType, true)) {
					DbIOItems.DbItemsStack(debug, this);
				}

				debug.DbSource = serverDbs[4];
				if (debug.Write(dbPath, subPath, serverType, fileType, true)) {
					DbIOItems.DbItemsTrade(debug, this);
				}

				debug.DbSource = serverDbs[5];
				if (debug.Write(dbPath, subPath, serverType, fileType, true)) {
					DbIOItems.DbItemsBuyingStore(debug, this);
				}
			}
		}
	}

	public class DbItems2 : DbItems {
		public DbItems2() {
			DbSource = ServerDbs.Items2;
			ThrowFileNotFoundException = false;
			//UsePreviousOutput = true;
		}
	}

	public class DbClientItems : AbstractDb<int> {
		public DbClientItems() {
			DbSource = ServerDbs.CItems;
			AttributeList = ClientItemAttributes.AttributeList;
			DbLoader = DbIOClientItems.Loader;
			DbWriter = DbIOClientItems.Writer;
			TabGenerator.GDbTabMaker = GTabsMaker.LoadCItemsTab<int>;
			TabGenerator.IsTabEnabledMethod = (e, a) => ProjectConfiguration.SynchronizeWithClientDatabases;
			TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromSkillRequirementsDb;
		}

		public override void OnLoadDataFromClipboard(DbDebugItem<int> debug, string text, string path, AbstractDb<int> abstractDb) {
			// The text comes from UTF-8 (clipboard), needs to be converted back to its proper encoding.
			if (EncodingService.Ansi.GetString(EncodingService.Ansi.GetBytes(text)) == text) {
				File.WriteAllText(path, text, EncodingService.Ansi);
			}
			if (EncodingService.Korean.GetString(EncodingService.Korean.GetBytes(text)) == text) {
				File.WriteAllText(path, text, EncodingService.Korean);
			}

			DbIOClientItems.LoadEntry(this, path);
		}

		protected override void _loadDb() {
			Table.Clear();
			if (ProjectConfiguration.SynchronizeWithClientDatabases) {
				TextFileHelper.LatestFile = DbSource;
				DbLoader(null, this);
			}
		}

		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			if (ProjectConfiguration.SynchronizeWithClientDatabases) {
				if (!IsEnabled) return;

				TextFileHelper.LatestFile = DbSource;
				DbWriter(null, this);
			}
		}
	}

	public class DbMobs : AbstractDb<int> {
		private int _format = -1;

		public DbMobs() {
			LayoutIndexes = new[] {
				new[] {
					ServerMobAttributes.Id.Index, ServerMobAttributes.Sprite.Index,
					ServerMobAttributes.AegisName.Index, -1,
					ServerMobAttributes.ClientSprite.Index, -1,
					ServerMobAttributes.KRoName.Index, -1,
					ServerMobAttributes.IRoName.Index, -1
				},
				null,
				new[] {
					ServerMobAttributes.Lv.Index, ServerMobAttributes.ExpPer.Index,
					ServerMobAttributes.Str.Index, ServerMobAttributes.Agi.Index,
					ServerMobAttributes.Vit.Index, ServerMobAttributes.Int.Index,
					ServerMobAttributes.Dex.Index, ServerMobAttributes.Luk.Index
				},
				new[] {
					ServerMobAttributes.Hp.Index, ServerMobAttributes.Sp.Index,
					ServerMobAttributes.Exp.Index, ServerMobAttributes.JExp.Index,
					ServerMobAttributes.Atk1.Index, ServerMobAttributes.Atk2.Index,
					ServerMobAttributes.Def.Index, ServerMobAttributes.Mdef.Index
				},
				new[] {
					ServerMobAttributes.Race.Index, ServerMobAttributes.AttackRange.Index,
					ServerMobAttributes.Size.Index, ServerMobAttributes.ViewRange.Index,
					ServerMobAttributes.Element.Index, ServerMobAttributes.ChaseRange.Index
				},
				new[] {
					ServerMobAttributes.MvpExp.Index, ServerMobAttributes.DamageMotion.Index,
					ServerMobAttributes.MoveSpeed.Index, ServerMobAttributes.AttackMotion.Index,
					ServerMobAttributes.Mode.Index, ServerMobAttributes.AttackDelay.Index
				}
			};
			GridIndexes = new[] {
				new[] { -1, 0, -1, 0 }, null,
				new[] { 60, 0, -1, 0 },
				new[] { -1, 0, -1, 0 },
				new[] { 60, -115, 77, 0 },
				new[] { -1, 0, -1, 0 }
			};
			DbLoader = DbIOMobs.Loader;
			DbSource = ServerDbs.Mobs;
			AttributeList = ServerMobAttributes.AttributeList;
			ThrowFileNotFoundException = false;
			TabGenerator.MaxElementsToCopyInCustomMethods = ServerMobAttributes.ClientSprite.Index;
			TabGenerator.OnSetCustomCommands = delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard (txt)",
					ImagePath = "export.png",
					InsertIndex = 4,
					Shortcut = ApplicationShortcut.Copy,
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<int>> items) {
						if (_format == 1) {
							var itemDb = GetMeta<int>(ServerDbs.Items);
							StringBuilder builder = new StringBuilder();

							foreach (var item in items) {
								DbIOMobs.WriteEntryYaml(builder, item, itemDb);
							}

							Clipboard.SetDataObject(builder.ToString());
						}
						else {
							TabGenerator<int>.CopyTuplesDefault(TabGenerator, items, gdb);
						}
					}
				});

				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard (conf)",
					ImagePath = "export.png",
					InsertIndex = 5,
					Shortcut = ApplicationShortcut.Copy2,
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<int>> items) {
						StringBuilder builder = new StringBuilder();
						var db = gdb.To<int>();

						foreach (var item in items) {
							DbIOMobs.WriteEntry(db, builder, item);
							builder.AppendLine();
						}

						Clipboard.SetDataObject(builder.ToString());
					}
				});

				//settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
				//	AllowMultipleSelection = true,
				//	DisplayName = "Import from gnjoy",
				//	ImagePath = "imconvert.png",
				//	InsertIndex = 5,
				//	Shortcut = ApplicationShortcut.FromString("Ctrl-U", "Autocomplete2"),
				//	AddToCommandsStack = true,
				//	Command = delegate(ReadableTuple<int> item) {
				//		try {
				//			return MobGeneratorEngine.GeneratekRO(item);
				//		}
				//		catch (Exception err) {
				//			ErrorHandler.HandleException(err);
				//			return null;
				//		}
				//	}
				//});
				//
				//settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
				//	AllowMultipleSelection = true,
				//	DisplayName = "Import from Divine Pride",
				//	ImagePath = "imconvert.png",
				//	InsertIndex = 5,
				//	Shortcut = ApplicationShortcut.FromString("Ctrl-G", "Autocomplete"),
				//	AddToCommandsStack = true,
				//	Command = delegate(ReadableTuple<int> item) {
				//		try {
				//			return MobGeneratorEngine.Generate(item, false);
				//		}
				//		catch (Exception err) {
				//			ErrorHandler.HandleException(err);
				//			return null;
				//		}
				//	}
				//});
				//
				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Import from Divine Pride3",
					ImagePath = "imconvert.png",
					InsertIndex = 5,
					Shortcut = ApplicationShortcut.FromString("Ctrl-H", "Autocomplete3"),
					AddToCommandsStack = true,
					Command = delegate(ReadableTuple<int> item) {
						try {
							return MobGeneratorEngine.Generate(item, true);
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
							return null;
						}
					}
				});
			};
			DbWriterSql = SqlParser.DbSqlMobs;
			DbWriter = DbIOMobs.Writer;
			TabGenerator.OnSetCustomCommands += (tab, settings, gdb) => settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
				Visibility = gdb.DbSource == ServerDbs.Mobs ? Visibility.Visible : Visibility.Collapsed,
				AllowMultipleSelection = false,
				DisplayName = "Copy to [" + ServerDbs.Mobs2.DisplayName + "]...",
				ImagePath = "convert.png",
				InsertIndex = 3,
				Shortcut = ApplicationShortcut.CopyTo2,
				AddToCommandsStack = false,
				GenericCommand = tuple => tab.CopyItemTo(gdb.GetDb<int>(ServerDbs.Mobs2))
			});
			TabGenerator.GenerateGrid = null;
			TabGenerator.OnPreviewTabVisualUpdate = _generateTab;
		}

		private void _generateTab(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
			string path = DbPathLocator.DetectPath(settings.DbData);
			int format;

			if (path == null)
				return;

			if (path.EndsWith(".yml")) {
				format = 1;
				DbPathLocator.SetYamlMob(true);
			}
			else {
				format = 0;
				DbPathLocator.SetYamlMob(false);
			}

			if (tab.IsDeployed && _format == format)
				return;

			_format = format;

			if (format == 1) {
				LayoutIndexes = new[] {
					new[] {
						ServerMobAttributes.Id.Index, ServerMobAttributes.Sprite.Index,
						ServerMobAttributes.AegisName.Index, -1,
						ServerMobAttributes.ClientSprite.Index, -1,
						ServerMobAttributes.KRoName.Index, -1,
						ServerMobAttributes.IRoName.Index, -1
					},
					null,
					new[] {
						ServerMobAttributes.Lv.Index, ServerMobAttributes.DamageTaken.Index,
						ServerMobAttributes.Str.Index, ServerMobAttributes.Agi.Index,
						ServerMobAttributes.Vit.Index, ServerMobAttributes.Int.Index,
						ServerMobAttributes.Dex.Index, ServerMobAttributes.Luk.Index
					},
					new[] {
						ServerMobAttributes.Hp.Index, ServerMobAttributes.Sp.Index,
						ServerMobAttributes.Exp.Index, ServerMobAttributes.JExp.Index,
						ServerMobAttributes.Atk1.Index, ServerMobAttributes.Atk2.Index,
						ServerMobAttributes.Def.Index, ServerMobAttributes.Mdef.Index
					},
					new[] {
						ServerMobAttributes.Race.Index, ServerMobAttributes.AttackRange.Index,
						ServerMobAttributes.Size.Index, ServerMobAttributes.ViewRange.Index,
						ServerMobAttributes.Element.Index, ServerMobAttributes.ChaseRange.Index,
						ServerMobAttributes.Class.Index, ServerMobAttributes.RaceGroups.Index
					},
					new[] {
						ServerMobAttributes.MvpExp.Index, ServerMobAttributes.DamageMotion.Index,
						ServerMobAttributes.MoveSpeed.Index, ServerMobAttributes.AttackMotion.Index,
						ServerMobAttributes.NewMode.Index, ServerMobAttributes.AttackDelay.Index
					}
				};
				GridIndexes = new[] {
					new[] { -1, 0, -1, 0 }, null,
					new[] { 60, 0, -1, 0 },
					new[] { -1, 0, -1, 0 },
					new[] { 60, -115, 77, 0 },
					new[] { -1, 0, -1, 0 }
				};

				tab.Clear();
				TabGenerator.Settings.GeneralProperties.Clear();

				int line = 0;
				TabGenerator.GenerateGridDefault(ref line, TabGenerator.Settings);

				TabGenerator.Settings.GeneralProperties.SetRow(line, new GridLength(1, GridUnitType.Star));
				TabGenerator.Settings.GeneralProperties.AddCustomProperty(new QueryNormalDrops<int, ReadableTuple<int>>(line, 1));
				TabGenerator.Settings.GeneralProperties.AddCustomProperty(new QueryMvpDrops<int, ReadableTuple<int>>(line));
				TabGenerator.Settings.GeneralProperties.AddCustomProperty(new QueryMobSkills<int, ReadableTuple<int>>(line));
				tab.TabChanged();
			}
			else if (format == 0) {
				LayoutIndexes = new[] {
					new[] {
						ServerMobAttributes.Id.Index, ServerMobAttributes.Sprite.Index,
						ServerMobAttributes.AegisName.Index, -1,
						ServerMobAttributes.ClientSprite.Index, -1,
						ServerMobAttributes.KRoName.Index, -1,
						ServerMobAttributes.IRoName.Index, -1
					},
					null,
					new[] {
						ServerMobAttributes.Lv.Index, ServerMobAttributes.ExpPer.Index,
						ServerMobAttributes.Str.Index, ServerMobAttributes.Agi.Index,
						ServerMobAttributes.Vit.Index, ServerMobAttributes.Int.Index,
						ServerMobAttributes.Dex.Index, ServerMobAttributes.Luk.Index
					},
					new[] {
						ServerMobAttributes.Hp.Index, ServerMobAttributes.Sp.Index,
						ServerMobAttributes.Exp.Index, ServerMobAttributes.JExp.Index,
						ServerMobAttributes.Atk1.Index, ServerMobAttributes.Atk2.Index,
						ServerMobAttributes.Def.Index, ServerMobAttributes.Mdef.Index
					},
					new[] {
						ServerMobAttributes.Race.Index, ServerMobAttributes.AttackRange.Index,
						ServerMobAttributes.Size.Index, ServerMobAttributes.ViewRange.Index,
						ServerMobAttributes.Element.Index, ServerMobAttributes.ChaseRange.Index
					},
					new[] {
						ServerMobAttributes.MvpExp.Index, ServerMobAttributes.DamageMotion.Index,
						ServerMobAttributes.MoveSpeed.Index, ServerMobAttributes.AttackMotion.Index,
						ServerMobAttributes.Mode.Index, ServerMobAttributes.AttackDelay.Index
					}
				};
				GridIndexes = new[] {
					new[] { -1, 0, -1, 0 }, null,
					new[] { 60, 0, -1, 0 },
					new[] { -1, 0, -1, 0 },
					new[] { 60, -115, 77, 0 },
					new[] { -1, 0, -1, 0 }
				};

				tab.Clear();
				TabGenerator.Settings.GeneralProperties.Clear();

				int line = 0;
				TabGenerator.GenerateGridDefault(ref line, TabGenerator.Settings);

				TabGenerator.Settings.GeneralProperties.SetRow(line, new GridLength(1, GridUnitType.Star));
				TabGenerator.Settings.GeneralProperties.AddCustomProperty(new QueryNormalDrops<int, ReadableTuple<int>>(line, 1));
				TabGenerator.Settings.GeneralProperties.AddCustomProperty(new QueryMvpDrops<int, ReadableTuple<int>>(line));
				TabGenerator.Settings.GeneralProperties.AddCustomProperty(new QueryMobSkills<int, ReadableTuple<int>>(line));
				tab.TabChanged();
			}
		}

		protected override void _loadDb() {
			base._loadDb();
			LuaHelper.ReloadJobTable(this);

			DbDebugItem<int> debug = new DbDebugItem<int>(this);

			string path = DbPathLocator.DetectPath(DbSource);

			if (path.IsExtension(".yml")) {
				return;
			}

			// These are all being read twice and assigned to their respective table
			if (debug.Load(ServerDbs.MobAvail)) DbIOMethods.DbLoaderCommaRange(debug, AttributeList, ServerMobAttributes.Sprite.Index, 1, false);
		}

		/// <summary>
		/// Writes the db.
		/// </summary>
		/// <param name="dbPath">The db path.</param>
		/// <param name="subPath">The sub path.</param>
		/// <param name="serverType">Type of the server.</param>
		/// <param name="fileType">Type of the file.</param>
		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			LuaHelper.WriteMobLuaFiles(this);
			base.WriteDb(dbPath, subPath, serverType, fileType);

			DbDebugItem<int> debug = new DbDebugItem<int>(this);
			debug.DbSource = DbSource;

			// Fix : 2015-06-28
			// These are saved twice to avoid buggus states between Mob1 and Mob2
			if (true) {
				var temp = (DummyDb<int>)Copy();
				temp.DbSource = DbSource;

				if (IsEnabled) {
					temp.Copy(this);
				}

				var db2 = GetDb<int>(ServerDbs.Mobs2);
				if (db2.IsEnabled) {
					temp.Copy(db2);
				}

				var mobDbs = GetMeta<int>(ServerDbs.Mobs);

				debug.DbSource = ServerDbs.MobAvail;
				if (debug.Write(dbPath, subPath, serverType, fileType, true)) {
					DbIOItems.DbItemsCommaRange(debug, temp, ServerMobAttributes.Sprite.Index, 1, "", (t, v, l) => {
						if (SdeAppConfiguration.AddCommentForMobAvail) {
							try {
								l += "\t//" + t.GetValue<string>(ServerMobAttributes.KRoName);
								int id2;

								if (Int32.TryParse(v[0], out id2)) {
									var t2 = mobDbs.TryGetTuple(id2);

									if (t2 != null) {
										l += " - " + t2.GetValue<string>(ServerMobAttributes.KRoName);
									}
								}
							}
							catch {
							}
						}

						return l;
					});
				}
			}
		}
	}

	public class DbMobs2 : DbMobs {
		public DbMobs2() {
			DbSource = ServerDbs.Mobs2;
			ThrowFileNotFoundException = false;
		}
	}

	public class DbCastle : AbstractDb<int> {
		public DbCastle() {
			LayoutSearch = new DbAttribute[] {
				ServerCastleAttributes.Id, ServerCastleAttributes.CastleName,
				ServerCastleAttributes.MapName, ServerCastleAttributes.NpcName
			};
			DbSource = ServerDbs.Castle;
			AttributeList = ServerCastleAttributes.AttributeList;
			DbWriter = DbIOCastle.Writer;
			DbLoader = DbIOCastle.Loader;
		}
	}

	public class DbCastle2 : DbCastle {
		public DbCastle2() {
			DbSource = ServerDbs.Castle2;
			ThrowFileNotFoundException = false;
		}
	}

	public class DbPet : AbstractDb<int> {
		private int _format = -1;

		public DbPet() {
			LayoutIndexes = new int[] {
				0, 1, -1, 1,
				1, 19, -1, 1,
				20, 5
			};
			LayoutSearch = new DbAttribute[] {
				ServerPetAttributes.MobId, ServerPetAttributes.JName,
				ServerPetAttributes.Name, ServerPetAttributes.EquipId,
				ServerPetAttributes.EggId, ServerPetAttributes.FoodId,
				ServerPetAttributes.PetScript, ServerPetAttributes.LoyalScript
			};
			DbSource = ServerDbs.Pet;
			AttributeList = ServerPetAttributes.AttributeList;
			DbWriter = DbIOPet.Writer;
			DbLoader = DbIOPet.Loader;
			TabGenerator.MaxElementsToCopyInCustomMethods = 25;
			TabGenerator.OnSetCustomCommands = delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					Visibility = gdb.DbSource == ServerDbs.Pet ? Visibility.Visible : Visibility.Collapsed,
					AllowMultipleSelection = false,
					DisplayName = "Copy to [" + ServerDbs.Pet2.DisplayName + "]...",
					ImagePath = "convert.png",
					InsertIndex = 3,
					Shortcut = ApplicationShortcut.CopyTo2,
					AddToCommandsStack = false,
					GenericCommand = tuple => tab.CopyItemTo(GetDb<int>(ServerDbs.Pet2))
				});
				
				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard (txt or yml)",
					ImagePath = "export.png",
					InsertIndex = 4,
					Shortcut = ApplicationShortcut.Copy,
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<int>> items) {
						if (_format == 1) {
							var itemDb = GetMeta<int>(ServerDbs.Items);
							var mobDb = GetMeta<int>(ServerDbs.Mobs);
							StringBuilder builder = new StringBuilder();

							foreach (var item in items) {
								DbIOPet.WriteEntryYaml(builder, item, itemDb, mobDb);
							}

							Clipboard.SetDataObject(builder.ToString());
						}
						else {
							//TabGenerator<int>.CopyTuplesDefault(TabGenerator, items, ServerPetAttributes.AttributeList.Take(22).ToArray());
							TabGenerator<int>.CopyTuplesDefault(TabGenerator, items, gdb);
						}
					}
				});
			};
			TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromMobDb;
			TabGenerator.GenerateGrid = null;
			TabGenerator.OnPreviewTabVisualUpdate = _generateTab;
		}

		private void _generateTab(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
			string path = DbPathLocator.DetectPath(settings.DbData);
			int format;

			if (path == null)
				return;

			if (path.EndsWith(".yml")) {
				format = 1;
			}
			else {
				format = 0;
			}
			
			if (tab.IsDeployed && _format == format)
				return;

			_format = format;

			if (format == 1) {
				LayoutIndexes = new int[] {
					ServerPetAttributes.MobId.Index, 1, -1, 1,
					ServerPetAttributes.LureId.Index, 1, ServerPetAttributes.EggId.Index, 1,
					ServerPetAttributes.EquipId.Index, 1, ServerPetAttributes.FoodId.Index, 1,
					ServerPetAttributes.Fullness.Index, 1, -1 , 1,
					ServerPetAttributes.HungryDelay.Index, 1, ServerPetAttributes.HungerIncrease.Index, 1,
					ServerPetAttributes.IntimacyStart.Index, 1, ServerPetAttributes.IntimacyFed.Index, 1,
					ServerPetAttributes.IntimacyOverfed.Index, 1, ServerPetAttributes.IntimacyHungry.Index, 1,
					ServerPetAttributes.IntimacyOwnerDie.Index, 1, ServerPetAttributes.CaptureRate.Index, 1,
					ServerPetAttributes.SpecialPerformance.Index, 1, ServerPetAttributes.AllowAutoFeed.Index, 1,
					ServerPetAttributes.AttackRate.Index, 1, ServerPetAttributes.RetaliateRate.Index, 1,
					ServerPetAttributes.ChangeTargetRate.Index, 1, -1, 1,
					ServerPetAttributes.PetScript.Index, 1, ServerPetAttributes.LoyalScript.Index, 1,
				};

				tab.Clear();
				TabGenerator.Settings.GeneralProperties.Clear();
				tab.Settings.AttDisplay = ServerPetAttributes.MobId;

				int line = 0;
				TabGenerator.GenerateGridDefault(ref line, TabGenerator.Settings);
				TabGenerator.Settings.GeneralProperties.SetRow(line - 1, new GridLength(1, GridUnitType.Star));
				TabGenerator.Settings.GeneralProperties.AddCustomProperty(new CustomEvolutionProperty<int, ReadableTuple<int>>(line, 1));
				tab.TabChanged();
			}
			else if (format == 0) {
				LayoutIndexes = new int[] {
					0, 1, -1, 1,
					1, 19, -1, 1,
					20, 5
				};

				tab.Clear();
				TabGenerator.Settings.GeneralProperties.Clear();
				
				int line = 0;
				TabGenerator.GenerateGridDefault(ref line, TabGenerator.Settings);

				tab.TabChanged();
			}
		}

		protected override void _loadDb() {
			LayoutIndexes = new int[] {
				0, 1, -1, 1,
				-2, 1, -2, 1,
				20, 5
			};

			base._loadDb();
		}
	}

	public class DbPet2 : DbPet {
		public DbPet2() {
			DbSource = ServerDbs.Pet2;
			ThrowFileNotFoundException = false;
		}
	}

	public class DbItemCombos : AbstractDb<string> {
		public DbItemCombos() {
			LayoutIndexes = new int[] { 0, 1, -1, 1, 1, 1 };
			UnsafeContext = true;
			DbSource = ServerDbs.Combos;
			AttributeList = ServerComboAttributes.AttributeList;
			TabGenerator.MaxElementsToCopyInCustomMethods = 2;
			TabGenerator.GenerateGrid = delegate(ref int line, TabSettings<string> settings) {
				settings.GeneralProperties.AddProperty(ServerComboAttributes.Id, 0, 1, null, true);
				settings.GeneralProperties.AddLabel(ServerComboAttributes.Script, 1, 0);
				settings.GeneralProperties.AddProperty(ServerComboAttributes.Script, 1, 1);
				ServerComboAttributes.Display.AttachedObject = settings.Gdb;
			};
			TabGenerator.OnInitSettings = delegate(GDbTabWrapper<string, ReadableTuple<string>> tab, GTabSettings<string, ReadableTuple<string>> settings, BaseDb gdb) { settings.AttIdWidth = 80; };
			DbWriter = DbIOMethods.DbWriterComma;
		}
	}

	public class DbItemCombos2 : DbItemCombos {
		public DbItemCombos2() {
			DbSource = ServerDbs.Combos2;
			ThrowFileNotFoundException = false;
		}
	}

	public class DbItemGroups : AbstractDb<int> {
		public DbItemGroups() {
			DbSource = ServerDbs.ItemGroups;
			AttributeList = ServerItemGroupAttributes.AttributeList;
			DbLoader = DbIOItemGroups.Loader;
			DbWriter = DbIOItemGroups.Writer;
			TabGenerator.OnTabVisualUpdate += GTabsMaker.LoadSItemGroupVisualUpdate;
			TabGenerator.OnInitSettings += delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
				//settings.CanBeDelayed = false;
				ServerItemGroupAttributes.Display.AttachedObject = gdb;
			};
			TabGenerator.GenerateGrid = delegate(ref int line, TabSettings<int> settings) { settings.GeneralProperties.AddProperty(ServerItemGroupAttributes.Table, 0, 1); };
			TabGenerator.OnSetCustomCommands = delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard (rAthena)",
					ImagePath = "export.png",
					Shortcut = ApplicationShortcut.Copy,
					InsertIndex = 3,
					AddToCommandsStack = false,
					GenericCommand = items => GTabsMaker.ItemGroupCopyEntries(items, gdb, settings.Control, ServerType.RAthena)
				});

				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard (Hercules)",
					ImagePath = "export.png",
					Shortcut = ApplicationShortcut.Copy2,
					InsertIndex = 4,
					AddToCommandsStack = false,
					GenericCommand = items => GTabsMaker.ItemGroupCopyEntries(items, gdb, settings.Control, ServerType.Hercules)
				});
			};
		}

		protected override void _loadDb() {
			DbDebugItem<int> debug = new DbDebugItem<int>(this);

			if (DbPathLocator.GetServerType() == ServerType.RAthena) {
				if (debug.Load(ServerDbs.ItemGroupsBlueBox)) DbIOItemGroups.Loader(debug, this);
				if (debug.Load(ServerDbs.ItemGroupsVioletBox)) DbIOItemGroups.Loader(debug, this);
				if (debug.Load(ServerDbs.ItemGroupsCardalbum)) DbIOItemGroups.Loader(debug, this);
				if (debug.Load(ServerDbs.ItemGroupsFindingore)) DbIOItemGroups.Loader(debug, this);
				if (debug.Load(ServerDbs.ItemGroupsGiftBox)) DbIOItemGroups.Loader(debug, this);
				if (debug.Load(ServerDbs.ItemGroupsMisc)) DbIOItemGroups.Loader(debug, this);

				if (DbPathLocator.GetIsRenewal())
					if (debug.Load(ServerDbs.ItemGroupsPackages)) DbIOItemGroups.Loader(debug, this);
			}
			else if (DbPathLocator.GetServerType() == ServerType.Hercules) {
				if (debug.Load(ServerDbs.ItemGroups)) DbIOItemGroups.Loader(debug, this);
				//if (debug.Load(ServerDbs.ItemGroupsPackages)) DbIOItemGroups.Loader(debug, this);
			}
		}

		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			DbDebugItem<int> debug = new DbDebugItem<int>(this);

			if (DbPathLocator.GetServerType() == ServerType.RAthena) {
				if (debug.Write(ServerDbs.ItemGroupsBlueBox, dbPath, subPath, serverType, fileType)) DbIOItemGroups.Writer(debug, this);
				if (debug.Write(ServerDbs.ItemGroupsVioletBox, dbPath, subPath, serverType, fileType)) DbIOItemGroups.Writer(debug, this);
				if (debug.Write(ServerDbs.ItemGroupsCardalbum, dbPath, subPath, serverType, fileType)) DbIOItemGroups.Writer(debug, this);
				if (debug.Write(ServerDbs.ItemGroupsFindingore, dbPath, subPath, serverType, fileType)) DbIOItemGroups.Writer(debug, this);
				if (debug.Write(ServerDbs.ItemGroupsGiftBox, dbPath, subPath, serverType, fileType)) DbIOItemGroups.Writer(debug, this);
				if (debug.Write(ServerDbs.ItemGroupsMisc, dbPath, subPath, serverType, fileType)) DbIOItemGroups.Writer(debug, this);

				if (DbPathLocator.GetIsRenewal())
					if (debug.Write(ServerDbs.ItemGroupsPackages, dbPath, subPath, serverType, fileType)) DbIOItemGroups.Writer(debug, this);
			}
			else if (DbPathLocator.GetServerType() == ServerType.Hercules) {
				if (debug.Write(ServerDbs.ItemGroups, dbPath, subPath, serverType, fileType)) DbIOItemGroups.Writer(debug, this);
				//if (debug.Write(ServerDbs.ItemGroupsPackages, dbPath, subPath, serverType, fileType)) DbIOItemGroups.Writer(debug, this);
			}
		}
	}

	public class DbConstants : AbstractDb<string> {
		public DbConstants() {
			DbSource = ServerDbs.Constants;
			AttributeList = ServerConstantsAttributes.AttributeList;
			DbLoader = DbIOConstants.Loader;
			DbWriter = DbIOConstants.Writer;
			TabGenerator.OnDatabaseReloaded += delegate { TabGenerator.Show(ServerType.Hercules, null, ServerConstantsAttributes.Deprecated); };
			TabGenerator.OnSetCustomCommands = delegate(GDbTabWrapper<string, ReadableTuple<string>> tab, GTabSettings<string, ReadableTuple<string>> settings, BaseDb gdb) {
				settings.AddedCommands.Add(new GItemCommand<string, ReadableTuple<string>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard (txt)",
					ImagePath = "export.png",
					InsertIndex = 4,
					Shortcut = ApplicationShortcut.Copy,
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<string>> items) {
						StringBuilder builder = new StringBuilder();

						for (int i = 0; i < items.Count; i++) {
							ReadableTuple<string> tuple = items[i];
							int item2 = tuple.GetValue<int>(2);

							if (item2 == 0)
								builder.AppendLine(string.Join("\t", tuple.GetRawElements().Take(2).Select(p => (p ?? "").ToString()).ToArray()));
							else
								builder.AppendLine(string.Join("\t", tuple.GetRawElements().Take(3).Select(p => (p ?? "").ToString()).ToArray()));
						}

						Clipboard.SetDataObject(builder.ToString());
					}
				});

				settings.AddedCommands.Add(new GItemCommand<string, ReadableTuple<string>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard (conf)",
					ImagePath = "export.png",
					InsertIndex = 5,
					Shortcut = ApplicationShortcut.Copy2,
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<string>> items) {
						StringBuilder builder = new StringBuilder();

						foreach (var item in items) {
							builder.Append("\t");
							builder.Append(item.GetValue<string>(ServerConstantsAttributes.Id));
							builder.Append(": ");
							DbIOConstants.WriteEntry(builder, item);
							builder.AppendLine();
						}

						Clipboard.SetDataObject(builder.ToString());
					}
				});
			};
		}
	}

	public class DbHomuns : AbstractDb<int> {
		public DbHomuns() {
			LayoutIndexes = new int[] { 0, 10 };
			DbSource = ServerDbs.Homuns;
			AttributeList = ServerHomunAttributes.AttributeList;
			TabGenerator.OnGenerateGrid = delegate(ref int line, TabSettings<int> settings) {
				settings.GeneralProperties.AddLabel("Base stats", null, line++, 0, true);
				GTabsMaker.PrintGrid(ref line, 0, 1, 2, new DefaultIndexProvider(ServerHomunAttributes.BHp.Index, 8), -1, 0, -1, 0, settings.GeneralProperties, settings.Gdb.AttributeList);
				settings.GeneralProperties.AddLabel("Growth stats", null, line, 0, true);
				settings.GeneralProperties.AddLabel("Evolution stats", null, line, 3, true);
				line++;
				GTabsMaker.PrintGrid(ref line, 0, 1, 2, new DefaultIndexProvider(ServerHomunAttributes.GnHp.Index, 16), -1, 0, -1, 0, settings.GeneralProperties, settings.Gdb.AttributeList);
				line--;
				GTabsMaker.PrintGrid(ref line, 3, 1, 2, new DefaultIndexProvider(ServerHomunAttributes.EnHp.Index, 16), -1, 0, -1, 0, settings.GeneralProperties, settings.Gdb.AttributeList);
			};
			DbWriter = DbIOMethods.DbWriterComma;
			TabGenerator.OnSetCustomCommands += delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					Visibility = gdb.DbSource == ServerDbs.Homuns ? Visibility.Visible : Visibility.Collapsed,
					AllowMultipleSelection = false,
					DisplayName = "Copy to [" + ServerDbs.Homuns2.DisplayName + "]...",
					ImagePath = "convert.png",
					InsertIndex = 3,
					Shortcut = ApplicationShortcut.CopyTo2,
					AddToCommandsStack = false,
					GenericCommand = tuple => tab.CopyItemTo(GetDb<int>(ServerDbs.Homuns2))
				});
			};
		}
	}

	public class DbHomuns2 : DbHomuns {
		public DbHomuns2() {
			DbSource = ServerDbs.Homuns2;
			ThrowFileNotFoundException = false;
		}
	}

	public class DbSkillRequirements : AbstractDb<int> {
		public DbSkillRequirements() {
			LayoutIndexes = new int[] {
				0, 11, ServerSkillRequirementsAttributes.SpiritSphereCost.Index, 1,
				ServerSkillRequirementsAttributes.RequiredItemID1.Index, 20,
				ServerSkillRequirementsAttributes.RequiredStatuses.Index, 1, ServerSkillRequirementsAttributes.RequiredEquipment.Index, 1
			};
			TabGenerator.MaxElementsToCopyInCustomMethods = ServerSkillRequirementsAttributes.AttributeList.Attributes.Count - 1;
			DbSource = ServerDbs.SkillsRequirement;
			AttributeList = ServerSkillRequirementsAttributes.AttributeList;
			ThrowFileNotFoundException = false;
			ServerSkillRequirementsAttributes.Display.AttachedObject = this;
			DbWriter = DbIOMethods.DbWriterComma;
			//TabGenerator.OnSetCustomCommands = GTabsMaker.CopyEntriesToClipboardFunctionInt;
			TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromSkillDb;
		}
	}

	public class DbSkills : AbstractDb<int> {
		private int _format = -1;

		public DbSkills() {
			LayoutSearch = new DbAttribute[] {
				ServerSkillAttributes.Id, ServerSkillAttributes.Name,
				ServerSkillAttributes.Desc, ServerSkillAttributes.HitMode,
				ServerSkillAttributes.Element, ServerSkillAttributes.AttackType
			};
			LayoutIndexes = new int[] {
				0, 19,
				-1, 1,
				19, 2,
				ServerSkillAttributes.CastingTime.Index, 1, ServerSkillAttributes.Cooldown.Index, 1,
				ServerSkillAttributes.AfterCastActDelay.Index, 4,
				ServerSkillAttributes.FixedCastTime.Index, 1
			};
			TabGenerator.MaxElementsToCopyInCustomMethods = 18;
			DbSource = ServerDbs.Skills;
			AttributeList = ServerSkillAttributes.AttributeList;
			TabGenerator.OnPreviewTabInitialize = delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) { settings.AttDisplay = ServerSkillAttributes.Desc; };
			TabGenerator.OnSetCustomCommands = delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
				GTabsMaker.SelectFromSkillRequirementsDb(tab, settings, gdb);
				
				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard (yml)",
					ImagePath = "export.png",
					InsertIndex = 4,
					Shortcut = ApplicationShortcut.Copy,
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<int>> items) {
						var itemDb = GetMeta<int>(ServerDbs.Items);
						StringBuilder builder = new StringBuilder();

						foreach (var item in items) {
							DbIOSkills.WriteEntryYaml(builder, item, itemDb);
						}

						Clipboard.SetDataObject(builder.ToString());
					}
				});
			};
			TabGenerator.GenerateGrid = null;
			TabGenerator.OnPreviewTabVisualUpdate = _generateTab;
		}

		private void _generateTab(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
			string path = DbPathLocator.DetectPath(settings.DbData);
			int format = -1;

			if (path.IsExtension(".yml")) {
				format = 1;
			}
			else {
				format = 0;
			}

			if (tab.IsDeployed && _format == format)
				return;

			_format = format;

			if (format == 1) {
				LayoutIndexes = new int[] {
					ServerSkillAttributes.Id.Index, 1, ServerSkillAttributes.Range.Index, 1,
					ServerSkillAttributes.HitMode.Index, 1, ServerSkillAttributes.SkillTargetType.Index, 1,
					ServerSkillAttributes.SkillElement.Index, 1, ServerSkillAttributes.DamageFlags.Index, 1,
					ServerSkillAttributes.SplashArea.Index, 1, ServerSkillAttributes.MaxLevel.Index, 1,
					ServerSkillAttributes.HitCount.Index, 1, ServerSkillAttributes.CastInterrupt.Index, 1,
					ServerSkillAttributes.DefReduc.Index, 1, ServerSkillAttributes.Inf2New.Index, 1,
					ServerSkillAttributes.ActiveInstance.Index, 1, ServerSkillAttributes.AttackType.Index, 1,
					ServerSkillAttributes.Knockback.Index, 1, -1, 1,
					ServerSkillAttributes.Name.Index, 1, ServerSkillAttributes.Desc.Index, 1,
					ServerSkillAttributes.DISPLAY_Casttime.Index, 1, -1, 1,
					ServerSkillAttributes.CastTimeFlags.Index, 1, ServerSkillAttributes.CastDelayFlags.Index, 1,
					ServerSkillAttributes.CastingTime.Index, 1, ServerSkillAttributes.Cooldown.Index, 1,
					ServerSkillAttributes.AfterCastActDelay.Index, 4,
					ServerSkillAttributes.FixedCastTime.Index, 1, -1, 1,
					ServerSkillAttributes.DISPLAY_Requirement.Index, 1, -1, 1,
					ServerSkillAttributes.RequireHpCost.Index, 1, ServerSkillAttributes.RequireSpCost.Index, 1,
					ServerSkillAttributes.RequireHpRateCost.Index, 1, ServerSkillAttributes.RequireSpRateCost.Index, 1,
					ServerSkillAttributes.RequireMaxHpTrigger.Index, 1, ServerSkillAttributes.RequireZenyCost.Index, 1,
					ServerSkillAttributes.RequireAmmoTypes.Index, 1, ServerSkillAttributes.RequireAmmoAmount.Index, 1,
					ServerSkillAttributes.RequireWeapons.Index, 1, ServerSkillAttributes.RequireItemCost.Index, 1,
					ServerSkillAttributes.RequireState.Index, 1, ServerSkillAttributes.RequireSpiritSphereCost.Index, 1,
					ServerSkillAttributes.RequireStatuses.Index, 1, ServerSkillAttributes.RequiredEquipment.Index, 1,
					ServerSkillAttributes.DISPLAY_Unit.Index, 1, -1, 1,
					ServerSkillAttributes.UnitId.Index, 1, ServerSkillAttributes.UnitAlternateId.Index, 1,
					ServerSkillAttributes.UnitLayout.Index, 1, ServerSkillAttributes.UnitRange.Index, 1,
					ServerSkillAttributes.UnitInterval.Index, 1, ServerSkillAttributes.UnitTarget.Index, 1,
					ServerSkillAttributes.UnitFlag.Index, 1, -1, 1,
					ServerSkillAttributes.DISPLAY_Others.Index, 1, -1, 1,
					ServerSkillAttributes.CopyFlags.Index, 1, ServerSkillAttributes.CopyFlagsRemovedRequirement.Index, 1,
					ServerSkillAttributes.NoNearNPCRange.Index, 1, ServerSkillAttributes.NoNearNPCType.Index, 1,
				};

				tab.Clear();
				TabGenerator.Settings.GeneralProperties.Clear();
				tab.Settings.AttDisplay = ServerPetAttributes.MobId;

				int line = 0;
				TabGenerator.GenerateGridDefault(ref line, TabGenerator.Settings);
				tab.TabChanged();
			}
			else if (format == 0) {
				LayoutIndexes = new int[] {
					0, 19,
					-1, 1,
					19, 2,
					ServerSkillAttributes.CastingTime.Index, 1, ServerSkillAttributes.Cooldown.Index, 1,
					ServerSkillAttributes.AfterCastActDelay.Index, 4,
					ServerSkillAttributes.FixedCastTime.Index, 1
				};

				tab.Clear();
				TabGenerator.Settings.GeneralProperties.Clear();

				int line = 0;
				TabGenerator.GenerateGridDefault(ref line, TabGenerator.Settings);
				tab.TabChanged();
			}
		}

		public override void OnLoadDataFromClipboard(DbDebugItem<int> debug, string text, string path, AbstractDb<int> abstractDb) {
			if (debug.FileType == FileType.Yaml) {
				DbIOSkills.Loader(debug, this);
			}
			else {
				base.OnLoadDataFromClipboard(debug, text, path, abstractDb);
			}
		}

		protected override void _loadDb() {
			//string path = DbPathLocator.DetectPath(settings.DbData);
			Attached["NumberOfAttributesToGuess"] = 18;
			DbDebugItem<int> debug = new DbDebugItem<int>(this);

			if (debug.Load(DbSource)) {
				if (debug.FilePath.IsExtension(".yml")) {
					DbIOSkills.Loader(debug, this);
				}
				else {
					DbIOMethods.DbLoaderComma(debug, this);

					if (debug.Load(ServerDbs.SkillsNoDex)) DbIOMethods.DbLoaderCommaRange(debug, AttributeList, ServerSkillAttributes.Cast.Index, 2);

					if (DbPathLocator.GetServerType() == ServerType.RAthena)
						if (debug.Load(ServerDbs.SkillsNoCast)) DbIOMethods.DbLoaderCommaNoCast(debug, AttributeList, ServerSkillAttributes.Flag.Index, 1);

					if (debug.Load(ServerDbs.SkillsCast)) {
						DbIOMethods.DbLoaderCommaRange(debug, AttributeList, ServerSkillAttributes.Cast.Index + 2, DbPathLocator.GetIsRenewal() ? 7 : 6);
					}
				}
			}
		}

		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			DbDebugItem<int> debug = new DbDebugItem<int>(this);

			if (debug.Write(dbPath, subPath, serverType, fileType)) {
				if (debug.FilePath.IsExtension(".yml")) {
					DbIOSkills.Writer(debug, this);
				}
				else {
					DbIOMethods.DbWriterComma(debug, this, 0, 18);

					debug.DbSource = ServerDbs.SkillsNoDex;
					if (debug.Write(dbPath, subPath, serverType, fileType)) {
						DbIOSkills.DbSkillsNoDexCommaRange(debug, this, ServerSkillAttributes.Cast.Index, 2);
					}

					debug.DbSource = ServerDbs.SkillsNoCast;
					if (serverType == ServerType.RAthena && debug.Write(dbPath, subPath, serverType, fileType)) {
						DbIOSkills.DbSkillsNoCastCommaRange(debug, this, ServerSkillAttributes.Flag.Index, 1);
					}

					debug.DbSource = ServerDbs.SkillsCast;
					if (debug.Write(dbPath, subPath, serverType, fileType)) {
						DbIOSkills.DbSkillsCastCommaRange(debug, this, ServerSkillAttributes.Cast.Index + 2, DbPathLocator.GetIsRenewal() ? 7 : 6);
					}
				}
			}
		}
	}

	public class DbSkills2 : DbSkills {
		public DbSkills2() {
			DbSource = ServerDbs.Skills2;
			ThrowFileNotFoundException = false;
		}

		protected override void _loadDb() {
			string path = DbPathLocator.DetectPath(DbSource);

			if (!path.IsExtension(".yml"))
				return;

			base._loadDb();
		}

		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			string path = DbPathLocator.DetectPath(DbSource);

			if (!path.IsExtension(".yml"))
				return;

			base.WriteDb(dbPath, subPath, serverType, fileType);
		}
	}

	public class DbMobSkills : AbstractDb<string> {
		public DbMobSkills() {
			LayoutIndexes = new int[] {
				1, ServerMobSkillAttributes.AttributeList.Attributes.Count
			};
			DbSource = ServerDbs.MobSkills;
			AttributeList = ServerMobSkillAttributes.AttributeList;
			DbLoader = (q, r) => DbIOMethods.DbLoaderAny(q, r, TextFileHelper.GetElementsByCommas, false);
			DbWriter = DbIOMobSkills.Writer;
			DbWriterSql = SqlParser.DbSqlMobSkills;
			TabGenerator.OnInitSettings += delegate(GDbTabWrapper<string, ReadableTuple<string>> tab, GTabSettings<string, ReadableTuple<string>> settings, BaseDb gdb) {
				settings.CanChangeId = false;
				settings.CustomAddItemMethod = delegate {
					try {
						string id = Methods.RandomString(32);

						ReadableTuple<string> item = new ReadableTuple<string>(id, settings.AttributeList);
						item.Added = true;

						Table.Commands.AddTuple(id, item, false);
						tab._listView.ScrollToCenterOfView(item);
					}
					catch (KeyInvalidException) {
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				};
			};
			TabGenerator.StartIndexInCustomMethods = 1;
			TabGenerator.OnInitSettings += delegate(GDbTabWrapper<string, ReadableTuple<string>> tab, GTabSettings<string, ReadableTuple<string>> settings, BaseDb gdb) {
				settings.AttributeList = gdb.AttributeList;
				settings.AttId = gdb.AttributeList.Attributes[1];
				settings.AttDisplay = gdb.AttributeList.Attributes[2];
				settings.AttIdWidth = 60;
			};
			TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromMobDbString;
			TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromSkillDbString;
			TabGenerator.OnSetCustomCommands += delegate(GDbTabWrapper<string, ReadableTuple<string>> tab, GTabSettings<string, ReadableTuple<string>> settings, BaseDb gdb) {
				settings.AddedCommands.Add(new GItemCommand<string, ReadableTuple<string>> {
					Visibility = gdb.DbSource == ServerDbs.MobSkills ? Visibility.Visible : Visibility.Collapsed,
					AllowMultipleSelection = false,
					DisplayName = "Copy to [" + ServerDbs.MobSkills2.DisplayName + "]...",
					ImagePath = "convert.png",
					InsertIndex = 3,
					Shortcut = ApplicationShortcut.CopyTo2,
					AddToCommandsStack = false,
					GenericCommand = tuple => tab.CopyItemTo(GetDb<string>(ServerDbs.MobSkills2))
				});
			};
		}
	}

	public class DbMobSkills2 : DbMobSkills {
		public DbMobSkills2() {
			DbSource = ServerDbs.MobSkills2;
			ThrowFileNotFoundException = false;
		}
	}

	public class DbMobGroups : AbstractDb<int> {
		private readonly ServerDbs[] _serverDbs;

		public DbMobGroups() {
			DbSource = ServerDbs.MobGroups;
			AttributeList = ServerMobGroupAttributes.AttributeList;

			_serverDbs = new ServerDbs[] {
				ServerDbs.MobBranch,
				ServerDbs.MobPoring,
				ServerDbs.MobBoss,
				ServerDbs.RedPouch,
				ServerDbs.Classchange
			};

			TabGenerator.OnTabVisualUpdate += GTabsMaker.LoadSItemGroupVisualUpdate;
			TabGenerator.GenerateGrid = delegate(ref int line, TabSettings<int> settings) { settings.GeneralProperties.AddProperty(ServerMobGroupAttributes.Table, 0, 1); };

			TabGenerator.OnSetCustomCommands += delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = false,
					DisplayName = "Select in explorer",
					ImagePath = "arrowdown.png",
					InsertIndex = 3,
					AddToCommandsStack = false,
					GenericCommand = tuples => {
						if (tuples.Count > 0) {
							int id = tuples[0].GetValue<int>(ServerMobGroupAttributes.Id);

							if (id >= _serverDbs.Length || id < 0)
								return;

							try {
								string path = DbPathLocator.DetectPath(_serverDbs[id]);

								if (path != null) {
									if (IOHelper.IsSystemFile(path))
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
					}
				});
			};
		}

		protected override void _loadDb() {
			// Loads all the other tables
			DbDebugItem<int> debug = new DbDebugItem<int>(this);

			for (int i = 0; i < _serverDbs.Length; i++)
				if (debug.Load(_serverDbs[i])) DbIOMobGroups.Loader(debug, this, i);
		}

		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			DbDebugItem<int> debug = new DbDebugItem<int>(this);

			for (int i = 0; i < _serverDbs.Length; i++) {
				debug.DbSource = _serverDbs[i];
				if (debug.Write(dbPath, subPath, serverType, fileType)) DbIOMobGroups.Writer(debug, this, i);
			}
		}
	}

	public class DbQuest : AbstractDb<int> {
		private int _format = -1;

		public DbQuest() {
			DbSource = ServerDbs.Quests;
			TabGenerator.OnSetCustomCommands += (tab, settings, g) => settings.AddedCommands.Add(GTabsMaker.GenerateSelectFrom(ServerDbs.CQuests, tab));
			TabGenerator.OnDatabaseReloaded += delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
				TabGenerator.Show(this.GetDb<int>(ServerDbs.Quests).GetAttacked<int>("rAthenaFormat") == 18 || DbPathLocator.DetectPath(settings.DbData).IsExtension(".yml") || DbPathLocator.GetServerType() == ServerType.Hercules,
					ServerQuestsAttributes.MobId1,
					ServerQuestsAttributes.NameId1,
					ServerQuestsAttributes.Rate1,
					ServerQuestsAttributes.Count1,
					ServerQuestsAttributes.MobId2,
					ServerQuestsAttributes.NameId2,
					ServerQuestsAttributes.Rate2,
					ServerQuestsAttributes.Count2,
					ServerQuestsAttributes.MobId3,
					ServerQuestsAttributes.NameId3,
					ServerQuestsAttributes.Rate3,
					ServerQuestsAttributes.Count3
					);
			};
			TabGenerator.OnSetCustomCommands = delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard (txt or yml)",
					ImagePath = "export.png",
					InsertIndex = 4,
					Shortcut = ApplicationShortcut.Copy,
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<int>> items) {
						if (_format == 1) {
							var itemDb = GetMeta<int>(ServerDbs.Items);
							var mobDb = GetMeta<int>(ServerDbs.Mobs);
							StringBuilder builder = new StringBuilder();

							foreach (var item in items) {
								DbIOQuests.WriteEntryYaml(builder, item, itemDb, mobDb);
							}

							Clipboard.SetDataObject(builder.ToString());
						}
						else {
							TabGenerator<int>.CopyTuplesDefault(TabGenerator, items, ServerQuestsAttributes.AttributeList.Take(18).ToArray());
						}
					}
				});

				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard (conf)",
					ImagePath = "export.png",
					InsertIndex = 5,
					Shortcut = ApplicationShortcut.Copy2,
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<int>> items) {
						StringBuilder builder = new StringBuilder();

						foreach (var item in items) {
							DbIOQuests.WriteEntry(builder, item);
							builder.AppendLine();
						}

						Clipboard.SetDataObject(builder.ToString());
					}
				});
			};
			AttributeList = ServerQuestsAttributes.AttributeList;
			DbLoader = DbIOQuests.Loader;
			DbWriter = DbIOQuests.Writer;
			TabGenerator.GenerateGrid = null;
			TabGenerator.OnPreviewTabVisualUpdate = _generateTab;
		}

		private void _generateTab(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
			string path = DbPathLocator.DetectPath(settings.DbData);
			int format = -1;

			if (path.IsExtension(".yml")) {
				format = 1;
			}
			else {
				format = 0;
			}

			if (tab.IsDeployed && _format == format)
				return;

			_format = format;

			if (format == 1) {
				LayoutIndexes = new int[] {
					ServerQuestsAttributes.Id.Index, 1, ServerQuestsAttributes.TimeLimitNew.Index, 1,
					ServerQuestsAttributes.QuestTitle.Index, 1, -1, 1,
					-2, 1, -2, 1,
					ServerQuestsAttributes.TargetId1.Index, 1, ServerQuestsAttributes.Val1.Index, 1,
					ServerQuestsAttributes.Id1.Index, 1, ServerQuestsAttributes.Race1.Index, 1,
					ServerQuestsAttributes.Size1.Index, 1, ServerQuestsAttributes.Element1.Index, 1,
					ServerQuestsAttributes.MinLevel1.Index, 1, ServerQuestsAttributes.MaxLevel1.Index, 1,
					-2, 1, -2, 1,
					ServerQuestsAttributes.TargetId2.Index, 1, ServerQuestsAttributes.Val2.Index, 1,
					ServerQuestsAttributes.Id2.Index, 1, ServerQuestsAttributes.Race2.Index, 1,
					ServerQuestsAttributes.Size2.Index, 1, ServerQuestsAttributes.Element2.Index, 1,
					ServerQuestsAttributes.MinLevel2.Index, 1, ServerQuestsAttributes.MaxLevel2.Index, 1,
					-2, 1, -2, 1,
					ServerQuestsAttributes.TargetId3.Index, 1, ServerQuestsAttributes.Val3.Index, 1,
					ServerQuestsAttributes.Id3.Index, 1, ServerQuestsAttributes.Race3.Index, 1,
					ServerQuestsAttributes.Size3.Index, 1, ServerQuestsAttributes.Element3.Index, 1,
					ServerQuestsAttributes.MinLevel3.Index, 1, ServerQuestsAttributes.MaxLevel3.Index, 1,
					-2, 1, -2, 1,
					ServerQuestsAttributes.MobId1.Index, 1, ServerQuestsAttributes.NameId1.Index, 1,
					ServerQuestsAttributes.Rate1.Index, 1, ServerQuestsAttributes.Count1.Index, 1,
					-2, 1, -2, 1,
					ServerQuestsAttributes.MobId2.Index, 1, ServerQuestsAttributes.NameId2.Index, 1,
					ServerQuestsAttributes.Rate2.Index, 1, ServerQuestsAttributes.Count2.Index, 1,
					-2, 1, -2, 1,
					ServerQuestsAttributes.MobId3.Index, 1, ServerQuestsAttributes.NameId3.Index, 1,
					ServerQuestsAttributes.Rate3.Index, 1, ServerQuestsAttributes.Count3.Index, 1,
				};

				tab.Clear();
				TabGenerator.Settings.GeneralProperties.Clear();
				tab.Settings.AttDisplay = ServerPetAttributes.MobId;

				int line = 0;
				TabGenerator.GenerateGridDefault(ref line, TabGenerator.Settings);
				tab.TabChanged();
			}
			else if (format == 0) {
				LayoutIndexes = new[] {
					0, 1, 1, 1,
					2, 1, 3, 1,
					4, 1, 5, 1,
					6, 1, 7, 1,
					8, 1, 9, 1,
					10, 1, -1, 1,
					11, 1, 12, 1,
					13, 1, -1, 1,
					14, 1, 15, 1,
					16, 1, -1, 1,
					17, 1, -1, 1
				};

				tab.Clear();
				TabGenerator.Settings.GeneralProperties.Clear();

				int line = 0;
				TabGenerator.GenerateGridDefault(ref line, TabGenerator.Settings);
				tab.TabChanged();
			}
		}
	}

	public class DbQuest2 : DbQuest {
		public DbQuest2() {
			DbSource = ServerDbs.Quests2;
			ThrowFileNotFoundException = false;
		}
	}

	public class DbClientCheevo : AbstractDb<int> {
		public DbClientCheevo() {
			LayoutIndexes = new[] {
				0, 1, -1, 1,
				1, 12
			};

			DbSource = ServerDbs.CCheevo;
			AttributeList = ClientCheevoAttributes.AttributeList;
			TabGenerator.IsTabEnabledMethod = (e, a) => ProjectConfiguration.SynchronizeWithClientDatabases;
			TabGenerator.OnSetCustomCommands += (tab, settings, g) => settings.AddedCommands.Add(GTabsMaker.GenerateSelectFrom(ServerDbs.Cheevo, tab));
			DbLoader = DbIOClientCheevo.Loader;
			DbWriter = DbIOClientCheevo.Writer;

			TabGenerator.OnSetCustomCommands = delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
				var command = GTabsMaker.GenerateSelectFrom(ServerDbs.Cheevo, tab);
				command.Shortcut = ApplicationShortcut.Select;
				settings.AddedCommands.Add(command);

				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard",
					ImagePath = "export.png",
					InsertIndex = 5,
					Shortcut = ApplicationShortcut.Copy,
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<int>> items) {
						StringBuilder builder = new StringBuilder();
						var db = gdb.To<int>();

						foreach (var item in items) {
							DbIOClientCheevo.WriteEntry(builder, item, false);
						}

						Clipboard.SetDataObject(builder.ToString());
					}
				});

				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Autocomplete (from Server data)",
					ImagePath = "imconvert.png",
					InsertIndex = 5,
					Shortcut = ApplicationShortcut.FromString("Ctrl-G", "Autocomplete"),
					AddToCommandsStack = true,
					Command = delegate(ReadableTuple<int> item) {
						try {
							int id = item.GetKey<int>();

							var cheevoDb = tab.GetDb<int>(ServerDbs.Cheevo);

							var tupleServer = cheevoDb.Table.TryGetTuple(id);

							if (tupleServer == null)
								return null;

							return CheevoGeneratorEngine.Generate(item, tupleServer);
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
							return null;
						}
					}
				});
			};
		}

		public override void OnLoadDataFromClipboard(DbDebugItem<int> debug, string text, string path, AbstractDb<int> abstractDb) {
			// The text comes from UTF-8 (clipboard), needs to be converted back to its proper encoding.
			if (EncodingService.Ansi.GetString(EncodingService.Ansi.GetBytes(text)) == text) {
				File.WriteAllText(path, text, EncodingService.Ansi);
			}

			if (EncodingService.Korean.GetString(EncodingService.Korean.GetBytes(text)) == text) {
				File.WriteAllText(path, text, EncodingService.Korean);
			}

			DbIOClientCheevo.Loader(this, path);
		}

		protected override void _loadDb() {
			Table.Clear();
			if (ProjectConfiguration.SynchronizeWithClientDatabases) {
				TextFileHelper.LatestFile = DbSource;
				DbLoader(null, this);
			}
		}

		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			if (ProjectConfiguration.SynchronizeWithClientDatabases) {
				if (!IsEnabled) return;

				TextFileHelper.LatestFile = DbSource;
				DbWriter(null, this);
			}
		}
	}

	public class DbClientQuest : AbstractDb<int> {
		public DbClientQuest() {
			DbSource = ServerDbs.CQuests;
			AttributeList = ClientQuestsAttributes.AttributeList;
			TabGenerator.IsTabEnabledMethod = (e, a) => ProjectConfiguration.SynchronizeWithClientDatabases;
			TabGenerator.OnSetCustomCommands += (tab, settings, g) => settings.AddedCommands.Add(GTabsMaker.GenerateSelectFrom(ServerDbs.Quests, tab));
			DbLoader = DbIOClientQuests.Loader;
			DbWriter = DbIOClientQuests.Writer;
		}

		protected override void _loadDb() {
			Table.Clear();
			if (ProjectConfiguration.SynchronizeWithClientDatabases) {
				TextFileHelper.LatestFile = DbSource;
				DbLoader(null, this);
			}
		}

		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			if (ProjectConfiguration.SynchronizeWithClientDatabases) {
				if (!IsEnabled) return;

				TextFileHelper.LatestFile = DbSource;
				DbWriter(null, this);
			}
		}
	}

	public class DbCheevo : AbstractDb<int> {
		private int _format;

		public DbCheevo() {
			LayoutIndexes = new[] {
				0, 1, -1, 1,
				1, 9, -1, 1,
				10, 13
			};

			ThrowFileNotFoundException = false;
			DbSource = ServerDbs.Cheevo;
			AttributeList = ServerCheevoAttributes.AttributeList;
			DbLoader = DbIOCheevo.Loader;
			DbWriter = DbIOCheevo.Writer;

			TabGenerator.OnSetCustomCommands = delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard (yml)",
					ImagePath = "export.png",
					InsertIndex = 4,
					Shortcut = ApplicationShortcut.Copy,
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<int>> items) {
						StringBuilder builder = new StringBuilder();

						foreach (var item in items) {
							DbIOCheevo.WriteEntryYaml(builder, item);
							builder.AppendLine();
						}

						Clipboard.SetDataObject(builder.ToString());
					}
				});

				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard (conf)",
					ImagePath = "export.png",
					InsertIndex = 5,
					Shortcut = ApplicationShortcut.Copy2,
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<int>> items) {
						StringBuilder builder = new StringBuilder();

						foreach (var item in items) {
							DbIOCheevo.WriteEntry(builder, item);
							builder.AppendLine();
						}

						Clipboard.SetDataObject(builder.ToString());
					}
				});

				settings.AddedCommands.Add(new GItemCommand<int, ReadableTuple<int>> {
					AllowMultipleSelection = true,
					DisplayName = String.Format("Add in [{0}]", ServerDbs.CCheevo.DisplayName),
					ImagePath = "add.png",
					InsertIndex = 7,
					Shortcut = ApplicationShortcut.FromString("Ctrl-Alt-E", String.Format("Add in [{0}]", ServerDbs.CCheevo.DisplayName)),
					AddToCommandsStack = false,
					GenericCommand = delegate(List<ReadableTuple<int>> items) {
						var cCheevoDb = tab.GetDb<int>(ServerDbs.CCheevo);

						try {
							cCheevoDb.Table.Commands.Begin();

							foreach (var item in items) {
								int key = item.GetKey<int>();

								if (!cCheevoDb.Table.ContainsKey(key)) {
									ReadableTuple<int> tuple = new ReadableTuple<int>(key, ClientCheevoAttributes.AttributeList);
									tuple.Added = true;
									cCheevoDb.Table.Commands.AddTuple(key, tuple, false);

									var cmds = CheevoGeneratorEngine.Generate(tuple, item, true);

									if (cmds != null)
										cCheevoDb.Table.Commands.StoreAndExecute(cmds);
								}
							}
						}
						finally {
							cCheevoDb.Table.Commands.EndEdit();
						}
					},
				});

				var command = GTabsMaker.GenerateSelectFrom(ServerDbs.CCheevo, tab);
				command.Shortcut = ApplicationShortcut.Select;
				settings.AddedCommands.Add(command);
			};

			TabGenerator.GenerateGrid = null;
			TabGenerator.OnPreviewTabVisualUpdate = _generateTab;
		}

		private void _generateTab(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
			string path = DbPathLocator.DetectPath(settings.DbData);
			int format = -1;

			if (path.IsExtension(".yml")) {
				format = 1;
			}
			else {
				format = 0;
			}

			if (tab.IsDeployed && _format == format)
				return;

			_format = format;

			if (format == 1) {
				LayoutIndexes = new int[] {
					0, 1, -1, 1,
					1, 9, -1, 1,
					10, 14
				};

				tab.Clear();
				TabGenerator.Settings.GeneralProperties.Clear();

				int line = 0;
				TabGenerator.GenerateGridDefault(ref line, TabGenerator.Settings);
				tab.TabChanged();
			}
			else if (format == 0) {
				LayoutIndexes = new[] {
					0, 1, -1, 1,
					1, 9, -1, 1,
					10, 13
				};

				tab.Clear();
				TabGenerator.Settings.GeneralProperties.Clear();

				int line = 0;
				TabGenerator.GenerateGridDefault(ref line, TabGenerator.Settings);
				tab.TabChanged();
			}
		}
	}

	public class DbCheevo2 : DbCheevo {
		public DbCheevo2() {
			DbSource = ServerDbs.Cheevo2;
			ThrowFileNotFoundException = false;
		}
	}

	public class DbClientResource : AbstractDb<int> {
		public DbClientResource() {
			DbSource = ServerDbs.ClientResourceDb;
			AttributeList = ClientResourceAttributes.AttributeList;
			DbLoader = null;
			TabGenerator = null;
			IsGenerateTab = false;
		}

		protected override void _loadDb() {
			Table.Clear();
			TextFileHelper.LatestFile = DbSource;

			foreach (string[] elements in TextFileHelper.GetElements(Holder.Database.MetaGrf.GetData(@"data\idnum2itemresnametable.txt"))) {
				try {
					int itemId = Int32.Parse(elements[0]);
					Table.SetRaw(itemId, ClientResourceAttributes.ResourceName, elements[1]);
				}
				catch {
				}
			}
		}
	}
}