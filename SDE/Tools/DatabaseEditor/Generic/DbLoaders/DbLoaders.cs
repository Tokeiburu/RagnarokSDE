using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Database;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Engines.Parsers;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using Utilities;

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

			foreach (string[] elements in getter(File.ReadAllBytes(debug.FilePath))) {
				try {
					function(debug, list, elements, table);
				}
				catch (Exception err) {
					if (!debug.ReportException(err)) return;
				}
			}
		}

		public static void DbItemsNouseFunction<T>(DbDebugItem<T> debug, AttributeList list, string[] elements, Table<T, ReadableTuple<T>> table) {
			T itemId = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]);

			Nouse nouse = new Nouse();
			nouse.Sitting = elements[1] == "1" ? "true" : "false";
			nouse.Override = elements[2];

			table.SetRaw(itemId, ServerItemAttributes.Nouse, nouse);
		}

		public static void DbItemsFunction<TKey>(DbDebugItem<TKey> debug, AttributeList list, string[] elements, Table<TKey, ReadableTuple<TKey>> table) {
			ItemParser itemHelper = new ItemParser(elements[0]);
			TKey itemId = (TKey)TypeDescriptor.GetConverter(typeof(TKey)).ConvertFrom(itemHelper.Id);

			table.SetRaw(itemId, ServerItemAttributes.AegisName, itemHelper.AegisName);
			table.SetRaw(itemId, ServerItemAttributes.Name, itemHelper.Name);
			table.SetRaw(itemId, ServerItemAttributes.Type, itemHelper.Type);
			table.SetRaw(itemId, ServerItemAttributes.Buy, itemHelper.Buy);
			table.SetRaw(itemId, ServerItemAttributes.Sell, itemHelper.Sell);
			table.SetRaw(itemId, ServerItemAttributes.Weight, itemHelper.Weight);
			table.SetRaw(itemId, ServerItemAttributes.Attack, itemHelper.Atk);
			table.SetRaw(itemId, ServerItemAttributes.Defense, itemHelper.Def);
			table.SetRaw(itemId, ServerItemAttributes.Range, itemHelper.Range);
			table.SetRaw(itemId, ServerItemAttributes.NumberOfSlots, itemHelper.Slots);
			table.SetRaw(itemId, ServerItemAttributes.ApplicableJob, itemHelper.Job);
			table.SetRaw(itemId, ServerItemAttributes.Upper, itemHelper.Upper);
			table.SetRaw(itemId, ServerItemAttributes.Gender, itemHelper.Gender);
			table.SetRaw(itemId, ServerItemAttributes.Location, itemHelper.Loc);
			table.SetRaw(itemId, ServerItemAttributes.WeaponLevel, itemHelper.WeaponLv);
			table.SetRaw(itemId, ServerItemAttributes.EquipLevel, itemHelper.EquipLv);
			table.SetRaw(itemId, ServerItemAttributes.Refineable, itemHelper.Refineable);
			table.SetRaw(itemId, ServerItemAttributes.ClassNumber, itemHelper.View);
			table.SetRaw(itemId, ServerItemAttributes.Script, itemHelper.Script);
			table.SetRaw(itemId, ServerItemAttributes.OnEquipScript, itemHelper.OnEquipScript);
			table.SetRaw(itemId, ServerItemAttributes.OnUnequipScript, itemHelper.OnUnequipScript);
			
			table.SetRaw(itemId, ServerItemAttributes.Matk, itemHelper.Matk);
			table.SetRaw(itemId, ServerItemAttributes.BindOnEquip, itemHelper.BindOnEquip);
			table.SetRaw(itemId, ServerItemAttributes.BuyingStore, itemHelper.BuyingStore);
			table.SetRaw(itemId, ServerItemAttributes.Delay, itemHelper.Delay);
			table.SetRaw(itemId, ServerItemAttributes.Stack, itemHelper.Stack);
			table.SetRaw(itemId, ServerItemAttributes.Sprite, itemHelper.Sprite);
			table.SetRaw(itemId, ServerItemAttributes.Trade, itemHelper.Trade);
			table.SetRaw(itemId, ServerItemAttributes.Nouse, itemHelper.Nouse);
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

			foreach (string[] elements in getter(File.ReadAllBytes(debug.FilePath))) {
				try {
					_guessAttributes(elements, attributes, -1, db);

					TKey id;

					if (uniqueKey) {
						id = (TKey) TypeDescriptor.GetConverter(typeof (TKey)).ConvertFrom(elements[0]);
					}
					else {
						id = (TKey)(object)Methods.RandomString(128);
					}

					for (int index = indexOffset; index < elements.Length; index++) {
						DbAttribute property = attributes[index + attributesOffset];
						db.Table.SetRaw(id, property, elements[index]);
					}
				}
				catch {
					if (!debug.ReportIdException(elements[0])) return;
				}
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