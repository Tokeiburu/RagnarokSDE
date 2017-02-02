using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SDE.ApplicationConfiguration;
using Utilities;
using Utilities.Tools;

namespace SDE.Editor {
	/// <summary>
	/// Contains all the configuration information
	/// The ConfigAsker shouldn't be used manually to store variable,
	/// make a new property instead. The properties should also always
	/// have a default value.
	/// </summary>
	public static class ProjectConfiguration {
		public delegate void ConfigAskerChangedDelegate();

		public static string DefaultFileName = Path.Combine(SdeAppConfiguration.ProgramDataPath, "default.sde");
		private static ConfigAsker _configAsker;

		public static ConfigAsker ConfigAsker {
			get { return _configAsker ?? (_configAsker = new ConfigAsker(Path.Combine(Methods.ApplicationPath, "config.txt"))); }
			set {
				_configAsker = value;
				OnConfigAskerChanged();
			}
		}

		public static event ConfigAskerChangedDelegate ConfigAskerChanged;

		public static void OnConfigAskerChanged() {
			ConfigAskerChangedDelegate handler = ConfigAskerChanged;
			if (handler != null) handler();
		}

		// The information regarding the username and the password is not meant to be strong.
		public static string FtpUsername {
			get { return SimpleAES.Decrypt(ConfigAsker["[Server database editor - Username]", SimpleAES.Encrypt("username")]); }
			set { ConfigAsker["[Server database editor - Username]"] = SimpleAES.Encrypt(value); }
		}

		// The information regarding the username and the password is not meant to be strong.
		public static string FtpPassword {
			get { return SimpleAES.Decrypt(ConfigAsker["[Server database editor - Password]", SimpleAES.Encrypt("")]); }
			set { ConfigAsker["[Server database editor - Password]"] = SimpleAES.Encrypt(value); }
		}

		public static string DatabasePath {
			get { return ConfigAsker["[Server database editor - Database path]", @"C:\RO\db\pre-re"]; }
			set { ConfigAsker["[Server database editor - Database path]"] = value; }
		}

		public static string InternalDatabasePath {
			get { return ConfigAsker["[Server database editor - Database path]", @"C:\RO\db\pre-re"]; }
		}

		public static string SdeEditorResources {
			get { return ConfigAsker["[Server database editor - Resources]", ""]; }
			set { ConfigAsker["[Server database editor - Resources]"] = value; }
		}

