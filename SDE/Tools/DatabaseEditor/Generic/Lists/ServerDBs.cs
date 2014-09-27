using System.Collections.Generic;
using System.Linq;
using SDE.Tools.DatabaseEditor.Generic.Core;

namespace SDE.Tools.DatabaseEditor.Generic.Lists {
	public sealed class ServerDbs {
		// Client
		public static readonly ServerDbs CItems = new ServerDbs("items") { DisplayName = "Items" };
		public static readonly ServerDbs CCards = new ServerDbs("cards") { DisplayName = "Cards" };

		// Server
		public static readonly ServerDbs MobBranch = new ServerDbs("mob_branch") { DisplayName = "Dead Branch" };
		public static readonly ServerDbs MobPoring = new ServerDbs("mob_poring") { DisplayName = "Poring Box" };
		public static readonly ServerDbs MobBoss = new ServerDbs("mob_boss") { DisplayName = "Bloody Branch" };
		public static readonly ServerDbs RedPouch = new ServerDbs("mob_pouch") { DisplayName = "Red Pouch", UseSubPath = false };
		public static readonly ServerDbs Classchange = new ServerDbs("mob_classchange") { DisplayName = "Classchange", UseSubPath = false };
		public static readonly ServerDbs ItemsAvail = new ServerDbs("item_avail") { DisplayName = "Item redir", UseSubPath = false };
		public static readonly ServerDbs ItemsDelay = new ServerDbs("item_delay") { DisplayName = "Item delay", UseSubPath = false };
		public static readonly ServerDbs ItemsNoUse = new ServerDbs("item_nouse") { DisplayName = "Item no use", UseSubPath = false };
		public static readonly ServerDbs ItemsStack = new ServerDbs("item_stack") { DisplayName = "Item stack", UseSubPath = false };
		public static readonly ServerDbs ItemsTrade = new ServerDbs("item_trade") { DisplayName = "Item trade" };
		public static readonly ServerDbs ItemsBuyingStore = new ServerDbs("item_buyingstore") { DisplayName = "Item trade" };
		public static readonly ServerDbs Items2 = new ServerDbs("item_db2") { DisplayName = "Item2", SupportedFileType = FileType.Conf | FileType.Txt | FileType.Sql, UseSubPath = false };
		public static readonly ServerDbs Items = new ServerDbs("item_db") { DisplayName = "Item", SupportedFileType = FileType.Conf | FileType.Txt | FileType.Sql, AdditionalTable = Items2 };
		public static readonly ServerDbs Mobs2 = new ServerDbs("mob_db2") { DisplayName = "Mob2", SupportedFileType = FileType.Txt | FileType.Sql, UseSubPath = false };
		public static readonly ServerDbs Mobs = new ServerDbs("mob_db") { DisplayName = "Mob", SupportedFileType = FileType.Txt | FileType.Sql, AdditionalTable = Mobs2 };
		public static readonly ServerDbs Homuns = new ServerDbs("homunculus_db") { DisplayName = "Homunculus", UseSubPath = false };
		public static readonly ServerDbs Skills = new ServerDbs("skill_db") { DisplayName = "Skill" };
		public static readonly ServerDbs SkillsNoDex = new ServerDbs("skill_castnodex_db");
		public static readonly ServerDbs SkillsNoCast = new ServerDbs("skill_nocast_db");
		public static readonly ServerDbs SkillsCast = new ServerDbs("skill_cast_db");
		public static readonly ServerDbs SkillsRequirement = new ServerDbs("skill_require_db") { DisplayName = "Sk. Requirements" };
		public static readonly ServerDbs SkillsTree = new ServerDbs("skill_tree") { DisplayName = "Skill Tree" };
		public static readonly ServerDbs Combos = new ServerDbs("item_combo_db") { DisplayName = "Item Combo" };
		public static readonly ServerDbs MobSkills2 = new ServerDbs("mob_skill_db2") { DisplayName = "Mob Skills2", SupportedFileType = FileType.Txt | FileType.Sql };
		public static readonly ServerDbs MobSkills = new ServerDbs("mob_skill_db") { DisplayName = "Mob Skills", SupportedFileType = FileType.Txt | FileType.Sql, AdditionalTable = MobSkills2 };
		public static readonly ServerDbs Constants = new ServerDbs("const") { DisplayName = "Constants", UseSubPath = false };
		public static readonly ServerDbs ItemGroups = new ServerDbs("item_group_db") { DisplayName = "Item Groups", AlternativeName = "item_group", SupportedFileType = FileType.Conf | FileType.Txt };
		public static readonly ServerDbs Pet2 = new ServerDbs("pet_db2") { DisplayName = "Pet2", UseSubPath = false };
		public static readonly ServerDbs Pet = new ServerDbs("pet_db") { DisplayName = "Pet", UseSubPath = false, AdditionalTable = Pet2 };
		public static readonly ServerDbs Castle = new ServerDbs("castle_db") { DisplayName = "Castle", UseSubPath = false };
		public static readonly ServerDbs ClientResourceDb = new ServerDbs("idnum2itemresnametable.txt");

		private readonly string _filename = "null";
		private string _displayName;

		private ServerDbs(string name) {
			UseSubPath = true;
			SupportedFileType = FileType.Txt;
			_filename = name;
		}

		public string AlternativeName { get; set; }
		public string DisplayName {
			get {
				return _displayName ?? _filename;
			}
			set { _displayName = value; }
		}
		public string Filename {
			get { return _filename; }
		}
		public FileType SupportedFileType { get; set; }
		public ServerDbs AdditionalTable { get; set; }

		// Not used anymore
		public bool UseSubPath { get; set; }

		private bool Equals(ServerDbs other) {
			return string.Equals(_filename, other._filename);
		}
		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is ServerDbs && Equals((ServerDbs) obj);
		}
		public override int GetHashCode() {
			return (_filename != null ? _filename.GetHashCode() : 0);
		}

		public static implicit operator string(ServerDbs item) {
			return item.Filename;
		}

		public static ServerDbs Instantiate(string fileName, string displayName, FileType supportedFileType) {
			return new ServerDbs(fileName) {DisplayName = displayName, SupportedFileType = supportedFileType};
		}
	}
}
