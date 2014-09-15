using Utilities;

namespace SDE.Tools.DatabaseEditor.Generic.Lists {
	public enum ScaleType {
		Small,
		Medium,
		Large
	}

	public enum ConstantType {
		Constant,
		Parameter
	}

	public enum RequiredStateType {
		[Description("None (Nothing special)")]
		None,
		[Description("Move enable (Requires to be able to move)")]
		MoveEnable,
		[Description("Recover weight rate (Requires to be less than 50% weight)")]
		RecoverWeightRate,
		[Description("Water (Requires to be standing on a water cell)")]
		Water,
		[Description("Cart (Requires a Pushcart)")]
		Cart,
		[Description("Riding (Requires to ride a Peco)")]
		Riding,
		[Description("Falcon (Requires a Falcon)")]
		Falcon,
		[Description("Sight (Requires Sight skill activated)")]
		Sight,
		[Description("Hiding (Requires Hiding skill activated)")]
		Hiding,
		[Description("Cloaking (Requires Cloaking skill activated)")]
		Cloaking,
		[Description("Explosion spirits (Requires Fury skill activated)")]
		Explosionspirits,
		[Description("Cartboost (Requires a Pushcart and Cart Boost skill activated)")]
		Cartboost,
		[Description("Shield (Requires a 0,shield equipped)")]
		Shield,
		[Description("Warg (Requires a Warg)")]
		Warg,
		[Description("Ridingwarg (Requires to ride a Warg)")]
		Dragon,
		[Description("Dragon (Requires to ride a Dragon)")]
		Ridingwarg,
		[Description("Mado (Requires to have an active mado)")]
		Mado,
		[Description("Poison Weapon (Requires to be under Poisoning Weapon)")]
		Poisonweapon,
		[Description("Rolling Cutter (Requires at least one Rotation Counter from Rolling Cutter)")]
		RollingCutter,
		[Description("Elemental Spirit (Requires to have an Elemental Spirit summoned)")]
		Elementalspirit,
		[Description("MhFighting (Requires Eleanor fighthing mode)")]
		MhFighting,
		[Description("MhGrappling (Requires Eleanor grappling mode)")]
		MhGrappling,
		[Description("Peco (Requires riding a peco)")]
		Peco
	}

	public enum RequiredStatusesType {

	}

	public enum StateType {
		Any,
		Idle,
		Walk,
		Dead,
		Loot,
		Attack,
		Angry,
		Chase,
		Follow,
		[Description("Any target")]
		Anytarget
	}

	public enum ConditionType {
		Always,
		[Description("On spawn")]
		Onspawn,
		[Description("HP < [CValue] %")]
		Myhpltmaxrate,
		[Description("HP between [CValue] and [Val1]")]
		Myhpinrate,
		[Description("Has [CValue] status on")]
		Mystatuson,
		[Description("Has [CValue] status off")]
		Mystatusoff,
		[Description("Friend HP < [CValue] %")]
		Friendhpltmaxrate,
		[Description("Friend HP between [CValue] % and [Val1]")]
		Friendhpinrate,
		[Description("Friend has [CValue] status on")]
		Friendstatuson,
		[Description("Friend has [CValue] status off")]
		Friendstatusoff,
		[Description("Attack PCs > [CValue]")]
		Attackpcgt,
		[Description("Attack PCs >= [CValue]")]
		Attackpcge,
		[Description("Num of slaves < [CValue]")]
		Slavelt,
		[Description("Num of slaves <= [CValue]")]
		Slavele,
		[Description("When close range melee attacked")]
		Closedattacked,
		[Description("When long range melee attacked")]
		Longrangeattacked,
		[Description("Skill [CValue] used on mob")]
		Skillused,
		[Description("After skill [CValue] has been used")]
		Afterskill,
		[Description("Player is in range")]
		Casttargeted,
		[Description("Rude attacked")]
		Rudeattacked
	}

	public enum TargetType {
		Target,
		Self,
		Friend,
		Master,
		[Description("Random target")]
		RandomTarget
	}

	public enum MobRaceType {
		Formless,
		Undead,
		Brute,
		Plant,
		Insect,
		Fish,
		Demon,
		[Description("Demi Human")]
		DemiHuman,
		Angel,
		Dragon,
		Boss,
		[Description("Non Boss")]
		NonBoss,
		[Description("New Item")]
		NewItem,
		[Description("Non Demi Human")]
		NonDemiHuman,
	}

	public enum TypeType {
		[Description("Healing item")]
		HealingItem = 0,
		[Description("Usable item")]
		UsableItem = 2,
		[Description("Misc item")]
		EtcItem = 3,
		Weapon = 4,
		Armor = 5,
		Card = 6,
		[Description("Pet egg")]
		PetEgg = 7,
		[Description("Pet equipment")]
		PetEquip = 8,
		[Description("Arrow and ammunition")]
		Ammo = 10,
		[Description("Usable with delayed consumption")]
		UsableWithDelayed = 11,
		[Description("Shadow equipment")]
		ShadowEquip = 12,
		[Description("Usable with delayed consumption2")]
		UsableWithDelayed2 = 18,
	}

	public enum GenderType {
		Female,
		Male,
		Both
	}

	public enum MobElementType {
		Neutral,
		Water,
		Earth,
		Fire,
		Wind,
		Poison,
		Holy,
		Dark,
		Ghost,
		Undead
	}

	public enum HomunRaceType {
		Formless,
		Undead,
		Brute,
		Plant,
		Insect,
		Fish,
		Demon,
		[Description("Demi Human")]
		DemiHuman,
		Angel,
		Dragon
	}

	public enum HitType {
		None = 0,
		[Description("Single hit")]
		SingleHit = 6,
		[Description("Repeated hit")]
		RepeatedHit = 8
	}

	public enum AttackTypeType {
		None,
		Weapon,
		Magic,
		Misc
	}

	public enum SkillElementType {
		Neutral,
		Water,
		Earth,
		Fire,
		Wind,
		Poison,
		Holy,
		Dark,
		Ghost,
		Undead,
		[Description("Use weapon element")]
		UseWeaponElement = -1,
		[Description("Use endowed element")]
		UseEndowedElement = -2,
		[Description("Use random element")]
		UseRandomElement = -3
	}
}
