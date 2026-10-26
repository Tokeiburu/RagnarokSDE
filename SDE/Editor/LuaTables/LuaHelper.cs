using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ErrorManager;
using GRF.Core.GroupedGrf;
using GRF.FileFormats.LubFormat;
using GRF.IO;
using GRF.GrfSystem;
using Lua;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines.PreviewEngine;
using SDE.Editor.Generic.Parsers.Generic;
using Utilities;
using Utilities.Extension;
using Utilities.Services;
using SDE.Databases.Generic.Common;
using SDE.Databases.Items.Features;
using SDE.Databases.Generic.Common.Jobs;
using SDE.Databases.Mobs.Features;
using SDE.View;
using Lua.Structure;
using SDE.Databases.Items.Common;
using SDE.Databases;
using SDE.Editor.Database;
using SDE.Editor.Backups;
using SDE.Editor.Files;

namespace SDE.Editor.LuaTables {
	public static class LuaHelper {
		#region ViewIdTypes enum
		public enum ViewIdTypes {
			Shield,
			Weapon,
			Headgear,
			Garment,
			Npc
		}
		#endregion

		static LuaHelper() {
			foreach (var c in Latin)
				_latinCheck[c] = true;
		}

		private static bool[] _latinCheck = new bool[256];
		public const string Latin = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_0123456789";
		private static string _debugStatus = "";

		public static void ReloadJobTable(BaseDatabase db, bool clearTable = false) {
			if (clearTable) {
				foreach (var tuple in db.Table.FastItems) {
					tuple.GetModel<Mob>().ClientSprite = null;
				}
			}
			
			if (ProjectConfiguration.SynchronizeWithClientDatabases) {
				DbSaveContext context = new DbSaveContext(db);
				DbAttachLuaLoaderUpper(context, "jobtbl", ProjectConfiguration.SyncMobId);
				var table = db.Attached["jobtbl_T"] as Dictionary<string, string>;
			
				if (table != null) {
					foreach (var tuple in db.Table.FastItems) {
						var sprite = tuple.GetModel<Mob>().AegisName;
			
						if (!string.IsNullOrEmpty(sprite)) {
							table["JT_" + sprite.ToUpper()] = tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture);
						}
					}
			
					DbLuaLoader(context, "JobNameTable", 0, () => ProjectConfiguration.SyncMobName, p => {
						p = p.Trim('[', ']').Replace("jobtbl.", "").ToUpper();
						string sval;
						table.TryGetValue(p, out sval);
						int ival;
			
						if (!int.TryParse(sval, out ival)) {
							int.TryParse(p, out ival);
						}
			
						if (db.Table.ContainsKey(ival))
							return ival;
						return 0;
					}, p => p.Trim('\"'));
				}
			}
		}

