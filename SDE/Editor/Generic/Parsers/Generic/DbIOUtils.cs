using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Database;
using ErrorManager;
using SDE.Core;
using SDE.Editor.Engines;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.Parsers.Libconfig;
using Utilities;

namespace SDE.Editor.Generic.Parsers.Generic {
	public static class DbIOUtils {

		public static string LoadFlag(ParserObject entry, Dictionary<string, long> flags, string def = "", bool setZeroToDef = true) {
			if (entry == null)
				return def;

			long flag = 0;

			if (entry is ParserString) {
				return flags[entry.ObjectValue.ToLower()].ToString(CultureInfo.InvariantCulture);
			}

			foreach (var flagEntry in entry.OfType<ParserKeyValue>()) {
				try {
					long val = flags[flagEntry.Key.ToLower()];

					if (flagEntry.Value == "true") {
						flag |= val;
					}
					else {
						flag &= ~val;
					}
				}
				catch {
					throw new FileParserException(TextFileHelper.LatestFile, entry.Line, "Unknown flag: " + flagEntry.Key);
				}
			}

			if (setZeroToDef && flag == 0 && def != "")
				return def;

			return flag.ToString(CultureInfo.InvariantCulture);
		}

		public static string LoadFlag<T>(ParserObject entry, string def = "") {
			if (entry == null)
				return def;

			long flag = 0;
			var flagData = FlagsManager.GetFlag<T>();

			if (flagData == null) {
				throw new FileParserException(TextFileHelper.LatestFile, entry.Line, "Unknown flag provided: " + typeof(T));
			}

			if (entry is ParserString) {
				long val;

				if (!flagData.Name2Value.TryGetValue(entry.ObjectValue, out val)) {
					FlagsManager.AddValue(flagData, entry.ObjectValue);
				}

				if (!flagData.Name2Value.TryGetValue(entry.ObjectValue, out val)) {
					throw new FileParserException(TextFileHelper.LatestFile, entry.Line, "Unknown flag: " + entry.ObjectValue);
				}

				return flagData.Name2Value[entry.ObjectValue].ToString(CultureInfo.InvariantCulture);
			}

			foreach (var flagEntry in entry.OfType<ParserKeyValue>()) {
				try {
					long val = 0;

					if (!flagData.Name2Value.TryGetValue(flagEntry.Key, out val)) {
						FlagsManager.AddValue(flagData, flagEntry.Key);
					}

					if (!flagData.Name2Value.TryGetValue(flagEntry.Key, out val)) {
						throw new FileParserException(TextFileHelper.LatestFile, entry.Line, "Unknown flag: " + flagEntry.Key);
					}

					if (flagEntry.Value == "true") {
						flag |= val;
					}
					else {
						flag &= ~val;
					}
				}
				catch {
					throw new FileParserException(TextFileHelper.LatestFile, entry.Line, "Unknown flag: " + flagEntry.Key);
				}
			}

			if (flag == 0 && def != "")
				return def;

			return flag.ToString(CultureInfo.InvariantCulture);
		}

		public static string QuoteCheck(string value) {
			bool addQuotes = false;

			for (int i = 0; i < value.Length; i++) {
				if (i == 0) {
					switch(value[i]) {
						case '?':
						case '[':
						case ']':
						case '{':
						case '}':
						case ':':
							addQuotes = true;
							break;
					}
				}
				else {
					switch (value[i]) {
						case ':':
							addQuotes = true;
							break;
					}
				}

				if (addQuotes)
					break;
			}

			if (addQuotes)
				return "\"" + value + "\"";
			
			return value;
		}

		public static void ExpandFlag<T>(StringBuilder builder, ReadableTuple<int> tuple, string name, DbAttribute attribute, string indent1, string indent2, Func<bool> isExtra = null, Action extra = null) {
			long value = FormatConverters.LongOrHexConverter(tuple.GetValue<string>(attribute));

			if (value != 0 || (isExtra != null && isExtra())) {
				if (name != "") {
					builder.Append(indent1);
					builder.Append(name);
					builder.AppendLine(":");
				}

				var flagsData = FlagsManager.GetFlag<T>();

				if (flagsData != null) {
					foreach (var v in flagsData.Values) {
						long vF = v.Value;

						if ((v.DataFlag & FlagDataProperty.Hide) == FlagDataProperty.Hide)
							continue;

						if ((vF & value) == vF) {
							builder.Append(indent2);
							builder.Append(v.Name);
							builder.AppendLine(": true");
						}
					}
				}
				else {
					foreach (var v in Enum.GetValues(typeof(T)).Cast<T>()) {
						int vF = (int)(object)v;

						if ((vF & value) == vF) {
							builder.Append(indent2);
							builder.Append(Constants.ToString(v));
							builder.AppendLine(": true");
						}
					}
				}

				if (extra != null) {
					extra();
				}
			}
		}

