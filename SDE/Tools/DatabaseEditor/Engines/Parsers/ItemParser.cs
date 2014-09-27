using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Database;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Generic;
using SDE.Tools.DatabaseEditor.Generic.Core;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using Utilities;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Engines.Parsers {
	/// <summary>
	/// Parser for Hercules's item entries
	/// </summary>
	public class ItemParser {
		public string AegisName = "";
		public string Atk = "";
		public string BindOnEquip = "";
		public string Buy = "";
		public string BuyingStore = "";
		public string Def = "";
		public string Delay = "";
		public string EquipLv = "";
		public string Gender = "2";
		public string Id = "-1";
		public string Job = "";
		public string Loc = "";
		public string Matk = "";
		public string Name = "";
		public NoUse Nouse = new NoUse();
		public string OnEquipScript = "";
		public string OnUnequipScript = "";
		public string Range = "";
		public string Refineable = "false";
		public string Script = "";
		public string Sell = "";
		public string Slots = "";
		public string Sprite = "";
		public string Stack = "";
		public Trade Trade = new Trade();
		public string Type = "3";
		public string Upper = "0x3f";
		public string View = "";
		public string WeaponLv = "";
		public string Weight = "0";

		public ItemParser(string element) {
			List<string> lines = element.Split(TextFileHelper.SplitCharacter).ToList();

			for (int index = 0; index < lines.Count; index++) {
				string line = lines[index];
				int indexId = line.IndexOf(": ", StringComparison.Ordinal);

				if (indexId < 0)
					continue;

				string identifier = line.Substring(0, indexId + 2);

				switch (identifier) {
					case "Id: ":
						Id = line.Replace(identifier, "");

						// The .conf is actually quite confusing
						// Overriding values are not setup for some reason and the parser
						// has to guess and fix the issues.
						int ival;
						if (Int32.TryParse(Id, out ival)) {
							// Whips overrides the default property to 0
							if (ival >= 1950 && ival < 2000)
								Gender = "0";

							// Bride_Ring, I'm assuming it's hard coded in the client and
							// people thought it would be wise to ignore setting its gender
							if (ival == 2635)
								Gender = "0";

							// Bridegroom_Ring
							if (ival == 2634)
								Gender = "1";
						}
						break;
					case "Name: ": Name = line.Replace("Name: ", "").Trim('\"'); break;
					case "AegisName: ": AegisName = line.Replace("AegisName: ", "").Trim('\"'); break;
					case "Type: ":
						Type = line.Replace(identifier, "");

						// Refine: Refineable            (boolean, defaults to true)
						// ^ the most confusing line I've ever read, this is not true.
						// Defaults to false, default to true for item types 4 and 5
						if (Type == "4" || Type == "5") {
							Refineable = "true";
						}
						break;
					case "Sell: ": Sell = line.Replace(identifier, ""); break;
					case "Buy: ": Buy = line.Replace(identifier, ""); break;
					case "Weight: ": Weight = line.Replace(identifier, ""); break;
					case "Atk: ": Atk = line.Replace(identifier, ""); break;
					case "Matk: ": Matk = line.Replace(identifier, ""); break;
					case "Range: ": Range = line.Replace(identifier, ""); break;
					case "Def: ":
						Def = line.Replace(identifier, "");

						if (Def.Length > 0 && Def[0] == '-')
							Def = "0";

						break;
					case "Stack: ": Stack = line.Replace(identifier, ""); break;
					case "Sprite: ": Sprite = line.Replace(identifier, ""); break;
					case "Slots: ": Slots = line.Replace(identifier, ""); break;
					case "Job: ": Job = line.Replace(identifier, ""); break;
					case "Upper: ": Upper = line.Replace(identifier, ""); break;
					case "Gender: ": Gender = line.Replace(identifier, ""); break;
					case "Loc: ": Loc = line.Replace(identifier, ""); break;
					case "WeaponLv: ": WeaponLv = line.Replace(identifier, ""); break;
					case "EquipLv: ": EquipLv = line.Replace(identifier, ""); break;
					case "Refine: ": Refineable = line.Replace(identifier, ""); break;
					case "View: ": View = line.Replace(identifier, ""); break;
					case "BindOnEquip: ": BindOnEquip = line.Replace(identifier, ""); break;
					case "BuyingStore: ": BuyingStore = line.Replace(identifier, ""); break;
					case "Delay: ": Delay = line.Replace(identifier, ""); break;
					case "Trade: ":
						index++;
						line = lines[index];

						StringBuilder builderTrade = new StringBuilder();
						builderTrade.AppendLineUnix();

						while (line != "}" && line != null) {
							builderTrade.AppendLineUnix(line);
							index++;
							line = lines[index];
						}

						Trade.Set(builderTrade.ToString());
						break;
					case "Nouse: ":
						index++;
						line = lines[index];

						while (line != "}" && line != null) {
							indexId = line.IndexOf(": ", StringComparison.Ordinal);

							if (indexId < 0) {
								index++;
								line = index >= lines.Count ? null : lines[index];
								continue;
							}

							identifier = line.Substring(0, indexId + 2);

							switch (identifier) {
								case "override: ": Nouse.Override = line.Replace(identifier, ""); break;
								case "sitting: ": Nouse.Sitting = line.Replace(identifier, ""); break;
							}

							index++;
							line = lines[index];
						}
						break;
					case "Script: ": Script = _readScript(ref index, lines).Trim(' '); break;
					case "OnEquipScript: ": OnEquipScript = _readScript(ref index, lines).Trim(' '); break;
					case "OnUnequipScript: ": OnUnequipScript = _readScript(ref index, lines).Trim(' '); break;
				}
			}
		}

		private string _readScript(ref int index, List<string> lines) {
			// First line, may contain stuff
			string line = lines[index];

			int indexStart = line.IndexOf("<\"", StringComparison.Ordinal);
			int indexEnd = line.IndexOf("\">", indexStart + 1, StringComparison.Ordinal);

			if (indexEnd >= 0) {
				return line.Substring(indexStart + 2, indexEnd - indexStart - 2);
			}

			List<string> script = new List<string> { line.Substring(indexStart + 2, line.Length - indexStart - 2) };
			index++;

			for (; index < lines.Count; index++) {
				line = lines[index];

				if (line.StartsWith("\">")) {
					break;
				}

				script.Add(line);
			}

			return string.Join(" ", script.Where(p => p != "").ToArray());
		}

		public static string ToHerculesEntry(BaseDb db, int itemId) {
			var dbItems = db.GetMeta<int>(ServerDbs.Items);

			StringBuilder builder = new StringBuilder();

			var tuple = dbItems.TryGetTuple(itemId);

			if (tuple != null) {
				builder.AppendLineUnix("{");
				builder.AppendLineUnix("\tId: " + tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture));
				builder.AppendLineUnix("\tAegisName: \"" + tuple.GetValue<string>(ServerItemAttributes.AegisName) + "\"");
				builder.AppendLineUnix("\tName: \"" + tuple.GetValue<string>(ServerItemAttributes.Name) + "\"");

				builder.AppendLineUnix("\tType: " + tuple.GetValue<string>(ServerItemAttributes.Type));
				_trySet(tuple, builder, ServerItemAttributes.Buy);
				_trySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Sell, (tuple.GetValue<int>(ServerItemAttributes.Buy) / 2).ToString(CultureInfo.InvariantCulture));
				_trySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Weight, "0");
				_trySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Attack, "0");
				_trySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Matk, "0");
				_trySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Defense, "0");
				_trySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Range, "0");
				_trySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.NumberOfSlots, "0");
				_trySetIfDefaultEmptyAddHex(tuple, builder, ServerItemAttributes.ApplicableJob, "");
				_trySetIfDefaultEmptyUpper(tuple, builder, ServerItemAttributes.Upper);
				_trySetGender(tuple, builder, ServerItemAttributes.Gender, "2");
				_trySetIfDefaultLocation(tuple, builder, ServerItemAttributes.Location);
				_trySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.WeaponLevel, "0");
				_trySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.EquipLevel, "0");
				_trySetIfRefineable(tuple, builder, ServerItemAttributes.Refineable, true);
				_trySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.ClassNumber, "0");
				_trySetIfDefaultBoolean(tuple, builder, ServerItemAttributes.BindOnEquip, false);
				_trySetIfDefaultBoolean(tuple, builder, ServerItemAttributes.BuyingStore, false);
				_trySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Delay, "0");

				var trade = tuple.GetRawValue(ServerItemAttributes.Trade.Index) as Trade;
				if (trade != null && trade.NeedPrinting()) builder.AppendLineUnix(trade.ToWriteString());

				var nouse = tuple.GetRawValue(ServerItemAttributes.NoUse.Index) as NoUse;
				if (nouse != null && nouse.NeedPrinting()) builder.AppendLineUnix(nouse.ToWriteString());

				_trySetIfDefaultEmptyBracket(tuple, builder, ServerItemAttributes.Stack, "");
				_trySetIfDefaultEmpty(tuple, builder, ServerItemAttributes.Sprite, "0");
				_trySetIfDefaultEmptyScript(tuple, builder, ServerItemAttributes.Script, "");
				_trySetIfDefaultEmptyScript(tuple, builder, ServerItemAttributes.OnEquipScript, "");
				_trySetIfDefaultEmptyScript(tuple, builder, ServerItemAttributes.OnUnequipScript, "");
				builder.Append("},");
			}

			return builder.ToString();
		}

		private static void _trySetIfDefault(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue) {
			if (tuple.GetValue<string>(attribute) != defaultValue) {
				builder.AppendLineUnix("\t" + attribute.AttributeName + ": " + tuple.GetValue<string>(attribute));
			}
		}

		private static void _trySetIfDefaultEmpty(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue) {
			string val = tuple.GetValue<string>(attribute);

			if (val != "" && val != defaultValue && val != "-1") {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": ");
				builder.AppendLineUnix(val);
			}
		}

		private static void _trySetIfDefaultEmptyUpper(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute) {
			string val = tuple.GetValue<string>(attribute);

			if (val != "" && val != "7" && val != "63") {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": ");
				builder.AppendLineUnix(val);
			}
		}

		private static void _trySetIfDefaultLocation(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute) {
			string val = tuple.GetValue<string>(attribute);

			if (val != "" && val != "0") {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": ");
				builder.AppendLineUnix(val);
			}
		}

		private static void _trySetIfDefaultEmptyScript(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue) {
			string val = tuple.GetValue<string>(attribute);

			if (val != "" && val != defaultValue) {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": <\"");
				builder.Append(Format(val, 2, true));
				builder.AppendLineUnix("\">");
			}
		}

		private static void _trySetGender(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue) {
			int key = tuple.GetKey<int>();
			string val = tuple.GetValue<string>(attribute);

			if (key >= 1950 && key < 2000) {
				if (val == "0")
					return;
			}

			if (key == 2635) {
				if (val == "0")
					return;
			}

			if (val != "" && val != defaultValue && val != "-1") {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": ");
				builder.AppendLineUnix(val);
			}
		}

		private static void _trySetIfDefaultEmptyBracket(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue) {
			string val = tuple.GetValue<string>(attribute);

			if (val != "" && val != defaultValue) {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": [");
				builder.Append(val);
				builder.AppendLineUnix("]");
			}
		}

		public static string Format(string val, int indent = 2, bool setBrackets = false) {
			StringBuilder builder = new StringBuilder();

			int index = 0;
			int level = indent;
			bool quotation = false;
			bool trim = false;
			int lines = 1;

			if (level == 2) {
				builder.AppendLineUnix();
				builder.AppendIndent(level);
			}

			while (index < val.Length) {
				char c = val[index];

				switch (c) {
					case ';':
						if (!quotation) {
							lines++;
							builder.Append(";\n");
							builder.AppendIndent(level);
							trim = true;
						}
						else {
							builder.Append(c);
						}
						break;
					case '{':
						if (!quotation) {
							lines++;
							builder.Append("{\n");
							level++;
							builder.AppendIndent(level);
							trim = true;
						}
						else {
							builder.Append(c);
						}
						break;
					case '}':
						if (!quotation) {
							level--;
							lines++;

							if (builder.Length > 0 && builder[builder.Length - 1] == '\t') {
								builder[builder.Length - 1] = '}';
							}
							else {
								builder.Append(c);
							}

							builder.Append('\n');
							builder.AppendIndent(level);
							trim = true;
						}
						else {
							builder.Append(c);
						}
						break;
					case ' ':
					case '\t':
						if (trim) {
							index++;
							continue;
						}
						builder.Append(c);
						break;
					case '\"':
						trim = false;
						quotation = !quotation;
						builder.Append(c);
						break;
					default:
						trim = false;
						builder.Append(c);
						break;
				}

				index++;
			}

			string toRet = builder.ToString();

			if (indent == 2) {
				toRet = toRet.Trim(new char[] { '\n', '\t' });

				if (lines <= 2)
					return " " + toRet + " ";

				return "\n\t\t" + toRet + "\n\t";
			}

			return toRet;
		}

		private static void _trySetIfDefaultBoolean(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, bool defaultValue) {
			if (tuple.GetRawValue(attribute.Index) as string == "")
				return;

			bool val = tuple.GetValue<bool>(attribute);

			if (val != defaultValue) {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": ");
				builder.AppendLineUnix(val.ToString().ToLower());
			}
		}

		private static void _trySetIfRefineable(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, bool defaultValue) {
			int type = tuple.GetValue<int>(ServerItemAttributes.Type);
			bool val = tuple.GetValue<bool>(attribute);

			if (type != 4 && type != 5) {
				if (val) {
					// This is not supposed to be allowed, but... we'll let it slide
					DbLoaderErrorHandler.Handle("The refineable status on the item ID [" + tuple.GetKey<int>() + "] has been set to true but the item type is not an equipment. This is suspicious.", ErrorLevel.Warning);
					builder.AppendLineUnix("\t" + attribute.AttributeName + ": true");
				}
				return;
			}

			if (val != defaultValue) {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": ");
				builder.AppendLineUnix(val.ToString().ToLower());
			}
		}

		private static void _trySetIfDefaultEmptyAddHex(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue) {
			string val = "0x" + tuple.GetValue<string>(attribute);

			if (val != "" && val != defaultValue && val.Length > 2 && val.ToLower() != "0xffffffff") {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": ");
				builder.AppendLineUnix(val);
			}
		}

		private static void _trySetIfDefaultEmptyToHex(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue) {
			string val = tuple.GetValue<string>(attribute);

			int ival;

			if (Int32.TryParse(val, out ival)) {
				string sval = "0x" + ival.ToString("X").ToLower();

				if (val != defaultValue) {
					builder.Append("\t");
					builder.Append(attribute.AttributeName);
					builder.Append(": ");
					builder.AppendLineUnix(sval);
				}
			}
			else {
				Z.F();
			}
		}

		private static void _trySet(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute) {
			builder.Append("\t");
			builder.Append(attribute.AttributeName);
			builder.Append(": ");
			builder.AppendLineUnix(tuple.GetValue<string>(attribute));
		}
	}
}