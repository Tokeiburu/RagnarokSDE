using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Database;
using Database.Commands;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Engines.DatabaseEngine;
using SDE.Tools.DatabaseEditor.Engines.Parsers;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using SDE.Tools.DatabaseEditor.Generic.DbWriters;
using SDE.Tools.DatabaseEditor.Generic.IndexProviders;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using SDE.Tools.DatabaseEditor.Generic.UI.CustomControls;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using Utilities;

namespace SDE.Tools.DatabaseEditor.Generic.Core {
	// debug.Load must ALWAYS be called when loading an item in the DB
	// This method creates a backup which will be used when saving the file
	// The same rule applies for debug.Write
	public class DbItems : AbstractDb<int> {
		public DbItems() {
			DbSource = ServerDbs.Items;
			AttributeList = ServerItemAttributes.AttributeList;
			DbLoader = DbLoaderMethods.DbItemsLoader;
			TabGenerator.GDbTabMaker = GTabsMaker.LoadSItemsTab<int>;
			DbWriter = DbWriterMethods.DbItemsCommaWriter;
		}

		protected override void _loadDb() {
			base._loadDb();

			if (AllLoaders.GetServerType() == ServerType.RAthena) {
				DbDebugItem<int> debug = new DbDebugItem<int>(this);
				// These are all being read twice and assigned to their respective table
				if (debug.Load(ServerDbs.ItemsAvail)) DbLoaderMethods.DbCommaRange(debug, AttributeList, ServerItemAttributes.Sprite.Index, 1, false);
				if (debug.Load(ServerDbs.ItemsDelay)) DbLoaderMethods.DbCommaRange(debug, AttributeList, ServerItemAttributes.Delay.Index, 1, false);
				if (debug.Load(ServerDbs.ItemsNoUse)) DbLoaderMethods.DbCommaLoader(debug, AttributeList, DbLoaderMethods.DbItemsNouseFunction, false);
				if (debug.Load(ServerDbs.ItemsStack)) DbLoaderMethods.DbCommaLoader(debug, AttributeList, DbLoaderMethods.DbItemsStackFunction, false);
				if (debug.Load(ServerDbs.ItemsTrade)) DbLoaderMethods.DbCommaLoader(debug, AttributeList, DbLoaderMethods.DbItemsTradeFunction, false);
				if (debug.Load(ServerDbs.ItemsBuyingStore)) DbLoaderMethods.DbCommaLoader(debug, AttributeList, DbLoaderMethods.DbItemsBuyingStoreFunction, false);
			}
		}

		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			DbDebugItem<int> debug = new DbDebugItem<int>(this);

			debug.DbSource = DbSource;
			if (debug.Write(dbPath, subPath, serverType, fileType)) {
				DbWriterMethods.DbItemsCommaWriter(debug, this);
			}

