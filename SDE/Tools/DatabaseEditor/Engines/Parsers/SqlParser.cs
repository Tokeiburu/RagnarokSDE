using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDE.Tools.DatabaseEditor.Engines.Parsers {
	public class SqlParser {
		public string Header =
			"-- NOTE: This file was auto-generated and should never be manually edited,\n" +
			"--       as it will get overwritten. If you need to modify this file,\n" +
			"--       please consider modifying the corresponding .conf file inside\n" +
			"--       the db folder, and then re-run the db2sql plugin.\n" +
			"\n" +
			"--\n" +
			"-- Table structure for table `%s`\n" +
			"--\n" +
			"\n" +
			"DROP TABLE IF EXISTS `%s`;\n" +
			"CREATE TABLE `%s` (\n" +
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
	}
}
