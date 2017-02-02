using SDE.ApplicationConfiguration;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;

namespace SDE.Editor.Engines.Parsers {
	/// <summary>
	/// Parser for Hercules's item entries
	/// </summary>
	public class ItemParser {
		public static bool IsArmorType(ReadableTuple<int> tuple) {
			if (SdeAppConfiguration.RevertItemTypes) {
				return tuple.GetValue<TypeType>(ServerItemAttributes.Type) == TypeType.Weapon;
			}

			return tuple.GetValue<TypeType>(ServerItemAttributes.Type) == TypeType.Armor;
		}

		public static bool IsWeaponType(ReadableTuple<int> tuple) {
			if (SdeAppConfiguration.RevertItemTypes) {
				return tuple.GetValue<TypeType>(ServerItemAttributes.Type) == TypeType.Armor;
			}

			return tuple.GetValue<TypeType>(ServerItemAttributes.Type) == TypeType.Weapon;
		}
	}
}