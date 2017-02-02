using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.FileFormats.LubFormat;
using GRF.IO;
using GRF.Threading;
using Lua;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines.LuaEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Items;
using SDE.View.ObjectView;
using Utilities;
using Utilities.Services;

namespace SDE.Editor.Engines.RepairEngine {
	public partial class DbValidationEngine {
		public void FindClientItemErrors(List<ValidationErrorView> errors) {
			_startTask(() => _findClientItemErrors(errors));
		}

		private void _findClientItemErrors(List<ValidationErrorView> errors) {
			var itemDb = _database.GetMetaTable<int>(ServerDbs.Items);
			var citemDb = _database.GetMetaTable<int>(ServerDbs.CItems);

			int current = 0;
			int totalEntries = citemDb.FastItems.Count;

			Dictionary<int, int> viewIdToWepClass = _getWeaponClasses();

			//Expansion_Weapon_IDs
			foreach (var citem in citemDb.FastItems) {
				AProgress.IsCancelling(this);
				Progress = (float)current / totalEntries * 100f;
				current++;

				var sitem = itemDb.TryGetTuple(citem.Key);

				if (sitem == null)
					continue;

				var itemType = sitem.GetValue<TypeType>(ServerItemAttributes.Type);

				if (itemType == TypeType.Weapon || itemType == TypeType.Armor) {
					if (itemType == TypeType.Armor && !ItemParser.IsArmorType(sitem)) {
						if (itemType == TypeType.Armor)
							itemType = TypeType.Weapon;
						else
							itemType = TypeType.Armor;
					}
				}

				var parameters = citem.GetValue<ParameterHolder>(ClientItemAttributes.Parameters);

				if (parameters != null) {
					foreach (var param in ParameterHolder.KnownItemParameters) {
						try {
							if (parameters.Values.ContainsKey(param)) {
								var value = parameters.Values[param];

								if (param == ParameterHolderKeys.Class && SdeAppConfiguration.VaCiClass) {
									var sViewId = sitem.GetIntNoThrow(ServerItemAttributes.ClassNumber);
									var location = sitem.GetIntNoThrow(ServerItemAttributes.Location);

									int cViewId;

									switch(itemType) {
										case TypeType.Weapon:
											var twoHanded = (location & 34) == 34;
											var isWeaponLocation = (location & 2) != 0;

											if (!isWeaponLocation) {
												errors.Add(new CiError(ValidationErrors.CiItemClassUnknown, citem.Key, "ItemLocation: server Location requires the 'Weapon' flag.", ServerDbs.Items, this));
											}

											ItemGeneratorEngine.WeaponTypeToViewId.TryGetValue(value, out cViewId);

											if (sViewId < 0) {
												errors.Add(new CiError(ValidationErrors.CiItemClassUnknown, citem.Key, "ItemDescription: server View ID cannot be below 0.", ServerDbs.Items, this));
												continue;
											}

											if (sViewId >= ItemGeneratorEngine.WeaponTypes.Count) {
												errors.Add(new CiError(ValidationErrors.CiItemClassUnknown, citem.Key, "ItemDescription: unknown server View ID, custom weapon?", ServerDbs.Items, this));
												continue;
											}

											if (cViewId == 0) {
												errors.Add(new CiError(ValidationErrors.CiItemClassUnknown, citem.Key, "ItemDescription: found class '" + value + "', expected '" + ItemGeneratorEngine.WeaponTypes[sViewId] + "'", ServerDbs.CItems, this));
											}
											else if (cViewId != sViewId) {
												if (twoHanded) {
													if (ItemGeneratorEngine.TwoHandedWeapons.Contains(sViewId)) {
														errors.Add(new CiError(ValidationErrors.CiItemClassUnknown, citem.Key, "ItemDescription: found class '" + value + "', expected '" + ItemGeneratorEngine.WeaponTypes[sViewId] + "'", ServerDbs.CItems, this));
													}
													else {
														errors.Add(new CiError(ValidationErrors.CiItemClassUnknown, citem.Key, "ItemDescription: found class '" + value + "', expected '" + ItemGeneratorEngine.WeaponTypes[sViewId] + "'. The server View ID doesn't belong to a two-handed weapon class.", ServerDbs.CItems, this));
													}
												}
												else {
													if (!ItemGeneratorEngine.TwoHandedWeapons.Contains(sViewId)) {
														errors.Add(new CiError(ValidationErrors.CiItemClassUnknown, citem.Key, "ItemDescription: found class '" + value + "', expected '" + ItemGeneratorEngine.WeaponTypes[sViewId] + "'", ServerDbs.CItems, this));
													}
													else {
														errors.Add(new CiError(ValidationErrors.CiItemClassUnknown, citem.Key, "ItemDescription: found class '" + value + "', expected '" + ItemGeneratorEngine.WeaponTypes[sViewId] + "'. The server View ID belongs to a two-handed weapon class.", ServerDbs.CItems, this));
													}
												}
											}

											break;
										default:

											break;
									}
								}
								else if (param == ParameterHolderKeys.Attack && SdeAppConfiguration.VaCiAttack) {
									int ival;
									if (Int32.TryParse(value, out ival)) {
										var sval = sitem.GetIntNoThrow(ServerItemAttributes.Attack);
										var name = sitem.GetValue<string>(ServerItemAttributes.Name);

										if (name.EndsWith(" Box"))
											continue;

										if (ival != sval) {
											errors.Add(new CiError(ValidationErrors.CiAttack, citem.Key, "Attack: found '" + value + "', expected '" + sval + "'.", ServerDbs.CItems, this));
										}
									}
									else {
										errors.Add(new CiError(ValidationErrors.CiParseError, citem.Key, "Parse: failed to parse Attack field, found '" + value + "'.", ServerDbs.CItems, this));
									}
								}
								else if (param == ParameterHolderKeys.Defense && SdeAppConfiguration.VaCiDefense) {
									int ival;
									if (Int32.TryParse(value, out ival)) {
										var sval = sitem.GetIntNoThrow(ServerItemAttributes.Defense);
										if (ival != sval) {
											errors.Add(new CiError(ValidationErrors.CiDefense, citem.Key, "Defense: found '" + value + "', expected '" + sval + "'.", ServerDbs.CItems, this));
										}
									}
									else {
										errors.Add(new CiError(ValidationErrors.CiParseError, citem.Key, "Parse: failed to parse Defense field, found '" + value + "'.", ServerDbs.CItems, this));
									}
								}
								else if (param == ParameterHolderKeys.Property && SdeAppConfiguration.VaCiProperty) {
									var script = sitem.GetValue<string>(ServerItemAttributes.Script);
									const string Bonus1 = "bonus bAtkEle";
									const string Bonus2 = "bonus bDefEle";

									if (script.Contains(Bonus1) || script.Contains(Bonus2)) {
										var bonusScript = script.Contains(Bonus1) ? Bonus1 : Bonus2;

										int start = script.IndexOf(bonusScript, StringComparison.Ordinal) + bonusScript.Length;
										int end = script.IndexOf(";", start, StringComparison.Ordinal);

										if (end < 0)
											end = script.Length;

										var bonus = script.Substring(start, end - start).Trim(',', ' ', ';');

										if (bonus.Length > 4) {
											var element = bonus.Substring(4);

											if (element != value) {
												if (value == "Shadow" && element == "Dark") {
												}
												else {
													errors.Add(new CiError(ValidationErrors.CiProperty, citem.Key, "Property: found '" + value + "', expected '" + element + "'.", ServerDbs.CItems, this));
												}
											}
										}
									}
									else {
										if (value != "Neutral") {
											errors.Add(new CiError(ValidationErrors.CiProperty, citem.Key, "Property: found '" + value + "', expected no element or Neutral.", ServerDbs.CItems, this));
										}
									}
								}
								else if (param == ParameterHolderKeys.RequiredLevel && SdeAppConfiguration.VaCiRequiredLevel) {
									int ival;
									if (Int32.TryParse(value, out ival)) {
										var sval = sitem.GetIntNoThrow(ServerItemAttributes.EquipLevel);
										if (ival != sval) {
											errors.Add(new CiError(ValidationErrors.CiEquipLevel, citem.Key, "EquipLevel: found '" + value + "', expected '" + sval + "'.", ServerDbs.CItems, this));
										}
									}
									else {
										errors.Add(new CiError(ValidationErrors.CiParseError, citem.Key, "Parse: failed to parse EquipLevel field, found '" + value + "'.", ServerDbs.CItems, this));
									}
								}
								else if (param == ParameterHolderKeys.WeaponLevel && SdeAppConfiguration.VaCiWeaponLevel) {
									int ival;

									if (Int32.TryParse(value, out ival)) {
										var name = sitem.GetValue<string>(ServerItemAttributes.Name);
										var sval = sitem.GetIntNoThrow(ServerItemAttributes.WeaponLevel);

										if (name.EndsWith(" Box"))
											continue;

										if (ival != sval) {
											errors.Add(new CiError(ValidationErrors.CiWeaponLevel, citem.Key, "WeaponLevel: found '" + value + "', expected '" + sval + "'.", ServerDbs.CItems, this));
										}
									}
									else {
										errors.Add(new CiError(ValidationErrors.CiParseError, citem.Key, "Parse: failed to parse WeaponLevel field, found '" + value + "'.", ServerDbs.CItems, this));
									}
								}
								else if (param == ParameterHolderKeys.Weight && SdeAppConfiguration.VaCiWeight) {
									int ival = (int)(FormatConverters.SingleConverter(value) * 10);

									var sval = sitem.GetIntNoThrow(ServerItemAttributes.Weight);
									if (ival != sval) {
										errors.Add(new CiError(ValidationErrors.CiWeight, citem.Key, "Weight: found '" + ival + "', expected '" + sval + "'.", ServerDbs.CItems, this));
									}
								}
								else if ((param == ParameterHolderKeys.Location || param == ParameterHolderKeys.EquippedOn) && SdeAppConfiguration.VaCiLocation) {
									var name = sitem.GetValue<string>(ServerItemAttributes.Name);

									// Do not scan rental items
									if (name.EndsWith(" Box"))
										continue;

									string[] items = value.Split(',', '/', '-', '&').Select(p => p.Trim(' ')).ToArray();
									int ival = 0;

									foreach (string item in items) {
										if (item.ToLower() == "lower")
											ival |= 1;
										if (item.ToLower() == "mid" || item.ToLower() == "middle")
											ival |= 512;
										if (item.ToLower() == "upper")
											ival |= 256;
										if (item.ToLower() == "all slot")
											ival |= 1023;
									}

									var sval = sitem.GetIntNoThrow(ServerItemAttributes.Location);

									if ((sval & 7168) != 0) {
										// It's a costume ;x!
										ival = 0;

										foreach (string item in items) {
											if (item.ToLower() == "lower")
												ival |= 4096;
											if (item.ToLower() == "mid" || item.ToLower() == "middle")
												ival |= 2048;
											if (item.ToLower() == "upper")
												ival |= 1024;
										}
									}

									if (ival != sval) {
										errors.Add(new CiError(ValidationErrors.CiLocation, citem.Key, "Location: found '" + ival + "', expected '" + sval + "'.", ServerDbs.CItems, this));
									}
								}
								else if (param == ParameterHolderKeys.CompoundOn && SdeAppConfiguration.VaCiCompoundOn) {
									var valueLower = value.ToLower();
									int location = 0;

									switch(valueLower) {
										case "weapon":
											location = 2;
											break;
										case "headgear":
											location = 769;
											break;
										case "armor":
											location = 16;
											break;
										case "shield":
											location = 32;
											break;
										case "garment":
											location = 4;
											break;
										case "accessory":
											location = 136;
											break;
										case "shoes":
										case "footwear":
										case "foot gear":
										case "footgear":
											location = 64;
											break;
										default:
											errors.Add(new CiError(ValidationErrors.CiParseError, citem.Key, "CompoundOn: found '" + value + "'.", ServerDbs.CItems, this));
											break;
									}

									var sval = sitem.GetIntNoThrow(ServerItemAttributes.Location);

									if ((location & sval) != sval) {
										errors.Add(new CiError(ValidationErrors.CiCompoundOn, citem.Key, "CompoundOn: found '" + location + "', expected '" + sval + "'.", ServerDbs.CItems, this));
									}
								}
								else if (param == ParameterHolderKeys.ApplicableJob && SdeAppConfiguration.VaCiJob) {
									//var jobs = JobList.GetApplicableJobs(value);
									//var jobHex = JobList.GetHexJob(jobs);
									//
									//var sJobHex = "0x" + sitem.GetValue<string>(ServerItemAttributes.ApplicableJob);
									//jobs = JobList.GetApplicableJobsFromHex(sJobHex);
									//sJobHex = JobList.GetHexJob(jobs);
									//
									//if (jobHex != sJobHex) {
									//	errors.Add(new CiError(ValidationErrors.CiCompoundOn, citem.Key, "ApplicableJob: found '" + jobHex + "', expected '" + sJobHex + "'.", ServerDbs.CItems, this));
									//}
								}
							}
						}
						catch {
							errors.Add(new CiError(ValidationErrors.Generic, citem.Key, "Failed to analyse property '" + param + "'.", ServerDbs.Items, this));
						}
					}
				}
				// End of parameters

				if (itemType == TypeType.Weapon && SdeAppConfiguration.VaCiItemRange) {
					var sVal = sitem.GetIntNoThrow(ServerItemAttributes.ClassNumber);

					if (sVal < 24 && sVal > 0) {
						var range = _ranges[sVal];
						var id = sitem.Key;

						bool found = false;

						for (int i = 0; i < range.Length; i += 2) {
							if (range[i] < 0) {
								errors.Add(new CiError(ValidationErrors.CiItemRange, citem.Key, "ItemRange: found weapon class '" + sVal + "', which does not have any ID range.", ServerDbs.Items, this));
								found = true;
								break;
							}

							if (range[i] <= id && id <= range[i + 1]) {
								found = true;
								break;
							}
						}

						if (!found) {
							string idRange = "";

							for (int i = 0; i < range.Length; i += 2) {
								if (i > 0)
									idRange += ", ";

								idRange += range[i] + "-" + range[i + 1];
							}

							errors.Add(new CiError(ValidationErrors.CiItemRange, citem.Key, "ItemRange: found weapon class '" + sVal + "', which has an ID range of [" + idRange + "].", ServerDbs.Items, this));
						}
					}
				}

				if (SdeAppConfiguration.VaCiNumberOfSlots) {
					var sVal = sitem.GetIntNoThrow(ServerItemAttributes.NumberOfSlots);
					var cVal = citem.GetIntNoThrow(ClientItemAttributes.NumberOfSlots);

					if (sVal != cVal) {
						errors.Add(new CiError(ValidationErrors.CiNumOfSlots, citem.Key, "NumberOfSlots: found '" + cVal + "', expected '" + sVal + "'.", ServerDbs.CItems, this));
					}

					if (!ItemParser.IsArmorType(sitem) && !ItemParser.IsWeaponType(sitem)) {
						if (sVal > 0)
							errors.Add(new CiError(ValidationErrors.CiNumOfSlots, citem.Key, "NumberOfSlots: found '" + sVal + "', but the server item type is neither an armor or a weapon..", ServerDbs.Items, this));
					}
				}

				if (SdeAppConfiguration.VaCiViewId) {
					var sVal = sitem.GetIntNoThrow(ServerItemAttributes.ClassNumber);
					var cVal = citem.GetIntNoThrow(ClientItemAttributes.ClassNumber);

					if (sVal != cVal) {
						var nVal = cVal;

						if (cVal > 24) {
							if (viewIdToWepClass.ContainsKey(cVal)) {
								nVal = viewIdToWepClass[cVal];
							}
						}

						if (sVal != nVal) {
							if (nVal != cVal) {
// && nVal + 1 == sVal) {
							}
							else {
								bool showError = true;

								if (cVal == 0) {
									if ((citem.Key >= 18000 && citem.Key <= 18099) ||
									    (citem.Key >= 13260 && citem.Key <= 13290)) {
										showError = false;
									}
								}

								if (showError)
									errors.Add(new CiError(ValidationErrors.CiViewId, citem.Key, "ClassNumber: found '" + cVal + "', class '" + nVal + "', expected '" + sVal + "'.", ServerDbs.CItems, this));
							}
						}
					}
				}

				if (SdeAppConfiguration.VaCiIsCard) {
					var sVal = itemType;
					var cVal = citem.GetValue<bool>(ClientItemAttributes.IsCard);

					if ((sVal == TypeType.Card || cVal) && (sVal != TypeType.Card || !cVal)) {
						errors.Add(new CiError(ValidationErrors.CiCardType, citem.Key, "TypeMismatch: client item IsCard '" + cVal + "', server type '" + sVal + "'.", ServerDbs.CItems, this));
					}
				}

				if (SdeAppConfiguration.VaCiName) {
					var sname = sitem.GetValue<string>(ServerItemAttributes.Name);
					var cname = citem.GetValue<string>(ClientItemAttributes.IdentifiedDisplayName);

					if (sname != cname) {
						int distance = Methods.LevenshteinDistance(sname, cname);

						if (distance > 5) {
							errors.Add(new CiError(ValidationErrors.CiName, citem.Key, "NameMismatch: client name is '" + cname + "', server name is '" + sname + "', diff = " + distance + ".", ServerDbs.CItems, this));
						}
					}
				}
			}
		}

