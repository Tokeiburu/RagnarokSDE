using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Database;
using ErrorManager;
using SDE.ApplicationConfiguration;
using SDE.Editor.Generic.Lists;
using SDE.View;
using Utilities;
using Utilities.Extension;

namespace SDE.Editor.Generic.Parsers.Generic {
	public sealed class DbIOFormatting {
		public static string OutputInteger(string rawElement) {
			if (String.IsNullOrEmpty(rawElement))
				return "0";

			return rawElement;
		}

		public static string ZeroDefault(string rawElement) {
			if (String.IsNullOrEmpty(rawElement))
				return rawElement;

			if (rawElement == "0")
				return "";

			return rawElement;
		}

		public static string HexToInt(string rawElement) {
			if (String.IsNullOrEmpty(rawElement))
				return rawElement;

			if (rawElement.StartsWith("0x") || rawElement.StartsWith("0X"))
				return Convert.ToInt32(rawElement, 16).ToString(CultureInfo.InvariantCulture);

			return rawElement;
		}

		public static void TrySetIfDefaultEmptyAddHex(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue) {
			string val = "0x" + tuple.GetValue<string>(attribute);

			if (val != "" && val != defaultValue && val.Length > 2 && val.ToLower() != "0xffffffff") {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": ");
				builder.AppendLine(val);
			}
		}

		public static void TrySetAttack(ReadableTuple<int> tuple, StringBuilder builder) {
			var atk1 = FormatConverters.IntOrHexConverter(tuple.GetValue<string>(ServerMobAttributes.Atk1));
			var atk2 = FormatConverters.IntOrHexConverter(tuple.GetValue<string>(ServerMobAttributes.Atk2));

			if (atk1 == atk2 && atk1 == 0)
				return;

			builder.AppendLine("\tAttack: [" + atk1 + ", " + (atk2 - atk1) + "]");
		}

		public static void TrySetIfNotDefault(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue, bool useConstants, string constGroup) {
			if (!useConstants) {
				TrySetIfNotDefault(tuple, builder, attribute, defaultValue);
				return;
			}

			string val = tuple.GetValue<string>(attribute);

			if (val != defaultValue) {
				builder.AppendLine("\t" + attribute.AttributeName + ": \"" + SdeEditor.Instance.ProjectDatabase.IntToConstant(Int32.Parse(val), constGroup) + "\"");
			}
		}

		public static void TrySetIfNotDefault(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue) {
			string val = tuple.GetValue<string>(attribute);

			if (val != defaultValue) {
				builder.AppendLine("\t" + attribute.AttributeName + ": " + val);
			}
		}

		public static void TrySetIfDefaultEmpty(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue) {
			string val = tuple.GetValue<string>(attribute);

			if (val != "" && val != defaultValue && val != "-1") {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": ");
				builder.AppendLine(val);
			}
		}

		public static void TrySetIfDefaultEmptyUpper(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute) {
			string val = tuple.GetValue<string>(attribute);

			if (val != "" && val != "7" && val != "63") {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": ");
				builder.AppendLine(val);
			}
		}

		public static void TrySetIfDefaultLocation(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute) {
			string val = tuple.GetValue<string>(attribute);

			if (val != "" && val != "0") {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": ");
				builder.AppendLine(val);
			}
		}

		public static void TrySetIfDefaultEmptyScript(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue) {
			string val = tuple.GetValue<string>(attribute);

			if (val != "" && val != defaultValue) {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": <\"");
				builder.Append(ScriptFormat(val, 2, true));
				builder.AppendLine("\">");
			}
		}

		public static void TrySetGender(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue) {
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
				builder.AppendLine(val);
			}
		}

		public static void TrySetIfDefaultEmptyBracket(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue) {
			string val = tuple.GetValue<string>(attribute);

			if (val != "" && val != defaultValue) {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": [");
				builder.Append(val);
				builder.AppendLine("]");
			}
		}

		public static void TrySetIfDefaultBoolean(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, bool defaultValue) {
			if (tuple.GetRawValue(attribute.Index) as string == "")
				return;

			bool val = tuple.GetValue<bool>(attribute);

			if (val != defaultValue) {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": ");
				builder.AppendLine(val.ToString().ToLower());
			}
		}

		public static void TrySetIfRefineable(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, bool defaultValue) {
			int type = tuple.GetValue<int>(ServerItemAttributes.Type);
			bool val = tuple.GetValue<bool>(attribute);

			if (type != 4 && type != 5) {
				if (val) {
					// This is not supposed to be allowed, but... we'll let it slide
					DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "The refineable state on the item ID [" + tuple.GetKey<int>() + "] has been set to true but the item type is not an equipment. This is suspicious.", ErrorLevel.Warning);
					builder.AppendLine("\t" + attribute.AttributeName + ": true");
				}
				return;
			}

			if (val != defaultValue) {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.Append(": ");
				builder.AppendLine(val.ToString().ToLower());
			}
		}

		public static void TrySetIfDefaultEmptyAddHexJobEx(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute, string defaultValue) {
			string val = "0x" + tuple.GetValue<string>(attribute);

			if (val != "" && val != defaultValue && val.Length > 2 && val.ToLower() != "0xffffffff") {
				builder.Append("\t");
				builder.Append(attribute.AttributeName);
				builder.AppendLine(": {");

				int value = FormatConverters.IntOrHexConverter(val);

				if (value == 0) {
					builder.AppendLine("\t\tAll: false");
					builder.AppendLine("\t}");
					return;
				}

				if (value > 0) {
					foreach (var job in DbIOItems.ItemDbJobs) {
						if ((value & job.Value) == job.Value) {
							builder.Append("\t\t");
							builder.Append(job.Key);
							builder.AppendLine(": true");
						}
					}
				}
				else {
					builder.AppendLine("\t\tAll: true");

					foreach (var job in DbIOItems.ItemDbJobs.Skip(1)) {
						if ((value & ~job.Value) == ~job.Value) {
							builder.Append("\t\t");
							builder.Append(job.Key);
							builder.AppendLine(": false");
						}
					}
				}

				builder.AppendLine("\t}");
			}
		}

		public static void SetType(ReadableTuple<int> tuple, StringBuilder builder, DbAttribute attribute) {
			var type = tuple.GetValue<string>(attribute);

			if (!SdeAppConfiguration.RevertItemTypes) {
				if (type == "5")
					type = "4";
				else if (type == "4")
					type = "5";
			}

			if (type == "3")
				return;

			builder.AppendLine("\tType: " + type);
		}

		public static string ScriptFormat(string val, int indent = 2, bool setBrackets = false) {
			StringBuilder builder = new StringBuilder();

			int index = 0;
			int level = indent;
			bool quotation = false;
			bool parenthesis = false;
			bool trim = false;
			int lines = 1;

			if (level == 2) {
				builder.AppendLine();
				builder.AppendIndent(level);
			}

			while (index < val.Length) {
				char c = val[index];

				switch(c) {
					case ';':
						if (!quotation && !parenthesis) {
							lines++;
							builder.Append(";\r\n");
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
							builder.Append("{\r\n");
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
					case 'f':
						builder.Append(c);

						if (val.IndexOf("for (", index, StringComparison.OrdinalIgnoreCase) == index) {
							parenthesis = true;
						}

						break;
					case ')':
						if (!quotation) {
							if (parenthesis) {
								parenthesis = false;
							}
						}

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
				toRet = toRet.Trim(new char[] { '\r', '\n', '\t' });

				if (lines <= 2)
					return " " + toRet + " ";

				return "\r\n\t\t" + toRet + "\r\n\t";
			}

			return toRet;
		}
	}
}