		public static bool SynchronizeWithClientDatabases {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Synchronize with client databases]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Synchronize with client databases]"] = value.ToString(); }
		}

		public static bool UseLuaFiles {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Use Lua files]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Use Lua files]"] = value.ToString(); }
		}

		public static bool HandleViewIds {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Handle view IDs]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - Handle view IDs]"] = value.ToString(); }
		}

		public static string ClientCardIllustration {
			get { return ConfigAsker["[Server database editor - Client - Card Illustration]", "data\\num2cardillustnametable.txt"]; }
			set { ConfigAsker["[Server database editor - Client - Card Illustration]"] = value; }
		}

		public static string ClientCardPrefixes {
			get { return ConfigAsker["[Server database editor - Client - Card prefixes]", "data\\cardprefixnametable.txt"]; }
			set { ConfigAsker["[Server database editor - Client - Card prefixes]"] = value; }
		}

		public static string ClientCardPostfixes {
			get { return ConfigAsker["[Server database editor - Client - Card postfixes]", "data\\cardpostfixnametable.txt"]; }
			set { ConfigAsker["[Server database editor - Client - Card postfixes]"] = value; }
		}

		public static string ClientItemIdentifiedName {
			get { return ConfigAsker["[Server database editor - Client - Identified name]", "data\\idnum2itemdisplaynametable.txt"]; }
			set { ConfigAsker["[Server database editor - Client - Identified name]"] = value; }
		}

		public static string ClientItemUnidentifiedName {
			get { return ConfigAsker["[Server database editor - Client - Unidentified name]", "data\\num2itemdisplaynametable.txt"]; }
			set { ConfigAsker["[Server database editor - Client - Unidentified name]"] = value; }
		}

		public static string ClientItemIdentifiedDescription {
			get { return ConfigAsker["[Server database editor - Client - Identified description]", "data\\idnum2itemdesctable.txt"]; }
			set { ConfigAsker["[Server database editor - Client - Identified description]"] = value; }
		}

		public static string ClientItemUnidentifiedDescription {
			get { return ConfigAsker["[Server database editor - Client - Unidentified description]", "data\\num2itemdesctable.txt"]; }
			set { ConfigAsker["[Server database editor - Client - Unidentified description]"] = value; }
		}

		public static string ClientItemIdentifiedResourceName {
			get { return ConfigAsker["[Server database editor - Client - Identified resource name]", "data\\idnum2itemresnametable.txt"]; }
			set { ConfigAsker["[Server database editor - Client - Identified resource name]"] = value; }
		}

		public static string ClientItemUnidentifiedResourceName {
			get { return ConfigAsker["[Server database editor - Client - Unidentified resource name]", "data\\num2itemresnametable.txt"]; }
			set { ConfigAsker["[Server database editor - Client - Unidentified resource name]"] = value; }
		}

		public static string ClientItemSlotCount {
			get { return ConfigAsker["[Server database editor - Client - Slot count]", "data\\itemslotcounttable.txt"]; }
			set { ConfigAsker["[Server database editor - Client - Slot count]"] = value; }
		}

		public static string ClientQuest {
			get { return ConfigAsker["[Server database editor - Client - Quest]", "data\\questid2display.txt"]; }
			set { ConfigAsker["[Server database editor - Client - Quest]"] = value; }
		}

		public static string ClientCheevo {
			get { return ConfigAsker["[Server database editor - Client - Cheevo]", "System\\achievement_list.lub"]; }
			set { ConfigAsker["[Server database editor - Client - Cheevo]"] = value; }
		}

		public static string ClientItemInfo {
			get { return ConfigAsker["[Server database editor - Client - Item info]", "System\\itemInfo.lua"]; }
			set { ConfigAsker["[Server database editor - Client - Item info]"] = value; }
		}

		public static bool AutocompleteIdDisplayName {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - Id. display name]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - Id. display name]"] = value.ToString(); }
		}

		public static bool AutocompleteUnDisplayName {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - Un. display name]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - Un. display name]"] = value.ToString(); }
		}

		public static bool AutocompleteFillOnlyEmptyFields {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - Fill empty fields]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - Fill empty fields]"] = value.ToString(); }
		}

		public static bool AutocompleteIdResourceName {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - Id. resource name]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - Id. resource name]"] = value.ToString(); }
		}

		public static bool AutocompleteUnResourceName {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - Un. resource name]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - Un. resource name]"] = value.ToString(); }
		}

		public static bool AutocompleteIdDescription {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - Id. desc name]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - Id. desc name]"] = value.ToString(); }
		}

		public static bool AutocompleteUnDescription {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - Un. desc name]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - Un. desc name]"] = value.ToString(); }
		}

		public static bool AutocompleteNeutralProperty {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - Neutral property]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - Neutral property]"] = value.ToString(); }
		}

		public static string AutocompleteDescriptionFormat {
			get { return ConfigAsker["[Server database editor - Autocomplete - Description format]", "{0} :^777777 {1}^000000"]; }
			set { ConfigAsker["[Server database editor - Autocomplete - Description format]"] = value; }
		}

		public static string AutocompleteUnDescriptionFormat {
			get { return ConfigAsker["[Server database editor - Autocomplete - Un. description format]", "Unidentified item, can be identified with [Magnifier]."]; }
			set { ConfigAsker["[Server database editor - Autocomplete - Un. description format]"] = value; }
		}

		public static string AutocompleteDescNotSet {
			get { return ConfigAsker["[Server database editor - Autocomplete - Desc not set]", "Description not set..."]; }
			set { ConfigAsker["[Server database editor - Autocomplete - Desc not set]"] = value; }
		}

		public static bool AutocompleteDescShowNeutralPropertyForWeapon {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - Description show neutral prop]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - Description show neutral prop]"] = value.ToString(); }
		}

		public static List<string> AutocompleteProperties {
			get { return Methods.StringToList(ConfigAsker["[Server database editor - Autocomplete - Properties]", "^777777,^996600,^FF0000,^777777,^777777,^777777,^880000,^777777,^777777,^0000FF,^777777"]); }
			set { ConfigAsker["[Server database editor - Autocomplete - Properties]"] = Methods.ListToString(value); }
		}

		public static bool AutocompleteNumberOfSlot {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - Number of slot]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - Number of slot]"] = value.ToString(); }
		}

		public static bool AutocompleteViewId {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - View id]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - View id]"] = value.ToString(); }
		}

		public static bool AutocompleteRewardId {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - AutocompleteRewardId]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - AutocompleteRewardId]"] = value.ToString(); }
		}

		public static bool AutocompleteBuff {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - AutocompleteBuff]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - AutocompleteBuff]"] = value.ToString(); }
		}

		public static bool AutocompleteTitleId {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - AutocompleteTitleId]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - AutocompleteTitleId]"] = value.ToString(); }
		}

		public static bool AutocompleteScore {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - AutocompleteScore]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - AutocompleteScore]"] = value.ToString(); }
		}


		public static bool AutocompleteName {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - AutocompleteName]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - AutocompleteName]"] = value.ToString(); }
		}


		public static bool AutocompleteCount {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Autocomplete - AutocompleteCount]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Autocomplete - AutocompleteCount]"] = value.ToString(); }
		}

		public static bool SyncMobTables {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Client sync - Mob tables]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Client sync - Mob tables]"] = value.ToString(); }
		}

		public static bool SyncWpnTables {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Client sync - Wpn tables]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Client sync - Wpn tables]"] = value.ToString(); }
		}

		public static bool UseOldRAthenaMode {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Settings - Use old rAthena mode]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - Settings - Use old rAthena mode]"] = value.ToString(); }
		}

		public static string SyncMobId {
			get { return ConfigAsker["[Server database editor - Client sync - Mob id]", @"data\luafiles514\lua files\datainfo\npcidentity.lub"]; }
			set { ConfigAsker["[Server database editor - Client sync - Mob id]"] = value; }
		}

		public static string SyncMobName {
			get { return ConfigAsker["[Server database editor - Client sync - Mob name]", @"data\luafiles514\lua files\datainfo\jobname.lub"]; }
			set { ConfigAsker["[Server database editor - Client sync - Mob name]"] = value; }
		}

		public static string SyncAccId {
			get { return ConfigAsker["[Server database editor - Client sync - Acc id]", @"data\luafiles514\lua files\datainfo\accessoryid.lub"]; }
			set { ConfigAsker["[Server database editor - Client sync - Acc id]"] = value; }
		}

		public static string SyncAccName {
			get { return ConfigAsker["[Server database editor - Client sync - Acc name]", @"data\luafiles514\lua files\datainfo\accname.lub"]; }
			set { ConfigAsker["[Server database editor - Client sync - Acc name]"] = value; }
		}

		public static string SyncWeaponId {
			get { return ConfigAsker["[Server database editor - Client sync - Weapon id]", @"data\luafiles514\lua files\datainfo\weapontable.lub"]; }
			set { ConfigAsker["[Server database editor - Client sync - Weapon id]"] = value; }
		}

		public static int DropRatesCard {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Rates - Drop card]", "100"]); }
			set { ConfigAsker["[Server database editor - Rates - Drop card]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int DropRatesCommon {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Rates - Drop common]", "100"]); }
			set { ConfigAsker["[Server database editor - Rates - Drop common]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int DropRatesCommonBoss {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Rates - Drop common boss]", "100"]); }
			set { ConfigAsker["[Server database editor - Rates - Drop common boss]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int DropRatesHeal {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Rates - Drop heal]", "100"]); }
			set { ConfigAsker["[Server database editor - Rates - Drop heal]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int DropRatesHealBoss {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Rates - Drop heal boss]", "100"]); }
			set { ConfigAsker["[Server database editor - Rates - Drop heal boss]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int DropRatesUse {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Rates - Drop use]", "100"]); }
			set { ConfigAsker["[Server database editor - Rates - Drop use]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int DropRatesUseBoss {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Rates - Drop use boss]", "100"]); }
			set { ConfigAsker["[Server database editor - Rates - Drop use boss]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int DropRatesEquip {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Rates - Drop equip]", "100"]); }
			set { ConfigAsker["[Server database editor - Rates - Drop equip]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int DropRatesEquipBoss {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Rates - Drop equip boss]", "100"]); }
			set { ConfigAsker["[Server database editor - Rates - Drop equip boss]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int DropRatesMvp {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Rates - Drop mvp]", "100"]); }
			set { ConfigAsker["[Server database editor - Rates - Drop mvp]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int DropRatesMvpBoss {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Rates - Drop mvp boss]", "100"]); }
			set { ConfigAsker["[Server database editor - Rates - Drop mvp boss]"] = value.ToString(CultureInfo.InvariantCulture); }
		}
	}

	public class GetSetSetting {
		private readonly Action<string> _set;
		private readonly Func<string> _get;

		public GetSetSetting(Action<string> set, Func<string> get) {
			_set = set;
			_get = get;
		}

		public string Value {
			get { return _get(); }
			set { _set(value); }
		}
	}
}