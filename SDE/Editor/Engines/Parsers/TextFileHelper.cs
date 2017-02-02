using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SDE.ApplicationConfiguration;
using Utilities.Services;

namespace SDE.Editor.Engines.Parsers {
	public static class TextFileHelper {
		#region Delegates
		public delegate IEnumerable<string[]> TextFileHelperGetterDelegate(byte[] data);
		#endregion

		//public const char SplitCharacter = '¤';
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

				int pos1;
				int pos2;

				while (!String.IsNullOrEmpty(line) && (pos1 = line.IndexOf("/*", 0, StringComparison.Ordinal)) > -1) {
					if ((pos2 = line.IndexOf("*/", pos1, StringComparison.Ordinal)) > -1) {
						return line.Substring(0, pos1) + line.Substring(pos2 + 2, line.Length - pos2 - 2);
					}

					string current = line.Substring(0, pos1);

					while (!reader.EndOfStream) {
						line = reader.ReadLine();

						if (String.IsNullOrEmpty(line)) continue;

						if ((pos2 = line.IndexOf("*/", 0, StringComparison.Ordinal)) > -1) {
							line = current + line.Substring(pos2 + 2, line.Length - pos2 - 2);
							break;
						}
					}
				}

				if (String.IsNullOrEmpty(line)) continue;

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

		public static string[] ReadNextElement(StreamReader reader, bool allowCutLine = true) {
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
						if (allowCutLine)
							element[1] = line.Substring(firstIndexOfSharp + 1).TrimStart(' ', '\t') + ReadUntil(reader, "#");
						else
							element[1] = line.Substring(firstIndexOfSharp + 1);
					}

					return element;
				}

				return null;
			}

			return element;
		}

		public static IEnumerable<string[]> GetElementsInt(byte[] data) {
			if (data != null) {
				using (StreamReader reader = SetAndLoadReader(data, EncodingService.DetectEncoding(data))) {
					string[] elements = null;
					int ival;

					while (!reader.EndOfStream) {
						string line = ReadNextLine(reader);

						if (line == null)
							continue;

						string[] subElements = line.Split(new char[] { '#' });

						if (subElements.Length <= 1)
							continue;

						if (elements != null) {
							if (Int32.TryParse(subElements[0], out ival)) {
								yield return elements;
								elements = subElements;
							}
							else {
								elements[elements.Length - 1] = elements[elements.Length - 1].Trim(' ', '\t') == "" ? subElements[0] : elements[elements.Length - 1] + "\r\n" + subElements[0];
								subElements = subElements.Skip(1).ToArray();
								elements = elements.Concat(subElements).ToArray();
							}
						}
						else {
							elements = subElements;
						}
					}

					if (elements != null && Int32.TryParse(elements[0], out ival)) {
						yield return elements;
					}
				}
			}
		}

		public static IEnumerable<string[]> GetElements(byte[] data, bool allowCutLines = true) {
			if (data != null) {
				using (StreamReader reader = SetAndLoadReader(data, EncodingService.DetectEncoding(data))) {
					while (!reader.EndOfStream) {
						string[] element = ReadNextElement(reader, allowCutLines);
						if (element != null) {
							if (element.All(p => p == null))
								continue;
							yield return element;
						}
					}
				}
			}
		}

		public static IEnumerable<string> GetSingleElement(byte[] data) {
			if (data != null) {
				using (StreamReader reader = SetAndLoadReader(data, EncodingService.DetectEncoding(data))) {
					while (!reader.EndOfStream) {
						string element = ReadNextSingle(reader);
						if (element != null) {
							yield return element;
						}
					}
				}
			}
		}

		public static string ReadNextSingle(StreamReader reader) {
			while (!reader.EndOfStream) {
				string line = ReadNextLine(reader);

				if (line == null) return null;

				int firstIndexOfSharp = line.IndexOf('#');

				if (firstIndexOfSharp >= 0) {
					return line.Substring(0, firstIndexOfSharp);
				}

				return line;
			}

			return null;
		}

		public static IEnumerable<string[]> GetElementsByCommas2(byte[] data) {
			if (data != null) {
				using (StreamReader reader = SetAndLoadReader(data)) {
					while (!reader.EndOfStream) {
						string line = reader.ReadLine();

						if (String.IsNullOrEmpty(line) || (line.Length >= 2 && line[0] == '/' && line[1] == '/'))
							continue;

						if (SaveLastLine)
							LastLineRead = line;

						yield return ExcludeBrackets(line.Trim('\t'), '{', '}');
					}
				}
			}
		}

		public static IEnumerable<string[]> GetElementsByCommas(byte[] data) {
			if (data != null) {
				using (StreamReader reader = SetAndLoadReader(data)) {
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

		public static IEnumerable<string[]> GetElementsByCommasQuotes(byte[] data) {
			if (data != null) {
				using (StreamReader reader = SetAndLoadReader(data)) {
					while (!reader.EndOfStream) {
						string line = ReadNextLine(reader);

						if (!String.IsNullOrEmpty(line)) {
							if (SaveLastLine)
								LastLineRead = line;

							yield return ExcludeBrackets(line.Trim('\t'), '\"', '\"');
						}
					}
				}
			}
		}

		public static IEnumerable<string[]> GetElementsByTabs(byte[] data) {
			if (data != null) {
				using (StreamReader reader = SetAndLoadReader(data)) {
					while (!reader.EndOfStream) {
						string line = ReadNextLine(reader);

						if (!String.IsNullOrEmpty(line)) {
							var output = line.Trim('\t', ' ').Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

							if (output.Length == 0)
								continue;

							yield return output;
						}
					}
				}
			}
		}

		public static DebugStreamReader SetAndLoadReader(string file) {
			LatestFile = file;
			LastReader = new DebugStreamReader(File.ReadAllBytes(file), SdeAppConfiguration.EncodingServer);
			return LastReader;
		}

		public static DebugStreamReader SetAndLoadReader(byte[] data) {
			LastReader = new DebugStreamReader(data, SdeAppConfiguration.EncodingServer);
			return LastReader;
		}

		public static StreamReader SetAndLoadReader(byte[] data, Encoding encoding) {
			LastReader = new DebugStreamReader(data, encoding, true);
			return LastReader;
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

		public static string GetLastError() {
			if (LastReader == null) {
				return "No file being read.";
			}

			return "Failed to read line #" + LastReader.LineNumber + ", in '" + LatestFile + "'.";
		}
	}
}