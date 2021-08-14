using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Database;
using SDE.ApplicationConfiguration;
using SDE.Editor.Generic.Lists;
using Utilities;

namespace SDE.Editor.Items {
	public sealed class ParameterHolderKeys {
		public static List<ParameterHolderKeys> Keys = new List<ParameterHolderKeys>();

		public static readonly ParameterHolderKeys Class = new ParameterHolderKeys("Class");
		public static readonly ParameterHolderKeys CompoundOn = new ParameterHolderKeys("Compound on");
		public static readonly ParameterHolderKeys Attack = new ParameterHolderKeys("Attack");
		public static readonly ParameterHolderKeys Defense = new ParameterHolderKeys("Defense");
		public static readonly ParameterHolderKeys EquippedOn = new ParameterHolderKeys("Equipped on");
		public static readonly ParameterHolderKeys Weight = new ParameterHolderKeys("Weight");
		public static readonly ParameterHolderKeys Property = new ParameterHolderKeys("Property");
		public static readonly ParameterHolderKeys WeaponLevel = new ParameterHolderKeys("Weapon Level");
		public static readonly ParameterHolderKeys RequiredLevel = new ParameterHolderKeys("Required Level");
		public static readonly ParameterHolderKeys ApplicableJob = new ParameterHolderKeys("Applicable Job");
		public static readonly ParameterHolderKeys ApplicablePet = new ParameterHolderKeys("Applicable Pet");
		public static readonly ParameterHolderKeys Location = new ParameterHolderKeys("Location");
		public static readonly ParameterHolderKeys Description = new ParameterHolderKeys("Description");

		public string Key { get; private set; }

		private ParameterHolderKeys(string key) {
			Key = key;
			Keys.Add(this);
		}

		public string Display {
			get { return SdeAppConfiguration.ConfigAsker["Autocompletion - " + Key, Key]; }
		}

		public static implicit operator string(ParameterHolderKeys key) {
			return key.Key;
			//return SdeAppConfiguration.ConfigAsker["Autocompletion - " + key.Key, key.Key];
		}

		public override string ToString() {
			return Key;
			//return SdeAppConfiguration.ConfigAsker["Autocompletion - " + Key, Key];
		}

		public static TkDictionary<string, string> Redirections = new TkDictionary<string, string> {
			{ "Jobs", ApplicableJob },
			{ "Job", ApplicableJob },
			{ "Equips on", CompoundOn },
			{ "Type", Class },
			{ "Item Class", Class },
			{ "Series", Class },
			{ "Position", Location },
			{ "ATK", Attack },
			{ "DEF", Defense },
			{ "Required lvl", RequiredLevel },
			{ "Required Lv", RequiredLevel },
			{ "Req LV", RequiredLevel },
			{ "Weapon lvl", WeaponLevel },
			{ "무게", Weight },
			{ "방어", Defense },
			{ "요구", RequiredLevel },
			{ "요구 레벨", RequiredLevel },
			{ "무기레벨", WeaponLevel },
			{ "무기 레벨", WeaponLevel },
			{ "Equipped by", ApplicableJob },
			{ "Level Requirement", RequiredLevel },
			//{ "DEF", Defense },
			{ "Slot", "DoNotShow" },
		};
	}

	public class ParameterHolder {
		public static ParameterHolderKeys[] KnownParameterHolderKeys = {
			ParameterHolderKeys.Class,
			ParameterHolderKeys.CompoundOn,
			ParameterHolderKeys.Attack,
			ParameterHolderKeys.Defense,
			ParameterHolderKeys.EquippedOn,
			ParameterHolderKeys.Weight,
			ParameterHolderKeys.Property,
			ParameterHolderKeys.WeaponLevel,
			ParameterHolderKeys.RequiredLevel,
			ParameterHolderKeys.ApplicableJob,
			ParameterHolderKeys.Location,
			ParameterHolderKeys.ApplicablePet,
		};

		public static string[] KnownItemParameters = {
		};

		public static string[] SearchKnownItemParameters = {
		};

		static ParameterHolder() {
			KnownItemParameters = KnownParameterHolderKeys.ToList().Select(p => p.ToString()).ToArray();
			KnownItemParameters = KnownItemParameters.Concat(ParameterHolderKeys.Redirections.Keys).ToArray();

			HashSet<string> items = new HashSet<string>();

			for (int i = 0; i < KnownItemParameters.Length; i++) {
				bool isSub = false;

				for (int j = i + 1; j < KnownItemParameters.Length && !isSub; j++) {
					if (KnownItemParameters[i].Contains(KnownItemParameters[j])) {
						items.Add(KnownItemParameters[i]);
						items.Add(KnownItemParameters[j]);
						isSub = true;
					}
					if (KnownItemParameters[j].Contains(KnownItemParameters[i])) {
						items.Add(KnownItemParameters[j]);
						items.Add(KnownItemParameters[i]);
						isSub = true;
					}
				}

				if (!isSub)
					items.Add(KnownItemParameters[i]);
			}

			SearchKnownItemParameters = items.ToArray();
		}

