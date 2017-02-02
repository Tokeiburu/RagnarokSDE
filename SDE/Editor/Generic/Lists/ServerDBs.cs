using System;
using System.Collections.Generic;
using SDE.Editor.Generic.Core;

namespace SDE.Editor.Generic.Lists {
	public sealed class ServerDbs {
		public static List<ServerDbs> ListDbs = new List<ServerDbs>();
		public static readonly ServerDbs Zero = new ServerDbs("null");

		// Client
		public static readonly ServerDbs CItems = new ServerDbs("client_items") {
			DisplayName = "Client Items",
			IsClientSide = true,
			ClientSidePath = delegate {
				if (ProjectConfiguration.UseLuaFiles) {
					return ProjectConfiguration.ClientItemInfo;
				}

				return null;
			}
		};

		public static readonly ServerDbs CQuests = new ServerDbs("questid2display") { DisplayName = "Client Quests", IsClientSide = true, ClientSidePath = () => ProjectConfiguration.ClientQuest };
		public static readonly ServerDbs CCheevo = new ServerDbs("achievement_list") { DisplayName = "Client Cheevos", IsClientSide = true, ClientSidePath = () => ProjectConfiguration.ClientCheevo };

		// Server
		public static readonly ServerDbs MobGroups = new ServerDbs("null.txt") {
			DisplayName = "Mob Groups",
			IsClientSide = true,
			ClientSidePath = delegate {
				if (ProjectConfiguration.UseLuaFiles) {
					return DbPathLocator.DetectPath(MobBoss);
				}

				return null;
			}
		};

