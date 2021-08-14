using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Utilities;

namespace SDE.Editor.Generic.Lists {
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
		[Description("None (Nothing special)")] None,
		[Description("Move enable (Requires to be able to move)")] MoveEnable,
		[Description("Recover weight rate (Requires to be less than 50% weight)")] RecoverWeightRate,
		[Description("Water (Requires to be standing on a water cell)")] Water,
		[Description("Cart (Requires a Pushcart)")] Cart,
		[Description("Riding (Requires to ride a Peco)")] Riding,
		[Description("Falcon (Requires a Falcon)")] Falcon,
		[Description("Sight (Requires Sight skill activated)")] Sight,
		[Description("Hiding (Requires Hiding skill activated)")] Hiding,
		[Description("Cloaking (Requires Cloaking skill activated)")] Cloaking,
		[Description("Explosion spirits (Requires Fury skill activated)")] Explosionspirits,
		[Description("Cartboost (Requires a Pushcart and Cart Boost skill activated)")] Cartboost,
		[Description("Shield (Requires a 0,shield equipped)")] Shield,
		[Description("Warg (Requires a Warg)")] Warg,
		[Description("Ridingwarg (Requires to ride a Warg)")] Dragon,
		[Description("Dragon (Requires to ride a Dragon)")] Ridingwarg,
		[Description("Mado (Requires to have an active mado)")] Mado,
		[Description("Poison Weapon (Requires to be under Poisoning Weapon)")] Poisonweapon,
		[Description("Rolling Cutter (Requires at least one Rotation Counter from Rolling Cutter)")] RollingCutter,
		[Description("Elemental Spirit (Requires to have an Elemental Spirit summoned)")] Elementalspirit,
		[Description("MhFighting (Requires Eleanor fighthing mode)")] MhFighting,
		[Description("MhGrappling (Requires Eleanor grappling mode)")] MhGrappling,
		[Description("Peco (Requires riding a peco)")] Peco
	}

	public enum RequiredStateTypeNew {
		[Description("None (Nothing special)")] None,
		[Description("Hidden (Requires to be hidden)")] Hidden,
		[Description("Riding (Requires a mount)")] Riding,
		[Description("Falcon (Requires a Falcon)")] Falcon,
		[Description("Cart (Requires a Pushcart)")] Cart,
		[Description("Shield (Requires a shield equipped)")] Shield,
		[Description("Recover Weight Rate (Requires to be less than 70% weight)")] RecoverWeightRate,
		[Description("Move enable (Requires to be able to move)")] MoveEnable,
		[Description("Water (Requires to be standing on a water cell)")] Water,
		[Description("Riding Dragon (Requires to ride a Warg)")] RidingDragon,
		[Description("Warg (Requires a Warg)")] Warg,
		[Description("Dragon Warg (Requires to ride a Dragon)")] Ridingwarg,
		[Description("Mado (Requires to have an active mado)")] Mado,
		[Description("Elemental Spirit (Requires to have an Elemental Spirit summoned)")] Elementalspirit,
		[Description("Elemental Spirit2")] Elementalspirit2,
		[Description("Dragon Warg (Requires to ride a Peco)")] RidingPeco,
		[Description("Sun Stance (Requires Sun Stance active)")] SunStance,
		[Description("Moon Stance (Requires Moon Stance active)")] MoonStance,
		[Description("Stars Stance (Requires Stars Stance active)")] StarsStance,
		[Description("Universe Stance (Requires Stars Stance active)")] UniverseStance,
	}

	public enum UnitTargetType {
		None = 0,
		Self = 0x010000,
		Enemy = 0x020000,
		Party = 0x040000,
		GuildAlly = 0x080000,
		Neutral = 0x100000,
		SameGuild = 0x200000,
		All = 0x3F0000,
		WoS = 0x400000,
		Guild = SameGuild | GuildAlly,
		NoGuild = All & ~Guild,
		NoParty = All & ~Party,
		NoEnemy = All & ~Enemy,
		Ally = Party | Guild,
		Friend = NoEnemy,
	}

	public enum UnitFlagType {
		//[Description("NoEnemy#If 'defunit_not_enemy' is set, the target is changed to 'friend'.")] NoEnemy = 0x1,
		//[Description("NoReiteration#Spell cannot be stacked.")] NoReiteration = 0x2,
		//[Description("NoFootStep#Spell cannot be cast near/on targets.")] NoFootStep = 0x4,
		//[Description("NoOverlap#Spell effects do not overlap.")] NoOverlap = 0x8,
		//[Description("PathCheck#Spell effects do not overlap.")] PathCheck = 0x10,
		//[Description("NoPc#May not target players.")] NoPc = 0x20,
		//[Description("NoMob#May not target mobs.")] NoMob = 0x40,
		//[Description("Skill#May not target skill.")] Skill = 0x80,
		//[Description("Dance")] Dance = 0x100,
		//[Description("Ensemble")] Ensemble = 0x200,
		//[Description("Song")] Song = 0x400,
		//[Description("DualMode#Spells should trigger both ontimer and onplace/onout/onleft effects.")] DualMode = 0x800,
		//[Description("NoKnockback#Skill unit cannot be knocked back.")] NoKnockback = 0x1000,
		//[Description("RangedSingleUnit#Hack for ranged layout, only display center.")] RangedSingleUnit = 0x2000,
		//[Description("CrazyWeedImmune#Immune to Crazy Weed removal.")] CrazyWeedImmune = 0x4000,
		//[Description("RemovedByFireRain#Removed by Fire Rain.")] RemovedByFireRain = 0x8000,
		//[Description("KnockbackGroup#Removed by Fire Rain.")] KnockbackGroup = 0x10000,
		//[Description("HiddenTrap#Hidden trap.")] HiddenTrap = 0x20000,
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
		[Description("Any target")] Anytarget
	}

	public enum ConditionType {
		Always,
		[Description("On spawn")] Onspawn,
		[Description("HP < [CValue] %")] Myhpltmaxrate,
		[Description("HP between [CValue] and [Val1]")] Myhpinrate,
		[Description("Has [CValue] status on")] Mystatuson,
		[Description("Has [CValue] status off")] Mystatusoff,
		[Description("Friend HP < [CValue] %")] Friendhpltmaxrate,
		[Description("Friend HP between [CValue] % and [Val1]")] Friendhpinrate,
		[Description("Friend has [CValue] status on")] Friendstatuson,
		[Description("Friend has [CValue] status off")] Friendstatusoff,
		[Description("Attack PCs > [CValue]")] Attackpcgt,
		[Description("Attack PCs >= [CValue]")] Attackpcge,
		[Description("Num of slaves < [CValue]")] Slavelt,
		[Description("Num of slaves <= [CValue]")] Slavele,
		[Description("When close range melee attacked")] Closedattacked,
		[Description("When long range melee attacked")] Longrangeattacked,
		[Description("Skill [CValue] used on mob")] Skillused,
		[Description("After skill [CValue] has been used")] Afterskill,
		[Description("Player is in range")] Casttargeted,
		[Description("Rude attacked")] Rudeattacked
	}

	public enum TargetType {
		Target,
		Self,
		Friend,
		Master,
		[Description("Random target")] RandomTarget,
		[Description("3x3 area around self")] Around1,
		[Description("5x5 area around self")] Around2,
		[Description("7x7 area around self")] Around3,
		[Description("9x9 area around self")] Around4,
		[Description("3x3 area around target")] Around5,
		[Description("5x5 area around target")] Around6,
		[Description("7x7 area around target")] Around7,
		[Description("9x9 area around target")] Around8,
		[Description("3x3 area around target (again?)")] Around,
		//[Description("Non-Top-Hate")] nontophate,
		//[Description("1st Hate")] tophate,
		//[Description("2nd Hate")] secondhate,
		//[Description("Last Hate")] lasthate,
		//[Description("Tank")] tank,
		//[Description("Dps")] dps,
		//[Description("Healer")] healer,
		//[Description("Support")] support,
		//[Description("Non-Tank")] nontank,
		//[Description("Non-Dps")] nondps,
		//[Description("Non-Healer")] nonhealer,
		//[Description("Non-Support")] nonsupport,
	}

	[Description("Special group edit")]
	public enum MobGroup2Type {
	}

	public enum MobRaceType {
		Formless,
		Undead,
		Brute,
		Plant,
		Insect,
		Fish,
		Demon,
		[Description("Demi Human")] DemiHuman,
		Angel,
		Dragon,
		//Boss,
		//[Description("Non Boss")] NonBoss,
		//[Description("New Item")] NewItem,
		//[Description("Non Demi Human")] NonDemiHuman,
	}

	public enum QuestRaceType {
		All,
		Formless,
		Undead,
		Brute,
		Plant,
		Insect,
		Fish,
		Demon,
		[Description("Demi Human")] DemiHuman,
		Angel,
		Dragon
	}

	public enum QuestSizeType {
		All,
		Small,
		Medium,
		Large
	}

	public enum QuestElementType {
		All,
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

	public enum TypeType {
		[Description("Healing item")] HealingItem = 0,
		[Description("Usable item")] UsableItem = 2,
		[Description("Misc item")] EtcItem = 3,
		[Description("Armor#Weapon")] Armor = 4,
		[Description("Weapon#Armor")] Weapon = 5,
		Card = 6,
		[Description("Pet egg")] PetEgg = 7,
		[Description("Pet equipment")] PetEquip = 8,
		[Description("Arrow and ammunition")] Ammo = 10,
		[Description("Usable with delayed consumption")] UsableWithDelayed = 11,
		[Description("Shadow equipment")] ShadowEquip = 12,
		[Description("Cash")] UsableWithDelayed2 = 18,
	}

	public enum GenderType {
		Female,
		Male,
		Both,
		[Description("[Undefined]")] Undefined
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
		[Description("Demi Human")] DemiHuman,
		Angel,
		Dragon
	}

	public enum HitType {
		Normal = 0,
		[Description("Pickup item")] PickupItem = 1,
		[Description("Sit down")] SitDown = 2,
		[Description("Stand up")] StandUp = 3,
		Endure = 4,
		Splash = 5,
		[Description("Single hit")] SingleHit = 6,
		Repeat = 7,
		[Description("Multi-hit")] MultiHit = 8,
		[Description("Multi-hit endure")] MultiHitEndure = 9,
		Critical = 10,
		[Description("Lucky dodge")] LuckyDodge = 11,
		Touch = 12,
		[Description("Multi-hit critical")] MultiHitCritical = 13,
	}

	public enum SkillTargetType {
		Passive = 0x0,
		Attack = 0x1,
		Ground = 0x2,
		Self = 0x4,
		Support = 0x10,
		Trap = 0x20,
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
		[Description("Use weapon element")] UseWeaponElement = -1,
		[Description("Use endowed element")] UseEndowedElement = -2,
		[Description("Use random element")] UseRandomElement = -3
	}

	[Flags]
	[Description("Skill damage edit")]
	public enum SkillDamageType {
		//[Description("NoDamage#No damage skill.")] NoSkillDamage = 1 << 0,
		//[Description("Splash#Has splash area#Has splash area (requires source modification).")] SplashDamage = 1 << 1,
		//[Description("SplashSplit#Damage should be split among targets#Damage should be split among targets (requires 0x02 in order to work).")] SplitAmongTargets = 1 << 2,
		//[Description("IgnoreAtkCard#Skill ignores caster's % damage cards (misc type always ignores)#Skill ignores caster's % damage cards (misc type always ignores).")] IgnoresCasterDamage = 1 << 3,
		//[Description("IgnoreElement#Skill ignores elemental adjustments.")] IgnoresElementalAdjusments = 1 << 4,
		//[Description("IgnoreDefense#Skill ignores target's defense (misc type always ignores).")] IgnoresTargetDefense = 1 << 5,
		//[Description("IgnoreFlee#Skill ignores target's flee (magic type always ignores).")] IgnoresTargetFlee = 1 << 6,
		//[Description("IgnoreDefCard#Skill ignores target's def cards.")] IgnoresTargetDefCards = 1 << 7,
		//[Description("Critical#Skill can crit.")] Critical = 1 << 8,
		//[Description("IgnoreLongCard#Ignore long range card effects.")] IgnoreLongCard = 1 << 9,
	}

	[Flags]
	[Description("Item flag edit")]
	public enum ItemFlagType {
		//[Description("BuyingStore#If the item is available for Buyingstores. (Default: false)")] BuyingStore = 1 << 0,
		//[Description("DeadBranch#If the item is a Dead Branch. (Default: false)")] DeadBranch = 1 << 1,
		//[Description("Container#If the item is part of a container. (Default: false)")] Container = 1 << 2,
		//[Description("UniqueId#If the item is a unique stack. (Default: false)")] UniqueId = 1 << 3,
		//[Description("BindOnEquip#If the item is bound to the character upon equipping. (Default: false)")] BindOnEquip = 1 << 4,
		//[Description("DropAnnounce#If the item has a special announcement to self on drop. (Default: false)")] DropAnnounce = 1 << 5,
		//[Description("NoConsume#If the item is consumed on use. (Default: false)")] NoConsume = 1 << 6,
		//[Description("DropEffect#If the item has a special effect when on the ground. (Default: None)")] DropEffect = 1 << 7,
	}

	[Flags]
	[Description("Item custom flag edit")]
	public enum ItemCustomFlagType {
	}

	[Description("Item custom flag edit")]
	public enum SizeType {
	}

	[Description("Item custom flag edit")]
	public enum ClassType {
		Normal,
		Boss,
		Guardian,
		Battlefield,
		Event
	}

	[Description("Item custom flag edit")]
	public enum RaceType {
	}

	[Flags]
	[Description("Item MH flag edit")]
	public enum ItemMHFlagType {
	}

	[Flags]
	[Description("Stack flag edit")]
	public enum ItemStackFlagType {
		[Description("Inventory#If the stack is applied to player's inventory. (Default: true)")] Inventory = 1 << 0,
		[Description("Cart#If the stack is applied to the player's cart. (Default: false)")] Cart = 1 << 1,
		[Description("Storage#If the stack is applied to the player's storage. (Default: false)")] Storage = 1 << 2,
		[Description("GuildStorage#If the stack is applied to player's guild storage. (Default: true)")] GuildStorage = 1 << 3,
	}

	[Description("Skill type3 edit#disable_tooltips")]
	public enum SkillType3Type {
		[Description("Skill ignores land protector (e.g. arrow shower).")] En0 = 1 << 0,
		[Description("Spell that doesn't end camouflage.")] En1 = 1 << 1,
		[Description("Usable skills while hiding.")] En2 = 1 << 2,
		[Description("Spell that can be use while in dancing state.")] En3 = 1 << 3,
		[Description("Spell that could hit emperium.")] En4 = 1 << 4,
		[Description("Spell blocked by statis.")] En5 = 1 << 5,
		[Description("Spell blocked by kagehumi.")] En6 = 1 << 6,
		[Description("Spell range affected by AC_VULTURE.")] En7 = 1 << 7,
		[Description("Spell range affected by GS_SNAKEEYE.")] En8 = 1 << 8,
		[Description("Spell range affected by NJ_SHADOWJUMP.")] En9 = 1 << 9,
		[Description("Spell range affected by WL_RADIUS.")] En10 = 1 << 10,
		[Description("Spell range affected by RA_RESEARCHTRAP.")] En11 = 1 << 11,
		[Description("Spell that does not affect user that has NC_HOVERING active.")] En12 = 1 << 12,
		[Description("Spell that can be using while riding warg.")] En13 = 1 << 13,
		[Description("Spell that can't be used while in mado.")] En14 = 1 << 14,
	}

	[Description("Cast edit")]
	public enum CastingFlags {
		//[Description("IgnoreDex#Not affected by dex.")] En0 = 1 << 0,
		//[Description("IgnoreStatus#Not affected by statuses (Suffragium, etc).")] En1 = 1 << 1,
		//[Description("IgnoreItemBonus#Not affected by item bonuses (equips, cards).")] En2 = 1 << 2,
	}
	
	[Description("No near NPC edit")]
	public enum NoNearNpcType {
		[Description("WarpPortal")] En0 = 1 << 0,
		[Description("Shop")] En1 = 1 << 1,
		[Description("Npc")] En2 = 1 << 2,
		[Description("Tomb")] En3 = 1 << 3,
	}

	[Description("Weapon edit")]
	public enum WeaponType {
		//[Description("Fist")] En0 = 1 << 0,
		//[Description("Dagger")] En1 = 1 << 1,
		//[Description("1hSword")] En2 = 1 << 2,
		//[Description("2hSword")] En3 = 1 << 3,
		//[Description("1hSpear")] En4 = 1 << 4,
		//[Description("2hSpear")] En5 = 1 << 5,
		//[Description("1hAxe")] En6 = 1 << 6,
		//[Description("2hAxe")] En7 = 1 << 7,
		//[Description("Mace")] En8 = 1 << 8,
		//[Description("2hMace")] En9 = 1 << 9,
		//[Description("Staff")] En10 = 1 << 10,
		//[Description("Bow")] En11 = 1 << 11,
		//[Description("Knuckle")] En12 = 1 << 12,
		//[Description("Musical")] En13 = 1 << 13,
		//[Description("Whip")] En14 = 1 << 14,
		//[Description("Book")] En15 = 1 << 15,
		//[Description("Katar")] En16 = 1 << 16,
		//[Description("Revolver")] En17 = 1 << 17,
		//[Description("Rifle")] En18 = 1 << 18,
		//[Description("Gatling")] En19 = 1 << 19,
		//[Description("Shotgun")] En20 = 1 << 20,
		//[Description("Grenade")] En21 = 1 << 21,
		//[Description("Huuma")] En22 = 1 << 22,
		//[Description("2hStaff")] En23 = 1 << 23,
	}

	[Description("Ammo edit")]
	public enum AmmoType {
		//[Description("Arrow")] En0 = 1 << 0,
		//[Description("Dagger")] En1 = 1 << 1,
		//[Description("Bullet")] En2 = 1 << 2,
		//[Description("Shell")] En3 = 1 << 3,
		//[Description("Grenade")] En4 = 1 << 4,
		//[Description("Shuriken")] En5 = 1 << 5,
		//[Description("Kunai")] En6 = 1 << 6,
		//[Description("Cannonball")] En7 = 1 << 7,
		//[Description("Throwweapon")] En8 = 1 << 8,
	}

	[Description("Skill type2 edit#disable_tooltips")]
	public enum SkillType2Type : long {
		[Description("Quest skill")] En0 = 1 << 0,
		[Description("Npc skill")] En1 = 1 << 1,
		[Description("Wedding skill")] En2 = 1 << 2,
		[Description("Spirit skill")] En3 = 1 << 3,
		[Description("Guild skill")] En4 = 1 << 4,
		[Description("Song/dance")] En5 = 1 << 5,
		[Description("Ensemble skill")] En6 = 1 << 6,
		[Description("Trap")] En7 = 1 << 7,
		[Description("Skill that damages/targets yourself")] En8 = 1 << 8,
		[Description("Cannot be casted on self (if inf = 4, auto-select target skill)")] En9 = 1 << 9,
		[Description("Usable only on party-members (and enemies if skill is offensive)")] En10 = 1 << 10,
		[Description("Usable only on guild-mates (and enemies if skill is offensive)")] En11 = 1 << 11,
		[Description("Disable usage on enemies (for non-offensive skills).")] En12 = 1 << 12,
		[Description("Skill ignores land protector (e.g. arrow shower)")] En13 = 1 << 13,
		[Description("Chorus skill")] En14 = 1 << 14,
	}

	[Description("Skill type2 edit")]
	public enum SkillType2TypeNew {
	}

	[Description("Trade edit")]
	public enum TradeFlag {
	}
	
	[Description("Skill type edit#disable_tooltips")]
	public enum SkillType1Type {
		[Description("1 - Enemy")] En0 = 1 << 0,
		[Description("2 - Place")] En1 = 1 << 1,
		[Description("4 - Self")] En2 = 1 << 2,
		[Description("8 - $Undefined")] En3 = 1 << 3,
		[Description("16 - Friend")] En4 = 1 << 4,
		[Description("32 - Trap")] En5 = 1 << 5,
	}

	[Flags]
	[Description("Upper edit#disable_tooltips")]
	public enum UpperType {
		[Description("1 - Normal")] En0 = 1 << 0,
		[Description("2 - Reborn/Trans. Classes (excl. Trans-3rd classes)")] En1 = 1 << 1,
		[Description("4 - Baby Classes (excl. 3rd Baby Classes)")] En2 = 1 << 2,
		[Description("8 - 3rd Classes (excl. Trans-3rd classes and 3rd Baby classes)")] En3 = 1 << 3,
		[Description("16 - Trans-3rd Classes")] En4 = 1 << 4,
		[Description("32 - Baby 3rd Classes")] En5 = 1 << 5,
	}

	[Flags]
	[Description("Mode edit")]
	public enum NewMobModeType {
	}

	[Description("Mode edit")]
	public enum MobModeType {
		[Description("Can move#Enables the mob to move/chase characters.")] En0 = 1 << 0,
		[Description("Looter#The mob will loot up nearby items on the ground when it's on idle state.")] En1 = 1 << 1,
		[Description("Aggressive#Normal aggressive mob, will look for a close-by player to attack.")] En2 = 1 << 2,
		[Description("Assist#When a nearby mob of the same class attacks, assist types will join them.")] En3 = 1 << 3,
		[Description("Cast sensor#Will go after characters who start casting on them if idle or walking (without a target).")] En4 = 1 << 4,
		[Description("Boss#Special flag which makes mobs immune to certain status changes and skills.")] En5 = 1 << 5,
		[Description("Plant#Always receives 1 damage from attacks.")] En6 = 1 << 6,
		[Description("Can attack#Enables the mob to attack/retaliate when you are within attack range. Note that this only enables them to use normal attacks, skills are always allowed.")] En7 = 1 << 7,
		[Description("Detector#Enables mob to detect and attack characters who are in hiding/cloak.")] En8 = 1 << 8,
		[Description("Change target#Will go after characters who start casting on them if idle or chasing other players (they switch chase targets).")] En9 = 1 << 9,
		[Description("Change chase#Allows chasing mobs to switch targets if another player happens to be within attack range (handy on ranged attackers, for example).")] En10 = 1 << 10,
		[Description("Angry#These mobs are 'hyper-active'. Apart from 'chase'/'attack', they have the states 'follow'/'angry'. Once hit, they stop using these states and use the normal ones. The new states are used to determine a different skill-set for their 'before attacked' and 'after attacked' states. Also, when 'following', they automatically switch to whoever character is closest.")] En11 = 1 << 11,
		[Description("Change target melee#Enables a mob to switch targets when attacked while attacking someone else.")] En12 = 1 << 12,
		[Description("Change target chase#Enables a mob to switch targets when attacked while chasing another character.")] En13 = 1 << 13,
		[Description("Target weak#Allows aggressive monsters to only be aggressive against  characters that are five levels below it's own level. For example, a monster of level 104 will not pick fights with a level 99.")] En14 = 1 << 14,
		[Description("Random target#Picks a new random target in range on each attack / skill.")] En15 = 1 << 15,
		[Description("Ignore melee#The mob will take 1 HP damage from physical attacks.#rAthena")] En16 = 1 << 16,
		[Description("Ignore magic#The mob will take 1 HP damage from magic attacks.#rAthena")] En17 = 1 << 17,
		[Description("Ignore ranged#The mob will take 1 HP damage from ranged attacks.#rAthena")] En18 = 1 << 18,
		[Description("MVP#Flagged as MVP which makes mobs resistance to Coma.#rAthena")] En19 = 1 << 19,
		[Description("Ignore misc#The mob will take 1 HP damage from 'none' attack type.#rAthena")] En20 = 1 << 20,
		[Description("Knockback immune#The mob will be unable to be knocked back.#rAthena")] En21 = 1 << 21,
		[Description("No random walk#The mob will not walk randomly.#rAthena")] En22 = 1 << 22,
		[Description("No cast skill#The mob will not cast skills.#rAthena")] En23 = 1 << 23,
	}

	[Description("Mode edit")]
	public enum MobModeTypeNew {
		[Description("Can move#Enables the mob to move/chase characters.")] En0 = 1 << 0,
		[Description("Looter#The mob will loot up nearby items on the ground when it's on idle state.")] En1 = 1 << 1,
		[Description("Aggressive#Normal aggressive mob, will look for a close-by player to attack.")] En2 = 1 << 2,
		[Description("Assist#When a nearby mob of the same class attacks, assist types will join them.")] En3 = 1 << 3,
		[Description("Cast sensor#Will go after characters who start casting on them if idle or walking (without a target).")] En4 = 1 << 4, // MD_CASTSENSOR_IDLE
		[Description("No random walk#The mob will not walk randomly.#rAthena")] En5 = 1 << 5,
		[Description("No cast skill#The mob will not cast skills.#rAthena")] En6 = 1 << 6,
		[Description("Can attack#Enables the mob to attack/retaliate when you are within attack range. Note that this only enables them to use normal attacks, skills are always allowed.")] En7 = 1 << 7,
		// 8 = 256, free
		[Description("Change target#Will go after characters who start casting on them if idle or chasing other players (they switch chase targets).")] En9 = 1 << 9,
		[Description("Change chase#Allows chasing mobs to switch targets if another player happens to be within attack range (handy on ranged attackers, for example).")] En10 = 1 << 10,
		[Description("Angry#These mobs are 'hyper-active'. Apart from 'chase'/'attack', they have the states 'follow'/'angry'. Once hit, they stop using these states and use the normal ones. The new states are used to determine a different skill-set for their 'before attacked' and 'after attacked' states. Also, when 'following', they automatically switch to whoever character is closest.")] En11 = 1 << 11,
		[Description("Change target melee#Enables a mob to switch targets when attacked while attacking someone else.")] En12 = 1 << 12,
		[Description("Change target chase#Enables a mob to switch targets when attacked while chasing another character.")] En13 = 1 << 13,
		[Description("Target weak#Allows aggressive monsters to only be aggressive against  characters that are five levels below it's own level. For example, a monster of level 104 will not pick fights with a level 99.")] En14 = 1 << 14,
		[Description("Random target#Picks a new random target in range on each attack / skill.")] En15 = 1 << 15,
		[Description("Ignore melee#The mob will take 1 HP damage from physical attacks.#rAthena")] En16 = 1 << 16,
		[Description("Ignore magic#The mob will take 1 HP damage from magic attacks.#rAthena")] En17 = 1 << 17,
		[Description("Ignore ranged#The mob will take 1 HP damage from ranged attacks.#rAthena")] En18 = 1 << 18,
		[Description("MVP#Flagged as MVP which makes mobs resistance to Coma.#rAthena")] En19 = 1 << 19,
		[Description("Ignore misc#The mob will take 1 HP damage from 'none' attack type.#rAthena")] En20 = 1 << 20,
		[Description("Knockback immune#The mob will be unable to be knocked back.#rAthena")] En21 = 1 << 21,
		[Description("No teleport block#Allows the monster to teleport on noteleport maps.#rAthena")] En22 = 1 << 22,
		// 23 = , free
		[Description("Fixed item drop#The mob's drops are not affected by item drop modifiers.")] En24 = 1 << 24,
		[Description("Detector#Enables mob to detect and attack characters who are in hiding/cloak.")] En25 = 1 << 25,
		[Description("Status immune#Immune to being affected by statuses.#rAthena")] En26 = 1 << 26,
		[Description("Skill immune#Immune to being affected by skills.#rAthena")] En27 = 1 << 27,
	}

	[Description("Location edit#disable_tooltips#order:8,9,0,4,1,5,2,6,3,7,10")]
	public enum LocationType {
		[Description("Lower headgear")] En0 = 1 << 0,
		[Description("Weapon")] En1 = 1 << 1,
		[Description("Garment")] En2 = 1 << 2,
		[Description("Accessory right")] En3 = 1 << 3,
		[Description("Armor")] En4 = 1 << 4,
		[Description("Shield")] En5 = 1 << 5,
		[Description("Shoes (footgear)")] En6 = 1 << 6,
		[Description("Accessory left")] En7 = 1 << 7,
		[Description("Upper headgear")] En8 = 1 << 8,
		[Description("Middle headgear")] En9 = 1 << 9,
		[Description("Costume upper headgear")] En10 = 1 << 10,
		[Description("Costume middle headgear")] En11 = 1 << 11,
		[Description("Costume lower headgear")] En12 = 1 << 12,
		[Description("Costume garment/robe")] En13 = 1 << 13,
		[Description("Ammo")] En15 = 1 << 15,
		[Description("Shadow armor")] En16 = 1 << 16,
		[Description("Shadow weapon")] En17 = 1 << 17,
		[Description("Shadow shield")] En18 = 1 << 18,
		[Description("Shadow shoes")] En19 = 1 << 19,
		[Description("Shadow accessory right (earring)")] En20 = 1 << 20,
		[Description("Shadow accessory left (pendant)")] En21 = 1 << 21,
	}

	[Description("Map restriction edit#disable_tooltips#max_col_width:500")]
	public enum MapRestrictionType {
		[Description("Block in normal maps")] En0 = 1 << 0,
		[Description("Block in PvP maps (use this instead of 1 for PK-mode servers)")] En1 = 1 << 1,
		[Description("Block in GvG maps")] En2 = 1 << 2,
		[Description("Block in Battleground maps")] En3 = 1 << 3,
		[Description("Cannot be cloned (clones will not copy this skill)")] En4 = 1 << 4,
		[Description("Block in zone 1 maps (Aldebaran Turbo Track)")] En5 = 1 << 5,
		[Description("Block in zone 2 maps (Jail)")] En6 = 1 << 6,
		[Description("Block in zone 3 maps (Izlude Battle Arena)")] En7 = 1 << 7,
		[Description("Block in zone 4 maps (WoE:SE)")] En8 = 1 << 8,
		[Description("Block in zone 5 maps (Sealed Shrine)")] En9 = 1 << 9,
		[Description("Block in zone 6 maps (Endless Tower, Orc's Memory, Nidhoggur's Nest)")] En10 = 1 << 10,
		[Description("Block in zone 7 maps (Towns)")] En11 = 1 << 11,
	}

	public enum DropEffectType {
		None,
		Client,
		[Description("White Pillar")] White_Pillar,
		[Description("Blue Pillar")] Blue_Pillar,
		[Description("Yellow Pillar")] Yellow_Pillar,
		[Description("Purple Pillar")] Purple_Pillar,
		[Description("Orange Pillar")] Orange_Pillar,
		[Description("Green Pillar")] Green_Pillar,
		[Description("Red Pillar")] Red_Pillar,
		//[Description("Reproduce")] Reproduce = 1 << 1,
	}
	
	[Description("Copy skill edit#disable_tooltips")]
	public enum SkillCopyType {
		//[Description("Plagiarism")] Plagiarism = 1 << 0,
		//[Description("Reproduce")] Reproduce = 1 << 1,
	}
	
	[Description("Copy skill remove requirement edit#disable_tooltips")]
	public enum SkillCopyRemoveRequirementType {
		HpCost = 1 << 0,
		SpCost = 1 << 1,
		HpRateCost = 1 << 2,
		SpRateCost = 1 << 3,
		MaxHpTrigger = 1 << 4,
		ZenyCost = 1 << 5,
		Weapon = 1 << 6,
		Ammo = 1 << 7,
		State = 1 << 8,
		Status = 1 << 9,
		SpiritSphereCost = 1 << 10,
		ItemCost = 1 << 11,
		Equipment = 1 << 12,
	}
}