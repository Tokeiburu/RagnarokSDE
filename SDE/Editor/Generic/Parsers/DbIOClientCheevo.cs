using Database;
using ErrorManager;
using GRF.FileFormats.LubFormat;
using GRF.IO;
using Lua;
using Lua.Structure;
using SDE.ApplicationConfiguration;
using SDE.Core;
using SDE.Editor.Engines.BackupsEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Utilities;
using Utilities.Services;

namespace SDE.Editor.Generic.Parsers
{
    public sealed class DbIOClientCheevo
    {
        public static void Loader(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            Loader(db, ProjectConfiguration.ClientCheevo);
        }

        public static void Loader(AbstractDb<int> db, string file)
        {
            if (file == null)
            {
                Debug.Ignore(() => DbDebugHelper.OnUpdate(db.DbSource, null, "achievement_list table will not be loaded."));
                return;
            }

            LuaList list;

            var table = db.Table;
            var metaGrf = db.ProjectDatabase.MetaGrf;

            string outputPath = GrfPath.Combine(SdeAppConfiguration.TempPath, Path.GetFileName(file));

            byte[] itemData = metaGrf.GetData(file);

            if (itemData == null)
            {
                Debug.Ignore(() => DbDebugHelper.OnUpdate(db.DbSource, file, "File not found."));
                return;
            }

            File.WriteAllBytes(outputPath, itemData);

            if (!File.Exists(outputPath))
                return;

            if (Methods.ByteArrayCompare(itemData, 0, 4, new byte[] { 0x1b, 0x4c, 0x75, 0x61 }, 0))
            {
                // Decompile lub file
                Lub lub = new Lub(itemData);
                var text = lub.Decompile();
                itemData = EncodingService.DisplayEncoding.GetBytes(text);
                File.WriteAllBytes(outputPath, itemData);
            }

            DbIOMethods.DetectAndSetEncoding(itemData);

            using (LuaReader reader = new LuaReader(outputPath, DbIOMethods.DetectedEncoding))
            {
                list = reader.ReadAll();
            }

            LuaKeyValue itemVariable = list.Variables[0] as LuaKeyValue;

            if (itemVariable != null && itemVariable.Key == "achievement_tbl")
            {
                LuaList items = itemVariable.Value as LuaList;

                if (items != null)
                {
                    foreach (LuaKeyValue item in items.Variables)
                    {
                        _loadEntry(table, item);
                    }
                }
            }
            else
            {
                // Possible copy-paste data
                foreach (LuaKeyValue item in list.Variables)
                {
                    _loadEntry(table, item);
                }
            }

            Debug.Ignore(() => DbDebugHelper.OnLoaded(db.DbSource, metaGrf.FindTkPath(file), db));
        }

