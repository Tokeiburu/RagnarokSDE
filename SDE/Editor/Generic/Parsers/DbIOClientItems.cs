using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Database;
using ErrorManager;
using GRF.FileFormats.LubFormat;
using GRF.IO;
using Lua;
using Lua.Structure;
using SDE.ApplicationConfiguration;
using SDE.Core;
using SDE.Editor.Engines.BackupsEngine;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace SDE.Editor.Generic.Parsers {
	public sealed class DbIOClientItems {
		private delegate bool RequiredCondition<T>(T item) where T : Tuple;

		public static void Loader(DbDebugItem<int> debug, AbstractDb<int> db) {
			if (ProjectConfiguration.UseLuaFiles) {
				LoadEntry(db, ProjectConfiguration.ClientItemInfo);
				_loadCardIllustrationNames(db, db.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.ClientCardIllustration));
				_loadCardPrefixes(db, db.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.ClientCardPrefixes));
				_loadCardPostfixes(db, db.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.ClientCardPostfixes));
			}
			else {
				_loadCardIllustrationNames(db, db.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.ClientCardIllustration));
				_loadCardPrefixes(db, db.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.ClientCardPrefixes));
				_loadCardPostfixes(db, db.ProjectDatabase.MetaGrf.GetData(ProjectConfiguration.ClientCardPostfixes));

				_loadData(db, ProjectConfiguration.ClientItemSlotCount, ClientItemAttributes.NumberOfSlots);
				_loadData(db, ProjectConfiguration.ClientItemIdentifiedName, ClientItemAttributes.IdentifiedDisplayName, false, true);
				_loadData(db, ProjectConfiguration.ClientItemUnidentifiedName, ClientItemAttributes.UnidentifiedDisplayName, false, true);
				_loadData(db, ProjectConfiguration.ClientItemIdentifiedResourceName, ClientItemAttributes.IdentifiedResourceName, false);
				_loadData(db, ProjectConfiguration.ClientItemUnidentifiedResourceName, ClientItemAttributes.UnidentifiedResourceName, false);
				_loadData(db, ProjectConfiguration.ClientItemIdentifiedDescription, ClientItemAttributes.IdentifiedDescription);
				_loadData(db, ProjectConfiguration.ClientItemUnidentifiedDescription, ClientItemAttributes.UnidentifiedDescription);
				_loadViewId(db);
			}
		}

		private static void _loadViewId(AbstractDb<int> db) {
			var sItems = db.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

			foreach (var tuple in db.Table.FastItems) {
				var sTuple = sItems.TryGetTuple(tuple.GetKey<int>());

				if (sTuple != null) {
					tuple.SetRawValue(ClientItemAttributes.ClassNumber, sTuple.GetValue(ServerItemAttributes.ClassNumber));
				}
			}
		}

		public static void Writer(DbDebugItem<int> debug, AbstractDb<int> db) {
			WriterSub(debug, db, null, null);
		}

		public static void WriterSub(DbDebugItem<int> debug, AbstractDb<int> db, string path, FileType? typeOverride) {
			db.ProjectDatabase.MetaGrf.Clear();

			if (typeOverride == null) {
				if (db.Table.Commands.CommandIndex == -1 &&
				    !db.IsModified) return;
				//if (!db.IsModified) return;

				if (ProjectConfiguration.UseLuaFiles) {
					SaveAsLua(db.ProjectDatabase, path);
				}
				else {
					SaveAsTxt(db.ProjectDatabase, path);
				}
			}
			else {
				if (typeOverride == FileType.Lua) {
					SaveAsLua(db.ProjectDatabase, path);
				}
				else {
					SaveAsTxt(db.ProjectDatabase, path);
				}
			}

			try {
				if (path == null) {
					db.ProjectDatabase.MetaGrf.SaveAndReload();
				}
			}
			catch (OperationCanceledException) {
				ErrorHandler.HandleException("Failed to save the client files.");
			}
		}

		public static void SaveAsTxt(SdeDatabase gdb, string path) {
			try {
				_saveFile(gdb, ProjectConfiguration.ClientCardIllustration, path, ClientItemAttributes.Illustration, item => item.GetValue<bool>(ClientItemAttributes.IsCard));
				_saveFile(gdb, ProjectConfiguration.ClientCardPrefixes, path, ClientItemAttributes.Affix, item => item.GetValue<bool>(ClientItemAttributes.IsCard));
				_saveFile(gdb, ProjectConfiguration.ClientCardPostfixes, path, null, item => item.GetValue<bool>(ClientItemAttributes.Postfix), false);
				_saveFile(gdb, ProjectConfiguration.ClientItemSlotCount, path, ClientItemAttributes.NumberOfSlots, item => (item.GetValue<string>(ClientItemAttributes.NumberOfSlots)).Length > 0, false);
				_saveFile(gdb, ProjectConfiguration.ClientItemIdentifiedResourceName, path, ClientItemAttributes.IdentifiedResourceName, item => (item.GetValue<string>(ClientItemAttributes.IdentifiedResourceName)).Length > 0, true);
				_saveFile(gdb, ProjectConfiguration.ClientItemUnidentifiedResourceName, path, ClientItemAttributes.UnidentifiedResourceName, item => (item.GetValue<string>(ClientItemAttributes.UnidentifiedResourceName)).Length > 0, true);
				_saveFile(gdb, ProjectConfiguration.ClientItemIdentifiedDescription, path, ClientItemAttributes.IdentifiedDescription, item => (item.GetValue<string>(ClientItemAttributes.IdentifiedDescription)).Length > 0, false);
				_saveFile(gdb, ProjectConfiguration.ClientItemUnidentifiedDescription, path, ClientItemAttributes.UnidentifiedDescription, item => (item.GetValue<string>(ClientItemAttributes.UnidentifiedDescription)).Length > 0, false);
				_saveFile(gdb, ProjectConfiguration.ClientItemIdentifiedName, path, ClientItemAttributes.IdentifiedDisplayName, item => (item.GetValue<string>(ClientItemAttributes.IdentifiedDisplayName)).Length > 0, true);
				_saveFile(gdb, ProjectConfiguration.ClientItemUnidentifiedName, path, ClientItemAttributes.UnidentifiedDisplayName, item => (item.GetValue<string>(ClientItemAttributes.UnidentifiedDisplayName)).Length > 0, true);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public static void SaveAsLua(SdeDatabase gdb, string path) {
			try {
				_saveFile(gdb, ProjectConfiguration.ClientCardIllustration, path, ClientItemAttributes.Illustration, item => item.GetValue<bool>(ClientItemAttributes.IsCard));
				_saveFile(gdb, ProjectConfiguration.ClientCardPrefixes, path, ClientItemAttributes.Affix, item => item.GetValue<bool>(ClientItemAttributes.IsCard));
				_saveFile(gdb, ProjectConfiguration.ClientCardPostfixes, path, null, item => item.GetValue<bool>(ClientItemAttributes.Postfix), false);
				_saveItemDataToLua(gdb, ProjectConfiguration.ClientItemInfo, path);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public static void WriteEntry(StringBuilder builder, ReadableTuple<int> tuple, bool end = false) {
			builder.Append("\t[");
			builder.Append(tuple.GetValue<int>(0));
			builder.AppendLine("] = {");

			if (SdeAppConfiguration.DbWriterItemInfoUnDisplayName) {
				builder.Append("\t\tunidentifiedDisplayName = \"");
				builder.Append(_toAnsiEscaped(((string)tuple.GetRawValue(ClientItemAttributes.UnidentifiedDisplayName.Index)) ?? ""));
				builder.AppendLine("\",");
			}

			if (SdeAppConfiguration.DbWriterItemInfoUnResource) {
				builder.Append("\t\tunidentifiedResourceName = \"");
				builder.Append(_toAnsiEscaped(((string)tuple.GetRawValue(ClientItemAttributes.UnidentifiedResourceName.Index)) ?? ""));
				builder.AppendLine("\",");
			}

			if (SdeAppConfiguration.DbWriterItemInfoUnDescription) {
				builder.AppendLine("\t\tunidentifiedDescriptionName = {");
				_toLuaDescription(builder, _toAnsiEscaped((string)tuple.GetRawValue(ClientItemAttributes.UnidentifiedDescription.Index) ?? ""));
				builder.AppendLine("\t\t},");
			}

			if (SdeAppConfiguration.DbWriterItemInfoIdDisplayName) {
				builder.Append("\t\tidentifiedDisplayName = \"");
				builder.Append(_toAnsiEscaped(((string)tuple.GetRawValue(ClientItemAttributes.IdentifiedDisplayName.Index)) ?? ""));
				builder.AppendLine("\",");
			}

			if (SdeAppConfiguration.DbWriterItemInfoIdResource) {
				builder.Append("\t\tidentifiedResourceName = \"");
				builder.Append(_toAnsiEscaped(((string)tuple.GetRawValue(ClientItemAttributes.IdentifiedResourceName.Index)) ?? ""));
				builder.AppendLine("\",");
			}

			if (SdeAppConfiguration.DbWriterItemInfoIdDescription) {
				builder.AppendLine("\t\tidentifiedDescriptionName = {");
				_toLuaDescription(builder, _toAnsiEscaped((string)tuple.GetRawValue(ClientItemAttributes.IdentifiedDescription.Index) ?? ""));
				builder.AppendLine("\t\t},");
			}

			if (SdeAppConfiguration.DbWriterItemInfoSlotCount) {
				builder.Append("\t\tslotCount = ");
				builder.Append(_toAnsiEscaped((String.IsNullOrEmpty(tuple.GetValue<string>(ClientItemAttributes.NumberOfSlots.Index)) ? "0" : tuple.GetValue<string>(ClientItemAttributes.NumberOfSlots.Index))));
				builder.AppendLine(",");
			}

			if (SdeAppConfiguration.DbWriterItemInfoClassNum) {
				builder.Append("\t\tClassNum = ");
				builder.AppendLine(_toAnsiEscaped((String.IsNullOrEmpty(tuple.GetValue<string>(ClientItemAttributes.ClassNumber.Index)) ? "0" : tuple.GetValue<string>(ClientItemAttributes.ClassNumber.Index))));
			}

			builder.AppendLine(end ? "\t}" : "\t},");
		}

		private static string _toAnsiEscaped(string value) {
			return EncodingService.GetAnsiString(value).Escape(EscapeMode.KeepAsciiCode);
		}

		private static void _saveFile(SdeDatabase gdb, string filename, string output, DbAttribute attribute, RequiredCondition<ReadableTuple<int>> condition = null, bool allowReturns = true) {
			if (output == null && gdb.MetaGrf.GetData(filename) == null) {
				Debug.Ignore(() => DbDebugHelper.OnWriteStatusUpdate(ServerDbs.CItems, filename, null, "Table not saved (" + attribute.GetQueryName() + ")."));
				return;
			}

			if (output == null)
				BackupEngine.Instance.BackupClient(filename, gdb.MetaGrf);

			string tmpFilename = Path.Combine(SdeAppConfiguration.TempPath, Path.GetFileName(filename));
			Encoding encoder = EncodingService.DisplayEncoding;
			byte[] tmpBuffer;
			byte[] lineFeedByte = encoder.GetBytes(SdeStrings.LineFeed);
			byte[] doubleLineFeedByte = encoder.GetBytes(SdeStrings.LineFeed + SdeStrings.LineFeed);

			using (MemoryStream memStream = new MemoryStream()) {
				IEnumerable<ReadableTuple<int>> items = gdb.GetDb<int>(ServerDbs.CItems).Table.GetSortedItems();

				int previousId = -1;
				bool firstItem = true;

				foreach (ReadableTuple<int> item in items) {
					if (condition == null || condition(item)) {
						string itemProperty = attribute != null ? item.GetRawValue(attribute.Index) as string : null;
						if (itemProperty != null || attribute == null) {
							if (attribute == ClientItemAttributes.IdentifiedDisplayName || attribute == ClientItemAttributes.UnidentifiedDisplayName) {
								itemProperty = itemProperty.Replace(" ", "_");
							}

							if (!firstItem) {
								if (allowReturns) {
									if (previousId == (item.GetValue<int>(ClientItemAttributes.Id) - 1)) {
										memStream.Write(lineFeedByte, 0, lineFeedByte.Length);
									}
									else {
										memStream.Write(doubleLineFeedByte, 0, doubleLineFeedByte.Length);
									}
								}
								else
									memStream.Write(lineFeedByte, 0, lineFeedByte.Length);
							}

							if (attribute == null) {
								tmpBuffer = encoder.GetBytes(item.GetValue<int>(ClientItemAttributes.Id) + "#");
							}
							else {
								tmpBuffer = encoder.GetBytes(item.GetValue<int>(ClientItemAttributes.Id) + "#" + itemProperty + "#");
							}

							memStream.Write(tmpBuffer, 0, tmpBuffer.Length);

							previousId = item.GetValue<int>(ClientItemAttributes.Id);
							firstItem = false;
						}
					}
				}

				memStream.Write(lineFeedByte, 0, lineFeedByte.Length);

				tmpBuffer = new byte[memStream.Length];
				Buffer.BlockCopy(memStream.GetBuffer(), 0, tmpBuffer, 0, tmpBuffer.Length);

				File.WriteAllBytes(tmpFilename, tmpBuffer);
			}

			if (output == null) {
				var data = gdb.MetaGrf.GetData(filename);
				var toWrite = File.ReadAllBytes(tmpFilename);

				if (data != null && Methods.ByteArrayCompare(data, toWrite)) return;

				gdb.MetaGrf.SetData(filename, toWrite);
			}
			else {
				string copyPath = Path.Combine(output, Path.GetFileName(filename));

				try {
					File.Delete(copyPath);
					File.Copy(tmpFilename, copyPath);
					File.Delete(tmpFilename);
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}

			Debug.Ignore(() => DbDebugHelper.OnWriteStatusUpdate(ServerDbs.CItems, gdb.MetaGrf.FindTkPath(filename), null, "Saving client table (" + (attribute == null ? "" : attribute.GetQueryName()) + ")."));
		}

		private static void _saveItemDataToLua(SdeDatabase gdb, string filename, string output) {
			if (output == null && gdb.MetaGrf.GetData(filename) == null) {
				Debug.Ignore(() => DbDebugHelper.OnWriteStatusUpdate(ServerDbs.CItems, filename, null, "ItemInfo table not saved."));
				return;
			}

			if (output == null)
				BackupEngine.Instance.BackupClient(filename, gdb.MetaGrf);

			StringBuilder builder = new StringBuilder();
			builder.AppendLine("tbl = {");

			List<ReadableTuple<int>> tuples = gdb.GetDb<int>(ServerDbs.CItems).Table.GetSortedItems().ToList();
			ReadableTuple<int> tuple;

			for (int index = 0, count = tuples.Count; index < count; index++) {
				tuple = tuples[index];
				WriteEntry(builder, tuple, index == count - 1);
			}

			builder.AppendLine("}");
			builder.AppendLine();
			builder.AppendLine(ResourceString.Get("ItemInfoFunction"));

			if (output == null) {
				gdb.MetaGrf.SetData(filename, EncodingService.Ansi.GetBytes(builder.ToString()));
			}
			else {
				string copyPath = Path.Combine(output, Path.GetFileName(filename));

				try {
					File.WriteAllText(copyPath, builder.ToString(), EncodingService.Ansi);
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}

			Debug.Ignore(() => DbDebugHelper.OnWriteStatusUpdate(ServerDbs.CItems, gdb.MetaGrf.FindTkPath(filename), null, "Saving ItemInfo table."));
		}

		internal static void LoadData<T>(AbstractDb<T> db, string file, DbAttribute attribute, bool allowCutLine = true) {
			if (file.IndexOf("2itemdisplaynametable", 0, StringComparison.OrdinalIgnoreCase) > -1)
				_loadData(db, file, attribute, allowCutLine, true);
			else if (file.IndexOf("cardpostfixnametable", 0, StringComparison.OrdinalIgnoreCase) > -1)
				_loadCardPostfixes((AbstractDb<int>)(object)db, db.ProjectDatabase.MetaGrf.GetData(file));
			else if (file.IndexOf("cardprefixnametable", 0, StringComparison.OrdinalIgnoreCase) > -1)
				_loadCardPrefixes((AbstractDb<int>)(object)db, db.ProjectDatabase.MetaGrf.GetData(file));
			else if (file.IndexOf("num2cardillustnametable", 0, StringComparison.OrdinalIgnoreCase) > -1)
				_loadCardIllustrationNames((AbstractDb<int>)(object)db, db.ProjectDatabase.MetaGrf.GetData(file));
			else
				_loadData(db, file, attribute, allowCutLine);
		}

		private static void _loadData<T>(AbstractDb<T> db, string file, DbAttribute attribute, bool allowCutLine = true, bool removeUnderscore = false) {
			var table = db.Table;
			TextFileHelper.LatestFile = file;

			try {
				foreach (string[] elements in TextFileHelper.GetElements(db.ProjectDatabase.MetaGrf.GetData(file), allowCutLine)) {
					T itemId = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]);
					table.SetRaw(itemId, attribute, removeUnderscore ? elements[1].Replace("_", " ") : elements[1]);
				}

				Debug.Ignore(() => DbDebugHelper.OnLoaded(db.DbSource, db.ProjectDatabase.MetaGrf.FindTkPath(file), db));
			}
			catch (Exception err) {
				Debug.Ignore(() => DbDebugHelper.OnExceptionThrown(db.DbSource, file, db));
				throw new Exception(TextFileHelper.GetLastError(), err);
			}
		}

		private static void _loadCardIllustrationNames(AbstractDb<int> db, byte[] data) {
			var table = db.Table;
			DbDebugItem<int> debug = new DbDebugItem<int>(null);
			TextFileHelper.LatestFile = db.ProjectDatabase.MetaGrf.LatestFile;

			foreach (string[] elements in TextFileHelper.GetElements(data)) {
				try {
					int itemId = Int32.Parse(elements[0]);
					table.SetRaw(itemId, ClientItemAttributes.Illustration, elements[1]);
					table.SetRaw(itemId, ClientItemAttributes.IsCard, true, true);
				}
				catch (Exception err) {
					if (!debug.ReportException(err)) return;
				}
			}

			Debug.Ignore(() => DbDebugHelper.OnLoaded(db.DbSource, db.ProjectDatabase.MetaGrf.FindTkPath(db.ProjectDatabase.MetaGrf.LatestFile), db));
		}

		private static void _loadCardPrefixes(AbstractDb<int> db, byte[] data) {
			var table = db.Table;
			DbDebugItem<int> debug = new DbDebugItem<int>(null);
			TextFileHelper.LatestFile = db.ProjectDatabase.MetaGrf.LatestFile;

			foreach (string[] elements in TextFileHelper.GetElements(data)) {
				try {
					int itemId = Int32.Parse(elements[0]);
					table.SetRaw(itemId, ClientItemAttributes.Affix, elements[1]);
					table.SetRaw(itemId, ClientItemAttributes.IsCard, true, true);
				}
				catch (Exception err) {
					if (!debug.ReportException(err)) return;
				}
			}

			Debug.Ignore(() => DbDebugHelper.OnLoaded(db.DbSource, db.ProjectDatabase.MetaGrf.FindTkPath(db.ProjectDatabase.MetaGrf.LatestFile), db));
		}

		private static void _loadCardPostfixes(AbstractDb<int> db, byte[] data) {
			var table = db.Table;
			DbDebugItem<int> debug = new DbDebugItem<int>(null);
			TextFileHelper.LatestFile = db.ProjectDatabase.MetaGrf.LatestFile;

			foreach (string element in TextFileHelper.GetSingleElement(data)) {
				try {
					int itemId = Int32.Parse(element);
					table.SetRaw(itemId, ClientItemAttributes.IsCard, true, true);
					table.SetRaw(itemId, ClientItemAttributes.Postfix, true, true);
				}
				catch (Exception err) {
					if (!debug.ReportException(err)) return;
				}
			}

			Debug.Ignore(() => DbDebugHelper.OnLoaded(db.DbSource, db.ProjectDatabase.MetaGrf.FindTkPath(db.ProjectDatabase.MetaGrf.LatestFile), db));
		}

		private static void _toLuaDescription(StringBuilder builder, string value) {
			if (value.StartsWith("\r\n"))
				value = value.Remove(0, 2);

			if (value.EndsWith("\r\n"))
				value = value.Substring(0, value.Length - 2);

			string[] lines = value.Replace("\r\n", "\n").Split('\n');
			string line;

			if (lines.Length == 1 && lines[0] == "")
				return;

			for (int i = 0; i < lines.Length - 1; i++) {
				line = lines[i];
				builder.Append("\t\t\t\"");
				builder.Append(line);
				builder.AppendLine("\",");
			}

			if (lines.Length > 0) {
				line = lines[lines.Length - 1];
				builder.Append("\t\t\t\"");
				builder.Append(line);
				builder.AppendLine("\"");
			}
		}

		internal static void LoadEntry(AbstractDb<int> db, string file) {
			if (file == null) {
				Debug.Ignore(() => DbDebugHelper.OnUpdate(db.DbSource, null, "ItemInfo table will not be loaded."));
				return;
			}

			LuaList list;

			var table = db.Table;
			var metaGrf = db.ProjectDatabase.MetaGrf;

			string outputPath = GrfPath.Combine(SdeAppConfiguration.TempPath, Path.GetFileName(file));

			byte[] itemData = metaGrf.GetData(file);

			if (itemData == null) {
				Debug.Ignore(() => DbDebugHelper.OnUpdate(db.DbSource, file, "File not found."));
				return;
			}

			File.WriteAllBytes(outputPath, itemData);

			if (!File.Exists(outputPath))
				return;

			if (Methods.ByteArrayCompare(itemData, 0, 4, new byte[] { 0x1b, 0x4c, 0x75, 0x61 }, 0)) {
				// Decompile lub file
				Lub lub = new Lub(itemData);
				var text = lub.Decompile();
				itemData = EncodingService.DisplayEncoding.GetBytes(text);
				File.WriteAllBytes(outputPath, itemData);
			}

			DbIOMethods.DetectAndSetEncoding(itemData);

			using (LuaReader reader = new LuaReader(outputPath, DbIOMethods.DetectedEncoding)) {
				list = reader.ReadAll();
			}

			LuaKeyValue itemVariable = list.Variables[0] as LuaKeyValue;

			if (itemVariable != null && itemVariable.Key == "tbl") {
				LuaList items = itemVariable.Value as LuaList;

				if (items != null) {
					foreach (LuaKeyValue item in items.Variables) {
						_loadEntry(table, item);
					}
				}
			}
			else {
				// Possible copy-paste data
				foreach (LuaKeyValue item in list.Variables) {
					_loadEntry(table, item);
				}
			}

			Debug.Ignore(() => DbDebugHelper.OnLoaded(db.DbSource, metaGrf.FindTkPath(file), db));
		}

		private static void _loadEntry(Table<int, ReadableTuple<int>> table, LuaKeyValue item) {
			int itemIndex = Int32.Parse(item.Key.Substring(1, item.Key.Length - 2));
			LuaList itemProperties = item.Value as LuaList;
			LuaList itemList;

			if (itemProperties != null) {
				foreach (LuaKeyValue itemProperty in itemProperties.Variables) {
					switch(itemProperty.Key) {
						case "unidentifiedDisplayName":
							table.SetRaw(itemIndex, ClientItemAttributes.UnidentifiedDisplayName, DbIOMethods.RemoveQuotes(((LuaValue)itemProperty.Value).Value));
							break;
						case "unidentifiedResourceName":
							table.SetRaw(itemIndex, ClientItemAttributes.UnidentifiedResourceName, DbIOMethods.RemoveQuotes(((LuaValue)itemProperty.Value).Value));
							break;
						case "identifiedDisplayName":
							table.SetRaw(itemIndex, ClientItemAttributes.IdentifiedDisplayName, DbIOMethods.RemoveQuotes(((LuaValue)itemProperty.Value).Value));
							break;
						case "identifiedResourceName":
							table.SetRaw(itemIndex, ClientItemAttributes.IdentifiedResourceName, DbIOMethods.RemoveQuotes(((LuaValue)itemProperty.Value).Value));
							break;
						case "slotCount":
							table.SetRaw(itemIndex, ClientItemAttributes.NumberOfSlots, ((LuaValue)itemProperty.Value).Value);
							break;
						case "ClassNum":
							table.SetRaw(itemIndex, ClientItemAttributes.ClassNumber, ((LuaValue)itemProperty.Value).Value);
							break;
						case "unidentifiedDescriptionName":
							itemList = itemProperty.Value as LuaList;
							if (itemList != null) {
								StringBuilder b = new StringBuilder();
								b.Append("\r\n");
								foreach (LuaValue itemDescItem in itemList.Variables) {
									b.Append(DbIOMethods.RemoveQuotes(itemDescItem.Value));
									b.Append("\r\n");
								}
								table.SetRaw(itemIndex, ClientItemAttributes.UnidentifiedDescription, b.ToString());
							}
							break;
						case "identifiedDescriptionName":
							itemList = itemProperty.Value as LuaList;
							if (itemList != null) {
								StringBuilder b = new StringBuilder();
								b.Append("\r\n");
								foreach (LuaValue itemDescItem in itemList.Variables) {
									b.Append(DbIOMethods.RemoveQuotes(itemDescItem.Value));
									b.Append("\r\n");
								}
								table.SetRaw(itemIndex, ClientItemAttributes.IdentifiedDescription, b.ToString());
							}
							break;
					}
				}
			}
		}
	}
}