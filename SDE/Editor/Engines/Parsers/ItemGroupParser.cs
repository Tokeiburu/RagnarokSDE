using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ErrorManager;
using SDE.ApplicationConfiguration;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers;
using SDE.Editor.Generic.Parsers.Generic;
using Utilities;
using Utilities.Extension;

namespace SDE.Editor.Engines.Parsers {
	/// <summary>
	/// Reads an ItemGroup from a raw input (from the TextFileHelper).
	/// </summary>
	public class ItemGroupParser {
		public string Id;
		public List<Tuple<string, string>> Quantities = new List<Tuple<string, string>>();

		public bool Init<TKey>(DbDebugItem<TKey> debug, string[] elements) {
			Id = null;
			Quantities.Clear();

			List<string> lines = elements.ToList(); //.Split(new char[] { TextFileHelper.SplitCharacter }, StringSplitOptions.RemoveEmptyEntries).ToList();

			if (lines.Count > 0)
				Id = lines[0];

			for (int index = 1; index < lines.Count; index++) {
				string line = lines[index];
				int lineIndex = 0;

				while (lineIndex < line.Length) {
					if (line[lineIndex] == '(') {
						int end = line.IndexOf(')', lineIndex + 1);

						if (end < 0) {
							return debug.ReportIdExceptionWithError("Couln't find the parenthesis end branch.", Id);
						}

						string actualVal = line.Substring(lineIndex + 1, end - lineIndex - 1);
						_add(actualVal);
						lineIndex = end;
					}
					else if (line[lineIndex] == ' ' || line[lineIndex] == ',') {
					}
					else if (line[lineIndex] == '\"') {
						int end = line.IndexOf('\"', lineIndex + 1);

						if (end < 0) {
							return debug.ReportIdExceptionWithError("Couln't find the parenthesis end branch.", Id);
						}

						string actualVal = line.Substring(lineIndex + 1, end - lineIndex - 1);
						_add(actualVal);
						lineIndex = end;
					}
					lineIndex++;
				}
			}

			return true;
		}

		private void _add(string val) {
			string[] values = val.Split(',');

			if (values.Length > 1) {
				Quantities.Add(new Tuple<string, string>(values[0].Trim('\"'), values[1]));
			}
			else {
				Quantities.Add(new Tuple<string, string>(values[0].Trim('\"'), "1"));
			}
		}