        private static void _loadEntry(Table<int, ReadableTuple<int>> table, LuaKeyValue item)
        {
            int itemIndex = Int32.Parse(item.Key.Substring(1, item.Key.Length - 2));
            LuaList itemProperties = item.Value as LuaList;
            LuaList contentProperties;
            StringBuilder resources;

            if (itemProperties != null)
            {
                foreach (LuaKeyValue itemProperty in itemProperties.Variables)
                {
                    switch (itemProperty.Key)
                    {
                        case "UI_Type":
                            table.SetRaw(itemIndex, ClientCheevoAttributes.UiType, ((LuaValue)itemProperty.Value).Value);
                            break;

                        case "group":
                            table.SetRaw(itemIndex, ClientCheevoAttributes.GroupId, DbIOMethods.RemoveQuotes(((LuaValue)itemProperty.Value).Value));
                            break;

                        case "major":
                            table.SetRaw(itemIndex, ClientCheevoAttributes.Major, ((LuaValue)itemProperty.Value).Value);
                            break;

                        case "minor":
                            table.SetRaw(itemIndex, ClientCheevoAttributes.Minor, ((LuaValue)itemProperty.Value).Value);
                            break;

                        case "title":
                            table.SetRaw(itemIndex, ClientCheevoAttributes.Name, DbIOMethods.RemoveQuotes(((LuaValue)itemProperty.Value).Value));
                            break;

                        case "reward":
                        case "content":
                            contentProperties = itemProperty.Value as LuaList;

                            if (contentProperties == null)
                                continue;

                            try
                            {
                                foreach (LuaKeyValue contentProperty in contentProperties.Variables)
                                {
                                    switch (contentProperty.Key)
                                    {
                                        case "summary":
                                            table.SetRaw(itemIndex, ClientCheevoAttributes.Summary, DbIOMethods.RemoveQuotes(((LuaValue)contentProperty.Value).Value));
                                            break;

                                        case "details":
                                            table.SetRaw(itemIndex, ClientCheevoAttributes.Details, DbIOMethods.RemoveQuotes(((LuaValue)contentProperty.Value).Value));
                                            break;

                                        case "title":
                                            table.SetRaw(itemIndex, ClientCheevoAttributes.RewardTitleId, ((LuaValue)contentProperty.Value).Value);
                                            break;

                                        case "buff":
                                            table.SetRaw(itemIndex, ClientCheevoAttributes.RewardBuff, ((LuaValue)contentProperty.Value).Value);
                                            break;

                                        case "item":
                                            table.SetRaw(itemIndex, ClientCheevoAttributes.RewardId, ((LuaValue)contentProperty.Value).Value);
                                            break;
                                    }
                                }
                            }
                            catch (Exception err)
                            {
                                ErrorHandler.HandleException(err);
                            }

                            break;

                        case "resource":
                            resources = new StringBuilder();

                            contentProperties = itemProperty.Value as LuaList;

                            if (contentProperties == null)
                                continue;

                            foreach (LuaKeyValue contentProperty in contentProperties.Variables)
                            {
                                try
                                {
                                    int resId;

                                    if (!Int32.TryParse(contentProperty.Key.Substring(1, contentProperty.Key.Length - 2), out resId))
                                    {
                                        DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(),
                                            String.Format("ID: {0}, file: '{1}', exception: '{2}'",
                                            itemIndex.ToString(CultureInfo.InvariantCulture),
                                            TextFileHelper.LatestFile,
                                            "Invalid resource ID, found \"" + contentProperty.Key + "\", expected an integer."), ErrorLevel.Warning);
                                        continue;
                                    }

                                    LuaList resourceProperties = contentProperty.Value as LuaList;

                                    if (resourceProperties == null)
                                        continue;

                                    resources.Append(resId);

                                    foreach (LuaKeyValue resourceProperty in resourceProperties.Variables)
                                    {
                                        string value = ((LuaValue)resourceProperty.Value).Value;
                                        resources.Append("__%");
                                        resources.Append(resourceProperty.Key);
                                        resources.Append("__%");

                                        if (value.StartsWith("\""))
                                            resources.Append(DbIOMethods.RemoveQuotes(value));
                                        else
                                            resources.Append(value);
                                    }

                                    resources.Append("__&");
                                }
                                catch (Exception err)
                                {
                                    throw new Exception("Failed to read resource for ID " + itemIndex, err);
                                }
                            }

                            table.SetRaw(itemIndex, ClientCheevoAttributes.Resources, resources.ToString());
                            break;

                        case "score":
                            table.SetRaw(itemIndex, ClientCheevoAttributes.Score, ((LuaValue)itemProperty.Value).Value);
                            break;
                    }
                }
            }
        }

        public static void Writer(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            var gdb = db.ProjectDatabase;
            string filename = ProjectConfiguration.ClientCheevo;

            if (gdb.MetaGrf.GetData(filename) == null)
            {
                Debug.Ignore(() => DbDebugHelper.OnWriteStatusUpdate(ServerDbs.CItems, filename, null, "Achievement table not saved."));
                return;
            }

            BackupEngine.Instance.BackupClient(filename, gdb.MetaGrf);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("achievement_tbl = {");

            List<ReadableTuple<int>> tuples = gdb.GetDb<int>(ServerDbs.CCheevo).Table.GetSortedItems().ToList();
            ReadableTuple<int> tuple;

            for (int index = 0, count = tuples.Count; index < count; index++)
            {
                tuple = tuples[index];
                WriteEntry(builder, tuple, index == count - 1);
            }

            builder.AppendLine("}");
            builder.AppendLine();
            builder.AppendLine(ResourceString.Get("AchievementFunction"));

            gdb.MetaGrf.SetData(filename, EncodingService.Ansi.GetBytes(builder.ToString()));

            Debug.Ignore(() => DbDebugHelper.OnWriteStatusUpdate(ServerDbs.CItems, gdb.MetaGrf.FindTkPath(filename), null, "Saving Achievement table."));
        }

