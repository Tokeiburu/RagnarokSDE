using System;
using System.IO;

namespace SDE.Editor.Engines.Parsers {
	public class TextLineParser {
		public static string ReadNextLine(StreamReader reader) {
			string line;

			while (!reader.EndOfStream) {
				line = reader.ReadLine();

				if (String.IsNullOrEmpty(line) || (line.Length >= 2 && line[0] == '/' && line[1] == '/'))
					continue;

				return line;
			}

			return null;
		}
	}
}