using System;
using System.Globalization;
using System.Text;
using SDE.Tools.DatabaseEditor.Generic;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.Lists.DbAttributeHelpers;

namespace SDE.Tools.DatabaseEditor.Engines.Parsers {
	public partial class SqlParser {
		public static string RAthenaMobSkillDbSqlHeader = "#\r\n" +
		                                                  "# Table structure for table `{0}`\r\n" +
		                                                  "#\r\n" +
		                                                  "\r\n" +
														  "DROP TABLE IF EXISTS `{0}`;\r\n" +
														  "CREATE TABLE IF NOT EXISTS `{0}` (\r\n" +
		                                                  "  `MOB_ID` smallint(6) NOT NULL,\r\n" +
		                                                  "  `INFO` text NOT NULL,\r\n" +
		                                                  "  `STATE` text NOT NULL,\r\n" +
		                                                  "  `SKILL_ID` smallint(6) NOT NULL,\r\n" +
		                                                  "  `SKILL_LV` tinyint(4) NOT NULL,\r\n" +
		                                                  "  `RATE` smallint(4) NOT NULL,\r\n" +
		                                                  "  `CASTTIME` mediumint(9) NOT NULL,\r\n" +
		                                                  "  `DELAY` int(9) NOT NULL,\r\n" +
		                                                  "  `CANCELABLE` text NOT NULL,\r\n" +
		                                                  "  `TARGET` text NOT NULL,\r\n" +
		                                                  "  `CONDITION` text NOT NULL,\r\n" +
		                                                  "  `CONDITION_VALUE` text,\r\n" +
		                                                  "  `VAL1` mediumint(9) DEFAULT NULL,\r\n" +
		                                                  "  `VAL2` mediumint(9) DEFAULT NULL,\r\n" +
		                                                  "  `VAL3` mediumint(9) DEFAULT NULL,\r\n" +
		                                                  "  `VAL4` mediumint(9) DEFAULT NULL,\r\n" +
		                                                  "  `VAL5` mediumint(9) DEFAULT NULL,\r\n" +
		                                                  "  `EMOTION` text,\r\n" +
		                                                  "  `CHAT` text\r\n" +
		                                                  ") ENGINE=MyISAM;\r\n" +
		                                                  "\r\n";
		public static string HerculesMobSkillDbSqlHeader = "--\n" +
		                                                   "-- Table structure for table `{0}`\n" +
		                                                   "--\n" +
		                                                   "\n" +
														   "DROP TABLE IF EXISTS `{0}`;\n" +
														   "CREATE TABLE `{0}` (\n" +
		                                                   "  `MOB_ID` SMALLINT(6) NOT NULL,\n" +
		                                                   "  `INFO` TEXT NOT NULL,\n" +
		                                                   "  `STATE` TEXT NOT NULL,\n" +
		                                                   "  `SKILL_ID` SMALLINT(6) NOT NULL,\n" +
		                                                   "  `SKILL_LV` TINYINT(4) NOT NULL,\n" +
		                                                   "  `RATE` SMALLINT(4) NOT NULL,\n" +
		                                                   "  `CASTTIME` MEDIUMINT(9) NOT NULL,\n" +
		                                                   "  `DELAY` INT(9) NOT NULL,\n" +
		                                                   "  `CANCELABLE` TEXT NOT NULL,\n" +
		                                                   "  `TARGET` TEXT NOT NULL,\n" +
		                                                   "  `CONDITION` TEXT NOT NULL,\n" +
		                                                   "  `CONDITION_VALUE` TEXT,\n" +
		                                                   "  `VAL1` MEDIUMINT(9) DEFAULT NULL,\n" +
		                                                   "  `VAL2` MEDIUMINT(9) DEFAULT NULL,\n" +
		                                                   "  `VAL3` MEDIUMINT(9) DEFAULT NULL,\n" +
		                                                   "  `VAL4` MEDIUMINT(9) DEFAULT NULL,\n" +
		                                                   "  `VAL5` MEDIUMINT(9) DEFAULT NULL,\n" +
		                                                   "  `EMOTION` TEXT,\n" +
		                                                   "  `CHAT` TEXT\n" +
		                                                   ") ENGINE=MyISAM;\n" +
		                                                   "\n";

