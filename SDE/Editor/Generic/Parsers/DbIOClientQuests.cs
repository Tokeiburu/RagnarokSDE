using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Database;
using ErrorManager;
using SDE.Editor.Engines.BackupsEngine;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using Utilities;
using Utilities.Services;

namespace SDE.Editor.Generic.Parsers {
	public sealed class DbIOClientQuests {
		public static void Loader(DbDebugItem<int> debug, AbstractDb<int> db) {
			//_loadDataQuest(db, @"C:\Users\Sylvain\Desktop\Desktop\Grfs\Official\iRO-Ragray\data\questid2display.txt");
			_loadDataQuest(db, ProjectConfiguration.ClientQuest);
		}

		public static void Writer(DbDebugItem<int> debug, AbstractDb<int> db) {
			try {
				if (db.Table.Commands.CommandIndex == -1 &&
				    !db.IsModified) return;

				db.ProjectDatabase.MetaGrf.Clear();
				string path = ProjectConfiguration.ClientQuest;

				_dbClientQuestWrite(db.ProjectDatabase, db, path);

				try {
					db.ProjectDatabase.MetaGrf.SaveAndReload();
				}
				catch (OperationCanceledException) {
					ErrorHandler.HandleException("Failed to save the client files.");
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private static void _dbClientQuestWrite(SdeDatabase gdb, AbstractDb<int> db, string path) {
			if (path == null || gdb.MetaGrf.GetData(path) == null) {
				Debug.Ignore(() => DbDebugHelper.OnWriteStatusUpdate(ServerDbs.CQuests, "data\\questid2display.txt", null, "Table not saved."));
				return;
			}

			BackupEngine.Instance.BackupClient(path, gdb.MetaGrf);

			//string tmpFilename = Path.Combine(SdeAppConfiguration.TempPath, Path.GetFileName(path));
			Encoding encoder = EncodingService.DisplayEncoding;
			byte[] tmpBuffer;
			byte[] lineFeedByte = encoder.GetBytes(SdeStrings.LineFeed);
			byte[] doubleLineFeedByte = encoder.GetBytes(SdeStrings.LineFeed + SdeStrings.LineFeed);

			using (MemoryStream memStream = new MemoryStream()) {
				IEnumerable<ReadableTuple<int>> items = gdb.GetDb<int>(ServerDbs.CQuests).Table.GetSortedItems();

				foreach (ReadableTuple<int> item in items) {
					tmpBuffer = encoder.GetBytes(
						item.GetValue<int>(ClientQuestsAttributes.Id) + "#" +
						item.GetValue<string>(ClientQuestsAttributes.Name) + "#" +
						item.GetValue<string>(ClientQuestsAttributes.SG) + "#" +
						item.GetValue<string>(ClientQuestsAttributes.QUE) + "#" +
						"\r\n" + item.GetValue<string>(ClientQuestsAttributes.FullDesc) + "#" +
						"\r\n" + item.GetValue<string>(ClientQuestsAttributes.ShortDesc) + "#");

					memStream.Write(tmpBuffer, 0, tmpBuffer.Length);
					memStream.Write(doubleLineFeedByte, 0, doubleLineFeedByte.Length);
				}

				memStream.Write(lineFeedByte, 0, lineFeedByte.Length);

				tmpBuffer = new byte[memStream.Length];
				Buffer.BlockCopy(memStream.GetBuffer(), 0, tmpBuffer, 0, tmpBuffer.Length);

				//File.WriteAllBytes(tmpFilename, tmpBuffer);
			}

			string copyPath = path;

			try {
				gdb.MetaGrf.SetData(copyPath, tmpBuffer);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			Debug.Ignore(() => DbDebugHelper.OnWriteStatusUpdate(ServerDbs.CQuests, db.ProjectDatabase.MetaGrf.FindTkPath(path), null, "Saving client table (questdb)."));
		}

		private static bool _isKorean(string value) {
			return value != EncodingService.Korean.GetString(EncodingService.Ansi.GetBytes(value));
		}

		public static void SetQuestValue(Table<int, ReadableTuple<int>> table, ReadableTuple<int> tuple, string[] elements, int id) {
			string value = tuple.GetValue<string>(ClientQuestsAttributes.AttributeList[id]);

			if (value == "") {
				if (elements[id] == "")
					return;

				table.Commands.Set(tuple, ClientQuestsAttributes.AttributeList[id], elements[id]);
			}
			else if (elements[id] == "") {
				//table.Set(tuple.Key, ClientQuestsAttributes.AttributeList[id], value);
			}
			else if (_isKorean(value)) {
				if (elements[id] == value)
					return;

				table.Commands.Set(tuple, ClientQuestsAttributes.AttributeList[id], elements[id]);
			}
			else if (_isKorean(elements[id])) {
				//table.Set(tuple.Key, ClientQuestsAttributes.AttributeList[id], value);
			}
		}

		public static void SetQuestValue(Table<int, ReadableTuple<int>> table, ReadableTuple<int> tuple, string element, int id) {
			string value = tuple.GetValue<string>(ClientQuestsAttributes.AttributeList[id]);

			if (value == "") {
				table.Commands.Set(tuple, ClientQuestsAttributes.AttributeList[id], element);
			}
			else if (element == "") {
				//table.Set(tuple.Key, ClientQuestsAttributes.AttributeList[id], value);
			}
			else if (_isKorean(value)) {
				table.Commands.Set(tuple, ClientQuestsAttributes.AttributeList[id], element);
			}
			else if (_isKorean(element)) {
				//table.Set(tuple.Key, ClientQuestsAttributes.AttributeList[id], value);
			}
		}

		private static void _loadDataQuest(AbstractDb<int> db, string file) {
			var table = db.Table;
			TextFileHelper.LatestFile = file;

			try {
				foreach (string[] elements in TextFileHelper.GetElementsInt(db.ProjectDatabase.MetaGrf.GetData(file))) {
					int itemId = Int32.Parse(elements[0]);

					table.SetRaw(itemId, ClientQuestsAttributes.Name, elements[1]);
					table.SetRaw(itemId, ClientQuestsAttributes.SG, elements[2]);
					table.SetRaw(itemId, ClientQuestsAttributes.QUE, elements[3]);
					table.SetRaw(itemId, ClientQuestsAttributes.FullDesc, elements[4]);
					table.SetRaw(itemId, ClientQuestsAttributes.ShortDesc, elements[5]);
				}

				Debug.Ignore(() => DbDebugHelper.OnLoaded(db.DbSource, db.ProjectDatabase.MetaGrf.FindTkPath(file), db));
			}
			catch (Exception err) {
				Debug.Ignore(() => DbDebugHelper.OnExceptionThrown(db.DbSource, file, db));
				throw new Exception(TextFileHelper.GetLastError(), err);
			}
		}
	}
}