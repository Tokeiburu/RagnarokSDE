using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Database;
using ErrorManager;
using SDE.Core;
using SDE.Editor.Engines;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.Parsers.Libconfig;
using SDE.Editor.Engines.Parsers.Yaml;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.Editor.Writers;
using SDE.View;
using Utilities;

namespace SDE.Editor.Generic.Parsers {
	public sealed class DbIOSkills {
		private static string _loadListToString(ParserObject libList, string id, string value, string def = "") {
			if (libList == null)
				return def;

			string ret = "";

			if (libList is ParserString) {
				ret = libList;
			}
			else {
				Dictionary<int, string> ranges = new Dictionary<int, string>();

				try {
					foreach (var entry in libList) {
						ranges[Int32.Parse(entry[id])] = entry[value];
					}

					int start = 1;
					string previous = "";

					foreach (var entry in ranges.OrderBy(p => p.Key)) {
						if (entry.Key == start) {
							ret += entry.Value + ":";
							previous = entry.Value;
							start = entry.Key + 1;
							continue;
						}

						for (int i = start; i < entry.Key; i++) {
							ret += previous + ":";
						}

						ret += entry.Value + ":";
						previous = entry.Value;
						start = entry.Key + 1;
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}

			return ret.TrimEnd(':');
		}

		public static void Writer(DbDebugItem<int> debug, AbstractDb<int> db) {
			try {
				var itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

				if (debug.FileType == FileType.Yaml) {
					DbIOMethods.DbIOWriter(debug, db, (r, q) => WriteEntryYaml(r, q, itemDb));
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public static void Loader<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			if (debug.FileType == FileType.Yaml) {
				var ele = new YamlParser(debug.FilePath);
				var table = debug.AbsractDb.Table;

				if (ele.Output == null || ((ParserArray)ele.Output).Objects.Count == 0 || (ele.Output["copy_paste"] ?? ele.Output["Body"]) == null)
					return;

				var mobDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);
				var itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

				var stateFlags = new Dictionary<string, long>();
				stateFlags["none"] = 0;
				stateFlags["hidden"] = 1;
				stateFlags["riding"] = 2;
				stateFlags["falcon"] = 3;
				stateFlags["cart"] = 4;
				stateFlags["shield"] = 5;
				stateFlags["recover_weight_rate"] = 6;
				stateFlags["move_enable"] = 7;
				stateFlags["water"] = 8;
				stateFlags["ridingdragon"] = 9;
				stateFlags["wug"] = 10;
				stateFlags["ridingwug"] = 11;
				stateFlags["mado"] = 12;
				stateFlags["elementalspirit"] = 13;
				stateFlags["elementalspirit2"] = 14;
				stateFlags["peco"] = 15;
				stateFlags["sunstance"] = 16;
				stateFlags["moonstance"] = 17;
				stateFlags["starstance"] = 18;
				stateFlags["universestance"] = 19;
				
				foreach (var skill in ele.Output["copy_paste"] ?? ele.Output["Body"]) {
					try {
						int id = Int32.Parse(skill["Id"]);
						TKey skillId = (TKey)(object)id;

						table.SetRaw(skillId, ServerSkillAttributes.Name, skill["Name"] ?? "");
						table.SetRaw(skillId, ServerSkillAttributes.Desc, skill["Description"] ?? "");
						table.SetRaw(skillId, ServerSkillAttributes.MaxLevel, skill["MaxLevel"] ?? "1");
						table.SetRaw(skillId, ServerSkillAttributes.AttackType, Constants.Parse2DbString<AttackTypeType>(skill["Type"] ?? "None"));
						table.SetRaw(skillId, ServerSkillAttributes.SkillTargetType, Constants.Parse2DbString<SkillTargetType>(skill["TargetType"] ?? "0"));
						table.SetRaw(skillId, ServerSkillAttributes.DamageFlags, DbIOUtils.LoadFlag<SkillDamageType>(skill["DamageFlags"], "0"));
						table.SetRaw(skillId, ServerSkillAttributes.Inf2New, DbIOUtils.LoadFlag<SkillType2TypeNew>(skill["Flags"], "0"));
						table.SetRaw(skillId, ServerSkillAttributes.Range, _loadListToString(skill["Range"], "Level", "Size"));
						table.SetRaw(skillId, ServerSkillAttributes.HitMode, Constants.Parse2DbString<HitType>(skill["Hit"] ?? "0"));
						table.SetRaw(skillId, ServerSkillAttributes.HitCount, _loadListToString(skill["HitCount"], "Level", "Count", "0"));
						table.SetRaw(skillId, ServerSkillAttributes.SkillElement, _loadListToString(skill["Element"], "Level", "Element", "Neutral"));
						table.SetRaw(skillId, ServerSkillAttributes.SplashArea, _loadListToString(skill["SplashArea"], "Level", "Area"));
						table.SetRaw(skillId, ServerSkillAttributes.ActiveInstance, _loadListToString(skill["ActiveInstance"], "Level", "Max"));
						table.SetRaw(skillId, ServerSkillAttributes.Knockback, _loadListToString(skill["Knockback"], "Level", "Amount"));

						if (skill["CopyFlags"] != null) {
							var entry = skill["CopyFlags"];

							table.SetRaw(skillId, ServerSkillAttributes.CopyFlags, DbIOUtils.LoadFlag<SkillCopyType>(entry["Skill"], "0"));
							table.SetRaw(skillId, ServerSkillAttributes.CopyFlagsRemovedRequirement, DbIOUtils.LoadFlag<SkillCopyRemoveRequirementType>(entry["RemoveRequirement"], "0"));
						}

						if (skill["NoNearNPC"] != null) {
							var entry = skill["NoNearNPC"];

							table.SetRaw(skillId, ServerSkillAttributes.NoNearNPCRange, entry["AdditionalRange"] ?? "0");
							table.SetRaw(skillId, ServerSkillAttributes.NoNearNPCType, DbIOUtils.LoadFlag<NoNearNpcType>(entry["Type"], "0"));
						}

						table.SetRaw(skillId, ServerSkillAttributes.CastInterrupt, skill["CastCancel"] ?? "false");
						table.SetRaw(skillId, ServerSkillAttributes.CastDefenseReduction, skill["CastDefenseReduction"] ?? "0");
						table.SetRaw(skillId, ServerSkillAttributes.CastingTime, _loadListToString(skill["CastTime"], "Level", "Time"));
						table.SetRaw(skillId, ServerSkillAttributes.AfterCastActDelay, _loadListToString(skill["AfterCastActDelay"], "Level", "Time"));
						table.SetRaw(skillId, ServerSkillAttributes.AfterCastWalkDelay, _loadListToString(skill["AfterCastWalkDelay"], "Level", "Time"));
						table.SetRaw(skillId, ServerSkillAttributes.Duration1, _loadListToString(skill["Duration1"], "Level", "Time"));
						table.SetRaw(skillId, ServerSkillAttributes.Duration2, _loadListToString(skill["Duration2"], "Level", "Time"));
						table.SetRaw(skillId, ServerSkillAttributes.Cooldown, _loadListToString(skill["Cooldown"], "Level", "Time"));
						table.SetRaw(skillId, ServerSkillAttributes.FixedCastTime, _loadListToString(skill["FixedCastTime"], "Level", "Time"));
						table.SetRaw(skillId, ServerSkillAttributes.CastTimeFlags, DbIOUtils.LoadFlag<CastingFlags>(skill["CastTimeFlags"], "0"));
						table.SetRaw(skillId, ServerSkillAttributes.CastDelayFlags, DbIOUtils.LoadFlag<CastingFlags>(skill["CastDelayFlags"], "0"));

						if (skill["Requires"] != null) {
							var entry = skill["Requires"];

							table.SetRaw(skillId, ServerSkillAttributes.RequireHpCost, _loadListToString(entry["HpCost"], "Level", "Amount"));
							table.SetRaw(skillId, ServerSkillAttributes.RequireSpCost, _loadListToString(entry["SpCost"], "Level", "Amount"));
							table.SetRaw(skillId, ServerSkillAttributes.RequireHpRateCost, _loadListToString(entry["HpRateCost"], "Level", "Amount"));
							table.SetRaw(skillId, ServerSkillAttributes.RequireSpRateCost, _loadListToString(entry["SpRateCost"], "Level", "Amount"));
							table.SetRaw(skillId, ServerSkillAttributes.RequireMaxHpTrigger, _loadListToString(entry["MaxHpTrigger"], "Level", "Amount"));
							table.SetRaw(skillId, ServerSkillAttributes.RequireZenyCost, _loadListToString(entry["ZenyCost"], "Level", "Amount"));
							table.SetRaw(skillId, ServerSkillAttributes.RequireWeapons, DbIOUtils.LoadFlag<WeaponType>(entry["Weapon"], "0xFFFFFF"));
							table.SetRaw(skillId, ServerSkillAttributes.RequireAmmoTypes, DbIOUtils.LoadFlag<AmmoType>(entry["Ammo"], "0"));
							table.SetRaw(skillId, ServerSkillAttributes.RequireAmmoAmount, entry["AmmoAmount"] ?? "0");
							table.SetRaw(skillId, ServerSkillAttributes.RequireState, DbIOUtils.LoadFlag(entry["State"], stateFlags, "0"));

							if (entry["Status"] != null) {
								table.SetRaw(skillId, ServerSkillAttributes.RequireStatuses, Methods.Aggregate(entry["Status"].OfType<ParserKeyValue>().Select(p => p.Key).ToList(), ":"));
							}
							else {
								table.SetRaw(skillId, ServerSkillAttributes.RequireStatuses, "");
							}

							table.SetRaw(skillId, ServerSkillAttributes.RequireSpiritSphereCost, _loadListToString(entry["SpiritSphereCost"], "Level", "Amount"));

							if (entry["ItemCost"] != null) {
								StringBuilder b = new StringBuilder();
								var itemList = entry["ItemCost"];

								foreach (var item in itemList) {
									string key = item["Item"];
									int value = Int32.Parse(item["Amount"]);

									key = DbIOUtils.Name2Id(itemDb, ServerItemAttributes.AegisName, key, "item_db", true).ToString();
									b.Append(key);
									b.Append(":");
									b.Append(value);
									b.Append(":");
								}

								table.SetRaw(skillId, ServerSkillAttributes.RequireItemCost, b.ToString().Trim(':'));
							}

							if (entry["Equipment"] != null) {
								StringBuilder b = new StringBuilder();
								var itemList = entry["Equipment"];

								foreach (var item in itemList.OfType<ParserKeyValue>()) {
									string key = item.Key;

									key = DbIOUtils.Name2Id(itemDb, ServerItemAttributes.AegisName, key, "item_db", true).ToString();
									b.Append(key);
									b.Append(":");
								}

								table.SetRaw(skillId, ServerSkillAttributes.RequiredEquipment, b.ToString().Trim(':'));
							}
						}

						if (skill["Unit"] != null) {
							var entry = skill["Unit"];

							table.SetRaw(skillId, ServerSkillAttributes.UnitId, entry["Id"] ?? "");
							table.SetRaw(skillId, ServerSkillAttributes.UnitAlternateId, entry["AlternateId"] ?? "");
							table.SetRaw(skillId, ServerSkillAttributes.UnitLayout, _loadListToString(entry["Layout"], "Level", "Size"));
							table.SetRaw(skillId, ServerSkillAttributes.UnitRange, _loadListToString(entry["Range"], "Level", "Size"));
							table.SetRaw(skillId, ServerSkillAttributes.UnitInterval, entry["Interval"] ?? "0");
							table.SetRaw(skillId, ServerSkillAttributes.UnitTarget, DbIOUtils.LoadFlag<UnitTargetType>(entry["Target"], "0x3F0000"));
							table.SetRaw(skillId, ServerSkillAttributes.UnitFlag, DbIOUtils.LoadFlag<UnitFlagType>(entry["Flag"], "0"));
						}
					}
					catch (FileParserException fpe) {
						debug.ReportIdException(fpe, skill["Id"]);
					}
					catch {
						if (skill["Id"] == null) {
							if (!debug.ReportIdException("#", skill.Line)) return;
						}
						else if (!debug.ReportIdException(skill["Id"], skill.Line)) return;
					}
				}
			}
		}

		private static void _expandList(StringBuilder builder, ReadableTuple<int> tuple, string name, string level, string amount, DbAttribute attribute, string indent1, string indent2, string def, MetaTable<int> itemDb) {
			string value = tuple.GetValue<string>(attribute);

			if (value == def || value == "")
				return;

			string[] data = value.Split(':');
			int k = 1;

			if (data.Length == 1) {
				builder.Append(indent1);
				builder.Append(name);
				builder.Append(": ");
				builder.AppendLine(data[0]);
				return;
			}

			builder.Append(indent1);
			builder.Append(name);
			builder.AppendLine(":");

			if (attribute == ServerSkillAttributes.RequireItemCost) {
				for (int i = 0; i < data.Length; i += 2) {
					builder.Append(level);
					builder.Append(": ");
					builder.AppendLine(DbIOUtils.Id2Name(itemDb, ServerItemAttributes.AegisName, data[i]));

					if (i + 1 < data.Length) {
						builder.Append(indent2);
						builder.Append(amount);
						builder.Append(": ");
						builder.AppendLine(data[i + 1]);
					}
					else {
						builder.Append(indent2);
						builder.Append(amount);
						builder.AppendLine(": 0");
					}
				}

				return;
			}

			foreach (var field in data) {
				if (field == "") {
					k++;
					continue;
				}

				builder.Append(level);
				builder.Append(": ");
				builder.AppendLine(k.ToString(CultureInfo.InvariantCulture));
				k++;

				builder.Append(indent2);
				builder.Append(amount);
				builder.Append(": ");
				builder.AppendLine(field);
			}
		}

		public static void WriteEntryYaml(StringBuilder builder, ReadableTuple<int> tuple, MetaTable<int> itemDb) {
			if (tuple != null) {
				int value;
				bool valueB;
				string valueS;

				builder.AppendLine("  - Id: " + tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture));

				if ((valueS = tuple.GetValue<string>(ServerSkillAttributes.Name)) != "") {
					builder.AppendLine("    Name: " + DbIOUtils.QuoteCheck(valueS));
				}

				if ((valueS = tuple.GetValue<string>(ServerSkillAttributes.Desc)) != "") {
					builder.AppendLine("    Description: " + DbIOUtils.QuoteCheck(valueS));
				}

				builder.AppendLine("    MaxLevel: " + tuple.GetValue<int>(ServerSkillAttributes.MaxLevel));

				if ((value = tuple.GetValue<int>(ServerSkillAttributes.AttackType)) != 0) {
					builder.AppendLine("    Type: " + Constants.ToString<AttackTypeType>(value));
				}

				if ((value = tuple.GetValue<int>(ServerSkillAttributes.SkillTargetType)) != 0) {
					builder.AppendLine("    TargetType: " + Constants.ToString<SkillTargetType>(value));
				}

				DbIOUtils.ExpandFlag<SkillDamageType>(builder, tuple, "DamageFlags", ServerSkillAttributes.DamageFlags, YamlParser.Indent4, YamlParser.Indent6);
				DbIOUtils.ExpandFlag<SkillType2TypeNew>(builder, tuple, "Flags", ServerSkillAttributes.Inf2New, YamlParser.Indent4, YamlParser.Indent6);

				_expandList(builder, tuple, "Range", "      - Level", "Size", ServerSkillAttributes.Range, YamlParser.Indent4, YamlParser.Indent8, "0", itemDb);

				if ((value = tuple.GetValue<int>(ServerSkillAttributes.HitMode)) != 0) {
					builder.AppendLine("    Hit: " + Constants.ToString<HitType>(value));
				}

				_expandList(builder, tuple, "HitCount", "      - Level", "Count", ServerSkillAttributes.HitCount, YamlParser.Indent4, YamlParser.Indent8, "0", itemDb);
				_expandList(builder, tuple, "Element", "      - Level", "Element", ServerSkillAttributes.SkillElement, YamlParser.Indent4, YamlParser.Indent8, "Neutral", itemDb);
				_expandList(builder, tuple, "SplashArea", "      - Level", "Area", ServerSkillAttributes.SplashArea, YamlParser.Indent4, YamlParser.Indent8, "0", itemDb);
				_expandList(builder, tuple, "ActiveInstance", "      - Level", "Max", ServerSkillAttributes.ActiveInstance, YamlParser.Indent4, YamlParser.Indent8, "0", itemDb);
				_expandList(builder, tuple, "Knockback", "      - Level", "Amount", ServerSkillAttributes.Knockback, YamlParser.Indent4, YamlParser.Indent8, "0", itemDb);

				if (tuple.GetValue<int>(ServerSkillAttributes.CopyFlags) > 0 || tuple.GetValue<int>(ServerSkillAttributes.CopyFlagsRemovedRequirement) > 0) {
					builder.AppendLine("    CopyFlags:");

					DbIOUtils.ExpandFlag<SkillCopyType>(builder, tuple, "Skill", ServerSkillAttributes.CopyFlags, YamlParser.Indent6, YamlParser.Indent8);
					DbIOUtils.ExpandFlag<SkillCopyRemoveRequirementType>(builder, tuple, "RemoveRequirement", ServerSkillAttributes.CopyFlagsRemovedRequirement, YamlParser.Indent6, YamlParser.Indent8);
				}

				if (tuple.GetValue<int>(ServerSkillAttributes.NoNearNPCRange) > 0 || tuple.GetValue<int>(ServerSkillAttributes.NoNearNPCType) > 0) {
					builder.AppendLine("    NoNearNPC:");

					if ((value = tuple.GetValue<int>(ServerSkillAttributes.NoNearNPCRange)) != 0) {
						builder.AppendLine("      AdditionalRange: " + value);
					}

					DbIOUtils.ExpandFlag<NoNearNpcType>(builder, tuple, "Type", ServerSkillAttributes.NoNearNPCType, YamlParser.Indent6, YamlParser.Indent8);
				}

				if ((valueB = tuple.GetValue<bool>(ServerSkillAttributes.CastInterrupt)) != false) {
					builder.AppendLine("    CastCancel: " + (valueB ? "true" : "false"));
				}

				if ((value = tuple.GetValue<int>(ServerSkillAttributes.CastDefenseReduction)) != 0) {
					builder.AppendLine("    CastDefenseReduction: " + value);
				}

				_expandList(builder, tuple, "CastTime", "      - Level", "Time", ServerSkillAttributes.CastingTime, YamlParser.Indent4, YamlParser.Indent8, "0", itemDb);
				_expandList(builder, tuple, "AfterCastActDelay", "      - Level", "Time", ServerSkillAttributes.AfterCastActDelay, YamlParser.Indent4, YamlParser.Indent8, "0", itemDb);
				_expandList(builder, tuple, "AfterCastWalkDelay", "      - Level", "Time", ServerSkillAttributes.AfterCastWalkDelay, YamlParser.Indent4, YamlParser.Indent8, "0", itemDb);
				_expandList(builder, tuple, "Duration1", "      - Level", "Time", ServerSkillAttributes.Duration1, YamlParser.Indent4, YamlParser.Indent8, "0", itemDb);
				_expandList(builder, tuple, "Duration2", "      - Level", "Time", ServerSkillAttributes.Duration2, YamlParser.Indent4, YamlParser.Indent8, "0", itemDb);
				_expandList(builder, tuple, "Cooldown", "      - Level", "Time", ServerSkillAttributes.Cooldown, YamlParser.Indent4, YamlParser.Indent8, "0", itemDb);
				_expandList(builder, tuple, "FixedCastTime", "      - Level", "Time", ServerSkillAttributes.FixedCastTime, YamlParser.Indent4, YamlParser.Indent8, "0", itemDb);
				DbIOUtils.ExpandFlag<CastingFlags>(builder, tuple, "CastTimeFlags", ServerSkillAttributes.CastTimeFlags, YamlParser.Indent4, YamlParser.Indent6);
				DbIOUtils.ExpandFlag<CastingFlags>(builder, tuple, "CastDelayFlags", ServerSkillAttributes.CastDelayFlags, YamlParser.Indent4, YamlParser.Indent6);

				StringBuilder require = new StringBuilder();
				_expandList(require, tuple, "HpCost", "        - Level", "Amount", ServerSkillAttributes.RequireHpCost, YamlParser.Indent6, YamlParser.Indent10, "0", itemDb);
				_expandList(require, tuple, "SpCost", "        - Level", "Amount", ServerSkillAttributes.RequireSpCost, YamlParser.Indent6, YamlParser.Indent10, "0", itemDb);
				_expandList(require, tuple, "HpRateCost", "        - Level", "Amount", ServerSkillAttributes.RequireHpRateCost, YamlParser.Indent6, YamlParser.Indent10, "0", itemDb);
				_expandList(require, tuple, "SpRateCost", "        - Level", "Amount", ServerSkillAttributes.RequireSpRateCost, YamlParser.Indent6, YamlParser.Indent10, "0", itemDb);
				_expandList(require, tuple, "MaxHpTrigger", "        - Level", "Amount", ServerSkillAttributes.RequireMaxHpTrigger, YamlParser.Indent6, YamlParser.Indent10, "0", itemDb);
				_expandList(require, tuple, "ZenyCost", "        - Level", "Amount", ServerSkillAttributes.RequireZenyCost, YamlParser.Indent6, YamlParser.Indent10, "0", itemDb);

				value = tuple.GetValue<int>(ServerSkillAttributes.RequireWeapons);

				if (value != 0xFFFFFF) {
					DbIOUtils.ExpandFlag<WeaponType>(require, tuple, "Weapon", ServerSkillAttributes.RequireWeapons, YamlParser.Indent6, YamlParser.Indent8);
				}

				DbIOUtils.ExpandFlag<AmmoType>(require, tuple, "Ammo", ServerSkillAttributes.RequireAmmoTypes, YamlParser.Indent6, YamlParser.Indent8);
				_expandList(require, tuple, "AmmoAmount", "        - Level", "Amount", ServerSkillAttributes.RequireAmmoAmount, YamlParser.Indent6, YamlParser.Indent10, "0", itemDb);

				if ((value = tuple.GetValue<int>(ServerSkillAttributes.RequireState)) > 0) {
					valueS = "";
					
					switch((RequiredStateTypeNew)value) {
						case RequiredStateTypeNew.Hidden:
							valueS = "Hidden";
							break;
						case RequiredStateTypeNew.Riding:
							valueS = "Riding";
							break;
						case RequiredStateTypeNew.Falcon:
							valueS = "Falcon";
							break;
						case RequiredStateTypeNew.Cart:
							valueS = "Cart";
							break;
						case RequiredStateTypeNew.Shield:
							valueS = "Shield";
							break;
						case RequiredStateTypeNew.RecoverWeightRate:
							valueS = "Recover_Weight_Rate";
							break;
						case RequiredStateTypeNew.MoveEnable:
							valueS = "Move_Enable";
							break;
						case RequiredStateTypeNew.Water:
							valueS = "Water";
							break;
						case RequiredStateTypeNew.RidingDragon:
							valueS = "Ridingdragon";
							break;
						case RequiredStateTypeNew.Warg:
							valueS = "Wug";
							break;
						case RequiredStateTypeNew.Ridingwarg:
							valueS = "Ridingwug";
							break;
						case RequiredStateTypeNew.Mado:
							valueS = "Mado";
							break;
						case RequiredStateTypeNew.Elementalspirit:
							valueS = "Elementalspirit";
							break;
						case RequiredStateTypeNew.Elementalspirit2:
							valueS = "Elementalspirit2";
							break;
						case RequiredStateTypeNew.RidingPeco:
							valueS = "Peco";
							break;
						case RequiredStateTypeNew.SunStance:
							valueS = "Sunstance";
							break;
						case RequiredStateTypeNew.MoonStance:
							valueS = "Moonstance";
							break;
						case RequiredStateTypeNew.StarsStance:
							valueS = "Starstance";
							break;
						case RequiredStateTypeNew.UniverseStance:
							valueS = "Universestance";
							break;
						default:
							valueS = "";
							break;
					}

					require.Append("      State: ");
					require.AppendLine(valueS);
				}

				if ((valueS = tuple.GetValue<string>(ServerSkillAttributes.RequireStatuses)) != "" && valueS != "0") {
					var data = valueS.Split(':');

					if (data.Length > 0) {
						require.AppendLine("      Status:");

						foreach (var da in data) {
							require.Append("        ");
							require.Append(da);
							require.AppendLine(": true");
						}
					}
				}
				
				//_DbIOUtils.ExpandFlag<AmmoType>(require, tuple, "Status", ServerSkillAttributes.RequireStatuses, YamlParser2.Indent6, YamlParser2.Indent8);
				_expandList(require, tuple, "SpiritSphereCost", "        - Level", "Amount", ServerSkillAttributes.RequireSpiritSphereCost, YamlParser.Indent6, YamlParser.Indent10, "0", itemDb);
				_expandList(require, tuple, "ItemCost", "        - Item", "Amount", ServerSkillAttributes.RequireItemCost, YamlParser.Indent6, YamlParser.Indent10, "0", itemDb);

				if ((valueS = tuple.GetValue<string>(ServerSkillAttributes.RequiredEquipment)) != "" && valueS != "0") {
					var data = valueS.Split(':');
					
					require.AppendLine("      Equipment:");

					foreach (var item in data) {
						require.Append("        ");
						require.Append(DbIOUtils.Id2Name(itemDb, ServerItemAttributes.AegisName, item));
						require.AppendLine(": true");
					}
				}
				//_expandList(require, tuple, "Equipment", "", "", ServerSkillAttributes.RequiredEquipment, YamlParser2.Indent6, YamlParser2.Indent8, "", itemDb);

				string requireData = require.ToString();

				if (requireData != "") {
					builder.AppendLine("    Requires:");
					builder.Append(requireData);
				}

				StringBuilder unit = new StringBuilder();

				if ((valueS = tuple.GetValue<string>(ServerSkillAttributes.UnitId)) != "" && valueS != "0") {
					unit.Append("      Id: ");
					unit.AppendLine(valueS);
				}

				if ((valueS = tuple.GetValue<string>(ServerSkillAttributes.UnitAlternateId)) != "" && valueS != "0") {
					unit.Append("      AlternateId: ");
					unit.AppendLine(valueS);
				}

				_expandList(unit, tuple, "Layout", "        - Level", "Size", ServerSkillAttributes.UnitLayout, YamlParser.Indent6, YamlParser.Indent10, "0", itemDb);
				_expandList(unit, tuple, "Range", "        - Level", "Size", ServerSkillAttributes.UnitRange, YamlParser.Indent6, YamlParser.Indent10, "0", itemDb);

				if ((value = tuple.GetValue<int>(ServerSkillAttributes.UnitInterval)) != 0) {
					unit.Append("      Interval: ");
					unit.AppendLine(value.ToString(CultureInfo.InvariantCulture));
				}

				if ((value = tuple.GetValue<int>(ServerSkillAttributes.UnitTarget)) != 0x3F0000) {
					unit.Append("      Target: ");

					var flag = FlagsManager.GetFlag<UnitTargetType>();
					valueS = flag.Value2Name[value];
					unit.AppendLine(valueS);
				}

				DbIOUtils.ExpandFlag<UnitFlagType>(unit, tuple, "Flag", ServerSkillAttributes.UnitFlag, YamlParser.Indent6, YamlParser.Indent8);

				string unitData = unit.ToString();

				if (unitData != "") {
					builder.AppendLine("    Unit:");
					builder.Append(unit);
				}
			}
		}

		public static void DbSkillsCastCommaRange<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, int from, int length) {
			try {
				IntLineStream lines = new IntLineStream(debug.OldPath);
				lines.Remove(db);
				string line;

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					List<string> items = tuple.GetRawElements().Skip(from).Take(length).Select(p => p.ToString()).ToList();

					if (items.All(p => p == "0")) {
						lines.Delete(key);
						continue;
					}

					line = string.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture) }.Concat(items).ToArray());
					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbSkillsNoDexCommaRange<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, int from, int length) {
			try {
				IntLineStream lines = new IntLineStream(debug.OldPath);
				lines.Remove(db);
				string line;

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					List<string> items = tuple.GetRawElements().Skip(from).Take(length).Select(p => p.ToString()).ToList();

					if (items.All(p => p == "0")) {
						lines.Delete(key);
						continue;
					}

					string item1 = tuple.GetValue<string>(from);
					string item2 = tuple.GetValue<string>(from + 1);

					if (item1 != "0" && item2 == "0") {
						line = string.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), item1 }.ToArray());
					}
					else {
						line = string.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), item1, item2 }.ToArray());
					}

					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbSkillsNoCastCommaRange<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, int from, int length) {
			try {
				IntLineStream lines = new IntLineStream(debug.OldPath);
				lines.Remove(db);
				string line;

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					string item1 = tuple.GetValue<string>(from);
					string item2 = tuple.GetValue<string>(from + 1);

					if (item1 != "0" && item2 == "0") {
						line = string.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), item1 });
					}
					else if (item1 == "0" && item2 == "0") {
						lines.Delete(key);
						continue;
					}
					else {
						line = string.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), item1, item2 });
					}

					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}
	}
}