using System;
using System.Collections.Generic;
using System.Linq;
using ErrorManager;
using GRF.Core;
using GRF.Threading;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.PreviewEngine;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Items;
using SDE.View.ObjectView;
using Utilities.Extension;

namespace SDE.Editor.Engines.RepairEngine {
	public partial class DbValidationEngine : IProgress {
		private readonly SdeDatabase _database;
		private PreviewHelper _helper;

		public DbValidationEngine(SdeDatabase database) {
			_database = database;
			Grf = new GrfHolder();
		}

		private void _validateMobDb(MetaTable<int> db, List<ValidationErrorView> errors) {
			foreach (var tuple in db.FastItems) {
				if (SdeAppConfiguration.DbValidMaxItemDbId >= 0) {
					if (tuple.Key <= 1000 || tuple.Key > SdeAppConfiguration.DbValidMaxMobDbId) {
						errors.Add(new TableError(ValidationErrors.TbMobId, tuple.Key,
							String.Format("Invalid monster ID {0}, allowed values {1} < ID <= {2} (MAX_MOB_DB).",
								tuple.Key, 1000, SdeAppConfiguration.DbValidMaxMobDbId), ServerDbs.Mobs, this));
					}
				}

				if (_pcCheckId((uint)tuple.Key)) {
					errors.Add(new TableError(ValidationErrors.TbReservedId, tuple.Key,
						String.Format("Invalid monster ID {0}, reserved for player classes.",
							tuple.Key), ServerDbs.Mobs, this));
				}

				if (tuple.Key >= (SdeAppConfiguration.DbValidMaxMobDbId - 999) && tuple.Key < SdeAppConfiguration.DbValidMaxMobDbId) {
					errors.Add(new TableError(ValidationErrors.TbInvalidRange, tuple.Key,
						String.Format("Invalid monster ID {0}. Range {1}-{2} is reserved for player clones. Please increase MAX_MOB_DB ({3}).",
							tuple.Key, SdeAppConfiguration.DbValidMaxMobDbId - 999, SdeAppConfiguration.DbValidMaxMobDbId - 1, SdeAppConfiguration.DbValidMaxMobDbId), ServerDbs.Mobs, this));
				}

				var level = tuple.GetIntNoThrow(ServerMobAttributes.Lv);
				var minAtk = tuple.GetIntNoThrow(ServerMobAttributes.Atk1);
				var maxAtk = tuple.GetIntNoThrow(ServerMobAttributes.Atk2);
				var def = tuple.GetIntNoThrow(ServerMobAttributes.Def);
				var mdef = tuple.GetIntNoThrow(ServerMobAttributes.Mdef);
				//var baseExp = tuple.GetIntNoThrow(ServerMobAttributes.Lv);
				//var jobExp = tuple.GetIntNoThrow(ServerMobAttributes.Lv);

				_capValue(level, "level", 1, 0xffff, tuple, errors);
				_capValue(minAtk, "minAtk", 0, 0xffff, tuple, errors);
				_capValue(maxAtk, "maxAtk", 0, 0xffff, tuple, errors);

				var isRenewal = DbPathLocator.GetIsRenewal();

				_capValue(def, "def", isRenewal ? -32768 : -128, isRenewal ? 32767 : 127, tuple, errors);
				_capValue(mdef, "mdef", isRenewal ? -32768 : -128, isRenewal ? 32767 : 127, tuple, errors);

				for (int i = 0; i < 6; i++) {
					var value = tuple.GetIntNoThrow(ServerMobAttributes.Str.Index + i);

					_capValue(value, ServerMobAttributes.AttributeList.Attributes[ServerMobAttributes.Str.Index + i].DisplayName, 0, 0xffff, tuple, errors);
				}

				var defEle = tuple.GetIntNoThrow(ServerMobAttributes.Element) % 10;
				var eleLevel = tuple.GetIntNoThrow(ServerMobAttributes.Element) / 20;

				if (defEle >= SdeAppConfiguration.DbValidMaxMobDbElement) {
					errors.Add(new TableError(ValidationErrors.TbElementType, tuple.Key,
						String.Format("Invalid element type {0} for monster ID {1} (max = {2}).",
							defEle, tuple.Key, SdeAppConfiguration.DbValidMaxMobDbElement - 1), ServerDbs.Mobs, this));
				}

				if (eleLevel < 1 || eleLevel > 4) {
					errors.Add(new TableError(ValidationErrors.TbElementLevel, tuple.Key,
						String.Format("Invalid element level {0} for monster ID {1}, must be in range 1-4.",
							eleLevel, tuple.Key), ServerDbs.Mobs, this));
				}

				var adelay = tuple.GetIntNoThrow(ServerMobAttributes.AttackDelay);
				var amotion = tuple.GetIntNoThrow(ServerMobAttributes.AttackMotion);
				var hp = (long)tuple.GetIntNoThrow(ServerMobAttributes.Hp);
				var mexp = (long)tuple.GetIntNoThrow(ServerMobAttributes.MvpExp);

				_capValue(adelay, "aDelay", 0, 4000, tuple, errors);
				_capValue(amotion, "aMotion", 0, 2000, tuple, errors);
				_capValue(hp, "HP", 0, Int32.MaxValue, tuple, errors);
				_capValue(mexp, "MExp", 0, Int32.MaxValue, tuple, errors);
			}
		}

