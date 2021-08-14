using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Database;
using Database.Commands;
using SDE.Editor.Engines;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Jobs;
using Utilities;
using Utilities.Services;

namespace SDE.Editor.Items {
	public class ItemGeneratorEngine {
		private bool _emptyFill(DbAttribute attribute, Database.Tuple item) {
			string value = item.GetValue<string>(attribute);

			if (ProjectConfiguration.AutocompleteFillOnlyEmptyFields) {
				return String.IsNullOrEmpty(value);
			}

			return true;
		}

		public GroupCommand<TKey, ReadableTuple<TKey>> Generate<TKey>(ReadableTuple<TKey> item, ReadableTuple<int> tupleSource, AbstractDb<int> mobDb1, AbstractDb<int> mobDb2, AbstractDb<int> pet1, AbstractDb<int> pet2) {
			var description = item.GetValue<ParameterHolder>(ClientItemAttributes.Parameters).Values[ParameterHolderKeys.Description] ?? "";
			description = ParameterHolder.ClearDescription(description);

			ParameterHolder holder = new ParameterHolder();
			GroupCommand<TKey, ReadableTuple<TKey>> commands = GroupCommand<TKey, ReadableTuple<TKey>>.Make();

			int numSlotC = _getInt(ClientItemAttributes.NumberOfSlots, item);
			int numSlotS = _getInt(ServerItemAttributes.NumberOfSlots, tupleSource);

			if (ProjectConfiguration.AutocompleteViewId) {
				int viewIdC = _getInt(ClientItemAttributes.ClassNumber, item);
				int viewIdS = _getInt(ServerItemAttributes.ClassNumber, tupleSource);

				if (viewIdC != viewIdS) {
					commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ClientItemAttributes.ClassNumber, viewIdS));
				}
			}

