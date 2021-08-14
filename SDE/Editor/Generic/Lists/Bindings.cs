using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Database;
using SDE.Editor.Generic.Core;
using SDE.View;

namespace SDE.Editor.Generic.Lists {
	/// Bindings are normally used to change the display of the list view
	public class ComboBinding : IBinding {
		#region IBinding Members
		public Database.Tuple Tuple { get; set; }
		public DbAttribute AttachedAttribute { get; set; }
		#endregion

		public override string ToString() {
			if (AttachedAttribute.AttachedObject == null)
				return "NULL BINDING";

			Table<int, ReadableTuple<int>> btable = ((BaseDb)AttachedAttribute.AttachedObject).GetMeta<int>(ServerDbs.Items);

			string value = Tuple.GetValue<string>(0);

			List<string> values = value.Split(':').ToList();
			List<string> output = new List<string>();

			for (int i = 0; i < values.Count; i++) {
				int val;

				Int32.TryParse(values[i], out val);

				if (val == 0) {
					output.Add("");
				}
				else {
					Database.Tuple tuple = btable.TryGetTuple(val);

					if (tuple == null)
						output.Add("Unknown");
					else
						output.Add(tuple.GetValue<string>(ServerItemAttributes.Name));
				}
			}

			return string.Join(Environment.NewLine, output.ToArray());
		}
	}

	public class ItemGroupBinding : IBinding {
		#region IBinding Members
		public Database.Tuple Tuple { get; set; }
		public DbAttribute AttachedAttribute { get; set; }
		#endregion

		public override string ToString() {
			if (AttachedAttribute.AttachedObject == null)
				return "";

			Table<string, ReadableTuple<string>> btable = ((BaseDb)AttachedAttribute.AttachedObject).Get<string>(ServerDbs.Constants);

			string value = Tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture);

			var res = btable.FastItems.FirstOrDefault(p => p.GetStringValue(1) == value && p.GetStringValue(0).StartsWith("IG_", StringComparison.Ordinal));

			if (res != null) {
				return res.GetKey<string>();
			}
			// This is an item table property
			Table<int, ReadableTuple<int>> table = ((BaseDb)AttachedAttribute.AttachedObject).GetMeta<int>(ServerDbs.Items);
			int key = Tuple.GetKey<int>();

			var res2 = table.TryGetTuple(key);

			if (res2 != null) {
				return res2.GetValue(ServerItemAttributes.AegisName).ToString();
			}