		public static string HerculesMobDbSqlHeader = "--\n" +
		                                             "-- Table structure for table `{0}`\n" +
		                                             "--\n" +
		                                             "\n" +
													 "DROP TABLE IF EXISTS `{0}`;\n" +
													 "CREATE TABLE `{0}` (\n" +
		                                             "  `ID` MEDIUMINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Sprite` TEXT NOT NULL,\n" +
		                                             "  `kName` TEXT NOT NULL,\n" +
		                                             "  `iName` TEXT NOT NULL,\n" +
		                                             "  `LV` TINYINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `HP` INT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `SP` MEDIUMINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `EXP` MEDIUMINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `JEXP` MEDIUMINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Range1` TINYINT(4) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `ATK1` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `ATK2` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `DEF` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `MDEF` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `STR` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `AGI` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `VIT` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `INT` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `DEX` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `LUK` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Range2` TINYINT(4) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Range3` TINYINT(4) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Scale` TINYINT(4) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Race` TINYINT(4) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Element` TINYINT(4) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Mode` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Speed` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `aDelay` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `aMotion` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `dMotion` SMALLINT(6) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `MEXP` MEDIUMINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `MVP1id` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `MVP1per` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `MVP2id` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `MVP2per` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `MVP3id` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `MVP3per` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop1id` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop1per` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop2id` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop2per` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop3id` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop3per` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop4id` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop4per` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop5id` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop5per` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop6id` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop6per` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop7id` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop7per` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop8id` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop8per` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop9id` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `Drop9per` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `DropCardid` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  `DropCardper` SMALLINT(9) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                             "  PRIMARY KEY (`ID`)\n" +
		                                             ") ENGINE=MyISAM;\n" +
		                                             "\n";

		public static string HerculesItemDbSqlHeader = "-- NOTE: This file was auto-generated and should never be manually edited,\n" +
		                                               "--       as it will get overwritten. If you need to modify this file,\n" +
		                                               "--       please consider modifying the corresponding .conf file inside\n" +
		                                               "--       the db folder, and then re-run the db2sql plugin.\n" +
		                                               "\n" +
		                                               "--\n" +
		                                               "-- Table structure for table `{0}`\n" +
		                                               "--\n" +
		                                               "\n" +
													   "DROP TABLE IF EXISTS `{0}`;\n" +
													   "CREATE TABLE `{0}` (\n" +
		                                               "  `id` smallint(5) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                               "  `name_english` varchar(50) NOT NULL DEFAULT '',\n" +
		                                               "  `name_japanese` varchar(50) NOT NULL DEFAULT '',\n" +
		                                               "  `type` tinyint(2) UNSIGNED NOT NULL DEFAULT '0',\n" +
		                                               "  `price_buy` mediumint(10) DEFAULT NULL,\n" +
		                                               "  `price_sell` mediumint(10) DEFAULT NULL,\n" +
		                                               "  `weight` smallint(5) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `atk` smallint(5) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `matk` smallint(5) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `defence` smallint(5) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `range` tinyint(2) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `slots` tinyint(2) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `equip_jobs` int(12) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `equip_upper` tinyint(8) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `equip_genders` tinyint(2) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `equip_locations` smallint(4) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `weapon_level` tinyint(2) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `equip_level_min` smallint(5) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `equip_level_max` smallint(5) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `refineable` tinyint(1) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `view` smallint(3) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `bindonequip` tinyint(1) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `buyingstore` tinyint(1) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `delay` mediumint(9) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `trade_flag` smallint(4) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `trade_group` smallint(3) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `nouse_flag` smallint(4) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `nouse_group` smallint(4) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `stack_amount` mediumint(6) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `stack_flag` tinyint(2) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `sprite` mediumint(6) UNSIGNED DEFAULT NULL,\n" +
		                                               "  `script` text,\n" +
		                                               "  `equip_script` text,\n" +
		                                               "  `unequip_script` text,\n" +
		                                               " PRIMARY KEY (`id`)\n" +
		                                               ") ENGINE=MyISAM;\n" +
		                                               "\n";

		public static string RAthenaMobDbSqlHeader = "#\r\n" +
													 "# Table structure for table `{0}`\r\n" +
		                                             "#\r\n" +
		                                             "\r\n" +
													 "DROP TABLE IF EXISTS `{0}`;\r\n" +
													 "CREATE TABLE `{0}` (\r\n" +
		                                             "  `ID` mediumint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Sprite` text NOT NULL,\r\n" +
		                                             "  `kName` text NOT NULL,\r\n" +
		                                             "  `iName` text NOT NULL,\r\n" +
		                                             "  `LV` tinyint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `HP` int(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `SP` mediumint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `EXP` mediumint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `JEXP` mediumint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Range1` tinyint(4) unsigned NOT NULL default '0',\r\n" +
		                                             "  `ATK1` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `ATK2` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `DEF` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `MDEF` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `STR` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `AGI` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `VIT` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `INT` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `DEX` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `LUK` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Range2` tinyint(4) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Range3` tinyint(4) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Scale` tinyint(4) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Race` tinyint(4) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Element` tinyint(4) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Mode` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Speed` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `aDelay` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `aMotion` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `dMotion` smallint(6) unsigned NOT NULL default '0',\r\n" +
		                                             "  `MEXP` mediumint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `MVP1id` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `MVP1per` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `MVP2id` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `MVP2per` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `MVP3id` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `MVP3per` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop1id` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop1per` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop2id` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop2per` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop3id` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop3per` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop4id` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop4per` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop5id` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop5per` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop6id` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop6per` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop7id` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop7per` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop8id` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop8per` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop9id` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `Drop9per` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `DropCardid` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  `DropCardper` smallint(9) unsigned NOT NULL default '0',\r\n" +
		                                             "  PRIMARY KEY  (`ID`)\r\n" +
		                                             ") ENGINE=MyISAM;\r\n" +
		                                             "\r\n";

