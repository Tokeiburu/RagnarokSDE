using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Database;
using SDE.Editor.Engines;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.Parsers.Libconfig;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Writers;

namespace SDE.Editor.Generic.Parsers {
	public sealed class DbIOQuests {
		public static void Loader<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			if (debug.FileType == FileType.Txt) {
				List<DbAttribute> attributes = new List<DbAttribute>(db.AttributeList.Attributes);

				bool rAthenaNewFormat = false;
				int[] oldColumns = {
					0, 1, 2, 3, 4, 5, 6, 7, 17
				};

				foreach (string[] elements in TextFileHelper.GetElementsByCommasQuotes(FtpHelper.ReadAllBytes(debug.FilePath))) {
					try {
						TKey id = (TKey)TypeDescriptor.GetConverter(typeof(TKey)).ConvertFrom(elements[0]);

						if (elements.Length == 18) {
							rAthenaNewFormat = true;
							db.Attached["rAthenaFormat"] = 18;
						}

						if (rAthenaNewFormat) {
							for (int index = 1; index < elements.Length; index++) {
								DbAttribute property = attributes[index];
								db.Table.SetRaw(id, property, elements[index]);
							}
						}
						else {
							for (int index = 1; index < oldColumns.Length; index++) {
								DbAttribute property = attributes[oldColumns[index]];
								db.Table.SetRaw(id, property, elements[index]);
							}
						}
					}
					catch {
						if (elements.Length <= 0) {
							if (!debug.ReportIdException("#")) return;
						}
						else if (!debug.ReportIdException(elements[0])) return;
					}
				}
			}
			else if (debug.FileType == FileType.Conf) {
				var ele = new LibconfigParser(debug.FilePath);
				var table = debug.AbsractDb.Table;

				foreach (var quest in ele.Output["copy_paste"] ?? ele.Output["quest_db"]) {
					try {
						int id = Int32.Parse(quest["Id"]);
						TKey questId = (TKey)(object)id;

						table.SetRaw(questId, ServerQuestsAttributes.QuestTitle, "\"" + (quest["Name"] ?? "") + "\"");
						table.SetRaw(questId, ServerQuestsAttributes.TimeLimit, (quest["TimeLimit"] ?? "0"));

						var targets = quest["Targets"] as LibconfigList;

						if (targets != null) {
							int count = 0;

							foreach (var target in targets) {
								if (count >= 3) {
									debug.ReportIdExceptionWithError("The maximum amount of targets has been reached (up to 3).", id, targets.Line);
									continue;
								}

								table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.TargetId1.Index + 2 * count], target["MobId"] ?? "0");
								table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.Val1.Index + 2 * count], target["Count"] ?? "0");
								count++;
							}
						}

						var drops = quest["Drops"] as LibconfigList;

