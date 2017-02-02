using System;
using System.Linq;
using System.Text;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.Parsers.Libconfig;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.Editor.Writers;

namespace SDE.Editor.Generic.Parsers {
	public sealed class DbIOConstants {
		public static void Loader<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			if (debug.FileType == FileType.Txt) {
				DbIOMethods.DbLoaderAny(debug, db, TextFileHelper.GetElementsByTabs);
			}
			else if (debug.FileType == FileType.Conf) {
				var ele = new LibconfigParser(debug.FilePath);
				var table = debug.AbsractDb.Table;

				foreach (var constant in ele.Output["copy_paste"] ?? ele.Output["constants_db"]) {
					try {
						var keyValue = constant as LibconfigKeyValue;

						if (keyValue != null) {
							if (keyValue.Key == "comment__")
								continue;

							if (keyValue.Value is LibconfigArrayBase) {
								var arrayList = (LibconfigArrayBase)keyValue.Value;

								table.SetRaw((TKey)(object)keyValue.Key, ServerConstantsAttributes.Deprecated, arrayList["Deprecated"] ?? "false");
								table.SetRaw((TKey)(object)keyValue.Key, ServerConstantsAttributes.Value, arrayList["Value"] ?? "0");

								if (arrayList["Parameter"] != null) {
									table.SetRaw((TKey)(object)keyValue.Key, ServerConstantsAttributes.Type, Boolean.Parse(arrayList["Parameter"]) ? "1" : "0");
								}
							}
							else {
								table.SetRaw((TKey)(object)keyValue.Key, ServerConstantsAttributes.Value, keyValue.ObjectValue);
							}
						}
					}
					catch {
						if (!debug.ReportIdException(constant.ObjectValue, constant.Line)) return;
					}
				}
			}
		}

		public static void WriteEntry<TKey>(StringBuilder builder, ReadableTuple<TKey> tuple) {
			string value = tuple.GetValue<string>(ServerConstantsAttributes.Value);
			int group = tuple.GetValue<int>(ServerConstantsAttributes.Type);
			bool deprecated = tuple.GetValue<bool>(ServerConstantsAttributes.Deprecated);
			bool writeGroup = group == 1 || deprecated;

			if (writeGroup) {
				builder.AppendLine("{");

				builder.Append("\t\tValue: ");
				builder.AppendLine(value);

				if (group == 1) {
					builder.AppendLine("\t\tParameter: true");
				}

				if (deprecated) {
					builder.AppendLine("\t\tDeprecated: true");
				}

				builder.Append("\t}");
			}
			else {
				builder.Append(value);
			}
		}

		public static void Writer<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			if (debug.FileType == FileType.Conf) {
				try {
					var lines = new LibconfigParser(debug.OldPath, LibconfigMode.Write);
					lines.Remove(db);

					foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
						string key = tuple.GetKey<string>();

						StringBuilder builder = new StringBuilder();
						WriteEntry(builder, tuple);
						lines.Write(key, builder.ToString());
					}

					lines.WriteFile(debug.FilePath);
				}
				catch (Exception err) {
					debug.ReportException(err);
				}
			}
			else {
				try {
					StringLineStream lines = new StringLineStream(debug.OldPath);
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
		}
	}
}