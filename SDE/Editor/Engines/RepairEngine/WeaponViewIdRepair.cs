using System.Collections.Generic;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.LuaEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;

namespace SDE.Editor.Engines.RepairEngine {
	public static class TableRepairs {
		public static void WeaponViewIdRepair(SdeDatabase sdb, BaseDb currentDb) {
			var itemDb = sdb.GetMetaTable<int>(ServerDbs.Items);
			var itemDb1 = sdb.GetDb<int>(ServerDbs.Items);
			var itemDb2 = sdb.GetDb<int>(ServerDbs.Items2);

			string error;
			Dictionary<int, string> dico;

			if (LuaHelper.GetIdToSpriteTable(itemDb2, LuaHelper.ViewIdTypes.Weapon, out dico, out error)) {
			}

			foreach (var tuple in itemDb1.Table.FastItems) {
				if (ItemParser.IsWeaponType(tuple)) {
					int viewId = tuple.GetIntNoThrow(ServerItemAttributes.ClassNumber);
				}
			}
		}
	}

	public class WeaponViewIdRepair : IRepair {
		#region IRepair Members
		public string ImagePath {
			get { return "warning16.png"; }
			set { }
		}

		public string DisplayName { get; set; }

		public bool Show(BaseDb db) {
			return true;
		}

		public bool CanRepair(BaseDb db) {
			return true;
		}

		public bool Repair(BaseDb db) {
			return true;
		}
		#endregion
	}
}