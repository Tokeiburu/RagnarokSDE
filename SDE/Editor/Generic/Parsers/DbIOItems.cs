using Database;
using ErrorManager;
using GRF.IO;
using SDE.ApplicationConfiguration;
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
using System.IO;
using System.Linq;
using System.Text;
using Utilities;
using Utilities.Extension;

namespace SDE.Editor.Generic.Parsers
{
    public sealed class DbIOItems
    {
        public delegate void DbCommaFunctionDelegate<T>(DbDebugItem<T> debug, AttributeList list, string[] elements, Table<T, ReadableTuple<T>> table);

        public static void Loader<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db)
        {
            var typeFlags = new Dictionary<string, long>();
            typeFlags["healing"] = 0;
            typeFlags["unknown"] = 1;
            typeFlags["usable"] = 2;
            typeFlags["etc"] = 3;
            typeFlags["armor"] = 4;
            typeFlags["weapon"] = 5;
            typeFlags["card"] = 6;
            typeFlags["petegg"] = 7;
            typeFlags["petarmor"] = 8;
            typeFlags["unknown2"] = 9;
            typeFlags["ammo"] = 10;
            typeFlags["delayconsume"] = 11;
            typeFlags["shadowgear"] = 12;
            typeFlags["cash"] = 18;

            var weaponFlags = new Dictionary<string, long>();
            weaponFlags["fist"] = 0x0;
            weaponFlags["dagger"] = 0x1;
            weaponFlags["1hsword"] = 0x2;
            weaponFlags["2hsword"] = 0x4;
            weaponFlags["1hspear"] = 0x8;
            weaponFlags["2hspear"] = 0x10;
            weaponFlags["1haxe"] = 0x20;
            weaponFlags["2haxe"] = 0x40;
            weaponFlags["mace"] = 0x80;
            weaponFlags["2hmace"] = 0x100;
            weaponFlags["staff"] = 0x200;
            weaponFlags["bow"] = 0x400;
            weaponFlags["knuckle"] = 0x800;
            weaponFlags["musical"] = 0x1000;
            weaponFlags["whip"] = 0x2000;
            weaponFlags["book"] = 0x4000;
            weaponFlags["katar"] = 0x8000;
            weaponFlags["revolver"] = 0x10000;
            weaponFlags["rifle"] = 0x20000;
            weaponFlags["gatling"] = 0x40000;
            weaponFlags["shotgun"] = 0x80000;
            weaponFlags["grenade"] = 0x100000;
            weaponFlags["huuma"] = 0x200000;
            weaponFlags["2hstaff"] = 0x400000;

            var ammoFlags = new Dictionary<string, long>();
            ammoFlags["none"] = 0;
            ammoFlags["arrow"] = 0x1;
            ammoFlags["dagger"] = 0x2;
            ammoFlags["bullet"] = 0x4;
            ammoFlags["shell"] = 0x8;
            ammoFlags["grenade"] = 0x10;
            ammoFlags["shuriken"] = 0x20;
            ammoFlags["kunai"] = 0x40;
            ammoFlags["cannonball"] = 0x80;
            ammoFlags["throwweapon"] = 0x100;

            var genderFlags = new Dictionary<string, long>();
            genderFlags["female"] = 0x0;
            genderFlags["male"] = 0x1;
            genderFlags["both"] = 0x2;

            var eqpFlags = new Dictionary<string, long>();
            eqpFlags["head_low"] = 0x000001;
            eqpFlags["head_mid"] = 0x000200;
            eqpFlags["head_top"] = 0x000100;
            eqpFlags["hand_r"] = 0x000002;
            eqpFlags["hand_l"] = 0x000020;
            eqpFlags["armor"] = 0x000010;
            eqpFlags["shoes"] = 0x000040;
            eqpFlags["garment"] = 0x000004;
            eqpFlags["acc_l"] = 0x000008;
            eqpFlags["acc_r"] = 0x000080;
            eqpFlags["costume_head_top"] = 0x000400;
            eqpFlags["costume_head_mid"] = 0x000800;
            eqpFlags["costume_head_low"] = 0x001000;
            eqpFlags["costume_garment"] = 0x002000;
            eqpFlags["ammo"] = 0x008000;
            eqpFlags["shadow_armor"] = 0x010000;
            eqpFlags["shadow_weapon"] = 0x020000;
            eqpFlags["shadow_shield"] = 0x040000;
            eqpFlags["shadow_shoes"] = 0x080000;
            eqpFlags["shadow_acc_r"] = 0x100000;
            eqpFlags["shadow_acc_l"] = 0x200000;
            eqpFlags["acc_rl"] = 0x000008 | 0x000080;
            eqpFlags["shadow_acc_rl"] = 0x100000 | 0x200000;

            eqpFlags["both_hand"] = 0x000002 | 0x000020;
            eqpFlags["right_hand"] = 0x000002;
            eqpFlags["left_hand"] = 0x000020;
            eqpFlags["right_accessory"] = 0x000080;
            eqpFlags["left_accessory"] = 0x000008;
            eqpFlags["both_accessory"] = 0x000080 | 0x000008;
            eqpFlags["shadow_right_accessory"] = 0x100000;
            eqpFlags["shadow_left_accessory"] = 0x200000;

            var classFlags = new Dictionary<string, long>();
            classFlags["all"] = 0x3f;
            classFlags["none"] = 0;
            classFlags["normal"] = 0x1;
            classFlags["upper"] = 0x2;
            classFlags["baby"] = 0x4;
            classFlags["third"] = 0x8;
            classFlags["third_upper"] = 0x10;
            classFlags["third_baby"] = 0x20;
            classFlags["all_upper"] = 0x02 | 0x10;
            classFlags["all_baby"] = 0x04 | 0x20;
            classFlags["all_third"] = 0x08 | 0x10 | 0x20;

            var dropEffectFlags = new Dictionary<string, int>();
            dropEffectFlags["none"] = (int)DropEffectType.None;
            dropEffectFlags["client"] = (int)DropEffectType.Client;
            dropEffectFlags["white_pillar"] = (int)DropEffectType.White_Pillar;
            dropEffectFlags["blue_pillar"] = (int)DropEffectType.Blue_Pillar;
            dropEffectFlags["yellow_pillar"] = (int)DropEffectType.Yellow_Pillar;
            dropEffectFlags["purple_pillar"] = (int)DropEffectType.Purple_Pillar;
            dropEffectFlags["orange_pillar"] = (int)DropEffectType.Orange_Pillar;
            dropEffectFlags["green_pillar"] = (int)DropEffectType.Green_Pillar;
            dropEffectFlags["red_pillar"] = (int)DropEffectType.Red_Pillar;

            if (debug.FileType == FileType.Txt)
            {
                DbIOMethods.DbLoaderAny(debug, db, TextFileHelper.GetElementsByCommas2);

                if (db.ProjectDatabase.IsRenewal)
                {
                    string val;

                    foreach (var tuple in db.Table.FastItems)
                    {
                        try
                        {
                            val = tuple.GetStringValue(ServerItemAttributes.Attack.Index);

                            if (val != null && val.Contains(":"))
                            {
                                string[] values = val.Split(':');

                                tuple.SetRawValue(ServerItemAttributes.Attack, values[0]);
                                tuple.SetRawValue(ServerItemAttributes.Matk, values[1]);
                            }

                            val = tuple.GetRawValue(ServerItemAttributes.ApplicableJob.Index) as string;

                            if (!string.IsNullOrEmpty(val) && !val.StartsWith("0x") && !val.StartsWith("0X"))
                            {
                                int vval;

                                if (Int32.TryParse(val, out vval))
                                {
                                    val = "0x" + vval.ToString("X");
                                    tuple.SetRawValue(ServerItemAttributes.ApplicableJob, val);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            if (!debug.ReportIdException(tuple.Key)) return;
                        }
                    }
                }
            }
            else if (debug.FileType == FileType.Yaml)
            {
                var ele = new YamlParser(debug.FilePath);
                var table = debug.AbsractDb.Table;

                if (ele.Output == null || ((ParserArray)ele.Output).Objects.Count == 0)
                    return;

                if (ele.Output["Footer"] != null)
                {
                    foreach (var import in ele.Output["Footer.Imports"])
                    {
                        var path = import["Path"];

                        string dbPath = ProjectConfiguration.DatabasePath;
                        dbPath = GrfPath.GetDirectoryName(dbPath);
                        dbPath = GrfPath.GetDirectoryName(dbPath);

                        dbPath = GrfPath.CombineUrl(dbPath, ((string)path).ReplaceAll("/", "\\"));

                        debug.FileType = FileType.Yaml;
                        debug.FilePath = dbPath;
                        DbPathLocator.StoreFile(debug.FilePath);
                        DbDebugHelper.OnLoaded(debug.DbSource, debug.FilePath, db);

                        db.Attached["Import:" + Path.GetFileNameWithoutExtension(dbPath)] = true;

                        var storeCompareList = db.Attached["StoreCompare"] as List<string>;

                        if (storeCompareList == null)
                        {
                            storeCompareList = new List<string>();
                            db.Attached["StoreCompare"] = storeCompareList;
                        }

                        storeCompareList.Add(debug.FilePath);

                        Loader(debug, db);
                    }

                    return;
                }

                if ((ele.Output["copy_paste"] ?? ele.Output["Body"]) == null)
                    return;

                foreach (var item in ele.Output["copy_paste"] ?? ele.Output["Body"])
                {
                    try
                    {
                        TKey itemId = (TKey)(object)Int32.Parse(item["Id"]);

                        var defaultGender = "2";

                        // The .conf is actually quite confusing
                        // Overriding values are not setup for some reason and the parser
                        // has to guess and fix the issues.
                        int ival;
                        if (Int32.TryParse(item["Id"], out ival))
                        {
                            // Whips overrides the default property to 0
                            if (ival >= 1950 && ival < 2000)
                                defaultGender = "0";

                            // Bride_Ring, I'm assuming it's hard coded in the client and
                            // people thought it would be wise to ignore setting its gender
                            if (ival == 2635)
                                defaultGender = "0";

                            // Bridegroom_Ring
                            if (ival == 2634)
                                defaultGender = "1";
                        }

                        table.SetRaw(itemId, ServerItemAttributes.AegisName, item["AegisName"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Name, item["Name"] ?? "");

                        string type = DbIOUtils.LoadFlag(item["Type"], typeFlags, "etc");

                        table.SetRaw(itemId, ServerItemAttributes.Type, type);

                        if (type == "10")
                        {
                            table.SetRaw(itemId, ServerItemAttributes.SubType, DbIOUtils.LoadFlag(item["SubType"], ammoFlags, "0"));
                        }
                        else
                        {
                            table.SetRaw(itemId, ServerItemAttributes.SubType, DbIOUtils.LoadFlag(item["SubType"], weaponFlags, "0"));
                        }

                        table.SetRaw(itemId, ServerItemAttributes.Buy, item["Buy"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Sell, item["Sell"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Weight, item["Weight"] ?? "0");
                        table.SetRaw(itemId, ServerItemAttributes.Attack, item["Attack"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Matk, item["MagicAttack"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Defense, item["Defense"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Range, item["Range"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.NumberOfSlots, item["Slots"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.ApplicableJob, _jobToIdYaml(debug.AbsractDb.To<int>(), item));
                        table.SetRaw(itemId, ServerItemAttributes.Upper, DbIOUtils.LoadFlag(item["Classes"], classFlags, "63"));
                        table.SetRaw(itemId, ServerItemAttributes.Gender, DbIOUtils.LoadFlag(item["Gender"], genderFlags, defaultGender));
                        table.SetRaw(itemId, ServerItemAttributes.Location, DbIOUtils.LoadFlag(item["Locations"], eqpFlags, "0"));
                        table.SetRaw(itemId, ServerItemAttributes.WeaponLevel, item["WeaponLevel"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.EquipLevelMin, item["EquipLevelMin"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.EquipLevelMax, item["EquipLevelMax"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Refineable, item["Refineable"] ?? "false");
                        table.SetRaw(itemId, ServerItemAttributes.ClassNumber, item["View"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.AliasName, item["AliasName"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Flags, DbIOUtils.LoadFlag<ItemFlagType>(item["Flags"], "0"));

                        if (item["Flags.DropEffect"] != null)
                        {
                            table.SetRaw(itemId, ServerItemAttributes.DropEffect, dropEffectFlags[item["Flags.DropEffect"].ObjectValue.ToLowerInvariant()]);
                        }

                        if (item["Delay"] != null)
                        {
                            var delay = item["Delay"];

                            table.SetRaw(itemId, ServerItemAttributes.Delay, delay["Duration"] ?? "");
                            table.SetRaw(itemId, ServerItemAttributes.DelayStatus, delay["Status"] ?? "");
                        }

                        if (item["Stack"] != null)
                        {
                            var stack = item["Stack"];

                            table.SetRaw(itemId, ServerItemAttributes.StackAmount, stack["Amount"] ?? "true");
                            table.SetRaw(itemId, ServerItemAttributes.StackFlags, (
                                (!Boolean.Parse((stack["Inventory"] ?? "false")) ? 0 : (1 << 0)) |
                                (!Boolean.Parse((stack["Cart"] ?? "false")) ? 0 : (1 << 1)) |
                                (!Boolean.Parse((stack["Storage"] ?? "false")) ? 0 : (1 << 2)) |
                                (!Boolean.Parse((stack["GuildStorage"] ?? "false")) ? 0 : (1 << 3))
                                ));
                        }

                        table.SetRaw(itemId, ServerItemAttributes.NoUseOverride, item["NoUse.Override"] ?? "100");
                        table.SetRaw(itemId, ServerItemAttributes.NoUseFlag, (
                            (!Boolean.Parse((item["NoUse.Sitting"] ?? "false")) ? 0 : (1 << 0))
                            ).ToString(CultureInfo.InvariantCulture));

                        table.SetRaw(itemId, ServerItemAttributes.TradeOverride, item["Trade.Override"] ?? "100");
                        table.SetRaw(itemId, ServerItemAttributes.TradeFlag, (
                            (!Boolean.Parse((item["Trade.NoDrop"] ?? "false")) ? 0 : (1 << 0)) |
                            (!Boolean.Parse((item["Trade.NoTrade"] ?? "false")) ? 0 : (1 << 1)) |
                            (!Boolean.Parse((item["Trade.TradePartner"] ?? "false")) ? 0 : (1 << 2)) |
                            (!Boolean.Parse((item["Trade.NoSell"] ?? "false")) ? 0 : (1 << 3)) |
                            (!Boolean.Parse((item["Trade.NoCart"] ?? "false")) ? 0 : (1 << 4)) |
                            (!Boolean.Parse((item["Trade.NoStorage"] ?? "false")) ? 0 : (1 << 5)) |
                            (!Boolean.Parse((item["Trade.NoGuildStorage"] ?? "false")) ? 0 : (1 << 6)) |
                            (!Boolean.Parse((item["Trade.NoMail"] ?? "false")) ? 0 : (1 << 7)) |
                            (!Boolean.Parse((item["Trade.NoAuction"] ?? "false")) ? 0 : (1 << 8))
                            ).ToString(CultureInfo.InvariantCulture));

                        table.SetRaw(itemId, ServerItemAttributes.Script, item["Script"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.OnEquipScript, item["EquipScript"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.OnUnequipScript, item["UnEquipScript"] ?? "");
                    }
                    catch (Exception err)
                    {
                        ErrorHandler.HandleException(err);
                    }
                }
            }
            else if (debug.FileType == FileType.Conf && db.ProjectDatabase.IsNova)
            {
                var ele = new LibconfigParser(debug.FilePath);
                var table = debug.AbsractDb.Table;

                if (ele.Output == null || (ele.Output["copy_paste"] ?? ele.Output["item_db"]) == null)
                    return;

                foreach (var item in ele.Output["copy_paste"] ?? ele.Output["item_db"])
                {
                    try
                    {
                        TKey itemId = (TKey)(object)Int32.Parse(item["Id"]);

                        var defaultGender = "2";

                        // The .conf is actually quite confusing
                        // Overriding values are not setup for some reason and the parser
                        // has to guess and fix the issues.
                        int ival;
                        if (Int32.TryParse(item["Id"], out ival))
                        {
                            // Whips overrides the default property to 0
                            if (ival >= 1950 && ival < 2000)
                                defaultGender = "0";

                            // Bride_Ring, I'm assuming it's hard coded in the client and
                            // people thought it would be wise to ignore setting its gender
                            if (ival == 2635)
                                defaultGender = "0";

                            // Bridegroom_Ring
                            if (ival == 2634)
                                defaultGender = "1";
                        }

                        table.SetRaw(itemId, ServerItemAttributes.AegisName, item["AegisName"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Name, item["Name"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.TempClientName, item["ItemInfoName"] ?? "");

                        string type = DbIOUtils.LoadFlag(item["Type"], typeFlags, "etc");

                        table.SetRaw(itemId, ServerItemAttributes.Type, type);

                        if (type == "10")
                        {
                            table.SetRaw(itemId, ServerItemAttributes.SubType, DbIOUtils.LoadFlag(item["SubType"], ammoFlags, "0"));
                        }
                        else
                        {
                            table.SetRaw(itemId, ServerItemAttributes.SubType, DbIOUtils.LoadFlag(item["SubType"], weaponFlags, "0"));
                        }

                        if (ival == 29932)
                        {
                            Z.F();
                        }

                        table.SetRaw(itemId, ServerItemAttributes.Buy, item["Buy"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Sell, item["Sell"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Weight, item["Weight"] ?? "0");
                        table.SetRaw(itemId, ServerItemAttributes.Attack, item["Attack"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Matk, item["MagicAttack"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Defense, item["Defense"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Range, item["Range"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.NumberOfSlots, item["Slots"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.ApplicableJob, _jobToIdYaml3(debug.AbsractDb.To<int>(), item));
                        table.SetRaw(itemId, ServerItemAttributes.Upper, DbIOUtils.LoadFlag(item["Classes"], classFlags, "63", false));
                        table.SetRaw(itemId, ServerItemAttributes.Gender, DbIOUtils.LoadFlag(item["Gender"], genderFlags, defaultGender, false));
                        table.SetRaw(itemId, ServerItemAttributes.Location, DbIOUtils.LoadFlag(item["Locations"], eqpFlags, "0", false));
                        table.SetRaw(itemId, ServerItemAttributes.WeaponLevel, item["WeaponLevel"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.EquipLevelMin, item["EquipLevelMin"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.EquipLevelMax, item["EquipLevelMax"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Refineable, item["Refineable"] ?? "false");
                        table.SetRaw(itemId, ServerItemAttributes.ClassNumber, item["View"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.AliasName, item["AliasName"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.Flags, DbIOUtils.LoadFlag<ItemFlagType>(item["Flags"], "0"));
                        table.SetRaw(itemId, ServerItemAttributes.CustomFlags, DbIOUtils.LoadFlag<ItemCustomFlagType>(item["CustomFlags"], "0"));
                        table.SetRaw(itemId, ServerItemAttributes.MHFlags, DbIOUtils.LoadFlag<ItemMHFlagType>(item["MH_Data"], "0"));
                        table.SetRaw(itemId, ServerItemAttributes.MHMaxUses, item["MH_Data.MaxUses"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.TempMvpCategory, item["MvpCardTier"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.TempWoEDelay, item["WoEDelay"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.TempExpectedWeight, item["ContainerExpectedWeight"] ?? "");

                        if (item["Flags.DropEffect"] != null)
                        {
                            table.SetRaw(itemId, ServerItemAttributes.DropEffect, dropEffectFlags[item["Flags.DropEffect"].ObjectValue.ToLowerInvariant()]);
                        }

                        if (item["Delay"] != null)
                        {
                            var delay = item["Delay"];

                            table.SetRaw(itemId, ServerItemAttributes.Delay, delay["Duration"] ?? "");
                            table.SetRaw(itemId, ServerItemAttributes.DelayStatus, delay["Status"] ?? "");
                        }

                        if (item["Stack"] != null)
                        {
                            var stack = item["Stack"];

                            table.SetRaw(itemId, ServerItemAttributes.StackAmount, stack["Amount"] ?? "0");
                            table.SetRaw(itemId, ServerItemAttributes.StackFlags, (
                                (!Boolean.Parse((stack["Inventory"] ?? "false")) ? 0 : (1 << 0)) |
                                (!Boolean.Parse((stack["Cart"] ?? "false")) ? 0 : (1 << 1)) |
                                (!Boolean.Parse((stack["Storage"] ?? "false")) ? 0 : (1 << 2)) |
                                (!Boolean.Parse((stack["GuildStorage"] ?? "false")) ? 0 : (1 << 3))
                                ));
                        }

                        table.SetRaw(itemId, ServerItemAttributes.NoUseOverride, item["NoUse.Override"] ?? "100");
                        table.SetRaw(itemId, ServerItemAttributes.NoUseFlag, (
                            (!Boolean.Parse((item["NoUse.Sitting"] ?? "false")) ? 0 : (1 << 0))
                            ).ToString(CultureInfo.InvariantCulture));

                        table.SetRaw(itemId, ServerItemAttributes.TradeOverride, item["Trade.Override"] ?? "100");
                        table.SetRaw(itemId, ServerItemAttributes.TradeFlag, (
                            (!Boolean.Parse((item["Trade.NoDrop"] ?? "false")) ? 0 : (1 << 0)) |
                            (!Boolean.Parse((item["Trade.NoTrade"] ?? "false")) ? 0 : (1 << 1)) |
                            (!Boolean.Parse((item["Trade.TradePartner"] ?? "false")) ? 0 : (1 << 2)) |
                            (!Boolean.Parse((item["Trade.NoSell"] ?? "false")) ? 0 : (1 << 3)) |
                            (!Boolean.Parse((item["Trade.NoCart"] ?? "false")) ? 0 : (1 << 4)) |
                            (!Boolean.Parse((item["Trade.NoStorage"] ?? "false")) ? 0 : (1 << 5)) |
                            (!Boolean.Parse((item["Trade.NoGuildStorage"] ?? "false")) ? 0 : (1 << 6)) |
                            (!Boolean.Parse((item["Trade.NoMail"] ?? "false")) ? 0 : (1 << 7)) |
                            (!Boolean.Parse((item["Trade.NoAuction"] ?? "false")) ? 0 : (1 << 8))
                            ).ToString(CultureInfo.InvariantCulture));

                        table.SetRaw(itemId, ServerItemAttributes.Script, item["Script"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.OnEquipScript, item["OnEquipScript"] ?? "");
                        table.SetRaw(itemId, ServerItemAttributes.OnUnequipScript, item["OnUnequipScript"] ?? "");
                    }
                    catch (Exception err)
                    {
                        ErrorHandler.HandleException(err);
                    }
                }
            }
            else if (debug.FileType == FileType.Conf)
            {
                var ele = new LibconfigParser(debug.FilePath);
                var table = debug.AbsractDb.Table;

                foreach (var item in ele.Output["copy_paste"] ?? ele.Output["item_db"])
                {
                    TKey itemId = (TKey)(object)Int32.Parse(item["Id"]);

                    var defaultGender = "2";

                    // The .conf is actually quite confusing
                    // Overriding values are not setup for some reason and the parser
                    // has to guess and fix the issues.
                    int ival;
                    if (Int32.TryParse(item["Id"], out ival))
                    {
                        // Whips overrides the default property to 0
                        if (ival >= 1950 && ival < 2000)
                            defaultGender = "0";

                        // Bride_Ring, I'm assuming it's hard coded in the client and
                        // people thought it would be wise to ignore setting its gender
                        if (ival == 2635)
                            defaultGender = "0";

                        // Bridegroom_Ring
                        if (ival == 2634)
                            defaultGender = "1";
                    }

                    table.SetRaw(itemId, ServerItemAttributes.AegisName, item["AegisName"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.Name, item["Name"] ?? "");

                    var type = item["Type"] ?? "3";
                    var defaultRefineable = "false";

                    if (type == "4" || type == "5")
                    {
                        defaultRefineable = "true";

                        if (!SdeAppConfiguration.RevertItemTypes)
                        {
                            if (type == "4")
                                type = "5";
                            else if (type == "5")
                                type = "4";
                        }
                    }

                    table.SetRaw(itemId, ServerItemAttributes.Type, type);
                    table.SetRaw(itemId, ServerItemAttributes.Buy, item["Buy"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.Sell, item["Sell"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.Weight, item["Weight"] ?? "0");
                    table.SetRaw(itemId, ServerItemAttributes.Attack, item["Atk"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.Defense, item["Def"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.Range, item["Range"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.NumberOfSlots, item["Slots"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.ApplicableJob, _jobToId(debug.AbsractDb.To<int>(), item));
                    table.SetRaw(itemId, ServerItemAttributes.Upper, item["Upper"] ?? "0x3f");
                    table.SetRaw(itemId, ServerItemAttributes.Gender, item["Gender"] ?? defaultGender);
                    table.SetRaw(itemId, ServerItemAttributes.Location, item["Loc"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.WeaponLevel, item["WeaponLv"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.EquipLevel, item["EquipLv"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.Refineable, item["Refine"] ?? defaultRefineable);
                    table.SetRaw(itemId, ServerItemAttributes.ClassNumber, item["View"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.Script, item["Script"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.OnEquipScript, item["OnEquipScript"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.OnUnequipScript, item["OnUnequipScript"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.KeepAfterUse, item["KeepAfterUse"] ?? "false");
                    table.SetRaw(itemId, ServerItemAttributes.ForceSerial, item["ForceSerial"] ?? "false");

                    table.SetRaw(itemId, ServerItemAttributes.Matk, item["Matk"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.BindOnEquip, item["BindOnEquip"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.BuyingStore, item["BuyingStore"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.Delay, item["Delay"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.Stack, item["Stack"] ?? "");
                    table.SetRaw(itemId, ServerItemAttributes.Sprite, item["Sprite"] ?? "");

                    table.SetRaw(itemId, ServerItemAttributes.TradeOverride, item["Trade.override"] ?? "100");
                    table.SetRaw(itemId, ServerItemAttributes.TradeFlag, (
                        (!Boolean.Parse((item["Trade.nodrop"] ?? "false")) ? 0 : (1 << 0)) |
                        (!Boolean.Parse((item["Trade.notrade"] ?? "false")) ? 0 : (1 << 1)) |
                        (!Boolean.Parse((item["Trade.partneroverride"] ?? "false")) ? 0 : (1 << 2)) |
                        (!Boolean.Parse((item["Trade.noselltonpc"] ?? "false")) ? 0 : (1 << 3)) |
                        (!Boolean.Parse((item["Trade.nocart"] ?? "false")) ? 0 : (1 << 4)) |
                        (!Boolean.Parse((item["Trade.nostorage"] ?? "false")) ? 0 : (1 << 5)) |
                        (!Boolean.Parse((item["Trade.nogstorage"] ?? "false")) ? 0 : (1 << 6)) |
                        (!Boolean.Parse((item["Trade.nomail"] ?? "false")) ? 0 : (1 << 7)) |
                        (!Boolean.Parse((item["Trade.noauction"] ?? "false")) ? 0 : (1 << 8))
                        ).ToString(CultureInfo.InvariantCulture));

                    table.SetRaw(itemId, ServerItemAttributes.NoUseOverride, item["Nouse.override"] ?? "100");
                    table.SetRaw(itemId, ServerItemAttributes.NoUseFlag, (
                        (!Boolean.Parse((item["Nouse.sitting"] ?? "false")) ? 0 : (1 << 0))
                        ).ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static object _jobToId(AbstractDb<int> adb, ParserObject parser)
        {
            int outputJob = 0;
            var value = parser["Jobs"];

            if (value == null)
            {
                return "0xFFFFFFFF";
            }

            if (value is ParserString)
            {
                return value.ObjectValue;
            }

            if (value is ParserArrayBase)
            {
                foreach (ParserKeyValue job in value.OfType<ParserKeyValue>())
                {
                    int ival;

                    if (ItemDbJobs.TryGetValue(job.Key, out ival))
                    {
                        if (ItemDbJobs.ContainsKey(job.Key))
                        {
                            if (Boolean.Parse(job.Value))
                                outputJob |= ival;
                            else
                                outputJob &= ~ival;
                        }
                        else
                        {
                            throw new Exception("Unknown job : " + job.ObjectValue);
                        }
                    }
                }

                adb.Attached["ItemDb.UseExtendedJobs"] = true;
                return "0x" + outputJob.ToString("X8");
            }

            return "0xFFFFFFFF";
        }

        private static object _jobToIdYaml(AbstractDb<int> adb, ParserObject parser)
        {
            int outputJob = 0;
            var value = parser["Jobs"];

            if (value == null)
            {
                return "0xFFFFFFFF";
            }

            if (value is ParserString)
            {
                return value.ObjectValue;
            }

            if (value is ParserArrayBase)
            {
                foreach (ParserKeyValue job in value.OfType<ParserKeyValue>())
                {
                    int ival;

                    if (ItemDbJobsYaml.TryGetValue(job.Key, out ival))
                    {
                        if (ItemDbJobsYaml.ContainsKey(job.Key))
                        {
                            if (Boolean.Parse(job.Value))
                            {
                                outputJob |= ival;
                            }
                            else
                            {
                                outputJob &= ~ival;
                            }
                        }
                        else
                        {
                            throw new Exception("Unknown job : " + job.ObjectValue);
                        }
                    }
                    else
                    {
                        throw new Exception("Unknown job : " + job.ObjectValue);
                    }
                }

                return "0x" + outputJob.ToString("X8");
            }

            return "0xFFFFFFFF";
        }

        private static object _jobToIdYaml3(AbstractDb<int> adb, ParserObject parser)
        {
            int outputJob = 0;
            var value = parser["Jobs"];

            if (value == null)
            {
                return "0xFFFFFFFF";
            }

            if (value is ParserString)
            {
                return value.ObjectValue;
            }

            if (value is ParserArrayBase)
            {
                foreach (ParserKeyValue job in value.OfType<ParserKeyValue>())
                {
                    int ival;

                    if (ItemDbJobsYaml3.TryGetValue(job.Key, out ival))
                    {
                        if (ItemDbJobsYaml3.ContainsKey(job.Key))
                        {
                            if (Boolean.Parse(job.Value))
                            {
                                outputJob |= ival;
                            }
                            else
                            {
                                outputJob &= ~ival;
                            }
                        }
                        else
                        {
                            throw new Exception("Unknown job : " + job.ObjectValue);
                        }
                    }
                    else
                    {
                        throw new Exception("Unknown job : " + job.ObjectValue);
                    }
                }

                return "0x" + outputJob.ToString("X8");
            }

            return "0xFFFFFFFF";
        }

        public static void DbItemsBuyingStoreFunction<T>(DbDebugItem<T> debug, AttributeList list, string[] elements, Table<T, ReadableTuple<T>> table)
        {
            T itemId = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]);
            table.SetRaw(itemId, ServerItemAttributes.BuyingStore, "true");
        }

        public static void DbItemsStackFunction<T>(DbDebugItem<T> debug, AttributeList list, string[] elements, Table<T, ReadableTuple<T>> table)
        {
            T itemId = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]);
            table.SetRaw(itemId, ServerItemAttributes.Stack, "[" + elements[1] + "," + elements[2] + "]");
        }

        public static void DbItemsWriter(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            try
            {
                StringBuilder builder = new StringBuilder();

                if (debug.FileType == FileType.Txt)
                {
                    if (DbPathLocator.GetServerType() == ServerType.RAthena)
                    {
                        DbIOMethods.DbWriterComma(debug, db, 0, ServerItemAttributes.OnUnequipScript.Index + 1, (tuple, items) =>
                        {
                            if (db.ProjectDatabase.IsRenewal)
                            {
                                string value = tuple.GetValue<string>(ServerItemAttributes.Matk) ?? "";

                                if (value == "" || value == "0")
                                    return;

                                string atk = items[ServerItemAttributes.Attack.Index].ToString();

                                items[ServerItemAttributes.Attack.Index] = (atk == "" ? "0" : atk) + ":" + value;
                            }
                        });
                        return;
                    }

                    DbItemsWriterSub(builder, db, db.Table.FastItems.OrderBy(p => p.GetKey<int>()), ServerType.RAthena);
                    IOHelper.WriteAllText(debug.FilePath, builder.ToString());
                }
                else if (debug.FileType == FileType.Yaml)
                {
                    var itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

                    try
                    {
                        if (db.Attached["Import:item_db_usable"] != null &&
                            db.Attached["Import:item_db_equip"] != null &&
                            db.Attached["Import:item_db_etc"] != null)
                        {
                            string dbPath = GrfPath.GetDirectoryName(ProjectConfiguration.DatabasePath);
                            dbPath = GrfPath.GetDirectoryName(dbPath);
                            string renewal = "re";

                            if (!db.ProjectDatabase.IsRenewal)
                            {
                                renewal = "pre-re";
                            }

                            var paths = new string[] {
                                GrfPath.CombineUrl(dbPath, "db", renewal, "item_db_usable.yml"),
                                GrfPath.CombineUrl(dbPath, "db", renewal, "item_db_equip.yml"),
                                GrfPath.CombineUrl(dbPath, "db", renewal, "item_db_etc.yml"),
                            };

                            var linesUsable = new YamlParser(DbPathLocator.GetStoredFile(paths[0]), ParserMode.Write);
                            var linesEquip = new YamlParser(DbPathLocator.GetStoredFile(paths[1]), ParserMode.Write);
                            var linesEtc = new YamlParser(DbPathLocator.GetStoredFile(paths[2]), ParserMode.Write);

                            if (linesUsable.Output == null ||
                                linesEquip.Output == null ||
                                linesEtc.Output == null)
                                return;

                            linesUsable.Remove(db);
                            linesEquip.Remove(db);
                            linesEtc.Remove(db);

                            foreach (ReadableTuple<int> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<int>()))
                            {
                                string key = tuple.Key.ToString();

                                int type = tuple.GetValue<int>(ServerItemAttributes.Type);
                                StringBuilder b = new StringBuilder();

                                WriteEntryYaml(b, tuple, itemDb);

                                switch (type)
                                {
                                    default: // Usable
                                        linesEquip.Delete(key);
                                        linesEtc.Delete(key);
                                        linesUsable.Write(key, b.ToString().Trim('\r', '\n'));
                                        break;

                                    case 4:
                                    case 5:
                                    case 7:
                                    case 8:
                                    case 12: // Equip
                                        linesEquip.Write(key, b.ToString().Trim('\r', '\n'));
                                        linesEtc.Delete(key);
                                        linesUsable.Delete(key);
                                        break;

                                    case 3:
                                    case 6:
                                    case 10: // Etc
                                        linesEquip.Delete(key);
                                        linesEtc.Write(key, b.ToString().Trim('\r', '\n'));
                                        linesUsable.Delete(key);
                                        break;
                                }
                            }

                            linesUsable.WriteFile(paths[0]);
                            linesEquip.WriteFile(paths[1]);
                            linesEtc.WriteFile(paths[2]);
                        }
                        else
                        {
                            DbIOMethods.DbIOWriter(debug, db, (r, q) => WriteEntryYaml(r, q, itemDb));
                        }
                    }
                    catch (Exception err)
                    {
                        debug.ReportException(err);
                    }
                }
                else if (debug.FileType == FileType.Conf && db.ProjectDatabase.IsNova)
                {
                    var clientDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.CItems);

                    DbIOMethods.DbIOWriter(debug, db, (r, q) => WriteEntry2(db, r, q, clientDb));
                }
                else if (debug.FileType == FileType.Conf)
                {
                    DbIOMethods.DbIOWriter(debug, db, (r, q) => WriteEntry(db, r, q));
                }
                else if (debug.FileType == FileType.Sql)
                {
                    SqlParser.DbSqlItems(debug, db);
                }
            }
            catch (Exception err)
            {
                debug.ReportException(err);
            }
        }

        public static void WriteEntryYaml(StringBuilder builder, ReadableTuple<int> tuple, MetaTable<int> itemDb)
        {
            if (tuple != null)
            {
                builder.AppendLine("  - Id: " + tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture));
                builder.AppendLine("    AegisName: " + tuple.GetValue<string>(ServerItemAttributes.AegisName));
                builder.AppendLine("    Name: " + DbIOUtils.QuoteCheck(tuple.GetValue<string>(ServerItemAttributes.Name)));

                int value;
                bool valueB;
                string valueS;
                int type = 0;

                type = value = tuple.GetValue<int>(ServerItemAttributes.Type);

                builder.AppendLine("    Type: " + Constants.ToString<TypeType>(value));

                if ((value = tuple.GetValue<int>(ServerItemAttributes.SubType)) != 0)
                {
                    if (type == 10)
                    {
                        builder.AppendLine("    SubType: " + Constants.ToString<AmmoType>(value));
                    }
                    else
                    {
                        builder.AppendLine("    SubType: " + Constants.ToString<WeaponType>(value));
                    }
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.ClassNumber)) != 0)
                {
                    builder.AppendLine("    View: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Buy)) != 0)
                {
                    builder.AppendLine("    Buy: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Sell)) != 0)
                {
                    builder.AppendLine("    Sell: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Weight)) != 0)
                {
                    builder.AppendLine("    Weight: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Attack)) != 0)
                {
                    builder.AppendLine("    Attack: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Matk)) != 0)
                {
                    builder.AppendLine("    MagicAttack: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Defense)) != 0)
                {
                    builder.AppendLine("    Defense: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Range)) != 0)
                {
                    builder.AppendLine("    Range: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.NumberOfSlots)) != 0)
                {
                    builder.AppendLine("    Slots: " + value);
                }

                DbIOFormatting.TrySetJobsYaml(tuple, builder, ServerItemAttributes.ApplicableJob, "");

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Upper)) != 63)
                {
                    builder.AppendLine("    Classes:");

                    if (value == 0)
                    {
                        builder.AppendLine("      All: false");
                    }
                    else
                    {
                        if (value == (0x08 | 0x10 | 0x20))
                        {
                            builder.AppendLine("      All_Third: true");
                        }
                        else if (value == (0x04 | 0x20))
                        {
                            builder.AppendLine("      All_Baby: true");
                        }
                        else if (value == (0x02 | 0x10))
                        {
                            builder.AppendLine("      All_Upper: true");
                        }
                        else
                        {
                            if ((value & 0x1) == 0x1)
                            {
                                builder.AppendLine("      Normal: true");
                            }

                            if ((value & 0x2) == 0x2)
                            {
                                builder.AppendLine("      Upper: true");
                            }

                            if ((value & 0x4) == 0x4)
                            {
                                builder.AppendLine("      Baby: true");
                            }

                            if ((value & 0x8) == 0x8)
                            {
                                builder.AppendLine("      Third: true");
                            }

                            if ((value & 0x10) == 0x10)
                            {
                                builder.AppendLine("      Third_Upper: true");
                            }

                            if ((value & 0x20) == 0x20)
                            {
                                builder.AppendLine("      Third_Baby: true");
                            }
                        }
                    }
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Gender)) != 2)
                {
                    if (value == 0)
                    {
                        builder.AppendLine("    Gender: Female");
                    }
                    else if (value == 1)
                    {
                        builder.AppendLine("    Gender: Male");
                    }
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Location)) != 0)
                {
                    builder.AppendLine("    Locations:");

                    if ((value & 0x000001) == 0x000001)
                    {
                        builder.AppendLine("      Head_Low: true");
                    }

                    if ((value & 0x000200) == 0x000200)
                    {
                        builder.AppendLine("      Head_Mid: true");
                    }

                    if ((value & 0x000100) == 0x000100)
                    {
                        builder.AppendLine("      Head_Top: true");
                    }

                    if ((value & 0x000040) == 0x000040)
                    {
                        builder.AppendLine("      Shoes: true");
                    }

                    if ((value & (0x000002 | 0x000020)) == (0x000002 | 0x000020))
                    {
                        builder.AppendLine("      Both_Hand: true");
                    }
                    else
                    {
                        if ((value & 0x000002) == 0x000002)
                        {
                            builder.AppendLine("      Right_Hand: true");
                        }

                        if ((value & 0x000020) == 0x000020)
                        {
                            builder.AppendLine("      Left_Hand: true");
                        }
                    }

                    if ((value & 0x000010) == 0x000010)
                    {
                        builder.AppendLine("      Armor: true");
                    }

                    if ((value & 0x000004) == 0x000004)
                    {
                        builder.AppendLine("      Garment: true");
                    }

                    if ((value & (0x000080 | 0x000008)) == (0x000080 | 0x000008))
                    {
                        builder.AppendLine("      Both_Accessory: true");
                    }
                    else
                    {
                        if ((value & 0x000080) == 0x000080)
                        {
                            builder.AppendLine("      Right_Accessory: true");
                        }

                        if ((value & 0x000008) == 0x000008)
                        {
                            builder.AppendLine("      Left_Accessory: true");
                        }
                    }

                    if ((value & 0x008000) == 0x008000)
                    {
                        builder.AppendLine("      Ammo: true");
                    }

                    if ((value & 0x000400) == 0x000400)
                    {
                        builder.AppendLine("      Costume_Head_Top: true");
                    }

                    if ((value & 0x000800) == 0x000800)
                    {
                        builder.AppendLine("      Costume_Head_Mid: true");
                    }

                    if ((value & 0x001000) == 0x001000)
                    {
                        builder.AppendLine("      Costume_Head_Low: true");
                    }

                    if ((value & 0x002000) == 0x002000)
                    {
                        builder.AppendLine("      Costume_Garment: true");
                    }

                    if ((value & 0x010000) == 0x010000)
                    {
                        builder.AppendLine("      Shadow_Armor: true");
                    }

                    if ((value & 0x020000) == 0x020000)
                    {
                        builder.AppendLine("      Shadow_Weapon: true");
                    }

                    if ((value & 0x040000) == 0x040000)
                    {
                        builder.AppendLine("      Shadow_Shield: true");
                    }

                    if ((value & 0x080000) == 0x080000)
                    {
                        builder.AppendLine("      Shadow_Shoes: true");
                    }

                    if ((value & 0x100000) == 0x100000)
                    {
                        builder.AppendLine("      Shadow_Right_Accessory: true");
                    }

                    if ((value & 0x200000) == 0x200000)
                    {
                        builder.AppendLine("      Shadow_Left_Accessory: true");
                    }
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.WeaponLevel)) != 0)
                {
                    builder.AppendLine("    WeaponLevel: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.EquipLevelMin)) != 0)
                {
                    builder.AppendLine("    EquipLevelMin: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.EquipLevelMax)) != 0)
                {
                    builder.AppendLine("    EquipLevelMax: " + value);
                }

                if ((valueB = tuple.GetValue<bool>(ServerItemAttributes.Refineable)) != false)
                {
                    builder.AppendLine("    Refineable: true");
                }

                if ((valueS = tuple.GetValue<string>(ServerItemAttributes.AliasName)) != "")
                {
                    builder.AppendLine("    AliasName: " + DbIOUtils.Id2Name(itemDb, ServerItemAttributes.AegisName, valueS));
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Flags)) != 0 || tuple.GetValue<int>(ServerItemAttributes.DropEffect) != 0)
                {
                    DbIOUtils.ExpandFlag<ItemFlagType>(builder, tuple, "Flags", ServerItemAttributes.Flags, YamlParser.Indent4, YamlParser.Indent6,
                        () => tuple.GetValue<int>(ServerItemAttributes.DropEffect) != 0,
                        delegate
                        {
                            DropEffectType effect = (DropEffectType)tuple.GetValue<int>(ServerItemAttributes.DropEffect);

                            switch (effect)
                            {
                                case DropEffectType.Client: builder.Append(YamlParser.Indent6); builder.Append("DropEffect: "); builder.AppendLine("CLIENT"); break;
                                case DropEffectType.White_Pillar: builder.Append(YamlParser.Indent6); builder.Append("DropEffect: "); builder.AppendLine("WHITE_PILLAR"); break;
                                case DropEffectType.Blue_Pillar: builder.Append(YamlParser.Indent6); builder.Append("DropEffect: "); builder.AppendLine("BLUE_PILLAR"); break;
                                case DropEffectType.Yellow_Pillar: builder.Append(YamlParser.Indent6); builder.Append("DropEffect: "); builder.AppendLine("YELLOW_PILLAR"); break;
                                case DropEffectType.Purple_Pillar: builder.Append(YamlParser.Indent6); builder.Append("DropEffect: "); builder.AppendLine("PURPLE_PILLAR"); break;
                                case DropEffectType.Orange_Pillar: builder.Append(YamlParser.Indent6); builder.Append("DropEffect: "); builder.AppendLine("ORANGE_PILLAR"); break;
                                case DropEffectType.Green_Pillar: builder.Append(YamlParser.Indent6); builder.Append("DropEffect: "); builder.AppendLine("GREEN_PILLAR"); break;
                                case DropEffectType.Red_Pillar: builder.Append(YamlParser.Indent6); builder.Append("DropEffect: "); builder.AppendLine("RED_PILLAR"); break;
                            }
                        });
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Delay)) != 0)
                {
                    builder.AppendLine("    Delay:");
                    builder.AppendLine("      Duration: " + value);

                    if ((valueS = tuple.GetValue<string>(ServerItemAttributes.DelayStatus)) != "")
                    {
                        builder.AppendLine("      Status: " + valueS);
                    }
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.StackAmount)) != 0)
                {
                    builder.AppendLine("    Stack:");
                    builder.AppendLine("      Amount: " + value);

                    DbIOUtils.ExpandFlag<ItemStackFlagType>(builder, tuple, "", ServerItemAttributes.StackFlags, YamlParser.Indent4, YamlParser.Indent6);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.NoUseFlag)) != 0)
                {
                    builder.AppendLine("    NoUse:");
                    builder.AppendLine("      Override: " + tuple.GetValue<int>(ServerItemAttributes.NoUseOverride));

                    if ((value & 0x1) == 0x1)
                    {
                        builder.AppendLine("      Sitting: true");
                    }
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.TradeFlag)) != 0)
                {
                    builder.AppendLine("    Trade:");
                    builder.AppendLine("      Override: " + tuple.GetValue<int>(ServerItemAttributes.TradeOverride));

                    if ((value & (1 << 0)) == (1 << 0)) builder.AppendLine("      NoDrop: true");
                    if ((value & (1 << 1)) == (1 << 1)) builder.AppendLine("      NoTrade: true");
                    if ((value & (1 << 2)) == (1 << 2)) builder.AppendLine("      TradePartner: true");
                    if ((value & (1 << 3)) == (1 << 3)) builder.AppendLine("      NoSell: true");
                    if ((value & (1 << 4)) == (1 << 4)) builder.AppendLine("      NoCart: true");
                    if ((value & (1 << 5)) == (1 << 5)) builder.AppendLine("      NoStorage: true");
                    if ((value & (1 << 6)) == (1 << 6)) builder.AppendLine("      NoGuildStorage: true");
                    if ((value & (1 << 7)) == (1 << 7)) builder.AppendLine("      NoMail: true");
                    if ((value & (1 << 8)) == (1 << 8)) builder.AppendLine("      NoAuction: true");
                }

                if ((valueS = tuple.GetValue<string>(ServerItemAttributes.Script)) != "" && valueS != "{}")
                {
                    builder.AppendLine("    Script: |");
                    builder.AppendLine(DbIOFormatting.ScriptFormatYaml(valueS, "      "));
                }

                if ((valueS = tuple.GetValue<string>(ServerItemAttributes.OnEquipScript)) != "" && valueS != "{}")
                {
                    builder.AppendLine("    EquipScript: |");
                    builder.AppendLine(DbIOFormatting.ScriptFormatYaml(valueS, "      "));
                }

                if ((valueS = tuple.GetValue<string>(ServerItemAttributes.OnUnequipScript)) != "" && valueS != "{}")
                {
                    builder.AppendLine("    UnEquipScript: |");
                    builder.AppendLine(DbIOFormatting.ScriptFormatYaml(valueS, "      "));
                }
            }
        }

        public static void DbItemsWriterSub<TKey>(StringBuilder builder, AbstractDb<TKey> db, IEnumerable<ReadableTuple<TKey>> tuples, ServerType to)
        {
            if (to == ServerType.RAthena)
            {
                bool fromTxtDb = DbPathLocator.DetectPath(db.DbSource).IsExtension(".txt");

                foreach (ReadableTuple<TKey> tuple in tuples)
                {
                    List<string> rawElements = tuple.GetRawElements().Take(22).Select(p => p.ToString()).ToList();

                    if (tuple.Normal && fromTxtDb && tuple.GetValue<int>(ServerItemAttributes.Matk) == 0)
                    {
                        builder.AppendLine(String.Join(",", rawElements.ToArray()));
                        continue;
                    }

                    string script1 = tuple.GetValue<string>(19);
                    string script2 = tuple.GetValue<string>(20);
                    string script3 = tuple.GetValue<string>(21);
                    string refine = tuple.GetValue<string>(17);

                    if (refine == "")
                    {
                    }
                    else if (refine == "true" || refine == "1")
                    {
                        refine = "1";
                    }
                    else
                    {
                        refine = "0";
                    }

                    string atk = DbIOFormatting.ZeroDefault(rawElements[7]);

                    if (db.ProjectDatabase.IsRenewal)
                    {
                        string matk = tuple.GetValue<string>(ServerItemAttributes.Matk) ?? "";

                        if (matk != "" && matk != "0")
                        {
                            atk = (atk == "" ? "0" : atk) + ":" + matk;
                        }
                    }

                    builder.AppendLine(String.Join(",",
                        new string[] {
                            rawElements[0], // ID
							rawElements[1], // AegisName
							rawElements[2], // Name
							DbIOFormatting.OutputInteger(rawElements[3]), // Type
							DbIOFormatting.ZeroDefault(rawElements[4]), // Buy
							DbIOFormatting.ZeroDefault(rawElements[5]), // Sell
							String.IsNullOrEmpty(rawElements[6]) ? "0" : rawElements[6], // Weight
							atk, // ATK + matk
							DbIOFormatting.ZeroDefault(rawElements[8]),
                            DbIOFormatting.ZeroDefault(rawElements[9]),
                            DbIOFormatting.ZeroDefault(rawElements[10]), // Slots
							String.IsNullOrEmpty(rawElements[11]) ? "0xFFFFFFFF" : !rawElements[11].StartsWith("0x") ? "0x" + Int32.Parse(rawElements[11]).ToString("X8") : rawElements[11],
                            DbIOFormatting.HexToInt(rawElements[12]), // Upper
							DbIOFormatting.ZeroDefault(rawElements[13]),
                            DbIOFormatting.ZeroDefault(DbIOFormatting.HexToInt(rawElements[14])),
                            DbIOFormatting.ZeroDefault(rawElements[15]),
                            DbIOFormatting.ZeroDefault(rawElements[16]),
                            refine,
                            DbIOFormatting.ZeroDefault(rawElements[18]),
                            String.IsNullOrEmpty(script1) ? "{}" : "{ " + script1 + " }",
                            String.IsNullOrEmpty(script2) ? "{}" : "{ " + script2 + " }",
                            String.IsNullOrEmpty(script3) ? "{}" : "{ " + script3 + " }"
                        }));
                }
            }
            else if (to == ServerType.Hercules)
            {
                foreach (var tuple in tuples.OrderBy(p => p.GetKey<int>()).OfType<ReadableTuple<int>>())
                {
                    WriteEntry(db, builder, tuple);
                    builder.AppendLine();
                }
            }
        }

        public static void DbItemsWriterSub2<TKey>(StringBuilder builder, AbstractDb<TKey> db, List<ReadableTuple<TKey>> tuples, ServerType to)
        {
            DbIOMethods.DbLoaderCommaRange(new DbDebugItem<TKey>(db) { FilePath = GrfPath.CombineUrl(Path.GetDirectoryName(ProjectConfiguration.DatabasePath), "re/item_flag.txt") }, db.AttributeList, ServerItemAttributes.TempFlags.Index, 1, false);
            DbIOMethods.DbLoaderCommaRange(new DbDebugItem<TKey>(db) { FilePath = GrfPath.CombineUrl(Path.GetDirectoryName(ProjectConfiguration.DatabasePath), "item_ii.txt") }, db.AttributeList, ServerItemAttributes.TempHideII.Index, 1, false);
            DbIOMethods.DbLoaderCommaRange(new DbDebugItem<TKey>(db) { FilePath = GrfPath.CombineUrl(Path.GetDirectoryName(ProjectConfiguration.DatabasePath), "item_db_helper.txt") }, db.AttributeList, ServerItemAttributes.TempClientName.Index, 1, false);
            DbIOMethods.DbLoaderCommaRange(new DbDebugItem<TKey>(db) { FilePath = GrfPath.CombineUrl(Path.GetDirectoryName(ProjectConfiguration.DatabasePath), "item_nodrop.txt") }, db.AttributeList, ServerItemAttributes.TempNoDrop.Index, 1, false);
            DbIOMethods.DbLoaderCommaRange(new DbDebugItem<TKey>(db) { FilePath = GrfPath.CombineUrl(Path.GetDirectoryName(ProjectConfiguration.DatabasePath), "item_forcelog.txt") }, db.AttributeList, ServerItemAttributes.TempForceLog.Index, 1, false);
            DbIOMethods.DbLoaderCommaRange(new DbDebugItem<TKey>(db) { FilePath = GrfPath.CombineUrl(Path.GetDirectoryName(ProjectConfiguration.DatabasePath), "re/item_mvp_category.txt") }, db.AttributeList, ServerItemAttributes.TempMvpCategory.Index, 1, false);
            DbIOMethods.DbLoaderCommaRange(new DbDebugItem<TKey>(db) { FilePath = GrfPath.CombineUrl(Path.GetDirectoryName(ProjectConfiguration.DatabasePath), "item_weight.txt") }, db.AttributeList, ServerItemAttributes.TempExpectedWeight.Index, 1, false);

            var ele = new LibconfigParser(GrfPath.CombineUrl(Path.GetDirectoryName(ProjectConfiguration.DatabasePath), "re/item_db.conf"));

            foreach (var item in ele.Output["item_data"])
            {
                int id = Int32.Parse(item["ID"]);
                var tuple = tuples.FirstOrDefault(p => p.GetKey<int>() == id);

                if (tuple == null)
                    continue;

                tuple.SetRawValue(ServerItemAttributes.TempShadowGear, item["IsShadowGearBonus"] ?? "false");
                tuple.SetRawValue(ServerItemAttributes.MHItem, item["MHItem"] ?? "false");
                tuple.SetRawValue(ServerItemAttributes.MHHuntItem, item["MHHuntItem"] ?? "false");
                tuple.SetRawValue(ServerItemAttributes.MHMaxUses, item["MHMaxUses"] ?? "0");
                tuple.SetRawValue(ServerItemAttributes.MHResetUsesOnDeath, item["MHResetUsesOnDeath"] ?? "false");
                tuple.SetRawValue(ServerItemAttributes.MHUseIncOnSuccess, item["MHUseIncOnSuccess"] ?? "false");
            }

            var clientDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.CItems);

            foreach (var tuple in tuples.OrderBy(p => p.GetKey<int>()).OfType<ReadableTuple<int>>())
            {
                WriteEntry2(db, builder, tuple, clientDb);
                builder.AppendLine();
            }
        }

        private static TkDictionary<TKey, string[]> _getPhantomTable<TKey>(DbDebugItem<TKey> debug)
        {
            if (debug.AbsractDb.DbSource != ServerDbs.Items2)
                return null;

            if (debug.AbsractDb.Attached.ContainsKey("Phantom." + debug.DbSource.Filename))
            {
                return (TkDictionary<TKey, string[]>)debug.AbsractDb.Attached["Phantom." + debug.DbSource.Filename];
            }

            return null;
        }

        public static void DbItemsNouse<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db)
        {
            try
            {
                IntLineStream lines = new IntLineStream(debug.OldPath);
                lines.Remove(db);
                string line;

                TkDictionary<TKey, string[]> phantom = _getPhantomTable(debug);

                if (phantom != null)
                {
                    var itemDb = debug.AbsractDb.GetDb<TKey>(ServerDbs.Items).Table;

                    // Check if the phantom values differ from the Items1
                    foreach (var tuple in phantom)
                    {
                        if (debug.AbsractDb.Table.ContainsKey(tuple.Key))
                            continue;

                        var key = tuple.Key;
                        var elements = tuple.Value;
                        var tuple1 = itemDb.TryGetTuple(key);

                        if (tuple1 != null)
                        {
                            int val1 = tuple1.GetIntNoThrow(ServerItemAttributes.NoUseFlag);
                            int val2 = FormatConverters.IntOrHexConverter(elements[1]);

                            int val3 = tuple1.GetIntNoThrow(ServerItemAttributes.NoUseOverride);
                            int val4 = FormatConverters.IntOrHexConverter(elements[2]);

                            // There is no flag set
                            if (val1 != val2 || val3 != val4)
                            {
                                lines.Delete(tuple1.GetKey<int>());
                            }
                        }
                    }
                }

                foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>()))
                {
                    int key = tuple.GetKey<int>();

                    string overrideValue = tuple.GetValue<string>(ServerItemAttributes.NoUseOverride);
                    string flagValue = tuple.GetValue<string>(ServerItemAttributes.NoUseFlag);

                    if (flagValue == "0")
                    {
                        if (overrideValue == "100" || SdeAppConfiguration.DbNouseIgnoreOverride)
                        {
                            lines.Delete(key);
                            continue;
                        }
                    }

                    line = String.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), flagValue, overrideValue }.ToArray());

                    if (SdeAppConfiguration.AddCommentForItemNoUse)
                    {
                        line += "\t// " + tuple.GetValue<string>(ServerItemAttributes.AegisName);
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

        public static void DbItemsTrade<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db)
        {
            try
            {
                IntLineStream lines = new IntLineStream(debug.OldPath);
                lines.Remove(db);
                string line;

                TkDictionary<TKey, string[]> phantom = _getPhantomTable(debug);

                if (phantom != null)
                {
                    var itemDb = debug.AbsractDb.GetDb<TKey>(ServerDbs.Items).Table;

                    // Check if the phantom values differ from the Items1
                    foreach (var tuple in phantom)
                    {
                        if (debug.AbsractDb.Table.ContainsKey(tuple.Key))
                            continue;

                        var key = tuple.Key;
                        var elements = tuple.Value;
                        var tuple1 = itemDb.TryGetTuple(key);

                        if (tuple1 != null)
                        {
                            int val1 = tuple1.GetIntNoThrow(ServerItemAttributes.TradeFlag);
                            int val2 = FormatConverters.IntOrHexConverter(elements[1]);

                            int val3 = tuple1.GetIntNoThrow(ServerItemAttributes.TradeOverride);
                            int val4 = FormatConverters.IntOrHexConverter(elements[2]);

                            // There is no flag set
                            if (val1 != val2 || val3 != val4)
                            {
                                lines.Delete(tuple1.GetKey<int>());
                            }
                        }
                    }
                }

                foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>()))
                {
                    int key = tuple.GetKey<int>();

                    string overrideValue = tuple.GetValue<string>(ServerItemAttributes.TradeOverride);
                    string flagValue = tuple.GetValue<string>(ServerItemAttributes.TradeFlag);

                    if (flagValue == "0")
                    {
                        if (overrideValue == "100" || SdeAppConfiguration.DbTradeIgnoreOverride)
                        {
                            lines.Delete(key);
                            continue;
                        }
                    }

                    line = String.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), flagValue, overrideValue }.ToArray());

                    if (SdeAppConfiguration.AddCommentForItemTrade)
                    {
                        line += "\t// " + tuple.GetValue<string>(ServerItemAttributes.AegisName);
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

        public static void DbItemsCommaRange<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, int from, int length, string defaultValue, Func<ReadableTuple<TKey>, List<string>, string, string> append)
        {
            try
            {
                IntLineStream lines = new IntLineStream(debug.OldPath);
                lines.Remove(db);
                string line;

                TkDictionary<TKey, string[]> phantom = _getPhantomTable(debug);

                if (phantom != null)
                {
                    var itemDb = debug.AbsractDb.GetDb<TKey>(ServerDbs.Items).Table;

                    // Check if the phantom values differ from the Items1
                    foreach (var tuple in phantom)
                    {
                        if (debug.AbsractDb.Table.ContainsKey(tuple.Key))
                            continue;

                        var key = tuple.Key;
                        var elements = tuple.Value;
                        var tuple1 = itemDb.TryGetTuple(key);

                        if (tuple1 != null)
                        {
                            string val1 = tuple1.GetValue<string>(@from);
                            string val2 = elements[1];

                            if (val1 != val2)
                            {
                                lines.Delete(tuple1.GetKey<int>());
                            }
                        }
                    }
                }

                foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>()))
                {
                    int key = tuple.GetKey<int>();

                    List<string> items = tuple.GetRawElements().Skip(@from).Take(length).Select(p => p.ToString()).ToList();

                    if (items.All(p => p == defaultValue))
                    {
                        lines.Delete(key);
                        continue;
                    }

                    if (append != null)
                        line = append(tuple, items, String.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture) }.Concat(items).ToArray()));
                    else
                        line = String.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture) }.Concat(items).ToArray());
                    lines.Write(key, line);
                }

                lines.WriteFile(debug.FilePath);
            }
            catch (Exception err)
            {
                debug.ReportException(err);
            }
        }

        public static void DbItemsStack<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db)
        {
            try
            {
                IntLineStream lines = new IntLineStream(debug.OldPath);
                lines.Remove(db);
                string line;

                TkDictionary<TKey, string[]> phantom = _getPhantomTable(debug);

                if (phantom != null)
                {
                    var itemDb = debug.AbsractDb.GetDb<TKey>(ServerDbs.Items).Table;

                    // Check if the phantom values differ from the Items1
                    foreach (var tuple in phantom)
                    {
                        if (debug.AbsractDb.Table.ContainsKey(tuple.Key))
                            continue;

                        var key = tuple.Key;
                        var elements = tuple.Value;
                        var tuple1 = itemDb.TryGetTuple(key);

                        if (tuple1 != null)
                        {
                            string val1 = tuple1.GetValue<string>(ServerItemAttributes.Stack);
                            string val2 = elements[1];

                            if (val1 != val2)
                            {
                                lines.Delete(tuple1.GetKey<int>());
                            }
                        }
                    }
                }

                foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>()))
                {
                    int key = tuple.GetKey<int>();

                    string item1 = tuple.GetValue<string>(ServerItemAttributes.Stack);

                    if (item1 == "")
                    {
                        lines.Delete(key);
                        continue;
                    }

                    line = String.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), item1 }.ToArray());
                    lines.Write(key, line);
                }

                lines.WriteFile(debug.FilePath);
            }
            catch (Exception err)
            {
                debug.ReportException(err);
            }
        }

        public static void DbItemsBuyingStore<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db)
        {
            try
            {
                IntLineStream lines = new IntLineStream(debug.OldPath);
                lines.Remove(db);
                string line;

                TkDictionary<TKey, string[]> phantom = _getPhantomTable(debug);

                if (phantom != null)
                {
                    var itemDb = debug.AbsractDb.GetDb<TKey>(ServerDbs.Items).Table;

                    // Check if the phantom values differ from the Items1
                    foreach (var tuple in phantom)
                    {
                        if (debug.AbsractDb.Table.ContainsKey(tuple.Key))
                            continue;

                        var key = tuple.Key;
                        var tuple1 = itemDb.TryGetTuple(key);

                        if (tuple1 != null)
                        {
                            bool val1 = tuple1.GetValue<bool>(ServerItemAttributes.BuyingStore);

                            if (val1 != true)
                            {
                                lines.Delete(tuple1.GetKey<int>());
                            }
                        }
                    }
                }

                foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>()))
                {
                    int key = tuple.GetKey<int>();

                    bool item1 = tuple.GetValue<bool>(ServerItemAttributes.BuyingStore);

                    if (!item1)
                    {
                        lines.Delete(key);
                        continue;
                    }

                    line = key.ToString(CultureInfo.InvariantCulture) + "  // " + tuple.GetValue<string>(ServerItemAttributes.AegisName);
                    lines.Write(key, line);
                }

                lines.WriteFile(debug.FilePath);
            }
            catch (Exception err)
            {
                debug.ReportException(err);
            }
        }

        public static void WriteEntry(BaseDb db, StringBuilder builder, ReadableTuple<int> tuple)
        {
            bool useExtendedJobs = db.Attached["ItemDb.UseExtendedJobs"] != null && (bool)db.Attached["ItemDb.UseExtendedJobs"];

            builder.AppendLine("{");
            builder.AppendLine("\tId: " + tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("\tAegisName: \"" + tuple.GetValue<string>(ServerItemAttributes.AegisName) + "\"");
            builder.AppendLine("\tName: \"" + tuple.GetValue<string>(ServerItemAttributes.Name) + "\"");

            DbIOFormatting.SetType(tuple, builder, ServerItemAttributes.Type);
            DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerItemAttributes.Buy, "");
            DbIOFormatting.TrySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Sell, (tuple.GetIntNoThrow(ServerItemAttributes.Buy) / 2).ToString(CultureInfo.InvariantCulture));
            DbIOFormatting.TrySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Weight, "0");
            DbIOFormatting.TrySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Attack, "0");
            DbIOFormatting.TrySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Matk, "0");
            DbIOFormatting.TrySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Defense, "0");
            DbIOFormatting.TrySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Range, "0");
            DbIOFormatting.TrySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.NumberOfSlots, "0");

            if (useExtendedJobs)
                DbIOFormatting.TrySetIfDefaultEmptyAddHexJobEx(tuple, builder, ServerItemAttributes.ApplicableJob, "");
            else
                DbIOFormatting.TrySetIfDefaultEmptyAddHex(tuple, builder, ServerItemAttributes.ApplicableJob, "");

            DbIOFormatting.TrySetIfDefaultEmptyUpper(tuple, builder, ServerItemAttributes.Upper);
            DbIOFormatting.TrySetGender(tuple, builder, ServerItemAttributes.Gender, "2");
            DbIOFormatting.TrySetIfDefaultLocation(tuple, builder, ServerItemAttributes.Location);
            DbIOFormatting.TrySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.WeaponLevel, "0");
            DbIOFormatting.TrySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.EquipLevel, "0");
            DbIOFormatting.TrySetIfRefineable(tuple, builder, ServerItemAttributes.Refineable, true);
            DbIOFormatting.TrySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.ClassNumber, "0");
            DbIOFormatting.TrySetIfDefaultBoolean(tuple, builder, ServerItemAttributes.BindOnEquip, false);
            DbIOFormatting.TrySetIfDefaultBoolean(tuple, builder, ServerItemAttributes.ForceSerial, false);
            DbIOFormatting.TrySetIfDefaultBoolean(tuple, builder, ServerItemAttributes.BuyingStore, false);
            DbIOFormatting.TrySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Delay, "0");
            DbIOFormatting.TrySetIfDefaultBoolean(tuple, builder, ServerItemAttributes.KeepAfterUse, false);

            var tradeOverride = tuple.GetIntNoThrow(ServerItemAttributes.TradeOverride);
            var tradeFlag = tuple.GetIntNoThrow(ServerItemAttributes.TradeFlag);

            if (tradeOverride != 100 || tradeFlag != 0)
            {
                builder.AppendLine("	Trade: {");

                if (tradeOverride != 100) builder.AppendLine("		override: " + tradeOverride);
                if ((tradeFlag & (1 << 0)) == (1 << 0)) builder.AppendLine("		nodrop: true");
                if ((tradeFlag & (1 << 1)) == (1 << 1)) builder.AppendLine("		notrade: true");
                if ((tradeFlag & (1 << 2)) == (1 << 2)) builder.AppendLine("		partneroverride: true");
                if ((tradeFlag & (1 << 3)) == (1 << 3)) builder.AppendLine("		noselltonpc: true");
                if ((tradeFlag & (1 << 4)) == (1 << 4)) builder.AppendLine("		nocart: true");
                if ((tradeFlag & (1 << 5)) == (1 << 5)) builder.AppendLine("		nostorage: true");
                if ((tradeFlag & (1 << 6)) == (1 << 6)) builder.AppendLine("		nogstorage: true");
                if ((tradeFlag & (1 << 7)) == (1 << 7)) builder.AppendLine("		nomail: true");
                if ((tradeFlag & (1 << 8)) == (1 << 8)) builder.AppendLine("		noauction: true");

                builder.AppendLine("	}");
            }

            var nouseOverride = tuple.GetIntNoThrow(ServerItemAttributes.NoUseOverride);
            var nouseFlag = tuple.GetIntNoThrow(ServerItemAttributes.NoUseFlag);

            if (nouseOverride != 100 || nouseFlag != 0)
            {
                builder.AppendLine("	Nouse: {");

                if (nouseOverride != 100) builder.AppendLine("		override: " + nouseOverride);
                if ((nouseFlag & (1 << 0)) == (1 << 0)) builder.AppendLine("		sitting: true");

                builder.AppendLine("	}");
            }

            DbIOFormatting.TrySetIfDefaultEmptyBracket(tuple, builder, ServerItemAttributes.Stack, "");
            DbIOFormatting.TrySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Sprite, "0");
            DbIOFormatting.TrySetIfDefaultEmptyScript(tuple, builder, ServerItemAttributes.Script, "");
            DbIOFormatting.TrySetIfDefaultEmptyScript(tuple, builder, ServerItemAttributes.OnEquipScript, "");
            DbIOFormatting.TrySetIfDefaultEmptyScript(tuple, builder, ServerItemAttributes.OnUnequipScript, "");

            builder.Append("},");
        }

        public static void WriteEntry2(BaseDb db, StringBuilder builder, ReadableTuple<int> tuple, MetaTable<int> clientDb)
        {
            bool useExtendedJobs = db.Attached["ItemDb.UseExtendedJobs"] != null && (bool)db.Attached["ItemDb.UseExtendedJobs"];

            try
            {
                builder.AppendLine("{");
                builder.AppendLine("\tId: " + tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture));
                builder.AppendLine("\tAegisName: \"" + tuple.GetValue<string>(ServerItemAttributes.AegisName) + "\"");
                builder.AppendLine("\tName: \"" + tuple.GetValue<string>(ServerItemAttributes.Name) + "\"");

                int value;
                bool valueB;
                string valueS;
                int type = 0;

                var clientTuple = clientDb.TryGetTuple(tuple.Key);

                if (clientTuple != null)
                {
                    valueS = tuple.GetValue<string>(ServerItemAttributes.Name);
                    string clientValueS = clientTuple.GetValue<string>(ClientItemAttributes.IdentifiedDisplayName);

                    if (clientValueS != valueS)
                    {
                        builder.AppendLine("	ItemInfoName: \"" + clientValueS + "\"");
                    }
                }

                type = tuple.GetValue<int>(ServerItemAttributes.Type);

                builder.AppendLine("	Type: \"" + Constants.ToString<TypeType>(type) + "\"");

                if (type == 10 || type == 5)
                {
                    value = tuple.GetValue<int>(ServerItemAttributes.SubType);

                    if (type == 10)
                    {
                        builder.AppendLine("	SubType: \"" + Constants.ToString<AmmoType>(value) + "\"");
                    }
                    else
                    {
                        try
                        {
                            //if (value < 0 || value >= 24) {
                            //	// Do nothing, but write it as View instead
                            //	builder.AppendLine("	View: " + value);
                            //}
                            //else {
                            builder.AppendLine("	SubType: \"" + Constants.ToString<WeaponType>(value) + "\"");
                            //}
                        }
                        catch
                        {
                            // Do nothing, but write it as View instead
                            builder.AppendLine("	View: " + value);
                        }
                    }
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.ClassNumber)) != 0)
                {
                    builder.AppendLine("	View: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Buy)) != 0)
                {
                    builder.AppendLine("	Buy: " + value);
                }

                if (tuple.GetValue<string>(ServerItemAttributes.Sell) != "")
                {
                    builder.AppendLine("	Sell: " + tuple.GetValue<int>(ServerItemAttributes.Sell));
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Weight)) != 0)
                {
                    builder.AppendLine("	Weight: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Attack)) != 0)
                {
                    builder.AppendLine("	Attack: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Matk)) != 0)
                {
                    builder.AppendLine("	MagicAttack: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Defense)) != 0)
                {
                    builder.AppendLine("	Defense: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Range)) != 0)
                {
                    builder.AppendLine("	Range: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.NumberOfSlots)) != 0)
                {
                    builder.AppendLine("	Slots: " + value);
                }

                if ((TypeType)type != TypeType.EtcItem && (TypeType)type != TypeType.Card && (TypeType)type != TypeType.PetEgg)
                {
                    DbIOFormatting.TrySetJobsYaml2(tuple, builder, ServerItemAttributes.ApplicableJob, "");
                }

                //if (useExtendedJobs)
                //	DbIOFormatting.TrySetIfDefaultEmptyAddHexJobEx(tuple, builder, ServerItemAttributes.ApplicableJob, "");
                //else
                //	DbIOFormatting.TrySetIfDefaultEmptyAddHex(tuple, builder, ServerItemAttributes.ApplicableJob, "");

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Upper)) != 63 && (TypeType)type != TypeType.EtcItem && (TypeType)type != TypeType.Card && (TypeType)type != TypeType.PetEgg)
                {
                    builder.AppendLine("	Classes: {");

                    if (value == 0)
                    {
                        builder.AppendLine("		All: false");
                    }
                    else
                    {
                        if (value == (0x08 | 0x10 | 0x20))
                        {
                            builder.AppendLine("		All_Third: true");
                        }
                        else if (value == (0x04 | 0x20))
                        {
                            builder.AppendLine("		All_Baby: true");
                        }
                        else if (value == (0x02 | 0x10))
                        {
                            builder.AppendLine("		All_Upper: true");
                        }
                        else
                        {
                            if ((value & 0x1) == 0x1)
                            {
                                builder.AppendLine("		Normal: true");
                            }

                            if ((value & 0x2) == 0x2)
                            {
                                builder.AppendLine("		Upper: true");
                            }

                            if ((value & 0x4) == 0x4)
                            {
                                builder.AppendLine("		Baby: true");
                            }

                            if ((value & 0x8) == 0x8)
                            {
                                builder.AppendLine("		Third: true");
                            }

                            if ((value & 0x10) == 0x10)
                            {
                                builder.AppendLine("		Third_Upper: true");
                            }

                            if ((value & 0x20) == 0x20)
                            {
                                builder.AppendLine("		Third_Baby: true");
                            }
                        }
                    }

                    builder.AppendLine("	}");
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Gender)) != 2)
                {
                    if (value == 0)
                    {
                        builder.AppendLine("	Gender: \"Female\"");
                    }
                    else if (value == 1)
                    {
                        builder.AppendLine("	Gender: \"Male\"");
                    }
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Location)) != 0)
                {
                    builder.AppendLine("	Locations: {");

                    if ((value & 0x000001) == 0x000001)
                    {
                        builder.AppendLine("		Head_Low: true");
                    }

                    if ((value & 0x000200) == 0x000200)
                    {
                        builder.AppendLine("		Head_Mid: true");
                    }

                    if ((value & 0x000100) == 0x000100)
                    {
                        builder.AppendLine("		Head_Top: true");
                    }

                    if ((value & 0x000040) == 0x000040)
                    {
                        builder.AppendLine("		Shoes: true");
                    }

                    if ((value & (0x000002 | 0x000020)) == (0x000002 | 0x000020))
                    {
                        builder.AppendLine("		Both_Hand: true");
                    }
                    else
                    {
                        if ((value & 0x000002) == 0x000002)
                        {
                            builder.AppendLine("		Right_Hand: true");
                        }

                        if ((value & 0x000020) == 0x000020)
                        {
                            builder.AppendLine("		Left_Hand: true");
                        }
                    }

                    if ((value & 0x000010) == 0x000010)
                    {
                        builder.AppendLine("		Armor: true");
                    }

                    if ((value & 0x000004) == 0x000004)
                    {
                        builder.AppendLine("		Garment: true");
                    }

                    if ((value & (0x000080 | 0x000008)) == (0x000080 | 0x000008))
                    {
                        builder.AppendLine("		Both_Accessory: true");
                    }
                    else
                    {
                        if ((value & 0x000080) == 0x000080)
                        {
                            builder.AppendLine("		Right_Accessory: true");
                        }

                        if ((value & 0x000008) == 0x000008)
                        {
                            builder.AppendLine("		Left_Accessory: true");
                        }
                    }

                    if ((value & 0x008000) == 0x008000)
                    {
                        builder.AppendLine("		Ammo: true");
                    }

                    if ((value & 0x000400) == 0x000400)
                    {
                        builder.AppendLine("		Costume_Head_Top: true");
                    }

                    if ((value & 0x000800) == 0x000800)
                    {
                        builder.AppendLine("		Costume_Head_Mid: true");
                    }

                    if ((value & 0x001000) == 0x001000)
                    {
                        builder.AppendLine("		Costume_Head_Low: true");
                    }

                    if ((value & 0x002000) == 0x002000)
                    {
                        builder.AppendLine("		Costume_Garment: true");
                    }

                    if ((value & 0x010000) == 0x010000)
                    {
                        builder.AppendLine("		Shadow_Armor: true");
                    }

                    if ((value & 0x020000) == 0x020000)
                    {
                        builder.AppendLine("		Shadow_Weapon: true");
                    }

                    if ((value & 0x040000) == 0x040000)
                    {
                        builder.AppendLine("		Shadow_Shield: true");
                    }

                    if ((value & 0x080000) == 0x080000)
                    {
                        builder.AppendLine("		Shadow_Shoes: true");
                    }

                    if ((value & 0x100000) == 0x100000)
                    {
                        builder.AppendLine("		Shadow_Right_Accessory: true");
                    }

                    if ((value & 0x200000) == 0x200000)
                    {
                        builder.AppendLine("		Shadow_Left_Accessory: true");
                    }

                    builder.AppendLine("	}");
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.WeaponLevel)) != 0)
                {
                    builder.AppendLine("	WeaponLevel: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.EquipLevelMin)) != 0)
                {
                    builder.AppendLine("	EquipLevelMin: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.EquipLevelMax)) != 0)
                {
                    builder.AppendLine("	EquipLevelMax: " + value);
                }

                if ((valueB = tuple.GetValue<bool>(ServerItemAttributes.Refineable)) != false)
                {
                    builder.AppendLine("	Refineable: true");
                }

                if ((valueS = tuple.GetValue<string>(ServerItemAttributes.Sprite)) != "")
                {
                    builder.AppendLine("	AliasName: " + valueS);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Flags)) != 0 || tuple.GetValue<int>(ServerItemAttributes.DropEffect) != 0)
                {
                    DbIOUtils.ExpandFlagYaml<ItemFlagType>(builder, tuple, "Flags", ServerItemAttributes.Flags, "\t", "\t\t",
                        () => tuple.GetValue<int>(ServerItemAttributes.DropEffect) != 0,
                        delegate
                        {
                            DropEffectType effect = (DropEffectType)tuple.GetValue<int>(ServerItemAttributes.DropEffect);

                            switch (effect)
                            {
                                case DropEffectType.Client: builder.Append("\t\t"); builder.Append("DropEffect: "); builder.AppendLine("\"CLIENT\""); break;
                                case DropEffectType.White_Pillar: builder.Append("\t\t"); builder.Append("DropEffect: "); builder.AppendLine("\"WHITE_PILLAR\""); break;
                                case DropEffectType.Blue_Pillar: builder.Append("\t\t"); builder.Append("DropEffect: "); builder.AppendLine("\"BLUE_PILLAR\""); break;
                                case DropEffectType.Yellow_Pillar: builder.Append("\t\t"); builder.Append("DropEffect: "); builder.AppendLine("\"YELLOW_PILLAR\""); break;
                                case DropEffectType.Purple_Pillar: builder.Append("\t\t"); builder.Append("DropEffect: "); builder.AppendLine("\"PURPLE_PILLAR\""); break;
                                case DropEffectType.Orange_Pillar: builder.Append("\t\t"); builder.Append("DropEffect: "); builder.AppendLine("\"ORANGE_PILLAR\""); break;
                                case DropEffectType.Green_Pillar: builder.Append("\t\t"); builder.Append("DropEffect: "); builder.AppendLine("\"GREEN_PILLAR\""); break;
                                case DropEffectType.Red_Pillar: builder.Append("\t\t"); builder.Append("DropEffect: "); builder.AppendLine("\"RED_PILLAR\""); break;
                            }
                        });
                }

                //DbIOUtils.ExpandFlagYaml<ItemFlagType>(builder, tuple, "Flags", ServerItemAttributes.Flags, "\t", "\t\t");
                DbIOUtils.ExpandFlagYaml<ItemCustomFlagType>(builder, tuple, "CustomFlags", ServerItemAttributes.CustomFlags, "\t", "\t\t");

                value = tuple.GetValue<int>(ServerItemAttributes.MHFlags) +
                        tuple.GetValue<int>(ServerItemAttributes.MHMaxUses);

                if (value != 0)
                {
                    builder.AppendLine("	MH_Data: {");

                    if (tuple.GetValue<int>(ServerItemAttributes.MHMaxUses) > 0)
                    {
                        builder.AppendLine("		MaxUses: " + tuple.GetValue<int>(ServerItemAttributes.MHMaxUses));
                    }

                    var flagsData = FlagsManager.GetFlag<ItemMHFlagType>();
                    value = tuple.GetValue<int>(ServerItemAttributes.MHFlags);

                    if (flagsData != null)
                    {
                        foreach (var v in flagsData.Values)
                        {
                            long vF = v.Value;

                            if ((vF & value) == vF)
                            {
                                builder.Append("\t\t");
                                builder.Append(v.Name);
                                builder.AppendLine(": true");
                            }
                        }
                    }
                    builder.AppendLine("	}");
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.TempMvpCategory)) != 0)
                {
                    builder.AppendLine("	MvpCardTier: " + value);
                }

                if ((valueS = tuple.GetValue<string>(ServerItemAttributes.TempWoEDelay)) != "")
                {
                    builder.AppendLine("	WoEDelay: " + valueS);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.TempExpectedWeight)) != 0)
                {
                    builder.AppendLine("	ContainerExpectedWeight: " + value);
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.Delay)) != 0)
                {
                    builder.AppendLine("	Delay: {");
                    builder.AppendLine("		Duration: " + value);

                    if ((valueS = tuple.GetValue<string>(ServerItemAttributes.DelayStatus)) != "")
                    {
                        builder.AppendLine("		Status: \"" + valueS + "\"");
                    }

                    builder.AppendLine("	}");
                }

                var tradeOverride = tuple.GetIntNoThrow(ServerItemAttributes.TradeOverride);
                var tradeFlag = tuple.GetIntNoThrow(ServerItemAttributes.TradeFlag);

                if (tradeOverride != 100 || tradeFlag != 0)
                {
                    builder.AppendLine("	Trade: {");

                    builder.AppendLine("		Override: " + tradeOverride);
                    if ((tradeFlag & (1 << 0)) == (1 << 0)) builder.AppendLine("		NoDrop: true");
                    if ((tradeFlag & (1 << 1)) == (1 << 1)) builder.AppendLine("		NoTrade: true");
                    if ((tradeFlag & (1 << 2)) == (1 << 2)) builder.AppendLine("		TradePartner: true");
                    if ((tradeFlag & (1 << 3)) == (1 << 3)) builder.AppendLine("		NoSell: true");
                    if ((tradeFlag & (1 << 4)) == (1 << 4)) builder.AppendLine("		NoCart: true");
                    if ((tradeFlag & (1 << 5)) == (1 << 5)) builder.AppendLine("		NoStorage: true");
                    if ((tradeFlag & (1 << 6)) == (1 << 6)) builder.AppendLine("		NoGuildStorage: true");
                    if ((tradeFlag & (1 << 7)) == (1 << 7)) builder.AppendLine("		NoMail: true");
                    if ((tradeFlag & (1 << 8)) == (1 << 8)) builder.AppendLine("		NoAuction: true");

                    builder.AppendLine("	}");
                }

                var nouseOverride = tuple.GetIntNoThrow(ServerItemAttributes.NoUseOverride);
                var nouseFlag = tuple.GetIntNoThrow(ServerItemAttributes.NoUseFlag);

                if (nouseOverride != 100 || nouseFlag != 0)
                {
                    builder.AppendLine("	NoUse: {");

                    builder.AppendLine("		Override: " + nouseOverride);
                    if ((nouseFlag & (1 << 0)) == (1 << 0)) builder.AppendLine("		Sitting: true");

                    builder.AppendLine("	}");
                }

                if ((value = tuple.GetValue<int>(ServerItemAttributes.StackAmount)) != 0)
                {
                    builder.AppendLine("	Stack: {");
                    builder.AppendLine("		Amount: " + value);

                    value = tuple.GetValue<int>(ServerItemAttributes.StackFlags);

                    if ((value & 1) == 1)
                    {
                        builder.AppendLine("		Inventory: true");
                    }

                    if ((value & 2) == 2)
                    {
                        builder.AppendLine("		Cart: true");
                    }

                    if ((value & 4) == 4)
                    {
                        builder.AppendLine("		Storage: true");
                    }

                    if ((value & 8) == 8)
                    {
                        builder.AppendLine("		GuildStorage: true");
                    }

                    builder.AppendLine("	}");
                }

                DbIOFormatting.TrySetIfDefaultEmptyScript(tuple, builder, ServerItemAttributes.Script, "");
                DbIOFormatting.TrySetIfDefaultEmptyScript(tuple, builder, ServerItemAttributes.OnEquipScript, "");
                DbIOFormatting.TrySetIfDefaultEmptyScript(tuple, builder, ServerItemAttributes.OnUnequipScript, "");

                builder.Append("},");
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        public static readonly Dictionary<string, int> ItemDbJobs = new Dictionary<string, int> {
            { "All", -1 },
            { "Novice", 1 << 0 },
            { "Swordsman", 1 << 1 },
            { "Magician", 1 << 2 },
            { "Archer", 1 << 3 },
            { "Acolyte", 1 << 4 },
            { "Merchant", 1 << 5 },
            { "Thief", 1 << 6 },
            { "Knight", 1 << 7 },
            { "Priest", 1 << 8 },
            { "Wizard", 1 << 9 },
            { "Blacksmith", 1 << 10 },
            { "Hunter", 1 << 11 },
            { "Assassin", 1 << 12 },
            { "Crusader", 1 << 14 },
            { "Monk", 1 << 15 },
            { "Sage", 1 << 16 },
            { "Rogue", 1 << 17 },
            { "Alchemist", 1 << 18 },
            { "Bard", 1 << 19 },
            { "Taekwon", 1 << 21 },
            { "Star_Gladiator", 1 << 22 },
            { "Soul_Linker", 1 << 23 },
            { "Gunslinger", 1 << 24 },
            { "Ninja", 1 << 25 },
            { "Gangsi", 1 << 26 },
            { "Death_Knight", 1 << 27 },
            { "Dark_Collector", 1 << 28 },
            { "Kagerou", 1 << 29 },
            { "Rebellion", 1 << 30 },
        };

        public static readonly Dictionary<string, int> ItemDbJobsYaml = new Dictionary<string, int> {
            { "All", -1 },
            { "Acolyte", 1 << 4 },
            { "Alchemist", 1 << 18 },
            { "Archer", 1 << 3 },
            { "Assassin", 1 << 12 },
            { "BardDancer", 1 << 19 },
            { "Blacksmith", 1 << 10 },
            { "Crusader", 1 << 14 },
            { "Gunslinger", 1 << 24 },
            { "Hunter", 1 << 11 },
            { "KagerouOboro", 1 << 29 },
            { "Knight", 1 << 7 },
            { "Mage", 1 << 2 },
            { "Merchant", 1 << 5 },
            { "Monk", 1 << 15 },
            { "Ninja", 1 << 25 },
            { "Novice", 1 << 0 },
            { "Priest", 1 << 8 },
            { "Rebellion", 1 << 30 },
            { "Rogue", 1 << 17 },
            { "Sage", 1 << 16 },
            { "SoulLinker", 1 << 23 },
            { "StarGladiator", 1 << 22 },
            { "Summoner", 1 << 13 },
            { "SuperNovice", 1 << 20 },
            { "Swordman", 1 << 1 },
            { "Taekwon", 1 << 21 },
            { "Thief", 1 << 6 },
            { "Wizard", 1 << 9 },
        };

        public static readonly Dictionary<string, int> ItemDbJobsYaml2 = new Dictionary<string, int> {
            { "All", -1 },
            { "Acolyte", 1 << 4 },
            { "Alchemist", 1 << 18 },
            { "Archer", 1 << 3 },
            { "Assassin", 1 << 12 },
            { "BardDancer", 1 << 19 },
            { "Blacksmith", 1 << 10 },
            { "Crusader", 1 << 14 },
            { "Gunslinger", 1 << 24 },
            { "Hunter", 1 << 11 },
            { "KagerouOboro", 1 << 29 },
            { "Knight", 1 << 7 },
            { "Mage", 1 << 2 },
            { "Merchant", 1 << 5 },
            { "Monk", 1 << 15 },
            { "Ninja", 1 << 25 },
            { "Novice", 1 << 0 },
            { "Priest", 1 << 8 },
            { "Rebellion", 1 << 30 },
            { "Rogue", 1 << 17 },
            { "Sage", 1 << 16 },
            { "SoulLinker", 1 << 23 },
            { "StarGladiator", 1 << 22 },
            { "Summoner", 1 << 31 },
            { "SuperNovice", 1 << 20 },
            { "Swordman", 1 << 1 },
            { "Taekwon", 1 << 21 },
            { "Thief", 1 << 6 },
            { "Wizard", 1 << 9 },
        };

        public static readonly Dictionary<string, int> ItemDbJobsYaml3 = new Dictionary<string, int> {
            { "All", -1 },
            { "Acolyte", 1 << 4 },
            { "Alchemist", 1 << 18 },
            { "Archer", 1 << 3 },
            { "Assassin", 1 << 12 },
            { "BardDancer", 1 << 19 },
            { "Blacksmith", 1 << 10 },
            { "Crusader", 1 << 14 },
            { "Gunslinger", 1 << 24 },
            { "Hunter", 1 << 11 },
            { "KagerouOboro", 1 << 29 },
            { "Knight", 1 << 7 },
            { "Mage", 1 << 2 },
            { "Merchant", 1 << 5 },
            { "Monk", 1 << 15 },
            { "Ninja", 1 << 25 },
            { "Novice", 1 << 0 },
            { "Priest", 1 << 8 },
            { "Rebellion", 1 << 30 },
            { "Rogue", 1 << 17 },
            { "Sage", 1 << 16 },
            { "SoulLinker", 1 << 23 },
            { "StarGladiator", 1 << 22 },
            { "Summoner", 1 << 31 },
            { "SuperNovice", 1 << 20 },
            { "Swordman", 1 << 1 },
            { "Taekwon", 1 << 21 },
            { "Thief", 1 << 6 },
            { "Wizard", 1 << 9 },
            { "Wizard2", 1 << 20 },
            { "Wizard3", 1 << 26 },
            { "Wizard4", 1 << 27 },
            { "Wizard5", 1 << 28 },
        };
    }
}