		private void _capValue(int value, string nameValue, int min, int max, ReadableTuple<int> tuple, List<ValidationErrorView> errors) {
			if (value > max) {
				errors.Add(new TableError(ValidationErrors.TbCapValue, tuple.Key,
					String.Format("Invalid {0} for {1}, found {3}. Value cannot be above {2}.", nameValue, tuple.Key, max, value),
					ServerDbs.Mobs, this));
			}

			if (value < min) {
				errors.Add(new TableError(ValidationErrors.TbCapValue, tuple.Key,
					String.Format("Invalid {0} for {1}, found {3}. Value cannot be below {2}.", nameValue, tuple.Key, min, value),
					ServerDbs.Mobs, this));
			}
		}

		private void _capValue(long value, string nameValue, long min, long max, ReadableTuple<int> tuple, List<ValidationErrorView> errors) {
			if (value > max) {
				errors.Add(new TableError(ValidationErrors.TbCapValue, tuple.Key,
					String.Format("Invalid {0} for {1}, found {3}. Value cannot be above {2}.", nameValue, tuple.Key, max, value),
					ServerDbs.Mobs, this));
			}

			if (value < min) {
				errors.Add(new TableError(ValidationErrors.TbCapValue, tuple.Key,
					String.Format("Invalid {0} for {1}, found {3}. Value cannot be below {2}.", nameValue, tuple.Key, min, value),
					ServerDbs.Mobs, this));
			}
		}

