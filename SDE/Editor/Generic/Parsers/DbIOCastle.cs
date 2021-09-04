using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.Parsers.Libconfig;
using SDE.Editor.Engines.Parsers.Yaml;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SDE.Editor.Generic.Parsers
{
    public sealed class DbIOCastle
    {
        public static void Loader(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            if (debug.FileType == FileType.Yaml)
            {
                var ele = new YamlParser(debug.FilePath);
                var table = debug.AbsractDb.Table;

                if (ele.Output == null || ((ParserArray)ele.Output).Objects.Count == 0 || (ele.Output["copy_paste"] ?? ele.Output["Body"]) == null)
                    return;

                foreach (var castle in ele.Output["copy_paste"] ?? ele.Output["Body"])
                {
                    int castleId = (int)(object)Int32.Parse(castle["Id"]);

                    table.SetRaw(castleId, ServerCastleAttributes.MapName, castle["Map"] ?? "");
                    table.SetRaw(castleId, ServerCastleAttributes.CastleName, castle["Name"] ?? "");
                    table.SetRaw(castleId, ServerCastleAttributes.NpcName, castle["Npc"] ?? "");
                }
            }
            else if (debug.FileType == FileType.Txt)
            {
                DbIOMethods.DbLoaderComma(debug, db);
            }
        }

        public static void Writer(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            if (debug.FileType == FileType.Txt)
            {
                DbIOMethods.DbWriterComma(debug, db);
            }
            else if (debug.FileType == FileType.Yaml)
            {
                try
                {
                    var lines = new YamlParser(debug.OldPath, ParserMode.Write, "Id");

                    if (lines.Output == null)
                        return;

                    lines.Remove(db);

                    foreach (ReadableTuple<int> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<int>()))
                    {
                        string key = tuple.Key.ToString(CultureInfo.InvariantCulture);

                        StringBuilder builder = new StringBuilder();
                        WriteEntryYaml(builder, tuple);
                        lines.Write(key, builder.ToString().Trim('\r', '\n'));
                    }

                    lines.WriteFile(debug.FilePath);
                }
                catch (Exception err)
                {
                    debug.ReportException(err);
                }
            }
        }

        public static void WriteEntryYaml(StringBuilder builder, ReadableTuple<int> tuple)
        {
            if (tuple != null)
            {
                string valueS;

                builder.AppendLine("  - Id: " + tuple.Key);
                builder.AppendLine("    Map: " + (String.IsNullOrEmpty((valueS = tuple.GetValue<string>(ServerCastleAttributes.MapName))) ? "SDE_NULL" : valueS));
                builder.AppendLine("    Name: " + (String.IsNullOrEmpty((valueS = tuple.GetValue<string>(ServerCastleAttributes.CastleName))) ? "SDE_NULL" : valueS));
                builder.AppendLine("    Npc: " + (String.IsNullOrEmpty((valueS = tuple.GetValue<string>(ServerCastleAttributes.NpcName))) ? "SDE_NULL" : valueS));
            }
        }
    }
}