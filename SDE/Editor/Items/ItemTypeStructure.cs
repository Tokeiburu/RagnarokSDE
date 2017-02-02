using System.Collections.Generic;

namespace SDE.Editor.Items {
	public sealed class ItemTypeStructure {
		public static List<ItemTypeStructure> AllTypes = new List<ItemTypeStructure>();

		public static readonly ItemTypeStructure NotSpecified = new ItemTypeStructure("Not specified", new HashSet<string>());

		public static readonly ItemTypeStructure Weapon = new ItemTypeStructure("Weapon", new HashSet<string> {
			"Weapon",
			"2-Handed Huuma Shuriken",
			"Axe",
			"Book",
			"Bow",
			"Claw",
			"Dagger",
			"Gatling Gun",
			"Grenade Launcher",
			"Huuma",
			"Katar",
			"Mace",
			"Musical Instrument",
			"One-handed Axe",
			"One-handed Staff",
			"One-handed Sword",
			"Revolver",
			"Rifle",
			"Rod",
			"Shotgun",
			"Spear",
			"Staff",
			"Sword",
			"Two-handed Axe",
			"Two-handed Staff",
			"Two-handed Sword",
			"Whip"
		});

		public static readonly ItemTypeStructure Card = new ItemTypeStructure("Card", new HashSet<string>());
		public static readonly ItemTypeStructure Headgear = new ItemTypeStructure("Headgear", new HashSet<string>());
		public static readonly ItemTypeStructure MonsterEgg = new ItemTypeStructure("Monster Egg", new HashSet<string>());

		public static readonly ItemTypeStructure Armor = new ItemTypeStructure("Armor", new HashSet<string> {
			"Armor",
			"Accessory",
			"Footgear",
			"Garment",
			"Shield",
			"Shoes",
		});

		public static readonly ItemTypeStructure PetArmor = new ItemTypeStructure("Cute Pet Armor", new HashSet<string>());

		public static readonly ItemTypeStructure Ammunation = new ItemTypeStructure("Ammunation", new HashSet<string> {
			"Arrow",
			"Bullet",
			"Shell",
			"Throwing Dagger",
			"Throwing Weapon",
		});

		public static readonly ItemTypeStructure TamingItem = new ItemTypeStructure("Taming item", new HashSet<string>());

		public HashSet<string> SubItems = new HashSet<string>();
		public string Type { get; set; }

		private ItemTypeStructure(string type, HashSet<string> subItems) {
			SubItems = subItems;
			Type = type;

			AllTypes.Add(this);
		}

		public override string ToString() {
			return Type;
		}
	}
}