		public static readonly ServerDbs MobBranch = new ServerDbs("mob_branch") { DisplayName = "Dead Branch" };
		public static readonly ServerDbs MobPoring = new ServerDbs("mob_poring") { DisplayName = "Poring Box" };
		public static readonly ServerDbs MobBoss = new ServerDbs("mob_boss") { DisplayName = "Bloody Branch" };
		public static readonly ServerDbs RedPouch = new ServerDbs("mob_pouch") { DisplayName = "Red Pouch", UseSubPath = false };
		public static readonly ServerDbs Classchange = new ServerDbs("mob_classchange") { DisplayName = "Classchange", UseSubPath = false };
		public static readonly ServerDbs ItemsAvail2 = new ServerDbs("import\\item_avail") { DisplayName = "Item>Avail" };
		public static readonly ServerDbs ItemsAvail = new ServerDbs("item_avail") { DisplayName = "Item>Avail", AdditionalTable = ItemsAvail2 };
		public static readonly ServerDbs ItemsDelay2 = new ServerDbs("import\\item_delay") { DisplayName = "Item>Delay" };
		public static readonly ServerDbs ItemsDelay = new ServerDbs("item_delay") { DisplayName = "Item>Delay", AdditionalTable = ItemsDelay2 };
		public static readonly ServerDbs ItemsNoUse2 = new ServerDbs("import\\item_nouse") { DisplayName = "Item>No use" };
		public static readonly ServerDbs ItemsNoUse = new ServerDbs("item_nouse") { DisplayName = "Item>No use", AdditionalTable = ItemsNoUse2 };
		public static readonly ServerDbs ItemsStack2 = new ServerDbs("import\\item_stack") { DisplayName = "Item>Stack" };
		public static readonly ServerDbs ItemsStack = new ServerDbs("item_stack") { DisplayName = "Item>Stack", AdditionalTable = ItemsStack2 };
		public static readonly ServerDbs ItemsTrade2 = new ServerDbs("import\\item_trade") { DisplayName = "Item>Trade2" };
		public static readonly ServerDbs ItemsTrade = new ServerDbs("item_trade") { DisplayName = "Item>Trade" };
		public static readonly ServerDbs ItemsBuyingStore2 = new ServerDbs("import\\item_buyingstore") { DisplayName = "Item trade" };
		public static readonly ServerDbs ItemsBuyingStore = new ServerDbs("item_buyingstore") { DisplayName = "Item trade", AdditionalTable = ItemsBuyingStore2 };
		public static readonly ServerDbs Items2 = new ServerDbs("item_db2") { DisplayName = "Item2", SupportedFileType = FileType.Conf | FileType.Txt | FileType.Sql, UseSubPath = false, AlternativeName = "import\\item_db", IsImport = true };
		public static readonly ServerDbs Items = new ServerDbs("item_db") { DisplayName = "Item", SupportedFileType = FileType.Conf | FileType.Txt | FileType.Sql, AdditionalTable = Items2 };
		public static readonly ServerDbs Mobs2 = new ServerDbs("mob_db2") { DisplayName = "Mob2", SupportedFileType = FileType.Txt | FileType.Sql | FileType.Conf, UseSubPath = false, AlternativeName = "import\\mob_db", IsImport = true };
		public static readonly ServerDbs Mobs = new ServerDbs("mob_db") { DisplayName = "Mob", SupportedFileType = FileType.Txt | FileType.Sql | FileType.Conf, AdditionalTable = Mobs2 };
		public static readonly ServerDbs MobAvail = new ServerDbs("mob_avail") { DisplayName = "Mob>Avail", UseSubPath = false };
		public static readonly ServerDbs Homuns2 = new ServerDbs("homunculus_db2") { DisplayName = "Homuns2", UseSubPath = false, AlternativeName = "import\\homunculus_db", IsImport = true };
		public static readonly ServerDbs Homuns = new ServerDbs("homunculus_db") { DisplayName = "Homuns", AdditionalTable = Homuns2, UseSubPath = false };
		public static readonly ServerDbs Skills = new ServerDbs("skill_db") { DisplayName = "Skill" };
		public static readonly ServerDbs SkillsNoDex = new ServerDbs("skill_castnodex_db") { DisplayName = "Skill>CastNoDex" };
		public static readonly ServerDbs SkillsNoCast = new ServerDbs("skill_nocast_db") { DisplayName = "Skill>NoCast" };
		public static readonly ServerDbs SkillsCast = new ServerDbs("skill_cast_db") { DisplayName = "Skill>Cast" };
		public static readonly ServerDbs SkillsRequirement = new ServerDbs("skill_require_db") { DisplayName = "Sk. Requirements" };
		public static readonly ServerDbs SkillsTree = new ServerDbs("skill_tree") { DisplayName = "Skill Tree" };
		public static readonly ServerDbs Combos2 = new ServerDbs("item_combo_db2") { DisplayName = "Item Combo", AlternativeName = "import\\item_combo_db", IsImport = true };
		public static readonly ServerDbs Combos = new ServerDbs("item_combo_db") { DisplayName = "Item Combo", AdditionalTable = Combos2 };
		public static readonly ServerDbs MobSkills2 = new ServerDbs("mob_skill_db2") { DisplayName = "Mob Skills2", SupportedFileType = FileType.Txt | FileType.Sql, AlternativeName = "import\\mob_skill_db", IsImport = true };
		public static readonly ServerDbs MobSkills = new ServerDbs("mob_skill_db") { DisplayName = "Mob Skills", SupportedFileType = FileType.Txt | FileType.Sql, AdditionalTable = MobSkills2 };
		public static readonly ServerDbs Constants = new ServerDbs("const") { DisplayName = "Constants", UseSubPath = false, SupportedFileType = FileType.Txt | FileType.Conf, AlternativeName = "constants" };
		public static readonly ServerDbs Pet2 = new ServerDbs("pet_db2") { DisplayName = "Pet2", UseSubPath = false, AlternativeName = "import\\pet_db", IsImport = true };
		public static readonly ServerDbs Pet = new ServerDbs("pet_db") { DisplayName = "Pet", UseSubPath = false, AdditionalTable = Pet2 };
		public static readonly ServerDbs Castle2 = new ServerDbs("castle_db2") { DisplayName = "Castle", UseSubPath = false, AlternativeName = "import\\castle_db", IsImport = true };
		public static readonly ServerDbs Castle = new ServerDbs("castle_db") { DisplayName = "Castle", UseSubPath = false, AdditionalTable = Castle2 };
		public static readonly ServerDbs Quests2 = new ServerDbs("quest_db2") { DisplayName = "Quest2", UseSubPath = false, SupportedFileType = FileType.Conf | FileType.Txt, AlternativeName = "import\\quest_db", IsImport = true };
		public static readonly ServerDbs Quests = new ServerDbs("quest_db") { DisplayName = "Quest", UseSubPath = true, SupportedFileType = FileType.Conf | FileType.Txt, AdditionalTable = Quests2 };
		public static readonly ServerDbs ClientResourceDb = new ServerDbs("idnum2itemresnametable.txt") { DisplayName = "Fallback Resources" };
		public static readonly ServerDbs Cheevo = new ServerDbs("achievement_db") { DisplayName = "Cheevos", SupportedFileType = FileType.Conf, UseSubPath = false };
		public static readonly ServerDbs ItemGroups = new ServerDbs("item_group_db") { DisplayName = "Item Groups", AlternativeName = "item_group", SupportedFileType = FileType.Conf | FileType.Txt };
		public static readonly ServerDbs ItemGroupsGiftBox = new ServerDbs("item_giftbox") { DisplayName = "Item Groups", AlternativeName = "item_group", SupportedFileType = FileType.Conf | FileType.Txt };
		public static readonly ServerDbs ItemGroupsMisc = new ServerDbs("item_misc") { DisplayName = "Item Groups", AlternativeName = "item_group", SupportedFileType = FileType.Conf | FileType.Txt };
		public static readonly ServerDbs ItemGroupsFindingore = new ServerDbs("item_findingore") { DisplayName = "Item Groups", AlternativeName = "item_group", SupportedFileType = FileType.Conf | FileType.Txt };
		public static readonly ServerDbs ItemGroupsCardalbum = new ServerDbs("item_cardalbum") { DisplayName = "Item Groups", AlternativeName = "item_group", SupportedFileType = FileType.Conf | FileType.Txt };
		public static readonly ServerDbs ItemGroupsVioletBox = new ServerDbs("item_violetbox") { DisplayName = "Item Groups", AlternativeName = "item_group", SupportedFileType = FileType.Conf | FileType.Txt };
		public static readonly ServerDbs ItemGroupsBlueBox = new ServerDbs("item_bluebox") { DisplayName = "Item Groups", AlternativeName = "item_group", SupportedFileType = FileType.Conf | FileType.Txt };
		public static readonly ServerDbs ItemGroupsPackages = new ServerDbs("item_package") { DisplayName = "Item Groups", AlternativeName = "item_packages", SupportedFileType = FileType.Conf | FileType.Txt };

