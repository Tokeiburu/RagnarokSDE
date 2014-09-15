using System;

namespace SDE.Tools.DatabaseEditor.Engines.Parsers {
	public static class Parser {
		public static bool IsTrue(object obj, object objId) {
			string text = obj.ToString();
			string id = objId.ToString();
			int indexId = text.IndexOf(id + ": ", StringComparison.Ordinal);

			if (indexId < 0) {
				return false;
			}

			int indexEnd = text.IndexOf(Environment.NewLine, indexId + (id + ": ").Length, StringComparison.Ordinal);

			if (indexEnd < 0) {
				indexEnd = text.IndexOf('\n', indexId + (id + ": ").Length);
			}

			indexEnd = indexEnd < 0 ? text.Length : indexEnd;

			string sub = text.Substring(indexId + id.Length + 2, indexEnd - id.Length - indexId - 2).Trim(new char[] { ' ', '{', '}' });

			if (sub == "false")
				return false;
			if (sub == "true")
				return true;

			return false;
		}

		public static string GetVal(string text, object objId, string @default) {
			string id = objId.ToString();
			int indexId = text.IndexOf(id + ": ", StringComparison.Ordinal);

			if (indexId < 0) {
				return @default;
			}

			int indexEnd = text.IndexOf(Environment.NewLine, indexId + (id + ": ").Length, StringComparison.Ordinal);

			if (indexEnd < 0) {
				indexEnd = text.IndexOf('\n', indexId + (id + ": ").Length);
			}

			indexEnd = indexEnd < 0 ? text.Length : indexEnd;

			string sub = text.Substring(indexId + id.Length + 2, indexEnd - id.Length - indexId - 2).Trim(new char[] { ' ', '{', '}' });
			return sub;
		}
	}
}