		private readonly Dictionary<int, int[]> _ranges = new Dictionary<int, int[]> {
			{ 1, new[] { 1200, 1249, 13000, 13099 } },
			{ 2, new[] { 1100, 1115, 1119, 1149, 13400, 13499 } },
			{ 3, new[] { 1116, 1118, 1150, 1199, 21000, 21999 } },
			{ 4, new[] { 1400, 1409, 1413, 1449 } },
			{ 5, new[] { 1410, 1412, 1450, 1471, 1474, 1499 } },
			{ 6, new[] { 1300, 1313, 1316, 1349 } },
			{ 7, new[] { 1314, 1315, 1350, 1399 } },
			{ 8, new[] { 1500, 1549, 1599, 1599, 16000, 16999 } },
			{ 9, new[] { -1 } },
			{ 10, new[] { 1600, 1699 } },
			{ 11, new[] { 1700, 1749, 18100, 18499 } },
			{ 12, new[] { 1800, 1899 } },
			{ 13, new[] { 1900, 1949 } },
			{ 14, new[] { 1950, 1999 } },
			{ 15, new[] { 1550, 1599 } },
			{ 16, new[] { 1250, 1299 } },
			{ 17, new[] { 13100, 13149 } },
			{ 18, new[] { 13150, 13199 } },
			{ 19, new[] { 13150, 13199 } },
			{ 20, new[] { 13150, 13199 } },
			{ 21, new[] { 13150, 13199 } },
			{ 22, new[] { 13300, 13399 } },
			{ 23, new[] { 1472, 1473, 2000, 2099 } },
		};