		public static void WriteMobLuaFiles(BaseDatabase db) {
			// Ensures this is only written once
			if (ProjectConfiguration.SynchronizeWithClientDatabases && db.Source == DataSources.Mob &&
			    ProjectConfiguration.SyncMobTables) {
				var metaTable = SdeEditor.Project.GetMergedTable(DataSources.Mob);
				var metaGrf = SdeEditor.MetaGrf;
				//var table = Attached["jobtbl_T"] as Dictionary<string, string>;
			
				// Load the tables
				DbSaveContext context = new DbSaveContext(db);
				DbAttachLuaLoaderUpper(context, "jobtbl", ProjectConfiguration.SyncMobId);
				var table = db.Attached["jobtbl_T"] as Dictionary<string, string>;
			
				if (table != null) {
					Dictionary<int, Npc> npcs = new Dictionary<int, Npc>();
			
					var dataJobName = metaGrf.GetData(ProjectConfiguration.SyncMobName);
			
					if (dataJobName == null) return;
			
					LuaParser parser = new LuaParser(dataJobName, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(dataJobName), EncodingService.DisplayEncoding);
					var jobNames = parser.Tables["JobNameTable"];
			
					// Load the npcs from the lua files first
					foreach (var keyPair in table) {
						npcs[int.Parse(keyPair.Value)] = new Npc { NpcName = keyPair.Key };
					}
			
					foreach (var keyPair in jobNames) {
						var key = keyPair.Key.Trim('[', ']');
						var ingameSprite = keyPair.Value.Trim('\"');
			
						int ival;
						if (!int.TryParse(key, out ival)) {
							key = key.Substring(7);
			
							var npcKeyPair = npcs.FirstOrDefault(p => p.Value.NpcName == key);
			
							if (npcKeyPair.Equals(default(KeyValuePair<int, Npc>))) {
								// Key not found
								// We ignore it
							}
							else {
								npcs[npcKeyPair.Key] = new Npc(npcKeyPair.Value) { IngameSprite = ingameSprite };
								//npcKeyPair.Value = new ingameSprite;
							}
			
							continue;
						}
			
						npcs[ival] = new Npc { IngameSprite = ingameSprite };
					}
			
					foreach (var tuple in metaTable.FastItems.OrderBy(p => p.Key)) {
						var model = tuple.GetModel<Mob>();
						var ssprite = "JT_" + (model.AegisName ?? "");
						var csprite = model.ClientSprite;
			
						if (ssprite != "JT_") {
							// not empty
							if (npcs.ContainsKey(tuple.Key)) {
								npcs[tuple.Key] = new Npc(npcs[tuple.Key]) { IngameSprite = csprite, NpcName = ssprite };
							}
							else {
								Npc npc = new Npc { IngameSprite = csprite, NpcName = ssprite };
								npcs[tuple.Key] = npc;
							}
						}
					}
			
					// Validation
					HashSet<string> duplicates = new HashSet<string>();
					foreach (var npc in npcs) {
						if (!duplicates.Add(npc.Value.NpcName)) {
							DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "Duplicate mob name (" + npc.Value.NpcName + ") for mobid " + npc.Key + " while saving npcidentity and jobname. The files have not been resaved.");
							DbIOErrorHandler.Focus();
							return;
						}
			
						if (LatinOnly(npc.Value.NpcName) != npc.Value.NpcName) {
							DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "The mob name (" + npc.Value.NpcName + ") is invalid, only ASCII characters are allowed. Consider using '" + LatinOnly(npc.Value.NpcName) + "' as the name instead. The files have not been resaved.");
							DbIOErrorHandler.Focus();
							return;
						}
					}
			
					// Converts back to a lua format
					{
						BackupManager.Instance.BackupClient(ProjectConfiguration.SyncMobId);
						string file = TemporaryFilesManager.GetTemporaryFilePath("tmp2_{0:0000}.lua");
			
						parser.Tables.Clear();
						var dico = new Dictionary<string, string>();
						parser.Tables["jobtbl"] = dico;
						foreach (var npc in npcs.OrderBy(p => p.Key)) {
							dico[npc.Value.NpcName] = npc.Key.ToString(CultureInfo.InvariantCulture);
						}
						parser.Write(file, EncodingService.DisplayEncoding);

						IOHelper.SetData(ProjectConfiguration.SyncMobId, File.ReadAllBytes(file));
					}
			
					{
						BackupManager.Instance.BackupClient(ProjectConfiguration.SyncMobName);
						string file = TemporaryFilesManager.GetTemporaryFilePath("tmp2_{0:0000}.lua");
			
						parser.Tables.Clear();
						var dico = new Dictionary<string, string>();
						parser.Tables["JobNameTable"] = dico;
						foreach (var npc in npcs.OrderBy(p => p.Key)) {
							var ingameSprite = LatinUpper(npc.Value.IngameSprite ?? "");
			
							if (!string.IsNullOrEmpty(ingameSprite.GetExtension()))
								ingameSprite = ingameSprite.ReplaceExtension(ingameSprite.GetExtension().ToLower());
			
							if (string.IsNullOrEmpty(ingameSprite)) continue;
							dico["[jobtbl." + npc.Value.NpcName + "]"] = "\"" + ingameSprite + "\"";
						}
						parser.Write(file, EncodingService.DisplayEncoding);

						IOHelper.SetData(ProjectConfiguration.SyncMobName, File.ReadAllBytes(file));
					}
				}
			}
		}

		public static void DbLuaLoader(DbSaveContext context,
			string tableName, int tableId, Func<string> getPath,
			Func<string, int> getId, Func<string, string> getValue) {
			var metaGrf = SdeEditor.MetaGrf;

			try {
				var table = context.AbsractDb.Table;
				var data = metaGrf.GetData(getPath());

				if (data == null) return;
				LuaParser parser = new LuaParser(data, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(data), EncodingService.DisplayEncoding);

				var luaTable = parser.Tables[tableName];

				foreach (var pair in luaTable) {
					int id = getId(pair.Key);

					if (id.Equals(default)) continue;
					var tuple = table.TryGetTuple(id);
					if (tuple != null) {
						tuple.GetModel<Mob>().ClientSprite = getValue(pair.Value);
					}
				}
			}
			catch (Exception err) {
				context.ReportException(err);
			}
		}

		public static void DbAttachLuaLoaderUpper(DbSaveContext context, string tableName, string path) {
			try {
				var metaGrf = SdeEditor.MetaGrf;
				var db = context.AbsractDb;
				var data = metaGrf.GetData(path);

				if (data == null) {
					db.Attached[tableName] = null;
					db.Attached[tableName + "_T"] = null;
					return;
				}

				LuaParser parser = new LuaParser(data, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(data), EncodingService.DisplayEncoding);

				try {
					var luaTable = parser.Tables[tableName];
					Dictionary<string, string> dico = new Dictionary<string, string>();
					foreach (var pair in luaTable) {
						dico[pair.Key.Trim('[', ']', '\"').ToUpper()] = pair.Value;
					}
					parser.Tables[tableName] = dico;
					db.Attached[tableName] = parser;
					db.Attached[tableName + "_T"] = dico;
				}
				catch {
					db.Attached[tableName] = null;
				}
			}
			catch (Exception err) {
				context.ReportException(err);
			}
		}

		public static void DbAttachLuaLoader(DbSaveContext context, string tableName, string path) {
			try {
				var metaGrf = SdeEditor.MetaGrf;
				var db = context.AbsractDb;
				var data = metaGrf.GetData(path);

				if (data == null) {
					db.Attached[tableName] = null;
					db.Attached[tableName + "_T"] = null;
					return;
				}

				LuaParser parser = new LuaParser(data, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(data), EncodingService.DisplayEncoding);

				try {
					var luaTable = parser.Tables[tableName];
					Dictionary<string, string> dico = new Dictionary<string, string>();
					foreach (var pair in luaTable) {
						dico[pair.Key.Trim('[', ']', '\"')] = pair.Value;
					}
					parser.Tables[tableName] = dico;
					db.Attached[tableName] = parser;
					db.Attached[tableName + "_T"] = dico;
				}
				catch {
					db.Attached[tableName] = null;
				}
			}
			catch (Exception err) {
				context.ReportException(err);
			}
		}

		private static int _getNextViewId(ref int viewId, Dictionary<string, int> resourceToIds) {
			while (resourceToIds.Values.Contains(viewId)) {
				viewId++;
			}

			return viewId;
		}

		public static Dictionary<int, string> GetRedirectionTable() {
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

		public class PreviewBuffered {
			private DateTime _lastRequest;
			public Dictionary<int, string> Ids { get; private set; }
			public string Error { get; private set; }
			public bool Result { get; private set; }

			public PreviewBuffered() {
				Ids = new Dictionary<int, string>();
				_lastRequest = new DateTime(DateTime.Now.Ticks - 2000000000);
			}

			public bool IsBuffered() {
				if ((DateTime.Now - _lastRequest).Seconds < 3) {
					_lastRequest = DateTime.Now;
					return true;
				}

				return false;
			}

			public void Buffer(Dictionary<int, string> ids, bool result, string error) {
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

		public static bool GetIdToSpriteTable(ViewIdTypes type, out Dictionary<int, string> outputIdsToSprites, out string error) {
			outputIdsToSprites = new Dictionary<int, string>();
			error = null;
			var metaGrf = SdeEditor.MetaGrf;

			if (metaGrf.GetData(ProjectConfiguration.SyncAccId) == null || metaGrf.GetData(ProjectConfiguration.SyncAccName) == null) {
				error = "The accessory ID table or accessory name table has not been set, the paths are based on those.";
				return false;
			}

			int temp_i;
			string temp_s;
			var accIdPath = ProjectConfiguration.SyncAccId;
			Dictionary<string, int> ids;

			switch(type) {
				case ViewIdTypes.Weapon:
					if (_weaponBuffer.IsBuffered()) {
						outputIdsToSprites = _weaponBuffer.Ids;
						error = _weaponBuffer.Error;
						return _weaponBuffer.Result;
					}

					var weaponPath = GrfPath.Combine(GrfPath.GetDirectoryName(accIdPath), "weapontable" + Path.GetExtension(accIdPath));
					var weaponData = metaGrf.GetData(weaponPath);

					if (weaponData == null) {
						error = "Couldn't find " + weaponPath;
						_weaponBuffer.Buffer(outputIdsToSprites, false, error);
						return false;
					}

					var weaponTable = new Parser(Lub.AutoDecompile(weaponData)).Parse(EncodingService.DisplayEncoding);
					var weaponIds = GetLuaTable(weaponTable, "Weapon_IDs");
					var weaponNameTable = GetLuaTable(weaponTable, "WeaponNameTable");

					ids = SetIds(weaponIds, "Weapon_IDs");

					foreach (var pair in weaponNameTable) {
						temp_s = pair.Key.Trim('[', ']');

						if (ids.TryGetValue(temp_s, out temp_i) || int.TryParse(temp_s, out temp_i)) {
							outputIdsToSprites[temp_i] = pair.Value.Trim('\"');
						}
					}

					_weaponBuffer.Buffer(outputIdsToSprites, true, null);
					return true;
				case ViewIdTypes.Npc:
					if (_npcBuffer.IsBuffered()) {
						outputIdsToSprites = _npcBuffer.Ids;
						error = _npcBuffer.Error;
						return _npcBuffer.Result;
					}

					var npcPathSprites = GrfPath.Combine(GrfPath.GetDirectoryName(accIdPath), "jobname" + Path.GetExtension(accIdPath));
					var npcPathIds = GrfPath.Combine(GrfPath.GetDirectoryName(accIdPath), "npcIdentity" + Path.GetExtension(accIdPath));
					var npcDataSprites = metaGrf.GetData(npcPathSprites);
					var npcDataIds = metaGrf.GetData(npcPathIds);

					if (npcDataSprites == null) {
						error = "Couldn't find " + npcPathSprites;
						_npcBuffer.Buffer(outputIdsToSprites, false, error);
						return false;
					}

					if (npcDataIds == null) {
						error = "Couldn't find " + npcPathIds;
						_npcBuffer.Buffer(outputIdsToSprites, false, error);
						return false;
					}

					var jobname = new Parser(Lub.AutoDecompile(npcDataSprites)).Parse(EncodingService.DisplayEncoding);
					var jobtbl = new Parser(Lub.AutoDecompile(npcDataIds)).Parse(EncodingService.DisplayEncoding);

					var jobtblT = GetLuaTable(jobtbl, "jobtbl");
					var jobnameT = GetLuaTable(jobname, "JobNameTable");

					ids = SetIds(jobtblT, "jobtbl");

					foreach (var pair in jobnameT) {
						temp_s = pair.Key.Trim('[', ']');

						if (ids.TryGetValue(temp_s, out temp_i) || int.TryParse(temp_s, out temp_i)) {
							outputIdsToSprites[temp_i] = pair.Value.Trim('\"');
						}
					}

					_npcBuffer.Buffer(outputIdsToSprites, true, null);
					return true;
				case ViewIdTypes.Headgear:
					if (_headgearBuffer.IsBuffered()) {
						outputIdsToSprites = _headgearBuffer.Ids;
						error = _headgearBuffer.Error;
						return _headgearBuffer.Result;
					}

					var redirectionTable = GetRedirectionTable();
					var dataAccId = metaGrf.GetData(ProjectConfiguration.SyncAccId);
					var dataAccName = metaGrf.GetData(ProjectConfiguration.SyncAccName);
					var itemDb = SdeEditor.Project.GetMergedTable(DataSources.Item);
					var accId = new Parser(Lub.AutoDecompile(dataAccId)).Parse(EncodingService.DisplayEncoding);
					var accName = new Parser(Lub.AutoDecompile(dataAccName)).Parse(EncodingService.DisplayEncoding);
					var accIdT = GetLuaTable(accId, "ACCESSORY_IDs");
					var accNameT = GetLuaTable(accName, "AccNameTable");
					outputIdsToSprites = _getViewIdTable(accIdT, accNameT);

					accIdT.Clear();
					accNameT.Clear();

					var resourceToIds = new Dictionary<string, int>();

					if (ProjectConfiguration.HandleViewIds) {
						try {
							List<ReadableTuple> headgears = itemDb.FastItems.Where(p => p.GetModel<Item>().Type == ItemType.IT_ARMOR && (p.GetModel<Item>().Locations.ToLong() & 7937) != 0).OrderBy(p => p.GetModel<Item>().View.ToInt()).ToList();
							_loadFallbackValues(outputIdsToSprites, headgears, accIdT, accNameT, resourceToIds, redirectionTable);
						}
						catch (Exception err) {
							error = err.ToString();
							_headgearBuffer.Buffer(outputIdsToSprites, false, error);
							return false;
						}
					}

					_headgearBuffer.Buffer(outputIdsToSprites, true, null);
					return true;
				case ViewIdTypes.Shield:
					if (_shieldBuffer.IsBuffered()) {
						outputIdsToSprites = _shieldBuffer.Ids;
						error = _shieldBuffer.Error;
						return _shieldBuffer.Result;
					}

					var shieldPath = GrfPath.Combine(GrfPath.GetDirectoryName(accIdPath), "ShieldTable" + Path.GetExtension(accIdPath));
					var shieldData = metaGrf.GetData(shieldPath);

					if (shieldData == null) {
						outputIdsToSprites[1] = "_°¡µå";
						outputIdsToSprites[2] = "_¹öÅ¬·¯";
						outputIdsToSprites[3] = "_½¯µå";
						outputIdsToSprites[4] = "_¹Ì·¯½¯µå";
						outputIdsToSprites[5] = "";
						outputIdsToSprites[6] = "";
					}
					else {
						_debugStatus = "OK";

						var shieldTable = new Parser(Lub.AutoDecompile(shieldData)).Parse(EncodingService.DisplayEncoding);

						_debugStatus = "LoadTables";

						var shieldIds = GetLuaTable(shieldTable, "Shield_IDs");
						var shieldNameTable = GetLuaTable(shieldTable, "ShieldNameTable");
						var shieldMapTable = GetLuaTable(shieldTable, "ShieldMapTable");

						ids = SetIds(shieldIds, "Shield_IDs");
						Dictionary<int, string> idsToSprite = new Dictionary<int, string>();

						foreach (var pair in shieldNameTable) {
							temp_s = pair.Key.Trim('[', ']');

							if (ids.TryGetValue(temp_s, out temp_i) || int.TryParse(temp_s, out temp_i)) {
								temp_s = pair.Value.Trim('\"');
								idsToSprite[temp_i] = temp_s;
								outputIdsToSprites[temp_i] = temp_s;
							}
						}

						foreach (var pair in shieldMapTable) {
							var key = pair.Key.Trim('[', ']', '\t');
							int id1;

							if (ids.TryGetValue(key, out id1)) {
								int id2;
								temp_s = pair.Value.Trim('\"', '\t');

								if (ids.TryGetValue(temp_s, out id2) || int.TryParse(temp_s, out id2)) {
									if (idsToSprite.TryGetValue(id2, out temp_s)) {
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
					if (_garmentBuffer.IsBuffered()) {
						outputIdsToSprites = _garmentBuffer.Ids;
						error = _garmentBuffer.Error;
						return _garmentBuffer.Result;
					}

					var robeSpriteName = GrfPath.Combine(GrfPath.GetDirectoryName(accIdPath), "spriterobename" + Path.GetExtension(accIdPath));
					var robeSpriteId = GrfPath.Combine(GrfPath.GetDirectoryName(accIdPath), "spriterobeid" + Path.GetExtension(accIdPath));
					var robeNameData = metaGrf.GetData(robeSpriteName);
					var robeIdData = metaGrf.GetData(robeSpriteId);

					if (robeNameData == null) {
						error = "Couldn't find " + robeSpriteName;
						_garmentBuffer.Buffer(outputIdsToSprites, false, error);
						return false;
					}

					if (robeIdData == null) {
						error = "Couldn't find " + robeSpriteId;
						_garmentBuffer.Buffer(outputIdsToSprites, false, error);
						return false;
					}

					var robeNameTable = new Parser(Lub.AutoDecompile(robeNameData)).Parse(EncodingService.DisplayEncoding);
					var robeIdTable = new Parser(Lub.AutoDecompile(robeIdData)).Parse(EncodingService.DisplayEncoding);
					var robeNames = GetLuaTable(robeNameTable, "RobeNameTable");
					var robeIds = GetLuaTable(robeIdTable, "SPRITE_ROBE_IDs");

					ids = SetIds(robeIds, "SPRITE_ROBE_IDs");

					foreach (var pair in robeNames) {
						if (ids.TryGetValue(pair.Key, out temp_i) || int.TryParse(pair.Key, out temp_i)) {
							outputIdsToSprites[temp_i] = pair.Value;
						}
					}

					_garmentBuffer.Buffer(outputIdsToSprites, true, null);
					return true;
			}

			return false;
		}

		public static Dictionary<string, int> SetIds(Dictionary<string, string> inputTable, string tableId) {
			Dictionary<string, int> ids = new Dictionary<string, int>();
			string identifier = tableId + ".";
			foreach (var pair in inputTable) {
				ids[identifier + pair.Key] = int.Parse(pair.Value);
			}
			return ids;
		}

		public static string GetSpriteFromViewId(int viewIdToFind, ViewIdTypes type, ReadableTuple tuple) {
			string error;
			Dictionary<int, string> idsToSprite;

			if (GetIdToSpriteTable(type, out idsToSprite, out error)) {
				if (error == PreviewHelper.ViewIdIncrease && tuple != null) {
					var itemKey = tuple.GetKey<int>();

					if (itemKey >= 2101) {
						viewIdToFind = itemKey;
					}
				}

				if (viewIdToFind == 0 && tuple != null) {
					var itemKey = tuple.Key;

					if (itemKey == 2230 || itemKey == 2231 || itemKey == 5394) {
						return PreviewHelper.SpriteNone;
					}
				}

				if (idsToSprite.ContainsKey(viewIdToFind)) {
					return idsToSprite[viewIdToFind];
				}
			}
			else {
				throw new Exception(error);
			}

			return null;
		}

		public static string GetSpriteFromJob(MultiGrfReader grf, Job job, PreviewHelper helper, string sprite, ViewIdTypes type) {
			switch(type) {
				case ViewIdTypes.Garment:
					return GetSpritePathFromJob(job, @"data\sprite\·Îºê\" + sprite + @"\" + helper.GenderString + "\\{0}_" + helper.GenderString, helper.Gender, sprite);
				case ViewIdTypes.Shield:
					return GetSpritePathFromJob(job, @"data\sprite\¹æÆÐ\{0}\{0}_" + helper.GenderString + sprite, helper.Gender, sprite);
				case ViewIdTypes.Weapon:
					if (job.BaseJob == Job.Summoner)
						return GetSpritePathFromJob(job, @"data\sprite\µµ¶÷Á·\{0}\{0}_" + helper.GenderString + sprite, helper.Gender, sprite);

					return GetSpritePathFromJob(job, @"data\sprite\ÀÎ°£Á·\{0}\{0}_" + helper.GenderString + sprite, helper.Gender, sprite);
				case ViewIdTypes.Headgear:
					return EncodingService.FromAnyToDisplayEncoding(@"data\sprite\¾Ç¼¼»ç¸®\" + helper.GenderString + "\\" + EncodingService.FromAnyToDisplayEncoding(helper.GenderString + "_") + helper.PreviewSprite);
				case ViewIdTypes.Npc:
					if (helper.PreviewSprite != null && helper.PreviewSprite.EndsWith(".gr2", StringComparison.OrdinalIgnoreCase)) {
						return EncodingService.FromAnyToDisplayEncoding(@"data\model\3dmob\" + helper.PreviewSprite);
					}

					return EncodingService.FromAnyToDisplayEncoding(@"data\sprite\npc\" + helper.PreviewSprite);
			}

			return null;
		}

		public static string GetSpritePathFromJob(Job job, string spriteFormat, GenderType gender, string sprite) {
			if (sprite == PreviewHelper.SpriteNone)
				return PreviewHelper.SpriteNone;

			spriteFormat = EncodingService.FromAnyToDisplayEncoding(spriteFormat);

			string subPath = job.GetResource(gender);
			return string.Format(spriteFormat, subPath);
		}

		public static void WriteViewIds(DataSource source, BaseDatabase db) {
			if (ProjectConfiguration.SynchronizeWithClientDatabases && source == DataSources.Item &&
			    ProjectConfiguration.HandleViewIds) {
				//return;
				int debugInfo = 0;
				_debugStatus = "OK";
				var metaGrf = SdeEditor.MetaGrf;

				var dataAccId = metaGrf.GetData(ProjectConfiguration.SyncAccId);
				var dataAccName = metaGrf.GetData(ProjectConfiguration.SyncAccName);

				if (dataAccId != null && dataAccName != null) {
					var itemDb1 = SdeEditor.Project.GetTable(DataSources.Item);
					var itemDb2 = SdeEditor.Project.GetTable(DataSources.ItemImport);
					var citemDb = SdeEditor.Project.GetTable(DataSources.ClientItem);
					debugInfo++;

					try {
						itemDb1.Commands.Begin();
						itemDb2.Commands.Begin();
						citemDb.Commands.Begin();
						debugInfo++;

						AccessoryTable table = new AccessoryTable(db, dataAccId, dataAccName);
						table.SetLuaTables();
						table.SetTables();
						table.SetDbs();

						_debugStatus = "BackupManager";
						BackupManager.Instance.BackupClient(ProjectConfiguration.SyncAccId, metaGrf.GetData(ProjectConfiguration.SyncAccId));
						BackupManager.Instance.BackupClient(ProjectConfiguration.SyncAccName, metaGrf.GetData(ProjectConfiguration.SyncAccName));
						debugInfo++;

						_debugStatus = "WriteLuaFiles";
						_writeLuaFiles(table.LuaAccIdParser, table.LuaAccNameParser);
						debugInfo++;
					}
					catch (Exception err) {
						ErrorHandler.HandleException("Couldn't save the accessory item files. Error code = " + debugInfo + ", state = " + _debugStatus, err, ErrorLevel.Low);
						DbIOErrorHandler.Handle(err, "Generic exception while trying to save the client accessory items, context code = " + debugInfo, ErrorLevel.NotSpecified);
						DbIOErrorHandler.Focus();
					}
					finally {
						itemDb1.Commands.End();
						itemDb2.Commands.End();
						citemDb.Commands.End();
					}
				}
			}
		}

		internal static Dictionary<string, string> GetLuaTable(LList parser, string tId) {
			var table = parser.Variables.FirstOrDefault(p => p is LKeyValue kv && string.Compare(tId, kv.Key) == 0);

			if (table != null && ((LKeyValue)table).Value is LList lTable) {
				return lTable.CreateDirectDictionary();
			}

			_debugStatus += "#" + tId + " missing";
			throw new Exception("Invalid table file (lua/lub), missing '" + tId + "'. Tables found: " + Methods.Aggregate(parser.Variables.OfType<LKeyValue>().Select(p => p.Key).ToList(), ", "));
		}

		internal static Dictionary<string, string> GetLuaTable(LuaParser parser, string tId) {
			if (parser.Tables.Keys.Any(p => string.Compare(tId, p, StringComparison.OrdinalIgnoreCase) == 0)) {
				return parser.Tables.FirstOrDefault(p => string.Compare(tId, p.Key, StringComparison.OrdinalIgnoreCase) == 0).Value;
			}

			_debugStatus += "#" + tId + " missing";
			throw new Exception("Invalid table file (lua/lub), missing '" + tId + "'. Tables found: " + Methods.Aggregate(parser.Tables.Keys.ToList(), ", "));
		}

		private static void _writeLuaFiles(LuaParser accId, LuaParser accName) {
			var metaGrf = SdeEditor.MetaGrf;

			string file = TemporaryFilesManager.GetTemporaryFilePath("tmp2_{0:0000}.lua");
			accId.Write(file, EncodingService.DisplayEncoding);
			IOHelper.SetData(ProjectConfiguration.SyncAccId, File.ReadAllBytes(file));

			file = TemporaryFilesManager.GetTemporaryFilePath("tmp2_{0:0000}.lua");
			accName.Write(file, EncodingService.DisplayEncoding);
			IOHelper.SetData(ProjectConfiguration.SyncAccName, File.ReadAllBytes(file));
		}

		private static void _loadFallbackValues(Dictionary<int, string> fallbackSprites, List<ReadableTuple> headgears, IDictionary<string, string> accIdT, IDictionary<string, string> accNameT, IDictionary<string, int> resourceToIds, Dictionary<int, string> redirectionTable) {
			TkDictionary<int, ReadableTuple> buffered = new TkDictionary<int, ReadableTuple>();
			var rRedirectionTable = new HashSet<string>();

			foreach (var headgear in headgears) {
				if (!buffered.ContainsKey(headgear.Key)) {
					buffered[headgear.Key] = headgear;
				}
			}

			foreach (var pair in redirectionTable) {
				rRedirectionTable.Add(pair.Value);
			}

			foreach (var keyPair in fallbackSprites) {
				if (rRedirectionTable.Contains(keyPair.Value)) continue;
				if (keyPair.Key <= 0) continue; // throw new Exception("View ID cannot be equal or below 0.");

				var sTuple = buffered[keyPair.Key]; // headgears.FirstOrDefault(p => p.GetIntNoThrow(ServerItemAttributes.ClassNumber) == keyPair.Key);
				string accessoryName;

				if (sTuple != null)
					accessoryName = GetAccAegisNameFromTuple(sTuple);
				else
					// No item associated with this view ID
					accessoryName = string.Format("UNREGISTERED_{0:0000}", keyPair.Key);

				// Bogus entry - entry by number
				if (keyPair.Key.ToString(CultureInfo.InvariantCulture) == keyPair.Value) continue;

				accIdT["ACCESSORY_" + accessoryName] = keyPair.Key.ToString(CultureInfo.InvariantCulture);
				accNameT["[ACCESSORY_IDs.ACCESSORY_" + accessoryName + "]"] = "\"_" + keyPair.Value + "\"";
				resourceToIds[keyPair.Value] = keyPair.Key;
			}
		}

		public static string LatinOnly(string value) {
			value = value ?? "";
			StringBuilder builder = new StringBuilder();
			char c;

			for (int i = 0; i < value.Length; i++) {
				c = value[i];

				if (_latinCheck[c]) {
					builder.Append(value[i]);
				}
				else {
					builder.Append('_');
				}
			}

			return builder.ToString();
		}

		public static bool IsLatinOnly(string value) {
			return value.All(c => _latinCheck[c]);
		}

		public static string LatinUpper(string value) {
			StringBuilder builder = new StringBuilder();
			char c;

			for (int i = 0; i < value.Length; i++) {
				c = value[i];

				if (_latinCheck[c]) {
					builder.Append(char.ToUpperInvariant(value[i]));
				}
				else {
					builder.Append(c);
				}
			}

			return builder.ToString();
		}

		public static string GetAccAegisNameFromTuple(ReadableTuple tuple) {
			string accessoryName = tuple.GetModel<Item>().AegisName ?? "";
			return LatinOnly(accessoryName);
		}

		private static Dictionary<int, string> _getViewIdTable(Dictionary<string, string> accIdT, Dictionary<string, string> accNameT) {
			Dictionary<int, string> viewId = new Dictionary<int, string>();

			foreach (var pair in accIdT) {
				var key = "[ACCESSORY_IDs." + pair.Key + "]";

				if (accNameT.ContainsKey(key)) {
					int ival;

					if (int.TryParse(pair.Value, out ival)) {
						var sprite = accNameT[key].Trim('\"');

						if (sprite.Length > 1)
							sprite = sprite.Substring(1);

						if (ival.ToString(CultureInfo.InvariantCulture) == sprite) {
							continue;
						}

						viewId[ival] = sprite;
					}
				}
			}

			foreach (var pair in accNameT) {
				var key = pair.Key.Trim('[', ']');

				int ival;

				if (int.TryParse(key, out ival)) {
					var sprite = pair.Value.Trim('\"');

					if (sprite.Length > 1)
						sprite = sprite.Substring(1);

					if (ival.ToString(CultureInfo.InvariantCulture) == sprite) {
						continue;
					}

					viewId[ival] = sprite;
				}
			}

			return viewId;
		}
	}
}