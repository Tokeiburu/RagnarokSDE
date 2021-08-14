using System;
using System.Collections.Generic;
using System.Globalization;
using SDE.Editor.Engines;
using SDE.Editor.Generic.Lists;

namespace SDE.Editor.Generic.Parsers.Generic {
	public static class Constants {
		private static readonly Dictionary<Type, object> _dicosEnums2Strings = new Dictionary<Type, object>();
		private static readonly Dictionary<Type, object> _dicosStrings2Enum = new Dictionary<Type, object>();

		static Constants() {
			_set(HitType.Normal, "Normal");
			_set(HitType.PickupItem, "Pickup_Item");
			_set(HitType.SitDown, "Sit_Down");
			_set(HitType.StandUp, "Stand_Up");
			_set(HitType.Endure, "Endure");
			_set(HitType.Splash, "Splash");
			_set(HitType.SingleHit, "Single");
			_set(HitType.Repeat, "Repeat");
			_set(HitType.MultiHit, "Multi_Hit");
			_set(HitType.MultiHitEndure, "Multi_Hit_Endure");
			_set(HitType.Critical, "Critical");
			_set(HitType.LuckyDodge, "Lucy_Dodge");
			_set(HitType.Touch, "Touch");
			_set(HitType.MultiHitCritical, "Multi_Hit_Critical");
			_makeDico<HitType>();

			_set(SkillTargetType.Passive, "Passive");
			_set(SkillTargetType.Attack, "Attack");
			_set(SkillTargetType.Ground, "Ground");
			_set(SkillTargetType.Self, "Self");
			_set(SkillTargetType.Support, "Support");
			_set(SkillTargetType.Trap, "Trap");
			_makeDico<SkillTargetType>();

			_set(SkillElementType.Neutral, "Neutral");
			_set(SkillElementType.Water, "Water");
			_set(SkillElementType.Earth, "Earth");
			_set(SkillElementType.Fire, "Fire");
			_set(SkillElementType.Wind, "Wind");
			_set(SkillElementType.Poison, "Poison");
			_set(SkillElementType.Holy, "Holy");
			_set(SkillElementType.Dark, "Dark");
			_set(SkillElementType.Ghost, "Ghost");
			_set(SkillElementType.Undead, "Undead");
			_set(SkillElementType.UseWeaponElement, "Weapon");
			_set(SkillElementType.UseEndowedElement, "Endowed");
			_set(SkillElementType.UseRandomElement, "Random");
			_makeDico<SkillElementType>();

			_set(AttackTypeType.None, "None");
			_set(AttackTypeType.Weapon, "Weapon");
			_set(AttackTypeType.Magic, "Magic");
			_set(AttackTypeType.Misc, "Misc");
			_makeDico<AttackTypeType>();

			//_set(SkillDamageType.NoSkillDamage, "NoDamage");
			//_set(SkillDamageType.SplashDamage, "Splash");
			//_set(SkillDamageType.SplitAmongTargets, "SplashSplit");
			//_set(SkillDamageType.IgnoresCasterDamage, "IgnoreAtkCard");
			//_set(SkillDamageType.IgnoresElementalAdjusments, "IgnoreElement");
			//_set(SkillDamageType.IgnoresTargetDefense, "IgnoreDefense");
			//_set(SkillDamageType.IgnoresTargetFlee, "IgnoreFlee");
			//_set(SkillDamageType.IgnoresTargetDefCards, "IgnoreDefCard");
			//_set(SkillDamageType.Critical, "Critical");
			//_set(SkillDamageType.IgnoreLongCard, "IgnoreLongCard");
			//_makeDico<SkillDamageType>();

			//_set(SkillCopyType.Plagiarism, "Plagiarism");
			//_set(SkillCopyType.Reproduce, "Reproduce");
			//_makeDico<SkillCopyType>();

			//_set(SkillCopyRemoveRequirementType.Ammo, "Ammo");
			//_set(SkillCopyRemoveRequirementType.Equipment, "Equipment");
			//_set(SkillCopyRemoveRequirementType.HpCost, "HpCost");
			//_set(SkillCopyRemoveRequirementType.HpRateCost, "HpRateCost");
			//_set(SkillCopyRemoveRequirementType.ItemCost, "ItemCost");
			//_set(SkillCopyRemoveRequirementType.MaxHpTrigger, "MaxHpTrigger");
			//_set(SkillCopyRemoveRequirementType.SpCost, "SpCost");
			//_set(SkillCopyRemoveRequirementType.SpiritSphereCost, "SpiritSphereCost");
			//_set(SkillCopyRemoveRequirementType.SpRateCost, "SpRateCost");
			//_set(SkillCopyRemoveRequirementType.State, "State");
			//_set(SkillCopyRemoveRequirementType.Status, "Status");
			//_set(SkillCopyRemoveRequirementType.Weapon, "Weapon");
			//_set(SkillCopyRemoveRequirementType.ZenyCost, "ZenyCost");
			//_makeDico<SkillCopyRemoveRequirementType>();

			//_set(NoNearNpcType.En0, "WarpPortal");
			//_set(NoNearNpcType.En1, "Shop");
			//_set(NoNearNpcType.En2, "Npc");
			//_set(NoNearNpcType.En3, "Tomb");
			//_makeDico<NoNearNpcType>();

			//_set(CastingFlags.En0, "IgnoreDex");
			//_set(CastingFlags.En1, "IgnoreStatus");
			//_set(CastingFlags.En2, "IgnoreItemBonus");
			//_makeDico<CastingFlags>();

			//_set(WeaponType.En0, "Fist");
			//_set(WeaponType.En1, "Dagger");
			//_set(WeaponType.En2, "1hSword");
			//_set(WeaponType.En3, "2hSword");
			//_set(WeaponType.En4, "1hSpear");
			//_set(WeaponType.En5, "2hSpear");
			//_set(WeaponType.En6, "1hAxe");
			//_set(WeaponType.En7, "2hAxe");
			//_set(WeaponType.En8, "Mace");
			//_set(WeaponType.En9, "2hMace");
			//_set(WeaponType.En10, "Staff");
			//_set(WeaponType.En11, "Bow");
			//_set(WeaponType.En12, "Knuckle");
			//_set(WeaponType.En13, "Musical");
			//_set(WeaponType.En14, "Whip");
			//_set(WeaponType.En15, "Book");
			//_set(WeaponType.En16, "Katar");
			//_set(WeaponType.En17, "Revolver");
			//_set(WeaponType.En18, "Rifle");
			//_set(WeaponType.En19, "Gatling");
			//_set(WeaponType.En20, "Shotgun");
			//_set(WeaponType.En21, "Grenade");
			//_set(WeaponType.En22, "Huuma");
			//_set(WeaponType.En23, "2hStaff");
			//_makeDico<WeaponType>();

			//_set(AmmoType.En0, "Arrow");
			//_set(AmmoType.En1, "Dagger");
			//_set(AmmoType.En2, "Bullet");
			//_set(AmmoType.En3, "Shell");
			//_set(AmmoType.En4, "Grenade");
			//_set(AmmoType.En5, "Shuriken");
			//_set(AmmoType.En6, "Kunai");
			//_set(AmmoType.En7, "Cannonball");
			//_set(AmmoType.En8, "Throwweapon");
			//_makeDico<AmmoType>();

			//_set(UnitFlagType.NoEnemy, "NoEnemy");
			//_set(UnitFlagType.NoReiteration, "NoReiteration");
			//_set(UnitFlagType.NoFootStep, "NoFootSet");
			//_set(UnitFlagType.NoOverlap, "NoOverlap");
			//_set(UnitFlagType.PathCheck, "PathCheck");
			//_set(UnitFlagType.NoPc, "NoPc");
			//_set(UnitFlagType.NoMob, "NoMob");
			//_set(UnitFlagType.Skill, "Skill");
			//_set(UnitFlagType.Dance, "Dance");
			//_set(UnitFlagType.Ensemble, "Ensemble");
			//_set(UnitFlagType.Song, "Song");
			//_set(UnitFlagType.DualMode, "DualMode");
			//_set(UnitFlagType.NoKnockback, "NoKnockback");
			//_set(UnitFlagType.RangedSingleUnit, "RangedSingleUnit");
			//_set(UnitFlagType.CrazyWeedImmune, "CrazyWeedImmune");
			//_set(UnitFlagType.RemovedByFireRain, "RemovedByFireRain");
			//_set(UnitFlagType.KnockbackGroup, "KnockbackGroup");
			//_set(UnitFlagType.HiddenTrap, "HiddenTrap");
			//_makeDico<UnitFlagType>();

			_set(QuestRaceType.All, "All");
			_set(QuestRaceType.Angel, "Angel");
			_set(QuestRaceType.Brute, "Brute");
			_set(QuestRaceType.DemiHuman, "DemiHuman");
			_set(QuestRaceType.Demon, "Demon");
			_set(QuestRaceType.Dragon, "Dragon");
			_set(QuestRaceType.Fish, "Fish");
			_set(QuestRaceType.Formless, "Formless");
			_set(QuestRaceType.Insect, "Insect");
			_set(QuestRaceType.Plant, "Plant");
			_set(QuestRaceType.Undead, "Undead");
			_makeDico<QuestRaceType>();

			_set(QuestSizeType.All, "All");
			_set(QuestSizeType.Small, "Small");
			_set(QuestSizeType.Medium, "Medium");
			_set(QuestSizeType.Large, "Large");
			_makeDico<QuestSizeType>();

			_set(QuestElementType.Neutral, "Neutral");
			_set(QuestElementType.Water, "Water");
			_set(QuestElementType.Earth, "Earth");
			_set(QuestElementType.Fire, "Fire");
			_set(QuestElementType.Wind, "Wind");
			_set(QuestElementType.Poison, "Poison");
			_set(QuestElementType.Holy, "Holy");
			_set(QuestElementType.Dark, "Dark");
			_set(QuestElementType.Ghost, "Ghost");
			_set(QuestElementType.Undead, "Undead");
			_set(QuestElementType.All, "All");
			_makeDico<QuestElementType>();

			_set(TypeType.HealingItem, "Healing");
			//_set(TypeType.HealingItem, "Unknown");
			_set(TypeType.UsableItem, "Usable");
			_set(TypeType.EtcItem, "Etc");
			_set(TypeType.Armor, "Armor");
			_set(TypeType.Weapon, "Weapon");
			_set(TypeType.Card, "Card");
			_set(TypeType.PetEgg, "Petegg");
			_set(TypeType.PetEquip, "Petarmor");
			//_set(TypeType.PetEquip, "unknown2");
			_set(TypeType.Ammo, "Ammo");
			_set(TypeType.UsableWithDelayed, "Delayconsume");
			_set(TypeType.ShadowEquip, "Shadowgear");
			_set(TypeType.UsableWithDelayed2, "Cash");
			_makeDico<TypeType>();

			//_set(ItemFlagType.BindOnEquip, "BindOnEquip");
			//_set(ItemFlagType.BuyingStore, "BuyingStore");
			//_set(ItemFlagType.Container, "Container");
			//_set(ItemFlagType.DeadBranch, "DeadBranch");
			//_set(ItemFlagType.DropAnnounce, "DropAnnounce");
			//_set(ItemFlagType.DropEffect, "DropEffect");
			//_set(ItemFlagType.NoConsume, "NoConsume");
			//_set(ItemFlagType.UniqueId, "UniqueId");
			//_makeDico<ItemFlagType>();

			_set(ItemStackFlagType.Cart, "Cart");
			_set(ItemStackFlagType.GuildStorage, "GuildStorage");
			_set(ItemStackFlagType.Inventory, "Inventory");
			_set(ItemStackFlagType.Storage, "Storage");
			_makeDico<ItemStackFlagType>();
		}

