using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Utilities.Services;

namespace SDE.Tools.DatabaseEditor.Engines.Parsers {
	public static class TextFileHelper {
		#region Delegates

		public delegate IEnumerable<string[]> TextFileHelperGetterDelegate(byte[] data);

		#endregion

		public static DebugStreamReader LastReader { get; set; }

		public static string ReadNextLine(StreamReader reader) {
			string line = null;

			while (!reader.EndOfStream) {
				line = reader.ReadLine();

				if (string.IsNullOrEmpty(line) || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("\r\n", StringComparison.Ordinal))
					continue;

				if (line.StartsWith("/*", StringComparison.Ordinal)) {
					while (!reader.EndOfStream) {
						line = reader.ReadLine();

						if (string.IsNullOrEmpty(line))
							continue;

						if (line.IndexOf("*/", 0, StringComparison.Ordinal) > -1) {
							line = reader.ReadLine();

							if (!string.IsNullOrEmpty(line) && line.StartsWith("/*"))
								continue;
							break;
						}
					}
				}

				if (string.IsNullOrEmpty(line) || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("\r\n", StringComparison.Ordinal))
					continue;

				if (line.IndexOf("//", 0, StringComparison.Ordinal) > -1) {
					return line.Substring(0, line.IndexOf('/'));
				}

				return line;
			}

			if (string.IsNullOrEmpty(line) || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("\r\n", StringComparison.Ordinal))
				return null;

			return line;
		}

		public static string ReadUntil(StreamReader reader, string c) {
			StringBuilder toReturn = null;

			while (!reader.EndOfStream) {
				string temp = ReadNextLine(reader);

				if (temp != null) {
					if (toReturn == null)
						toReturn = new StringBuilder();

					if (temp == c) {
						toReturn.Append("\r\n");
						return toReturn.ToString();
					}

					toReturn.Append("\r\n" + temp);
				}
			}

			return toReturn == null ? "" : toReturn.ToString();
		}

		public static ElementRead ReadNextElement(StreamReader reader) {
			ElementRead element = null;

			while (!reader.EndOfStream) {
				element = new ElementRead();
				string line = ReadNextLine(reader);

				if (line == null)
					continue;

				int firstIndexOfSharp = line.IndexOf('#');

				if (firstIndexOfSharp >= 0) {
					element.Element1 = line.Substring(0, firstIndexOfSharp);

					if (element.Element1.Length == 0)
						continue;

					int secondIndexOfSharp = line.IndexOf('#', firstIndexOfSharp + 1);

					if (secondIndexOfSharp >= 0) {
						element.Element2 = line.Substring(firstIndexOfSharp + 1, secondIndexOfSharp - firstIndexOfSharp - 1);
					}
					else {
						// The second element is cut
						element.Element2 = ReadUntil(reader, "#");
					}

					return element;
				}

				return null;
			}

			return element;
		}

		public static IEnumerable<ElementRead> GetElements(byte[] data) {
			if (data != null) {
				using (StreamReader reader = _setAndLoadReader(new MemoryStream(data), EncodingService.DisplayEncoding)) {
					while (!reader.EndOfStream) {
						ElementRead element = ReadNextElement(reader);
						if (element != null) {
							yield return element;
						}
					}
				}
			}
		}

		public static IEnumerable<string[]> GetElementsByCommas(byte[] data) {
			if (data != null) {
				using (StreamReader reader = _setAndLoadReader(new MemoryStream(data), EncodingService.DisplayEncoding)) {
					while (!reader.EndOfStream) {
						string line = ReadNextLine(reader);

						if (!String.IsNullOrEmpty(line)) {
							yield return ExcludeBrackets(line.Trim('\t'));
						}
					}
				}
			}
		}

		private static StreamReader _setAndLoadReader(MemoryStream memoryStream, Encoding @default) {
			LastReader = new DebugStreamReader(memoryStream, @default);
			return LastReader;
		}