		/// <summary>
		/// Converts a group from an ItemGroup to a Hercules entry
		/// </summary>
		/// <param name="gdb">The base db.</param>
		/// <param name="groupId">The group id.</param>
		/// <param name="aegisNames">The aegis names.</param>
		/// <param name="names">The names.</param>
		/// <returns>A string for an ItemGroup entry converted to Hercules's format</returns>
		public static string ToHerculesDbEntry(BaseDb gdb, int groupId, List<string> aegisNames, List<string> names) {
			StringBuilder builder = new StringBuilder();
			var dbItems = gdb.GetMeta<int>(ServerDbs.Items);
			var dbGroups = gdb.Get<int>(ServerDbs.ItemGroups);

			if (groupId < 500) {
				var dbConstants = gdb.Get<string>(ServerDbs.Constants);
				string sId = groupId.ToString(CultureInfo.InvariantCulture);
				// The current db is from rAthena

				var tuple = dbConstants.FastItems.FirstOrDefault(p => p.GetValue<string>(1) == sId && p.GetKey<string>().StartsWith("IG_"));
				string constant = null;

				if (tuple != null) {
					constant = tuple.GetKey<string>().Substring(3);
				}
				else {
					var res = DbIOItemGroups.Constants.Where(p => p.Value == groupId).ToList();

					if (res.Count > 0) {
						constant = res[0].Key;
					}
				}

				if (constant != null) {
					string originalConstantValue = constant;
					ReadableTuple<int> tupleItem;

					// Attempts to retrieve the item based on the script
					tupleItem = dbItems.FastItems.FirstOrDefault(p => p.GetValue<string>(ServerItemAttributes.Script).IndexOf("getrandgroupitem(IG_" + originalConstantValue + ")", StringComparison.OrdinalIgnoreCase) > -1);

					if (tupleItem == null) {
						// Attempts to retrieve the item based on a formatted constant name (with underscore)

						StringBuilder temp = new StringBuilder();
						temp.Append(constant[0]);

						for (int i = 1; i < constant.Length; i++) {
							if (constant[i] == '_') {
								i++;

								if (i < constant.Length)
									temp.Append(constant[i]);
							}
							else if (char.IsUpper(constant[i])) {
								temp.Append('_');
								temp.Append(constant[i]);
							}
							else {
								temp.Append(constant[i]);
							}
						}

						constant = temp.ToString();

						// Attempts to retrieve the item with the Old prefix
						string oldConstant = "Old_" + constant;

						// Attempts to retrieve the item without the Old prefix
						tupleItem = dbItems.FastItems.FirstOrDefault(p => p.GetStringValue(ServerItemAttributes.AegisName.Index) == oldConstant);

						if (tupleItem == null) {
							tupleItem = dbItems.FastItems.FirstOrDefault(p => p.GetStringValue(ServerItemAttributes.AegisName.Index) == constant);
						}
					}

					// Retrieve the closest item based on the names in the ItemDb.
					// It uses the Levenshtein distance algorithm to find the clostest match.
					// This method 'always' returns a value, but a warning is prompted to the user in the error console.
					if (tupleItem == null) {
						List<string> values1 = aegisNames;
						List<string> values2 = names;

						string closestMatch1 = Methods.ClosestString(originalConstantValue, values1);
						string closestMatch2 = Methods.ClosestString(originalConstantValue, values2);

						int lev1 = Methods.LevenshteinDistance(originalConstantValue, closestMatch1);
						int lev2 = Methods.LevenshteinDistance(originalConstantValue, closestMatch2);

						tupleItem = dbItems.FastItems[lev1 < lev2 ? values1.IndexOf(closestMatch1) : values2.IndexOf(closestMatch2)];

						string closestMatch = tupleItem.GetValue<string>(ServerItemAttributes.AegisName);

						if (Math.Min(lev1, lev2) != 0 && closestMatch.Replace("_", "") != constant) {
							DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "A suspicious conversion occurred for the item [" + originalConstantValue + "]. The group item name is [" + tupleItem.GetValue<string>(ServerItemAttributes.AegisName) + "].");
						}
					}

					builder.AppendLine(tupleItem.GetValue<string>(ServerItemAttributes.AegisName) + ": (");

					Dictionary<int, ReadableTuple<int>> table = (Dictionary<int, ReadableTuple<int>>)dbGroups.GetTuple(groupId).GetRawValue(ServerItemGroupAttributes.Table.Index);

					foreach (var pair in table) {
						tupleItem = dbItems.TryGetTuple(pair.Key);
						string name = tupleItem == null ? "ID" + pair.Key : tupleItem.GetValue<string>(ServerItemAttributes.AegisName);

						if (pair.Value.GetValue<string>(ServerItemGroupSubAttributes.Rate) == "1") {
							builder.Append("\t\"");
							builder.Append(name);
							builder.AppendLine("\",");
						}
						else {
							builder.Append("\t(\"");
							builder.Append(name);
							builder.Append("\",");
							builder.Append(pair.Value.GetValue<string>(ServerItemGroupSubAttributes.Rate));
							builder.AppendLine("),");
						}
					}

					builder.Append(")");
				}
				else {
					DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "Failed to find the constant name with the id [" + sId + "].");
				}
			}
			else {
				// The current db is from Hercules
				var tuple = dbItems.TryGetTuple(groupId);

				if (tuple != null) {
					// Check if the item is a package or a group!
					Dictionary<int, ReadableTuple<int>> table = (Dictionary<int, ReadableTuple<int>>)dbGroups.GetTuple(groupId).GetRawValue(ServerItemGroupAttributes.Table.Index);

					if (table.Values.Any(p => p.GetRawElements().Skip(3).Take(6).Count(q => (q ?? "").ToString() == "") > 0)) {
						// This is a package
					}
					else {
						// This is an item_group
						builder.AppendLine(tuple.GetValue<string>(ServerItemAttributes.AegisName) + ": (");

						foreach (var pair in table) {
							tuple = dbItems.TryGetTuple(pair.Key);
							string name = tuple == null ? "ID" + pair.Key : tuple.GetValue<string>(ServerItemAttributes.AegisName);

							if (pair.Value.GetValue<string>(ServerItemGroupSubAttributes.Rate) == "1") {
								builder.Append("\t\"");
								builder.Append(name);
								builder.AppendLine("\",");
							}
							else {
								builder.Append("\t(\"");
								builder.Append(name);
								builder.Append("\",");
								builder.Append(pair.Value.GetValue<string>(ServerItemGroupSubAttributes.Rate));
								builder.AppendLine("),");
							}
						}

						builder.Append(")");
					}
				}
				else {
					DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "Failed to retrieve the item ID associated with the group ID.", groupId.ToString(CultureInfo.InvariantCulture), ErrorLevel.Critical);
				}
			}

			return builder.ToString();
		}
	}
}