		public static void ExpandFlagYaml<T>(StringBuilder builder, ReadableTuple<int> tuple, string name, DbAttribute attribute, string indent1, string indent2, Func<bool> isExtra = null, Action extra = null) {
			long value = FormatConverters.LongOrHexConverter(tuple.GetValue<string>(attribute));

			if (value != 0 || (isExtra != null && isExtra())) {
				if (name != "") {
					builder.Append(indent1);
					builder.Append(name);
					builder.AppendLine(": {");
				}

				var flagsData = FlagsManager.GetFlag<T>();

				if (flagsData != null) {
					foreach (var v in flagsData.Values) {
						long vF = v.Value;

						if ((v.DataFlag & FlagDataProperty.Hide) == FlagDataProperty.Hide)
							continue;

						if ((vF & value) == vF) {
							builder.Append(indent2);
							builder.Append(v.Name);
							builder.AppendLine(": true");
						}
					}
				}
				else {
					foreach (var v in Enum.GetValues(typeof(T)).Cast<T>()) {
						int vF = (int)(object)v;

						if ((vF & value) == vF) {
							builder.Append(indent2);
							builder.Append(Constants.ToString(v));
							builder.AppendLine(": true");
						}
					}
				}

				if (extra != null) {
					extra();
				}

				builder.Append(indent1);
				builder.AppendLine("}");
			}
		}

		private static Dictionary<string, Dictionary<string, int>> _bufferedDicos = new Dictionary<string, Dictionary<string, int>>();

		public static void ClearBuffer() {
			_bufferedDicos.Clear();
		}

		public static object Name2IdBuffered(IEnumerable<ReadableTuple<int>> table, DbAttribute attribute, string name, string table_sourc, bool silent) {
			if (string.IsNullOrEmpty(name))
				return 0;

			int res;

			if (Int32.TryParse(name, out res)) {
				return res;
			}

			if (!_bufferedDicos.ContainsKey(table_sourc)) {
				_bufferedDicos[table_sourc] = new Dictionary<string, int>();
				var dico = _bufferedDicos[table_sourc];

				foreach (var entry2 in table) {
					dico[entry2.GetStringValue(attribute.Index)] = entry2.Key;
				}
			}

			var metaDico = _bufferedDicos[table_sourc];
			int ival;

			if (metaDico.TryGetValue(name, out ival)) {
				return ival;
			}

			if (silent)
				return name;

			throw new Exception("Unable to convert the name '" + name + "' to an ID. This means the name is not defined in your " + table_sourc + " file and SDE requires it.");
		}

		public static object Name2Id(IEnumerable<ReadableTuple<int>> table, DbAttribute attribute, string name, string table_sourc, bool silent) {
			if (string.IsNullOrEmpty(name))
				return 0;

			int res;

			if (Int32.TryParse(name, out res)) {
				return res;
			}

			var entry = table.FirstOrDefault(p => p.GetValue<string>(attribute) == name);

			if (entry == null) {
				if (silent) {
					return name;
				}
				
				throw new Exception("Unable to convert the name '" + name + "' to an ID. This means the name is not defined in your " + table_sourc + " file and SDE requires it.");
			}

			return entry.GetKey<int>();
		}

		public static string Id2Name(Table<int, ReadableTuple<int>> table, DbAttribute attribute, string id) {
			if (string.IsNullOrEmpty(id))
				return "";

			int res;

			if (Int32.TryParse(id, out res)) {
				var item = table.TryGetTuple(res);

				if (item == null)
					return id;

				return item.GetValue<string>(attribute);
			}

			return id;
		}
	}
}
