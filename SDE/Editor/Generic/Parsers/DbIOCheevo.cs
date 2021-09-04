using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.Parsers.Libconfig;
using SDE.Editor.Engines.Parsers.Yaml;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using System;
using System.Globalization;
using System.Text;

namespace SDE.Editor.Generic.Parsers
{
    public sealed class DbIOCheevo
    {
        public static void Loader(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            if (debug.FileType == FileType.Yaml)
            {
                var ele = new YamlParser(debug.FilePath);
                var table = debug.AbsractDb.Table;
                var attributeList = debug.AbsractDb.AttributeList;

                if (ele.Output == null || ((ParserArray)ele.Output).Objects.Count == 0 || (ele.Output["copy_paste"] ?? ele.Output["Body"]) == null)
                    return;

                foreach (var achievement in ele.Output["copy_paste"] ?? ele.Output["Body"])
                {
                    try
                    {
                        int cheevoId = Int32.Parse((achievement["ID"] ?? achievement["Id"]).ObjectValue);

                        table.SetRaw(cheevoId, ServerCheevoAttributes.Name, achievement["Name"] ?? "");
                        table.SetRaw(cheevoId, ServerCheevoAttributes.GroupId, achievement["Group"] ?? "");
                        table.SetRaw(cheevoId, ServerCheevoAttributes.Score, achievement["Score"] ?? "");
                        table.SetRaw(cheevoId, ServerCheevoAttributes.Map, achievement["Map"] ?? "");

                        if (achievement["Reward"] != null)
                        {
                            var reward = achievement["Reward"];
                            table.SetRaw(cheevoId, ServerCheevoAttributes.RewardId, reward["ItemID"] ?? "");
                            table.SetRaw(cheevoId, ServerCheevoAttributes.RewardAmount, reward["Amount"] ?? "");
                            table.SetRaw(cheevoId, ServerCheevoAttributes.RewardTitleId, reward["TitleID"] ?? "");
                            table.SetRaw(cheevoId, ServerCheevoAttributes.RewardScript, (reward["Script"] ?? "").Trim(' ', '\t'));
                        }

                        if (achievement["Target"] != null)
                        {
                            int targetId;

                            foreach (var target in achievement["Target"])
                            {
                                targetId = Int32.Parse(target["Id"] ?? "0");
                                table.SetRaw(cheevoId, attributeList[ServerCheevoAttributes.TargetId1.Index + targetId], target["MobID"] ?? "");
                                table.SetRaw(cheevoId, attributeList[ServerCheevoAttributes.TargetCount1.Index + targetId], target["Count"] ?? "");
                            }
                        }

                        if (achievement["Condition"] != null)
                        {
                            table.SetRaw(cheevoId, ServerCheevoAttributes.Condition, achievement["Condition"].ObjectValue.Trim(' ', '\t'));
                        }

                        if (achievement["Dependent"] != null)
                        {
                            int id;
                            bool first = true;
                            StringBuilder dependency = new StringBuilder();

                            foreach (var target in achievement["Dependent"])
                            {
                                id = Int32.Parse(target["Id"] ?? "0");

                                if (first)
                                {
                                    dependency.Append(id);
                                    first = false;
                                }
                                else
                                {
                                    dependency.Append(":" + id);
                                }
                            }

                            table.SetRaw(cheevoId, ServerCheevoAttributes.Dependent, dependency.ToString());
                        }
                    }
                    catch
                    {
                        if ((achievement["ID"] ?? achievement["Id"]) == null)
                        {
                            if (!debug.ReportIdException("#", achievement.Line)) return;
                        }
                        else if (!debug.ReportIdException((achievement["ID"] ?? achievement["Id"]), achievement.Line)) return;
                    }
                }
            }
            else if (debug.FileType == FileType.Conf)
            {
                var ele = new LibconfigParser(debug.FilePath);
                var table = debug.AbsractDb.Table;
                var attributeList = debug.AbsractDb.AttributeList;

                foreach (var achievement in ele.Output["copy_paste"] ?? ele.Output["achievement_db"])
                {
                    int cheevoId = Int32.Parse(achievement["id"].ObjectValue);

                    table.SetRaw(cheevoId, ServerCheevoAttributes.Name, achievement["name"] ?? "");
                    table.SetRaw(cheevoId, ServerCheevoAttributes.GroupId, achievement["group"] ?? "");
                    table.SetRaw(cheevoId, ServerCheevoAttributes.Score, achievement["score"] ?? "");

                    if (achievement["reward"] != null)
                    {
                        foreach (var reward in achievement["reward"])
                        {
                            table.SetRaw(cheevoId, ServerCheevoAttributes.RewardId, reward["itemid"] ?? "");
                            table.SetRaw(cheevoId, ServerCheevoAttributes.RewardAmount, reward["amount"] ?? "");
                            table.SetRaw(cheevoId, ServerCheevoAttributes.RewardTitleId, reward["titleid"] ?? "");
                            table.SetRaw(cheevoId, ServerCheevoAttributes.RewardScript, (reward["script"] ?? "").Trim(' ', '\t'));
                            break;
                        }
                    }

                    if (achievement["target"] != null)
                    {
                        int targetId = 0;

                        foreach (var target in achievement["target"])
                        {
                            table.SetRaw(cheevoId, attributeList[ServerCheevoAttributes.TargetId1.Index + targetId], target["mobid"] ?? "");
                            table.SetRaw(cheevoId, attributeList[ServerCheevoAttributes.TargetCount1.Index + targetId], target["count"] ?? "");
                            targetId += 2;
                        }
                    }

                    if (achievement["condition"] != null)
                    {
                        table.SetRaw(cheevoId, ServerCheevoAttributes.Condition, achievement["condition"].ObjectValue);
                    }

                    if (achievement["dependent"] != null)
                    {
                        int id = 0;
                        StringBuilder dependency = new StringBuilder();

                        foreach (var target in achievement["dependent"])
                        {
                            if (id == 0)
                            {
                                dependency.Append(target.ObjectValue);
                            }
                            else
                            {
                                dependency.Append(":" + target.ObjectValue);
                            }

                            id++;
                        }

                        table.SetRaw(cheevoId, ServerCheevoAttributes.Dependent, dependency.ToString());
                    }
                }
            }
        }