						if (drops != null) {
							int count = 0;

							foreach (var drop in drops) {
								if (count >= 3) {
									debug.ReportIdExceptionWithError("The maximum amount of drops has been reached (up to 3).", id, drops.Line);
									continue;
								}

								table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.NameId1.Index + 3 * count], drop["ItemId"] ?? "0");
								table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.Rate1.Index + 3 * count], drop["Rate"] ?? "0");
								table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.MobId1.Index + 3 * count], drop["MobId"] ?? "0");
								count++;
							}
						}
					}
					catch {
						if (quest["Id"] == null) {
							if (!debug.ReportIdException("#", quest.Line)) return;
						}
						else if (!debug.ReportIdException(quest["Id"], quest.Line)) return;
					}
				}
			}
		}

		public static void WriteEntry<TKey>(StringBuilder builder, ReadableTuple<TKey> tuple) {
			builder.AppendLine("{");

			builder.Append("\tId: ");
			builder.AppendLine(tuple.GetValue<string>(ServerQuestsAttributes.Id));

			builder.Append("\tName: ");
			builder.AppendLine("\"" + (tuple.GetValue<string>(ServerQuestsAttributes.QuestTitle) ?? "") + "\"");

			int val = tuple.GetValue<int>(ServerQuestsAttributes.TimeLimit);

			if (val != 0) {
				builder.Append("\tTimeLimit: ");
				builder.AppendLine(val.ToString(CultureInfo.InvariantCulture));
			}

			var target1 = tuple.GetValue<int>(ServerQuestsAttributes.TargetId1);
			var target2 = tuple.GetValue<int>(ServerQuestsAttributes.TargetId2);
			var target3 = tuple.GetValue<int>(ServerQuestsAttributes.TargetId3);

			if (target1 != 0 || target2 != 0 || target3 != 0) {
				builder.AppendLine("\tTargets: (");

				if (target1 != 0) {
					builder.AppendLine("\t{");
					builder.AppendLine("\t\tMobId: " + target1);
					builder.AppendLine("\t\tCount: " + tuple.GetValue<int>(ServerQuestsAttributes.Val1));
					builder.AppendLine("\t},");
				}

				if (target2 != 0) {
					builder.AppendLine("\t{");
					builder.AppendLine("\t\tMobId: " + target2);
					builder.AppendLine("\t\tCount: " + tuple.GetValue<int>(ServerQuestsAttributes.Val2));
					builder.AppendLine("\t},");
				}

				if (target3 != 0) {
					builder.AppendLine("\t{");
					builder.AppendLine("\t\tMobId: " + target3);
					builder.AppendLine("\t\tCount: " + tuple.GetValue<int>(ServerQuestsAttributes.Val3));
					builder.AppendLine("\t},");
				}

				builder.AppendLine("\t)");
			}

			target1 = tuple.GetValue<int>(ServerQuestsAttributes.NameId1);
			target2 = tuple.GetValue<int>(ServerQuestsAttributes.NameId2);
			target3 = tuple.GetValue<int>(ServerQuestsAttributes.NameId3);

			if (target1 != 0 || target2 != 0 || target3 != 0) {
				builder.AppendLine("\tDrops: (");

				if (target1 != 0) {
					builder.AppendLine("\t{");
					builder.AppendLine("\t\tItemId: " + target1);
					builder.AppendLine("\t\tRate: " + tuple.GetValue<int>(ServerQuestsAttributes.Rate1));

					target1 = tuple.GetValue<int>(ServerQuestsAttributes.MobId1);

					if (target1 != 0)
						builder.AppendLine("\t\tMobId: " + target1);

					builder.AppendLine("\t},");
				}

				if (target2 != 0) {
					builder.AppendLine("\t{");
					builder.AppendLine("\t\tItemId: " + target2);
					builder.AppendLine("\t\tRate: " + tuple.GetValue<int>(ServerQuestsAttributes.Rate2));

					target2 = tuple.GetValue<int>(ServerQuestsAttributes.MobId2);

					if (target2 != 0)
						builder.AppendLine("\t\tMobId: " + target2);

					builder.AppendLine("\t},");
				}

				if (target3 != 0) {
					builder.AppendLine("\t{");
					builder.AppendLine("\t\tItemId: " + target3);
					builder.AppendLine("\t\tRate: " + tuple.GetValue<int>(ServerQuestsAttributes.Rate3));

					target3 = tuple.GetValue<int>(ServerQuestsAttributes.MobId3);

					if (target3 != 0)
						builder.AppendLine("\t\tMobId: " + target3);

					builder.AppendLine("\t},");
				}

				builder.AppendLine("\t)");
			}

			builder.Append("},");
		}

		public static void Writer<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			if (debug.FileType == FileType.Conf) {
				try {
					var lines = new LibconfigParser(debug.OldPath, LibconfigMode.Write);
					lines.Remove(db);
					string line;

					foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
						int key = tuple.GetKey<int>();
						StringBuilder builder = new StringBuilder();
						WriteEntry(builder, tuple);
						line = builder.ToString();
						lines.Write(key.ToString(CultureInfo.InvariantCulture), line);
					}

					lines.WriteFile(debug.FilePath);
				}
				catch (Exception err) {
					debug.ReportException(err);
				}
			}
			else {
				int format = db.GetAttacked<int>("rAthenaFormat");

				try {
					IntLineStream lines = new IntLineStream(debug.OldPath);
					lines.Remove(db);
					string line;

					foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
						int key = tuple.GetKey<int>();

						if (format == 18) {
							line = string.Join(",", tuple.GetRawElements().Select(p => (p ?? "").ToString()).ToArray());
						}
						else {
							line = string.Join(",", tuple.GetRawElements().Take(ServerQuestsAttributes.MobId1.Index).Concat(new object[] { tuple.GetRawValue(ServerQuestsAttributes.QuestTitle.Index) }).Select(p => (p ?? "").ToString()).ToArray());
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