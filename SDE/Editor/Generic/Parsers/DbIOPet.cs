using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.Parsers.Libconfig;
using SDE.Editor.Engines.Parsers.Yaml;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.View;
using SDE.View.ObjectView;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SDE.Editor.Generic.Parsers
{
    public sealed class DbIOPet
    {
        public static void Loader(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            if (debug.FileType == FileType.Yaml)
            {
                var ele = new YamlParser(debug.FilePath);
                var table = debug.AbsractDb.Table;

                if (ele.Output == null || ((ParserArray)ele.Output).Objects.Count == 0 || (ele.Output["copy_paste"] ?? ele.Output["Body"]) == null)
                    return;

                var mobDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);
                var itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

                foreach (var pet in ele.Output["copy_paste"] ?? ele.Output["Body"])
                {
                    string mob = pet["Mob"] ?? "";

                    int mobId = (int)DbIOUtils.Name2Id(mobDb, ServerMobAttributes.AegisName, mob, "mob_db", false);

                    table.SetRaw(mobId, ServerPetAttributes.LureId, DbIOUtils.Name2Id(itemDb, ServerItemAttributes.AegisName, pet["TameItem"] ?? "", "item_db", true));
                    table.SetRaw(mobId, ServerPetAttributes.Name, mob);
                    table.SetRaw(mobId, ServerPetAttributes.EggId, DbIOUtils.Name2Id(itemDb, ServerItemAttributes.AegisName, pet["EggItem"] ?? "", "item_db", true));
                    table.SetRaw(mobId, ServerPetAttributes.EquipId, DbIOUtils.Name2Id(itemDb, ServerItemAttributes.AegisName, pet["EquipItem"] ?? "", "item_db", true));
                    table.SetRaw(mobId, ServerPetAttributes.FoodId, DbIOUtils.Name2Id(itemDb, ServerItemAttributes.AegisName, pet["FoodItem"] ?? "", "item_db", true));
                    table.SetRaw(mobId, ServerPetAttributes.Fullness, pet["Fullness"] ?? "");
                    table.SetRaw(mobId, ServerPetAttributes.HungryDelay, pet["HungryDelay"] ?? ServerPetAttributes.HungryDelay.Default.ToString());
                    table.SetRaw(mobId, ServerPetAttributes.HungerIncrease, pet["HungerIncrease"] ?? ServerPetAttributes.HungerIncrease.Default.ToString());

                    table.SetRaw(mobId, ServerPetAttributes.IntimacyStart, pet["IntimacyStart"] ?? ServerPetAttributes.IntimacyStart.Default.ToString());
                    table.SetRaw(mobId, ServerPetAttributes.IntimacyFed, pet["IntimacyFed"] ?? ServerPetAttributes.IntimacyFed.Default.ToString());
                    table.SetRaw(mobId, ServerPetAttributes.IntimacyOverfed, pet["IntimacyOverfed"] ?? ServerPetAttributes.IntimacyOverfed.Default.ToString());
                    table.SetRaw(mobId, ServerPetAttributes.IntimacyHungry, pet["IntimacyHungry"] ?? ServerPetAttributes.IntimacyHungry.Default.ToString());
                    table.SetRaw(mobId, ServerPetAttributes.IntimacyOwnerDie, pet["Intimacy.OwnerDie"] ?? ServerPetAttributes.IntimacyOwnerDie.Default.ToString());
                    table.SetRaw(mobId, ServerPetAttributes.CaptureRate, pet["CaptureRate"] ?? ServerPetAttributes.CaptureRate.Default.ToString());

                    table.SetRaw(mobId, ServerPetAttributes.SpecialPerformance, (pet["SpecialPerformance"] ?? "true") == "true" ? "1" : "0");
                    table.SetRaw(mobId, ServerPetAttributes.AttackRate, pet["AttackRate"] ?? "");
                    table.SetRaw(mobId, ServerPetAttributes.RetaliateRate, pet["RetaliateRate"] ?? "");
                    table.SetRaw(mobId, ServerPetAttributes.ChangeTargetRate, pet["ChangeTargetRate"] ?? "");
                    table.SetRaw(mobId, ServerPetAttributes.AllowAutoFeed, pet["AllowAutoFeed"] ?? "");
                    table.SetRaw(mobId, ServerPetAttributes.LoyalScript, pet["Script"] ?? "");
                    table.SetRaw(mobId, ServerPetAttributes.PetScript, pet["SupportScript"] ?? "");

                    if (pet["Evolution"] != null)
                    {
                        var evolution = pet["Evolution"];

                        Evolution evolutionObj = new Evolution();

                        foreach (var target in evolution)
                        {
                            EvolutionTarget targetObj = new EvolutionTarget();
                            targetObj.Target = target["Target"];

                            foreach (var requirement in target["ItemRequirements"])
                            {
                                targetObj.ItemRequirements.Add(new Utilities.Extension.Tuple<object, int>(requirement["Item"] ?? "501", Int32.Parse(requirement["Amount"])));
                            }

                            evolutionObj.Targets.Add(targetObj);
                        }

                        table.SetRaw(mobId, ServerPetAttributes.Evolution, evolutionObj.ToString());
                    }
                }
            }
            else if (debug.FileType == FileType.Txt)
            {
                //DbIOMethods.DbLoaderComma(debug, db);
                DbIOMethods.DbLoaderAny(debug, db, TextFileHelper.GetElementsByCommas, true, 25);
                //numberOfAttributesToGuess
            }
        }

        public static void Writer(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            if (debug.FileType == FileType.Txt)
            {
                DbIOMethods.DbWriterComma(debug, db, 0, db.TabGenerator.MaxElementsToCopyInCustomMethods);
                //DbIOMethods.DbWriterComma(debug, db, 0, 22);
            }
            //else if (debug.FileType == FileType.Conf)
            //	DbIOMethods.DbIOWriter(debug, db, WriteEntry);
            else if (debug.FileType == FileType.Yaml)
            {
                var itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);
                var mobDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);

                try
                {
                    var lines = new YamlParser(debug.OldPath, ParserMode.Write, "Mob");

                    if (lines.Output == null)
                        return;

                    lines.Remove(db, v => DbIOUtils.Id2Name(mobDb, ServerMobAttributes.AegisName, v.ToString(CultureInfo.InvariantCulture)));

                    foreach (ReadableTuple<int> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<int>()))
                    {
                        string key = tuple.Key.ToString(CultureInfo.InvariantCulture);

                        StringBuilder builder = new StringBuilder();
                        WriteEntryYaml(builder, tuple, itemDb, mobDb);
                        lines.Write(DbIOUtils.Id2Name(mobDb, ServerMobAttributes.AegisName, key), builder.ToString().Trim('\r', '\n'));
                    }

                    lines.WriteFile(debug.FilePath);
                }
                catch (Exception err)
                {
                    debug.ReportException(err);
                }
            }
        }

        public static void WriteEntryYaml(StringBuilder builder, ReadableTuple<int> tuple, MetaTable<int> itemDb, MetaTable<int> mobDb)
        {
            if (tuple != null)
            {
                string valueS;
                bool valueB;

                builder.AppendLine("  - Mob: " + DbIOUtils.Id2Name(mobDb, ServerMobAttributes.AegisName, tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture)));

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.LureId)) != "" && valueS != "0")
                {
                    builder.AppendLine("    TameItem: " + DbIOUtils.Id2Name(itemDb, ServerItemAttributes.AegisName, valueS));
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.EggId)) != "" && valueS != "0")
                {
                    builder.AppendLine("    EggItem: " + DbIOUtils.Id2Name(itemDb, ServerItemAttributes.AegisName, valueS));
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.EquipId)) != "" && valueS != "0")
                {
                    builder.AppendLine("    EquipItem: " + DbIOUtils.Id2Name(itemDb, ServerItemAttributes.AegisName, valueS));
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.FoodId)) != "" && valueS != "0")
                {
                    builder.AppendLine("    FoodItem: " + DbIOUtils.Id2Name(itemDb, ServerItemAttributes.AegisName, valueS));
                }

                builder.AppendLine("    Fullness: " + tuple.GetValue<int>(ServerPetAttributes.Fullness));

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.HungryDelay)) != "60")
                {
                    builder.AppendLine("    HungryDelay: " + valueS);
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.HungerIncrease)) != "20")
                {
                    builder.AppendLine("    HungerIncrease: " + valueS);
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.IntimacyStart)) != "250")
                {
                    builder.AppendLine("    IntimacyStart: " + valueS);
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.IntimacyFed)) != "50")
                {
                    builder.AppendLine("    IntimacyFed: " + valueS);
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.IntimacyOverfed)) != "-100")
                {
                    builder.AppendLine("    IntimacyOverfed: " + valueS);
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.IntimacyHungry)) != "-5")
                {
                    builder.AppendLine("    IntimacyHungry: " + valueS);
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.IntimacyOwnerDie)) != "-20")
                {
                    builder.AppendLine("    IntimacyOwnerDie: " + valueS);
                }

                builder.AppendLine("    CaptureRate: " + tuple.GetValue<int>(ServerPetAttributes.CaptureRate));

                if ((valueB = tuple.GetValue<bool>(ServerPetAttributes.SpecialPerformance)) == false)
                {
                    builder.AppendLine("    SpecialPerformance: false");
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.AttackRate)) != "" && valueS != "0")
                {
                    builder.AppendLine("    AttackRate: " + valueS);
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.RetaliateRate)) != "" && valueS != "0")
                {
                    builder.AppendLine("    RetaliateRate: " + valueS);
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.ChangeTargetRate)) != "" && valueS != "0")
                {
                    builder.AppendLine("    ChangeTargetRate: " + valueS);
                }

                if ((valueB = tuple.GetValue<bool>(ServerPetAttributes.AllowAutoFeed)) == true)
                {
                    builder.AppendLine("    AllowAutoFeed: true");
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.LoyalScript)) != "" && valueS != "{}")
                {
                    builder.AppendLine("    Script: >");
                    builder.AppendLine(DbIOFormatting.ScriptFormatYaml(valueS, "      "));
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.PetScript)) != "" && valueS != "{}")
                {
                    builder.AppendLine("    SupportScript: >");
                    builder.AppendLine(DbIOFormatting.ScriptFormatYaml(valueS, "      "));
                }

                if ((valueS = tuple.GetValue<string>(ServerPetAttributes.Evolution)) != "" && valueS != "0")
                {
                    builder.AppendLine("    Evolution:");
                    Evolution evolution = new Evolution(valueS);

                    foreach (var evo in evolution.Targets)
                    {
                        builder.Append("      - Target: ");
                        builder.AppendLine(DbIOUtils.Id2Name(mobDb, ServerMobAttributes.AegisName, evo.Target));

                        if (evo.ItemRequirements.Count > 0)
                        {
                            builder.AppendLine("        ItemRequirements:");

                            foreach (var item in evo.ItemRequirements)
                            {
                                builder.Append("          - Item: ");
                                builder.AppendLine(DbIOUtils.Id2Name(itemDb, ServerItemAttributes.AegisName, item.Item1.ToString()));
                                builder.Append("            Amount: ");
                                builder.AppendLine(item.Item2.ToString(CultureInfo.InvariantCulture));
                            }
                        }
                    }
                }
            }
        }
    }
}