		private void _validateItemDb(MetaTable<int> db, List<ValidationErrorView> errors) {
			HashSet<int> typeValues = Enum.GetValues(typeof(TypeType)).Cast<int>().ToList().ToHashSet();

			foreach (var tuple in db.FastItems) {
				if (SdeAppConfiguration.DbValidMaxItemDbId >= 0) {
					if (tuple.Key <= 0 || tuple.Key >= SdeAppConfiguration.DbValidMaxItemDbId) {
						errors.Add(new TableError(ValidationErrors.TbMaxItemId, tuple.Key, String.Format("Invalid item ID {0}, allowed values {1} < ID < {2} (MAX_ITEMDB).", tuple.Key, 0, SdeAppConfiguration.DbValidMaxItemDbId), ServerDbs.Items, this));
					}
				}

				try {
					if (!typeValues.Contains(tuple.GetValue<int>(ServerItemAttributes.Type))) {
						errors.Add(new TableError(ValidationErrors.TbItemType, tuple.Key, String.Format("Invalid item type {0} for item {1}.", tuple.GetValue<int>(ServerItemAttributes.Type), tuple.Key), ServerDbs.Items, this));
					}
				}
				catch {
					errors.Add(new TableError(ValidationErrors.TbItemType, tuple.Key, String.Format("Invalid item type {0} for item {1}.", "?", tuple.Key), ServerDbs.Items, this));
				}

				var buy = tuple.GetIntNoThrow(ServerItemAttributes.Buy);
				var sell = tuple.GetIntNoThrow(ServerItemAttributes.Sell);

				if (tuple.GetValue<string>(ServerItemAttributes.Buy) == "" && tuple.GetValue<string>(ServerItemAttributes.Sell) == "") {
					buy = sell = 0;
				}
				else if (tuple.GetValue<string>(ServerItemAttributes.Buy) == "") {
					buy = 2 * sell;
				}
				else if (tuple.GetValue<string>(ServerItemAttributes.Sell) == "") {
					sell = buy / 2;
				}

				if (buy / 124.0 < sell / 75.0) {
					errors.Add(new TableError(ValidationErrors.TbZenyExploit, tuple.Key,
						String.Format("Buying/Selling [{0}/{1}] price of item {2} allows Zeny making exploit through buying/selling at discounted/overcharged prices.",
							buy, sell, tuple.Key), ServerDbs.Items, this));
				}

				var slot = tuple.GetIntNoThrow(ServerItemAttributes.NumberOfSlots);

				if (slot > SdeAppConfiguration.DbValidMaxSlotCount) {
					errors.Add(new TableError(ValidationErrors.TbMaxSlotCount, tuple.Key,
						String.Format("Item {0} specifies {1} slots, but the server only supports up to {2}.",
							tuple.Key, slot, SdeAppConfiguration.DbValidMaxSlotCount), ServerDbs.Items, this));
				}

				var equip = tuple.GetIntNoThrow(ServerItemAttributes.Location);
				var type = tuple.GetValue<TypeType>(ServerItemAttributes.Type);

				if (type == TypeType.Weapon || type == TypeType.Armor) {
					if (type == TypeType.Armor && !ItemParser.IsArmorType(tuple)) {
						if (type == TypeType.Armor)
							type = TypeType.Weapon;
						else
							type = TypeType.Armor;
					}
				}

				var sTwoHanded = (tuple.GetIntNoThrow(ServerItemAttributes.Location) & 34) == 34;
				var sVal = tuple.GetIntNoThrow(ServerItemAttributes.ClassNumber);

				bool sTwoHanded2 = ItemGeneratorEngine.TwoHandedWeapons.Contains(sVal);

				if (type == TypeType.Weapon && sTwoHanded != sTwoHanded2) {
					errors.Add(new TableError(ValidationErrors.TbViewId, tuple.Key,
						String.Format("ClassNumber: found server value '" + sVal + "', the location suggests a {0} but the view ID suggests a {1}.",
							sTwoHanded ? "two-handed weapon" : "one-handed weapon",
							sTwoHanded2 ? "two-handed weapon" : "one-handed weapon"),
						ServerDbs.Items, this));
				}

				if (equip == 0 && _isEquip2(type)) {
					errors.Add(new TableError(ValidationErrors.TbEquipField, tuple.Key,
						String.Format("Item {0} is an equipment with no equip-field.",
							tuple.Key), ServerDbs.Items, this));
				}

				var trade = tuple.GetValue<int>(ServerItemAttributes.TradeFlag);

				if (trade > 0x1ff) {
					errors.Add(new TableError(ValidationErrors.TbTradeRestrict, tuple.Key,
						String.Format("Invalid trade restriction flag {0} for item {1}.",
							trade, tuple.Key), ServerDbs.Items, this));
				}

				var tradeOverride = tuple.GetValue<int>(ServerItemAttributes.TradeOverride);

				if (tradeOverride <= 0 || tradeOverride > 100) {
					errors.Add(new TableError(ValidationErrors.TbTradeOverr, tuple.Key,
						String.Format("Invalid trade-override GM level {0} for item {1}.",
							tradeOverride, tuple.Key), ServerDbs.Items, this));
				}

				var nouse = tuple.GetValue<int>(ServerItemAttributes.NoUseFlag);

				if (nouse > 1) {
					errors.Add(new TableError(ValidationErrors.TbNoUseRestrict, tuple.Key,
						String.Format("Invalid nouse restriction flag {0} for item {1}.",
							nouse, tuple.Key), ServerDbs.Items, this));
				}

				var nouseOverride = tuple.GetValue<int>(ServerItemAttributes.NoUseOverride);

				if (nouseOverride <= 0 || nouseOverride > 100) {
					errors.Add(new TableError(ValidationErrors.TbNoUseOverr, tuple.Key,
						String.Format("Invalid nouse-override GM level {0} for item {1}.",
							nouseOverride, tuple.Key), ServerDbs.Items, this));
				}

				var stackValue = (tuple.GetValue<string>(ServerItemAttributes.Stack) ?? "").Trim('[', ']');
				int stackAmount = 0;

				if (stackValue.Contains(",")) {
					stackAmount = stackValue.Split(',')[0].ToInt();
				}

				if (stackAmount > 0 && !_isStackable2(type)) {
					errors.Add(new TableError(ValidationErrors.TbNoStack, tuple.Key,
						String.Format("Item {0} of type {1} is not stackable.",
							tuple.Key, type), ServerDbs.Items, this));
				}

				var viewId = tuple.GetIntNoThrow(ServerItemAttributes.ClassNumber);

				if ((viewId == 13 || viewId == 14) && type == TypeType.Weapon) {
					var gender = tuple.GetIntNoThrow(ServerItemAttributes.Gender);

					if (viewId == 13 && gender != 1) {
						errors.Add(new TableError(ValidationErrors.TbGender, tuple.Key,
							String.Format("Item {0}; musical instruments are always male-only, but the gender field allows females as well.",
								tuple.Key), ServerDbs.Items, this));
					}

					if (viewId == 14 && gender != 0) {
						errors.Add(new TableError(ValidationErrors.TbGender, tuple.Key,
							String.Format("Item {0}; whips are always female-only, but the gender field allows males as well.",
								tuple.Key), ServerDbs.Items, this));
					}
				}
			}
		}