		public static string[] Properties = {
			"Dark",
			"Earth",
			"Fire",
			"Ghost",
			"Holy",
			"Neutral",
			"Poison",
			"Shadow",
			"Undead",
			"Water",
			"Wind"
		};

		public static string[] CompoundOn = {
			"Accessory",
			"Armor",
			"Footgear",
			"Garment",
			"Headgear",
			"Shield",
			"Weapon",
		};

		public static string[] Jobs = {
			"--- Specials and generic ---",
			"Every Job",
			"Every Rebirth Job",
			"Every 2nd Job or above",
			"Every 3rd Job",
			"Every Job Except Novice",
			"Every Rebirth Job except High Novice",
			"Every Rebirth Job or above",
			"Female Only, Every Job",
			"Male Only, Every Job",
			"",
			"--- 1st Job Class ---",
			"Acolyte Class",
			"Archer Class",
			"Mage Class",
			"Merchant Class",
			"Swordman Class",
			"Thief Class",
			"",
			"--- 2nd Job Class ---",
			"Alchemist",
			"Assassin",
			"Bard",
			"Blacksmith",
			"Crusader",
			"Dancer",
			"Hunter",
			"Knight",
			"Monk",
			"Priest",
			"Rogue",
			"Sage",
			"Wizard",
			"",
			"--- Rebirth 2nd Job Class ---",
			"Assassin Cross",
			"Biochemist",
			"Champion",
			"Clown",
			"Gypsy",
			"High Priest",
			"High Wizard",
			"Lord Knight",
			"Paladin",
			"Scholar",
			"Sniper",
			"Stalker",
			"Whitesmith",
			"",
			"--- 3rd Job Class ---",
			"Arch Bishop",
			"Geneticist",
			"Guillotine Cross",
			"Maestro",
			"Mechanic",
			"Ranger",
			"Royal Guard",
			"Rune Knight",
			"Shadow Chaser",
			"Sorcerer",
			"Sura",
			"Wanderer",
			"Warlock",
			"",
			"--- Extended Class ---",
			"Gunslinger",
			"Kagerou",
			"Ninja",
			"Oboro",
			"Soul Linker",
			"Super Novice",
			"Taekwon",
			"Taekwon Master"
		};

		private readonly TkDictionary<string, string> _values = new TkDictionary<string, string>();

		public TkDictionary<string, string> Values {
			get { return _values; }
		}

		public ParameterHolder() {
		}

		private int _indexOf(string parameter, string description, int start) {
			int index = -1;

			if (index < 0) {
				index = description.IndexOf("\n" + parameter + ":", start, StringComparison.OrdinalIgnoreCase);

				if (index > -1) {
					index = index + 1;
				}
			}

			if (index < 0) {
				index = description.IndexOf(parameter + " :", start, StringComparison.OrdinalIgnoreCase);
			}

			if (index < 0) {
				index = description.IndexOf("\n" + parameter + " :", start, StringComparison.OrdinalIgnoreCase);

				if (index > -1) {
					index = index + 1;
				}
			}

			if (index < 0) {
				index = description.IndexOf(parameter + ":", start, StringComparison.OrdinalIgnoreCase);
			}

			if (index < 0) {
				index = description.IndexOf(parameter + "\t:", start, StringComparison.OrdinalIgnoreCase);
			}

			return index;
		}

		private static readonly Regex _parameter = new Regex(@"[^\ \r\n]+.*?:.*?\^[\d]{6}.*?\^[\d]{6}", RegexOptions.Multiline | RegexOptions.Compiled);

		public ParameterHolder(Database.Tuple item) {
			string description = item.GetValue<string>(ClientItemAttributes.IdentifiedDescription);

			foreach (string parameter in SearchKnownItemParameters) {
				int index;
				if ((index = _indexOf(parameter, description, 0)) > -1) {
					try {
						string value;
						int toRemove = _readNextElement(parameter, description, index, out value);
						description = description.Remove(index, toRemove);
						_values[parameter] = value;
					}
					catch {
					}

					if (ParameterHolderKeys.Redirections[parameter] != null) {
						_values[ParameterHolderKeys.Redirections[parameter]] = _values[parameter];
					}
				}
			}

			// Removes any other parameter
			foreach (Match match in _parameter.Matches(description).Cast<Match>().OrderByDescending(p => p.Index)) {
				description = description.Remove(match.Index, match.Length);
			}

			description = description.Trim('\r', '\n', '\t', ' ');

			//while (description.EndsWith("\r\n")) {
			//    description = description.Remove(description.Length - 2, 2);
			//}

			//while (description.EndsWith("\n")) {
			//    description = description.Remove(description.Length - 1, 1);
			//}

			_values[ParameterHolderKeys.Description] = description;
		}

