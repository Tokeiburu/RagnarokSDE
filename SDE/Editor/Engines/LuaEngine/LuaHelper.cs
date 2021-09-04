using Database;
using ErrorManager;
using GRF.Core.GroupedGrf;
using GRF.FileFormats.LubFormat;
using GRF.IO;
using GRF.System;
using Lua;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines.BackupsEngine;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.PreviewEngine;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.Editor.Jobs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace SDE.Editor.Engines.LuaEngine
{
    public static class LuaHelper
    {
        #region ViewIdTypes enum

        public enum ViewIdTypes
        {
            Shield,
            Weapon,
            Headgear,
            Garment,
            Npc
        }

        #endregion ViewIdTypes enum

        public const string Latin = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_0123456789";
        private static string _debugStatus = "";

        public static void ReloadJobTable(AbstractDb<int> db, bool clearTable = false)
        {
            if (clearTable)
            {
                foreach (var tuple in db.Table.FastItems)
                {
                    tuple.SetRawValue(ServerMobAttributes.ClientSprite, null);
                }
            }

            if (ProjectConfiguration.SynchronizeWithClientDatabases)
            {
                DbDebugItem<int> debug = new DbDebugItem<int>(db);
                DbAttachLuaLoaderUpper(debug, "jobtbl", ProjectConfiguration.SyncMobId);
                var table = db.Attached["jobtbl_T"] as Dictionary<string, string>;

                if (table != null)
                {
                    foreach (var tuple in db.Table.FastItems)
                    {
                        var sprite = tuple.GetValue<string>(ServerMobAttributes.AegisName);

                        if (!String.IsNullOrEmpty(sprite))
                        {
                            table["JT_" + sprite.ToUpper()] = tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture);
                        }
                    }

                    DbLuaLoader(debug, ServerMobAttributes.ClientSprite, "JobNameTable", 0, () => ProjectConfiguration.SyncMobName, p =>
                    {
                        p = p.Trim('[', ']').Replace("jobtbl.", "").ToUpper();
                        string sval;
                        table.TryGetValue(p, out sval);
                        int ival;

                        if (!Int32.TryParse(sval, out ival))
                        {
                            Int32.TryParse(p, out ival);
                        }

                        if (db.Table.ContainsKey(ival))
                            return ival;
                        return 0;
                    }, p => p.Trim('\"'));
                }
            }
        }

        public static void WriteMobLuaFiles(AbstractDb<int> db)
        {
            // Ensures this is only written once
            if (ProjectConfiguration.SynchronizeWithClientDatabases && db.DbSource == ServerDbs.Mobs &&
                ProjectConfiguration.SyncMobTables)
            {
                var metaTable = db.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);
                //var table = Attached["jobtbl_T"] as Dictionary<string, string>;

                // Load the tables
                DbDebugItem<int> debug = new DbDebugItem<int>(db);
                DbAttachLuaLoaderUpper(debug, "jobtbl", ProjectConfiguration.SyncMobId);
                var table = db.Attached["jobtbl_T"] as Dictionary<string, string>;

                if (table != null)
                {
                    Dictionary<int, Npc> npcs = new Dictionary<int, Npc>();

                    var dataJobName = debug.AbsractDb.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.SyncMobName);

                    if (dataJobName == null) return;

                    LuaParser parser = new LuaParser(dataJobName, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(dataJobName), EncodingService.DisplayEncoding);
                    var jobNames = parser.Tables["JobNameTable"];

                    // Load the npcs from the lua files first
                    foreach (var keyPair in table)
                    {
                        npcs[Int32.Parse(keyPair.Value)] = new Npc { NpcName = keyPair.Key };
                    }

                    foreach (var keyPair in jobNames)
                    {
                        var key = keyPair.Key.Trim('[', ']');
                        var ingameSprite = keyPair.Value.Trim('\"');

                        int ival;
                        if (!Int32.TryParse(key, out ival))
                        {
                            key = key.Substring(7);

                            var npcKeyPair = npcs.FirstOrDefault(p => p.Value.NpcName == key);

                            if (npcKeyPair.Equals(default(KeyValuePair<int, Npc>)))
                            {
                                // Key not found
                                // We ignore it
                            }
                            else
                            {
                                npcs[npcKeyPair.Key] = new Npc(npcKeyPair.Value) { IngameSprite = ingameSprite };
                                //npcKeyPair.Value = new ingameSprite;
                            }

                            continue;
                        }

                        npcs[ival] = new Npc { IngameSprite = ingameSprite };
                    }

                    foreach (var tuple in metaTable.FastItems.OrderBy(p => p.Key))
                    {
                        var ssprite = "JT_" + (tuple.GetValue<string>(ServerMobAttributes.AegisName) ?? "");
                        var csprite = tuple.GetValue<string>(ServerMobAttributes.ClientSprite);

                        if (ssprite != "JT_")
                        {
                            // not empty
                            if (npcs.ContainsKey(tuple.Key))
                            {
                                npcs[tuple.Key] = new Npc(npcs[tuple.Key]) { IngameSprite = csprite, NpcName = ssprite };
                            }
                            else
                            {
                                Npc npc = new Npc { IngameSprite = csprite, NpcName = ssprite };
                                npcs[tuple.Key] = npc;
                            }
                        }
                    }

                    // Validation
                    HashSet<string> duplicates = new HashSet<string>();
                    foreach (var npc in npcs)
                    {
                        if (!duplicates.Add(npc.Value.NpcName))
                        {
                            DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "Duplicate mob name (" + npc.Value.NpcName + ") for mobid " + npc.Key + " while saving npcidentity and jobname. The files have not been resaved.");
                            DbIOErrorHandler.Focus();
                            return;
                        }

                        if (LatinOnly(npc.Value.NpcName) != npc.Value.NpcName)
                        {
                            DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "The mob name (" + npc.Value.NpcName + ") is invalid, only ASCII characters are allowed. Consider using '" + LatinOnly(npc.Value.NpcName) + "' as the name instead. The files have not been resaved.");
                            DbIOErrorHandler.Focus();
                            return;
                        }
                    }

                    // Converts back to a lua format
                    {
                        BackupEngine.Instance.BackupClient(ProjectConfiguration.SyncMobId, db.ProjectDatabase.MetaGrf);
                        string file = TemporaryFilesManager.GetTemporaryFilePath("tmp2_{0:0000}.lua");

                        parser.Tables.Clear();
                        var dico = new Dictionary<string, string>();
                        parser.Tables["jobtbl"] = dico;
                        foreach (var npc in npcs.OrderBy(p => p.Key))
                        {
                            dico[npc.Value.NpcName] = npc.Key.ToString(CultureInfo.InvariantCulture);
                        }
                        parser.Write(file, EncodingService.DisplayEncoding);

                        db.ProjectDatabase.MetaGrf.SetData(ProjectConfiguration.SyncMobId, File.ReadAllBytes(file));
                    }

                    {
                        BackupEngine.Instance.BackupClient(ProjectConfiguration.SyncMobName, db.ProjectDatabase.MetaGrf);
                        string file = TemporaryFilesManager.GetTemporaryFilePath("tmp2_{0:0000}.lua");

                        parser.Tables.Clear();
                        var dico = new Dictionary<string, string>();
                        parser.Tables["JobNameTable"] = dico;
                        foreach (var npc in npcs.OrderBy(p => p.Key))
                        {
                            var ingameSprite = LatinUpper((npc.Value.IngameSprite ?? ""));

                            if (!String.IsNullOrEmpty(ingameSprite.GetExtension()))
                                ingameSprite = ingameSprite.ReplaceExtension(ingameSprite.GetExtension().ToLower());

                            if (string.IsNullOrEmpty(ingameSprite)) continue;
                            dico["[jobtbl." + npc.Value.NpcName + "]"] = "\"" + ingameSprite + "\"";
                        }
                        parser.Write(file, EncodingService.DisplayEncoding);

                        db.ProjectDatabase.MetaGrf.SetData(ProjectConfiguration.SyncMobName, File.ReadAllBytes(file));
                    }
                }
            }
        }

        public static void DbLuaLoader<T>(DbDebugItem<T> debug,
            DbAttribute attribute, string tableName, int tableId, Func<string> getPath,
            Func<string, T> getId, Func<string, string> getValue)
        {
            try
            {
                var table = debug.AbsractDb.Table;

                var data = debug.AbsractDb.ProjectDatabase.MetaGrf.GetData(getPath());

                if (data == null) return;
                LuaParser parser = new LuaParser(data, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(data), EncodingService.DisplayEncoding);

                var luaTable = parser.Tables[tableName];

                foreach (var pair in luaTable)
                {
                    T id = getId(pair.Key);

                    if (id.Equals(default(T))) continue;
                    table.SetRaw(id, attribute, getValue(pair.Value));
                }
            }
            catch (Exception err)
            {
                debug.ReportException(err);
            }
        }

        public static void DbAttachLuaLoaderUpper<T>(DbDebugItem<T> debug, string tableName, string path)
        {
            try
            {
                var db = debug.AbsractDb;
                var data = debug.AbsractDb.ProjectDatabase.MetaGrf.GetData(path);

                if (data == null)
                {
                    db.Attached[tableName] = null;
                    db.Attached[tableName + "_T"] = null;
                    return;
                }

                LuaParser parser = new LuaParser(data, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(data), EncodingService.DisplayEncoding);

                try
                {
                    var luaTable = parser.Tables[tableName];
                    Dictionary<string, string> dico = new Dictionary<string, string>();
                    foreach (var pair in luaTable)
                    {
                        dico[pair.Key.Trim('[', ']', '\"').ToUpper()] = pair.Value;
                    }
                    parser.Tables[tableName] = dico;
                    db.Attached[tableName] = parser;
                    db.Attached[tableName + "_T"] = dico;
                }
                catch
                {
                    db.Attached[tableName] = null;
                }
            }
            catch (Exception err)
            {
                debug.ReportException(err);
            }
        }

        public static void DbAttachLuaLoader<T>(DbDebugItem<T> debug, string tableName, string path)
        {
            try
            {
                var db = debug.AbsractDb;
                var data = debug.AbsractDb.ProjectDatabase.MetaGrf.GetData(path);

                if (data == null)
                {
                    db.Attached[tableName] = null;
                    db.Attached[tableName + "_T"] = null;
                    return;
                }

                LuaParser parser = new LuaParser(data, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(data), EncodingService.DisplayEncoding);

                try
                {
                    var luaTable = parser.Tables[tableName];
                    Dictionary<string, string> dico = new Dictionary<string, string>();
                    foreach (var pair in luaTable)
                    {
                        dico[pair.Key.Trim('[', ']', '\"')] = pair.Value;
                    }
                    parser.Tables[tableName] = dico;
                    db.Attached[tableName] = parser;
                    db.Attached[tableName + "_T"] = dico;
                }
                catch
                {
                    db.Attached[tableName] = null;
                }
            }
            catch (Exception err)
            {
                debug.ReportException(err);
            }
        }

        private static int _getNextViewId(ref int viewId, Dictionary<string, int> resourceToIds)
        {
            while (resourceToIds.Values.Contains(viewId))
            {
                viewId++;
            }

            return viewId;
        }

        public static Dictionary<int, string> GetRedirectionTable()
        {
            return new Dictionary<int, string> {
                { 2207, EncodingService.FromAnyToDisplayEncoding("²É") },
                { 2230, null },
                { 2231, null },
                { 5054, EncodingService.FromAnyToDisplayEncoding("¾î»õ½Å¸¶½ºÅ©") },
                { 5097, EncodingService.FromAnyToDisplayEncoding("²¿±ò¸ðÀÚ") },
                { 5190, EncodingService.FromAnyToDisplayEncoding("¾ß±¸¸ðÀÚ") },
                { 5244, EncodingService.FromAnyToDisplayEncoding("´«°¡¸®°³") },
                { 5245, EncodingService.FromAnyToDisplayEncoding("¼±±Û·¡½º") },
                { 5248, EncodingService.FromAnyToDisplayEncoding("¿äÁ¤ÀÇ±Í") },
                { 5249, EncodingService.FromAnyToDisplayEncoding("¿äÁ¤ÀÇ±Í") },
                { 5282, EncodingService.FromAnyToDisplayEncoding("¾ß±¸¸ðÀÚ") },
                { 5394, null },
                { 5516, EncodingService.FromAnyToDisplayEncoding("¿Ü´«¾È°æ") },
                { 5517, EncodingService.FromAnyToDisplayEncoding("¿Ü´«¾È°æ") },
                { 5518, EncodingService.FromAnyToDisplayEncoding("´ëÇü¸¶Á¦½ºÆ½°í¿ìÆ®2") }
            };
        }

        public class PreviewBuffered
        {
            private DateTime _lastRequest;
            public Dictionary<int, string> Ids { get; private set; }
            public string Error { get; private set; }
            public bool Result { get; private set; }

            public PreviewBuffered()
            {
                Ids = new Dictionary<int, string>();
                _lastRequest = new DateTime(DateTime.Now.Ticks - 2000000000);
            }

            public bool IsBuffered()
            {
                if ((DateTime.Now - _lastRequest).Seconds < 3)
                {
                    _lastRequest = DateTime.Now;
                    return true;
                }

                return false;
            }

            public void Buffer(Dictionary<int, string> ids, bool result, string error)
            {
                _lastRequest = DateTime.Now;
                Ids = ids;
                Result = result;
                Error = error;
            }
        }

        private static readonly PreviewBuffered _headgearBuffer = new PreviewBuffered();
        private static readonly PreviewBuffered _weaponBuffer = new PreviewBuffered();
        private static readonly PreviewBuffered _shieldBuffer = new PreviewBuffered();
        private static readonly PreviewBuffered _garmentBuffer = new PreviewBuffered();
        private static readonly PreviewBuffered _npcBuffer = new PreviewBuffered();

        public static bool GetIdToSpriteTable(AbstractDb<int> db, ViewIdTypes type, out Dictionary<int, string> outputIdsToSprites, out string error)
        {
            outputIdsToSprites = new Dictionary<int, string>();
            error = null;

            if (db.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.SyncAccId) == null || db.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.SyncAccName) == null)
            {
                error = "The accessory ID table or accessory name table has not been set, the paths are based on those.";
                return false;
            }

            int temp_i;
            string temp_s;
            var accIdPath = ProjectConfiguration.SyncAccId;
            Dictionary<string, int> ids;

            switch (type)
            {
                case ViewIdTypes.Weapon:
                    if (_weaponBuffer.IsBuffered())
                    {
                        outputIdsToSprites = _weaponBuffer.Ids;
                        error = _weaponBuffer.Error;
                        return _weaponBuffer.Result;
                    }

                    var weaponPath = GrfPath.Combine(GrfPath.GetDirectoryName(accIdPath), "weapontable" + Path.GetExtension(accIdPath));
                    var weaponData = db.ProjectDatabase.MetaGrf.GetData(weaponPath);

                    if (weaponData == null)
                    {
                        error = "Couldn't find " + weaponPath;
                        _weaponBuffer.Buffer(outputIdsToSprites, false, error);
                        return false;
                    }

                    var weaponTable = new LuaParser(weaponData, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(weaponData), EncodingService.DisplayEncoding);
                    var weaponIds = GetLuaTable(weaponTable, "Weapon_IDs");
                    var weaponNameTable = GetLuaTable(weaponTable, "WeaponNameTable");

                    ids = SetIds(weaponIds, "Weapon_IDs");

                    foreach (var pair in weaponNameTable)
                    {
                        temp_s = pair.Key.Trim('[', ']');

                        if (ids.TryGetValue(temp_s, out temp_i) || Int32.TryParse(temp_s, out temp_i))
                        {
                            outputIdsToSprites[temp_i] = pair.Value.Trim('\"');
                        }
                    }

                    _weaponBuffer.Buffer(outputIdsToSprites, true, null);
                    return true;

                case ViewIdTypes.Npc:
                    if (_npcBuffer.IsBuffered())
                    {
                        outputIdsToSprites = _npcBuffer.Ids;
                        error = _npcBuffer.Error;
                        return _npcBuffer.Result;
                    }

                    var npcPathSprites = GrfPath.Combine(GrfPath.GetDirectoryName(accIdPath), "jobname" + Path.GetExtension(accIdPath));
                    var npcPathIds = GrfPath.Combine(GrfPath.GetDirectoryName(accIdPath), "npcIdentity" + Path.GetExtension(accIdPath));
                    var npcDataSprites = db.ProjectDatabase.MetaGrf.GetData(npcPathSprites);
                    var npcDataIds = db.ProjectDatabase.MetaGrf.GetData(npcPathIds);

                    if (npcDataSprites == null)
                    {
                        error = "Couldn't find " + npcPathSprites;
                        _npcBuffer.Buffer(outputIdsToSprites, false, error);
                        return false;
                    }

                    if (npcDataIds == null)
                    {
                        error = "Couldn't find " + npcPathIds;
                        _npcBuffer.Buffer(outputIdsToSprites, false, error);
                        return false;
                    }

                    //var itemDb = db.GetMeta<int>(ServerDbs.Items);
                    var jobname = new LuaParser(npcDataSprites, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(npcDataSprites), EncodingService.DisplayEncoding);
                    var jobtbl = new LuaParser(npcDataIds, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(npcDataIds), EncodingService.DisplayEncoding);

                    var jobtblT = GetLuaTable(jobtbl, "jobtbl");
                    var jobnameT = GetLuaTable(jobname, "JobNameTable");

                    ids = SetIds(jobtblT, "jobtbl");

                    foreach (var pair in jobnameT)
                    {
                        temp_s = pair.Key.Trim('[', ']');

                        if (ids.TryGetValue(temp_s, out temp_i) || Int32.TryParse(temp_s, out temp_i))
                        {
                            outputIdsToSprites[temp_i] = pair.Value.Trim('\"');
                        }
                    }

                    _npcBuffer.Buffer(outputIdsToSprites, true, null);
                    return true;

                case ViewIdTypes.Headgear:
                    if (_headgearBuffer.IsBuffered())
                    {
                        outputIdsToSprites = _headgearBuffer.Ids;
                        error = _headgearBuffer.Error;
                        return _headgearBuffer.Result;
                    }

                    var redirectionTable = GetRedirectionTable();
                    var dataAccId = db.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.SyncAccId);
                    var dataAccName = db.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.SyncAccName);
                    var itemDb = db.GetMeta<int>(ServerDbs.Items);
                    var accId = new LuaParser(dataAccId, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(dataAccId), EncodingService.DisplayEncoding);
                    var accName = new LuaParser(dataAccName, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(dataAccName), EncodingService.DisplayEncoding);
                    var accIdT = GetLuaTable(accId, "ACCESSORY_IDs");
                    var accNameT = GetLuaTable(accName, "AccNameTable");
                    outputIdsToSprites = _getViewIdTable(accIdT, accNameT);

                    accIdT.Clear();
                    accNameT.Clear();

                    var resourceToIds = new Dictionary<string, int>();

                    if (ProjectConfiguration.HandleViewIds)
                    {
                        try
                        {
                            List<ReadableTuple<int>> headgears = itemDb.FastItems.Where(p => ItemParser.IsArmorType(p) && (p.GetIntNoThrow(ServerItemAttributes.Location) & 7937) != 0).OrderBy(p => p.GetIntNoThrow(ServerItemAttributes.ClassNumber)).ToList();
                            _loadFallbackValues(outputIdsToSprites, headgears, accIdT, accNameT, resourceToIds, redirectionTable);
                        }
                        catch (Exception err)
                        {
                            error = err.ToString();
                            _headgearBuffer.Buffer(outputIdsToSprites, false, error);
                            return false;
                        }
                    }

                    _headgearBuffer.Buffer(outputIdsToSprites, true, null);
                    return true;

                case ViewIdTypes.Shield:
                    if (_shieldBuffer.IsBuffered())
                    {
                        outputIdsToSprites = _shieldBuffer.Ids;
                        error = _shieldBuffer.Error;
                        return _shieldBuffer.Result;
                    }

                    var shieldPath = GrfPath.Combine(GrfPath.GetDirectoryName(accIdPath), "ShieldTable" + Path.GetExtension(accIdPath));
                    var shieldData = db.ProjectDatabase.MetaGrf.GetData(shieldPath);

                    if (shieldData == null)
                    {
                        outputIdsToSprites[1] = "_°¡µå";
                        outputIdsToSprites[2] = "_¹öÅ¬·¯";
                        outputIdsToSprites[3] = "_½¯µå";
                        outputIdsToSprites[4] = "_¹Ì·¯½¯µå";
                        outputIdsToSprites[5] = "";
                        outputIdsToSprites[6] = "";
                    }
                    else
                    {
                        _debugStatus = "OK";

                        var shieldTable = new LuaParser(shieldData, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(shieldData), EncodingService.DisplayEncoding);

                        _debugStatus = "LoadTables";

                        var shieldIds = GetLuaTable(shieldTable, "Shield_IDs");
                        var shieldNameTable = GetLuaTable(shieldTable, "ShieldNameTable");
                        var shieldMapTable = GetLuaTable(shieldTable, "ShieldMapTable");

                        ids = SetIds(shieldIds, "Shield_IDs");
                        Dictionary<int, string> idsToSprite = new Dictionary<int, string>();

                        foreach (var pair in shieldNameTable)
                        {
                            temp_s = pair.Key.Trim('[', ']');

                            if (ids.TryGetValue(temp_s, out temp_i) || Int32.TryParse(temp_s, out temp_i))
                            {
                                temp_s = pair.Value.Trim('\"');
                                idsToSprite[temp_i] = temp_s;
                                outputIdsToSprites[temp_i] = temp_s;
                            }
                        }

                        foreach (var pair in shieldMapTable)
                        {
                            var key = pair.Key.Trim('[', ']', '\t');
                            int id1;

                            if (ids.TryGetValue(key, out id1))
                            {
                                int id2;
                                temp_s = pair.Value.Trim('\"', '\t');

                                if (ids.TryGetValue(temp_s, out id2) || Int32.TryParse(temp_s, out id2))
                                {
                                    if (idsToSprite.TryGetValue(id2, out temp_s))
                                    {
                                        outputIdsToSprites[id1] = temp_s;
                                    }
                                }
                            }
                        }

                        error = PreviewHelper.ViewIdIncrease;
                    }

                    _shieldBuffer.Buffer(outputIdsToSprites, true, error);
                    return true;

                case ViewIdTypes.Garment:
                    if (_garmentBuffer.IsBuffered())
                    {
                        outputIdsToSprites = _garmentBuffer.Ids;
                        error = _garmentBuffer.Error;
                        return _garmentBuffer.Result;
                    }

                    var robeSpriteName = GrfPath.Combine(GrfPath.GetDirectoryName(accIdPath), "spriterobename" + Path.GetExtension(accIdPath));
                    var robeSpriteId = GrfPath.Combine(GrfPath.GetDirectoryName(accIdPath), "spriterobeid" + Path.GetExtension(accIdPath));
                    var robeNameData = db.ProjectDatabase.MetaGrf.GetData(robeSpriteName);
                    var robeIdData = db.ProjectDatabase.MetaGrf.GetData(robeSpriteId);

                    if (robeNameData == null)
                    {
                        error = "Couldn't find " + robeSpriteName;
                        _garmentBuffer.Buffer(outputIdsToSprites, false, error);
                        return false;
                    }

                    if (robeIdData == null)
                    {
                        error = "Couldn't find " + robeSpriteId;
                        _garmentBuffer.Buffer(outputIdsToSprites, false, error);
                        return false;
                    }

                    var robeNameTable = new LuaParser(robeNameData, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(robeNameData), EncodingService.DisplayEncoding);
                    var robeIdTable = new LuaParser(robeIdData, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(robeIdData), EncodingService.DisplayEncoding);
                    var robeNames = GetLuaTable(robeNameTable, "RobeNameTable");
                    var robeIds = GetLuaTable(robeIdTable, "SPRITE_ROBE_IDs");

                    ids = SetIds(robeIds, "SPRITE_ROBE_IDs");

                    foreach (var pair in robeNames)
                    {
                        temp_s = pair.Key.Trim('[', ']');

                        if (ids.TryGetValue(temp_s, out temp_i) || Int32.TryParse(temp_s, out temp_i))
                        {
                            outputIdsToSprites[temp_i] = pair.Value.Trim('\"');
                        }
                    }

                    _garmentBuffer.Buffer(outputIdsToSprites, true, null);
                    return true;
            }

            return false;
        }

        public static Dictionary<string, int> SetIds(Dictionary<string, string> inputTable, string tableId)
        {
            Dictionary<string, int> ids = new Dictionary<string, int>();
            string identifier = tableId + ".";
            foreach (var pair in inputTable)
            {
                ids[identifier + pair.Key] = Int32.Parse(pair.Value);
            }
            return ids;
        }

        public static string GetSpriteFromViewId(int viewIdToFind, ViewIdTypes type, SdeDatabase db, ReadableTuple<int> tuple)
        {
            return GetSpriteFromViewId(viewIdToFind, type, db.GetDb<int>(ServerDbs.Items), tuple);
        }

        public static string GetSpriteFromViewId(int viewIdToFind, ViewIdTypes type, AbstractDb<int> db, ReadableTuple<int> tuple)
        {
            string error;
            Dictionary<int, string> idsToSprite;

            if (GetIdToSpriteTable(db, type, out idsToSprite, out error))
            {
                if (error == PreviewHelper.ViewIdIncrease && tuple != null)
                {
                    var itemKey = tuple.GetKey<int>();

                    if (itemKey >= 2101)
                    {
                        viewIdToFind = itemKey;
                    }
                }

                if (viewIdToFind == 0 && tuple != null)
                {
                    var itemKey = tuple.Key;

                    if (itemKey == 2230 || itemKey == 2231 || itemKey == 5394)
                    {
                        return PreviewHelper.SpriteNone;
                    }
                }

                if (idsToSprite.ContainsKey(viewIdToFind))
                {
                    return idsToSprite[viewIdToFind];
                }
            }
            else
            {
                throw new Exception(error);
            }

            return null;
        }

        private static Dictionary<Job, string> _getJobToPath(string sub, GenderType gender)
        {
            if (_isShield(sub))
            {
                return new Dictionary<Job, string> {
                    { JobList.Novice, EncodingService.FromAnyToDisplayEncoding("ÃÊº¸ÀÚ") },
                    { JobList.Swordman, EncodingService.FromAnyToDisplayEncoding("°Ë»ç") },
                    { JobList.Mage, EncodingService.FromAnyToDisplayEncoding("¸¶¹ý»ç") },
                    { JobList.Archer, EncodingService.FromAnyToDisplayEncoding("±Ã¼ö") },
                    { JobList.Acolyte, EncodingService.FromAnyToDisplayEncoding("¼ºÁ÷ÀÚ") },
                    { JobList.Merchant, EncodingService.FromAnyToDisplayEncoding("»óÀÎ") },
                    { JobList.Thief, EncodingService.FromAnyToDisplayEncoding("µµµÏ") },
                    { JobList.Knight, EncodingService.FromAnyToDisplayEncoding("±â»ç") },
                    { JobList.Priest, EncodingService.FromAnyToDisplayEncoding("ÇÁ¸®½ºÆ®") },
                    { JobList.Wizard, EncodingService.FromAnyToDisplayEncoding("À§Àúµå") },
                    { JobList.Blacksmith, EncodingService.FromAnyToDisplayEncoding("Á¦Ã¶°ø") },
                    { JobList.Hunter, EncodingService.FromAnyToDisplayEncoding("ÇåÅÍ") },
                    { JobList.Assassin, EncodingService.FromAnyToDisplayEncoding("¾î¼¼½Å") },
                    { JobList.Crusader, EncodingService.FromAnyToDisplayEncoding("Å©·ç¼¼ÀÌ´õ") },
                    { JobList.Monk, EncodingService.FromAnyToDisplayEncoding("¸ùÅ©") },
                    { JobList.Sage, EncodingService.FromAnyToDisplayEncoding("¼¼ÀÌÁö") },
                    { JobList.Rogue, EncodingService.FromAnyToDisplayEncoding("·Î±×") },
                    { JobList.Alchemist, EncodingService.FromAnyToDisplayEncoding("¿¬±Ý¼ú»ç") },
                    { JobList.BardDancer, EncodingService.FromAnyToDisplayEncoding(gender == GenderType.Male ? "¹Ùµå" : "¹«Èñ") },
                };
            }
            if (sub == null)
            {
                var dico = new Dictionary<Job, string>();

                foreach (var job in JobList.AllJobs.Where(p => !string.IsNullOrEmpty(p.SpriteName) && !JobGroup.Baby2.Is(p.Upper) && !JobGroup.Baby3.Is(p.Upper)))
                {
                    dico[job] = EncodingService.FromAnyToDisplayEncoding(job.SpriteName);
                }

                return dico;
            }
            return new Dictionary<Job, string> {
                { JobList.Novice, EncodingService.FromAnyToDisplayEncoding("ÃÊº¸ÀÚ") },
                { JobList.Swordman, EncodingService.FromAnyToDisplayEncoding("°Ë»ç") },
                { JobList.Mage, EncodingService.FromAnyToDisplayEncoding("¸¶¹ý»ç") },
                { JobList.Archer, EncodingService.FromAnyToDisplayEncoding("±Ã¼ö") },
                { JobList.Acolyte, EncodingService.FromAnyToDisplayEncoding("¼ºÁ÷ÀÚ") },
                { JobList.Merchant, EncodingService.FromAnyToDisplayEncoding("»óÀÎ") },
                { JobList.Thief, EncodingService.FromAnyToDisplayEncoding("µµµÏ") },
                { JobList.Knight, EncodingService.FromAnyToDisplayEncoding("±â»ç") },
                { JobList.Priest, EncodingService.FromAnyToDisplayEncoding("ÇÁ¸®½ºÆ®") },
                { JobList.Wizard, EncodingService.FromAnyToDisplayEncoding("À§Àúµå") },
                { JobList.Blacksmith, EncodingService.FromAnyToDisplayEncoding("Á¦Ã¶°ø") },
                { JobList.Hunter, EncodingService.FromAnyToDisplayEncoding("ÇåÅÍ") },
                { JobList.Assassin, EncodingService.FromAnyToDisplayEncoding("¾î¼¼½Å") },
                { JobList.Crusader, EncodingService.FromAnyToDisplayEncoding("Å©·ç¼¼ÀÌ´õ") },
                { JobList.Monk, EncodingService.FromAnyToDisplayEncoding("¸ùÅ©") },
                { JobList.Sage, EncodingService.FromAnyToDisplayEncoding("¼¼ÀÌÁö") },
                { JobList.Rogue, EncodingService.FromAnyToDisplayEncoding("·Î±×") },
                { JobList.Alchemist, EncodingService.FromAnyToDisplayEncoding("¿¬±Ý¼ú»ç") },
                { JobList.BardDancer, EncodingService.FromAnyToDisplayEncoding(gender == GenderType.Male ? "¹Ùµå" : "¹«Èñ") },
                { JobList.ShadowChaser, EncodingService.FromAnyToDisplayEncoding("½¦µµ¿ìÃ¼ÀÌ¼­") },
                { JobList.Taekwon, EncodingService.FromAnyToDisplayEncoding("ÅÂ±Ç¼Ò³â") },
                { JobList.Ninja, EncodingService.FromAnyToDisplayEncoding("´ÑÀÚ") },
                { JobList.Gunslinger, EncodingService.FromAnyToDisplayEncoding("°Ç³Ê") },
                { JobList.KagerouOboro, EncodingService.FromAnyToDisplayEncoding(gender == GenderType.Male ? "kagerou" : "oboro") },
            };
        }

        private static bool _isShield(string sub)
        {
            if (sub == null)
                return false;
            return EncodingService.FromAnyTo(sub, EncodingService.Ansi) == "¹æÆÐ";
        }

        public static string GetSpriteFromJob(MultiGrfReader grf, Job job, PreviewHelper helper, string sprite, ViewIdTypes type)
        {
            switch (type)
            {
                case ViewIdTypes.Garment:
                    return GetSpritePathFromJob(grf, job, @"data\sprite\·Îºê\" + sprite + @"\" + helper.GenderString + "\\{0}_" + helper.GenderString, helper.Gender, null, sprite);

                case ViewIdTypes.Shield:
                    return GetSpritePathFromJob(grf, job, @"data\sprite\¹æÆÐ\{0}\{0}_" + helper.GenderString + sprite, helper.Gender, "¹æÆÐ", sprite);

                case ViewIdTypes.Weapon:
                    return GetSpritePathFromJob(grf, job, @"data\sprite\ÀÎ°£Á·\{0}\{0}_" + helper.GenderString + sprite, helper.Gender, "ÀÎ°£Á·", sprite);

                case ViewIdTypes.Headgear:
                    return EncodingService.FromAnyToDisplayEncoding(@"data\sprite\¾Ç¼¼»ç¸®\" + helper.GenderString + "\\" + EncodingService.FromAnyToDisplayEncoding(helper.GenderString + "_") + helper.PreviewSprite);

                case ViewIdTypes.Npc:
                    if (helper.PreviewSprite != null && helper.PreviewSprite.EndsWith(".gr2", StringComparison.OrdinalIgnoreCase))
                    {
                        return EncodingService.FromAnyToDisplayEncoding(@"data\model\3dmob\" + helper.PreviewSprite);
                    }

                    return EncodingService.FromAnyToDisplayEncoding(@"data\sprite\npc\" + helper.PreviewSprite);
            }

            return null;
        }

        public static string GetSpritePathFromJob(MultiGrfReader grf, Job job, string spriteFormat, GenderType gender, string folder, string sprite, int level = 0)
        {
            if (sprite == PreviewHelper.SpriteNone)
                return PreviewHelper.SpriteNone;

            spriteFormat = EncodingService.FromAnyToDisplayEncoding(spriteFormat);

            var dico = _getJobToPath(folder, gender);
            string subPath;

            Job current = job;

            // Remove baby jobs
            if (JobGroup.Baby2.Is(current.Upper))
                current = Job.Get(current.Id, JobGroup.Normal2);

            if (JobGroup.Baby3.Is(current.Upper))
                current = Job.Get(current.Id, JobGroup.Normal3);

            if (!dico.ContainsKey(current))
            {
                // Find the job by its parent
                while (current != null)
                {
                    // Remove trans jobs
                    if (JobGroup.Trans2.Is(current.Upper))
                        current = Job.Get(current.Id, JobGroup.Normal2);

                    if (JobGroup.Trans3.Is(current.Upper))
                        current = Job.Get(current.Id, JobGroup.Normal3);

                    if (dico.ContainsKey(current))
                    {
                        break;
                    }

                    if (current.Parent != null && current.Parent.Id != JobList.Novice.Id)
                        current = current.Parent;
                    else
                        break;
                }
            }

            if (current == null || !dico.ContainsKey(current))
                subPath = job.SpriteName;
            else
                subPath = dico[current];

            string path = String.Format(spriteFormat, subPath);

            if (level == 0 && (grf.GetData(path + ".act") == null && job.Parent != null))
            {
                if (job.Parent.Id != JobList.Novice.Id)
                {
                    path = GetSpritePathFromJob(grf, job.Parent, spriteFormat, gender, folder, sprite, level + 1);

                    if (grf.GetData(path + ".act") == null && job.Parent.Parent != null)
                    {
                        if (job.Parent.Parent.Id != JobList.Novice.Id)
                        {
                            path = GetSpritePathFromJob(grf, job.Parent.Parent, spriteFormat, gender, folder, sprite, level + 2);
                        }
                    }
                }
            }

            //if (level == 0) {
            //	if (grf.GetData(path + ".act") == null) {
            //		return PreviewHelper.SpriteMissing;
            //	}
            //}

            return path;
        }

        public static void WriteViewIds(ServerDbs dbSource, AbstractDb<int> db)
        {
            if (ProjectConfiguration.SynchronizeWithClientDatabases && dbSource == ServerDbs.Items &&
                ProjectConfiguration.HandleViewIds)
            {
                //return;
                int debugInfo = 0;
                _debugStatus = "OK";

                var dataAccId = db.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.SyncAccId);
                var dataAccName = db.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.SyncAccName);

                if (dataAccId != null && dataAccName != null)
                {
                    var itemDb1 = db.Get<int>(ServerDbs.Items);
                    var itemDb2 = db.Get<int>(ServerDbs.Items2);
                    var citemDb = db.Get<int>(ServerDbs.CItems);
                    debugInfo++;

                    try
                    {
                        itemDb1.Commands.Begin();
                        itemDb2.Commands.Begin();
                        citemDb.Commands.Begin();
                        debugInfo++;

                        AccessoryTable table = new AccessoryTable(db, dataAccId, dataAccName);
                        table.SetLuaTables();
                        table.SetTables();
                        table.SetDbs();

                        _debugStatus = "BackupManager";
                        BackupEngine.Instance.BackupClient(ProjectConfiguration.SyncAccId, db.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.SyncAccId));
                        BackupEngine.Instance.BackupClient(ProjectConfiguration.SyncAccName, db.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.SyncAccName));
                        debugInfo++;

                        _debugStatus = "WriteLuaFiles";
                        _writeLuaFiles(table.LuaAccIdParser, table.LuaAccNameParser, db);
                        debugInfo++;
                    }
                    catch (Exception err)
                    {
                        ErrorHandler.HandleException("Couldn't save the accessory item files. Error code = " + debugInfo + ", state = " + _debugStatus, err, ErrorLevel.Low);
                        DbIOErrorHandler.Handle(err, "Generic exception while trying to save the client accessory items, debug code = " + debugInfo, ErrorLevel.NotSpecified);
                        DbIOErrorHandler.Focus();
                    }
                    finally
                    {
                        itemDb1.Commands.End();
                        itemDb2.Commands.End();
                        citemDb.Commands.End();
                    }
                }
            }
        }

        internal static Dictionary<string, string> GetLuaTable(LuaParser parser, string tId)
        {
            if (parser.Tables.Keys.Any(p => String.Compare(tId, p, StringComparison.OrdinalIgnoreCase) == 0))
            {
                return parser.Tables.FirstOrDefault(p => String.Compare(tId, p.Key, StringComparison.OrdinalIgnoreCase) == 0).Value;
            }

            _debugStatus += "#" + tId + " missing";
            throw new Exception("Invalid table file (lua/lub), missing '" + tId + "'. Tables found: " + Methods.Aggregate(parser.Tables.Keys.ToList(), ", "));
        }

        private static void _writeLuaFiles(LuaParser accId, LuaParser accName, AbstractDb<int> db)
        {
            string file = TemporaryFilesManager.GetTemporaryFilePath("tmp2_{0:0000}.lua");
            accId.Write(file, EncodingService.DisplayEncoding);
            db.ProjectDatabase.MetaGrf.SetData(ProjectConfiguration.SyncAccId, File.ReadAllBytes(file));

            file = TemporaryFilesManager.GetTemporaryFilePath("tmp2_{0:0000}.lua");
            accName.Write(file, EncodingService.DisplayEncoding);
            db.ProjectDatabase.MetaGrf.SetData(ProjectConfiguration.SyncAccName, File.ReadAllBytes(file));
        }

        private static void _loadFallbackValues(Dictionary<int, string> fallbackSprites, List<ReadableTuple<int>> headgears, IDictionary<string, string> accIdT, IDictionary<string, string> accNameT, IDictionary<string, int> resourceToIds, Dictionary<int, string> redirectionTable)
        {
            TkDictionary<int, ReadableTuple<int>> buffered = new TkDictionary<int, ReadableTuple<int>>();
            var rRedirectionTable = new HashSet<string>();

            foreach (var headgear in headgears)
            {
                if (!buffered.ContainsKey(headgear.Key))
                {
                    buffered[headgear.Key] = headgear;
                }
            }

            foreach (var pair in redirectionTable)
            {
                rRedirectionTable.Add(pair.Value);
            }

            foreach (var keyPair in fallbackSprites)
            {
                if (rRedirectionTable.Contains(keyPair.Value)) continue;
                if (keyPair.Key <= 0) continue; // throw new Exception("View ID cannot be equal or below 0.");

                var sTuple = buffered[keyPair.Key]; // headgears.FirstOrDefault(p => p.GetIntNoThrow(ServerItemAttributes.ClassNumber) == keyPair.Key);
                string accessoryName;

                if (sTuple != null)
                    accessoryName = GetAccAegisNameFromTuple(sTuple);
                else
                    // No item associated with this view ID
                    accessoryName = String.Format("UNREGISTERED_{0:0000}", keyPair.Key);

                // Bogus entry - entry by number
                if (keyPair.Key.ToString(CultureInfo.InvariantCulture) == keyPair.Value) continue;

                accIdT["ACCESSORY_" + accessoryName] = keyPair.Key.ToString(CultureInfo.InvariantCulture);
                accNameT["[ACCESSORY_IDs.ACCESSORY_" + accessoryName + "]"] = "\"_" + keyPair.Value + "\"";
                resourceToIds[keyPair.Value] = keyPair.Key;
            }
        }

        public static string LatinOnly(string value)
        {
            StringBuilder builder = new StringBuilder();
            char c;

            for (int i = 0; i < value.Length; i++)
            {
                c = value[i];

                if (Latin.Contains(c))
                {
                    builder.Append(value[i]);
                }
                else
                {
                    builder.Append('_');
                }
            }

            return builder.ToString();
        }

        public static bool IsLatinOnly(string value)
        {
            return value.All(c => Latin.Contains(c));
        }

        public static string LatinUpper(string value)
        {
            StringBuilder builder = new StringBuilder();
            char c;

            for (int i = 0; i < value.Length; i++)
            {
                c = value[i];

                if (Latin.Contains(c))
                {
                    builder.Append(char.ToUpperInvariant(value[i]));
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        public static string GetAccAegisNameFromTuple(Database.Tuple tuple)
        {
            string accessoryName = tuple.GetValue<string>(ServerItemAttributes.AegisName.Index);
            return LatinOnly(accessoryName);
        }

        private static Dictionary<int, string> _getViewIdTable(Dictionary<string, string> accIdT, Dictionary<string, string> accNameT)
        {
            Dictionary<int, string> viewId = new Dictionary<int, string>();

            foreach (var pair in accIdT)
            {
                var key = "[ACCESSORY_IDs." + pair.Key + "]";

                if (accNameT.ContainsKey(key))
                {
                    int ival;

                    if (Int32.TryParse(pair.Value, out ival))
                    {
                        var sprite = accNameT[key].Trim('\"');

                        if (sprite.Length > 1)
                            sprite = sprite.Substring(1);

                        if (ival.ToString(CultureInfo.InvariantCulture) == sprite)
                        {
                            continue;
                        }

                        viewId[ival] = sprite;
                    }
                }
            }

            foreach (var pair in accNameT)
            {
                var key = pair.Key.Trim('[', ']');

                int ival;

                if (Int32.TryParse(key, out ival))
                {
                    var sprite = pair.Value.Trim('\"');

                    if (sprite.Length > 1)
                        sprite = sprite.Substring(1);

                    if (ival.ToString(CultureInfo.InvariantCulture) == sprite)
                    {
                        continue;
                    }

                    viewId[ival] = sprite;
                }
            }

            return viewId;
        }
    }
}