		private static bool _isEquip2(TypeType type) {
			switch(type) {
				case TypeType.Ammo:
				case TypeType.Armor:
				case TypeType.Weapon:
					return true;
			}

			return false;
		}

		private static bool _isStackable2(TypeType type) {
			switch(type) {
				case TypeType.PetEgg:
				case TypeType.PetEquip:
				case TypeType.Armor:
				case TypeType.Weapon:
					return false;
			}

			return true;
		}

		private static bool _pcCheckId(uint id) {
			return id < 30
			       || (id >= 4001 && id <= 4052)
			       || (id >= 4054 && id <= 4087)
			       || (id >= 4096 && id <= 4112)
			       || (id >= 4190 && id <= 4191)
			       || (id >= 4211 && id <= 4212)
			       || (id >= 4215 && id < 4216);
		}

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		public GrfHolder Grf { get; set; }

		public void FindTableErrors(List<ValidationErrorView> errors) {
			_startTask(() => _findTableErrors(errors));
		}

		private void _startTask(Action action) {
			try {
				AProgress.Init(this);
				action();
			}
			catch (OperationCanceledException) {
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				AProgress.Finalize(this);
			}
		}

		private void _findTableErrors(List<ValidationErrorView> errors) {
			_validateItemDb(_database.GetMetaTable<int>(ServerDbs.Items), errors);
			_validateMobDb(_database.GetMetaTable<int>(ServerDbs.Mobs), errors);
		}
	}
}