		public static string RAthenaItemDbSqlHeader = "#\r\n" +
		                        "# Table structure for table `{0}`\r\n" +
		                        "#\r\n" +
		                        "\r\n" +
								"DROP TABLE IF EXISTS `{0}`;\r\n" +
								"CREATE TABLE `{0}` (\r\n" +
		                        "  `id` smallint(5) unsigned NOT NULL DEFAULT '0',\r\n" +
		                        "  `name_english` varchar(50) NOT NULL DEFAULT '',\r\n" +
		                        "  `name_japanese` varchar(50) NOT NULL DEFAULT '',\r\n" +
		                        "  `type` tinyint(2) unsigned NOT NULL DEFAULT '0',\r\n" +
		                        "  `price_buy` mediumint(8) unsigned DEFAULT NULL,\r\n" +
		                        "  `price_sell` mediumint(8) unsigned DEFAULT NULL,\r\n" +
		                        "  `weight` smallint(5) unsigned NOT NULL DEFAULT '0',\r\n" +
		                        "  `attack` smallint(5) unsigned DEFAULT NULL,\r\n" +
		                        "  `defence` smallint(5) unsigned DEFAULT NULL,\r\n" +
		                        "  `range` tinyint(2) unsigned DEFAULT NULL,\r\n" +
		                        "  `slots` tinyint(2) unsigned DEFAULT NULL,\r\n" +
		                        "  `equip_jobs` int(10) unsigned DEFAULT NULL,\r\n" +
		                        "  `equip_upper` tinyint(2) unsigned DEFAULT NULL,\r\n" +
		                        "  `equip_genders` tinyint(1) unsigned DEFAULT NULL,\r\n" +
		                        "  `equip_locations` mediumint(7) unsigned DEFAULT NULL,\r\n" +
		                        "  `weapon_level` tinyint(1) unsigned DEFAULT NULL,\r\n" +
		                        "  `equip_level` tinyint(3) unsigned DEFAULT NULL,\r\n" +
		                        "  `refineable` tinyint(1) unsigned DEFAULT NULL,\r\n" +
		                        "  `view` smallint(5) unsigned DEFAULT NULL,\r\n" +
		                        "  `script` text,\r\n" +
		                        "  `equip_script` text,\r\n" +
		                        "  `unequip_script` text,\r\n" +
		                        "  PRIMARY KEY (`id`)\r\n" +
		                        ") ENGINE=MyISAM;\r\n" +
		                        "\r\n";

		public static string RAthenaItemDbSqlHeaderRenewal = "#\r\n" +
								"# Table structure for table `{0}`\r\n" +
								"#\r\n" +
								"\r\n" +
								"DROP TABLE IF EXISTS `{0}`;\r\n" +
								"CREATE TABLE `{0}` (\r\n" +
								"  `id` smallint(5) unsigned NOT NULL DEFAULT '0',\r\n" +
								"  `name_english` varchar(50) NOT NULL DEFAULT '',\r\n" +
								"  `name_japanese` varchar(50) NOT NULL DEFAULT '',\r\n" +
								"  `type` tinyint(2) unsigned NOT NULL DEFAULT '0',\r\n" +
								"  `price_buy` mediumint(8) unsigned DEFAULT NULL,\r\n" +
								"  `price_sell` mediumint(8) unsigned DEFAULT NULL,\r\n" +
								"  `weight` smallint(5) unsigned NOT NULL DEFAULT '0',\r\n" +
								"  `atk:matk` varchar(11) DEFAULT NULL,\r\n" +
								"  `defence` smallint(5) unsigned DEFAULT NULL,\r\n" +
								"  `range` tinyint(2) unsigned DEFAULT NULL,\r\n" +
								"  `slots` tinyint(2) unsigned DEFAULT NULL,\r\n" +
								"  `equip_jobs` int(10) unsigned DEFAULT NULL,\r\n" +
								"  `equip_upper` tinyint(2) unsigned DEFAULT NULL,\r\n" +
								"  `equip_genders` tinyint(1) unsigned DEFAULT NULL,\r\n" +
								"  `equip_locations` mediumint(7) unsigned DEFAULT NULL,\r\n" +
								"  `weapon_level` tinyint(1) unsigned DEFAULT NULL,\r\n" +
								"  `equip_level` varchar(10) DEFAULT NULL,\r\n" +
								"  `refineable` tinyint(1) unsigned DEFAULT NULL,\r\n" +
								"  `view` smallint(5) unsigned DEFAULT NULL,\r\n" +
								"  `script` text,\r\n" +
								"  `equip_script` text,\r\n" +
								"  `unequip_script` text,\r\n" +
								"  PRIMARY KEY (`id`)\r\n" +
								") ENGINE=MyISAM;\r\n" +
								"\r\n";

