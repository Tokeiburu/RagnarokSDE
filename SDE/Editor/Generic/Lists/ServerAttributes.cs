using System.Collections.Generic;
using Database;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.UI.FormatConverters;
using SDE.View.ObjectView;

namespace SDE.Editor.Generic.Lists {
	public sealed class ServerItemAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ServerItemAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Item ID"));
		public static readonly DbAttribute AegisName = new ServerItemAttributes(new DbAttribute("AegisName", typeof(AutoAegisNameProperty<int>), "", "Aegis name"));
		public static readonly DbAttribute Name = new ServerItemAttributes(new DbAttribute("Name", typeof(AutoDisplayNameProperty<int>), "", "Name")) { IsDisplayAttribute = true };
		public static readonly DbAttribute Type = new ServerItemAttributes(new DbAttribute("Type", typeof(TypeType), "")) { DataConverter = ValueConverters.GetIntSetZeroStringType, Description = "They type defines the default behavior of the item.", IsSearchable = false };
		public static readonly DbAttribute Buy = new ServerItemAttributes(new DbAttribute("Buy", typeof(CustomBuyProperty), "0", "Buy")) { Description = "Default buying price. When not specified, becomes double the sell price." };
		public static readonly DbAttribute Sell = new ServerItemAttributes(new DbAttribute("Sell", typeof(CustomSellProperty), "")) { Description = "Default selling price. When not specified, becomes half the buy price." };
		public static readonly DbAttribute Weight = new ServerItemAttributes(new DbAttribute("Weight", typeof(WeightPreviewProperty), "")) { DataConverter = ValueConverters.GetSetZeroString, Description = "Item's weight. Each 10 is 1 weight." };
		public static readonly DbAttribute Attack = new ServerItemAttributes(new DbAttribute("Atk", typeof(CustomItemAttackProperty), "", "Attack")) { Description = "Weapon's attack." };
		public static readonly DbAttribute Defense = new ServerItemAttributes(new DbAttribute("Def", typeof(string), "", "Defense")) { Description = "Armor's defense." };
		public static readonly DbAttribute Range = new ServerItemAttributes(new DbAttribute("Range", typeof(string), "")) { Description = "Sets the range (number of cells) of the weapon." };
		public static readonly DbAttribute NumberOfSlots = new ServerItemAttributes(new DbAttribute("Slots", typeof(string), "", "Number of slots")) { Description = "Amount of slots the item possesses." };
		public static readonly DbAttribute ApplicableJob = new ServerItemAttributes(new DbAttribute("Job", typeof(CustomJobProperty), "0xFFFFFFFF", "Applicable job")) { DataConverter = ValueConverters.GetNoHexJobSetHexJob, IsSearchable = false, Description = "Only the specified jobs will be able to use or wear this item." };
		public static readonly DbAttribute Upper = new ServerItemAttributes(new DbAttribute("Upper", typeof(PreviewUpperFlagProperty<int>), "63")) { DataConverter = ValueConverters.GetHexToIntSetInt, Description = "Restricts the usage of the item based for a specific class group." };
		public static readonly DbAttribute Gender = new ServerItemAttributes(new DbAttribute("Gender", typeof(GenderType), "2")) { DataConverter = ValueConverters.GetSetGenderString, IsSearchable = false, Description = "Restricts the usage of the item for a specific gender." };
		public static readonly DbAttribute Location = new ServerItemAttributes(new DbAttribute("Loc", typeof(PreviewLocationDefinedFlagProperty<int, LocationType>), "", "Location")) { DataConverter = ValueConverters.GetHexToIntSetInt, Description = "Restricts the location of the item." };
		public static readonly DbAttribute WeaponLevel = new ServerItemAttributes(new DbAttribute("WeaponLv", typeof(string), "", "Weapon level")) { Description = "Sets the weapon level." };
		public static readonly DbAttribute EquipLevel = new ServerItemAttributes(new DbAttribute("EquipLv", typeof(string), "", "Equip level")) { Description = "Sets the minimum level required to equip this item." };
		public static readonly DbAttribute Refineable = new ServerItemAttributes(new DbAttribute("Refine", typeof(bool), "", "Refineable")) { DataConverter = ValueConverters.GetBooleanSetRefinableString, Description = "Defines wheter or not the equipment can be refined (the value is ignored for other item types)." };
		public static readonly DbAttribute ClassNumber = new ServerItemAttributes(new DbAttribute("View", typeof(CustomHeadgearSprite2Property), "", "View ID")) { Description = "Tells the client which sprite to load." };
		public static readonly DbAttribute Script = new ServerItemAttributes(new DbAttribute("Script", typeof(CustomScriptProperty<int>), "{}")) { DataConverter = ValueConverters.GetScriptNoBracketsSetScriptWithBrackets, IsSearchable = false, Description = "This script will be executed when the item is equipped or used." };
		public static readonly DbAttribute OnEquipScript = new ServerItemAttributes(new DbAttribute("OnEquipScript", typeof(CustomScriptProperty<int>), "{}", "On equip script")) { DataConverter = ValueConverters.GetScriptNoBracketsSetScriptWithBrackets, IsSearchable = false, Description = "This script will be executed when the item is equipped by the player." };
		public static readonly DbAttribute OnUnequipScript = new ServerItemAttributes(new DbAttribute("OnUnequipScript", typeof(CustomScriptProperty<int>), "{}", "On unequip script")) { DataConverter = ValueConverters.GetScriptNoBracketsSetScriptWithBrackets, IsSearchable = false, Description = "This script will be executed when the item is unequipped by the player." };
		public static readonly DbAttribute BindOnEquip = new ServerItemAttributes(new DbAttribute("BindOnEquip", typeof(bool), "false", "Bind on equip")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString, Description = "Binds the item to the player after equipping it." };
		public static readonly DbAttribute BuyingStore = new ServerItemAttributes(new DbAttribute("BuyingStore", typeof(bool), "false", "Buying store")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString, Description = "Allows the item to be bought from a store." };
		public static readonly DbAttribute KeepAfterUse = new ServerItemAttributes(new DbAttribute("KeepAfterUse", typeof(bool), "false", "Keep after use")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString, Description = "Keeps the item after being consumed." };
		public static readonly DbAttribute ForceSerial = new ServerItemAttributes(new DbAttribute("ForceSerial", typeof(bool), "false", "Force unique ID")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute Matk = new ServerItemAttributes(new DbAttribute("Matk", typeof(string), "", "Matk"));
		public static readonly DbAttribute Delay = new ServerItemAttributes(new DbAttribute("Delay", typeof(string), "", "Delay")) { Description = "Delay after using the item (in milliseconds)." };
		public static readonly DbAttribute DelayStatus = new ServerItemAttributes(new DbAttribute("Status", typeof(string), "", "Status")) { Description = "Status Change used to track delay. (Default: None)" };
		public static readonly DbAttribute Stack = new ServerItemAttributes(new DbAttribute("Stack", typeof(string), "[]", "Stack")) { DataConverter = ValueConverters.GetScriptNoBracketsSetScriptWithBracketsSqure, Description = "[amount, type]\r\namount: The maximum amount of items that can be stacked.\r\ntype (mask): \r\n1 = Character inventory\r\n2 = Character cart\r\n4 = Account storage\r\n8 = Guild storage" };
		public static readonly DbAttribute Sprite = new ServerItemAttributes(new DbAttribute("Sprite", typeof(SpriteRedirect), "", "Sprite ID")) { Description = "Redirects the item's sprite (client side) for this one instead.", Visibility = VisibleState.Hidden | VisibleState.ForceShow };

		public static readonly DbAttribute TradeFlag = new ServerItemAttributes(new DbAttribute("TradeFlag", typeof(PreviewTradeDefinedFlagProperty<int, TradeFlag>), "0", "Trade")) { Description = "Trading restrictions (can't be dropped, traded, etc)." };
		public static readonly DbAttribute TradeOverride = new ServerItemAttributes(new DbAttribute("TradeOverride", typeof(int), "100", "Override")) { Description = "Group level to override these trade conditions." };
		public static readonly DbAttribute NoUseFlag = new ServerItemAttributes(new DbAttribute("NoUseFlag", typeof(PreviewNoUseDefinedFlagProperty<int>), "0", "No use")) { Description = "Conditions to make the item not usable." };
		public static readonly DbAttribute NoUseOverride = new ServerItemAttributes(new DbAttribute("NoUseOverride", typeof(int), "100", "Override")) { Description = "Group level to override these trade conditions." };

		public static readonly DbAttribute SubType = new ServerItemAttributes(new DbAttribute("SubType", typeof(SubTypeProperty<int>), "0", "Sub type")) { DataConverter = ValueConverters.GetHexToIntSetInt };
		public static readonly DbAttribute EquipLevelMin = new ServerItemAttributes(new DbAttribute("EquipLevelMin", typeof(CustomItemMaxEquipProperty), "", "Min equip level")) { Description = "Sets the minimum level required to equip this item." };
		public static readonly DbAttribute EquipLevelMax = new ServerItemAttributes(new DbAttribute("EquipLevelMax", typeof(string), "", "Max equip level")) { Description = "Sets the maximum level to equip this item." };
		public static readonly DbAttribute AliasName = new ServerItemAttributes(new DbAttribute("AliasName", typeof(SelectTupleProperty<int>), "", "Alias name")) { AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute Flags = new ServerItemAttributes(new DbAttribute("Flags", typeof(PreviewGenericDefinedFlagProperty<int, ItemFlagType>), "0", "Flags")) { DataConverter = ValueConverters.GetHexToIntSetInt, Description = "The behavior of the skill's attack.\r\n(No damage, spash area, etc)." };
		public static readonly DbAttribute StackAmount = new ServerItemAttributes(new DbAttribute("StackAmount", typeof(string), "", "Stack"));
		public static readonly DbAttribute StackFlags = new ServerItemAttributes(new DbAttribute("StackFlags", typeof(PreviewGenericFlagProperty<int, ItemStackFlagType>), "", "S. flags")) { Description = "Stack restriction flags." };
		public static readonly DbAttribute TempFlags = new ServerItemAttributes(new DbAttribute("StackFlags", typeof(string), "", "S. flags")) { };
		public static readonly DbAttribute TempHideII = new ServerItemAttributes(new DbAttribute("StackFlags", typeof(string), "", "S. flags")) { };
		public static readonly DbAttribute TempClientName = new ServerItemAttributes(new DbAttribute("StackFlags", typeof(AutoDisplayItemInfoNameProperty<int>), "", "ItemInfo name")) { };
		public static readonly DbAttribute TempNoDrop = new ServerItemAttributes(new DbAttribute("StackFlags", typeof(string), "", "S. flags")) { };
		public static readonly DbAttribute TempForceLog = new ServerItemAttributes(new DbAttribute("StackFlags", typeof(string), "", "S. flags")) { };
		public static readonly DbAttribute TempMvpCategory = new ServerItemAttributes(new DbAttribute("StackFlags", typeof(string), "", "MVP tier")) { };
		public static readonly DbAttribute TempExpectedWeight = new ServerItemAttributes(new DbAttribute("StackFlags", typeof(string), "", "Open weight")) { };
		public static readonly DbAttribute TempShadowGear = new ServerItemAttributes(new DbAttribute("StackFlags", typeof(string), "", "S. flags")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute MHItem = new ServerItemAttributes(new DbAttribute("StackFlags", typeof(string), "", "S. flags")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute MHHuntItem = new ServerItemAttributes(new DbAttribute("StackFlags", typeof(string), "", "S. flags")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute MHMaxUses = new ServerItemAttributes(new DbAttribute("StackFlags", typeof(string), "", "MH max uses")) { };
		public static readonly DbAttribute MHResetUsesOnDeath = new ServerItemAttributes(new DbAttribute("StackFlags", typeof(string), "", "S. flags")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute MHUseIncOnSuccess = new ServerItemAttributes(new DbAttribute("StackFlags", typeof(string), "", "S. flags")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute CustomFlags = new ServerItemAttributes(new DbAttribute("CustomFlags", typeof(PreviewGenericDefinedFlagProperty<int, ItemCustomFlagType>), "0", "Custom flags")) { DataConverter = ValueConverters.GetHexToIntSetInt };
		public static readonly DbAttribute MHFlags = new ServerItemAttributes(new DbAttribute("MHFlags", typeof(PreviewGenericDefinedFlagProperty<int, ItemMHFlagType>), "0", "MH flags")) { DataConverter = ValueConverters.GetHexToIntSetInt };
		public static readonly DbAttribute DropEffect = new ServerItemAttributes(new DbAttribute("DropEffect", typeof(DropEffectType), "0", "Drop effect")) { DataConverter = ValueConverters.GetIntSetZeroStringType };
		public static readonly DbAttribute TempWoEDelay = new ServerItemAttributes(new DbAttribute("TempWoEDelay", typeof(string), "", "WoE delay")) { };
		//public static readonly DbAttribute NoUse = new ServerItemAttributes(new DbAttribute("NoUse", typeof(NouseProperty), "{\n}", "No use")) { DataConverter = ValueConverters.GetSetTypeNouse, Description = "Conditions to make the item not usable." };

		static ServerItemAttributes() {
			Buy.AttachedAttribute = Sell;
			Sell.AttachedAttribute = Buy;
		}

		private ServerItemAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ServerComboAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ServerComboAttributes(new PrimaryAttribute("Id", typeof(CustomComboIdProperty<string>), "0:0", "Combo ID"));
		public static readonly DbAttribute Script = new ServerComboAttributes(new DbAttribute("Script", typeof(CustomScriptProperty<string>), "{}")) { DataConverter = ValueConverters.GetScriptNoBracketsSetScriptWithBrackets, Description = "This script will be executed when all the items are equipped." };
		public static readonly DbAttribute Display = new ServerComboAttributes(new DbAttribute("Elements", typeof(ComboBinding), null, "Elements")) { IsDisplayAttribute = true, Visibility = VisibleState.Hidden };

		private ServerComboAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ServerItemGroupAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ServerItemGroupAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Group ID"));
		public static readonly DbAttribute Table = new ServerItemGroupAttributes(new DbAttribute("Value", typeof(CustomItemGroupDisplay<int>), null)) {
			DataCopy = new ReadableTableCopyParser<int>(),
			AttachedObject = new CustomTableInitializer {
				ServerDb = ServerDbs.ItemGroups,
				SubTableAttributeList = ServerItemGroupSubAttributes.AttributeList,
				SubTableServerDbSearch = ServerDbs.Items,
				SubTableParentAttribute = ServerItemGroupSubAttributes.ParentGroup,
				MaxElementsToCopy = ServerItemGroupSubAttributes.DropPerc.Index
			}
		};

		public static readonly DbAttribute Display = new ServerItemGroupAttributes(new DbAttribute("Display name", typeof(ItemGroupBinding), null)) { IsDisplayAttribute = true };

		private ServerItemGroupAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}

		#region Nested type: ReadableTableCopyParser
		public class ReadableTableCopyParser<TKey> : IDataCopy {
			#region IDataCopy Members
			public object CopyFrom(object value) {
				if (!(value is Dictionary<TKey, ReadableTuple<TKey>>))
					return value;

				Dictionary<TKey, ReadableTuple<TKey>> dictionary1 = (Dictionary<TKey, ReadableTuple<TKey>>)value;
				Dictionary<TKey, ReadableTuple<TKey>> dictionary2 = new Dictionary<TKey, ReadableTuple<TKey>>(dictionary1.Count);

				foreach (KeyValuePair<TKey, ReadableTuple<TKey>> keyValuePair in dictionary1) {
					ReadableTuple<TKey> tuple = new ReadableTuple<TKey>(keyValuePair.Key, keyValuePair.Value.Attributes);
					tuple.Copy(keyValuePair.Value);
					dictionary2[keyValuePair.Key] = tuple;
				}

				return dictionary2;
			}
			#endregion
		}
		#endregion
	}

	public sealed class ServerItemGroupSubAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ServerItemGroupSubAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Item ID"));
		public static readonly DbAttribute Rate = new ServerItemGroupSubAttributes(new DbAttribute("Rate", typeof(int), 0)) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Amount = new ServerItemGroupSubAttributes(new DbAttribute("Amount", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Random = new ServerItemGroupSubAttributes(new DbAttribute("Random", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute IsAnnounced = new ServerItemGroupSubAttributes(new DbAttribute("IsAnnounced", typeof(bool), "0", "Is announced")) { DataConverter = ValueConverters.GetBooleanSetIntString };
		public static readonly DbAttribute Duration = new ServerItemGroupSubAttributes(new DbAttribute("Duration", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GUID = new ServerItemGroupSubAttributes(new DbAttribute("GUID", typeof(string), "0", "Unique ID")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute IsBound = new ServerItemGroupSubAttributes(new DbAttribute("IsBound", typeof(bool), "0", "Is bound")) { DataConverter = ValueConverters.GetBooleanSetIntString };
		public static readonly DbAttribute IsNamed = new ServerItemGroupSubAttributes(new DbAttribute("IsNamed", typeof(bool), "0", "Is named")) { DataConverter = ValueConverters.GetBooleanSetIntString };
		public static readonly DbAttribute DropPerc = new ServerItemGroupSubAttributes(new DbAttribute("DropPerc", typeof(DropPercentageBinding), null)) { Visibility = VisibleState.Hidden };
		public static readonly DbAttribute Name = new ServerItemGroupSubAttributes(new DbAttribute("Name", typeof(ItemGroupSubBinding), null)) { IsDisplayAttribute = true, Visibility = VisibleState.Hidden };
		public static readonly DbAttribute ParentGroup = new ServerItemGroupSubAttributes(new DbAttribute("ParentGroup", typeof(int), 0)) { Visibility = VisibleState.Hidden };

		private ServerItemGroupSubAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ServerSkillAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ServerSkillAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Skill ID"));
		public static readonly DbAttribute Range = new ServerSkillAttributes(new DbAttribute("Range", typeof(LevelIntEditProperty<int>), "0")) { DataConverter = ValueConverters.GetIntSetZeroString, Description = "The skill range (number of cells)­­.\r\nCombo skills do not check for range when used, if range is < 5, the skill is considered melee-range." };
		public static readonly DbAttribute HitMode = new ServerSkillAttributes(new DbAttribute("HitMode", typeof(HitType), "0", "Hit mode")) { DataConverter = ValueConverters.GetIntSetZeroString };
		public static readonly DbAttribute TargetType = new ServerSkillAttributes(new DbAttribute("TargetType", typeof(CustomSkillTypeProperty), "0", "Target type")) { DataConverter = ValueConverters.GetIntSetZeroString, Description = "The general behavior of the skill\r\n(passive, enemy, place, self, friend, trap)." };
		public static readonly DbAttribute Element = new ServerSkillAttributes(new DbAttribute("Element", typeof(SkillElementType), "0", "Element")) { DataConverter = ValueConverters.GetIntSetZeroString, Description = "The skill's elemental property." };
		public static readonly DbAttribute DamageFlags = new ServerSkillAttributes(new DbAttribute("SkillDamage", typeof(PreviewGenericDefinedFlagProperty<int, SkillDamageType>), "0", "Damage flags")) { DataConverter = ValueConverters.GetHexToIntSetInt, Description = "The behavior of the skill's attack.\r\n(No damage, spash area, etc)." };
		public static readonly DbAttribute SplashArea = new ServerSkillAttributes(new DbAttribute("SplashEffect", typeof(LevelIntEditProperty<int>), "0", "Splash/effect range")) { DataConverter = ValueConverters.GetIntSetZeroString, Description = "The skill's splash or effect range (-1 for screen-wide)." };
		public static readonly DbAttribute MaxLevel = new ServerSkillAttributes(new DbAttribute("MaxLevel", typeof(string), "0", "Max level")) { DataConverter = ValueConverters.GetIntSetZeroString, Description = "The skill's max level." };
		public static readonly DbAttribute HitCount = new ServerSkillAttributes(new DbAttribute("NumberOfHits", typeof(LevelIntEditProperty<int>), "0", "Number of hits")) { DataConverter = ValueConverters.GetIntSetZeroString, Description = "Number of hits (when positive, damage is increased by hits, negative values just show number of hits without increasing total damage)." };
		public static readonly DbAttribute CastInterrupt = new ServerSkillAttributes(new DbAttribute("CastInterrupt", typeof(bool), "yes", "Cast interruptable")) { DataConverter = ValueConverters.GetBooleanSetYesNoString, Description = "Defines wheter or not the skill can be interrupted." };
		public static readonly DbAttribute DefReduc = new ServerSkillAttributes(new DbAttribute("DefReduc", typeof(LevelIntEditProperty<int>), "0", "Defense reduction")) { DataConverter = ValueConverters.GetIntSetZeroString, Description = "Defense reduction rate while casting." };
		public static readonly DbAttribute Inf2 = new ServerSkillAttributes(new DbAttribute("Inf2", typeof(CustomSkillType2Property), "0", "Flags (inf2)")) { DataConverter = ValueConverters.GetHexToIntSetInt, Description = "Sets special attributes to the skill (ex: quest skill, npc skill, trap, ignores land protected, etc)." };
		public static readonly DbAttribute ActiveInstance = new ServerSkillAttributes(new DbAttribute("Maxcount", typeof(LevelIntEditProperty<int>), "0", "Max active")) { DataConverter = ValueConverters.GetIntSetZeroString, Description = "Max amount of skill instances to place on the ground when player_land_skill_limit/monster_land_skill_limit is enabled. For skills that attack using a path, this is the path length to be used." };
		public static readonly DbAttribute AttackType = new ServerSkillAttributes(new DbAttribute("AttackType", typeof(AttackTypeType), "none", "Attack type")) { DataConverter = ValueConverters.GetIntSetSkillAttackString };
		public static readonly DbAttribute Knockback = new ServerSkillAttributes(new DbAttribute("Blowcount", typeof(LevelIntEditProperty<int>), "0", "Knockback")) { DataConverter = ValueConverters.GetIntSetZeroString, Description = "The amount of cells the skill's knockback will apply." };
		public static readonly DbAttribute Inf3 = new ServerSkillAttributes(new DbAttribute("Inf3", typeof(CustomSkillType3Property), "0", "Renewal behavior")) { DataConverter = ValueConverters.GetHexToIntSetInt, IsSkippable = true };
		public static readonly DbAttribute Name = new ServerSkillAttributes(new DbAttribute("Name", typeof(string), "", "Name")) { DataConverter = ValueConverters.StringTrimEmptyDefault };
		public static readonly DbAttribute Desc = new ServerSkillAttributes(new DbAttribute("Description", typeof(string), "", "Description")) { IsDisplayAttribute = true };
		public static readonly DbAttribute Flag = new ServerSkillAttributes(new DbAttribute("Flag", typeof(CustomSkillFlagProperty), "0", "Map restrictions")) { DataConverter = ValueConverters.GetIntSetZeroString };
		public static readonly DbAttribute Cast = new ServerSkillAttributes(new DbAttribute("Cast", typeof(CustomCastProperty), "0", "Casting time\r\nrestrictions")) { DataConverter = ValueConverters.GetIntSetZeroString };
		public static readonly DbAttribute Delay = new ServerSkillAttributes(new DbAttribute("Delay", typeof(CustomDelayProperty), "0", "Delay\r\nrestrictions")) { DataConverter = ValueConverters.GetIntSetZeroString };
		public static readonly DbAttribute CastingTime = new ServerSkillAttributes(new DbAttribute("CastingTime", typeof(LevelEditProperty<int>), "0", "Casting time")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The amount of time it takes to cast the skill (in milliseconds)." };
		public static readonly DbAttribute AfterCastActDelay = new ServerSkillAttributes(new DbAttribute("AfterCastActDelay", typeof(LevelEditProperty<int>), "0", "After cast\r\nact delay")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The amount of time the character cannot use skills (in milliseconds)." };
		public static readonly DbAttribute AfterCastWalkDelay = new ServerSkillAttributes(new DbAttribute("AfterCastWalkDelay", typeof(LevelEditProperty<int>), "0", "After cast\r\nwalk delay")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The amount of time before the character can move again (in milliseconds)." };
		public static readonly DbAttribute Duration1 = new ServerSkillAttributes(new DbAttribute("Duration1", typeof(LevelEditProperty<int>), "0")) { DataConverter = ValueConverters.GetSetZeroString, Description = "(usually) The duration of the skill on the player." };
		public static readonly DbAttribute Duration2 = new ServerSkillAttributes(new DbAttribute("Duration2", typeof(LevelEditProperty<int>), "0")) { DataConverter = ValueConverters.GetSetZeroString, Description = "(usually) The duration of the skill on the target." };
		public static readonly DbAttribute Cooldown = new ServerSkillAttributes(new DbAttribute("Cooldown", typeof(LevelEditProperty<int>), "0", "Cooldown")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The amount of time until the character can use this skill again (in milliseconds)." };
		public static readonly DbAttribute FixedCastTime = new ServerSkillAttributes(new DbAttribute("FixedCastTime", typeof(LevelEditProperty<int>), "0", "Fixed casttime")) { DataConverter = ValueConverters.GetSetZeroString, Description = "Sets the fixed casttime (renewal only, in milliseconds).", Requirements = new DbRequirement { Renewal = RenewalType.Renewal } };

		public static readonly DbAttribute CopyFlags = new ServerSkillAttributes(new DbAttribute("CopyFlags", typeof(PreviewGenericDefinedFlagProperty<int, SkillCopyType>), "0", "Copy flags")) { DataConverter = ValueConverters.GetHexToIntSetInt };
		public static readonly DbAttribute CopyFlagsRemovedRequirement = new ServerSkillAttributes(new DbAttribute("CopyFlags", typeof(PreviewGenericDefinedFlagProperty<int, SkillCopyRemoveRequirementType>), "0", "Copy remove\r\nrequirement flags")) { DataConverter = ValueConverters.GetHexToIntSetInt };
		public static readonly DbAttribute NoNearNPCRange = new ServerSkillAttributes(new DbAttribute("NoNearNPCRange", typeof(string), "", "No near NPC range")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute NoNearNPCType = new ServerSkillAttributes(new DbAttribute("NoNearNPCType", typeof(PreviewGenericFlagProperty<int, NoNearNpcType>), "", "No near NPC type")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute CastDefenseReduction = new ServerSkillAttributes(new DbAttribute("CastDefenseReduction", typeof(string), "", "No near NPC ")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute CastTimeFlags = new ServerSkillAttributes(new DbAttribute("CastTimeFlags", typeof(PreviewGenericDefinedFlagProperty<int, CastingFlags>), "0", "Cast time flags")) { DataConverter = ValueConverters.GetHexToIntSetInt };
		public static readonly DbAttribute CastDelayFlags = new ServerSkillAttributes(new DbAttribute("CastDelayFlags", typeof(PreviewGenericDefinedFlagProperty<int, CastingFlags>), "0", "Cast delay flags")) { DataConverter = ValueConverters.GetHexToIntSetInt };

		public static readonly DbAttribute RequireHpCost = new ServerSkillAttributes(new DbAttribute("HpCost", typeof(LevelIntEditProperty<int>), "0", "HP Cost")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequireMaxHpTrigger = new ServerSkillAttributes(new DbAttribute("MaxHpTrigger", typeof(string), "0", "Max HP Trigger")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequireSpCost = new ServerSkillAttributes(new DbAttribute("SpCost", typeof(LevelIntEditProperty<int>), "0", "SP Cost")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequireHpRateCost = new ServerSkillAttributes(new DbAttribute("HpRateCost", typeof(LevelIntEditProperty<int>), "0", "HP Rate Cost")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequireSpRateCost = new ServerSkillAttributes(new DbAttribute("SpRateCost", typeof(LevelIntEditProperty<int>), "0", "SP Rate Cost")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequireZenyCost = new ServerSkillAttributes(new DbAttribute("ZenyCost", typeof(LevelIntEditProperty<int>), "0", "Zeny Cost")) { DataConverter = ValueConverters.GetSetZeroString };

		public static readonly DbAttribute RequireWeapons = new ServerSkillAttributes(new DbAttribute("RequireWeapons", typeof(PreviewWeaponFlagProperty<int, WeaponType>), "0xFFFFFF", "Required weapons")) { DataConverter = ValueConverters.GetHexToIntSetInt };
		public static readonly DbAttribute RequireAmmoTypes = new ServerSkillAttributes(new DbAttribute("RequireAmmoTypes", typeof(PreviewGenericDefinedFlagProperty<int, AmmoType>), "0", "Required ammo\r\ntypes")) { DataConverter = ValueConverters.GetHexToIntSetInt };
		public static readonly DbAttribute RequireAmmoAmount = new ServerSkillAttributes(new DbAttribute("RequireAmmoAmount", typeof(LevelIntEditProperty<int>), "0", "Required ammo\r\namount")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequireState = new ServerSkillAttributes(new DbAttribute("RequireState", typeof(RequiredStateTypeNew), "0", "Required state")) { DataConverter = ValueConverters.GetIntSetZeroString };
		public static readonly DbAttribute RequireStatuses = new ServerSkillAttributes(new DbAttribute("RequireStatuses", typeof(LevelEditProperty10<int>), "", "Required statuses")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequireSpiritSphereCost = new ServerSkillAttributes(new DbAttribute("RequireSpiritSphereCost", typeof(LevelIntEditProperty<int>), "", "Spirit sphere cost")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequireItemCost = new ServerSkillAttributes(new DbAttribute("RequireItemCost", typeof(PreviewSelectItemLevelIntEditProperty<int>), "", "Required items"));
		public static readonly DbAttribute RequiredEquipment = new ServerSkillAttributes(new DbAttribute("RequiredEquipment", typeof(PreviewSelectLevelIntEditProperty<int>), "", "Required equipment")) { Description = "Specified equipment to be equipped." };

		public static readonly DbAttribute UnitId = new ServerSkillAttributes(new DbAttribute("UnitId", typeof(string), "", "Id"));
		public static readonly DbAttribute UnitAlternateId = new ServerSkillAttributes(new DbAttribute("UnitAlternateId", typeof(string), "", "Alternate id"));
		public static readonly DbAttribute UnitLayout = new ServerSkillAttributes(new DbAttribute("UnitLayout", typeof(LevelIntEditProperty<int>), "", "Layout")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute UnitRange = new ServerSkillAttributes(new DbAttribute("UnitRange", typeof(LevelIntEditProperty<int>), "", "Range")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute UnitInterval = new ServerSkillAttributes(new DbAttribute("UnitInterval", typeof(string), "", "Interval")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute UnitTarget = new ServerSkillAttributes(new DbAttribute("UnitTarget", typeof(UnitTargetType), "0x3F0000", "Target")) { DataConverter = ValueConverters.GetHexToIntSetInt };
		public static readonly DbAttribute UnitFlag = new ServerSkillAttributes(new DbAttribute("UnitFlag", typeof(PreviewGenericDefinedFlagProperty<int, UnitFlagType>), "", "Flag")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute SkillTargetType = new ServerSkillAttributes(new DbAttribute("TargetType", typeof(SkillTargetType), "0", "Target type")) { DataConverter = ValueConverters.GetIntSetZeroString, Description = "The general behavior of the skill\r\n(passive, enemy, place, self, friend, trap)." };
		public static readonly DbAttribute SkillElement = new ServerSkillAttributes(new DbAttribute("SkillElement", typeof(LevelElementEditProperty<int, SkillElementType>), "Neutral", "Element")) { DataConverter = ValueConverters.GetIntSetZeroString, Description = "The skill's elemental property." };
		public static readonly DbAttribute Inf2New = new ServerSkillAttributes(new DbAttribute("Inf2", typeof(PreviewGenericDefinedFlagProperty<int, SkillType2TypeNew>), "0", "Flags (inf2)")) { DataConverter = ValueConverters.GetHexToIntSetInt, Description = "Sets special attributes to the skill (ex: quest skill, npc skill, trap, ignores land protected, etc)." };

		public static readonly DbAttribute DISPLAY_Requirement = new ServerSkillAttributes(new DbAttribute("DISPLAY", typeof(InvisibleProperty<int>), "", "Skill Requirements")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute DISPLAY_Casttime = new ServerSkillAttributes(new DbAttribute("DISPLAY", typeof(InvisibleProperty<int>), "", "Skill Duration")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute DISPLAY_Unit = new ServerSkillAttributes(new DbAttribute("DISPLAY", typeof(InvisibleProperty<int>), "", "Unit")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute DISPLAY_Others = new ServerSkillAttributes(new DbAttribute("DISPLAY", typeof(InvisibleProperty<int>), "", "Others")) { DataConverter = ValueConverters.GetSetZeroString };

		private ServerSkillAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ServerSkillRequirementsAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute SkillId = new ServerSkillRequirementsAttributes(new PrimaryAttribute("SkillId", typeof(int), 0, "Skill ID"));
		public static readonly DbAttribute HpCost = new ServerSkillRequirementsAttributes(new DbAttribute("HpCost", typeof(LevelIntEditProperty<int>), "0", "HP Cost")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute MaxHpTrigger = new ServerSkillRequirementsAttributes(new DbAttribute("MaxHpTrigger", typeof(string), "0", "Max HP Trigger")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute SpCost = new ServerSkillRequirementsAttributes(new DbAttribute("SpCost", typeof(LevelIntEditProperty<int>), "0", "SP Cost")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute HpRateCost = new ServerSkillRequirementsAttributes(new DbAttribute("HpRateCost", typeof(LevelIntEditProperty<int>), "0", "HP Rate Cost")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute SpRateCost = new ServerSkillRequirementsAttributes(new DbAttribute("SpRateCost", typeof(LevelIntEditProperty<int>), "0", "SP Rate Cost")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ZenyCost = new ServerSkillRequirementsAttributes(new DbAttribute("ZenyCost", typeof(LevelIntEditProperty<int>), "0", "Zeny Cost")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredWeapons = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredWeapons", typeof(LevelIntEditAnyProperty<int>), "99", "Required weapons")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredAmmoTypes = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredAmmoTypes", typeof(LevelIntEditAnyProperty<int>), "0", "Required ammo\r\ntypes")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredAmmoAmount = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredAmmoAmount", typeof(LevelIntEditProperty<int>), "0", "Required ammo\r\namount")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredState = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredState", typeof(RequiredStateType), "none", "Required state")) { DataConverter = ValueConverters.GetIntSetRequiredStateString, IsSearchable = false };
		public static readonly DbAttribute RequiredStatuses = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredStatuses", typeof(LevelEditProperty3<int>), "0", "Required statuses")) { DataConverter = ValueConverters.GetSetZeroString, IsSkippable = true };
		public static readonly DbAttribute SpiritSphereCost = new ServerSkillRequirementsAttributes(new DbAttribute("SpiritSphereCost", typeof(string), "0", "Spirit sphere cost")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredItemID1 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemID1", typeof(SelectTupleProperty<int>), "0", "Required item ID1")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute RequiredItemAmount1 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemAmount1", typeof(string), "0", "Amount")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredItemID2 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemID2", typeof(SelectTupleProperty<int>), "0", "Required item ID2")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute RequiredItemAmount2 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemAmount2", typeof(string), "0", "Amount")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredItemID3 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemID3", typeof(SelectTupleProperty<int>), "0", "Required item ID3")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute RequiredItemAmount3 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemAmount3", typeof(string), "0", "Amount")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredItemID4 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemID4", typeof(SelectTupleProperty<int>), "0", "Required item ID4")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute RequiredItemAmount4 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemAmount4", typeof(string), "0", "Amount")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredItemID5 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemID5", typeof(SelectTupleProperty<int>), "0", "Required item ID5")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute RequiredItemAmount5 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemAmount5", typeof(string), "0", "Amount")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredItemID6 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemID6", typeof(SelectTupleProperty<int>), "0", "Required item ID6")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute RequiredItemAmount6 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemAmount6", typeof(string), "0", "Amount")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredItemID7 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemID7", typeof(SelectTupleProperty<int>), "0", "Required item ID7")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute RequiredItemAmount7 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemAmount7", typeof(string), "0", "Amount")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredItemID8 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemID8", typeof(SelectTupleProperty<int>), "0", "Required item ID8")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute RequiredItemAmount8 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemAmount8", typeof(string), "0", "Amount")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredItemID9 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemID9", typeof(SelectTupleProperty<int>), "0", "Required item ID9")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute RequiredItemAmount9 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemAmount9", typeof(string), "0", "Amount")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredItemID10 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemID10", typeof(SelectTupleProperty<int>), "0", "Required item ID10")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute RequiredItemAmount10 = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredItemAmount10", typeof(string), "0", "Amount")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RequiredEquipment = new ServerSkillRequirementsAttributes(new DbAttribute("RequiredEquipment", typeof(LevelEditProperty10<int>), "0", "Required equipment")) { DataConverter = ValueConverters.GetSetZeroString, IsSkippable = true, Description = "Specified equipment to be equipped." };
		public static readonly DbAttribute Display = new ServerSkillRequirementsAttributes(new DbAttribute("Name", typeof(SkillBinding), null)) { IsDisplayAttribute = true, Visibility = VisibleState.Hidden };

		private ServerSkillRequirementsAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	// ReSharper disable InconsistentNaming
	public sealed class ServerMobAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ServerMobAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Mob ID"));
		public static readonly DbAttribute AegisName = new ServerMobAttributes(new DbAttribute("SpriteName", typeof(AutoSpriteNameProperty<int>), "", "Aegis name")) { IsSearchable = false, Description = "This is the resource's name id (must be unique) that will be used by the client." };
		public static readonly DbAttribute KRoName = new ServerMobAttributes(new DbAttribute("KRoName", typeof(AutokRONameProperty<int>), "", "kRO name")) { IsDisplayAttribute = true, IsSearchable = true, Description = "This name is what will be displayed under the mob ingame." };
		public static readonly DbAttribute IRoName = new ServerMobAttributes(new DbAttribute("IRoName", typeof(AutoiRONameProperty<int>), "", "iRO name")) { IsSearchable = true };
		public static readonly DbAttribute Lv = new ServerMobAttributes(new DbAttribute("Lv", typeof(string), "1", "Level")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The mob's level." };
		public static readonly DbAttribute Hp = new ServerMobAttributes(new DbAttribute("Hp", typeof(string), "1", "HP")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The mob's hit points." };
		public static readonly DbAttribute Sp = new ServerMobAttributes(new DbAttribute("Sp", typeof(string), "0", "SP")) { DataConverter = ValueConverters.GetSetZeroString, Description = "(not used) The mob's spell points." };
		public static readonly DbAttribute Exp = new ServerMobAttributes(new DbAttribute("Exp", typeof(string), "0", "Exp")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The base experience the player will get after killing the mob." };
		public static readonly DbAttribute JExp = new ServerMobAttributes(new DbAttribute("JExp", typeof(string), "0", "Job exp")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The job experience the player will get after killing the mob." };
		public static readonly DbAttribute AttackRange = new ServerMobAttributes(new DbAttribute("AttackRange", typeof(string), "0", "Attack range")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The range of the attack." };
		public static readonly DbAttribute Atk1 = new ServerMobAttributes(new DbAttribute("Atk1", typeof(string), "0", "Min attack")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The minimum attack of the mob." };
		public static readonly DbAttribute Atk2 = new ServerMobAttributes(new DbAttribute("Atk2", typeof(CustomAttackProperty), "", "Max attack")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The maximum attack of the mob. If set to 0, the value will be the same as the minimum attack.", AttachedAttribute = Atk1 };
		public static readonly DbAttribute Def = new ServerMobAttributes(new DbAttribute("Def", typeof(string), "0", "Defense")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The defense of the mob." };
		public static readonly DbAttribute Mdef = new ServerMobAttributes(new DbAttribute("Mdef", typeof(string), "0", "M. defense")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The magical defense of the mob." };
		public static readonly DbAttribute Str = new ServerMobAttributes(new DbAttribute("Str", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The strength of the mob." };
		public static readonly DbAttribute Agi = new ServerMobAttributes(new DbAttribute("Agi", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The agility of the mob." };
		public static readonly DbAttribute Vit = new ServerMobAttributes(new DbAttribute("Vit", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The vitality of the mob." };
		public static readonly DbAttribute Int = new ServerMobAttributes(new DbAttribute("Int", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The intelligence of the mob." };
		public static readonly DbAttribute Dex = new ServerMobAttributes(new DbAttribute("Dex", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The dexterity of the mob." };
		public static readonly DbAttribute Luk = new ServerMobAttributes(new DbAttribute("Luk", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The luck of the mob." };
		public static readonly DbAttribute ViewRange = new ServerMobAttributes(new DbAttribute("ViewRange", typeof(string), "0", "Spell range")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The maximum range the mob can use its spells." };
		public static readonly DbAttribute ChaseRange = new ServerMobAttributes(new DbAttribute("ChaseRange", typeof(string), "0", "Sight range")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The sight of the mob (the number of cells it can see)." };
		public static readonly DbAttribute Size = new ServerMobAttributes(new DbAttribute("Size", typeof(ScaleType), "0")) { IsSearchable = false, DataConverter = ValueConverters.GetIntSetEmptyString };
		public static readonly DbAttribute Race = new ServerMobAttributes(new DbAttribute("Race", typeof(MobRaceType), "0")) { IsSearchable = false, DataConverter = ValueConverters.GetIntSetEmptyString };
		public static readonly DbAttribute Element = new ServerMobAttributes(new DbAttribute("Element", typeof(ElementalFormat), "20")) { DataConverter = ValueConverters.GetIntSetEmptyString };
		public static readonly DbAttribute Mode = new ServerMobAttributes(new DbAttribute("Mode", typeof(CustomModeProperty), "0")) { DataConverter = ValueConverters.GetHexToIntSetInt, Description = "Mode of the mob (can move, looter, aggressive, etc)." };
		public static readonly DbAttribute MoveSpeed = new ServerMobAttributes(new DbAttribute("MoveSpeed", typeof(string), "0", "Speed")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The lower this value, the faster the mob will walk.\r\nCells/second = 1000/Speed.\r\nSpeed = 1000/(Cells/second)" };
		public static readonly DbAttribute AttackDelay = new ServerMobAttributes(new DbAttribute("AttackDelay", typeof(string), "0", "ADelay")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The attack speed of the mob (aspd). The lower this value, the faster the attack will be. This corresponds to the delay in ms after the mob attacked.\r\nAttacks/second (ASPD) = 1000/ADelay\r\nADelay = 1000/ASPD" };
		public static readonly DbAttribute AttackMotion = new ServerMobAttributes(new DbAttribute("AttackMotion", typeof(string), "0", "AMotion")) { DataConverter = ValueConverters.GetSetZeroString, Description = "The duration in ms of the attack animation. The lower this value, the faster the animation will be. After a hit, the mob will need to wait for AMotion ms." };
		public static readonly DbAttribute DamageMotion = new ServerMobAttributes(new DbAttribute("DamageMotion", typeof(string), "0", "DMotion")) { DataConverter = ValueConverters.GetSetZeroString, Description = "Amount of ms the mob will freeze after being hit." };
		public static readonly DbAttribute MvpExp = new ServerMobAttributes(new DbAttribute("MvpExp", typeof(string), "0", "MVP exp")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ExpPer = new ServerMobAttributes(new DbAttribute("ExpPer", typeof(string), "0", "Exp %")) { DataConverter = ValueConverters.GetSetZeroString, IsSkippable = true, Description = "MVP experience boost (default at 10000, 100%)." }; // eAthena support
		public static readonly DbAttribute Mvp1ID = new ServerMobAttributes(new DbAttribute("Mvp1ID", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Mvp1Per = new ServerMobAttributes(new DbAttribute("Mvp1Per", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Mvp2ID = new ServerMobAttributes(new DbAttribute("Mvp2ID", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Mvp2Per = new ServerMobAttributes(new DbAttribute("Mvp2Per", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Mvp3ID = new ServerMobAttributes(new DbAttribute("Mvp3ID", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Mvp3Per = new ServerMobAttributes(new DbAttribute("Mvp3Per", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop1ID = new ServerMobAttributes(new DbAttribute("Drop1ID", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop1Per = new ServerMobAttributes(new DbAttribute("Drop1Per", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop2ID = new ServerMobAttributes(new DbAttribute("Drop2ID", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop2Per = new ServerMobAttributes(new DbAttribute("Drop2Per", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop3ID = new ServerMobAttributes(new DbAttribute("Drop3ID", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop3Per = new ServerMobAttributes(new DbAttribute("Drop3Per", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop4ID = new ServerMobAttributes(new DbAttribute("Drop4ID", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop4Per = new ServerMobAttributes(new DbAttribute("Drop4Per", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop5ID = new ServerMobAttributes(new DbAttribute("Drop5ID", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop5Per = new ServerMobAttributes(new DbAttribute("Drop5Per", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop6ID = new ServerMobAttributes(new DbAttribute("Drop6ID", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop6Per = new ServerMobAttributes(new DbAttribute("Drop6Per", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop7ID = new ServerMobAttributes(new DbAttribute("Drop7ID", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop7Per = new ServerMobAttributes(new DbAttribute("Drop7Per", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop8ID = new ServerMobAttributes(new DbAttribute("Drop8ID", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop8Per = new ServerMobAttributes(new DbAttribute("Drop8Per", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop9ID = new ServerMobAttributes(new DbAttribute("Drop9ID", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Drop9Per = new ServerMobAttributes(new DbAttribute("Drop9Per", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute DropCardid = new ServerMobAttributes(new DbAttribute("DropCardid", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute DropCardper = new ServerMobAttributes(new DbAttribute("DropCardper", typeof(string), "0")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ClientSprite = new ServerMobAttributes(new DbAttribute("ClientSprite", typeof(CustomMobSpriteProperty), "", "Client sprite")) { Description = "This is the resource name that will be used by the client.", Visibility = VisibleState.Hidden | VisibleState.ForceShow };
		public static readonly DbAttribute Drop1Flags = new ServerMobAttributes(new DbAttribute("Drop1Flags", typeof(bool), "false")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute Drop2Flags = new ServerMobAttributes(new DbAttribute("Drop2Flags", typeof(bool), "false")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute Drop3Flags = new ServerMobAttributes(new DbAttribute("Drop3Flags", typeof(bool), "false")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute Drop4Flags = new ServerMobAttributes(new DbAttribute("Drop4Flags", typeof(bool), "false")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute Drop5Flags = new ServerMobAttributes(new DbAttribute("Drop5Flags", typeof(bool), "false")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute Drop6Flags = new ServerMobAttributes(new DbAttribute("Drop6Flags", typeof(bool), "false")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute Drop7Flags = new ServerMobAttributes(new DbAttribute("Drop7Flags", typeof(bool), "false")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute Drop8Flags = new ServerMobAttributes(new DbAttribute("Drop8Flags", typeof(bool), "false")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute Drop9Flags = new ServerMobAttributes(new DbAttribute("Drop9Flags", typeof(bool), "false")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute Drop10Flags = new ServerMobAttributes(new DbAttribute("Drop10Flags", typeof(bool), "false")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };
		public static readonly DbAttribute Drop1RandomOptionGroup = new ServerMobAttributes(new DbAttribute("Drop1RandomOptionGroup", typeof(string), ""));
		public static readonly DbAttribute Drop2RandomOptionGroup = new ServerMobAttributes(new DbAttribute("Drop2RandomOptionGroup", typeof(string), ""));
		public static readonly DbAttribute Drop3RandomOptionGroup = new ServerMobAttributes(new DbAttribute("Drop3RandomOptionGroup", typeof(string), ""));
		public static readonly DbAttribute Drop4RandomOptionGroup = new ServerMobAttributes(new DbAttribute("Drop4RandomOptionGroup", typeof(string), ""));
		public static readonly DbAttribute Drop5RandomOptionGroup = new ServerMobAttributes(new DbAttribute("Drop5RandomOptionGroup", typeof(string), ""));
		public static readonly DbAttribute Drop6RandomOptionGroup = new ServerMobAttributes(new DbAttribute("Drop6RandomOptionGroup", typeof(string), ""));
		public static readonly DbAttribute Drop7RandomOptionGroup = new ServerMobAttributes(new DbAttribute("Drop7RandomOptionGroup", typeof(string), ""));
		public static readonly DbAttribute Drop8RandomOptionGroup = new ServerMobAttributes(new DbAttribute("Drop8RandomOptionGroup", typeof(string), ""));
		public static readonly DbAttribute Drop9RandomOptionGroup = new ServerMobAttributes(new DbAttribute("Drop9RandomOptionGroup", typeof(string), ""));
		public static readonly DbAttribute Drop10RandomOptionGroup = new ServerMobAttributes(new DbAttribute("Drop10RandomOptionGroup", typeof(string), ""));
		public static readonly DbAttribute Mvp1RandomOptionGroup = new ServerMobAttributes(new DbAttribute("Mvp1RandomOptionGroup", typeof(string), ""));
		public static readonly DbAttribute Mvp2RandomOptionGroup = new ServerMobAttributes(new DbAttribute("Mvp2RandomOptionGroup", typeof(string), ""));
		public static readonly DbAttribute Mvp3RandomOptionGroup = new ServerMobAttributes(new DbAttribute("Mvp3RandomOptionGroup", typeof(string), ""));
		public static readonly DbAttribute Class = new ServerMobAttributes(new DbAttribute("Class", typeof(ClassType), "0", "Class")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute NewMode = new ServerMobAttributes(new DbAttribute("Mode", typeof(PreviewGenericDefinedFlagProperty<int, NewMobModeType>), "")) { DataConverter = ValueConverters.GetHexToIntSetInt, Description = "Mode of the mob (can move, looter, aggressive, etc)." };
		public static readonly DbAttribute RaceGroups = new ServerMobAttributes(new DbAttribute("RaceGroups", typeof(PreviewGenericDefinedFlagProperty<int, MobGroup2Type>), "Race groups")) { DataConverter = ValueConverters.GetHexToIntSetInt, Description = "Special group that the mob belongs to." };
		public static readonly DbAttribute DamageTaken = new ServerMobAttributes(new DbAttribute("DamageTaken", typeof(string), "0", "DT")) { DataConverter = ValueConverters.GetSetZeroString, Description = "Damage taken in percentage. For Green Aura MVPs, this value is set to 10." };
		public static readonly DbAttribute Sprite = new ServerMobAttributes(new DbAttribute("Sprite", typeof(SpriteRedirect2), "", "Sprite ID")) { Description = "Redirects the mob's sprite (client side) for this one instead." };

		private ServerMobAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ServerMobSkillAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute RealId = new ServerMobSkillAttributes(new PrimaryAttribute("RealId", typeof(string), 0, "_internalId"));
		public static readonly DbAttribute MobId = new ServerMobSkillAttributes(new DbAttribute("Id", typeof(SelectTupleProperty<string>), "0", "Mob ID")) { AttachedObject = ServerDbs.Mobs, DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute DummyName = new ServerMobSkillAttributes(new DbAttribute("SkillName", typeof(AutoDisplayMobSkillProperty<string>), "", "Display name"));
		public static readonly DbAttribute State = new ServerMobSkillAttributes(new DbAttribute("State", typeof(StateType), "any")) { DataConverter = ValueConverters.GetIntSetStateTypeString, IsSearchable = false };
		public static readonly DbAttribute SkillId = new ServerMobSkillAttributes(new DbAttribute("SkillId", typeof(SelectTupleProperty<string>), "0", "Skill ID")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Skills };
		public static readonly DbAttribute SkillLv = new ServerMobSkillAttributes(new DbAttribute("SkillLv", typeof(SkillLevelPreviewProperty<string>), "0", "Skill level")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Rate = new ServerMobSkillAttributes(new DbAttribute("Rate", typeof(RatePreviewProperty<string>), "0")) { DataConverter = ValueConverters.GetSetZeroString, Description = "Chance of this skill triggering after the delay." };
		public static readonly DbAttribute CastTime = new ServerMobSkillAttributes(new DbAttribute("CastTime", typeof(TimePreviewProperty<string>), "0", "Cast time")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Delay = new ServerMobSkillAttributes(new DbAttribute("Delay", typeof(TimePreviewProperty<string>), "0")) { DataConverter = ValueConverters.GetSetZeroString, Description = "Minimal delay before this skill can be triggered again." };
		public static readonly DbAttribute Cancelable = new ServerMobSkillAttributes(new DbAttribute("Cancelable", typeof(bool), "no")) { DataConverter = ValueConverters.GetBooleanSetYesNoString };
		public static readonly DbAttribute Target = new ServerMobSkillAttributes(new DbAttribute("Target", typeof(TargetType), "target", "Target")) { DataConverter = ValueConverters.GetIntSetTargetString, IsSearchable = false };
		public static readonly DbAttribute ConditionType = new ServerMobSkillAttributes(new DbAttribute("ConditionType", typeof(ConditionType), "always", "Condition type")) { DataConverter = ValueConverters.GetIntSetConditionTypeString, IsSearchable = true };
		public static readonly DbAttribute ConditionValue = new ServerMobSkillAttributes(new DbAttribute("ConditionValue", typeof(string), "0", "Condition value"));
		public static readonly DbAttribute Val1 = new ServerMobSkillAttributes(new DbAttribute("Val1", typeof(string), ""));
		public static readonly DbAttribute Val2 = new ServerMobSkillAttributes(new DbAttribute("Val2", typeof(string), ""));
		public static readonly DbAttribute Val3 = new ServerMobSkillAttributes(new DbAttribute("Val3", typeof(string), ""));
		public static readonly DbAttribute Val4 = new ServerMobSkillAttributes(new DbAttribute("Val4", typeof(string), ""));
		public static readonly DbAttribute Val5 = new ServerMobSkillAttributes(new DbAttribute("Val5", typeof(string), ""));
		public static readonly DbAttribute Emotion = new ServerMobSkillAttributes(new DbAttribute("Emotion", typeof(SelectEmotion<string>), ""));
		public static readonly DbAttribute Chat = new ServerMobSkillAttributes(new DbAttribute("Chat", typeof(string), ""));

		private ServerMobSkillAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ServerMobBossAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ServerMobBossAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Mob ID"));
		public static readonly DbAttribute DummyName = new ServerMobBossAttributes(new DbAttribute("DummyName", typeof(AutoDummyNameProperty<int>), "", "Dummy name"));
		public static readonly DbAttribute Rate = new ServerMobBossAttributes(new DbAttribute("Rate", typeof(ListPourcentagePreviewProperty), "0", "Spawn rate")) { DataConverter = ValueConverters.GetIntSetZeroString };
		public static readonly DbAttribute MobGroup = new ServerMobBossAttributes(new DbAttribute("MobGroup", typeof(string), "", "Mob group")) { IsSkippable = true };

		private ServerMobBossAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ServerMobGroupAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ServerMobGroupAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Group ID"));

		public static readonly DbAttribute Table = new ServerMobGroupAttributes(new DbAttribute("Value", typeof(CustomMobGroupDisplay<int>), null)) {
			DataCopy = new ServerItemGroupAttributes.ReadableTableCopyParser<int>(),
			AttachedObject = new CustomTableInitializer {
				ServerDb = ServerDbs.MobGroups,
				SubTableAttributeList = ServerMobGroupSubAttributes.AttributeList,
				SubTableServerDbSearch = ServerDbs.Mobs,
				SubTableParentAttribute = ServerMobGroupSubAttributes.ParentGroup,
				MaxElementsToCopy = 3
			}
		};

		public static readonly DbAttribute Display = new ServerMobGroupAttributes(new DbAttribute("Display name", typeof(MobGroupsBinding), null)) { IsDisplayAttribute = true };

		private ServerMobGroupAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ServerMobGroupSubAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ServerMobGroupSubAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Mob ID"));
		public static readonly DbAttribute DummyName = new ServerMobGroupSubAttributes(new DbAttribute("Name", typeof(AutoDummyNameProperty<int>), "", "Name")) { IsDisplayAttribute = true };
		//public static readonly DbAttribute IRoName = new ServerMobAttributes(new DbAttribute("IRoName", typeof(AutoiRONameProperty<int>), "", "iRO name")) { IsSearchable = true };
		public static readonly DbAttribute Rate = new ServerMobGroupSubAttributes(new DbAttribute("Rate", typeof(int), 0, "Rate")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute MobGroup = new ServerMobGroupSubAttributes(new DbAttribute("MobGroup", typeof(string), "", "Mob group")) { IsSkippable = true };
		public static readonly DbAttribute DropPerc = new ServerMobGroupSubAttributes(new DbAttribute("DropPerc", typeof(DropPercentageMobBinding), null)) { Visibility = VisibleState.Hidden };
		public static readonly DbAttribute ParentGroup = new ServerMobGroupSubAttributes(new DbAttribute("ParentGroup", typeof(int), 0)) { Visibility = VisibleState.Hidden };

		private ServerMobGroupSubAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ServerQuestsAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ServerQuestsAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Quest ID"));
		public static readonly DbAttribute TimeLimit = new ServerQuestsAttributes(new DbAttribute("TimeLimit", typeof(TimeHourPreviewProperty<int>), "0", "Time limit")) { Description = "The amount of time allowed to finish the quest." };
		public static readonly DbAttribute TargetId1 = new ServerQuestsAttributes(new DbAttribute("TargetId1", typeof(SelectTupleProperty<int>), "0", "1 - Target ID")) { Description = "Mob ID to kill.", AttachedObject = ServerDbs.Mobs };
		public static readonly DbAttribute Val1 = new ServerQuestsAttributes(new DbAttribute("Val1", typeof(string), "0", "1 - Amount")) { Description = "The amount of mobs to kill." };
		public static readonly DbAttribute TargetId2 = new ServerQuestsAttributes(new DbAttribute("TargetId2", typeof(SelectTupleProperty<int>), "0", "2 - Target ID")) { Description = "Mob ID to kill.", AttachedObject = ServerDbs.Mobs };
		public static readonly DbAttribute Val2 = new ServerQuestsAttributes(new DbAttribute("Val2", typeof(string), "0", "2 - Amount")) { Description = "The amount of mobs to kill." };
		public static readonly DbAttribute TargetId3 = new ServerQuestsAttributes(new DbAttribute("TargetId3", typeof(SelectTupleProperty<int>), "0", "3 - Target ID")) { Description = "Mob ID to kill.", AttachedObject = ServerDbs.Mobs };
		public static readonly DbAttribute Val3 = new ServerQuestsAttributes(new DbAttribute("Val3", typeof(string), "0", "3 - Amount")) { Description = "The amount of mobs to kill." };
		public static readonly DbAttribute MobId1 = new ServerQuestsAttributes(new DbAttribute("MobId1", typeof(SelectTupleProperty<int>), "0", "1 - Mob ID")) { Description = "Mob ID.", AttachedObject = ServerDbs.Mobs };
		public static readonly DbAttribute NameId1 = new ServerQuestsAttributes(new DbAttribute("NameId1", typeof(SelectTupleProperty<int>), "0", "1 - Drop ID")) { Description = "The item ID.", AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute Rate1 = new ServerQuestsAttributes(new DbAttribute("Rate1", typeof(RatePreviewProperty<int>), "0", "1 - Rate")) { Description = "The rate of the item drop." };
		public static readonly DbAttribute MobId2 = new ServerQuestsAttributes(new DbAttribute("MobId2", typeof(SelectTupleProperty<int>), "0", "2 - Drop ID")) { Description = "Mob ID.", AttachedObject = ServerDbs.Mobs };
		public static readonly DbAttribute NameId2 = new ServerQuestsAttributes(new DbAttribute("NameId2", typeof(SelectTupleProperty<int>), "0", "2 - Name ID")) { Description = "The item ID.", AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute Rate2 = new ServerQuestsAttributes(new DbAttribute("Rate2", typeof(RatePreviewProperty<int>), "0", "2 - Rate")) { Description = "The rate of the item drop." };
		public static readonly DbAttribute MobId3 = new ServerQuestsAttributes(new DbAttribute("MobId3", typeof(SelectTupleProperty<int>), "0", "3 - Drop ID")) { Description = "Mob ID.", AttachedObject = ServerDbs.Mobs };
		public static readonly DbAttribute NameId3 = new ServerQuestsAttributes(new DbAttribute("NameId3", typeof(SelectTupleProperty<int>), "0", "3 - Name ID")) { Description = "The item ID.", AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute Rate3 = new ServerQuestsAttributes(new DbAttribute("Rate3", typeof(RatePreviewProperty<int>), "0", "3 - Rate")) { Description = "The rate of the item drop." };
		public static readonly DbAttribute QuestTitle = new ServerQuestsAttributes(new DbAttribute("QuestTitle", typeof(string), "", "Name")) { Description = "The name of the quest (not read by the client).", DataConverter = ValueConverters.StringRemoveQuotes, IsDisplayAttribute = true };

		public static readonly DbAttribute Race1 = new ServerQuestsAttributes(new DbAttribute("Race1", typeof(QuestRaceType), "0", "1 - Race")) { IsSearchable = false, DataConverter = ValueConverters.GetIntSetRaceTypeString };
		public static readonly DbAttribute Race2 = new ServerQuestsAttributes(new DbAttribute("Race2", typeof(QuestRaceType), "0", "2 - Race")) { IsSearchable = false, DataConverter = ValueConverters.GetIntSetRaceTypeString };
		public static readonly DbAttribute Race3 = new ServerQuestsAttributes(new DbAttribute("Race3", typeof(QuestRaceType), "0", "3 - Race")) { IsSearchable = false, DataConverter = ValueConverters.GetIntSetRaceTypeString };

		public static readonly DbAttribute Size1 = new ServerQuestsAttributes(new DbAttribute("Size1", typeof(QuestSizeType), "0", "1 - Size")) { IsSearchable = false, DataConverter = ValueConverters.GetIntSetSizeTypeString };
		public static readonly DbAttribute Size2 = new ServerQuestsAttributes(new DbAttribute("Size2", typeof(QuestSizeType), "0", "2 - Size")) { IsSearchable = false, DataConverter = ValueConverters.GetIntSetSizeTypeString };
		public static readonly DbAttribute Size3 = new ServerQuestsAttributes(new DbAttribute("Size3", typeof(QuestSizeType), "0", "3 - Size")) { IsSearchable = false, DataConverter = ValueConverters.GetIntSetSizeTypeString };

		public static readonly DbAttribute Element1 = new ServerQuestsAttributes(new DbAttribute("Element1", typeof(QuestElementType), "0", "1 - Element")) { IsSearchable = false, DataConverter = ValueConverters.GetIntSetElementTypeString };
		public static readonly DbAttribute Element2 = new ServerQuestsAttributes(new DbAttribute("Element2", typeof(QuestElementType), "0", "2 - Element")) { IsSearchable = false, DataConverter = ValueConverters.GetIntSetElementTypeString };
		public static readonly DbAttribute Element3 = new ServerQuestsAttributes(new DbAttribute("Element3", typeof(QuestElementType), "0", "3 - Element")) { IsSearchable = false, DataConverter = ValueConverters.GetIntSetElementTypeString };

		public static readonly DbAttribute MinLevel1 = new ServerQuestsAttributes(new DbAttribute("MinLevel1", typeof(string), "0", "1 - MinLevel")) { Description = "Minimum monster level target. If not supplied but MaxLevel defined, MinLevel is 1. Set to 0 to ignore MinLevel on import. (Default: 0)" };
		public static readonly DbAttribute MinLevel2 = new ServerQuestsAttributes(new DbAttribute("MinLevel2", typeof(string), "0", "2 - MinLevel")) { Description = "Minimum monster level target. If not supplied but MaxLevel defined, MinLevel is 1. Set to 0 to ignore MinLevel on import. (Default: 0)" };
		public static readonly DbAttribute MinLevel3 = new ServerQuestsAttributes(new DbAttribute("MinLevel3", typeof(string), "0", "3 - MinLevel")) { Description = "Minimum monster level target. If not supplied but MaxLevel defined, MinLevel is 1. Set to 0 to ignore MinLevel on import. (Default: 0)" };

		public static readonly DbAttribute MaxLevel1 = new ServerQuestsAttributes(new DbAttribute("MaxLevel1", typeof(string), "0", "1 - MaxLevel")) { Description = "Maximum monster level target. Set to 0 to ignore MaxLevel on import. (Default: 0)" };
		public static readonly DbAttribute MaxLevel2 = new ServerQuestsAttributes(new DbAttribute("MaxLevel2", typeof(string), "0", "2 - MaxLevel")) { Description = "Maximum monster level target. Set to 0 to ignore MaxLevel on import. (Default: 0)" };
		public static readonly DbAttribute MaxLevel3 = new ServerQuestsAttributes(new DbAttribute("MaxLevel3", typeof(string), "0", "3 - MaxLevel")) { Description = "Maximum monster level target. Set to 0 to ignore MaxLevel on import. (Default: 0)" };

		public static readonly DbAttribute Id1 = new ServerQuestsAttributes(new DbAttribute("Id1", typeof(string), "0", "1 - Id")) { Description = "Unique target index for the quest Id. Requires a positive number." };
		public static readonly DbAttribute Id2 = new ServerQuestsAttributes(new DbAttribute("Id2", typeof(string), "0", "2 - Id")) { Description = "Unique target index for the quest Id. Requires a positive number." };
		public static readonly DbAttribute Id3 = new ServerQuestsAttributes(new DbAttribute("Id3", typeof(string), "0", "3 - Id")) { Description = "Unique target index for the quest Id. Requires a positive number." };

		public static readonly DbAttribute Count1 = new ServerQuestsAttributes(new DbAttribute("Count1", typeof(string), "1", "1 - Count")) { Description = "Amount of items that will drop. Non-stackable items default to 1. (Default: 1)" };
		public static readonly DbAttribute Count2 = new ServerQuestsAttributes(new DbAttribute("Count2", typeof(string), "1", "2 - Count")) { Description = "Amount of items that will drop. Non-stackable items default to 1. (Default: 1)" };
		public static readonly DbAttribute Count3 = new ServerQuestsAttributes(new DbAttribute("Count3", typeof(string), "1", "3 - Count")) { Description = "Amount of items that will drop. Non-stackable items default to 1. (Default: 1)" };

		public static readonly DbAttribute TimeLimitNew = new ServerQuestsAttributes(new DbAttribute("TimeLimit", typeof(TimePreviewProperty2<int>), "", "Time limit")) { Description = "The amount of time allowed to finish the quest." };

		private ServerQuestsAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ServerHomunAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Class = new ServerHomunAttributes(new PrimaryAttribute("Class", typeof(int), 0, "Homun ID")) { Description = "Homunculus ID." };
		public static readonly DbAttribute EvoClass = new ServerHomunAttributes(new DbAttribute("EvoClass", typeof(string), "0", "Evolution ID")) { Description = "Homunculus's evolved ID." };
		public static readonly DbAttribute Name = new ServerHomunAttributes(new DbAttribute("Name", typeof(string), "", "Name")) { IsDisplayAttribute = true, Description = "Name of the homunculus." };
		public static readonly DbAttribute FoodID = new ServerHomunAttributes(new DbAttribute("FoodID", typeof(SelectTupleProperty<int>), "0", "Food ID")) { AttachedObject = ServerDbs.Items, Description = "Item ID of the food the homunculus needs." };
		public static readonly DbAttribute HungryDelay = new ServerHomunAttributes(new DbAttribute("HungryDelay", typeof(string), "60", "Hungry delay")) { Description = "Time interval in milliseconds after which the homunculus' hunger value is altered." };
		public static readonly DbAttribute BaseSize = new ServerHomunAttributes(new DbAttribute("BaseSize", typeof(ScaleType), "0", "Base size")) { IsSearchable = false, Description = "Size of the base homunculus class." };
		public static readonly DbAttribute EvoSize = new ServerHomunAttributes(new DbAttribute("EvoSize", typeof(ScaleType), "0", "Evolution size")) { IsSearchable = false, Description = "Size of the evolved homunculus class." };
		public static readonly DbAttribute Race = new ServerHomunAttributes(new DbAttribute("Race", typeof(HomunRaceType), "0")) { IsSearchable = false, Description = "Race of the homunculus." };
		public static readonly DbAttribute Element = new ServerHomunAttributes(new DbAttribute("Element", typeof(MobElementType), "0")) { IsSearchable = false, Description = "Element of the homunculus." };
		public static readonly DbAttribute BAspd = new ServerHomunAttributes(new DbAttribute("BAspd", typeof(string), "0", "Base aspd"));
		public static readonly DbAttribute BHp = new ServerHomunAttributes(new DbAttribute("BHp", typeof(string), "0", "HP")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute BSp = new ServerHomunAttributes(new DbAttribute("BSp", typeof(string), "0", "SP")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute BStr = new ServerHomunAttributes(new DbAttribute("BStr", typeof(string), "0", "Str")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute BAgi = new ServerHomunAttributes(new DbAttribute("BAgi", typeof(string), "0", "Agi")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute BVit = new ServerHomunAttributes(new DbAttribute("BVit", typeof(string), "0", "Vit")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute BInt = new ServerHomunAttributes(new DbAttribute("BInt", typeof(string), "0", "Int")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute BDex = new ServerHomunAttributes(new DbAttribute("BDex", typeof(string), "0", "Dex")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute BLuk = new ServerHomunAttributes(new DbAttribute("BLuk", typeof(string), "0", "Luk")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GnHp = new ServerHomunAttributes(new DbAttribute("GnHp", typeof(string), "0", "Min HP")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GxHp = new ServerHomunAttributes(new DbAttribute("GxHp", typeof(string), "0", "Max HP")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GnSp = new ServerHomunAttributes(new DbAttribute("GnSp", typeof(string), "0", "Min SP")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GxSp = new ServerHomunAttributes(new DbAttribute("GxSp", typeof(string), "0", "Max SP")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GnStr = new ServerHomunAttributes(new DbAttribute("GnStr", typeof(string), "0", "Min Str")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GxStr = new ServerHomunAttributes(new DbAttribute("GxStr", typeof(string), "0", "Max Str")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GnAgi = new ServerHomunAttributes(new DbAttribute("GnAgi", typeof(string), "0", "Min Agi")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GxAgi = new ServerHomunAttributes(new DbAttribute("GxAgi", typeof(string), "0", "Max Agi")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GnVit = new ServerHomunAttributes(new DbAttribute("GnVit", typeof(string), "0", "Min Vit")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GxVit = new ServerHomunAttributes(new DbAttribute("GxVit", typeof(string), "0", "Max Vit")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GnInt = new ServerHomunAttributes(new DbAttribute("GnInt", typeof(string), "0", "Min Int")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GxInt = new ServerHomunAttributes(new DbAttribute("GxInt", typeof(string), "0", "Max Int")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GnDex = new ServerHomunAttributes(new DbAttribute("GnDex", typeof(string), "0", "Min Dex")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GxDex = new ServerHomunAttributes(new DbAttribute("GxDex", typeof(string), "0", "Max Dex")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GnLuk = new ServerHomunAttributes(new DbAttribute("GnLuk", typeof(string), "0", "Min Luk")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute GxLuk = new ServerHomunAttributes(new DbAttribute("GxLuk", typeof(string), "0", "Max Luk")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute EnHp = new ServerHomunAttributes(new DbAttribute("EnHp", typeof(string), "0", "Min HP")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ExHp = new ServerHomunAttributes(new DbAttribute("ExHp", typeof(string), "0", "Max HP")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute EnSp = new ServerHomunAttributes(new DbAttribute("EnSp", typeof(string), "0", "Min SP")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ExSp = new ServerHomunAttributes(new DbAttribute("ExSp", typeof(string), "0", "Max SP")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute EnStr = new ServerHomunAttributes(new DbAttribute("EnStr", typeof(string), "0", "Min Str")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ExStr = new ServerHomunAttributes(new DbAttribute("ExStr", typeof(string), "0", "Max Str")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute EnAgi = new ServerHomunAttributes(new DbAttribute("EnAgi", typeof(string), "0", "Min Agi")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ExAgi = new ServerHomunAttributes(new DbAttribute("ExAgi", typeof(string), "0", "Max Agi")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute EnVit = new ServerHomunAttributes(new DbAttribute("EnVit", typeof(string), "0", "Min Vit")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ExVit = new ServerHomunAttributes(new DbAttribute("ExVit", typeof(string), "0", "Max Vit")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute EnInt = new ServerHomunAttributes(new DbAttribute("EnInt", typeof(string), "0", "Min Int")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ExInt = new ServerHomunAttributes(new DbAttribute("ExInt", typeof(string), "0", "Max Int")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute EnDex = new ServerHomunAttributes(new DbAttribute("EnDex", typeof(string), "0", "Min Dex")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ExDex = new ServerHomunAttributes(new DbAttribute("ExDex", typeof(string), "0", "Max Dex")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute EnLuk = new ServerHomunAttributes(new DbAttribute("EnLuk", typeof(string), "0", "Min Luk")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ExLuk = new ServerHomunAttributes(new DbAttribute("ExLuk", typeof(string), "0", "Max Luk")) { DataConverter = ValueConverters.GetSetZeroString };

		private ServerHomunAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ServerPetAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute MobId = new ServerPetAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Mob ID")); // { AttachedObject = ServerDBs.Mobs };
		public static readonly DbAttribute Name = new ServerPetAttributes(new DbAttribute("Name", typeof(AutoSpritePetProperty<int>), "", "Sprite name"));
		public static readonly DbAttribute JName = new ServerPetAttributes(new DbAttribute("JName", typeof(AutoNamePetProperty<int>), "", "Display name")) { IsDisplayAttribute = true, DataConverter = ValueConverters.GetSetZeroString, Accessor = ValueAccessors.GetDisplayString };
		public static readonly DbAttribute LureId = new ServerPetAttributes(new DbAttribute("LureId", typeof(SelectTupleProperty<int>), "0", "Lure item ID")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute EggId = new ServerPetAttributes(new DbAttribute("EggId", typeof(SelectTupleProperty<int>), "0", "Egg ID")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute EquipId = new ServerPetAttributes(new DbAttribute("EquipId", typeof(SelectTupleProperty<int>), "0", "Equip ID")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute FoodId = new ServerPetAttributes(new DbAttribute("FoodId", typeof(SelectTupleProperty<int>), "0", "Food ID")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute Fullness = new ServerPetAttributes(new DbAttribute("Fullness", typeof(string), "0", "Hunger decrease amount")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute HungryDelay = new ServerPetAttributes(new DbAttribute("HungryDelay", typeof(string), "60", "Hunger decrease delay")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute IntimacyFed = new ServerPetAttributes(new DbAttribute("RHungry", typeof(string), "50", "Intimacy increased\r\nwhen fed")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute IntimacyOverfed = new ServerPetAttributes(new DbAttribute("RFull", typeof(string), "-100", "Intimacy decreased\r\nwhen over-fed")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute IntimacyStart = new ServerPetAttributes(new DbAttribute("Intimate", typeof(string), "250", "Initial intimacy")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute IntimacyOwnerDie = new ServerPetAttributes(new DbAttribute("Die", typeof(string), "-20", "Initial lost after dying")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute CaptureRate = new ServerPetAttributes(new DbAttribute("Capture", typeof(RatePreviewProperty<int>), "0", "Capture success rate")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Speed = new ServerPetAttributes(new DbAttribute("Speed", typeof(string), "0", "Speed")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute SpecialPerformance = new ServerPetAttributes(new DbAttribute("SPerformance", typeof(bool), "1", "Special performance")) { DataConverter = ValueConverters.GetBooleanSetIntString };
		public static readonly DbAttribute DisablePetTalk = new ServerPetAttributes(new DbAttribute("DisablePetTalk", typeof(bool), "0", "Disable pet talk")) { DataConverter = ValueConverters.GetBooleanSetIntString };
		public static readonly DbAttribute AttackRate = new ServerPetAttributes(new DbAttribute("AttackRate", typeof(RatePreviewProperty<int>), "0", "Attack rate")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute RetaliateRate = new ServerPetAttributes(new DbAttribute("DefAttackRate", typeof(RatePreviewProperty<int>), "0", "Defense attack rate")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ChangeTargetRate = new ServerPetAttributes(new DbAttribute("ChangeTargetRate", typeof(RatePreviewProperty<int>), "0", "Change target rate")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute PetScript = new ServerPetAttributes(new DbAttribute("PetScript", typeof(CustomScriptProperty<int>), "{}", "Pet script")) { DataConverter = ValueConverters.GetScriptNoBracketsSetScriptWithBrackets };
		public static readonly DbAttribute LoyalScript = new ServerPetAttributes(new DbAttribute("LoyalScript", typeof(CustomScriptProperty<int>), "{}", "Loyal script")) { DataConverter = ValueConverters.GetScriptNoBracketsSetScriptWithBrackets };

		public static readonly DbAttribute CordialScript = new ServerPetAttributes(new DbAttribute("CordialScript", typeof(CustomScriptProperty<int>), "{}", "Cordial script")) { DataConverter = ValueConverters.GetScriptNoBracketsSetScriptWithBrackets, IsSkippable = true };
		public static readonly DbAttribute NeutralScript = new ServerPetAttributes(new DbAttribute("NeutralScript", typeof(CustomScriptProperty<int>), "{}", "Neutral script")) { DataConverter = ValueConverters.GetScriptNoBracketsSetScriptWithBrackets, IsSkippable = true };
		public static readonly DbAttribute AwkwardScript = new ServerPetAttributes(new DbAttribute("AwkwardScript", typeof(CustomScriptProperty<int>), "{}", "Awkard script")) { DataConverter = ValueConverters.GetScriptNoBracketsSetScriptWithBrackets, IsSkippable = true };

		public static readonly DbAttribute IntimacyHungry = new ServerPetAttributes(new DbAttribute("IntimacyHungry", typeof(string), "-5", "Intimacy increased\r\nwhen pet hungry")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute AllowAutoFeed = new ServerPetAttributes(new DbAttribute("AllowAutoFeed", typeof(bool), "0", "Allow autofeeding")) { DataConverter = ValueConverters.GetBooleanSetIntString };
		public static readonly DbAttribute HungerIncrease = new ServerPetAttributes(new DbAttribute("HungerIncrease", typeof(string), "20", "Hunger increase")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute Evolution = new ServerPetAttributes(new DbAttribute("Evolution", typeof(string), "", "Evolution")) { DataConverter = ValueConverters.GetSetZeroString };

		public static readonly DbAttribute TargetId = new ServerPetAttributes(new DbAttribute("TargetId", typeof(SelectTupleProperty<int>), "0", "Evolution target pet ID")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Pet };
		public static readonly DbAttribute ItemId1 = new ServerPetAttributes(new DbAttribute("ItemId1", typeof(SelectTupleProperty<int>), "0", "Required item ID 1")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute Amount1 = new ServerPetAttributes(new DbAttribute("Amount1", typeof(string), "0", "Required amount for item ID 1")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ItemId2 = new ServerPetAttributes(new DbAttribute("ItemId2", typeof(SelectTupleProperty<int>), "0", "Required item ID 2")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute Amount2 = new ServerPetAttributes(new DbAttribute("Amount2", typeof(string), "0", "Required amount for item ID 2")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ItemId3 = new ServerPetAttributes(new DbAttribute("ItemId3", typeof(SelectTupleProperty<int>), "0", "Required item ID 3")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute Amount3 = new ServerPetAttributes(new DbAttribute("Amount3", typeof(string), "0", "Required amount for item ID 3")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ItemId4 = new ServerPetAttributes(new DbAttribute("ItemId4", typeof(SelectTupleProperty<int>), "0", "Required item ID 4")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute Amount4 = new ServerPetAttributes(new DbAttribute("Amount4", typeof(string), "0", "Required amount for item ID 4")) { DataConverter = ValueConverters.GetSetZeroString };
		public static readonly DbAttribute ItemId5 = new ServerPetAttributes(new DbAttribute("ItemId5", typeof(SelectTupleProperty<int>), "0", "Required item ID 5")) { DataConverter = ValueConverters.GetSetZeroString, AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute Amount5 = new ServerPetAttributes(new DbAttribute("Amount5", typeof(string), "0", "Required amount for item ID 5")) { DataConverter = ValueConverters.GetSetZeroString };

		private ServerPetAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ServerCastleAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ServerCastleAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Castle ID"));
		public static readonly DbAttribute MapName = new ServerCastleAttributes(new DbAttribute("MapName", typeof(string), "", "Map name")) { Description = "The name of the map used by the castle." };
		public static readonly DbAttribute CastleName = new ServerCastleAttributes(new DbAttribute("CastleName", typeof(string), "", "Castle name")) { IsDisplayAttribute = true, Description = "The name of the castle (used by scripts and guardian name tags)." };
		public static readonly DbAttribute NpcName = new ServerCastleAttributes(new DbAttribute("NPC", typeof(string), "", "NPC")) { Description = "NPC unique name to invoke ::OnGuildBreak on, when a occupied castle is abandoned during guild break." };

		private ServerCastleAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ServerConstantsAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ServerConstantsAttributes(new PrimaryAttribute("Id", typeof(string), "", "Constant ID"));
		public static readonly DbAttribute Value = new ServerConstantsAttributes(new DbAttribute("Value", typeof(int), 0)) { DataConverter = ValueConverters.GetSetZeroString, Description = "Value of the constant." };
		public static readonly DbAttribute Type = new ServerConstantsAttributes(new DbAttribute("Type", typeof(ConstantType), "0", "Type")) { DataConverter = ValueConverters.GetIntSetEmptyString, IsSearchable = true, Description = "A parameter variable is attached to the player." };
		public static readonly DbAttribute Deprecated = new ServerConstantsAttributes(new DbAttribute("Deprecated", typeof(bool), "false", "Deprecated")) { DataConverter = ValueConverters.GetBooleanSetTrueFalseString };

		private ServerConstantsAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ViewConstantsAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ViewConstantsAttributes(new PrimaryAttribute("Id", typeof(int), "", "Constant ID"));
		public static readonly DbAttribute Value = new ViewConstantsAttributes(new DbAttribute("Value", typeof(string), ""));

		private ViewConstantsAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ServerCheevoAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute CheevoId = new ServerCheevoAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Cheevo ID")); // { AttachedObject = ServerDBs.Mobs };
		public static readonly DbAttribute Name = new ServerCheevoAttributes(new DbAttribute("Name", typeof(string), "", "Name")) { IsDisplayAttribute = true };
		public static readonly DbAttribute GroupId = new ServerCheevoAttributes(new DbAttribute("GroupId", typeof(string), "0", "Group ID"));
		public static readonly DbAttribute RewardId = new ServerCheevoAttributes(new DbAttribute("RewardId", typeof(SelectTupleProperty<int>), "", "Reward ID")) { AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute RewardAmount = new ServerCheevoAttributes(new DbAttribute("RewardAmount", typeof(string), "", "Amount"));
		public static readonly DbAttribute RewardScript = new ServerCheevoAttributes(new DbAttribute("RewardScript", typeof(CustomScriptProperty<int>), "", "Script"));
		public static readonly DbAttribute RewardTitleId = new ServerCheevoAttributes(new DbAttribute("RewardTitleId", typeof(string), "", "Title ID"));
		public static readonly DbAttribute Condition = new ServerCheevoAttributes(new DbAttribute("Condition", typeof(CustomScriptProperty<int>), "", "Condition"));
		public static readonly DbAttribute Score = new ServerCheevoAttributes(new DbAttribute("Score", typeof(string), "", "Score"));
		public static readonly DbAttribute Dependent = new ServerCheevoAttributes(new DbAttribute("Dependent", typeof(LevelIntEditAnyProperty<int>), "", "Dependencies"));
		public static readonly DbAttribute TargetId1 = new ServerCheevoAttributes(new DbAttribute("TargetId1", typeof(SelectTupleProperty<int>), "", "0 - Target ID")) { AttachedObject = ServerDbs.Mobs };
		public static readonly DbAttribute TargetCount1 = new ServerCheevoAttributes(new DbAttribute("TargetCount1", typeof(string), "", "0 - Amount"));
		public static readonly DbAttribute TargetId2 = new ServerCheevoAttributes(new DbAttribute("TargetId2", typeof(SelectTupleProperty<int>), "", "1 - Target ID")) { AttachedObject = ServerDbs.Mobs };
		public static readonly DbAttribute TargetCount2 = new ServerCheevoAttributes(new DbAttribute("TargetCount2", typeof(string), "", "1 - Amount"));
		public static readonly DbAttribute TargetId3 = new ServerCheevoAttributes(new DbAttribute("TargetId3", typeof(SelectTupleProperty<int>), "", "2 - Target ID")) { AttachedObject = ServerDbs.Mobs };
		public static readonly DbAttribute TargetCount3 = new ServerCheevoAttributes(new DbAttribute("TargetCount3", typeof(string), "", "2 - Amount"));
		public static readonly DbAttribute TargetId4 = new ServerCheevoAttributes(new DbAttribute("TargetId4", typeof(SelectTupleProperty<int>), "", "3 - Target ID")) { AttachedObject = ServerDbs.Mobs };
		public static readonly DbAttribute TargetCount4 = new ServerCheevoAttributes(new DbAttribute("TargetCount4", typeof(string), "", "3 - Amount"));
		public static readonly DbAttribute TargetId5 = new ServerCheevoAttributes(new DbAttribute("TargetId5", typeof(SelectTupleProperty<int>), "", "4 - Target ID")) { AttachedObject = ServerDbs.Mobs };
		public static readonly DbAttribute TargetCount5 = new ServerCheevoAttributes(new DbAttribute("TargetCount5", typeof(string), "", "4 - Amount"));
		public static readonly DbAttribute Map = new ServerCheevoAttributes(new DbAttribute("Map", typeof(string), "", "Map"));

		private ServerCheevoAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}
}