		private static void _set<T>(T key, string value) {
			if (!_dicosEnums2Strings.ContainsKey(typeof(T)))
				_dicosEnums2Strings[typeof(T)] = new Dictionary<T, string>();

			var dico = (Dictionary<T, string>)_dicosEnums2Strings[typeof(T)];
			dico[key] = value;
		}

		private static void _makeDico<T>() {
			Dictionary<T, string> dico1 = (Dictionary<T, string>)_dicosEnums2Strings[typeof(T)];
			Dictionary<string, T> dico2 = new Dictionary<string, T>();

			foreach (var entry in dico1) {
				dico2[entry.Value.ToLower()] = entry.Key;
			}

			_dicosStrings2Enum[typeof(T)] = dico2;
			_dicosEnums2Strings[typeof(T)] = dico1;
		}

		private static TValue _getValue<TKey, TValue>(Dictionary<TKey, TValue> dico, TKey key) {
			if (typeof(TKey) == typeof(string)) {
				int v;

				if (Int32.TryParse((string)(object)key, out v)) {
					return (TValue)(object)v;
				}
			}

			if (dico.ContainsKey(key))
				return dico[key];

			throw new Exception("Undefined constant: " + key + "\r\nType:" + typeof(TValue));
		}

		public static TValue FromString<TValue>(string key) {
			var dico = (Dictionary<string, TValue>)_dicosStrings2Enum[typeof(TValue)];
			return _getValue(dico, key.ToLower());
		}

