using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Database;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using Utilities;

namespace SDE.Tools.DatabaseEditor.Generic.Lists.DbAttributeHelpers {
	public class ComboBinding : IBinding {
		#region IBinding Members

		public Tuple Tuple { get; set; }
		public DbAttribute AttachedAttribute { get; set; }

		#endregion

		public override string ToString() {
			if (AttachedAttribute.AttachedObject == null)
				return "";

			Table<int, ReadableTuple<int>> btable = ((BaseDb)AttachedAttribute.AttachedObject).Get<int>(ServerDBs.Items);

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
					Tuple tuple = btable.TryGetTuple(val);

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

		public Tuple Tuple { get; set; }
		public DbAttribute AttachedAttribute { get; set; }

		#endregion

		public override string ToString() {
			if (AttachedAttribute.AttachedObject == null)
				return "";

			Table<string, ReadableTuple<string>> btable = ((BaseDb)AttachedAttribute.AttachedObject).Get<string>(ServerDBs.Constants);

			string value = Tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture);

			var res = btable.FastItems.FirstOrDefault(p => p.GetStringValue(1) == value && p.GetStringValue(0).StartsWith("IG_", StringComparison.Ordinal));

			if (res != null) {
				return res.GetKey<string>();
			}
			else {
				// This is an item table property
				Table<int, ReadableTuple<int>> table = ((BaseDb)AttachedAttribute.AttachedObject).Get<int>(ServerDBs.Items);
				int key = Tuple.GetKey<int>();

				var res2 = table.TryGetTuple(key);

				if (res2 != null) {
					return res2.GetValue(ServerItemAttributes.AegisName).ToString();
				}
			}

			return "";
		}
	}

	public class MobSkillBinding : IBinding {
		#region IBinding Members

		public Tuple Tuple { get; set; }
		public DbAttribute AttachedAttribute { get; set; }

		#endregion

		public override string ToString() {
			Table<int, ReadableTuple<int>> btable = ((BaseDb)AttachedAttribute.AttachedObject).Get<int>(ServerDBs.Skills);

			int key = Tuple.GetValue<int>(0);

			var tuple = btable.TryGetTuple(key);

			if (tuple != null) {
				return tuple.GetStringValue(ServerSkillAttributes.Desc.Index);
			}

			return "";
		}
	}

	public class ItemBinding : IBinding {
		#region IBinding Members

		public Tuple Tuple { get; set; }
		public DbAttribute AttachedAttribute { get; set; }

		#endregion

		public override string ToString() {
			Table<int, ReadableTuple<int>> btable = ((BaseDb) AttachedAttribute.AttachedObject).Get<int>(ServerDBs.Items);

			int key = Tuple.GetValue<int>(0);

			var tuple = btable.TryGetTuple(key);

			if (tuple != null) {
				return tuple.GetStringValue(ServerItemAttributes.AegisName.Index);
			}

			return "";
		}
	}
}