			return "";
		}
	}

	public class DropPercentageBinding : IBinding {
		#region IBinding Members
		public Database.Tuple Tuple { get; set; }
		public DbAttribute AttachedAttribute { get; set; }
		#endregion

		public override string ToString() {
			int parentGroup = Tuple.GetValue<int>(ServerItemGroupSubAttributes.ParentGroup);
			Table<int, ReadableTuple<int>> btable = SdeEditor.Instance.ProjectDatabase.GetDb<int>(ServerDbs.ItemGroups).Table;

			var tuple = btable.TryGetTuple(parentGroup);

			if (tuple != null) {
				Dictionary<int, ReadableTuple<int>> groups = (Dictionary<int, ReadableTuple<int>>)tuple.GetRawValue(1);

				if (groups != null) {
					ulong total = 0;

					foreach (var subTuple in groups.Values) {
						total += (ulong)subTuple.GetValue<int>(ServerItemGroupSubAttributes.Rate);
					}

					if (total <= 0)
						return "";

					return String.Format("{0:0.00} %", (Tuple.GetValue<int>(ServerItemGroupSubAttributes.Rate) / (float)total) * 100f);
				}
			}

			return "";
		}
	}

	public class DropPercentageMobBinding : IBinding {
		#region IBinding Members
		public Database.Tuple Tuple { get; set; }
		public DbAttribute AttachedAttribute { get; set; }
		#endregion

		public override string ToString() {
			int parentGroup = Tuple.GetValue<int>(ServerMobGroupSubAttributes.ParentGroup);
			Table<int, ReadableTuple<int>> btable = SdeEditor.Instance.ProjectDatabase.GetDb<int>(ServerDbs.MobGroups).Table;

			var tuple = btable.TryGetTuple(parentGroup);

			if (tuple != null) {
				Dictionary<int, ReadableTuple<int>> groups = (Dictionary<int, ReadableTuple<int>>)tuple.GetRawValue(1);

				if (groups != null) {
					ulong total = 0;
					int currentRate = 0;

					foreach (var subTuple in groups.Values) {
						int ival;
						Int32.TryParse(subTuple.GetValue<string>(ServerMobGroupSubAttributes.Rate) ?? "0", out ival);

						if (subTuple.Key == Tuple.GetKey<int>())
							currentRate = ival;

						total += (ulong)ival;
					}

					if (total <= 0)
						return "0 %";

					return String.Format("{0:0.00} %", (currentRate / (float)total) * 100f);
				}
			}

			return "0 %";
		}
	}

	public class ItemGroupSubBinding : IBinding {
		#region IBinding Members
		public Database.Tuple Tuple { get; set; }
		public DbAttribute AttachedAttribute { get; set; }
		#endregion

		public override string ToString() {
			Table<int, ReadableTuple<int>> btable = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

			int key = Tuple.GetValue<int>(0);

			var tuple = btable.TryGetTuple(key);

			if (tuple != null) {
				return tuple.GetStringValue(ServerItemAttributes.Name.Index);
			}

			return "";
		}
	}

	public class MobGroupsBinding : IBinding {
		#region IBinding Members
		public Database.Tuple Tuple { get; set; }
		public DbAttribute AttachedAttribute { get; set; }
		#endregion

		public override string ToString() {
			switch(Tuple.GetKey<int>()) {
				case 0:
					return "Dead Branch";
				case 1:
					return "Poring Box";
				case 2:
					return "Bloody Branch";
				case 3:
					return "Red Pouch";
				case 4:
					return "Hocus Pocus (Abracadabra)";
				default:
					return "Unknown";
			}
		}
	}

	public class MobBinding : IBinding {
		#region IBinding Members
		public Database.Tuple Tuple { get; set; }
		public DbAttribute AttachedAttribute { get; set; }
		#endregion

		public override string ToString() {
			Table<int, ReadableTuple<int>> btable = ((BaseDb)AttachedAttribute.AttachedObject).GetMeta<int>(ServerDbs.Mobs);

			int key;

			if (Tuple.Attributes.PrimaryAttribute.DataType == typeof(string))
				key = Tuple.GetValue<int>((int)AttachedAttribute.Default);
			else
				key = Tuple.GetValue<int>(0);

			var tuple = btable.TryGetTuple(key);

			if (tuple != null) {
				return tuple.GetStringValue(ServerMobAttributes.KRoName.Index);
			}

			return "";
		}
	}

	public class SkillBinding : IBinding {
		#region IBinding Members
		public Database.Tuple Tuple { get; set; }
		public DbAttribute AttachedAttribute { get; set; }
		#endregion

		public override string ToString() {
			if (AttachedAttribute.AttachedObject == null) {
				return "NULL BINDING";
			}

			Table<int, ReadableTuple<int>> btable = ((BaseDb)AttachedAttribute.AttachedObject).Get<int>(ServerDbs.Skills);

			int key;

			if (Tuple.Attributes.PrimaryAttribute.DataType == typeof(string))
				key = Tuple.GetValue<int>((int)AttachedAttribute.Default);
			else
				key = Tuple.GetValue<int>(0);

			var tuple = btable.TryGetTuple(key);

			if (tuple != null) {
				return tuple.GetStringValue(ServerSkillAttributes.Desc.Index);
			}

			return "";
		}
	}
}