		private static int _readNextElement(string parameter, string description, int index, out string outValue) {
			int originalIndex = index;
			index = parameter.Length + originalIndex;

			int startIndex = description.IndexOf('^', index);
			int validateIndex = description.IndexOf(':', index);
			int toSkipStart = 7;

			if (startIndex > validateIndex + 10) {
				startIndex = -1;
			}

			if (startIndex < 0) {
				startIndex = validateIndex;
				toSkipStart = 1;
			}

			int endIndex;

			if (toSkipStart == 1) {
				endIndex = description.IndexOf('\r', startIndex + toSkipStart);

				if (endIndex < 0)
					endIndex = description.Length;
			}
			else {
				endIndex = description.IndexOf('^', startIndex + toSkipStart);

				if (endIndex < 0) {
					endIndex = description.Length;
				}
			}

			string value = description.Substring(startIndex + toSkipStart, endIndex - (startIndex + toSkipStart));

			if (value.Contains("\r\n"))
				outValue = String.Join(" ", value.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList().Select(p => p.Trim(' ')).ToArray());
			else
				outValue = value.Trim(' ');

			if (description.Length <= endIndex + 7)
				return description.Length - originalIndex;

			if (description[endIndex + 7] == '\r' && description[endIndex + 8] == '\n')
				return endIndex + 9 - originalIndex;
			if (description[endIndex + 7] == ' ')
				return endIndex + 8 - originalIndex;
			return endIndex + 7 - originalIndex;
		}

		public string GenerateDescription() {
			return _generateDescription(_values);
		}

		private string _generateDescription(TkDictionary<string, string> para) {
			string description = "";

			description += _trimReturns(para[ParameterHolderKeys.Description]);
			description += "\r\n";

			if (para.Count < 3) {
				if (!description.EndsWith("^ffffff_^000000\r\n"))
					description += "^ffffff_^000000\r\n";
			}

			_addDescFor(ref description, para[KnownParameterHolderKeys[0]], 1);
			_addDescFor(ref description, para[KnownParameterHolderKeys[1]], 2);
			_addDescFor(ref description, para[KnownParameterHolderKeys[2]], 3);
			_addDescFor(ref description, para[KnownParameterHolderKeys[3]], 4);
			_addDescFor(ref description, para[KnownParameterHolderKeys[4]], 5);
			_addDescFor(ref description, para[KnownParameterHolderKeys[10]], 11);
			_addDescFor(ref description, para[KnownParameterHolderKeys[5]], 6);
			_addDescFor(ref description, para[KnownParameterHolderKeys[11]], 12);

			var property = para[ParameterHolderKeys.Property];

			if (!String.IsNullOrEmpty(property)) {
				string[] properties = property.Split(',').Select(p => p.Trim(' ')).ToArray();

				properties = properties.Where(p => p != "").ToArray();

				if (properties.Length > 0) {
					description += KnownParameterHolderKeys[6].Display + " :";

					foreach (string prop in properties) {
						try {
							description += ProjectConfiguration.AutocompleteProperties[Properties.ToList().IndexOf(prop)];
						}
						catch {
							description += "^000000";
						}
						description += " " + prop + "^000000, ";
					}

					description = description.Remove(description.Length - 2, 2);
					description += "\r\n";
				}
			}

			_addDescFor(ref description, para[KnownParameterHolderKeys[7]], 8);
			_addDescFor(ref description, para[KnownParameterHolderKeys[8]], 9);
			_addDescFor(ref description, para[KnownParameterHolderKeys[9]], 10);

			return description.Trim('\r', '\n');
		}

		private static void _addDescFor(ref string description, string text, int i) {
			if (text == null)
				return;

			text = text.Trim(' ');

			if (text == "")
				return;

			description += String.Format(ProjectConfiguration.AutocompleteDescriptionFormat, KnownParameterHolderKeys[i - 1].Display, text) + "\r\n";
		}

		private static string _trimReturns(string description) {
			if (string.IsNullOrEmpty(description))
				return "";

			while (description.EndsWith("\r\n")) {
				description = description.Remove(description.Length - 2, 2);
			}

			while (description.EndsWith("\n")) {
				description = description.Remove(description.Length - 1, 1);
			}

			return description;
		}

		public static string ClearDescription(string description) {
			description = description.Trim(' ', '\t', '\r', '\n');

			while (description.LastIndexOf('^') == description.Length - 7 && description.Length > 9 && description[description.Length - 8] != '_') {
				description = description.Substring(0, description.Length - 7).Trim(' ', '\t', '\r', '\n');
			}

			return description;
		}
	}
}