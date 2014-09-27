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

		public const char SplitCharacter = '¤';
		public static string LastLineRead;
		public static bool SaveLastLine { get; set; }
		public static DebugStreamReader LastReader { get; set; }
		public static string LatestFile { get; set; }

		public static string ReadNextLine(StreamReader reader) {
			string line = null;

			while (!reader.EndOfStream) {
				line = reader.ReadLine();

				if (String.IsNullOrEmpty(line) || (line.Length >= 2 && line[0] == '/' && line[1] == '/'))
					continue;

				if (line.Length >= 2 && line[0] == '/' && line[1] == '*') {
					while (!reader.EndOfStream) {
						line = reader.ReadLine();

						if (String.IsNullOrEmpty(line))
							continue;

						if (line.IndexOf("*/", 0, StringComparison.Ordinal) > -1) {
							line = reader.ReadLine();

							if (!String.IsNullOrEmpty(line) && line.Length >= 2 && line[0] == '/' && line[1] == '*')
								continue;
							break;
						}
					}
				}

				if (String.IsNullOrEmpty(line) || (line.Length >= 2 && line[0] == '/' && line[1] == '/'))
					continue;

				if (line.IndexOf("//", 0, StringComparison.Ordinal) > -1) {
					return line.Substring(0, line.IndexOf('/'));
				}

				return line;
			}

			if (String.IsNullOrEmpty(line) || (line.Length >= 2 && line[0] == '/' && line[1] == '/'))
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

		public static string[] ReadNextElement(StreamReader reader) {
			string[] element = new string[2];

			while (!reader.EndOfStream) {
				string line = ReadNextLine(reader);

				if (line == null)
					continue;

				int firstIndexOfSharp = line.IndexOf('#', 0);

				if (firstIndexOfSharp >= 0) {
					element[0] = line.Substring(0, firstIndexOfSharp);

					if (element[0].Length == 0)
						continue;

					int secondIndexOfSharp = line.IndexOf('#', firstIndexOfSharp + 1);

					if (secondIndexOfSharp >= 0) {
						element[1] = line.Substring(firstIndexOfSharp + 1, secondIndexOfSharp - firstIndexOfSharp - 1);
					}
					else {
						// The second element is cut
						element[1] = ReadUntil(reader, "#");
					}

					return element;
				}

				return null;
			}

			return element;
		}

		public static IEnumerable<string[]> GetElements(byte[] data) {
			if (data != null) {
				using (StreamReader reader = SetAndLoadReader(new MemoryStream(data), EncodingService.DisplayEncoding)) {
					while (!reader.EndOfStream) {
						string[] element = ReadNextElement(reader);
						if (element != null) {
							yield return element;
						}
					}
				}
			}
		}

		public static IEnumerable<string[]> GetElementsByCommas(byte[] data) {
			if (data != null) {
				using (StreamReader reader = SetAndLoadReader(new MemoryStream(data), EncodingService.DisplayEncoding)) {
					while (!reader.EndOfStream) {
						string line = ReadNextLine(reader);

						if (!String.IsNullOrEmpty(line)) {
							if (SaveLastLine)
								LastLineRead = line;

							yield return ExcludeBrackets(line.Trim('\t'), '{', '}');
						}
					}
				}
			}
		}

		public static StreamReader SetAndLoadReader(string file, Encoding @default) {
			LatestFile = file;
			LastReader = new DebugStreamReader(new FileStream(file, FileMode.Open, FileAccess.Read), @default);
			return LastReader;
		}

		public static StreamReader SetAndLoadReader(MemoryStream memoryStream, Encoding @default) {
			LastReader = new DebugStreamReader(memoryStream, @default);
			return LastReader;
		}

		public static IEnumerable<string[]> GetElementsByTabs(byte[] data) {
			if (data != null) {
				using (StreamReader reader = SetAndLoadReader(new MemoryStream(data), EncodingService.DisplayEncoding)) {
					while (!reader.EndOfStream) {
						string line = ReadNextLine(reader);

						if (!String.IsNullOrEmpty(line)) {
							yield return line.Trim('\t', ' ').Split('\t', ' ').Select(p => p.Trim(' ')).Where(p => !String.IsNullOrEmpty(p)).ToArray();
						}
					}
				}
			}
		}

		public static string[] ExcludeBrackets(string line, char left = '{', char right = '}') {
			List<string> elements = new List<string>();

			int index = 0;
			int start = 0;

			while (index < line.Length) {
				if (line[index] == ',') {
					elements.Add(line.Substring(start, index - start));
					start = index + 1;
				}
				else if (line[index] == left) {
					int level = 1;
					index++;

					while (index < line.Length && level > 0) {
						if (line[index] == left)
							level++;

						if (line[index] == right)
							level--;

						index++;
					}

					elements.Add(line.Substring(start, index - start));

					while (index < line.Length && line[index] != ',')
						index++;

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

			if (elements.Count > 0)
				elements[elements.Count - 1] = elements[elements.Count - 1].Trim(' ', '\t');

			return elements.ToArray();
		}

		public static IEnumerable<string[]> GetElementsByBrackets(byte[] data) {
			if (data != null) {
				StringBuilder currentGroup = new StringBuilder();
				int level = 0;

				bool startedBracket = false;
				using (StreamReader reader = SetAndLoadReader(new MemoryStream(data), EncodingService.DisplayEncoding)) {
					while (!reader.EndOfStream) {
						string line = ReadNextLine(reader);
						if (!String.IsNullOrEmpty(line)) {
							line = line.Trim('\t', ' ');

							if (line.Length > 0 && line[0] == '{' && startedBracket == false && level == 0) {
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
									currentGroup.Append(SplitCharacter);
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
				using (StreamReader reader = SetAndLoadReader(new MemoryStream(data), EncodingService.DisplayEncoding)) {
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
}