        public static void Writer(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            if (debug.FileType == FileType.Conf)
                DbIOMethods.DbIOWriter(debug, db, WriteEntry);
            else if (debug.FileType == FileType.Yaml)
                DbIOMethods.DbIOWriter(debug, db, WriteEntryYaml);
        }

        public static void WriteEntry(StringBuilder builder, ReadableTuple<int> tuple)
        {
            if (tuple != null)
            {
                string valueS;

                builder.AppendLine("{");
                builder.AppendLine("\tid: " + tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture));
                builder.AppendLine("\tgroup: \"" + tuple.GetValue<string>(ServerCheevoAttributes.GroupId) + "\"");
                builder.AppendLine("\tname: \"" + tuple.GetValue<string>(ServerCheevoAttributes.Name) + "\"");

                string condition = tuple.GetValue<string>(ServerCheevoAttributes.Condition);

                if (!String.IsNullOrEmpty(condition))
                {
                    builder.AppendLine("\tcondition: \"" + condition.Trim('\t', ' ') + "\"");
                }

                if (!String.IsNullOrEmpty(valueS = tuple.GetValue<string>(ServerCheevoAttributes.Dependent)))
                {
                    builder.AppendLine("\tdependent: [" + valueS.Replace(":", ", ") + "]");
                }

                int rewardId = tuple.GetValue<int>(ServerCheevoAttributes.RewardId);
                int amountId = tuple.GetValue<int>(ServerCheevoAttributes.RewardAmount);
                string script = tuple.GetValue<string>(ServerCheevoAttributes.RewardScript);
                int titleId = tuple.GetValue<int>(ServerCheevoAttributes.RewardTitleId);

                if (rewardId > 0 || !String.IsNullOrEmpty(script) || titleId > 0)
                {
                    builder.AppendLine("\treward: (");
                    builder.AppendLine("\t{");

                    if (rewardId > 0)
                    {
                        builder.AppendLine("\t\titemid: " + rewardId);
                    }

                    if (amountId > 1)
                    {
                        builder.AppendLine("\t\tamount: " + amountId);
                    }

                    if (!String.IsNullOrEmpty(script))
                    {
                        builder.AppendLine("\t\tscript: \" " + script.Trim(' ') + " \"");
                    }

                    if (titleId > 0)
                    {
                        builder.AppendLine("\t\ttitleid: " + titleId);
                    }

                    builder.AppendLine("\t}");
                    builder.AppendLine("\t)");
                }

                int count = 0;

                for (int i = 0; i < 10; i++)
                {
                    count += (tuple.GetValue<int>(ServerCheevoAttributes.TargetId1.Index + i) != 0) ? 1 : 0;
                }

                if (count > 0)
                {
                    int total = 0;
                    builder.AppendLine("\ttarget: (");

                    for (int i = 0; i < 10; i += 2)
                    {
                        int mobId = tuple.GetValue<int>(ServerCheevoAttributes.TargetId1.Index + i);
                        int targetCount = tuple.GetValue<int>(ServerCheevoAttributes.TargetId1.Index + i + 1);

                        if (mobId != 0 || targetCount != 0)
                        {
                            builder.AppendLine("\t{");

                            if (mobId != 0)
                            {
                                builder.AppendLine("\t\tmobid: " + mobId);
                                total++;
                            }

                            if (targetCount != 0)
                            {
                                builder.AppendLine("\t\tcount: " + targetCount);
                                total++;
                            }

                            builder.AppendLine(total != count ? "\t}," : "\t}");
                        }
                    }

                    builder.AppendLine("\t)");
                }

                builder.AppendLine("\tscore: " + tuple.GetValue<int>(ServerCheevoAttributes.Score));
                builder.Append("},");
            }
        }

