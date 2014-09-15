using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Database;
using GRF.IO;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Engines.Parsers;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders.Writers {
	public static partial class DbWriters {
		public static void DbSkillsCommaRange<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, int from, int length) {
			try {
				IntLinesStream lines = new IntLinesStream(debug.OldPath);
				lines.Remove(db);
				ServerType serverType;

				if (db.Attached["DbCommaLoaderSkill.IsRAthena"] == null)
					serverType = ServerType.Hercules;
				else {
					serverType = (bool) db.Attached["DbCommaLoaderSkill.IsRAthena"] ? ServerType.RAthena : ServerType.Hercules;
				}

				if (serverType == ServerType.RAthena) {
					foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
						int key = tuple.GetKey<int>();

						List<string> items = tuple.GetRawElements().Skip(from).Take(length).Select(p => p.ToString()).ToList();
						lines.Write(key, string.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture) }.Concat(items).ToArray()));
					}
				}
				else {
					foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
						int key = tuple.GetKey<int>();

						var items = tuple.GetRawElements().Skip(0).Take(length + 1).Select(p => p.ToString()).ToList();
						items.RemoveAt(ServerSkillAttributes.Inf3.Index);

						lines.Write(key, string.Join(",", items.ToArray()));
					}
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbSkillsCastCommaRange<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, int from, int length) {
			try {
				IntLinesStream lines = new IntLinesStream(debug.OldPath);
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
				IntLinesStream lines = new IntLinesStream(debug.OldPath);
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

		public static void DbItemsCommaRange<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, int from, int length, string defaultValue) {
			try {
				IntLinesStream lines = new IntLinesStream(debug.OldPath);
				lines.Remove(db);
				string line;

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					List<string> items = tuple.GetRawElements().Skip(from).Take(length).Select(p => p.ToString()).ToList();

					if (items.All(p => p == defaultValue)) {
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

		public static void DbItemsNouse<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				IntLinesStream lines = new IntLinesStream(debug.OldPath);
				lines.Remove(db);
				string line;
				Nouse nouse = new Nouse();

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					string item1 = tuple.GetValue<string>(ServerItemAttributes.Nouse);
					nouse.Override = Parser.GetVal(item1, "override", "100");
					nouse.Sitting = Parser.GetVal(item1, "sitting", "false");

					if (nouse.Override == "100" && nouse.Sitting == "false") {
						lines.Delete(key);
						continue;
					}

					line = string.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), nouse.Sitting == "true" ? "1" : "0", nouse.Override }.ToArray());
					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbItemsTrade<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				IntLinesStream lines = new IntLinesStream(debug.OldPath);
				lines.Remove(db);
				string line;
				Trade trade = new Trade();

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					string output = tuple.GetValue<string>(ServerItemAttributes.Trade);
					trade.Set(output);

					if (!trade.NeedPrinting()) {
						lines.Delete(key);
						continue;
					}

					line = string.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), trade.GetInt().ToString(CultureInfo.InvariantCulture), trade.Override }.ToArray());
					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbItemsStack<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				IntLinesStream lines = new IntLinesStream(debug.OldPath);
				lines.Remove(db);
				string line;

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					string item1 = tuple.GetValue<string>(ServerItemAttributes.Stack);

					if (item1 == "") {
						lines.Delete(key);
						continue;
					}

					line = string.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), item1 }.ToArray());
					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbItemsBuyingStore<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				IntLinesStream lines = new IntLinesStream(debug.OldPath);
				lines.Remove(db);
				string line;

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					bool item1 = tuple.GetValue<bool>(ServerItemAttributes.BuyingStore);

					if (!item1) {
						lines.Delete(key);
						continue;
					}

					line = key.ToString(CultureInfo.InvariantCulture) + "  // " + tuple.GetValue<string>(ServerItemAttributes.AegisName);
					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbIntComma<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			DbIntCommaRange(debug, db, 0, db.AttributeList.Attributes.Count);
		}

		public static void DbIntCommaRange<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, int from, int to) {
			try {
				IntLinesStream lines = new IntLinesStream(debug.OldPath);
				lines.Remove(db);
				string line;

				for (int i = from; i < to; i++) {
					DbAttribute att = db.AttributeList.Attributes[i];

					if (att.Visibility == VisibleState.Hidden) {
						to = i;
						break;
					}
				}

				List<DbAttribute> attributes = new List<DbAttribute>(db.AttributeList.Attributes.Skip(from).Take(to));
				attributes.Reverse();

				List<DbAttribute> attributesToRemove = 
					(from attribute in attributes where db.Attached[attribute.DisplayName] != null 
					 let isLoaded = (bool) db.Attached[attribute.DisplayName] 
					 where !isLoaded 
					 select attribute).ToList();

				IEnumerable<ReadableTuple<TKey>> list;

				if (db.Attached["EntireRewrite"] != null && (bool)db.Attached["EntireRewrite"]) {
					list = db.Table.FastItems.Where(p => !p.Deleted).OrderBy(p => p.GetKey<TKey>());
				}
				else {
					list = db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>());
				}

				foreach (ReadableTuple<TKey> tuple in list) {
					int key = tuple.GetKey<int>();
					List<object> rawElements = tuple.GetRawElements().Skip(from).Take(to).ToList();

					foreach (var attribute in attributesToRemove) {
						rawElements.RemoveAt(attribute.Index - from);
					}

					line = string.Join(",", rawElements.Select(p => (p ?? "").ToString()).ToArray());
					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbConstantsWriter<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				StringLinesStream lines = new StringLinesStream(debug.OldPath);
				lines.Remove(db);
				string line;

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					string key = tuple.GetKey<string>();

					int item2 = tuple.GetValue<int>(2);

					if (item2 == 0) {
						line = string.Join("\t", tuple.GetRawElements().Take(2).Select(p => (p ?? "").ToString()).ToArray());
					}
					else {
						line = string.Join("\t", tuple.GetRawElements().Take(3).Select(p => (p ?? "").ToString()).ToArray());
					}

					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbStringCommaWriter<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				StringLinesStream lines = new StringLinesStream(debug.OldPath, ',');
				lines.Remove(db);
				string line;

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					string key = tuple.GetKey<string>();
					line = string.Join(",", tuple.GetRawElements().Select(p => (p ?? "").ToString()).ToArray());
					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbUniqueWriter<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				if (!db.IsModified) {
					DbDirectCopyWriter(debug, db);
					return;
				}

				StringLinesStream lines = new StringLinesStream(debug.OldPath);
				lines.ClearAfterComments();

				lines.Remove(db);
				string line;

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.OrderBy(p => p.GetValue<int>(1))) {
					line = string.Join(",", tuple.GetRawElements().Skip(1).Select(p => (p ?? "").ToString()).ToArray());
					lines.Append(line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbSkillsNoCastCommaRange<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, int from, int length) {
			try {
				IntLinesStream lines = new IntLinesStream(debug.OldPath);
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

		public static void DbCommaWriter<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				List<string> lines = File.ReadAllLines(debug.OldPath).ToList();

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					string key = tuple.GetKey<TKey>().ToString();
					string keyComma = key + ",";

					int index = lines.IndexOfFirstOrDefault(keyComma);

					if (index > -1) {
						List<string> items = tuple.GetRawElements().Select(p => p.ToString()).ToList();
						lines[index] = string.Join(",", new string[] { key }.Concat(items).ToArray());
					}
				}

				File.WriteAllLines(debug.FilePath, lines.ToArray());
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbItemsWriterSub<TKey>(StringBuilder builder, AbstractDb<TKey> db, IEnumerable<ReadableTuple<TKey>> tuples, ServerType to) {
			if (to == ServerType.RAthena) {
				bool fromTxtDb = AllLoaders.DetectPath(db.DbSource).IsExtension(".txt");

				foreach (ReadableTuple<TKey> tuple in tuples) {
					List<string> rawElements = tuple.GetRawElements().Take(22).Select(p => p.ToString()).ToList();

					if (tuple.Normal && fromTxtDb) {
						builder.AppendLine(string.Join(",", rawElements.ToArray()));
						continue;
					}

					string script1 = tuple.GetValue<string>(19);
					string script2 = tuple.GetValue<string>(20);
					string script3 = tuple.GetValue<string>(21);
					string refine = tuple.GetValue<string>(17);

					if (refine == "") {
						refine = "";
					}
					else if (refine == "true" || refine == "1") {
						refine = "1";
					}
					else {
						refine = "0";
					}

					builder.AppendLine(string.Join(",",
						new string[] {
							rawElements[0],
							rawElements[1],
							rawElements[2],
							_outputInteger(rawElements[3]), // Type
							_zeroDefault(rawElements[4]),
							_zeroDefault(rawElements[5]),
							String.IsNullOrEmpty(rawElements[6]) ? "0" : rawElements[6],
							_zeroDefault(rawElements[7]),
							_zeroDefault(rawElements[8]),
							_zeroDefault(rawElements[9]),
							_zeroDefault(rawElements[10]), // Slots
							String.IsNullOrEmpty(rawElements[11]) ? "0xFFFFFFFF" : !rawElements[11].StartsWith("0x") ? "0x" + Int32.Parse(rawElements[11]).ToString("X8") : rawElements[11],
							_hexToInt(rawElements[12]), // Upper
							_zeroDefault(rawElements[13]),
							_zeroDefault(_hexToInt(rawElements[14])),
							_zeroDefault(rawElements[15]),
							_zeroDefault(rawElements[16]),
							refine,
							_zeroDefault(rawElements[18]),
							String.IsNullOrEmpty(script1) ? "{}" : "{ " + script1 + " }",
							String.IsNullOrEmpty(script2) ? "{}" : "{ " + script2 + " }",
							String.IsNullOrEmpty(script3) ? "{}" : "{ " + script3 + " }"
						}));
				}
			}
			else if (to == ServerType.Hercules) {
				foreach (int id in tuples.Select(p => p.GetKey<int>()).OrderBy(p => p)) {
					builder.AppendLineUnix(ItemParser.ToHerculesEntry(db, id));
				}
			}
		}

		public static void DbItemsCommaWriter<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				StringBuilder builder = new StringBuilder();

				if (debug.FileType == FileType.Txt) {
					if (AllLoaders.GetServerType() == ServerType.RAthena) {
						DbIntCommaRange(debug, db, 0, ServerItemAttributes.OnUnequipScript.Index + 1);
						return;
					}

					DbItemsWriterSub(builder, db, db.Table.FastItems.OrderBy(p => p.GetKey<TKey>()), ServerType.RAthena);
					File.WriteAllText(debug.FilePath, builder.ToString(), Encoding.Default);
				}
				else if (debug.FileType == FileType.Conf) {
					builder.AppendLineUnix("item_db: (");
					DbItemsWriterSub(builder, db, db.Table.FastItems, ServerType.Hercules);
					builder.AppendLineUnix(")");
					File.WriteAllText(debug.FilePath, builder.ToString(), Encoding.Default);
				}
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbDirectCopyWriter<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				if (debug.OldPath != debug.FilePath) {
					GrfPath.Delete(debug.FilePath);
					File.Copy(debug.OldPath, debug.FilePath);
				}
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		private static string _outputInteger(string rawElement) {
			if (String.IsNullOrEmpty(rawElement))
				return "0";

			return rawElement;
		}

		private static string _zeroDefault(string rawElement) {
			if (String.IsNullOrEmpty(rawElement))
				return rawElement;

			if (rawElement == "0")
				return "";

			return rawElement;
		}

		private static string _hexToInt(string rawElement) {
			if (String.IsNullOrEmpty(rawElement))
				return rawElement;

			if (rawElement.StartsWith("0x") || rawElement.StartsWith("0X"))
				return Convert.ToInt32(rawElement, 16).ToString(CultureInfo.InvariantCulture);

			return rawElement;
		}
	}
}