		public static Enum FromString(string key, Type type) {
			if (type == typeof(SkillElementType)) {
				return _getValue((Dictionary<string, SkillElementType>)_dicosStrings2Enum[type], key.ToLower());
			}

			throw new Exception("Undefined type constants: " + type);
		}

		public static string ToString<TValue>(TValue key) {
			var dico = (Dictionary<TValue, string>)_dicosEnums2Strings[typeof(TValue)];
			return _getValue(dico, key);
		}

		public static string ToString<TValue>(int key) {
			var flagsData = FlagsManager.GetFlag<TValue>();

			if (flagsData != null) {
				return flagsData.Value2Name[key];
			}

			var dico = (Dictionary<TValue, string>)_dicosEnums2Strings[typeof(TValue)];
			return _getValue(dico, (TValue)(object)key);
		}

		public static string Int2String(int key, Type type) {
			if (type == typeof(SkillElementType)) {
				return ((Dictionary<SkillElementType, string>)_dicosEnums2Strings[type])[(SkillElementType)key];
			}

			throw new Exception("Undefined type constants: " + type);
		}

		public static string Parse2DbString<TValue>(string key) {
			Dictionary<string, TValue> dico = (Dictionary<string, TValue>)_dicosStrings2Enum[typeof(TValue)];
			TValue val = _getValue(dico, key.ToLower());
			return ((int)(object)val).ToString(CultureInfo.InvariantCulture);
		}
	}
}