        public static void WriteEntryYaml(StringBuilder builder, ReadableTuple<int> tuple)
        {
            if (tuple != null)
            {
                string valueS;

                builder.AppendLine("  - Id: " + tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture));
                builder.AppendLine("    Group: \"" + tuple.GetValue<string>(ServerCheevoAttributes.GroupId) + "\"");
                builder.AppendLine("    Name: \"" + tuple.GetValue<string>(ServerCheevoAttributes.Name) + "\"");

                if (tuple.GetValue<string>(ServerCheevoAttributes.Map) != "")
                {
                    builder.Append("    Map: \"" + tuple.GetValue<string>(ServerCheevoAttributes.Map) + "\"");
                }

                string condition = tuple.GetValue<string>(ServerCheevoAttributes.Condition);

                if (!String.IsNullOrEmpty(condition))
                {
                    builder.AppendLine("    Condition: \"" + condition.Trim('\t', ' ') + "\"");
                }

                if (!String.IsNullOrEmpty(valueS = tuple.GetValue<string>(ServerCheevoAttributes.Dependent)))
                {
                    builder.AppendLine("    Dependent: [" + valueS.Replace(":", ", ") + "]");
                }

                int rewardId = tuple.GetValue<int>(ServerCheevoAttributes.RewardId);
                int amountId = tuple.GetValue<int>(ServerCheevoAttributes.RewardAmount);
                string script = tuple.GetValue<string>(ServerCheevoAttributes.RewardScript);
                int titleId = tuple.GetValue<int>(ServerCheevoAttributes.RewardTitleId);

                if (rewardId > 0 || !String.IsNullOrEmpty(script) || titleId > 0)
                {
                    builder.AppendLine("    Reward:");

                    if (rewardId > 0)
                    {
                        builder.AppendLine("      ItemID: " + rewardId);
                    }

                    if (amountId > 1)
                    {
                        builder.AppendLine("      Amount: " + amountId);
                    }

                    if (!String.IsNullOrEmpty(script))
                    {
                        builder.AppendLine("      Script: \" " + script.Trim(' ') + " \"");
                    }

                    if (titleId > 0)
                    {
                        builder.AppendLine("      TitleID: " + titleId);
                    }
                }

                int count = 0;

                for (int i = 0; i < 10; i++)
                {
                    count += (tuple.GetValue<int>(ServerCheevoAttributes.TargetId1.Index + i) != 0) ? 1 : 0;
                }

                if (count > 0)
                {
                    builder.AppendLine("    Target:");

                    for (int i = 0; i < 10; i += 2)
                    {
                        int mobId = tuple.GetValue<int>(ServerCheevoAttributes.TargetId1.Index + i);
                        int targetCount = tuple.GetValue<int>(ServerCheevoAttributes.TargetId1.Index + i + 1);

                        if (mobId != 0 || targetCount != 0)
                        {
                            bool addedList = false;

                            if (mobId != 0)
                            {
                                builder.AppendLine("      - MobID: " + mobId);
                                addedList = true;
                            }

                            if (targetCount != 0)
                            {
                                if (!addedList)
                                {
                                    builder.AppendLine("      - Count: " + targetCount);
                                }
                                else
                                {
                                    builder.AppendLine("        Count: " + targetCount);
                                }
                            }
                        }
                    }
                }

                builder.Append("    Score: " + tuple.GetValue<int>(ServerCheevoAttributes.Score));
            }
        }
    }
}