		private Dictionary<int, int> _getWeaponClasses() {
			Dictionary<int, int> table = new Dictionary<int, int>();
			var accIdPath = ProjectConfiguration.SyncAccId;

			for (int i = 0; i <= 30; i++) {
				table[i] = i;
			}

			if (_database.MetaGrf.GetData(ProjectConfiguration.SyncAccId) == null || _database.MetaGrf.GetData(ProjectConfiguration.SyncAccName) == null) {
				return table;
			}

			var weaponPath = GrfPath.Combine(GrfPath.GetDirectoryName(accIdPath), "weapontable" + Path.GetExtension(accIdPath));
			var weaponData = _database.MetaGrf.GetData(weaponPath);

			if (weaponData == null) {
				return table;
			}

			var weaponTable = new LuaParser(weaponData, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(weaponData), EncodingService.DisplayEncoding);
			var weaponIds = LuaHelper.GetLuaTable(weaponTable, "Weapon_IDs");
			var weaponExpansionNameTable = LuaHelper.GetLuaTable(weaponTable, "Expansion_Weapon_IDs");

			var ids = LuaHelper.SetIds(weaponIds, "Weapon_IDs");

			foreach (var id in ids) {
				if (id.Value == 24)
					continue;

				if (id.Value <= 30) {
					table[id.Value] = id.Value;
				}
				else {
					string sval;
					if (weaponExpansionNameTable.TryGetValue("[" + id.Key + "]", out sval)) {
						int ival;
						if (ids.TryGetValue(sval, out ival)) {
							table[id.Value] = ival;
						}
					}
				}
			}

			return table;
		}
	}
}