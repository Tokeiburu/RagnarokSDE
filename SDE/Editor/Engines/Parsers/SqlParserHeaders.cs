using System;
using System.Globalization;
using System.Text;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;

namespace SDE.Editor.Engines.Parsers {
	public partial class SqlParser {
		private static string _addQuotesIfNotNull(string item) {
			if (item == "NULL" || item == "")
				return "NULL";
			if (item.Length >= 2 && item[0] == '\'' && item[item.Length - 1] == '\'')
				return item;
			return "'" + item + "'";
		}

		private static string _parse(string item) {
			return item.Replace("'", @"\'").Replace("\\\"", "\\\\\\\"").Trim(' ');
		}

		private static string _parseHerc(string item, bool trim = true) {
			StringBuilder builder = new StringBuilder();

			char c;
			for (int i = 0; i < item.Length; i++) {
				c = item[i];
				switch(c) {
					case '\'':
						builder.Append(@"\'");
						break;
					case '\\':
						builder.Append(@"\\");
						break;
					case '\"':
						builder.Append("\\\"");
						break;
					default:
						builder.Append(c);
						break;
				}
			}

			if (trim)
				return builder.ToString().Trim(' ');
			return builder.ToString();
		}

		private static string _parseAndSetToInteger(object obj) {
			string item = obj.ToString();
			var res = _parseHerc(item);

			if (res.StartsWith("0x") || res.StartsWith("0X")) {
				try {
					int ival = Convert.ToInt32(res, 16);

					if (ival >= -1)
						return ((uint)ival).ToString(CultureInfo.InvariantCulture);

					// Removes the first 0xF byte)
					return ((uint)(ival + 0x80000000)).ToString(CultureInfo.InvariantCulture);
				}
				catch {
					return res;
				}
			}

			return res;
		}

		private static string _parseEquip(string item, bool isMin) {
			string[] items = item.Split(',');

			if (items.Length == 1 && !isMin) {
				return "NULL";
			}

			return _parseAndSetToInteger(isMin ? items[0].TrimStart('[') : items[1].TrimEnd(']'));
		}

		private static string _defaultNull(string item) {
			if (string.IsNullOrEmpty(item))
				return "NULL";
			return item;
		}

		private static string _script(string item) {
			string val = ValueConverters.GetScriptNoBracketsSetScriptWithBrackets.ConvertFrom<string>(null, item).Trim(' ');
			if (string.IsNullOrEmpty(val))
				return "NULL";
			return "'" + val + "'";
		}

		private static string _scriptNotNull(string item) {
			string val = ValueConverters.GetScriptNoBracketsSetScriptWithBrackets.ConvertFrom<string>(null, item).Trim(' ');
			return "'" + val + "'";
		}

		private static string _notNull(string item) {
			if (string.IsNullOrEmpty(item))
				return "";
			return item;
		}

		private static string _notNullDefault(string item, string @default) {
			if (string.IsNullOrEmpty(item))
				return @default;
			return item;
		}

		private static string _setOverride(string item) {
			return item == "100" ? "" : item;
		}

		private static string _buy<TKey>(string value, ReadableTuple<TKey> tuple) {
			if (value == "0") {
				string val = tuple.GetValue<string>(ServerItemAttributes.Sell);
				int ival;

				if (Int32.TryParse(val, out ival)) {
					return (2 * ival).ToString(CultureInfo.InvariantCulture);
				}

				return "0";
			}

			return value;
		}

		private static string _sell<TKey>(string value, ReadableTuple<TKey> tuple) {
			if (value == "0") {
				string val = tuple.GetValue<string>(ServerItemAttributes.Buy);
				int ival;

				if (Int32.TryParse(val, out ival)) {
					return (ival / 2).ToString(CultureInfo.InvariantCulture);
				}

				return "0";
			}

			return value;
		}

		private static string _stringOrInt(string value) {
			if (value == "")
				return value;

			int ival;
			if (Int32.TryParse(value, out ival) || value.StartsWith("0x") || value.StartsWith("0X")) {
				return value;
			}

			return "'" + value + "'";
		}

		private static string _typeHerculesConvert(string value) {
			if (value == "4")
				return "5";
			if (value == "5")
				return "4";
			return value;
		}
	}
}