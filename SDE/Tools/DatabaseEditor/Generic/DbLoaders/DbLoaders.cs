using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Database;
using SDE.Tools.DatabaseEditor.Engines.Parsers;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using Utilities;
using Utilities.CommandLine;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders {
	public static partial class DbLoaders {
		#region Delegates

		public delegate void DbCommaFunctionDelegate<T>(DbDebugItem<T> debug, AttributeList list, string[] elements, Table<T, ReadableTuple<T>> table);

		#endregion

		public static void DbItemsLoader<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			if (debug.FileType == FileType.Txt) {
				DbLoaderAny(debug, db, TextFileHelper.GetElementsByCommas);
			}
			else if (debug.FileType == FileType.Conf) {
				DbCommaLoader(debug, ServerItemAttributes.AttributeList, DbItemsFunction, TextFileHelper.GetElementsByBrackets);
#if SDE_DEBUG
				CLHelper.WA = ", internal parser : " + CLHelper.CD(-3);
#endif
			}
		}

		public static void DbCommaRange<T>(DbDebugItem<T> debug, AttributeList list, int indexStart, int length) {
			var table = debug.AbsractDb.Table;

			foreach (string[] elements in TextFileHelper.GetElementsByCommas(File.ReadAllBytes(debug.FilePath))) {
				try {
					T itemId = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]);
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

		public static void DbCommaLoader<T>(DbDebugItem<T> debug, AttributeList list, DbCommaFunctionDelegate<T> function) {
			DbCommaLoader(debug, list, function, TextFileHelper.GetElementsByCommas);
		}

		public static void DbCommaLoader<T>(DbDebugItem<T> debug, AttributeList list, DbCommaFunctionDelegate<T> function, TextFileHelper.TextFileHelperGetterDelegate getter) {
			var table = debug.AbsractDb.Table;

#if SDE_DEBUG
			Z.StopAndRemoveWithoutDisplay(-1);
			Z.StopAndRemoveWithoutDisplay(-2);
			CLHelper.CR(-2);
#endif
			foreach (string[] elements in getter(File.ReadAllBytes(debug.FilePath))) {
#if SDE_DEBUG
				CLHelper.CS(-2);
				CLHelper.CR(-1);
#endif
				try {
					function(debug, list, elements, table);
				}
				catch (Exception err) {
					if (!debug.ReportException(err)) return;
				}
#if SDE_DEBUG
				CLHelper.CS(-1);
				CLHelper.CR(-2);
#endif
			}
#if SDE_DEBUG
			CLHelper.CS(-2);
			CLHelper.WA = ", method core : " + CLHelper.CD(-1) + "ms, loop getter : " + CLHelper.CD(-2) + "ms";
#endif
		}

		public static void DbItemsNouseFunction<T>(DbDebugItem<T> debug, AttributeList list, string[] elements, Table<T, ReadableTuple<T>> table) {
			T itemId = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]);

			Nouse nouse = new Nouse();
			nouse.Sitting = elements[1] == "1" ? "true" : "false";
			nouse.Override = elements[2];

			table.SetRaw(itemId, ServerItemAttributes.Nouse, nouse);
		}

		public static void DbItemsFunction<TKey>(DbDebugItem<TKey> debug, AttributeList list, string[] elements, Table<TKey, ReadableTuple<TKey>> table) {
#if SDE_DEBUG
				CLHelper.CR(-3);
#endif
			ItemParser itemHelper = new ItemParser(elements[0]);
#if SDE_DEBUG
			CLHelper.CS(-3);
#endif
			TKey itemId = (TKey) (object) Int32.Parse(itemHelper.Id);
			ReadableTuple<TKey> tuple = new ReadableTuple<TKey>(itemId, ServerItemAttributes.AttributeList);
			table.Add(itemId, tuple);

			tuple.SetRawValue(ServerItemAttributes.AegisName, itemHelper.AegisName);
			tuple.SetRawValue(ServerItemAttributes.Name, itemHelper.Name);
			tuple.SetRawValue(ServerItemAttributes.Type, itemHelper.Type);
			tuple.SetRawValue(ServerItemAttributes.Buy, itemHelper.Buy);
			tuple.SetRawValue(ServerItemAttributes.Sell, itemHelper.Sell);
			tuple.SetRawValue(ServerItemAttributes.Weight, itemHelper.Weight);
			tuple.SetRawValue(ServerItemAttributes.Attack, itemHelper.Atk);
			tuple.SetRawValue(ServerItemAttributes.Defense, itemHelper.Def);
			tuple.SetRawValue(ServerItemAttributes.Range, itemHelper.Range);
			tuple.SetRawValue(ServerItemAttributes.NumberOfSlots, itemHelper.Slots);
			tuple.SetRawValue(ServerItemAttributes.ApplicableJob, itemHelper.Job);
			tuple.SetRawValue(ServerItemAttributes.Upper, itemHelper.Upper);
			tuple.SetRawValue(ServerItemAttributes.Gender, itemHelper.Gender);
			tuple.SetRawValue(ServerItemAttributes.Location, itemHelper.Loc);
			tuple.SetRawValue(ServerItemAttributes.WeaponLevel, itemHelper.WeaponLv);
			tuple.SetRawValue(ServerItemAttributes.EquipLevel, itemHelper.EquipLv);
			tuple.SetRawValue(ServerItemAttributes.Refineable, itemHelper.Refineable);
			tuple.SetRawValue(ServerItemAttributes.ClassNumber, itemHelper.View);
			tuple.SetRawValue(ServerItemAttributes.Script, itemHelper.Script);
			tuple.SetRawValue(ServerItemAttributes.OnEquipScript, itemHelper.OnEquipScript);
			tuple.SetRawValue(ServerItemAttributes.OnUnequipScript, itemHelper.OnUnequipScript);

			tuple.SetRawValue(ServerItemAttributes.Matk, itemHelper.Matk);
			tuple.SetRawValue(ServerItemAttributes.BindOnEquip, itemHelper.BindOnEquip);
			tuple.SetRawValue(ServerItemAttributes.BuyingStore, itemHelper.BuyingStore);
			tuple.SetRawValue(ServerItemAttributes.Delay, itemHelper.Delay);
			tuple.SetRawValue(ServerItemAttributes.Stack, itemHelper.Stack);
			tuple.SetRawValue(ServerItemAttributes.Sprite, itemHelper.Sprite);
			tuple.SetRawValue(ServerItemAttributes.Trade, itemHelper.Trade);
			tuple.SetRawValue(ServerItemAttributes.Nouse, itemHelper.Nouse);
		}
		public static void DbItemsTradeFunction<T>(DbDebugItem<T> debug, AttributeList list, string[] elements, Table<T, ReadableTuple<T>> table) {
			T itemId = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]);

			Trade trade = new Trade();
			trade.Set(elements[1], elements[2]);
			table.SetRaw(itemId, ServerItemAttributes.Trade, trade);
		}
		public static void DbItemsBuyingStoreFunction<T>(DbDebugItem<T> debug, AttributeList list, string[] elements, Table<T, ReadableTuple<T>> table) {
			T itemId = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]);
			table.SetRaw(itemId, ServerItemAttributes.BuyingStore, "true");
		}
		public static void DbItemsStackFunction<T>(DbDebugItem<T> debug, AttributeList list, string[] elements, Table<T, ReadableTuple<T>> table) {
			T itemId = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]);
			table.SetRaw(itemId, ServerItemAttributes.Stack, "[" + elements[1] + "," + elements[2] + "]");
		}
		public static void DbCommaNoCast<T>(DbDebugItem<T> debug, AttributeList list, int indexStart, int length) {
			var table = debug.AbsractDb.Table;

			foreach (string[] elements in TextFileHelper.GetElementsByCommas(File.ReadAllBytes(debug.FilePath))) {
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

		public static void DbLoaderAny<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db, TextFileHelper.TextFileHelperGetterDelegate getter, bool uniqueKey = true) {
			List<DbAttribute> attributes = new List<DbAttribute>(db.AttributeList.Attributes);
			int indexOffset = uniqueKey ? 1 : 0;
			int attributesOffset = uniqueKey ? 0 : 1;

			if (!uniqueKey) {
				TextFileHelper.SaveLastLine = true;
			}

#if SDE_DEBUG
			Z.StopAndRemoveWithoutDisplay(-1);
			Z.StopAndRemoveWithoutDisplay(-2);
			CLHelper.CR(-2);
#endif
			foreach (string[] elements in getter(File.ReadAllBytes(debug.FilePath))) {
#if SDE_DEBUG
				CLHelper.CS(-2);
				CLHelper.CR(-1);
#endif
				try {
					_guessAttributes(elements, attributes, -1, db);

					TKey id;

					if (uniqueKey) {
						id = (TKey) TypeDescriptor.GetConverter(typeof (TKey)).ConvertFrom(elements[0]);
					}
					else {
						id = (TKey)(object)TextFileHelper.LastLineRead;
					}

					for (int index = indexOffset; index < elements.Length; index++) {
						DbAttribute property = attributes[index + attributesOffset];
						db.Table.SetRaw(id, property, elements[index]);
					}
				}
				catch {
					if (!debug.ReportIdException(elements[0])) return;
				}
#if SDE_DEBUG
				CLHelper.CS(-1);
				CLHelper.CR(-2);
#endif
			}
#if SDE_DEBUG
			CLHelper.CS(-2);
			CLHelper.WA = ", method core : " + CLHelper.CD(-1) + "ms, loop getter : " + CLHelper.CD(-2) + "ms";
#endif
			if (!uniqueKey) {
				TextFileHelper.SaveLastLine = false;
			}
		}

		public static void DbTabsLoader<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			DbLoaderAny(debug, db, TextFileHelper.GetElementsByTabs);
		}
		public static void DbCommaLoader<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			DbLoaderAny(debug, db, TextFileHelper.GetElementsByCommas);
		}
		public static void DbUniqueLoader<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			DbLoaderAny(debug, db, TextFileHelper.GetElementsByCommas, false);
		}

		private static void _guessAttributes(ICollection<string> elements, ICollection<DbAttribute> attributes, int numberOfAttributes, BaseDb db) {
			if (db.Attached["Scanned"] == null || (db.Attached["FromUserRawInput"] != null && (bool) db.Attached["FromUserRawInput"])) {
				if (attributes.Any(p => p.IsSkippable)) {
					attributes.Where(p => p.IsSkippable).ToList().ForEach(p => db.Attached[p.ToString()] = true);

					if (numberOfAttributes < 0) {
						// We have to detect how many attributes there are
						if (db.Attached["NumberOfAttributesToGuess"] != null) {
							numberOfAttributes = (int) db.Attached["NumberOfAttributesToGuess"];
						}
						else {
							numberOfAttributes = attributes.Count(p => p.Visibility == VisibleState.Visible);
						}
					}

					while (elements.Count < numberOfAttributes && attributes.Any(p => p.IsSkippable)) {
						var attribute = attributes.First(p => p.IsSkippable);
						attributes.Remove(attribute);

						if (db.Attached["FromUserRawInput"] == null || !((bool) db.Attached["FromUserRawInput"])) {
							db.Attached[attribute.DisplayName] = false;
						}
					}
				}

				db.Attached["Scanned"] = true;
				db.Attached["FromUserRawInput"] = false;
			}
		}
	}
}