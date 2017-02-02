using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Database;
using GRF.FileFormats.LubFormat;
using Lua;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace SDE.Editor.Engines.LuaEngine {
	public class AccessoryItem {
		public int ViewId;
		public string AccessoryName;
		public string Sprite;
	}

	public class AccessoryTable {
		private readonly Dictionary<int, string> _luaViewIdToAccessory = new Dictionary<int, string>();
		private readonly Dictionary<string, string> _luaAccessoryToSprite = new Dictionary<string, string>();
		private readonly Dictionary<string, int> _luaSpriteToViewId = new Dictionary<string, int>();

		private readonly Dictionary<int, string> _viewIdToSpriteFallback = new Dictionary<int, string>();
		private readonly MetaTable<int> _itemDb;
		private readonly Table<int, ReadableTuple<int>> _cItemDb;

		private readonly List<ReadableTuple<int>> _headgears;

		// Override
		public Dictionary<int, string> ItemIdToSprite = new Dictionary<int, string>();
		private int _backupViewId = 1;

		public Dictionary<string, string> LuaAccId { get; set; }
		public Dictionary<string, string> LuaAccName { get; set; }

		public LuaParser LuaAccIdParser { get; set; }
		public LuaParser LuaAccNameParser { get; set; }

		public AccessoryTable(AbstractDb<int> db, byte[] dataAccId, byte[] dataAccName) {
			// Load current tables as fallback values
			ItemIdToSprite = LuaHelper.GetRedirectionTable();
			_itemDb = db.GetMeta<int>(ServerDbs.Items);
			_cItemDb = db.Get<int>(ServerDbs.CItems);

			LuaAccIdParser = new LuaParser(dataAccId, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(dataAccId), EncodingService.DisplayEncoding);
			LuaAccNameParser = new LuaParser(dataAccName, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(dataAccName), EncodingService.DisplayEncoding);

			LuaAccId = LuaHelper.GetLuaTable(LuaAccIdParser, "ACCESSORY_IDs");
			LuaAccName = LuaHelper.GetLuaTable(LuaAccNameParser, "AccNameTable");

			_viewIdToSpriteFallback = GetViewIdTableFallback(LuaAccId, LuaAccName);
			_headgears = _itemDb.FastItems.Where(p => ItemParser.IsArmorType(p) && (p.GetIntNoThrow(ServerItemAttributes.Location) & 7937) != 0).OrderBy(p => p.GetIntNoThrow(ServerItemAttributes.ClassNumber)).ToList();

			LuaAccId.Clear();
			LuaAccName.Clear();
		}

		private AccessoryItem _getEntry(int viewId) {
			if (!_luaViewIdToAccessory.ContainsKey(viewId))
				return null;

			var accName = _luaViewIdToAccessory[viewId];
			return new AccessoryItem { ViewId = viewId, AccessoryName = accName, Sprite = _luaAccessoryToSprite[accName] };
		}

		private AccessoryItem _getEntryFromSprite(string sprite) {
			if (!_luaSpriteToViewId.ContainsKey(sprite))
				return null;

			return _getEntry(_luaSpriteToViewId[sprite]);
		}

		private void _delEntry(AccessoryItem accItem) {
			_luaAccessoryToSprite.Remove(accItem.AccessoryName);
			_luaSpriteToViewId.Remove(accItem.Sprite);
			_luaViewIdToAccessory.Remove(accItem.ViewId);
		}

		private void _setEntry(int viewId, string accessoryName, string resource) {
			if (viewId <= 0) throw new Exception("View ID cannot be equal or below 0.");

			resource = resource.ToDisplayEncoding();

			if (_luaViewIdToAccessory.ContainsKey(viewId)) {
				throw new Exception("Warning: trying to overwrite an already set View ID.");
			}

			_luaViewIdToAccessory[viewId] = accessoryName;

			if (_luaAccessoryToSprite.ContainsKey(accessoryName)) {
				throw new Exception("Warning: trying to overwrite an already set accessory name.");
			}

			_luaAccessoryToSprite[accessoryName] = resource;

			if (_luaSpriteToViewId.ContainsKey(resource)) {
				throw new Exception("Warning: trying to overwrite an already set resource.");
			}

			_luaSpriteToViewId[resource] = viewId;
		}

		public void SetLuaTables() {
			// Give priority to fallback values - current lua entries
			// Does NOT need to write all values, only those which are known to be correct.
			_setLuaFromFallback();

			// Set redirection entries
			_setLuaFromOverwrite();

			// All view IDs must be set
			_setLuaFromHeadgears();
		}

		public void SetTables() {
			foreach (var keyPair in _luaViewIdToAccessory.OrderBy(p => p.Key)) {
				var viewId = keyPair.Key;
				var accessoryName = keyPair.Value;
				var sprite = _luaAccessoryToSprite[accessoryName];

				LuaAccId["ACCESSORY_" + accessoryName] = viewId.ToString(CultureInfo.InvariantCulture);
				LuaAccName["[ACCESSORY_IDs.ACCESSORY_" + accessoryName + "]"] = "\"_" + sprite + "\"";
			}
		}

		public void SetDbs() {
			foreach (var tuple in _headgears) {
				var key = tuple.Key;
				var cTuple = _cItemDb.TryGetTuple(key);
				var sViewId = tuple.GetIntNoThrow(ServerItemAttributes.ClassNumber);

				// Special cases
				// These have already been taken care of.
				if (ItemIdToSprite.ContainsKey(key)) {
					continue;
				}

				if (cTuple != null) {
					var cViewId = cTuple.GetIntNoThrow(ClientItemAttributes.ClassNumber);
					var sprite = cTuple.GetValue<string>(ClientItemAttributes.IdentifiedResourceName).ToDisplayEncoding();
					var entry = _getEntryFromSprite(sprite);

					if (sprite == "")
						continue;

					// entry can never be null
					if (entry == null) {
						throw new Exception("warning: GetEntryFromSprite can never return null in this scenario.");
					}

					var nViewId = entry.ViewId;

					if (sViewId != nViewId) _itemDb.Commands.Set(tuple, ServerItemAttributes.ClassNumber, nViewId.ToString(CultureInfo.InvariantCulture));
					if (cViewId != nViewId) _cItemDb.Commands.Set(cTuple, ClientItemAttributes.ClassNumber, nViewId.ToString(CultureInfo.InvariantCulture));
				}
			}
		}

		private void _setLuaFromHeadgears() {
			foreach (var tuple in _headgears) {
				var key = tuple.Key;
				var cTuple = _cItemDb.TryGetTuple(key);
				string accessoryName = LuaHelper.GetAccAegisNameFromTuple(tuple);

				if (cTuple != null) {
					var sprite = cTuple.GetStringValue(ClientItemAttributes.IdentifiedResourceName.Index);
					var entry = _getEntryFromSprite(sprite);

					// Empty sprites are no longer allowed
					if (sprite == "")
						continue;

					// No View IDs are associated with this sprite, create a new entry
					if (entry == null) {
						int viewId = _getNextViewId();
						_setEntry(viewId, accessoryName, sprite);
					}
					else {
						// Prioritize entries without the unregistered keyword
						_delEntry(entry);
						_setEntry(entry.ViewId, accessoryName, sprite);
					}
				}
				else {
					// The item doesn't exist in the client_db
					// It cannot retrieve the associated sprite...
					var tViewId = tuple.GetIntNoThrow(ServerItemAttributes.ClassNumber);

					// View IDs 0 are fully ignored, and we don't care because
					// these are not setup in the client-side
					if (tViewId <= 0) {
						continue;
					}

					var entry = _getEntry(tViewId);

					// The entry hasn't been set from the previous lua files, so
					// we create a dummy one instead.
					if (entry == null) {
						accessoryName = String.Format("UNREGISTERED_{0:0000}", tViewId);
						_setEntry(tViewId, accessoryName, tViewId.ToString(CultureInfo.InvariantCulture));
					}
				}
			}
		}

		private void _setLuaFromOverwrite() {
			foreach (var keyPair in ItemIdToSprite) {
				var sprite = keyPair.Value;
				var sTuple = _itemDb.TryGetTuple(keyPair.Key);
				var cTuple = _cItemDb.TryGetTuple(keyPair.Key);

				if (sprite != null) {
					int viewId;
					var entry = _getEntryFromSprite(sprite);

					// The entry has not been set
					if (entry == null) {
						viewId = _getNextViewId();

						string accessoryName = String.Format("SPECIAL_{0:0000}", viewId);
						_setEntry(viewId, accessoryName, sprite);
					}
					else {
						// If it goes here, it means the ViewID is already in use by another sprite
						viewId = entry.ViewId;
					}

					if (sTuple != null && sTuple.GetIntNoThrow(ServerItemAttributes.ClassNumber) != viewId) _itemDb.Commands.Set(sTuple, ServerItemAttributes.ClassNumber, viewId.ToString(CultureInfo.InvariantCulture));
					if (cTuple != null && cTuple.GetIntNoThrow(ClientItemAttributes.ClassNumber) != viewId) _cItemDb.Commands.Set(cTuple, ClientItemAttributes.ClassNumber, viewId.ToString(CultureInfo.InvariantCulture));
				}
				else {
					// Invisible items
					if (sTuple != null && sTuple.GetIntNoThrow(ServerItemAttributes.ClassNumber) != 0) _itemDb.Commands.Set(sTuple, ServerItemAttributes.ClassNumber, "0");
					if (cTuple != null && cTuple.GetIntNoThrow(ClientItemAttributes.ClassNumber) != 0) _cItemDb.Commands.Set(cTuple, ClientItemAttributes.ClassNumber, "0");
				}
			}
		}

		private int _getNextViewId() {
			while (_luaViewIdToAccessory.ContainsKey(_backupViewId)) {
				_backupViewId++;
			}

			return _backupViewId;
		}

		private void _setLuaFromFallback() {
			TkDictionary<int, ReadableTuple<int>> buffered = new TkDictionary<int, ReadableTuple<int>>();
			var overrideTable = new HashSet<string>();

			foreach (var headgear in _headgears) {
				if (!buffered.ContainsKey(headgear.Key)) {
					buffered[headgear.Key] = headgear;
				}
			}

			foreach (var pair in ItemIdToSprite) {
				overrideTable.Add(pair.Value);
			}

			foreach (var keyPair in _viewIdToSpriteFallback) {
				if (overrideTable.Contains(keyPair.Value)) continue;
				if (keyPair.Key <= 0) continue;

				var sTuple = buffered[keyPair.Key];
				string accessoryName;

				if (sTuple != null)
					accessoryName = LuaHelper.GetAccAegisNameFromTuple(sTuple);
				else
					// No item associated with this view ID - these are NOT in the item_db tables
					accessoryName = String.Format("UNREGISTERED_{0:0000}", keyPair.Key);

				// Bogus entry - entry by number
				if (keyPair.Key.ToString(CultureInfo.InvariantCulture) == keyPair.Value) {
					// Should NEVER happen
					// Bogus entries are removed when the fallback values are read...
					continue;
				}

				// Do not allow multiple ViewIDs to point towards the same sprite
				if (_luaSpriteToViewId.ContainsKey(keyPair.Value)) {
					continue;
				}

				// Do not allow empty named sprites
				if (keyPair.Value == "")
					continue;

				_setEntry(keyPair.Key, accessoryName, keyPair.Value);
			}
		}

		public string ExtractSpriteName(string sprite) {
			sprite = sprite.Trim('\"');

			if (sprite.Length > 1)
				sprite = sprite.Substring(1);

			return sprite;
		}

		public Dictionary<int, string> GetViewIdTableFallback(Dictionary<string, string> accIdT, Dictionary<string, string> accNameT) {
			Dictionary<int, string> viewId = new Dictionary<int, string>();

			foreach (var pair in accIdT) {
				var key = "[ACCESSORY_IDs." + pair.Key + "]";

				if (accNameT.ContainsKey(key)) {
					int ival;

					if (Int32.TryParse(pair.Value, out ival)) {
						var sprite = ExtractSpriteName(accNameT[key]);

						// Fix : 2015-08-19
						// Invalid data detected, caused by SDE.
						// Do not allow this value to be repeated again as a fallback entry.
						if (ival.ToString(CultureInfo.InvariantCulture) == sprite) {
							continue;
						}

						// Fix : 2015-08-19
						// View IDs below or equal to 0 are no longer allowed.
						if (ival <= 0) {
							continue;
						}

						viewId[ival] = sprite;
					}
				}
			}

			foreach (var pair in accNameT) {
				var key = pair.Key.Trim('[', ']');
				int ival;

				if (Int32.TryParse(key, out ival)) {
					var sprite = ExtractSpriteName(pair.Value);

					// Fix : 2015-08-19
					// Invalid data detected, caused by SDE.
					// Do not allow this value to be repeated again as a fallback entry.
					if (ival.ToString(CultureInfo.InvariantCulture) == sprite) {
						continue;
					}

					// Fix : 2015-08-19
					// View IDs below or equal to 0 are no longer allowed.
					if (ival <= 0) {
						continue;
					}

					viewId[ival] = sprite;
				}
			}

			return viewId;
		}
	}
}