        public static void WriteEntry(StringBuilder builder, ReadableTuple<int> tuple, bool end = false)
        {
            builder.Append("\t[");
            builder.Append(tuple.GetValue<int>(0));
            builder.AppendLine("] = {");

            builder.Append("\t\tUI_Type = ");
            builder.Append(tuple.GetValue<string>(ClientCheevoAttributes.UiType));
            builder.AppendLine(",");

            builder.Append("\t\tgroup = \"");
            builder.Append(tuple.GetValue<string>(ClientCheevoAttributes.GroupId));
            builder.AppendLine("\",");

            builder.Append("\t\tmajor = ");
            builder.Append(tuple.GetValue<string>(ClientCheevoAttributes.Major));
            builder.AppendLine(",");

            builder.Append("\t\tminor = ");
            builder.Append(tuple.GetValue<string>(ClientCheevoAttributes.Minor));
            builder.AppendLine(",");

            builder.Append("\t\ttitle = \"");
            builder.Append(tuple.GetValue<string>(ClientCheevoAttributes.Name));
            builder.AppendLine("\",");

            builder.AppendLine("\t\tcontent = {");
            builder.Append("\t\t\tsummary = \"");
            builder.Append(tuple.GetValue<string>(ClientCheevoAttributes.Summary));
            builder.AppendLine("\",");

            builder.Append("\t\t\tdetails = \"");
            builder.Append(tuple.GetValue<string>(ClientCheevoAttributes.Details));
            builder.AppendLine("\",");
            builder.AppendLine("\t\t},");

            string val = tuple.GetValue<string>(ClientCheevoAttributes.Resources);

            if (val != null)
            {
                builder.AppendLine("\t\tresource = {");
                string[] resources = val.Split(new string[] { "__&" }, StringSplitOptions.None);

                foreach (var resource in resources)
                {
                    if (String.IsNullOrEmpty(resource))
                        continue;

                    string[] resValues = resource.Split(new string[] { "__%" }, StringSplitOptions.None);

                    int id = Int32.Parse(resValues[0]);

                    builder.AppendLine("\t\t\t[" + id + "] = {");

                    for (int j = 1; j < resValues.Length; j += 2)
                    {
                        string param = resValues[j];
                        int ival;
                        string value;

                        if (Int32.TryParse(resValues[j + 1], out ival))
                        {
                            value = "" + ival;
                        }
                        else
                        {
                            value = "\"" + resValues[j + 1] + "\"";
                        }

                        builder.AppendFormat("\t\t\t\t{0} = {1},", param, value);
                        builder.AppendLine();
                    }

                    builder.AppendLine("\t\t\t},");
                }

                builder.AppendLine("\t\t},");
            }

            int rewardTitle = tuple.GetValue<int>(ClientCheevoAttributes.RewardTitleId);
            int rewardItem = tuple.GetValue<int>(ClientCheevoAttributes.RewardId);
            int rewardBuff = tuple.GetValue<int>(ClientCheevoAttributes.RewardBuff);

            if (rewardTitle > 0 || rewardItem > 0 || rewardBuff > 0)
            {
                builder.AppendLine("\t\treward = {");

                if (rewardTitle > 0)
                {
                    builder.Append("\t\t\ttitle = ");
                    builder.Append(rewardTitle);
                    builder.AppendLine(",");
                }

                if (rewardBuff > 0)
                {
                    builder.Append("\t\t\tbuff = ");
                    builder.Append(rewardBuff);
                    builder.AppendLine(",");
                }

                if (rewardItem > 0)
                {
                    builder.Append("\t\t\titem = ");
                    builder.Append(rewardItem);
                    builder.AppendLine(",");
                }

                builder.AppendLine("\t\t},");
            }
            else
            {
                builder.AppendLine("\t\treward = {},");
            }

            builder.AppendFormat("\t\tscore = {0},", tuple.GetValue<int>(ClientCheevoAttributes.Score));
            builder.AppendLine();
            builder.AppendLine(end ? "\t}" : "\t},");
        }
    }
}