		public static ulong AllItemTables = CItems | Items | Items2;
		public static ulong ServerItems = Items | Items2;
		public static ulong ServerMobs = Mobs | Mobs2;
		public static ulong ClientItems = CItems;
		public static ulong MobSkillsItems = MobSkills | MobSkills2;

		private readonly string _filename = "null";
		private readonly ulong _subId;
		private string _displayName;

		private ServerDbs(string name) {
			UseSubPath = true;
			SupportedFileType = FileType.Txt;
			_filename = name;
			_subId = (ulong)1 << ListDbs.Count;
			ListDbs.Add(this);
		}

		public bool IsImport { get; set; }
		public bool IsClientSide { get; set; }
		public string AlternativeName { get; set; }
		public Func<string> ClientSidePath { get; set; }

		public string DisplayName {
			//get { return IsImport ? "import" : _displayName ?? _filename; }
			get { return _displayName ?? _filename; }
			set { _displayName = value; }
		}

		public string Filename {
			get { return _filename; }
		}

		public FileType SupportedFileType { get; set; }
		public ServerDbs AdditionalTable { get; set; }

		// Not used anymore
		public bool UseSubPath { get; set; }

		private bool _equals(ServerDbs other) {
			return string.Equals(_filename, other._filename);
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is ServerDbs && _equals((ServerDbs)obj);
		}

		public override int GetHashCode() {
			return (_filename != null ? _filename.GetHashCode() : 0);
		}

		public static implicit operator string(ServerDbs item) {
			return item.Filename;
		}

		public static implicit operator ulong(ServerDbs item) {
			return item._subId;
		}

		public static bool operator ==(ServerDbs item1, ServerDbs item2) {
			if (ReferenceEquals(item1, item2)) return true;
			if (ReferenceEquals(item1, null)) return false;
			if (ReferenceEquals(item2, null)) return false;
			return item1.Equals(item2);
		}

		public static bool operator !=(ServerDbs item1, ServerDbs item2) {
			return !(item1 == item2);
		}

		public static ServerDbs Instantiate(string fileName, string displayName, FileType supportedFileType) {
			return new ServerDbs(fileName) { DisplayName = displayName, SupportedFileType = supportedFileType };
		}

		public void Delete() {
			ListDbs.Remove(this);
		}

		public override string ToString() {
			return DisplayName;
		}
	}
}