		public static IEnumerable<string[]> GetElementsByTabs(byte[] data) {
			if (data != null) {
				using (StreamReader reader = _setAndLoadReader(new MemoryStream(data), EncodingService.DisplayEncoding)) {
					while (!reader.EndOfStream) {
						string line = ReadNextLine(reader);

						if (!String.IsNullOrEmpty(line)) {
							yield return line.Trim('\t').Split('\t').Select(p => p.Trim(' ')).Where(p => !String.IsNullOrEmpty(p)).ToArray();
						}
					}
				}
			}
		}

		public static string[] ExcludeBrackets(string line) {
			List<string> elements = new List<string>();

			int index = 0;
			int start = 0;

			while (index < line.Length) {
				if (line[index] == ',') {
					elements.Add(line.Substring(start, index - start));
					start = index + 1;
				}
				else if (line[index] == '{') {
					int level = 1;
					index++;

					while (index < line.Length && level > 0) {
						if (line[index] == '{')
							level++;

						if (line[index] == '}')
							level--;

						index++;
					}

					elements.Add(line.Substring(start, index - start));
					start = index + 1;
				}

				index++;
			}

			if (index > start) {
				elements.Add(line.Substring(start, index - start));
			}
			else if (index == start && index > 0 && line[line.Length - 1] == ',') {
				elements.Add("");
			}

			return elements.ToArray();
		}

		public static IEnumerable<string[]> GetElementsByBrackets(byte[] data) {
			if (data != null) {
				StringBuilder currentGroup = new StringBuilder();
				int level = 0;

				bool startedBracket = false;
				using (StreamReader reader = _setAndLoadReader(new MemoryStream(data), EncodingService.DisplayEncoding)) {
					while (!reader.EndOfStream) {
						string line = ReadNextLine(reader);
						if (!String.IsNullOrEmpty(line)) {
							line = line.Trim('\t').TrimStart(' ');
							if (line.StartsWith("{", StringComparison.Ordinal) && startedBracket == false && level == 0) {
								level = 1;
								currentGroup = new StringBuilder();
								string val = line.Substring(1);

								if (val != "") {
									currentGroup.AppendLine(val);
								}

								startedBracket = true;
								continue;
							}
							if (startedBracket) {
								if (line.IndexOf('{', 0) > -1)
									level++;

								if (line.IndexOf('}', 0) > -1) {
									level--;
								}

								if (level == 0) {
									startedBracket = false;
								}
								else {
									currentGroup.Append(line);
									currentGroup.Append("¤");
									continue;
								}
							}
							else {
								continue;
							}
							yield return new string[] { currentGroup.ToString() };
						}
					}
				}
			}
		}
		public static IEnumerable<string> GetElementsByParenthesis(byte[] data) {
			if (data != null) {
				StringBuilder currentGroup = new StringBuilder();
				int level = 0;

				bool startedBracket = false;
				using (StreamReader reader = _setAndLoadReader(new MemoryStream(data), EncodingService.DisplayEncoding)) {
					while (!reader.EndOfStream) {
						string line = ReadNextLine(reader);
						int smallIndex;
						if (!String.IsNullOrEmpty(line)) {
							line = line.Trim('\t').TrimStart(' ');
							if ((smallIndex = line.IndexOf(": (", StringComparison.Ordinal)) > -1 && startedBracket == false && level == 0) {
								level = 1;
								currentGroup = new StringBuilder();
								string val = line.Substring(0, smallIndex);

								if (val != "") {
									currentGroup.Append(val);
									currentGroup.Append("¤");
								}

								startedBracket = true;
								continue;
							}

							if (startedBracket) {
								if (line.IndexOf('(', 0) > -1)
									level++;

								if (line.IndexOf(')', 0) > -1) {
									level--;
								}

								if (level == 0) {
									startedBracket = false;
								}
								else {
									currentGroup.Append(line);
									currentGroup.Append("¤");
									continue;
								}
							}
							else {
								continue;
							}
							yield return currentGroup.ToString();
						}
					}
				}
			}
		}
	}

	public class ElementRead {
		public string Element1 { get; set; }
		public string Element2 { get; set; }
		public string Element3 { get; set; }
		public string Element4 { get; set; }
		public string Element5 { get; set; }
	}
}
