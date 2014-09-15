using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Database;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Engines.Parsers;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders {
	public static partial class DbLoaders {
		public static void DbItemGroups<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			foreach (DbAttribute attribute in ServerItemGroupSubAttributes.AttributeList.Attributes) {
				db.Attached[attribute.DisplayName] = false;
			}

			if (debug.FileType == FileType.Txt) {
				using (StreamReader reader = new StreamReader(File.OpenRead(debug.FilePath))) {
					string line;

					while (!reader.EndOfStream) {
						line = reader.ReadLine();

						if (line != null && line.StartsWith("import: ")) {
							string dbPath = AllLoaders.DetectPathAll(line.Replace("import: ", ""));

							if (dbPath == null) {
								ErrorHandler.HandleException("Couldn't find the file '" + line.Replace("import: ", "") + "'.");
							}
							else {
								db.Attached[dbPath] = new Tuple<string, HashSet<int>>(line.Replace("import: ", ""), new HashSet<int>());
								_loadItemsGroupdDb(db, dbPath);
							}
						}
					}
				}
			}
			else if (debug.FileType == FileType.Conf) {
				ItemGroupParser itemHelper;
				Table<int, ReadableTuple<int>> itemsDb = db.Get<int>(ServerDBs.Items);
				int index = ServerItemAttributes.AegisName.Index;
				var table = db.Table;

				foreach (string elements in TextFileHelper.GetElementsByParenthesis(File.ReadAllBytes(debug.FilePath))) {
					itemHelper = new ItemGroupParser(elements);

					try {
						Tuple tupleItem = itemsDb.FastItems.FirstOrDefault(p => String.Compare(p.GetStringValue(index), itemHelper.Id, StringComparison.OrdinalIgnoreCase) == 0);

						if (tupleItem == null) {
							debug.ReportIdException("Item ID '" + itemHelper.Id + "' couldn't be found.", itemHelper.Id);
							continue;
						}

						TKey itemId = tupleItem.GetKey<TKey>();

						if (!table.ContainsKey(itemId)) {
							ReadableTuple<TKey> tuple = new ReadableTuple<TKey>(itemId, db.AttributeList);
							tuple.SetRawValue(ServerItemGroupAttributes.Table, new Dictionary<int, ReadableTuple<int>>());
							table.Add(itemId, tuple);
						}

						for (int i = 0; i < itemHelper.Quantities.Count; i++) {
							string onameId = itemHelper.Quantities[i].Item1;
							string orate = itemHelper.Quantities[i].Item2;

							int rate;

							tupleItem = itemsDb.FastItems.FirstOrDefault(p => String.Compare(p.GetStringValue(index), onameId, StringComparison.OrdinalIgnoreCase) == 0);

							if (tupleItem == null) {
								debug.ReportIdException("Item ID '" + itemHelper.Quantities[i].Item1 + "' couldn't be found in group '" + itemHelper.Id + "'.", itemHelper.Id);
								continue;
							}

							int nameId = tupleItem.GetKey<int>();
							Int32.TryParse(orate, out rate);

							Dictionary<int, ReadableTuple<int>> dico = (Dictionary<int, ReadableTuple<int>>)table.GetRaw(itemId, ServerItemGroupAttributes.Table);

							ReadableTuple<int> tuple = new ReadableTuple<int>(nameId, ServerItemGroupSubAttributes.AttributeList);
							tuple.SetRawValue(ServerItemGroupSubAttributes.Rate, rate);
							dico[nameId] = tuple;
						}
					}
					catch {
						if (!debug.ReportIdException(itemHelper.Id)) return;
					}
				}
			}
		}
		private static void _loadItemsGroupdDb<TKey>(AbstractDb<TKey> db, string file) {
			int numberOfErrors = 3;
			AllLoaders.LatestFile = file;

			if (String.IsNullOrEmpty(file)) {
				DbLoaderErrorHandler.Handle("File not found " + ServerDBs.ItemGroups + ".", ErrorLevel.NotSpecified);
				return;
			}

			var table = db.Table;
			HashSet<int> loadedIds = ((Tuple<string, HashSet<int>>)db.Attached[file]).Item2;

			foreach (string[] elements in TextFileHelper.GetElementsByCommas(File.ReadAllBytes(file))) {
				try {
					TKey itemId;
					int iItemId;

					if (Int32.TryParse(elements[0], out iItemId)) {
						itemId = (TKey) (object) iItemId;
					}
					else {
						var constantDb = db.Database.GetDb<string>(ServerDBs.Constants);

						if (!constantDb.IsLoaded) {
							constantDb.LoadDb();
						}

						var tuple = constantDb.Table.TryGetTuple(elements[0]);

						if (tuple == null) {
							if (AllLoaders.GenericErrorHandler(ref numberOfErrors, elements[0])) return;
							continue;
						}

						itemId = (TKey) (object) tuple.GetValue<int>(1);
					}

					string orate = elements[2];

					int nameId;
					int rate;

					if (Int32.TryParse(elements[1], out nameId)) { }
					else {
						var itemDb = db.Database.GetDb<int>(ServerDBs.Items);

						if (!itemDb.IsLoaded) {
							itemDb.LoadDb();
						}

						var tuple = itemDb.Table.FastItems.FirstOrDefault(p => p.GetStringValue(ServerItemAttributes.AegisName.Index) == elements[1]);

						if (tuple == null) {
							if (AllLoaders.GenericErrorHandler(ref numberOfErrors, elements[0])) return;
							continue;
						}

						nameId = tuple.GetKey<int>();
					}

					Int32.TryParse(orate, out rate);

					var id = (object) itemId;
					loadedIds.Add((int) id);

					if (!table.ContainsKey(itemId)) {
						ReadableTuple<TKey> tuple = new ReadableTuple<TKey>(itemId, db.AttributeList);
						tuple.SetRawValue(ServerItemGroupAttributes.Table, new Dictionary<int, ReadableTuple<int>>());
						table.Add(itemId, tuple);
					}

					Dictionary<int, ReadableTuple<int>> dico = (Dictionary<int, ReadableTuple<int>>)table.GetRaw(itemId, ServerItemGroupAttributes.Table);

					ReadableTuple<int> newTuple = new ReadableTuple<int>(nameId, ServerItemGroupSubAttributes.AttributeList);
					List<DbAttribute> attributes = new List<DbAttribute>(ServerItemGroupSubAttributes.AttributeList.Attributes);

					for (int i = 2; i < elements.Length; i++) {
						db.Attached[attributes[i - 1].DisplayName] = true;
						newTuple.SetRawValue(attributes[i - 1], elements[i]);
					}

					dico[nameId] = newTuple;
				}
				catch {
					if (AllLoaders.GenericErrorHandler(ref numberOfErrors, elements[0])) return;
				}
			}
		}
	}
}