using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Database;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.Parsers.Libconfig;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.Editor.Writers;
using Utilities;
using Utilities.Extension;

namespace SDE.Editor.Generic.Parsers {
	public sealed class DbIOItems {
		public delegate void DbCommaFunctionDelegate<T>(DbDebugItem<T> debug, AttributeList list, string[] elements, Table<T, ReadableTuple<T>> table);

		public static void Loader<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			if (debug.FileType == FileType.Txt) {
				DbIOMethods.DbLoaderAny(debug, db, TextFileHelper.GetElementsByCommas2);

				if (db.ProjectDatabase.IsRenewal) {
					string val;

					foreach (var tuple in db.Table.FastItems) {
						try {
							val = tuple.GetStringValue(ServerItemAttributes.Attack.Index);

							if (val != null && val.Contains(":")) {
								string[] values = val.Split(':');

								tuple.SetRawValue(ServerItemAttributes.Attack, values[0]);
								tuple.SetRawValue(ServerItemAttributes.Matk, values[1]);
							}
						}
						catch (Exception) {
							if (!debug.ReportIdException(tuple.Key)) return;
						}
					}
				}
			}
			else if (debug.FileType == FileType.Conf) {
				var ele = new LibconfigParser(debug.FilePath);
				var table = debug.AbsractDb.Table;

				foreach (var item in ele.Output["copy_paste"] ?? ele.Output["item_db"]) {
					TKey itemId = (TKey)(object)Int32.Parse(item["Id"]);

					var defaultGender = "2";

					// The .conf is actually quite confusing
					// Overriding values are not setup for some reason and the parser
					// has to guess and fix the issues.
					int ival;
					if (Int32.TryParse(item["Id"], out ival)) {
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

					if (type == "4" || type == "5") {
						defaultRefineable = "true";

						if (!SdeAppConfiguration.RevertItemTypes) {
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

		private static object _jobToId(AbstractDb<int> adb, LibconfigObject parser) {
			int outputJob = 0;
			var value = parser["Job"];

			if (value == null) {
				return "";
			}

			if (value is LibconfigString) {
				return value.ObjectValue;
			}

			if (value is LibconfigArrayBase) {
				foreach (LibconfigKeyValue job in value.OfType<LibconfigKeyValue>()) {
					int ival;

					if (ItemDbJobs.TryGetValue(job.Key, out ival)) {
						if (ItemDbJobs.ContainsKey(job.Key)) {
							if (Boolean.Parse(job.Value))
								outputJob |= ival;
							else
								outputJob &= ~ival;
						}
						else {
							throw new Exception("Unknown job : " + job.ObjectValue);
						}
					}
				}

				adb.Attached["ItemDb.UseExtendedJobs"] = true;
				return "0x" + outputJob.ToString("X8");
			}

			return "";
		}

		public static void DbItemsBuyingStoreFunction<T>(DbDebugItem<T> debug, AttributeList list, string[] elements, Table<T, ReadableTuple<T>> table) {
			T itemId = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]);
			table.SetRaw(itemId, ServerItemAttributes.BuyingStore, "true");
		}

		public static void DbItemsStackFunction<T>(DbDebugItem<T> debug, AttributeList list, string[] elements, Table<T, ReadableTuple<T>> table) {
			T itemId = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]);
			table.SetRaw(itemId, ServerItemAttributes.Stack, "[" + elements[1] + "," + elements[2] + "]");
		}

		public static void DbItemsWriter(DbDebugItem<int> debug, AbstractDb<int> db) {
			try {
				StringBuilder builder = new StringBuilder();

				if (debug.FileType == FileType.Txt) {
					if (DbPathLocator.GetServerType() == ServerType.RAthena) {
						DbIOMethods.DbWriterComma(debug, db, 0, ServerItemAttributes.OnUnequipScript.Index + 1, (tuple, items) => {
							if (db.ProjectDatabase.IsRenewal) {
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
					FtpHelper.WriteAllText(debug.FilePath, builder.ToString());
				}
				else if (debug.FileType == FileType.Conf) {
					DbIOMethods.DbIOWriterConf(debug, db, (r, q) => WriteEntry(db, r, q));
				}
				else if (debug.FileType == FileType.Sql) {
					SqlParser.DbSqlItems(debug, db);
				}
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbItemsWriterSub<TKey>(StringBuilder builder, AbstractDb<TKey> db, IEnumerable<ReadableTuple<TKey>> tuples, ServerType to) {
			if (to == ServerType.RAthena) {
				bool fromTxtDb = DbPathLocator.DetectPath(db.DbSource).IsExtension(".txt");

				foreach (ReadableTuple<TKey> tuple in tuples) {
					List<string> rawElements = tuple.GetRawElements().Take(22).Select(p => p.ToString()).ToList();

					if (tuple.Normal && fromTxtDb && tuple.GetValue<int>(ServerItemAttributes.Matk) == 0) {
						builder.AppendLine(String.Join(",", rawElements.ToArray()));
						continue;
					}

					string script1 = tuple.GetValue<string>(19);
					string script2 = tuple.GetValue<string>(20);
					string script3 = tuple.GetValue<string>(21);
					string refine = tuple.GetValue<string>(17);

					if (refine == "") {
					}
					else if (refine == "true" || refine == "1") {
						refine = "1";
					}
					else {
						refine = "0";
					}

					string atk = DbIOFormatting.ZeroDefault(rawElements[7]);

					if (db.ProjectDatabase.IsRenewal) {
						string matk = tuple.GetValue<string>(ServerItemAttributes.Matk) ?? "";

						if (matk != "" && matk != "0") {
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
			else if (to == ServerType.Hercules) {
				foreach (var tuple in tuples.OrderBy(p => p.GetKey<int>()).OfType<ReadableTuple<int>>()) {
					WriteEntry(db, builder, tuple);
					builder.AppendLine();
				}
			}
		}

		private static TkDictionary<TKey, string[]> _getPhantomTable<TKey>(DbDebugItem<TKey> debug) {
			if (debug.AbsractDb.DbSource != ServerDbs.Items2)
				return null;

			if (debug.AbsractDb.Attached.ContainsKey("Phantom." + debug.DbSource.Filename)) {
				return (TkDictionary<TKey, string[]>)debug.AbsractDb.Attached["Phantom." + debug.DbSource.Filename];
			}

			return null;
		}

		public static void DbItemsNouse<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				IntLineStream lines = new IntLineStream(debug.OldPath);
				lines.Remove(db);
				string line;

				TkDictionary<TKey, string[]> phantom = _getPhantomTable(debug);

				if (phantom != null) {
					var itemDb = debug.AbsractDb.GetDb<TKey>(ServerDbs.Items).Table;

					// Check if the phantom values differ from the Items1
					foreach (var tuple in phantom) {
						if (debug.AbsractDb.Table.ContainsKey(tuple.Key))
							continue;

						var key = tuple.Key;
						var elements = tuple.Value;
						var tuple1 = itemDb.TryGetTuple(key);

						if (tuple1 != null) {
							int val1 = tuple1.GetIntNoThrow(ServerItemAttributes.NoUseFlag);
							int val2 = FormatConverters.IntOrHexConverter(elements[1]);

							int val3 = tuple1.GetIntNoThrow(ServerItemAttributes.NoUseOverride);
							int val4 = FormatConverters.IntOrHexConverter(elements[2]);

							// There is no flag set
							if (val1 != val2 || val3 != val4) {
								lines.Delete(tuple1.GetKey<int>());
							}
						}
					}
				}

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					string overrideValue = tuple.GetValue<string>(ServerItemAttributes.NoUseOverride);
					string flagValue = tuple.GetValue<string>(ServerItemAttributes.NoUseFlag);

					if (flagValue == "0") {
						if (overrideValue == "100" || SdeAppConfiguration.DbNouseIgnoreOverride) {
							lines.Delete(key);
							continue;
						}
					}

					line = String.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), flagValue, overrideValue }.ToArray());

					if (SdeAppConfiguration.AddCommentForItemNoUse) {
						line += "\t// " + tuple.GetValue<string>(ServerItemAttributes.AegisName);
					}

					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbItemsTrade<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				IntLineStream lines = new IntLineStream(debug.OldPath);
				lines.Remove(db);
				string line;

				TkDictionary<TKey, string[]> phantom = _getPhantomTable(debug);

				if (phantom != null) {
					var itemDb = debug.AbsractDb.GetDb<TKey>(ServerDbs.Items).Table;

					// Check if the phantom values differ from the Items1
					foreach (var tuple in phantom) {
						if (debug.AbsractDb.Table.ContainsKey(tuple.Key))
							continue;

						var key = tuple.Key;
						var elements = tuple.Value;
						var tuple1 = itemDb.TryGetTuple(key);

						if (tuple1 != null) {
							int val1 = tuple1.GetIntNoThrow(ServerItemAttributes.TradeFlag);
							int val2 = FormatConverters.IntOrHexConverter(elements[1]);

							int val3 = tuple1.GetIntNoThrow(ServerItemAttributes.TradeOverride);
							int val4 = FormatConverters.IntOrHexConverter(elements[2]);

							// There is no flag set
							if (val1 != val2 || val3 != val4) {
								lines.Delete(tuple1.GetKey<int>());
							}
						}
					}
				}

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					string overrideValue = tuple.GetValue<string>(ServerItemAttributes.TradeOverride);
					string flagValue = tuple.GetValue<string>(ServerItemAttributes.TradeFlag);

					if (flagValue == "0") {
						if (overrideValue == "100" || SdeAppConfiguration.DbTradeIgnoreOverride) {
							lines.Delete(key);
							continue;
						}
					}

					line = String.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), flagValue, overrideValue }.ToArray());

					if (SdeAppConfiguration.AddCommentForItemTrade) {
						line += "\t// " + tuple.GetValue<string>(ServerItemAttributes.AegisName);
					}

					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbItemsCommaRange<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, int from, int length, string defaultValue, Func<ReadableTuple<TKey>, List<string>, string, string> append) {
			try {
				IntLineStream lines = new IntLineStream(debug.OldPath);
				lines.Remove(db);
				string line;

				TkDictionary<TKey, string[]> phantom = _getPhantomTable(debug);

				if (phantom != null) {
					var itemDb = debug.AbsractDb.GetDb<TKey>(ServerDbs.Items).Table;

					// Check if the phantom values differ from the Items1
					foreach (var tuple in phantom) {
						if (debug.AbsractDb.Table.ContainsKey(tuple.Key))
							continue;

						var key = tuple.Key;
						var elements = tuple.Value;
						var tuple1 = itemDb.TryGetTuple(key);

						if (tuple1 != null) {
							string val1 = tuple1.GetValue<string>(@from);
							string val2 = elements[1];

							if (val1 != val2) {
								lines.Delete(tuple1.GetKey<int>());
							}
						}
					}
				}

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					List<string> items = tuple.GetRawElements().Skip(@from).Take(length).Select(p => p.ToString()).ToList();

					if (items.All(p => p == defaultValue)) {
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
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbItemsStack<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				IntLineStream lines = new IntLineStream(debug.OldPath);
				lines.Remove(db);
				string line;

				TkDictionary<TKey, string[]> phantom = _getPhantomTable(debug);

				if (phantom != null) {
					var itemDb = debug.AbsractDb.GetDb<TKey>(ServerDbs.Items).Table;

					// Check if the phantom values differ from the Items1
					foreach (var tuple in phantom) {
						if (debug.AbsractDb.Table.ContainsKey(tuple.Key))
							continue;

						var key = tuple.Key;
						var elements = tuple.Value;
						var tuple1 = itemDb.TryGetTuple(key);

						if (tuple1 != null) {
							string val1 = tuple1.GetValue<string>(ServerItemAttributes.Stack);
							string val2 = elements[1];

							if (val1 != val2) {
								lines.Delete(tuple1.GetKey<int>());
							}
						}
					}
				}

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					string item1 = tuple.GetValue<string>(ServerItemAttributes.Stack);

					if (item1 == "") {
						lines.Delete(key);
						continue;
					}

					line = String.Join(",", new string[] { key.ToString(CultureInfo.InvariantCulture), item1 }.ToArray());
					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbItemsBuyingStore<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				IntLineStream lines = new IntLineStream(debug.OldPath);
				lines.Remove(db);
				string line;

				TkDictionary<TKey, string[]> phantom = _getPhantomTable(debug);

				if (phantom != null) {
					var itemDb = debug.AbsractDb.GetDb<TKey>(ServerDbs.Items).Table;

					// Check if the phantom values differ from the Items1
					foreach (var tuple in phantom) {
						if (debug.AbsractDb.Table.ContainsKey(tuple.Key))
							continue;

						var key = tuple.Key;
						var tuple1 = itemDb.TryGetTuple(key);

						if (tuple1 != null) {
							bool val1 = tuple1.GetValue<bool>(ServerItemAttributes.BuyingStore);

							if (val1 != true) {
								lines.Delete(tuple1.GetKey<int>());
							}
						}
					}
				}

				foreach (ReadableTuple<TKey> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetKey<TKey>())) {
					int key = tuple.GetKey<int>();

					bool item1 = tuple.GetValue<bool>(ServerItemAttributes.BuyingStore);

					if (!item1) {
						lines.Delete(key);
						continue;
					}

					line = key.ToString(CultureInfo.InvariantCulture) + "  // " + tuple.GetValue<string>(ServerItemAttributes.AegisName);
					lines.Write(key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void WriteEntry(BaseDb db, StringBuilder builder, ReadableTuple<int> tuple) {
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

			if (tradeOverride != 100 || tradeFlag != 0) {
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

			if (nouseOverride != 100 || nouseFlag != 0) {
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
	}
}