			if (ProjectConfiguration.AutocompleteNumberOfSlot) {
				if (numSlotC != numSlotS) {
					commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ClientItemAttributes.NumberOfSlots, numSlotS));
				}
			}

			if ((TypeType)tupleSource.GetValue<int>(ServerItemAttributes.Type) != TypeType.Card) {
				if (item.GetValue<bool>(ClientItemAttributes.IsCard))
					commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ClientItemAttributes.IsCard, false));
			}

			string idDisplayName = tupleSource.GetValue<string>(ServerItemAttributes.Name);
			if (_emptyFill(ClientItemAttributes.IdentifiedDisplayName, item) && ProjectConfiguration.AutocompleteIdDisplayName && _isNotNullDifferent(idDisplayName, ClientItemAttributes.IdentifiedDisplayName, item)) {
				commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ClientItemAttributes.IdentifiedDisplayName, idDisplayName));
			}

			DbAttribute attribute;
			TypeType itemType = (TypeType)tupleSource.GetValue<int>(ServerItemAttributes.Type);

			if (itemType == TypeType.Weapon || itemType == TypeType.Armor) {
				if (itemType == TypeType.Armor && !ItemParser.IsArmorType(tupleSource)) {
					if (itemType == TypeType.Armor)
						itemType = TypeType.Weapon;
					else
						itemType = TypeType.Armor;
				}
			}

			// Weight:
			//holder = item.GetValue<ParameterHolder>(ClientItemAttributes.Parameters);
			//
			//switch (itemType) {
			//	case TypeType.Weapon:
			//	case TypeType.Ammo:
			//	case TypeType.Armor:
			//	case TypeType.Card:
			//	case TypeType.PetEgg:
			//	case TypeType.PetEquip:
			//	case TypeType.UsableItem:
			//	case TypeType.EtcItem:
			//	case TypeType.HealingItem:
			//	case TypeType.ShadowEquip:
			//	case TypeType.UsableWithDelayed:
			//	case TypeType.UsableWithDelayed2:
			//		_autoAddWeight(tupleSource, holder);
			//		break;
			//}

			DbAttribute equipLevelAttribute = ServerItemAttributes.EquipLevel;

			if (tupleSource.GetIntNoThrow(ServerItemAttributes.EquipLevelMin) > tupleSource.GetIntNoThrow(ServerItemAttributes.EquipLevel)) {
				equipLevelAttribute = ServerItemAttributes.EquipLevelMin;
			}

			switch(itemType) {
				case TypeType.Weapon:
					string type = _findWeaponType(tupleSource) ?? "Weapon";
					holder.Values[ParameterHolderKeys.Class] = type;

					string unidentifiedResourceName = EncodingService.FromAnyToDisplayEncoding(_findWeaponUnidentifiedResource(tupleSource) ?? "");
					attribute = ClientItemAttributes.UnidentifiedResourceName;
					if (_emptyFill(attribute, item) && ProjectConfiguration.AutocompleteUnResourceName && _isNotNullDifferent(unidentifiedResourceName, attribute, item)) {
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, attribute, unidentifiedResourceName));
					}

					string identifiedResourceName = item.GetStringValue(ClientItemAttributes.IdentifiedResourceName.Index);
					attribute = ClientItemAttributes.IdentifiedResourceName;
					if (String.IsNullOrEmpty(identifiedResourceName) && ProjectConfiguration.AutocompleteIdResourceName && _isNotNullDifferent(unidentifiedResourceName, attribute, item)) {
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, attribute, unidentifiedResourceName));
					}

					attribute = ClientItemAttributes.UnidentifiedDisplayName;
					if (_emptyFill(attribute, item) && ProjectConfiguration.AutocompleteUnDisplayName && _isNotNullDifferent(type, attribute, item)) {
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, attribute, type));
					}

					if (!tupleSource.GetValue<bool>(ServerItemAttributes.Refineable)) {
						if (!description.Contains("Impossible to refine") &&
						    !description.Contains("Cannot be upgraded") &&
						    !description.ToLower().Contains("rental item")) {
							description += "\r\nImpossible to refine this item.";
						}
					}

					if (tupleSource.GetValue<bool>(ServerItemAttributes.Refineable)) {
						if (description.Contains("Impossible to refine")) {
							description = description.Replace("Impossible to refine this item.", "").Trim('\r', '\n');
						}
					}

					_autoAdd(ServerItemAttributes.Attack, ParameterHolderKeys.Attack, tupleSource, holder);
					_autoAddWeight(tupleSource, holder);
					_autoAdd(ServerItemAttributes.WeaponLevel, ParameterHolderKeys.WeaponLevel, tupleSource, holder);
					_autoAddJob(tupleSource, holder, _getInt(equipLevelAttribute, tupleSource));
					_autoAddElement(tupleSource, holder);
					break;
				case TypeType.Ammo:
					type = _findAmmoType(tupleSource.GetStringValue(ServerItemAttributes.ApplicableJob.Index)) ?? "Ammunition";
					holder.Values[ParameterHolderKeys.Class] = type;

					_autoAdd(ServerItemAttributes.Attack, ParameterHolderKeys.Attack, tupleSource, holder, -1);
					_autoAddWeight(tupleSource, holder);
					_autoAddElement(tupleSource, holder);
					break;
				case TypeType.Armor:
					int location = _getInt(ServerItemAttributes.Location, tupleSource);
					type = _findArmorType(location) ?? "Armor";
					holder.Values[ParameterHolderKeys.Class] = type;

					unidentifiedResourceName = EncodingService.FromAnyToDisplayEncoding(_findArmorUnidentifiedResource(tupleSource, item) ?? "");
					attribute = ClientItemAttributes.UnidentifiedResourceName;
					if (_emptyFill(attribute, item) && ProjectConfiguration.AutocompleteUnResourceName && _isNotNullDifferent(unidentifiedResourceName, attribute, item)) {
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, attribute, unidentifiedResourceName));
					}
					else {
						unidentifiedResourceName = item.GetValue<string>(ClientItemAttributes.UnidentifiedResourceName);
					}

					identifiedResourceName = item.GetStringValue(ClientItemAttributes.IdentifiedResourceName.Index);
					attribute = ClientItemAttributes.IdentifiedResourceName;
					if (String.IsNullOrEmpty(identifiedResourceName) && ProjectConfiguration.AutocompleteIdResourceName && _isNotNullDifferent(unidentifiedResourceName, attribute, item)) {
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, attribute, unidentifiedResourceName));
					}

					string unDisplayName = _findArmorUnidentifiedDisplayName(unidentifiedResourceName);
					attribute = ClientItemAttributes.UnidentifiedDisplayName;
					if (_emptyFill(attribute, item) && ProjectConfiguration.AutocompleteUnDisplayName && _isNotNullDifferent(unDisplayName, attribute, item)) {
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, attribute, unDisplayName));
					}

					if ((_getInt(ServerItemAttributes.Location, tupleSource) & 374) != 0) {
						if (!tupleSource.GetValue<bool>(ServerItemAttributes.Refineable)) {
							if (!description.Contains("Impossible to refine")) {
								description += "\r\nImpossible to refine this item.";
							}
						}

						if (tupleSource.GetValue<bool>(ServerItemAttributes.Refineable)) {
							if (description.Contains("Impossible to refine")) {
								description = description.Replace("Impossible to refine this item.", "").Trim('\r', '\n');
							}
						}
					}

					_autoAdd(ServerItemAttributes.Defense, ParameterHolderKeys.Defense, tupleSource, holder);
					_autoAddEquippedOn(ServerItemAttributes.Location, ParameterHolderKeys.Location, tupleSource, holder);
					_autoAddWeight(tupleSource, holder);
					_autoAddJob(tupleSource, holder, _getInt(equipLevelAttribute, tupleSource));
					break;
				case TypeType.Card:
					holder.Values[ParameterHolderKeys.Class] = "Card";
					_autoAddCompound(ServerItemAttributes.Location, ParameterHolderKeys.CompoundOn, tupleSource, holder);
					_autoAdd(equipLevelAttribute, ParameterHolderKeys.RequiredLevel, tupleSource, holder, 1);
					_autoAddWeight(tupleSource, holder);

					if (!item.GetValue<bool>(ClientItemAttributes.IsCard))
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ClientItemAttributes.IsCard, true));

					if (String.IsNullOrEmpty(item.GetValue<string>(ClientItemAttributes.Illustration)))
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ClientItemAttributes.Illustration, "sorry"));

					if (String.IsNullOrEmpty(item.GetValue<string>(ClientItemAttributes.Affix)))
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ClientItemAttributes.Affix, tupleSource.GetValue<string>(ServerItemAttributes.Name)));

					const string CardResource = "ÀÌ¸§¾ø´ÂÄ«µå";

					unDisplayName = tupleSource.GetValue<string>(ServerItemAttributes.Name);
					attribute = ClientItemAttributes.UnidentifiedDisplayName;
					if (_emptyFill(attribute, item) && ProjectConfiguration.AutocompleteUnDisplayName && _isNotNullDifferent(unDisplayName, attribute, item)) {
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, attribute, unDisplayName));
					}

					unidentifiedResourceName = EncodingService.FromAnyToDisplayEncoding(CardResource);
					attribute = ClientItemAttributes.UnidentifiedResourceName;
					if (_emptyFill(attribute, item) && ProjectConfiguration.AutocompleteUnResourceName && _isNotNullDifferent(unidentifiedResourceName, attribute, item)) {
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, attribute, unidentifiedResourceName));
					}

					attribute = ClientItemAttributes.IdentifiedResourceName;
					if (_emptyFill(attribute, item) && ProjectConfiguration.AutocompleteIdResourceName && _isNotNullDifferent(unidentifiedResourceName, attribute, item)) {
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, attribute, unidentifiedResourceName));
					}
					break;
				case TypeType.PetEgg:
					holder.Values[ParameterHolderKeys.Class] = "Monster Egg";
					_autoAddWeight(tupleSource, holder);
					break;
				case TypeType.PetEquip:
					holder.Values[ParameterHolderKeys.Class] = "Cute Pet Armor";
					_autoAddWeight(tupleSource, holder);

					int id = item.GetKey<int>();

					List<ReadableTuple<int>> tuples = pet1.Table.Tuples.Where(p => p.Value.GetValue<int>(ServerPetAttributes.EquipId) == id).Select(p => p.Value).Concat(
						pet2.Table.Tuples.Where(p => p.Value.GetValue<int>(ServerPetAttributes.EquipId) == id).Select(p => p.Value)
						).ToList();

					if (tuples.Count > 0) {
						// Try to retrieve the names
						List<string> values = new List<string>();

						foreach (ReadableTuple<int> tuple in tuples) {
							var pid = tuple.GetKey<int>();

							var pTuple = mobDb2.Table.TryGetTuple(pid) ?? mobDb1.Table.TryGetTuple(pid);

							if (pTuple != null) {
								values.Add(pTuple.GetValue<string>(ServerMobAttributes.KRoName));
							}
						}

						if (values.Count > 0)
							holder.Values[ParameterHolderKeys.ApplicablePet] = String.Join(", ", values.ToArray());
					}
					break;
				case TypeType.UsableItem:
					_autoAddPet(tupleSource, holder);
					_autoAddWeight(tupleSource, holder);
					_autoAddJobIfRestricted(tupleSource, holder);
					break;
				case TypeType.EtcItem:
				case TypeType.HealingItem:
				case TypeType.ShadowEquip:
				case TypeType.UsableWithDelayed:
				case TypeType.UsableWithDelayed2:
					_autoAddWeight(tupleSource, holder);
					_autoAddJobIfRestricted(tupleSource, holder);
					break;
			}

			_autoAdd(equipLevelAttribute, ParameterHolderKeys.RequiredLevel, tupleSource, holder, 1);

			holder.Values[ParameterHolderKeys.Description] = description == "" ? ProjectConfiguration.AutocompleteDescNotSet : description;

			var idDescription = holder.GenerateDescription();

			if (ProjectConfiguration.AutocompleteIdDescription) {
				if (idDescription != item.GetValue<string>(ClientItemAttributes.IdentifiedDescription))
					commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ClientItemAttributes.IdentifiedDescription, idDescription));
			}

			var unDescription = item.GetValue<string>(ClientItemAttributes.UnidentifiedDescription);

			// unidentified
			switch(tupleSource.GetValue<TypeType>(ServerItemAttributes.Type)) {
				case TypeType.Ammo:
				case TypeType.EtcItem:
				case TypeType.HealingItem:
				case TypeType.PetEgg:
				case TypeType.UsableItem:
				case TypeType.UsableWithDelayed:
				case TypeType.UsableWithDelayed2:
					if (ProjectConfiguration.AutocompleteUnDescription && unDescription != idDescription)
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ClientItemAttributes.UnidentifiedDescription, idDescription));

					string unDisplayName = tupleSource.GetValue<string>(ServerItemAttributes.Name);
					attribute = ClientItemAttributes.UnidentifiedDisplayName;
					if (_emptyFill(attribute, item) && ProjectConfiguration.AutocompleteUnDisplayName && _isNotNullDifferent(unDisplayName, attribute, item)) {
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, attribute, unDisplayName));
					}

					string unidentifiedResourceName = item.GetValue<string>(ClientItemAttributes.IdentifiedResourceName);
					if (String.IsNullOrEmpty(unidentifiedResourceName)) {
						unidentifiedResourceName = EncodingService.FromAnyToDisplayEncoding("Á¶°¢ÄÉÀÌÅ©"); // Cake
					}

					attribute = ClientItemAttributes.UnidentifiedResourceName;
					if (_emptyFill(attribute, item) && ProjectConfiguration.AutocompleteUnResourceName && _isNotNullDifferent(unidentifiedResourceName, attribute, item)) {
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, attribute, unidentifiedResourceName));
					}

					attribute = ClientItemAttributes.IdentifiedResourceName;
					if (_emptyFill(attribute, item) && ProjectConfiguration.AutocompleteIdResourceName && _isNotNullDifferent(unidentifiedResourceName, attribute, item)) {
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, attribute, unidentifiedResourceName));
					}
					break;
				case TypeType.Card:
					if (ProjectConfiguration.AutocompleteUnDescription && unDescription != idDescription)
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ClientItemAttributes.UnidentifiedDescription, idDescription));
					break;
				default:
					if (ProjectConfiguration.AutocompleteUnDescription && unDescription != ProjectConfiguration.AutocompleteUnDescriptionFormat)
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ClientItemAttributes.UnidentifiedDescription, ProjectConfiguration.AutocompleteUnDescriptionFormat));
					break;
			}

			if (commands.Commands.Count == 0)
				return null;

			return commands;
		}

		private readonly TkDictionary<string, string> _armorUnDisplayName = new TkDictionary<string, string> {
			{ "¸Ó¸®¶ì", "Hairband" },
			{ "Ä¸", "Hat" },
			{ "ÇÞ", "Hat" },
			{ "Çï¸§", "Helm" },
			{ "¸®º»", "Ribbon" },
			{ "½º¸¶ÀÏ", "Mask" },
			{ "±Û·¡½º", "Glasses" },
			{ "¿ìµç¸ÞÀÏ", "Armor" },
			{ "ÈÄµå", "Garment" },
			{ "ÀÌ¾î¸µ", "Earring" },
			{ "¸µ", "Accessory" },
			{ "±Û·¯ºê", "Glove" },
			{ "³×Å¬¸®½º", "Necklace" },
			{ "ÄÚÆ°¼ÅÃ÷", "Clothing" },
			{ "»÷µé", "Shoes" },
			{ "±×¸®ºê", "Greaves" },
			{ "ºÎÃ÷", "Boots" },
			{ "¸ÓÇÃ·¯", "Muffler" }
		};

		private string _findArmorUnidentifiedDisplayName(string value) {
			string ansi = EncodingService.GetAnsiString(value);
			return _armorUnDisplayName[ansi];
		}

		private bool _isNotNullDifferent(string newValue, DbAttribute attribute, Database.Tuple item) {
			if (string.IsNullOrEmpty(newValue)) return false;
			return newValue != item.GetValue<string>(attribute);
		}

		private bool _overridableString(Database.Tuple tupleSource, DbAttribute attribute, params string[] strings) {
			string value = tupleSource.GetValue<string>(attribute);
			return strings.Any(s => value.IndexOf(s, StringComparison.OrdinalIgnoreCase) > -1);
		}

		private string _findArmorUnidentifiedResource<TKey>(ReadableTuple<int> tupleSource, ReadableTuple<TKey> item) {
			List<Job> jobs = JobList.GetApplicableJobsFromHex(tupleSource.GetValue<string>(ServerItemAttributes.ApplicableJob));
			int location = _getInt(ServerItemAttributes.Location, tupleSource);

			if (_is(location, 32)) {
				return "°¡µå";
			}

			if (_is(location, 1, 256, 512, 1024, 2048, 4096)) {
				if (_overridableString(item, ClientItemAttributes.UnidentifiedResourceName,
					"¸Ó¸®¶ì", "±Û·¡½º", "Çï¸§", "Ä¸", "½º¸¶ÀÏ")) return null;

				if (location == 513)
					return "½º¸¶ÀÏ";

				if (location == 512)
					return "±Û·¡½º";

				if (_is(location, 256)) {
					if (jobs.All(p => JobList.GetFirstClass(p) == JobList.Swordman))
						return "Çï¸§";
				}

				return "Ä¸";
			}

			if (_is(location, 16)) {
				if (_overridableString(tupleSource, ServerItemAttributes.Name, "Armor"))
					return "¿ìµç¸ÞÀÏ";

				if (_overridableString(tupleSource, ServerItemAttributes.Name, "Clothe", "Robe", " Suit", "Coat"))
					return "ÄÚÆ°¼ÅÃ÷";

				if (jobs.Contains(JobList.Novice) || jobs.Any(p => JobList.GetFirstClass(p) == JobList.Acolyte))
					return "ÄÚÆ°¼ÅÃ÷";

				int weight = _getInt(ServerItemAttributes.Weight, tupleSource);

				if (weight >= 1000)
					return "¿ìµç¸ÞÀÏ";

				return "ÄÚÆ°¼ÅÃ÷";
			}

			if (_is(location, 64))
				return "»÷µé";

			if (_is(location, 4))
				return "ÈÄµå";

			if (_is(location, 8, 128))
				return "¸µ";
			return null;
		}

		private void _autoAddPet(ReadableTuple<int> tupleSource, ParameterHolder holder) {
			var script = tupleSource.GetValue<string>(ServerItemAttributes.Script);
			const string Bonus = "pet ";

			if (script.Contains(Bonus)) {
				int start = script.IndexOf(Bonus, StringComparison.Ordinal) + Bonus.Length;
				int end = script.IndexOf(";", start, StringComparison.Ordinal);

				if (end < 0)
					end = script.Length;

				var bonus = script.Substring(start, end - start).Trim(',', ' ', ';');

				if (bonus.Length > 0)
					holder.Values[ParameterHolderKeys.Class] = "Taming Item";
			}
		}

		private void _autoAddEquippedOn(DbAttribute attribute, ParameterHolderKeys key, ReadableTuple<int> tupleSource, ParameterHolder holder) {
			var location = tupleSource.GetValue<int>(attribute);

			if (_is(location, 1, 256, 512)) {
				List<string> values = new List<string>();

				if (_is(location, 256)) values.Add("Upper");
				if (_is(location, 512)) values.Add("Mid");
				if (_is(location, 1)) values.Add("Lower");

				holder.Values[key] = string.Join(", ", values.ToArray());
			}

			if (_is(location, 1024, 2048, 4096)) {
				List<string> values = new List<string>();

				if (_is(location, 1024)) values.Add("Upper");
				if (_is(location, 2048)) values.Add("Mid");
				if (_is(location, 4096)) values.Add("Lower");

				holder.Values[key] = string.Join(", ", values.ToArray());
			}
		}

		private void _autoAddCompound(DbAttribute attribute, ParameterHolderKeys para, ReadableTuple<int> tupleSource, ParameterHolder holder) {
			var sVal = tupleSource.GetValue<string>(attribute);
			int val;
			Int32.TryParse(sVal, out val);

			if (_is(val, 2)) {
				holder.Values[para] = "Weapon";
			}
			else if (_is(val, 32)) {
				holder.Values[para] = "Shield";
			}
			else if (_is(val, 8, 128)) {
				if (val == 8) {
					holder.Values[para] = "Accessory (Right)";
				}
				else if (val == 128) {
					holder.Values[para] = "Accessory (Left)";
				}
				else {
					holder.Values[para] = "Accessory";
				}
			}
			else if (_is(val, 16)) {
				holder.Values[para] = "Armor";
			}
			else if (_is(val, 64)) {
				holder.Values[para] = "Footgear";
			}
			else if (_is(val, 4)) {
				holder.Values[para] = "Garment";
			}
			else if (_is(val, 1, 256, 512)) {
				holder.Values[para] = "Headgear";
			}
		}

		private bool _is(int val, int to) {
			return (val & to) == to;
		}

		private bool _is(int val, params int[] to) {
			return to.Any(t => _is(val, t));
		}

		private string _findAmmoType(string jobHex) {
			List<Job> jobs = JobList.GetApplicableJobsFromHex(jobHex);

			if (jobs.Contains(JobList.Ninja))
				return "Throwing Weapon";

			if (jobs.Contains(JobList.Gunslinger))
				return "Bullet";

			if (jobs.Contains(JobList.Archer) || jobs.Contains(JobList.Hunter) || jobs.Contains(JobList.BardDancer))
				return "Arrow";

			if (jobs.Contains(JobList.Alchemist))
				return "Shell";

			if (jobs.Contains(JobList.Assassin))
				return "Throwing Dagger";

			return null;
		}

		private string _findArmorType(int location) {
			if (_is(location, 32))
				return "Shield";

			if (_is(location, 1, 256, 512))
				return "Headgear";

			if (_is(location, 1024, 2048, 4096))
				return "Costume";

			if (_is(location, 16))
				return "Armor";

			if (_is(location, 64))
				return "Footgear";

			if (_is(location, 4))
				return "Garment";

			if (_is(location, 8, 128)) {
				if (location == 8) {
					return "Accessory (Right)";
				}
				
				if (location == 128) {
					return "Accessory (Left)";
				}

				return "Accessory";
			}

			return null;
		}

		private void _autoAddElement(ReadableTuple<int> tupleSource, ParameterHolder holder) {
			var script = tupleSource.GetValue<string>(ServerItemAttributes.Script);
			const string Bonus = "bonus bAtkEle";

			if (script.Contains(Bonus)) {
				int start = script.IndexOf(Bonus, StringComparison.Ordinal) + Bonus.Length;
				int end = script.IndexOf(";", start, StringComparison.Ordinal);

				if (end < 0)
					end = script.Length;

				var bonus = script.Substring(start, end - start).Trim(',', ' ', ';');

				if (bonus.Length > 4)
					holder.Values[ParameterHolderKeys.Property] = bonus.Substring(4);
			}
			else {
				if (ProjectConfiguration.AutocompleteNeutralProperty) {
					holder.Values[ParameterHolderKeys.Property] = "Neutral";
				}
			}
		}

		private void _autoAddWeight(ReadableTuple<int> tupleSource, ParameterHolder holder) {
			var val = tupleSource.GetValue<int>(ServerItemAttributes.Weight);
			holder.Values[ParameterHolderKeys.Weight] = (val / 10f).ToString(CultureInfo.InvariantCulture).Replace(',', '.');
		}

		private void _autoAdd(DbAttribute attribute, ParameterHolderKeys key, ReadableTuple<int> tupleSource, ParameterHolder holder, int min = 0) {
			var sVal = tupleSource.GetValue<string>(attribute);
			int val = 0;

			if (!string.IsNullOrEmpty(sVal))
				Int32.TryParse(sVal, out val);

			if (val > min) {
				holder.Values[key] = val.ToString(CultureInfo.InvariantCulture);
			}
		}

		private void _autoAddJob(ReadableTuple<int> tupleSource, ParameterHolder holder, int equipLevel) {
			var val = tupleSource.GetValue<string>(ServerItemAttributes.ApplicableJob);
			string applicationJob = JobList.GetStringJobFromHex(val, _getUpper(tupleSource), _getGender(tupleSource), equipLevel);

			//if (!String.IsNullOrEmpty(applicationJob)) {
			holder.Values[ParameterHolderKeys.ApplicableJob] = applicationJob;
			//}
		}

		private int _getGender(ReadableTuple<int> tuple) {
			var gender = tuple.GetValue<int>(ServerItemAttributes.Gender);

			if (gender < 0)
				return 2;
			return gender;
		}

		private int _getUpper(ReadableTuple<int> tuple) {
			var upper = tuple.GetValue<string>(ServerItemAttributes.Upper);

			if (String.IsNullOrEmpty(upper)) {
				return JobGroup.All.Id;
			}

			int ival;
			Int32.TryParse(upper, out ival);
			return ival;
		}

		private void _autoAddJobIfRestricted(ReadableTuple<int> tupleSource, ParameterHolder holder) {
			var val = tupleSource.GetValue<string>(ServerItemAttributes.ApplicableJob);
			string applicationJob = JobList.GetStringJobFromHex(val, _getUpper(tupleSource), _getGender(tupleSource));

			if (String.CompareOrdinal(applicationJob, JobList.EveryJob.Name) != 0) {
				holder.Values[ParameterHolderKeys.ApplicableJob] = applicationJob;
			}
		}

		private int _getInt(DbAttribute attribute, Database.Tuple tupleSource) {
			var sValue = tupleSource.GetValue<string>(attribute);
			int ival;

			Int32.TryParse(sValue, out ival);
			return ival;
		}

		public static readonly List<string> WeaponTypes = new List<string> {
			"None",
			"Dagger",
			"Sword",
			"Two-handed Sword",
			"Spear",
			"Two-handed Spear", // 5
			"Axe",
			"Two-handed Axe",
			"Mace",
			"Two-handed Mace",
			"Rod", // 10
			"Bow",
			"Knuckle",
			"Instrument",
			"Whip",
			"Book", // 15
			"Katar",
			"Pistol",
			"Rifle",
			"Gatling Gun",
			"Shotgun", // 20
			"Grenade Launcher",
			"Huuma",
			"Two-handed Staff",
			"Last",
			"Sword", // 25
			"Sword",
			"Axe",
			"Sword",
			"Axe",
			"Sword Axe",
		};

		public static readonly HashSet<int> TwoHandedWeapons = new HashSet<int> {
			3, 5, 7, 9, 11, 16, 17, 18, 19, 20, 21, 22, 23
		};

		public static readonly Dictionary<string, int> WeaponTypeToViewId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
			{ "None", 0 },
			{ "Dagger", 1 },
			{ "Sword", 2 },
			{ "One-handed Sword", 2 },
			{ "One handed Sword", 2 },
			{ "1-handed Sword", 2 },
			{ "Two-handed Sword", 3 },
			{ "Two handed Sword", 3 },
			{ "2-handed Sword", 3 },
			{ "Spear", 4 },
			{ "One-handed Spear", 4 },
			{ "One handed Spear", 4 },
			{ "1-handed Spear", 4 },
			{ "Two-handed Spear", 5 },
			{ "Two handed Spear", 5 },
			{ "2-handed Spear", 5 },
			{ "Axe", 6 },
			{ "One-handed Axe", 6 },
			{ "One handed Axe", 6 },
			{ "1-handed Axe", 6 },
			{ "Two-handed Axe", 7 },
			{ "Two handed Axe", 7 },
			{ "2-handed Axe", 7 },
			{ "Mace", 8 },
			{ "Two-handed Mace", 9 },
			{ "Two handed Mace", 9 },
			{ "2-handed Mace", 9 },
			{ "Rod", 10 },
			{ "Staff", 10 },
			{ "One-handed Staff", 10 },
			{ "One handed Staff", 10 },
			{ "1-handed Staff", 10 },
			{ "Bow", 11 },
			{ "Knuckle", 12 },
			{ "Claw", 12 },
			{ "Instrument", 13 },
			{ "Musical Instrument", 13 },
			{ "Whip", 14 },
			{ "Book", 15 },
			{ "Katar", 16 },
			{ "Pistol", 17 },
			{ "Revolver", 17 },
			{ "Rifle", 18 },
			{ "Gatling Gun", 19 },
			{ "Shotgun", 20 },
			{ "Grenade Launcher", 21 },
			{ "Huuma", 22 },
			{ "Huuma Shuriken", 22 },
			{ "Two-handed Staff", 23 },
			{ "Two handed Staff", 23 },
			{ "2-handed Staff", 23 },
			{ "Last", 24 },
		};

		private static readonly List<string> _undWeaponTypes = new List<string> {
			"None",
			"³ªÀÌÇÁ",
			"¼Òµå",
			"¹Ù½ºÅ¸µå¼Òµå",
			"Àðº§¸°",
			"Àðº§¸°", // 5
			"¾×½º",
			"¾×½º",
			"Å¬·´",
			"Å¬·´",
			"·Ôµå", // 10
			"º¸¿ì",
			"¹Ù±×³«",
			"¹ÙÀÌ¿Ã¸°",
			"·ÎÇÁ",
			"ºÏ", // 15
			"Ä«Å¸¸£",
			"½Ä½º½´ÅÍ",
			"¶óÀÌÇÃ",
			"µå¸®ÇÁÅÍ",
			"½Ì±Û¼¦°Ç", // 20
			"µð½ºÆ®·ÎÀÌ¾î",
			"Ç³¸¶_ÆíÀÍ",
			"·Ôµå",
			null,
			null, // 25
			null,
			null,
			null,
			null,
			null,
		};

		private string _findWeaponType(ReadableTuple<int> tupleSource) {
			var viewId = _getInt(ServerItemAttributes.ClassNumber, tupleSource);
			var viewId2 = _getInt(ServerItemAttributes.SubType, tupleSource);

			if (viewId2 > 0) {
				var flagsData = FlagsManager.GetFlag<WeaponType>();
				var v = flagsData.Values.FirstOrDefault(p => p.Value == viewId2);

				if (v != null) {
					var index = flagsData.Values.IndexOf(v);
					return WeaponTypes[index];
				}
			}

			if (viewId > 0 && viewId < WeaponTypes.Count) {
				return WeaponTypes[viewId];
			}

			return null;
		}

		private string _findWeaponUnidentifiedResource(ReadableTuple<int> tupleSource) {
			var viewId = _getInt(ServerItemAttributes.ClassNumber, tupleSource);

			// Based on view id
			if (viewId > 0 && viewId < _undWeaponTypes.Count) {
				var res = _undWeaponTypes[viewId];
				if (res == null) return null;
				return EncodingService.FromAnyToDisplayEncoding(res);
			}

			return null;
		}
	}
}