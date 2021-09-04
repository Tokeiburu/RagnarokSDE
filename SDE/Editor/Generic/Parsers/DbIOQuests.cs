using Database;
using SDE.Editor.Engines;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.Parsers.Libconfig;
using SDE.Editor.Engines.Parsers.Yaml;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.Editor.Writers;
using SDE.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SDE.Editor.Generic.Parsers
{
    public sealed class DbIOQuests
    {
        public static void Loader<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db)
        {
            if (debug.FileType == FileType.Yaml)
            {
                var ele = new YamlParser(debug.FilePath);
                var table = debug.AbsractDb.Table;

                if (ele.Output == null || ((ParserArray)ele.Output).Objects.Count == 0 || (ele.Output["copy_paste"] ?? ele.Output["Body"]) == null)
                    return;

                var mobDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);
                var itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

                foreach (var quest in ele.Output["copy_paste"] ?? ele.Output["Body"])
                {
                    try
                    {
                        int id = Int32.Parse(quest["Id"]);
                        TKey questId = (TKey)(object)id;

                        table.SetRaw(questId, ServerQuestsAttributes.QuestTitle, "\"" + (quest["Title"] ?? "").Trim('\"') + "\"");
                        table.SetRaw(questId, ServerQuestsAttributes.TimeLimitNew, (quest["TimeLimit"] ?? ""));

                        var targets = quest["Targets"] as ParserList;

                        if (targets != null)
                        {
                            int count = 0;

                            foreach (var target in targets)
                            {
                                if (count >= 3)
                                {
                                    debug.ReportIdExceptionWithError("The maximum amount of targets has been reached (up to 3).", id, targets.Line);
                                    continue;
                                }

                                table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.TargetId1.Index + 2 * count], DbIOUtils.Name2Id(mobDb, ServerMobAttributes.AegisName, target["Mob"] ?? "", "mob_db", true));
                                table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.Val1.Index + 2 * count], target["Count"] ?? "0");
                                table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.Id1.Index + count], target["Id"] ?? "0");
                                table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.Race1.Index + count], target["Race"] ?? "0");
                                table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.Size1.Index + count], target["Size"] ?? "0");
                                table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.Element1.Index + count], target["Element"] ?? "0");
                                table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.MinLevel1.Index + count], target["MinLevel"] ?? "0");
                                table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.MaxLevel1.Index + count], target["MaxLevel"] ?? "0");
                                count++;
                            }
                        }

                        var drops = quest["Drops"] as ParserList;

                        if (drops != null)
                        {
                            int count = 0;

                            foreach (var drop in drops)
                            {
                                if (count >= 3)
                                {
                                    debug.ReportIdExceptionWithError("The maximum amount of drops has been reached (up to 3).", id, drops.Line);
                                    continue;
                                }

                                table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.NameId1.Index + 3 * count], DbIOUtils.Name2Id(itemDb, ServerItemAttributes.AegisName, drop["Item"] ?? "", "item_db", true));
                                table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.Rate1.Index + 3 * count], drop["Rate"] ?? "0");
                                table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.MobId1.Index + 3 * count], DbIOUtils.Name2Id(mobDb, ServerMobAttributes.AegisName, drop["Mob"] ?? "", "mob_db", true));
                                table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.Count1.Index + count], drop["Count"] ?? "1");
                                count++;
                            }
                        }
                    }
                    catch
                    {
                        if (quest["Id"] == null)
                        {
                            if (!debug.ReportIdException("#", quest.Line)) return;
                        }
                        else if (!debug.ReportIdException(quest["Id"], quest.Line)) return;
                    }
                }
            }
            else if (debug.FileType == FileType.Txt)
            {
                List<DbAttribute> attributes = new List<DbAttribute>(db.AttributeList.Attributes);

                bool rAthenaNewFormat = false;
                int[] oldColumns = {
                    0, 1, 2, 3, 4, 5, 6, 7, 17
                };

                foreach (string[] elements in TextFileHelper.GetElementsByCommasQuotes(IOHelper.ReadAllBytes(debug.FilePath)))
                {
                    try
                    {
                        TKey id = (TKey)TypeDescriptor.GetConverter(typeof(TKey)).ConvertFrom(elements[0]);

                        if (elements.Length == 18)
                        {
                            rAthenaNewFormat = true;
                            db.Attached["rAthenaFormat"] = 18;
                        }

                        if (rAthenaNewFormat)
                        {
                            for (int index = 1; index < elements.Length; index++)
                            {
                                DbAttribute property = attributes[index];
                                db.Table.SetRaw(id, property, elements[index]);
                            }
                        }
                        else
                        {
                            for (int index = 1; index < oldColumns.Length; index++)
                            {
                                DbAttribute property = attributes[oldColumns[index]];
                                db.Table.SetRaw(id, property, elements[index]);
                            }
                        }
                    }
                    catch
                    {
                        if (elements.Length <= 0)
                        {
                            if (!debug.ReportIdException("#")) return;
                        }
                        else if (!debug.ReportIdException(elements[0])) return;
                    }
                }
            }
            else if (debug.FileType == FileType.Conf)
            {
                var ele = new LibconfigParser(debug.FilePath);
                var table = debug.AbsractDb.Table;

                foreach (var quest in ele.Output["copy_paste"] ?? ele.Output["quest_db"])
                {
                    try
                    {
                        int id = Int32.Parse(quest["Id"]);
                        TKey questId = (TKey)(object)id;

                        table.SetRaw(questId, ServerQuestsAttributes.QuestTitle, "\"" + (quest["Name"] ?? "") + "\"");
                        table.SetRaw(questId, ServerQuestsAttributes.TimeLimit, (quest["TimeLimit"] ?? "0"));

                        var targets = quest["Targets"] as ParserList;

                        if (targets != null)
                        {
                            int count = 0;

                            foreach (var target in targets)
                            {
                                if (count >= 3)
                                {
                                    debug.ReportIdExceptionWithError("The maximum amount of targets has been reached (up to 3).", id, targets.Line);
                                    continue;
                                }

                                table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.TargetId1.Index + 2 * count], target["MobId"] ?? "0");
                                table.SetRaw(questId, ServerQuestsAttributes.AttributeList[ServerQuestsAttributes.Val1.Index + 2 * count], target["Count"] ?? "0");
                                count++;
                            }
                        }

                        var drops = quest["Drops"] as ParserList;

                        if (drops != null)
                        {
                            int count = 0;

                            foreach (var drop in drops)
                            {
                                if (count >= 3)
                                {
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
                    catch
                    {
                        if (quest["Id"] == null)
                        {
                            if (!debug.ReportIdException("#", quest.Line)) return;
                        }
                        else if (!debug.ReportIdException(quest["Id"], quest.Line)) return;
                    }
                }
            }
        }

        public static void WriteEntryYaml<TKey>(StringBuilder builder, ReadableTuple<TKey> tuple, MetaTable<int> itemDb, MetaTable<int> mobDb)
        {
            if (tuple != null)
            {
                string valueS;
                int value;

                builder.AppendLine("  - Id: " + tuple.GetKey<int>());

                if ((valueS = tuple.GetValue<string>(ServerQuestsAttributes.QuestTitle)) != "" && valueS != "0")
                {
                    builder.AppendLine("    Title: " + DbIOUtils.QuoteCheck(valueS));
                }

                if ((valueS = tuple.GetValue<string>(ServerQuestsAttributes.TimeLimitNew)) != "" && valueS != "0")
                {
                    builder.AppendLine("    TimeLimit: " + valueS);
                }

                if (tuple.GetValue<int>(ServerQuestsAttributes.TargetId1) > 0 || tuple.GetValue<int>(ServerQuestsAttributes.TargetId2) > 0 || tuple.GetValue<int>(ServerQuestsAttributes.TargetId3) > 0)
                {
                    builder.AppendLine("    Targets:");

                    for (int i = 0; i < 3; i++)
                    {
                        if ((valueS = tuple.GetValue<string>(ServerQuestsAttributes.TargetId1.Index + 2 * i)) != "" && valueS != "0")
                        {
                            builder.AppendLine("      - Mob: " + DbIOUtils.Id2Name(mobDb, ServerMobAttributes.AegisName, valueS));

                            if ((value = tuple.GetValue<int>(ServerQuestsAttributes.Val1.Index + 2 * i)) != 0)
                            {
                                builder.AppendLine("        Count: " + value);
                            }

                            if ((value = tuple.GetValue<int>(ServerQuestsAttributes.Id1.Index + i)) != 0)
                            {
                                builder.AppendLine("        Id: " + value);
                            }

                            if ((value = tuple.GetValue<int>(ServerQuestsAttributes.Race1.Index + i)) != 0)
                            {
                                builder.AppendLine("        Race: " + Constants.ToString<QuestRaceType>(value));
                            }

                            if ((value = tuple.GetValue<int>(ServerQuestsAttributes.Size1.Index + i)) != 0)
                            {
                                builder.AppendLine("        Size: " + Constants.ToString<QuestSizeType>(value));
                            }

                            if ((value = tuple.GetValue<int>(ServerQuestsAttributes.Element1.Index + i)) != 0)
                            {
                                builder.AppendLine("        Element: " + Constants.ToString<QuestElementType>(value));
                            }

                            if ((value = tuple.GetValue<int>(ServerQuestsAttributes.MinLevel1.Index + i)) != 0)
                            {
                                builder.AppendLine("        MinLevel: " + value);
                            }

                            if ((value = tuple.GetValue<int>(ServerQuestsAttributes.MaxLevel1.Index + i)) != 0)
                            {
                                builder.AppendLine("        MaxLevel: " + value);
                            }
                        }
                    }
                }

                if (tuple.GetValue<int>(ServerQuestsAttributes.NameId1) > 0 || tuple.GetValue<int>(ServerQuestsAttributes.NameId2) > 0 || tuple.GetValue<int>(ServerQuestsAttributes.NameId3) > 0)
                {
                    builder.AppendLine("    Drops:");

                    for (int i = 0; i < 3; i++)
                    {
                        if ((valueS = tuple.GetValue<string>(ServerQuestsAttributes.MobId1.Index + 3 * i)) != "" && valueS != "0")
                        {
                            builder.AppendLine("      - Mob: " + DbIOUtils.Id2Name(mobDb, ServerMobAttributes.AegisName, valueS));

                            builder.AppendLine("        Item: " + DbIOUtils.Id2Name(itemDb, ServerItemAttributes.AegisName, tuple.GetValue<string>(ServerQuestsAttributes.NameId1.Index + 3 * i)));
                            builder.AppendLine("        Rate: " + tuple.GetValue<string>(ServerQuestsAttributes.Rate1.Index + 3 * i));

                            if ((value = tuple.GetValue<int>(ServerQuestsAttributes.Count1.Index + i)) != 1)
                            {
                                builder.AppendLine("        Count: " + value);
                            }
                        }
                    }
                }
            }
        }

        public static void WriteEntry<TKey>(StringBuilder builder, ReadableTuple<TKey> tuple)
        {
            builder.AppendLine("{");

            builder.Append("\tId: ");
            builder.AppendLine(tuple.GetValue<string>(ServerQuestsAttributes.Id));

            builder.Append("\tName: ");
            builder.AppendLine("\"" + (tuple.GetValue<string>(ServerQuestsAttributes.QuestTitle) ?? "") + "\"");

            int val = tuple.GetValue<int>(ServerQuestsAttributes.TimeLimit);

            if (val != 0)
            {
                builder.Append("\tTimeLimit: ");
                builder.AppendLine(val.ToString(CultureInfo.InvariantCulture));
            }

            var target1 = tuple.GetValue<int>(ServerQuestsAttributes.TargetId1);
            var target2 = tuple.GetValue<int>(ServerQuestsAttributes.TargetId2);
            var target3 = tuple.GetValue<int>(ServerQuestsAttributes.TargetId3);

            if (target1 != 0 || target2 != 0 || target3 != 0)
            {
                builder.AppendLine("\tTargets: (");

                if (target1 != 0)
                {
                    builder.AppendLine("\t{");
                    builder.AppendLine("\t\tMobId: " + target1);
                    builder.AppendLine("\t\tCount: " + tuple.GetValue<int>(ServerQuestsAttributes.Val1));
                    builder.AppendLine("\t},");
                }

                if (target2 != 0)
                {
                    builder.AppendLine("\t{");
                    builder.AppendLine("\t\tMobId: " + target2);
                    builder.AppendLine("\t\tCount: " + tuple.GetValue<int>(ServerQuestsAttributes.Val2));
                    builder.AppendLine("\t},");
                }

                if (target3 != 0)
                {
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

            if (target1 != 0 || target2 != 0 || target3 != 0)
            {
                builder.AppendLine("\tDrops: (");

                if (target1 != 0)
                {
                    builder.AppendLine("\t{");
                    builder.AppendLine("\t\tItemId: " + target1);
                    builder.AppendLine("\t\tRate: " + tuple.GetValue<int>(ServerQuestsAttributes.Rate1));

                    target1 = tuple.GetValue<int>(ServerQuestsAttributes.MobId1);

                    if (target1 != 0)
                        builder.AppendLine("\t\tMobId: " + target1);

                    builder.AppendLine("\t},");
                }

                if (target2 != 0)
                {
                    builder.AppendLine("\t{");
                    builder.AppendLine("\t\tItemId: " + target2);
                    builder.AppendLine("\t\tRate: " + tuple.GetValue<int>(ServerQuestsAttributes.Rate2));

                    target2 = tuple.GetValue<int>(ServerQuestsAttributes.MobId2);

                    if (target2 != 0)
                        builder.AppendLine("\t\tMobId: " + target2);

                    builder.AppendLine("\t},");
                }

                if (target3 != 0)
                {
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

        public static void Writer<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db)
        {
            if (debug.FileType == FileType.Yaml)
            {
                var itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);
                var mobDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);

                try
                {
                    var lines = new YamlParser(debug.OldPath, ParserMode.Write, "Id");

                    if (lines.Output == null)
                        return;

                    lines.Remove(db, v => DbIOUtils.Id2Name(mobDb, ServerMobAttributes.AegisName, v.ToString()));

                    foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<int>()))
                    {
                        string key = tuple.Key.ToString();

                        StringBuilder builder = new StringBuilder();
                        WriteEntryYaml(builder, tuple, itemDb, mobDb);
                        lines.Write(key, builder.ToString().Trim('\r', '\n'));
                    }

                    lines.WriteFile(debug.FilePath);
                }
                catch (Exception err)
                {
                    debug.ReportException(err);
                }
            }
            else if (debug.FileType == FileType.Conf)
            {
                try
                {
                    var lines = new LibconfigParser(debug.OldPath, ParserMode.Write);
                    lines.Remove(db);
                    string line;

                    foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>()))
                    {
                        int key = tuple.GetKey<int>();
                        StringBuilder builder = new StringBuilder();
                        WriteEntry(builder, tuple);
                        line = builder.ToString();
                        lines.Write(key.ToString(CultureInfo.InvariantCulture), line);
                    }

                    lines.WriteFile(debug.FilePath);
                }
                catch (Exception err)
                {
                    debug.ReportException(err);
                }
            }
            else
            {
                int format = db.GetAttacked<int>("rAthenaFormat");

                try
                {
                    IntLineStream lines = new IntLineStream(debug.OldPath);
                    lines.Remove(db);
                    string line;

                    foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>()))
                    {
                        int key = tuple.GetKey<int>();

                        if (format == 18)
                        {
                            line = string.Join(",", tuple.GetRawElements().Take(18).Select(p => (p ?? "").ToString()).ToArray());
                        }
                        else
                        {
                            line = string.Join(",", tuple.GetRawElements().Take(ServerQuestsAttributes.MobId1.Index).Concat(new object[] { tuple.GetRawValue(ServerQuestsAttributes.QuestTitle.Index) }).Select(p => (p ?? "").ToString()).ToArray());
                        }

                        lines.Write(key, line);
                    }

                    lines.WriteFile(debug.FilePath);
                }
                catch (Exception err)
                {
                    debug.ReportException(err);
                }
            }
        }
    }
}