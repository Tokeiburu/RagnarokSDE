using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SDE.Editor.Generic.Core;
using SDE.Editor.Writers;

namespace SDE.Editor.Generic.Parsers {
	public sealed class DbIOSkills {
		public static void DbSkillsCastCommaRange<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, int from, int length) {
			try {
				IntLineStream lines = new IntLineStream(debug.OldPath);
				lines.Remove(db);
				string line;

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					List<string> items = tuple.GetRawElements().Skip(from).Take(length).Select(p => p.ToString()).ToList();

					if (items.All(p => p == "0")) {
						lines.Delete(key);
						continue;
					}

					line = string.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture) }.Concat(items).ToArray());
					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbSkillsNoDexCommaRange<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, int from, int length) {
			try {
				IntLineStream lines = new IntLineStream(debug.OldPath);
				lines.Remove(db);
				string line;

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					List<string> items = tuple.GetRawElements().Skip(from).Take(length).Select(p => p.ToString()).ToList();

					if (items.All(p => p == "0")) {
						lines.Delete(key);
						continue;
					}

					string item1 = tuple.GetValue<string>(from);
					string item2 = tuple.GetValue<string>(from + 1);

					if (item1 != "0" && item2 == "0") {
						line = string.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), item1 }.ToArray());
					}
					else {
						line = string.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), item1, item2 }.ToArray());
					}

					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbSkillsNoCastCommaRange<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, int from, int length) {
			try {
				IntLineStream lines = new IntLineStream(debug.OldPath);
				lines.Remove(db);
				string line;

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					string item1 = tuple.GetValue<string>(from);
					string item2 = tuple.GetValue<string>(from + 1);

					if (item1 != "0" && item2 == "0") {
						line = string.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), item1 });
					}
					else if (item1 == "0" && item2 == "0") {
						lines.Delete(key);
						continue;
					}
					else {
						line = string.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), item1, item2 });
					}

					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}
	}
}