		private static string _addCommaIfNotNull(string item) {
			if (item == "NULL" || item == "")
				return "NULL";
			if (item.Length >= 2 && item[0] == '\'' && item[item.Length - 1] == '\'')
				return item;
			return "'" + item + "'";
		}
			
		private static string _parse(string item) {
			return item.Replace("'", @"\'").Replace("\\\"", "\\\\\\\"").Trim(' ');
		}

		private static string _parseHerc(string item, bool trim = true) {
			StringBuilder builder = new StringBuilder();

			char c;
			for (int i = 0; i < item.Length; i++) {
				c = item[i];
				switch(c) {
					case '\'': builder.Append(@"\'"); break;
					case '\\': builder.Append(@"\\"); break;
					case '\"': builder.Append("\\\""); break;
					default: builder.Append(c); break;
				}
			}

			if (trim)
				return builder.ToString().Trim(' ');
			return builder.ToString();
		}

		private static string _parseAndSetToInteger(object obj) {
			string item = obj.ToString();
			var res = _parseHerc(item);

			if (res.StartsWith("0x") || res.StartsWith("0X")) {
				try {
					int ival = Convert.ToInt32(res, 16);

					if (ival >= -1)
						return ((uint)ival).ToString(CultureInfo.InvariantCulture);
					else {
						// Removes the first 0xF byte)
						return ((uint) (ival + 0x80000000)).ToString(CultureInfo.InvariantCulture);
					}
				}
				catch {
					return res;
				}
			}

			return res;
		}

		private static string _parseEquip(string item, bool isMin) {
			string[] items = item.Split(',');

			if (items.Length == 1 && !isMin) {
				return "NULL";
			}

			return _parseAndSetToInteger(isMin ? items[0].TrimStart('[') : items[1].TrimEnd(']'));
		}

		private static string _defaultNull(string item) {
			if (string.IsNullOrEmpty(item))
				return "NULL";
			return item;
		}

		private static string _script(string item) {
			string val = ValueConverters.GetScriptNoBracketsSetScriptWithBrackets.ConvertFrom<string>(null, item).Trim(' ');
			if (string.IsNullOrEmpty(val))
				return "NULL";
			return "'" + val + "'";
		}

		private static string _scriptNotNull(string item) {
			string val = ValueConverters.GetScriptNoBracketsSetScriptWithBrackets.ConvertFrom<string>(null, item).Trim(' ');
			return "'" + val + "'";
		}

		private static string _notNull(string item) {
			if (string.IsNullOrEmpty(item))
				return "";
			return item;
		}

		private static string _notNullDefault(string item, string @default) {
			if (string.IsNullOrEmpty(item))
				return @default;
			return item;
		}

		private static string _settableInt<T>(string item) where T : ISettable, new() {
			T temp = new T();
			temp.Set(item);
			return temp.GetInt().ToString(CultureInfo.InvariantCulture);
		}

		private static string _settableOverride<T>(string item) where T : ISettable, new() {
			T temp = new T();
			temp.Set(item);
			return temp.Override == "100" ? "" : temp.Override;
		}

		private static string _buy<TKey>(string value, ReadableTuple<TKey> tuple) {
			if (value == "0") {
				string val = tuple.GetValue<string>(ServerItemAttributes.Sell);
				int ival;

				if (Int32.TryParse(val, out ival)) {
					return (2 * ival).ToString(CultureInfo.InvariantCulture);
				}

				return "0";
			}

			return value;
		}

		private static string _sell<TKey>(string value, ReadableTuple<TKey> tuple) {
			if (value == "0") {
				string val = tuple.GetValue<string>(ServerItemAttributes.Buy);
				int ival;

				if (Int32.TryParse(val, out ival)) {
					return (ival / 2).ToString(CultureInfo.InvariantCulture);
				}

				return "0";
			}

			return value;
		}

		private static string _stringOrInt(string value) {
			if (value == "")
				return value;

			int ival;
			if (Int32.TryParse(value, out ival) || value.StartsWith("0x") || value.StartsWith("0X")) {
				return value;
			}

			return "'" + value + "'";
		}
	}
}
