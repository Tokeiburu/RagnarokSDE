using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Database;
using SDE.Editor.Engines;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.Parsers.Libconfig;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Writers;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace SDE.Editor.Generic.Parsers.Generic {
	public sealed class DbIOMethods {
		public static Encoding DetectedEncoding { get; set; }

		public delegate void DbIOWriteEntryMethod<TKey>(StringBuilder builder, ReadableTuple<TKey> tuple);

		public static void DbIOWriterConf<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, DbIOWriteEntryMethod<TKey> writeEntryMethod) {
			try {
				if (debug.FileType == FileType.Conf) {
					try {
						var lines = new LibconfigParser(debug.OldPath, LibconfigMode.Write);
						lines.Remove(db);

						foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
							string key = tuple.Key.ToString();

							StringBuilder builder = new StringBuilder();
							writeEntryMethod(builder, tuple);
							lines.Write(key, builder.ToString());
						}

						lines.WriteFile(debug.FilePath);
					}
					catch (Exception err) {
						debug.ReportException(err);
					}
				}
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		/// <summary>
		/// Parses the read strings from the lua file
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		internal static string RemoveQuotes(string value) {
			value = value.Unescape(EscapeMode.RemoveQuote | EscapeMode.KeepAsciiCode);

			if (DetectedEncoding.CodePage == EncodingService.DisplayEncoding.CodePage)
				return value;

			return EncodingService.DisplayEncoding.GetString(DetectedEncoding.GetBytes(value));
		}

		/// <summary>
		/// Detects and sets the encoding for further parsing.
		/// </summary>
		/// <param name="itemData">The file data.</param>
		public static void DetectAndSetEncoding(byte[] itemData) {
			DetectedEncoding = EncodingService.DetectEncoding(itemData) ?? EncodingService.Utf8;
		}

		public static void DbWriterAnyComma<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			if (typeof(TKey) == typeof(int))
				DbWriterComma((DbDebugItem<int>)(object)debug, (AbstractDb<int>)(object)db, 0, db.AttributeList.Attributes.Count, (t, p) => { });
			else
				DbWriterComma((DbDebugItem<string>)(object)debug, (AbstractDb<string>)(object)db);
		}

		public static void DbWriterComma(DbDebugItem<int> debug, AbstractDb<int> db) {
			DbWriterComma(debug, db, 0, db.AttributeList.Attributes.Count);
		}

		public static void DbWriterComma(DbDebugItem<int> debug, AbstractDb<int> db, int from, int to) {
			DbWriterComma(debug, db, from, to, (t, p) => { });
		}

		public static void DbWriterComma(DbDebugItem<int> debug, AbstractDb<int> db, int from, int to, Action<ReadableTuple<int>, List<object>> funcItems) {
			try {
				IntLineStream lines = new IntLineStream(debug.OldPath);

				if (db.Attached["EntireRewrite"] != null && (bool)db.Attached["EntireRewrite"]) {
					lines.ClearAfterComments();
				}

				lines.Remove(db);
				string line;

				for (int i = from; i < to; i++) {
					DbAttribute att = db.AttributeList.Attributes[i];

					if ((att.Visibility & VisibleState.Hidden) == VisibleState.Hidden) {
						to = i;
						break;
					}
				}

				List<DbAttribute> attributes = new List<DbAttribute>(db.AttributeList.Attributes.Skip(from).Take(to));
				attributes.Reverse();

				List<DbAttribute> attributesToRemove =
					(from attribute in attributes
						where db.Attached[attribute.DisplayName] != null
						let isLoaded = (bool)db.Attached[attribute.DisplayName]
						where !isLoaded
						select attribute).ToList();

				IEnumerable<ReadableTuple<int>> list;

				if (db.Attached["EntireRewrite"] != null && (bool)db.Attached["EntireRewrite"]) {
					list = db.Table.FastItems.Where(p => !p.Deleted).OrderBy(p => p.Key);
				}
				else {
					list = db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.Key);
				}

				foreach (ReadableTuple<int> tuple in list) {
					int key = tuple.GetKey<int>();
					List<object> rawElements = tuple.GetRawElements().Skip(from).Take(to).ToList();
					funcItems(tuple, rawElements);

					foreach (var attribute in attributesToRemove) {
						rawElements.RemoveAt(attribute.Index - from);
					}

					line = string.Join(",", rawElements.Select(p => (p ?? "").ToString()).ToArray());
					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbWriterComma(DbDebugItem<string> debug, AbstractDb<string> db) {
			try {
				StringLineStream lines = new StringLineStream(debug.OldPath, ',', false);
				lines.Remove(db);
				string line;

				int numOfElements = debug.AbsractDb.Table.AttributeList.Attributes.Count;
				var v = debug.AbsractDb.Table.AttributeList.Attributes.FirstOrDefault(p => (p.Visibility & VisibleState.Hidden) == VisibleState.Hidden);

				if (v != null) {
					numOfElements = debug.AbsractDb.Table.AttributeList.Attributes.IndexOf(v);
				}

				foreach (ReadableTuple<string> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.Key)) {
					line = string.Join(",", tuple.GetRawElements().Take(numOfElements).Select(p => (p ?? "").ToString()).ToArray());
					lines.Write(tuple.Key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbLoaderAny<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, TextFileHelper.TextFileHelperGetterDelegate getter, bool uniqueKey = true) {
			List<DbAttribute> attributes = new List<DbAttribute>(db.AttributeList.Attributes);
			int indexOffset = uniqueKey ? 1 : 0;
			int attributesOffset = uniqueKey ? 0 : 1;
			bool hasGuessedAttributes = false;
			int rndOffset = 0;
			Func<string, TKey> keyConverter;

			if (typeof(TKey) == typeof(int))
				keyConverter = q => (TKey)(object)Int32.Parse(q);
			else
				keyConverter = q => (TKey)(object)q;

			if (!uniqueKey) {
				TextFileHelper.SaveLastLine = true;
			}

			foreach (string[] elements in getter(FtpHelper.ReadAllBytes(debug.FilePath))) {
				try {
					if (!hasGuessedAttributes) {
						GuessAttributes(elements, attributes, -1, db);
						hasGuessedAttributes = true;
					}

					TKey id;

					if (uniqueKey) {
						id = keyConverter(elements[0]);
					}
					else {
						id = (TKey)(object)TextFileHelper.LastLineRead;

						while (db.Table.ContainsKey(id)) {
							id = (TKey)(object)((string)(object)id + "_" + rndOffset++);
						}
					}

					db.Table.SetRawRange(id, attributesOffset, indexOffset, attributes, elements);
					//for (int index = indexOffset; index < elements.Length; index++) {
					//	db.Table.SetRaw(id, attributes[index + attributesOffset], elements[index]);
					//}
				}
				catch {
					if (elements.Length <= 0) {
						if (!debug.ReportIdException("#")) return;
					}
					else if (!debug.ReportIdException(elements[0])) return;
				}
			}

			if (!uniqueKey) {
				TextFileHelper.SaveLastLine = false;
			}
		}

		public static void DbLoaderCommaRange<T>(DbDebugItem<T> debug, AttributeList list, int indexStart, int length, bool addAutomatically = true) {
			var table = debug.AbsractDb.Table;

			foreach (string[] elements in TextFileHelper.GetElementsByCommas(FtpHelper.ReadAllBytes(debug.FilePath))) {
				try {
					T itemId = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]);

					if (!addAutomatically && !table.ContainsKey(itemId)) {
						TkDictionary<T, string[]> phantomTable;

						if (!debug.AbsractDb.Attached.ContainsKey("Phantom." + debug.DbSource.Filename)) {
							phantomTable = new TkDictionary<T, string[]>();
							debug.AbsractDb.Attached["Phantom." + debug.DbSource.Filename] = phantomTable;
						}
						else {
							phantomTable = (TkDictionary<T, string[]>)debug.AbsractDb.Attached["Phantom." + debug.DbSource.Filename];
						}

						phantomTable[itemId] = elements;
						continue;
					}

					int max = length;

					for (int index = 1; index < elements.Length && max > 0; index++) {
						DbAttribute property = list.Attributes[index + indexStart - 1];
						table.SetRaw(itemId, property, elements[index]);
						max--;
					}
				}
				catch (Exception err) {
					if (!debug.ReportException(err)) return;
				}
			}
		}

		public static void DbLoaderComma<T>(DbDebugItem<T> debug, AttributeList list, DbIOItems.DbCommaFunctionDelegate<T> function, bool addAutomatically = true) {
			DbLoaderComma(debug, list, function, TextFileHelper.GetElementsByCommas, addAutomatically);
		}

		public static void DbLoaderComma<T>(DbDebugItem<T> debug, AttributeList list, DbIOItems.DbCommaFunctionDelegate<T> function, TextFileHelper.TextFileHelperGetterDelegate getter, bool addAutomatically = true) {
			var table = debug.AbsractDb.Table;

			foreach (string[] elements in getter(FtpHelper.ReadAllBytes(debug.FilePath))) {
				try {
					if (!addAutomatically) {
						T id = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]);

						if (!table.ContainsKey(id)) {
							TkDictionary<T, string[]> phantomTable;

							if (!debug.AbsractDb.Attached.ContainsKey("Phantom." + debug.DbSource.Filename)) {
								phantomTable = new TkDictionary<T, string[]>();
								debug.AbsractDb.Attached["Phantom." + debug.DbSource.Filename] = phantomTable;
							}
							else {
								phantomTable = (TkDictionary<T, string[]>)debug.AbsractDb.Attached["Phantom." + debug.DbSource.Filename];
							}

							phantomTable[id] = elements;
							continue;
						}
					}

					function(debug, list, elements, table);
				}
				catch (Exception err) {
					if (!debug.ReportException(err)) return;
				}
			}
		}

		public static void DbLoaderCommaNoCast<T>(DbDebugItem<T> debug, AttributeList list, int indexStart, int length) {
			var table = debug.AbsractDb.Table;

			foreach (string[] elements in TextFileHelper.GetElementsByCommas(FtpHelper.ReadAllBytes(debug.FilePath))) {
				try {
					T itemId = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]);
					int max = length;

					for (int index = 1; index < elements.Length && max > 0; index++) {
						DbAttribute property = list.Attributes[index + indexStart - 1];

						int previousVal = 0;

						if (table.ContainsKey(itemId)) {
							previousVal = table.GetTuple(itemId).GetValue<int>(ServerSkillAttributes.Flag);
						}

						table.SetRaw(itemId, property, Int32.Parse(elements[index]) | previousVal);
						max--;
					}
				}
				catch (Exception err) {
					if (!debug.ReportException(err)) return;
				}
			}
		}

		public static void DbLoaderTabs<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			DbLoaderAny(debug, db, TextFileHelper.GetElementsByTabs);
		}

		public static void DbLoaderComma<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			DbLoaderAny(debug, db, TextFileHelper.GetElementsByCommas);
		}

		public static void GuessAttributes(ICollection<string> elements, ICollection<DbAttribute> attributes, int numberOfAttributes, BaseDb db) {
			if (db.Attached["Scanned"] == null || (db.Attached["FromUserRawInput"] != null && (bool)db.Attached["FromUserRawInput"])) {
				if (attributes.Any(p => p.IsSkippable)) {
					attributes.Where(p => p.IsSkippable).ToList().ForEach(p => db.Attached[p.ToString()] = true);

					if (numberOfAttributes < 0) {
						// We have to detect how many attributes there are
						if (db.Attached["NumberOfAttributesToGuess"] != null) {
							numberOfAttributes = (int)db.Attached["NumberOfAttributesToGuess"];
						}
						else {
							numberOfAttributes = attributes.Count(p => (p.Visibility & VisibleState.Visible) == VisibleState.Visible);
						}
					}

					while (elements.Count < numberOfAttributes && attributes.Any(p => p.IsSkippable)) {
						var attribute = attributes.First(p => p.IsSkippable);
						attributes.Remove(attribute);

						if (db.Attached["FromUserRawInput"] == null || !((bool)db.Attached["FromUserRawInput"])) {
							db.Attached[attribute.DisplayName] = false;
						}
					}
				}

				db.Attached["Scanned"] = true;
				db.Attached["FromUserRawInput"] = false;
			}
		}

		public static void DbDirectCopyWriter<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				if (debug.OldPath != debug.FilePath && !FtpHelper.SameFile(debug.OldPath, debug.FilePath)) {
					// Test their modified date
					FtpHelper.Delete(debug.FilePath);
					FtpHelper.Copy(debug.OldPath, debug.FilePath);
				}
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}
	}
}