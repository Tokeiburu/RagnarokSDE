using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Database;
using Database.Commands;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Engines.Parsers;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders.Writers;
using SDE.Tools.DatabaseEditor.Generic.IndexProviders;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using Utilities;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders {
	// debug.Load must ALWAYS be called when loading an item in the DB
	// This method creates a backup which will be used when saving the file
	// The same rule applies for debug.Write
	public class DbItems : AbstractDb<int> {
		public DbItems() {
			DbSource = ServerDBs.Items;
			AttributeList = ServerItemAttributes.AttributeList;
			DbLoader = DbLoaders.DbItemsLoader;
			TabGenerator.GDbTabMaker = GTabsMaker.LoadSItemsTab<int>;
			DbWriter = DbWriters.DbItemsCommaWriter;
		}

		protected override void _loadDb() {
			base._loadDb();

			if (AllLoaders.GetServerType() == ServerType.RAthena) {
				DbDebugItem<int> debug = new DbDebugItem<int>(this);
				if (debug.Load(ServerDBs.ItemsAvail)) DbLoaders.DbCommaRange(debug, AttributeList, ServerItemAttributes.Sprite.Index, 1);
				if (debug.Load(ServerDBs.ItemsDelay)) DbLoaders.DbCommaRange(debug, AttributeList, ServerItemAttributes.Delay.Index, 1);
				if (debug.Load(ServerDBs.ItemsNoUse)) DbLoaders.DbCommaLoader(debug, AttributeList, DbLoaders.DbItemsNouseFunction);
				if (debug.Load(ServerDBs.ItemsStack)) DbLoaders.DbCommaLoader(debug, AttributeList, DbLoaders.DbItemsStackFunction);
				if (debug.Load(ServerDBs.ItemsTrade)) DbLoaders.DbCommaLoader(debug, AttributeList, DbLoaders.DbItemsTradeFunction);
				if (debug.Load(ServerDBs.ItemsBuyingStore)) DbLoaders.DbCommaLoader(debug, AttributeList, DbLoaders.DbItemsBuyingStoreFunction);
			}
		}

		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			DbDebugItem<int> debug = new DbDebugItem<int>(this);

			debug.DbSource = ServerDBs.Items;
			if (debug.Write(dbPath, subPath, serverType, fileType)) {
				DbWriters.DbItemsCommaWriter(debug, this);
			}

			if (serverType == ServerType.RAthena) {
				debug.DbSource = ServerDBs.ItemsAvail;
				if (debug.Write(dbPath, subPath, serverType, fileType)) {
					DbWriters.DbItemsCommaRange(debug, this, ServerItemAttributes.Sprite.Index, 1, "");
				}

				debug.DbSource = ServerDBs.ItemsDelay;
				if (debug.Write(dbPath, subPath, serverType, fileType)) {
					DbWriters.DbItemsCommaRange(debug, this, ServerItemAttributes.Sprite.Index, 1, "");
				}

				debug.DbSource = ServerDBs.ItemsNoUse;
				if (debug.Write(dbPath, subPath, serverType, fileType)) {
					DbWriters.DbItemsNouse(debug, this);
				}

				debug.DbSource = ServerDBs.ItemsStack;
				if (debug.Write(dbPath, subPath, serverType, fileType)) {
					DbWriters.DbItemsStack(debug, this);
				}

				debug.DbSource = ServerDBs.ItemsTrade;
				if (debug.Write(dbPath, subPath, serverType, fileType)) {
					DbWriters.DbItemsTrade(debug, this);
				}

				debug.DbSource = ServerDBs.ItemsBuyingStore;
				if (debug.Write(dbPath, subPath, serverType, fileType)) {
					DbWriters.DbItemsBuyingStore(debug, this);
				}
			}
		}
	}

	public class DbItems2 : AbstractDb<int> {
		public DbItems2() {
			DbSource = ServerDBs.Items2;
			AttributeList = ServerItemAttributes.AttributeList;
			DbLoader = DbLoaders.DbItemsLoader;
			TabGenerator.GDbTabMaker = GTabsMaker.LoadSItemsTab<int>;
			DbWriter = DbWriters.DbItemsCommaWriter;
			ThrowFileNotFoundException = false;
		}
	}

	public class DbMobs : AbstractDb<int> {
		public DbMobs() {
			DbSource = ServerDBs.Mobs;
			AttributeList = ServerMobAttributes.AttributeList;
			TabGenerator.GDbTabMaker = GTabsMaker.LoadSMobsTab;
			DbWriterSql = SqlParser.DbSqlMobs;
			DbWriter = DbWriters.DbIntComma;
		}
	}

	public class DbMobs2 : DbMobs {
		public DbMobs2() { DbSource = ServerDBs.Mobs2; ThrowFileNotFoundException = false; }
	}

	public class DbCastle : AbstractDb<int> {
		public DbCastle() {
			LayoutSearch = new DbAttribute[] {
				ServerCastleAttributes.Id, ServerCastleAttributes.CastleName,
				ServerCastleAttributes.MapName, ServerCastleAttributes.OnBreakGuildEventName
			};
			DbSource = ServerDBs.Castle;
			AttributeList = ServerCastleAttributes.AttributeList;
			DbWriter = DbWriters.DbIntComma;
		}
	}

	public class DbPet : AbstractDb<int> {
		public DbPet() {
			LayoutIndexes = new int[] { 
				0, 1, -1, 1,
				1, 19, -1, 1,
				20, 2
			};
			DbSource = ServerDBs.Pet;
			AttributeList = ServerPetAttributes.AttributeList;
			DbWriter = DbWriters.DbIntComma;
			TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromMobDb;
		}
	}

	public class DbPet2 : DbPet {
		public DbPet2() { DbSource = ServerDBs.Pet2; ThrowFileNotFoundException = false; }
	}

	public class DbItemCombos : AbstractDb<string> {
		public DbItemCombos() {
			LayoutIndexes = new int[] { 0, 1, -1, 1, 1, 1 };
			UnsafeContext = true;
			DbSource = ServerDBs.Combos;
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
			DbWriter = DbWriters.DbStringCommaWriter;
		}
	}

	public class DbItemGroups : AbstractDb<int> {
		public DbItemGroups() {
			DbSource = ServerDBs.ItemGroups;
			AttributeList = ServerItemGroupAttributes.AttributeList;
			DbLoader = DbLoaders.DbItemGroups;
			DbWriter = DbWriters.DbItemGroupWriter;
			TabGenerator.GDbTabMaker = GTabsMaker.LoadSItemGroupsTab;
			TabGenerator.OnTabVisualUpdate += GTabsMaker.LoadSItemGroupVisualUpdate;
		}
	}

	public class DbConstants : AbstractDb<string> {
		public DbConstants() {
			DbSource = ServerDBs.Constants;
			AttributeList = ServerConstantsAttributes.AttributeList;
			DbLoader = DbLoaders.DbTabsLoader;
			DbWriter = DbWriters.DbConstantsWriter;
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
			DbSource = ServerDBs.Homuns;
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
			DbWriter = DbWriters.DbIntComma;
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
			DbSource = ServerDBs.SkillsRequirement;
			AttributeList = ServerSkillRequirementsAttributes.AttributeList;
			ServerSkillRequirementsAttributes.Display.AttachedObject = this;
			DbWriter = DbWriters.DbIntComma;
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
			DbSource = ServerDBs.Skills;
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
			if (debug.Load(ServerDBs.Skills)) DbLoaders.DbCommaLoader(debug, this);
			if (debug.Load(ServerDBs.SkillsNoDex)) DbLoaders.DbCommaRange(debug, AttributeList, ServerSkillAttributes.Cast.Index, 2);

			if (AllLoaders.GetServerType() == ServerType.RAthena)
				if (debug.Load(ServerDBs.SkillsNoCast)) DbLoaders.DbCommaNoCast(debug, AttributeList, ServerSkillAttributes.Flag.Index, 1);

			if (debug.Load(ServerDBs.SkillsCast)) DbLoaders.DbCommaRange(debug, AttributeList, ServerSkillAttributes.Cast.Index + 2, 6);
		}

		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			DbDebugItem<int> debug = new DbDebugItem<int>(this);

			debug.DbSource = ServerDBs.Skills;
			if (debug.Write(dbPath, subPath, serverType, fileType)) {
				DbWriters.DbIntCommaRange(debug, this, 0, 18);
			}

			debug.DbSource = ServerDBs.SkillsNoDex;
			if (debug.Write(dbPath, subPath, serverType, fileType)) {
				DbWriters.DbSkillsNoDexCommaRange(debug, this, ServerSkillAttributes.Cast.Index, 2);
			}

			debug.DbSource = ServerDBs.SkillsNoCast;
			if (serverType == ServerType.RAthena && debug.Write(dbPath, subPath, serverType, fileType)) {
				DbWriters.DbSkillsNoCastCommaRange(debug, this, ServerSkillAttributes.Flag.Index, 1);
			}

			debug.DbSource = ServerDBs.SkillsCast;
			if (debug.Write(dbPath, subPath, serverType, fileType)) {
				DbWriters.DbSkillsCastCommaRange(debug, this, ServerSkillAttributes.Cast.Index + 2, 6);
			}
		}
	}

	public class DbMobSkills : AbstractDb<string> {
		public DbMobSkills() {
			LayoutIndexes = new int[] {
				1, ServerMobSkillAttributes.AttributeList.Attributes.Count
			};
			DbSource = ServerDBs.MobSkills;
			AttributeList = ServerMobSkillAttributes.AttributeList;
			DbLoader = DbLoaders.DbUniqueLoader;
			DbWriter = DbWriters.DbUniqueWriter;
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
		public DbMobSkills2() { DbSource = ServerDBs.MobSkills2; ThrowFileNotFoundException = false; }
	}

	public class DbMobBoss : AbstractDb<int> {
		public DbMobBoss() {
			DbSource = ServerDBs.MobBoss;
			AttributeList = ServerMobBossAttributes.AttributeList;
			DbWriter = DbWriters.DbIntComma;
			TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromMobDb;
		}
	}

	public class DbMobBranch : DbMobBoss {
		public DbMobBranch() { DbSource = ServerDBs.MobBranch; }
	}

	public class DbMobPoring : DbMobBoss {
		public DbMobPoring() { DbSource = ServerDBs.MobPoring; }
	}

	public class DbMobPouch : DbMobBoss {
		public DbMobPouch() { DbSource = ServerDBs.RedPouch; }
	}

	public class DbMobClasschange : DbMobBoss {
		public DbMobClasschange() { DbSource = ServerDBs.Classchange; }
	}

	public class DbClientResource : AbstractDb<int> {
		public DbClientResource() {
			DbSource = ServerDBs.ClientResourceDb;
			AttributeList = ClientResourceProperties.AttributeList;
			DbLoader = null;
			TabGenerator = null;
			IsGenerateTab = false;
		}

		protected override void _loadDb() {
			Table.Clear();
			AllLoaders.LatestFile = DbSource;

			foreach (string[] elements in TextFileHelper.GetElements(Holder.Database.MetaGrf.GetData(@"data\idnum2itemresnametable.txt"))) {
				try {
					int itemId = Int32.Parse(elements[0]);
					Table.SetRaw(itemId, ClientResourceProperties.ResourceName, elements[1]);
				}
				catch { }
			}
		}
	}
}
