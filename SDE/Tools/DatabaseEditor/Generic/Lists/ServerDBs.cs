using System.Collections.Generic;
using System.Linq;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;

namespace SDE.Tools.DatabaseEditor.Generic.Lists {
	public sealed class ServerDBs {
		private static readonly List<ServerDBs> _txtFiles = new List<ServerDBs>();

		// Client
		public static readonly ServerDBs CItems = new ServerDBs("items") { DisplayName = "Items" };
		public static readonly ServerDBs CCards = new ServerDBs("cards") { DisplayName = "Cards" };

		// Server
		public static readonly ServerDBs MobBranch = new ServerDBs("mob_branch") { DisplayName = "Dead Branch" };
		public static readonly ServerDBs MobPoring = new ServerDBs("mob_poring") { DisplayName = "Poring Box" };
		public static readonly ServerDBs MobBoss = new ServerDBs("mob_boss") { DisplayName = "Bloody Branch" };
		public static readonly ServerDBs RedPouch = new ServerDBs("mob_pouch") { DisplayName = "Red Pouch", UseSubPath = false };
		public static readonly ServerDBs Classchange = new ServerDBs("mob_classchange") { DisplayName = "Classchange", UseSubPath = false };
		public static readonly ServerDBs Items = new ServerDBs("item_db") { DisplayName = "Item", SupportedFileType = FileType.Conf | FileType.Txt };
		public static readonly ServerDBs ItemsAvail = new ServerDBs("item_avail") { DisplayName = "Item redir", UseSubPath = false };
		public static readonly ServerDBs ItemsDelay = new ServerDBs("item_delay") { DisplayName = "Item delay", UseSubPath = false };
		public static readonly ServerDBs ItemsNoUse = new ServerDBs("item_nouse") { DisplayName = "Item no use", UseSubPath = false };
		public static readonly ServerDBs ItemsStack = new ServerDBs("item_stack") { DisplayName = "Item stack", UseSubPath = false };
		public static readonly ServerDBs ItemsTrade = new ServerDBs("item_trade") { DisplayName = "Item trade" };
		public static readonly ServerDBs ItemsBuyingStore = new ServerDBs("item_buyingstore") { DisplayName = "Item trade" };
		public static readonly ServerDBs Items2 = new ServerDBs("item_db2") { DisplayName = "Item2", SupportedFileType = FileType.Conf | FileType.Txt, UseSubPath = false };
		public static readonly ServerDBs Mobs = new ServerDBs("mob_db") { DisplayName = "Mob" };
		public static readonly ServerDBs Mobs2 = new ServerDBs("mob_db2") { DisplayName = "Mob2", UseSubPath = false };
		public static readonly ServerDBs Homuns = new ServerDBs("homunculus_db") { DisplayName = "Homunculus", UseSubPath = false };
		public static readonly ServerDBs Skills = new ServerDBs("skill_db") { DisplayName = "Skill" };
		public static readonly ServerDBs SkillsNoDex = new ServerDBs("skill_castnodex_db");
		public static readonly ServerDBs SkillsNoCast = new ServerDBs("skill_nocast_db");
		public static readonly ServerDBs SkillsCast = new ServerDBs("skill_cast_db");
		public static readonly ServerDBs SkillsRequirement = new ServerDBs("skill_require_db") { DisplayName = "Sk. Requirements" };
		public static readonly ServerDBs SkillsTree = new ServerDBs("skill_tree") { DisplayName = "Skill Tree" };
		public static readonly ServerDBs Combos = new ServerDBs("item_combo_db") { DisplayName = "Item Combo" };
		public static readonly ServerDBs MobSkills = new ServerDBs("mob_skill_db") { DisplayName = "Mob Skills" };
		public static readonly ServerDBs Constants = new ServerDBs("const") { DisplayName = "Constants", UseSubPath = false };
		public static readonly ServerDBs ItemGroups = new ServerDBs("item_group_db") { DisplayName = "Item Groups", AlternativeName = "item_group", SupportedFileType = FileType.Conf | FileType.Txt };
		public static readonly ServerDBs Pet = new ServerDBs("pet_db") { DisplayName = "Pet", UseSubPath = false };
		public static readonly ServerDBs Pet2 = new ServerDBs("pet_db2") { DisplayName = "Pet2", UseSubPath = false };
		public static readonly ServerDBs Castle = new ServerDBs("castle_db") { DisplayName = "Castle", UseSubPath = false };
		public static readonly ServerDBs ClientResourceDb = new ServerDBs("idnum2itemresnametable.txt");

		private readonly string _filename = "null";
		private string _displayName;

		private ServerDBs(string name) {
			UseSubPath = true;
			SupportedFileType = FileType.Txt;
			_filename = name;
			_txtFiles.Add(this);
		}

		public FileType SupportedFileType { get; set; }
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

		public static IEnumerable<string> Files {
			get {
				return _txtFiles.Select(itemTxt => itemTxt.Filename);
			}
		}

		public bool UseSubPath { get; set; }

		private bool Equals(ServerDBs other) {
			return string.Equals(_filename, other._filename);
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is ServerDBs && Equals((ServerDBs) obj);
		}

		public override int GetHashCode() {
			return (_filename != null ? _filename.GetHashCode() : 0);
		}

		public static implicit operator string(ServerDBs item) {
			return item.Filename;
		}
	}
}