			if (serverType == ServerType.RAthena) {
				debug.DbSource = ServerDbs.ItemsAvail;
				if (debug.Write(dbPath, subPath, serverType, fileType)) {
					DbWriterMethods.DbItemsCommaRange(debug, this, ServerItemAttributes.Sprite.Index, 1, "");
				}

				debug.DbSource = ServerDbs.ItemsDelay;
				if (debug.Write(dbPath, subPath, serverType, fileType)) {
					DbWriterMethods.DbItemsCommaRange(debug, this, ServerItemAttributes.Delay.Index, 1, "");
				}

				debug.DbSource = ServerDbs.ItemsNoUse;
				if (debug.Write(dbPath, subPath, serverType, fileType)) {
					DbWriterMethods.DbItemsNouse(debug, this);
				}

				debug.DbSource = ServerDbs.ItemsStack;
				if (debug.Write(dbPath, subPath, serverType, fileType)) {
					DbWriterMethods.DbItemsStack(debug, this);
				}

				debug.DbSource = ServerDbs.ItemsTrade;
				if (debug.Write(dbPath, subPath, serverType, fileType)) {
					DbWriterMethods.DbItemsTrade(debug, this);
				}

				debug.DbSource = ServerDbs.ItemsBuyingStore;
				if (debug.Write(dbPath, subPath, serverType, fileType)) {
					DbWriterMethods.DbItemsBuyingStore(debug, this);
				}
			}
		}
	}

	public class DbItems2 : DbItems {
		public DbItems2() {
			DbSource = ServerDbs.Items2;
			ThrowFileNotFoundException = false;
			UsePreviousOutput = true;
		}
	}

	public class DbMobs : AbstractDb<int> {
		public DbMobs() {
			LayoutIndexes = new[] {
				new[] {0, -1, 1, -1, 2, -1, 3, -1}, null,
				new[] {
					ServerMobAttributes.Lv.Index, -1,
					ServerMobAttributes.Str.Index, ServerMobAttributes.Agi.Index,
					ServerMobAttributes.Vit.Index, ServerMobAttributes.Int.Index,
					ServerMobAttributes.Dex.Index, ServerMobAttributes.Luk.Index
				},
				new[] {
					ServerMobAttributes.Hp.Index, ServerMobAttributes.Sp.Index,
					ServerMobAttributes.Exp.Index, ServerMobAttributes.Jexp.Index,
					ServerMobAttributes.Atk1.Index, ServerMobAttributes.Atk2.Index,
					ServerMobAttributes.Def.Index, ServerMobAttributes.Mdef.Index
				},
				new[] {
					ServerMobAttributes.Race.Index, ServerMobAttributes.Range1.Index,
					ServerMobAttributes.Size.Index, ServerMobAttributes.Range2.Index,
					ServerMobAttributes.Element.Index, ServerMobAttributes.Range3.Index
				},
				new[] {
					ServerMobAttributes.Mexp.Index, ServerMobAttributes.DMotion.Index,
					ServerMobAttributes.Speed.Index, ServerMobAttributes.AMotion.Index,
					ServerMobAttributes.Mode.Index, ServerMobAttributes.ADelay.Index
				}
			};
			GridIndexes = new[] {
				null, null,
				new [] { 60, 0, -1, 0 },
				new [] { -1, 0, -1, 0 },
				new [] { 60, -115, 77, 0 },
				new [] { -1, 0, -1, 0 }
			};
			DbSource = ServerDbs.Mobs;
			AttributeList = ServerMobAttributes.AttributeList;
			DbWriterSql = SqlParser.DbSqlMobs;
			DbWriter = DbWriterMethods.DbIntComma;
			TabGenerator.OnGenerateGrid += delegate(ref int line, GenericDatabase database, TabControl control, DisplayableProperty<int, ReadableTuple<int>> generalProperties, BaseDb gdb) {
				generalProperties.AddCustomProperty(new CustomLinkedImage<int, ReadableTuple<int>>(generalProperties.GetComponent<TextBox>(2, 1), @"data\sprite\¸ó½ºÅÍ\", ".spr", 0, 3, 8, 2));
				generalProperties.SetRow(line, new GridLength(1, GridUnitType.Star));
				generalProperties.AddCustomProperty(new CustomQueryViewerMobOther<int, ReadableTuple<int>>(line, 1));
				generalProperties.AddCustomProperty(new CustomQueryViewerMobMvp<int, ReadableTuple<int>>(line));
				generalProperties.AddCustomProperty(new CustomQueryViewerMobSkills<int, ReadableTuple<int>>(line));
			};
		}
	}

	public class DbMobs2 : DbMobs {
		public DbMobs2() { DbSource = ServerDbs.Mobs2; ThrowFileNotFoundException = false; }
	}

	public class DbCastle : AbstractDb<int> {
		public DbCastle() {
			LayoutSearch = new DbAttribute[] {
				ServerCastleAttributes.Id, ServerCastleAttributes.CastleName,
				ServerCastleAttributes.MapName, ServerCastleAttributes.OnBreakGuildEventName
			};
			DbSource = ServerDbs.Castle;
			AttributeList = ServerCastleAttributes.AttributeList;
			DbWriter = DbWriterMethods.DbIntComma;
		}
	}

	public class DbPet : AbstractDb<int> {
		public DbPet() {
			LayoutIndexes = new int[] {
				0, 1, -1, 1,
				1, 19, -1, 1,
				20, 2
			};
			LayoutSearch = new DbAttribute[] {
				ServerPetAttributes.MobId, ServerPetAttributes.JName,
				ServerPetAttributes.Name, null,
				ServerPetAttributes.PetScript, ServerPetAttributes.LoyalScript
			};
			DbSource = ServerDbs.Pet;
			AttributeList = ServerPetAttributes.AttributeList;
			DbWriter = DbWriterMethods.DbIntComma;
			TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromMobDb;
		}
	}

	public class DbPet2 : DbPet {
		public DbPet2() { DbSource = ServerDbs.Pet2; ThrowFileNotFoundException = false; }
	}

	public class DbItemCombos : AbstractDb<string> {
		public DbItemCombos() {
			LayoutIndexes = new int[] { 0, 1, -1, 1, 1, 1 };
			UnsafeContext = true;
			DbSource = ServerDbs.Combos;
			AttributeList = ServerComboAttributes.AttributeList;
			TabGenerator.MaxElementsToCopyInCustomMethods = 2;
			TabGenerator.GenerateGrid = delegate(ref int line, GenericDatabase database, TabControl control, DisplayableProperty<string, ReadableTuple<string>> generalProperties, BaseDb gdb) {
				generalProperties.AddProperty(ServerComboAttributes.Id, 0, 1, null, true);
				generalProperties.AddLabel(ServerComboAttributes.Script, 1, 0);
				generalProperties.AddProperty(ServerComboAttributes.Script, 1, 1);
				ServerComboAttributes.Display.AttachedObject = gdb;
			};
			TabGenerator.OnInitSettings = delegate(GDbTabWrapper<string, ReadableTuple<string>> tab, GTabSettings<string, ReadableTuple<string>> settings, BaseDb gdb) {
				settings.AttIdWidth = 80;
			};
			DbWriter = DbWriterMethods.DbStringCommaWriter;
		}
	}

	public class DbItemGroups : AbstractDb<int> {
		public DbItemGroups() {
			DbSource = ServerDbs.ItemGroups;
			AttributeList = ServerItemGroupAttributes.AttributeList;
			DbLoader = DbLoaderMethods.DbItemGroups;
			DbWriter = DbWriterMethods.DbItemGroupWriter;
			TabGenerator.OnTabVisualUpdate += GTabsMaker.LoadSItemGroupVisualUpdate;
			TabGenerator.OnInitSettings += delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
				settings.CanBeDelayed = false;
				ServerItemGroupAttributes.Display.AttachedObject = gdb;
			};
			TabGenerator.GenerateGrid = delegate(ref int line, GenericDatabase database, TabControl control, DisplayableProperty<int, ReadableTuple<int>> generalProperties, BaseDb gdb) {
				generalProperties.AddProperty(ServerItemGroupAttributes.Table, 0, 1);
			};
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
	}

	public class DbConstants : AbstractDb<string> {
		public DbConstants() {
			DbSource = ServerDbs.Constants;
			AttributeList = ServerConstantsAttributes.AttributeList;
			DbLoader = DbLoaderMethods.DbTabsLoader;
			DbWriter = DbWriterMethods.DbConstantsWriter;
			TabGenerator.OnSetCustomCommands = delegate(GDbTabWrapper<string, ReadableTuple<string>> tab, GTabSettings<string, ReadableTuple<string>> settings, BaseDb gdb) {
				settings.AddedCommands.Add(new GItemCommand<string, ReadableTuple<string>> {
					AllowMultipleSelection = true,
					DisplayName = "Copy entries to clipboard",
					ImagePath = "export.png",
					InsertIndex = 3,
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

						Clipboard.SetText(builder.ToString());
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
			TabGenerator.OnGenerateGrid = delegate(ref int line, GenericDatabase database, TabControl control, DisplayableProperty<int, ReadableTuple<int>> generalProperties, BaseDb gdb) {
				generalProperties.AddLabel("Base stats", line++, 0, true);
				GTabsMaker.PrintGrid(ref line, 0, 1, 2, new DefaultIndexProvider(ServerHomunAttributes.BHp.Index, 8), -1, 0, -1, 0, generalProperties, gdb.AttributeList);
				generalProperties.AddLabel("Growth stats", line, 0, true);
				generalProperties.AddLabel("Evolution stats", line, 3, true);
				line++;
				GTabsMaker.PrintGrid(ref line, 0, 1, 2, new DefaultIndexProvider(ServerHomunAttributes.GnHp.Index, 16), -1, 0, -1, 0, generalProperties, gdb.AttributeList);
				line--;
				GTabsMaker.PrintGrid(ref line, 3, 1, 2, new DefaultIndexProvider(ServerHomunAttributes.EnHp.Index, 16), -1, 0, -1, 0, generalProperties, gdb.AttributeList);
			};
			DbWriter = DbWriterMethods.DbIntComma;
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
			ServerSkillRequirementsAttributes.Display.AttachedObject = this;
			DbWriter = DbWriterMethods.DbIntComma;
			TabGenerator.OnSetCustomCommands = GTabsMaker.AdvancedCustomCommands;
			TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromSkillDb;
		}
	}

	public class DbSkills : AbstractDb<int> {
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
				ServerSkillAttributes.CastingTime.Index, 1, ServerSkillAttributes.CoolDown.Index, 1,
				ServerSkillAttributes.AfterCastActDelay.Index, 4
			};
			TabGenerator.MaxElementsToCopyInCustomMethods = 18;
			DbSource = ServerDbs.Skills;
			AttributeList = ServerSkillAttributes.AttributeList;
			TabGenerator.OnPreviewTabInitialize = delegate(GDbTabWrapper<int, ReadableTuple<int>> tab, GTabSettings<int, ReadableTuple<int>> settings, BaseDb gdb) {
				settings.AttDisplay = ServerSkillAttributes.Desc;
			};
			TabGenerator.OnSetCustomCommands = GTabsMaker.AdvancedCustomCommands;
			TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromSkillRequirementsDb;
		}

		protected override void _loadDb() {
			Attached["NumberOfAttributesToGuess"] = 18;
			DbDebugItem<int> debug = new DbDebugItem<int>(this);
			if (debug.Load(ServerDbs.Skills)) DbLoaderMethods.DbCommaLoader(debug, this);
			if (debug.Load(ServerDbs.SkillsNoDex)) DbLoaderMethods.DbCommaRange(debug, AttributeList, ServerSkillAttributes.Cast.Index, 2);

			if (AllLoaders.GetServerType() == ServerType.RAthena)
				if (debug.Load(ServerDbs.SkillsNoCast)) DbLoaderMethods.DbCommaNoCast(debug, AttributeList, ServerSkillAttributes.Flag.Index, 1);

			if (debug.Load(ServerDbs.SkillsCast)) DbLoaderMethods.DbCommaRange(debug, AttributeList, ServerSkillAttributes.Cast.Index + 2, 6);
		}

		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			DbDebugItem<int> debug = new DbDebugItem<int>(this);

			debug.DbSource = ServerDbs.Skills;
			if (debug.Write(dbPath, subPath, serverType, fileType)) {
				DbWriterMethods.DbIntCommaRange(debug, this, 0, 18);
			}

			debug.DbSource = ServerDbs.SkillsNoDex;
			if (debug.Write(dbPath, subPath, serverType, fileType)) {
				DbWriterMethods.DbSkillsNoDexCommaRange(debug, this, ServerSkillAttributes.Cast.Index, 2);
			}

			debug.DbSource = ServerDbs.SkillsNoCast;
			if (serverType == ServerType.RAthena && debug.Write(dbPath, subPath, serverType, fileType)) {
				DbWriterMethods.DbSkillsNoCastCommaRange(debug, this, ServerSkillAttributes.Flag.Index, 1);
			}

			debug.DbSource = ServerDbs.SkillsCast;
			if (debug.Write(dbPath, subPath, serverType, fileType)) {
				DbWriterMethods.DbSkillsCastCommaRange(debug, this, ServerSkillAttributes.Cast.Index + 2, 6);
			}
		}
	}

	public class DbMobSkills : AbstractDb<string> {
		public DbMobSkills() {
			LayoutIndexes = new int[] {
				1, ServerMobSkillAttributes.AttributeList.Attributes.Count
			};
			DbSource = ServerDbs.MobSkills;
			AttributeList = ServerMobSkillAttributes.AttributeList;
			DbLoader = DbLoaderMethods.DbUniqueLoader;
			DbWriter = DbWriterMethods.DbUniqueWriter;
			DbWriterSql = SqlParser.DbSqlMobSkills;
			TabGenerator.OnInitSettings += delegate(GDbTabWrapper<string, ReadableTuple<string>> tab, GTabSettings<string, ReadableTuple<string>> settings, BaseDb gdb) {
				settings.CanChangeId = false;
				settings.CustomAddItemMethod = delegate {
					try {
						string id = Methods.RandomString(32);

						ReadableTuple<string> item = new ReadableTuple<string>(id, settings.AttributeList);
						item.Added = true;

						Table.Commands.StoreAndExecute(new AddTuple<string, ReadableTuple<string>>(id, item));
						tab._listView.ScrollToCenterOfView(item);
					}
					catch (KeyInvalidException) { }
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
		}
	}

	public class DbMobSkills2 : DbMobSkills {
		public DbMobSkills2() { DbSource = ServerDbs.MobSkills2; ThrowFileNotFoundException = false; }
	}

	public class DbMobBoss : AbstractDb<int> {
		public DbMobBoss() {
			DbSource = ServerDbs.MobBoss;
			AttributeList = ServerMobBossAttributes.AttributeList;
			DbWriter = DbWriterMethods.DbIntComma;
			TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromMobDb;
		}
	}

	public class DbMobBranch : DbMobBoss {
		public DbMobBranch() { DbSource = ServerDbs.MobBranch; }
	}

	public class DbMobPoring : DbMobBoss {
		public DbMobPoring() { DbSource = ServerDbs.MobPoring; }
	}

	public class DbMobPouch : DbMobBoss {
		public DbMobPouch() { DbSource = ServerDbs.RedPouch; }
	}

	public class DbMobClasschange : DbMobBoss {
		public DbMobClasschange() { DbSource = ServerDbs.Classchange; }
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
				catch { }
			}
		}
	}
}
