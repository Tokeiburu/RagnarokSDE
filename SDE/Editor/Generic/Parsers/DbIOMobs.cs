using ErrorManager;
using SDE.Core;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.Parsers.Libconfig;
using SDE.Editor.Engines.Parsers.Yaml;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.View;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Utilities;

namespace SDE.Editor.Generic.Parsers
{
    public sealed class DbIOMobs
    {
        public static void Loader<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db)
        {
            if (debug.FileType == FileType.Txt)
            {
                DbIOMethods.DbLoaderAny(debug, db, TextFileHelper.GetElementsByCommas);
            }
            else if (debug.FileType == FileType.Conf)
            {
                DbIOMethods.GuessAttributes(new string[] { }, db.AttributeList.Attributes.ToList(), -1, db);

                var ele = new LibconfigParser(debug.FilePath);
                var table = debug.AbsractDb.Table;

                foreach (var parser in ele.Output["copy_paste"] ?? ele.Output["mob_db"])
                {
                    TKey itemId = (TKey)(object)Int32.Parse(parser["Id"]);

                    table.SetRaw(itemId, ServerMobAttributes.AegisName, parser["SpriteName"].ObjectValue);
                    table.SetRaw(itemId, ServerMobAttributes.IRoName, parser["Name"].ObjectValue);
                    table.SetRaw(itemId, ServerMobAttributes.KRoName, parser["Name"].ObjectValue);
                    table.SetRaw(itemId, ServerMobAttributes.Lv, parser["Lv"] ?? "1");
                    table.SetRaw(itemId, ServerMobAttributes.Hp, parser["Hp"] ?? "1");
                    table.SetRaw(itemId, ServerMobAttributes.Sp, parser["Sp"] ?? "0");
                    table.SetRaw(itemId, ServerMobAttributes.Exp, parser["Exp"] ?? "0");
                    table.SetRaw(itemId, ServerMobAttributes.JExp, parser["JExp"] ?? "0");
                    table.SetRaw(itemId, ServerMobAttributes.AttackRange, parser["AttackRange"] ?? "1");
                    table.SetRaw(itemId, ServerMobAttributes.Atk1, Extensions.ParseBracket(parser["Attack"] ?? "[0, 0]", 0));
                    table.SetRaw(itemId, ServerMobAttributes.Atk2, (Int32.Parse(Extensions.ParseBracket(parser["Attack"] ?? "[0, 0]", 0)) + Int32.Parse(Extensions.ParseBracket(parser["Attack"] ?? "[0, 0]", 1))).ToString(CultureInfo.InvariantCulture));
                    table.SetRaw(itemId, ServerMobAttributes.Def, parser["Def"] ?? "0");
                    table.SetRaw(itemId, ServerMobAttributes.Mdef, parser["Mdef"] ?? "0");
                    table.SetRaw(itemId, ServerMobAttributes.Str, parser["Stats.Str"] ?? "0");
                    table.SetRaw(itemId, ServerMobAttributes.Agi, parser["Stats.Agi"] ?? "0");
                    table.SetRaw(itemId, ServerMobAttributes.Vit, parser["Stats.Vit"] ?? "0");
                    table.SetRaw(itemId, ServerMobAttributes.Int, parser["Stats.Int"] ?? "0");
                    table.SetRaw(itemId, ServerMobAttributes.Dex, parser["Stats.Dex"] ?? "0");
                    table.SetRaw(itemId, ServerMobAttributes.Luk, parser["Stats.Luk"] ?? "0");
                    table.SetRaw(itemId, ServerMobAttributes.ViewRange, parser["ViewRange"] ?? "1");
                    table.SetRaw(itemId, ServerMobAttributes.ChaseRange, parser["ChaseRange"] ?? "1");
                    table.SetRaw(itemId, ServerMobAttributes.Size, SdeEditor.Instance.ProjectDatabase.ConstantToInt(parser["Size"] ?? "1").ToString(CultureInfo.InvariantCulture));
                    table.SetRaw(itemId, ServerMobAttributes.Race, SdeEditor.Instance.ProjectDatabase.ConstantToInt(parser["Race"] ?? "0").ToString(CultureInfo.InvariantCulture));

                    var element = parser["Element"];
                    int elLevel;
                    int elType;

                    if (element != null)
                    {
                        debug.AbsractDb.Attached["MobDb.UseConstants"] = true;
                        elLevel = Int32.Parse(((ParserList)element).Objects[1].ObjectValue);
                        elType = SdeEditor.Instance.ProjectDatabase.ConstantToInt(((ParserList)element).Objects[0].ObjectValue);
                    }
                    else
                    {
                        elLevel = 0;
                        elType = 0;
                    }

                    table.SetRaw(itemId, ServerMobAttributes.Element, ((elLevel) * 20 + elType).ToString(CultureInfo.InvariantCulture));

                    table.SetRaw(itemId, ServerMobAttributes.Mode, (
                        (!Boolean.Parse((parser["Mode.CanMove"] ?? "false")) ? 0 : (1 << 0)) |
                        (!Boolean.Parse((parser["Mode.Looter"] ?? "false")) ? 0 : (1 << 1)) |
                        (!Boolean.Parse((parser["Mode.Aggressive"] ?? "false")) ? 0 : (1 << 2)) |
                        (!Boolean.Parse((parser["Mode.Assist"] ?? "false")) ? 0 : (1 << 3)) |
                        (!Boolean.Parse((parser["Mode.CastSensorIdle"] ?? "false")) ? 0 : (1 << 4)) |
                        (!Boolean.Parse((parser["Mode.Boss"] ?? "false")) ? 0 : (1 << 5)) |
                        (!Boolean.Parse((parser["Mode.Plant"] ?? "false")) ? 0 : (1 << 6)) |
                        (!Boolean.Parse((parser["Mode.CanAttack"] ?? "false")) ? 0 : (1 << 7)) |
                        (!Boolean.Parse((parser["Mode.Detector"] ?? "false")) ? 0 : (1 << 8)) |
                        (!Boolean.Parse((parser["Mode.CastSensorChase"] ?? "false")) ? 0 : (1 << 9)) |
                        (!Boolean.Parse((parser["Mode.ChangeChase"] ?? "false")) ? 0 : (1 << 10)) |
                        (!Boolean.Parse((parser["Mode.Angry"] ?? "false")) ? 0 : (1 << 11)) |
                        (!Boolean.Parse((parser["Mode.ChangeTargetMelee"] ?? "false")) ? 0 : (1 << 12)) |
                        (!Boolean.Parse((parser["Mode.ChangeTargetChase"] ?? "false")) ? 0 : (1 << 13)) |
                        (!Boolean.Parse((parser["Mode.TargetWeak"] ?? "false")) ? 0 : (1 << 14))
                        ).ToString(CultureInfo.InvariantCulture));

                    table.SetRaw(itemId, ServerMobAttributes.MoveSpeed, parser["MoveSpeed"] ?? "0");
                    table.SetRaw(itemId, ServerMobAttributes.AttackDelay, parser["AttackDelay"] ?? "4000");
                    table.SetRaw(itemId, ServerMobAttributes.AttackMotion, parser["AttackMotion"] ?? "2000");
                    table.SetRaw(itemId, ServerMobAttributes.DamageMotion, parser["DamageMotion"] ?? "0");
                    table.SetRaw(itemId, ServerMobAttributes.MvpExp, parser["MvpExp"] ?? "0");

                    int id = 0;

                    var mvpDrops = parser["MvpDrops"];

                    if (mvpDrops != null)
                    {
                        foreach (var drop in mvpDrops)
                        {
                            if (id > 2)
                            {
                                debug.ReportIdException("Too many MVP mob drops.", itemId, ErrorLevel.Critical);
                                break;
                            }

                            int tItemId = SdeDatabase.AegisNameToId(debug, itemId, ((ParserKeyValue)drop).Key);
                            table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Mvp1ID.Index + 2 * id], tItemId.ToString(CultureInfo.InvariantCulture));
                            table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Mvp1ID.Index + 2 * id + 1], drop.ObjectValue);
                            id++;
                        }
                    }

                    id = 0;

                    var regularDrops = parser["Drops"];

                    if (regularDrops != null)
                    {
                        foreach (var drop in regularDrops)
                        {
                            if (id > 8)
                            {
                                debug.ReportIdException("Too regular mob drops.", itemId, ErrorLevel.Critical);
                                break;
                            }

                            int tItemId = SdeDatabase.AegisNameToId(debug, itemId, ((ParserKeyValue)drop).Key);
                            var tuple = SdeDatabase.GetTuple(debug, tItemId);

                            if (tuple != null && tuple.GetValue<TypeType>(ServerItemAttributes.Type) == TypeType.Card)
                            {
                                table.SetRaw(itemId, ServerMobAttributes.DropCardid, tItemId.ToString(CultureInfo.InvariantCulture));
                                table.SetRaw(itemId, ServerMobAttributes.DropCardper, drop.ObjectValue);
                                id++;
                                continue;
                            }

                            table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Drop1ID.Index + 2 * id], tItemId.ToString(CultureInfo.InvariantCulture));
                            table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Drop1ID.Index + 2 * id + 1], drop.ObjectValue);
                            id++;
                        }
                    }
                }
            }
            else if (debug.FileType == FileType.Yaml)
            {
                try
                {
                    DbIOMethods.GuessAttributes(new string[] { }, db.AttributeList.Attributes.ToList(), -1, db);

                    var ele = new YamlParser(debug.FilePath);
                    var table = debug.AbsractDb.Table;

                    if (ele.Output == null || ((ParserArray)ele.Output).Objects.Count == 0)
                        return;

                    if ((ele.Output["copy_paste"] ?? ele.Output["Body"]) == null)
                        return;

                    DbIOUtils.ClearBuffer();
                    var mobDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);
                    var itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

                    foreach (var parser in ele.Output["copy_paste"] ?? ele.Output["Body"])
                    {
                        TKey itemId = (TKey)(object)Int32.Parse(parser["Id"]);

                        table.SetRaw(itemId, ServerMobAttributes.AegisName, parser["AegisName"].ObjectValue);
                        table.SetRaw(itemId, ServerMobAttributes.IRoName, parser["Name"].ObjectValue);
                        table.SetRaw(itemId, ServerMobAttributes.KRoName, (parser["JapaneseName"] ?? parser["Name"]).ObjectValue);
                        table.SetRaw(itemId, ServerMobAttributes.Lv, parser["Level"] ?? "1");
                        table.SetRaw(itemId, ServerMobAttributes.Hp, parser["Hp"] ?? "1");
                        table.SetRaw(itemId, ServerMobAttributes.Sp, parser["Sp"] ?? "1");
                        table.SetRaw(itemId, ServerMobAttributes.Exp, parser["BaseExp"] ?? "0");
                        table.SetRaw(itemId, ServerMobAttributes.JExp, parser["JobExp"] ?? "0");
                        table.SetRaw(itemId, ServerMobAttributes.MvpExp, parser["MvpExp"] ?? "0");
                        table.SetRaw(itemId, ServerMobAttributes.Atk1, parser["Attack"] ?? "0");
                        table.SetRaw(itemId, ServerMobAttributes.Atk2, parser["Attack2"] ?? "0");

                        table.SetRaw(itemId, ServerMobAttributes.Def, parser["Defense"] ?? "0");
                        table.SetRaw(itemId, ServerMobAttributes.Mdef, parser["MagicDefense"] ?? "0");

                        table.SetRaw(itemId, ServerMobAttributes.Str, parser["Str"] ?? "1");
                        table.SetRaw(itemId, ServerMobAttributes.Agi, parser["Agi"] ?? "1");
                        table.SetRaw(itemId, ServerMobAttributes.Vit, parser["Vit"] ?? "1");
                        table.SetRaw(itemId, ServerMobAttributes.Int, parser["Int"] ?? "1");
                        table.SetRaw(itemId, ServerMobAttributes.Dex, parser["Dex"] ?? "1");
                        table.SetRaw(itemId, ServerMobAttributes.Luk, parser["Luk"] ?? "1");

                        table.SetRaw(itemId, ServerMobAttributes.AttackRange, parser["AttackRange"] ?? "0");
                        table.SetRaw(itemId, ServerMobAttributes.ViewRange, parser["SkillRange"] ?? "0");
                        table.SetRaw(itemId, ServerMobAttributes.ChaseRange, parser["ChaseRange"] ?? "0");

                        table.SetRaw(itemId, ServerMobAttributes.Size, DbIOUtils.LoadFlag<SizeType>(parser["Size"], "0"));
                        table.SetRaw(itemId, ServerMobAttributes.Race, DbIOUtils.LoadFlag<MobRaceType>(parser["Race"], "0"));
                        table.SetRaw(itemId, ServerMobAttributes.RaceGroups, DbIOUtils.LoadFlag<MobGroup2Type>(parser["RaceGroups"], "0"));

                        var element = parser["Element"];
                        int elLevel;
                        int elType;

                        if (element != null)
                        {
                            switch (element.ObjectValue)
                            {
                                case "Neutral": elType = 0; break;
                                case "Water": elType = 1; break;
                                case "Earth": elType = 2; break;
                                case "Fire": elType = 3; break;
                                case "Wind": elType = 4; break;
                                case "Poison": elType = 5; break;
                                case "Holy": elType = 6; break;
                                case "Dark": elType = 7; break;
                                case "Ghost": elType = 8; break;
                                case "Undead": elType = 9; break;
                                default: elType = 0; break;
                            }
                        }
                        else
                        {
                            elType = 0;
                        }

                        element = parser["ElementLevel"];

                        if (element != null)
                        {
                            elLevel = Int32.Parse(element.ObjectValue);
                        }
                        else
                        {
                            elLevel = 1;
                        }

                        table.SetRaw(itemId, ServerMobAttributes.Element, ((elLevel) * 20 + elType).ToString(CultureInfo.InvariantCulture));

                        table.SetRaw(itemId, ServerMobAttributes.MoveSpeed, parser["WalkSpeed"] ?? "");
                        table.SetRaw(itemId, ServerMobAttributes.AttackDelay, parser["AttackDelay"] ?? "0");
                        table.SetRaw(itemId, ServerMobAttributes.AttackMotion, parser["AttackMotion"] ?? "0");
                        table.SetRaw(itemId, ServerMobAttributes.DamageMotion, parser["DamageMotion"] ?? "0");
                        table.SetRaw(itemId, ServerMobAttributes.DamageTaken, parser["DamageTaken"] ?? "100");

                        long mode = 0;

                        if (parser["Ai"] != null)
                        {
                            int ai = Int32.Parse(parser["Ai"].ObjectValue);

                            switch (ai)
                            {
                                case 1: mode |= 0x81; break;
                                case 2: mode |= 0x83; break;
                                case 3: mode |= 0x1089; break;
                                case 4: mode |= 0x3885; break;
                                case 5: mode |= 0x2085; break;
                                case 6: mode |= 0; break;
                                case 7: mode |= 0x108B; break;
                                case 8: mode |= 0x7085; break;
                                case 9: mode |= 0x3095; break;
                                case 10: mode |= 0x84; break;
                                case 11: mode |= 0x84; break;
                                case 12: mode |= 0x2085; break;
                                case 13: mode |= 0x308D; break;
                                case 17: mode |= 0x91; break;
                                case 19: mode |= 0x3095; break;
                                case 20: mode |= 0x3295; break;
                                case 21: mode |= 0x3695; break;
                                case 24: mode |= 0xA1; break;
                                case 25: mode |= 0x1; break;
                                case 26: mode |= 0xB695; break;
                                case 27: mode |= 0x8084; break;
                            }
                        }

                        mode |= Int64.Parse(DbIOUtils.LoadFlag<NewMobModeType>(parser["Modes"], "0"));

                        table.SetRaw(itemId, ServerMobAttributes.NewMode, mode.ToString(CultureInfo.InvariantCulture));
                        table.SetRaw(itemId, ServerMobAttributes.Class, DbIOUtils.LoadFlag<ClassType>(parser["Class"], "0"));
                        table.SetRaw(itemId, ServerMobAttributes.Sprite, parser["Sprite"] ?? "");

                        int id = 0;

                        var mvpDrops = parser["MvpDrops"];

                        if (mvpDrops != null)
                        {
                            foreach (var drop in mvpDrops)
                            {
                                if (id > 2)
                                {
                                    debug.ReportIdException("Too many MVP mob drops.", itemId, ErrorLevel.Critical);
                                    break;
                                }

                                int tItemId = (int)DbIOUtils.Name2IdBuffered(itemDb, ServerItemAttributes.AegisName, drop["Item"] ?? "", "item_db", false);
                                table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Mvp1ID.Index + 2 * id], tItemId.ToString(CultureInfo.InvariantCulture));
                                table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Mvp1ID.Index + 2 * id + 1], drop["Rate"] ?? "0");

                                if (drop["RandomOptionGroup"] != null)
                                {
                                    table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Mvp1RandomOptionGroup.Index + id], drop["RandomOptionGroup"].ObjectValue);
                                }

                                id++;
                            }
                        }

                        id = 0;

                        var regularDrops = parser["Drops"];

                        if (regularDrops != null)
                        {
                            foreach (var drop in regularDrops)
                            {
                                if (id > 9)
                                {
                                    debug.ReportIdException("Too many regular mob drops.", itemId, ErrorLevel.Critical);
                                    break;
                                }

                                int tItemId = (int)DbIOUtils.Name2IdBuffered(itemDb, ServerItemAttributes.AegisName, drop["Item"] ?? "", "item_db", false);

                                table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Drop1ID.Index + 2 * id], tItemId.ToString(CultureInfo.InvariantCulture));
                                table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Drop1ID.Index + 2 * id + 1], drop["Rate"] ?? "0");

                                if (drop["StealProtected"] != null)
                                {
                                    if (Boolean.Parse(drop["StealProtected"].ObjectValue))
                                    {
                                        table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Drop1Flags.Index + id], "true");
                                    }
                                }

                                if (drop["RandomOptionGroup"] != null)
                                {
                                    table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Drop1RandomOptionGroup.Index + id], drop["RandomOptionGroup"].ObjectValue);
                                }

                                id++;
                            }
                        }
                    }
                }
                finally
                {
                    DbIOUtils.ClearBuffer();
                }
            }
        }

        public static void Writer(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            if (debug.FileType == FileType.Conf)
            {
                DbIOMethods.DbIOWriter(debug, db, (q, r) => WriteEntry(db, q, r));
            }
            else if (debug.FileType == FileType.Txt)
            {
                DbIOMethods.DbWriterComma(debug, db);
            }
            else if (debug.FileType == FileType.Yaml)
            {
                var itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);
                DbIOMethods.DbIOWriter(debug, db, (r, q) => WriteEntryYaml(r, q, itemDb));
            }
        }

        public static void WriteEntry(BaseDb db, StringBuilder builder, ReadableTuple<int> tuple)
        {
            bool useConstants = db.Attached["MobDb.UseConstants"] != null && (bool)db.Attached["MobDb.UseConstants"];

            builder.AppendLine("{");
            builder.AppendLine("\tId: " + tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("\tSpriteName: \"" + tuple.GetValue<string>(ServerMobAttributes.AegisName) + "\"");
            builder.AppendLine("\tName: \"" + tuple.GetValue<string>(ServerMobAttributes.KRoName) + "\"");

            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Lv, "1");
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Hp, "1");
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Sp, "0");
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Exp, "0");
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.JExp, "0");
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.AttackRange, "1");
            DbIOFormatting.TrySetAttack(tuple, builder);
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Def, "0");
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Mdef, "0");

            int stat = 0;

            for (int i = 0; i < 6; i++)
            {
                stat += tuple.GetIntNoThrow(ServerMobAttributes.Str.Index + i);
            }

            if (stat != 0)
            {
                builder.AppendLine("	Stats: {");

                for (int i = 0; i < 6; i++)
                {
                    stat = tuple.GetIntNoThrow(ServerMobAttributes.Str.Index + i);

                    if (stat != 0)
                    {
                        builder.AppendLine(String.Format("		{0}: {1}", ServerMobAttributes.AttributeList[ServerMobAttributes.Str.Index + i].AttributeName, stat));
                    }
                }

                builder.AppendLine("	}");
            }

            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.ViewRange, "1");
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.ChaseRange, "1");
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Size, "1", useConstants, "Size_");
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Race, "0", useConstants, "RC_");

            int element = tuple.GetIntNoThrow(ServerMobAttributes.Element);
            int level = element / 10;
            int property = element - level * 10;
            level = level / 2;

            if (useConstants)
            {
                builder.AppendLine("\tElement: (\"" + SdeEditor.Instance.ProjectDatabase.IntToConstant(property, "Ele_") + "\", " + level + ")");
            }
            else
            {
                builder.AppendLine("\tElement: (" + property + ", " + level + ")");
            }

            int mode = tuple.GetIntNoThrow(ServerMobAttributes.Mode);

            if ((mode & 32767) != 0)
            {
                builder.AppendLine("	Mode: {");

                if ((mode & (1 << 0)) == (1 << 0)) builder.AppendLine("		CanMove: true");
                if ((mode & (1 << 1)) == (1 << 1)) builder.AppendLine("		Looter: true");
                if ((mode & (1 << 2)) == (1 << 2)) builder.AppendLine("		Aggressive: true");
                if ((mode & (1 << 3)) == (1 << 3)) builder.AppendLine("		Assist: true");
                if ((mode & (1 << 4)) == (1 << 4)) builder.AppendLine("		CastSensorIdle: true");
                if ((mode & (1 << 5)) == (1 << 5)) builder.AppendLine("		Boss: true");
                if ((mode & (1 << 6)) == (1 << 6)) builder.AppendLine("		Plant: true");
                if ((mode & (1 << 7)) == (1 << 7)) builder.AppendLine("		CanAttack: true");
                if ((mode & (1 << 8)) == (1 << 8)) builder.AppendLine("		Detector: true");
                if ((mode & (1 << 9)) == (1 << 9)) builder.AppendLine("		CastSensorChase: true");
                if ((mode & (1 << 10)) == (1 << 10)) builder.AppendLine("		ChangeChase: true");
                if ((mode & (1 << 11)) == (1 << 11)) builder.AppendLine("		Angry: true");
                if ((mode & (1 << 12)) == (1 << 12)) builder.AppendLine("		ChangeTargetMelee: true");
                if ((mode & (1 << 13)) == (1 << 13)) builder.AppendLine("		ChangeTargetChase: true");
                if ((mode & (1 << 14)) == (1 << 14)) builder.AppendLine("		TargetWeak: true");

                builder.AppendLine("	}");
            }

            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.MoveSpeed, "0");
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.AttackDelay, "4000");
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.AttackMotion, "2000");
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.DamageMotion, "0");
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.MvpExp, "0");

            stat = 0;

            for (int i = 0; i < 6; i += 2)
            {
                stat += tuple.GetIntNoThrow(ServerMobAttributes.Mvp1ID.Index + i);
            }

            if (stat != 0)
            {
                builder.AppendLine("	MvpDrops: {");

                for (int i = 0; i < 6; i += 2)
                {
                    stat = tuple.GetIntNoThrow(ServerMobAttributes.Mvp1ID.Index + i);

                    if (stat != 0)
                    {
                        var ttuple = SdeDatabase.GetTuple(null, stat);

                        if (ttuple != null)
                            builder.AppendLine(String.Format("		{0}: {1}", ttuple.GetStringValue(ServerItemAttributes.AegisName.Index), tuple.GetIntNoThrow(ServerMobAttributes.Mvp1ID.Index + i + 1)));
                    }
                }

                builder.AppendLine("	}");
            }

            stat = 0;

            for (int i = 0; i < 18; i += 2)
            {
                stat += tuple.GetIntNoThrow(ServerMobAttributes.Drop1ID.Index + i);
            }

            if (stat != 0)
            {
                builder.AppendLine("	Drops: {");

                for (int i = 0; i < 18; i += 2)
                {
                    stat = tuple.GetIntNoThrow(ServerMobAttributes.Drop1ID.Index + i);

                    if (stat != 0)
                    {
                        var ttuple = SdeDatabase.GetTuple(null, stat);

                        if (ttuple != null)
                            builder.AppendLine(String.Format("		{0}: {1}", ttuple.GetStringValue(ServerItemAttributes.AegisName.Index), tuple.GetIntNoThrow(ServerMobAttributes.Drop1ID.Index + i + 1)));
                    }
                }

                stat = tuple.GetIntNoThrow(ServerMobAttributes.DropCardid);

                if (stat != 0)
                {
                    var ttuple = SdeDatabase.GetTuple(null, stat);

                    if (ttuple != null)
                        builder.AppendLine(String.Format("		{0}: {1}", ttuple.GetStringValue(ServerItemAttributes.AegisName.Index), tuple.GetIntNoThrow(ServerMobAttributes.DropCardper.Index)));
                }

                builder.AppendLine("	}");
            }

            builder.Append("},");
        }

        public static void WriteEntryYaml(StringBuilder builder, ReadableTuple<int> tuple, MetaTable<int> itemDb)
        {
            if (tuple != null)
            {
                builder.AppendLine("  - Id: " + tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture));
                builder.AppendLine("    AegisName: " + tuple.GetValue<string>(ServerMobAttributes.AegisName));
                builder.AppendLine("    Name: " + DbIOUtils.QuoteCheck(tuple.GetValue<string>(ServerMobAttributes.IRoName)));

                if (tuple.GetValue<string>(ServerMobAttributes.IRoName) != tuple.GetValue<string>(ServerMobAttributes.KRoName))
                {
                    builder.AppendLine("    JapaneseName: " + DbIOUtils.QuoteCheck(tuple.GetValue<string>(ServerMobAttributes.KRoName)));
                }

                int value;
                //bool valueB;
                string valueS;
                //int type = 0;

                builder.AppendLine("    Level: " + tuple.GetValue<int>(ServerMobAttributes.Lv));

                if ((value = tuple.GetValue<int>(ServerMobAttributes.Hp)) != 1)
                {
                    builder.AppendLine("    Hp: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.Sp)) != 1)
                {
                    builder.AppendLine("    Sp: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.Exp)) != 0)
                {
                    builder.AppendLine("    BaseExp: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.JExp)) != 0)
                {
                    builder.AppendLine("    JobExp: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.MvpExp)) != 0)
                {
                    builder.AppendLine("    MvpExp: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.Atk1)) != 0)
                {
                    builder.AppendLine("    Attack: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.Atk2)) != 0)
                {
                    builder.AppendLine("    Attack2: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.Def)) != 0)
                {
                    builder.AppendLine("    Defense: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.Mdef)) != 0)
                {
                    builder.AppendLine("    MagicDefense: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.Str)) != 1)
                {
                    builder.AppendLine("    Str: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.Agi)) != 1)
                {
                    builder.AppendLine("    Agi: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.Vit)) != 1)
                {
                    builder.AppendLine("    Vit: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.Int)) != 1)
                {
                    builder.AppendLine("    Int: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.Dex)) != 1)
                {
                    builder.AppendLine("    Dex: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.Luk)) != 1)
                {
                    builder.AppendLine("    Luk: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.AttackRange)) != 0)
                {
                    builder.AppendLine("    AttackRange: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.ViewRange)) != 0)
                {
                    builder.AppendLine("    SkillRange: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.ChaseRange)) != 0)
                {
                    builder.AppendLine("    ChaseRange: " + value);
                }

                //if ((value = tuple.GetValue<int>(ServerMobAttributes.Size)) != 0) {
                builder.AppendLine("    Size: " + Constants.ToString<SizeType>(tuple.GetValue<int>(ServerMobAttributes.Size)));
                //}

                builder.AppendLine("    Race: " + Constants.ToString<MobRaceType>(tuple.GetValue<int>(ServerMobAttributes.Race)));

                DbIOUtils.ExpandFlag<MobGroup2Type>(builder, tuple, "RaceGroups", ServerMobAttributes.RaceGroups, YamlParser.Indent4, YamlParser.Indent6);

                int elementLevel = tuple.GetValue<int>(ServerMobAttributes.Element) / 20;
                int element = tuple.GetValue<int>(ServerMobAttributes.Element) % 20;

                switch (element)
                {
                    case 0: builder.AppendLine("    Element: Neutral"); break;
                    case 1: builder.AppendLine("    Element: Water"); break;
                    case 2: builder.AppendLine("    Element: Earth"); break;
                    case 3: builder.AppendLine("    Element: Fire"); break;
                    case 4: builder.AppendLine("    Element: Wind"); break;
                    case 5: builder.AppendLine("    Element: Poison"); break;
                    case 6: builder.AppendLine("    Element: Holy"); break;
                    case 7: builder.AppendLine("    Element: Dark"); break;
                    case 8: builder.AppendLine("    Element: Ghost"); break;
                    case 9: builder.AppendLine("    Element: Undead"); break;
                }

                builder.AppendLine("    ElementLevel: " + elementLevel);

                if ((value = tuple.GetValue<int>(ServerMobAttributes.MoveSpeed)) != 0)
                {
                    builder.AppendLine("    WalkSpeed: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.AttackDelay)) != 0)
                {
                    builder.AppendLine("    AttackDelay: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.AttackMotion)) != 0)
                {
                    builder.AppendLine("    AttackMotion: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.DamageMotion)) != 0)
                {
                    builder.AppendLine("    DamageMotion: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.DamageTaken)) != 100)
                {
                    builder.AppendLine("    DamageTaken: " + value);
                }

                long mode = FormatConverters.LongOrHexConverter(tuple.GetValue<string>(ServerMobAttributes.NewMode));

                if ((mode & 0xB695) == 0xB695)
                {
                    builder.AppendLine("    Ai: 26");
                    mode &= ~0xB695;
                }
                else if ((mode & 0x8084) == 0x8084)
                {
                    builder.AppendLine("    Ai: 27");
                    mode &= ~0x8084;
                }
                else if ((mode & 0x7085) == 0x7085)
                {
                    builder.AppendLine("    Ai: 08");
                    mode &= ~0x7085;
                }
                else if ((mode & 0x3885) == 0x3885)
                {
                    builder.AppendLine("    Ai: 04");
                    mode &= ~0x3885;
                }
                else if ((mode & 0x3695) == 0x3695)
                {
                    builder.AppendLine("    Ai: 21");
                    mode &= ~0x3695;
                }
                else if ((mode & 0x3295) == 0x3295)
                {
                    builder.AppendLine("    Ai: 20");
                    mode &= ~0x3295;
                }
                else if ((mode & 0x3095) == 0x3095)
                {
                    builder.AppendLine("    Ai: 09");
                    mode &= ~0x3095;
                }
                else if ((mode & 0x308D) == 0x308D)
                {
                    builder.AppendLine("    Ai: 13");
                    mode &= ~0x308D;
                }
                else if ((mode & 0x2085) == 0x2085)
                {
                    builder.AppendLine("    Ai: 05");
                    mode &= ~0x2085;
                }
                else if ((mode & 0x108B) == 0x108B)
                {
                    builder.AppendLine("    Ai: 07");
                    mode &= ~0x108B;
                }
                else if ((mode & 0x1089) == 0x1089)
                {
                    builder.AppendLine("    Ai: 03");
                    mode &= ~0x1089;
                }
                else if ((mode & 0xA1) == 0xA1)
                {
                    builder.AppendLine("    Ai: 24");
                    mode &= ~0xA1;
                }
                else if ((mode & 0x91) == 0x91)
                {
                    builder.AppendLine("    Ai: 17");
                    mode &= ~0x91;
                }
                else if ((mode & 0x84) == 0x84)
                {
                    builder.AppendLine("    Ai: 10");
                    mode &= ~0x84;
                }
                else if ((mode & 0x83) == 0x83)
                {
                    builder.AppendLine("    Ai: 02");
                    mode &= ~0x83;
                }
                else if ((mode & 0x81) == 0x81)
                {
                    builder.AppendLine("    Ai: 01");
                    mode &= ~0x81;
                }
                else if ((mode & 0x1) == 0x1)
                {
                    builder.AppendLine("    Ai: 25");
                    mode &= ~0x1;
                }

                if ((value = tuple.GetValue<int>(ServerMobAttributes.Class)) != 0)
                {
                    builder.AppendLine("    Class: " + Constants.ToString<ClassType>(value));
                }

                if (mode > 0)
                {
                    builder.AppendLine("    Modes:");
                    if ((mode & 0x1) == 0x1)
                    {
                        builder.AppendLine("      CanMove: true");
                    }
                    if ((mode & 0x80) == 0x80)
                    {
                        builder.AppendLine("      CanAttack: true");
                    }
                    if ((mode & 0x40) == 0x40)
                    {
                        builder.AppendLine("      NoCast: true");
                    }
                    if ((mode & 0x2) == 0x2)
                    {
                        builder.AppendLine("      Looter: true");
                    }
                    if ((mode & 0x4) == 0x4)
                    {
                        builder.AppendLine("      Aggressive: true");
                    }
                    if ((mode & 0x8) == 0x8)
                    {
                        builder.AppendLine("      Assist: true");
                    }
                    if ((mode & 0x20) == 0x20)
                    {
                        builder.AppendLine("      NoRandomWalk: true");
                    }
                    if ((mode & 0x200) == 0x200)
                    {
                        builder.AppendLine("      CastSensorChase: true");
                    }
                    if ((mode & 0x10) == 0x10)
                    {
                        builder.AppendLine("      CastSensorIdle: true");
                    }
                    if ((mode & 0x800) == 0x800)
                    {
                        builder.AppendLine("      Angry: true");
                    }
                    if ((mode & 0x400) == 0x400)
                    {
                        builder.AppendLine("      ChangeChase: true");
                    }
                    if ((mode & 0x1000) == 0x1000)
                    {
                        builder.AppendLine("      ChangeTargetMelee: true");
                    }
                    if ((mode & 0x2000) == 0x2000)
                    {
                        builder.AppendLine("      ChangeTargetChase: true");
                    }
                    if ((mode & 0x4000) == 0x4000)
                    {
                        builder.AppendLine("      TargetWeak: true");
                    }
                    if ((mode & 0x8000) == 0x8000)
                    {
                        builder.AppendLine("      RandomTarget: true");
                    }
                    if ((mode & 0x20000) == 0x20000)
                    {
                        builder.AppendLine("      IgnoreMagic: true");
                    }
                    if ((mode & 0x10000) == 0x10000)
                    {
                        builder.AppendLine("      IgnoreMelee: true");
                    }
                    if ((mode & 0x100000) == 0x100000)
                    {
                        builder.AppendLine("      IgnoreMisc: true");
                    }
                    if ((mode & 0x40000) == 0x40000)
                    {
                        builder.AppendLine("      IgnoreRanged: true");
                    }
                    if ((mode & 0x400000) == 0x400000)
                    {
                        builder.AppendLine("      TeleportBlock: true");
                    }
                    if ((mode & 0x1000000) == 0x1000000)
                    {
                        builder.AppendLine("      FixedItemDrop: true");
                    }
                    if ((mode & 0x2000000) == 0x2000000)
                    {
                        builder.AppendLine("      Detector: true");
                    }
                    if ((mode & 0x200000) == 0x200000)
                    {
                        builder.AppendLine("      KnockBackImmune: true");
                    }
                    if ((mode & 0x4000000) == 0x4000000)
                    {
                        builder.AppendLine("      StatusImmune: true");
                    }
                    if ((mode & 0x8000000) == 0x8000000)
                    {
                        builder.AppendLine("      SkillImmune: true");
                    }
                    if ((mode & 0x80000) == 0x80000)
                    {
                        builder.AppendLine("      Mvp: true");
                    }
                }

                int stat = 0;

                for (int i = 0; i < 6; i += 2)
                {
                    stat += tuple.GetIntNoThrow(ServerMobAttributes.Mvp1ID.Index + i);
                }

                if (stat != 0)
                {
                    builder.AppendLine("    MvpDrops:");

                    for (int i = 0; i < 6; i += 2)
                    {
                        valueS = tuple.GetValue<string>(ServerMobAttributes.Mvp1ID.Index + i);

                        if (stat != 0 && valueS != "0" && valueS != "")
                        {
                            builder.AppendLine("      - Item: " + DbIOUtils.Id2Name(itemDb, ServerItemAttributes.AegisName, valueS));
                            builder.AppendLine("        Rate: " + tuple.GetValue<int>(ServerMobAttributes.Mvp1ID.Index + i + 1));

                            if ((valueS = tuple.GetValue<string>(ServerMobAttributes.Mvp1RandomOptionGroup.Index + (i / 2))) != "")
                            {
                                builder.AppendLine("        RandomOptionGroup: " + valueS);
                            }
                        }
                    }
                }

                stat = 0;

                for (int i = 0; i < 20; i += 2)
                {
                    stat += tuple.GetIntNoThrow(ServerMobAttributes.Drop1ID.Index + i);
                }

                if (stat != 0)
                {
                    builder.AppendLine("    Drops:");

                    for (int i = 0; i < 20; i += 2)
                    {
                        valueS = tuple.GetValue<string>(ServerMobAttributes.Drop1ID.Index + i);

                        if (stat != 0 && valueS != "0" && valueS != "")
                        {
                            builder.AppendLine("      - Item: " + DbIOUtils.Id2Name(itemDb, ServerItemAttributes.AegisName, valueS));
                            builder.AppendLine("        Rate: " + tuple.GetValue<int>(ServerMobAttributes.Drop1ID.Index + i + 1));

                            if ((valueS = tuple.GetValue<string>(ServerMobAttributes.Drop1Flags.Index + (i / 2))) != "" && valueS != null)
                            {
                                if (FormatConverters.BooleanConverter(valueS))
                                {
                                    builder.AppendLine("        StealProtected: true");
                                }
                            }

                            if ((valueS = tuple.GetValue<string>(ServerMobAttributes.Drop1RandomOptionGroup.Index + (i / 2))) != "")
                            {
                                builder.AppendLine("        RandomOptionGroup: " + valueS);
                            }
                        }
                    }
                }
            }
        }
    }
}