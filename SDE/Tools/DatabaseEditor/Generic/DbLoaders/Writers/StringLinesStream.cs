using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Database.Commands;
using Utilities.Services;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders.Writers {
	public class StringLinesStream {
		private readonly List<string> _ids = new List<string>();
		private List<string> _allLines;
		private readonly string _newLine;

		public StringLinesStream(string path, char separator = '\t' ) {
			_allLines = LineStreamReader.ReadAllLines(path, EncodingService.DisplayEncoding, out _newLine).ToList();

			string current;

			for (int i = 0; i < _allLines.Count; i++) {
				current = _allLines[i];

				if (current.StartsWith("//") || String.IsNullOrEmpty(current)) {
					_ids.Add(null);
				}
				else {
					string[] elements = current.Split(new char[] { separator }, StringSplitOptions.RemoveEmptyEntries);

					if (elements.Length > 0) {
						_ids.Add(elements[0]);
						continue;
					}

					_ids.Add(null);
				}
			}
		}

		public void ClearAfterComments() {
			int indexStop = 0;
			for (int i = 0; i < _allLines.Count; i++) {
				if (String.IsNullOrEmpty(_allLines[i]) || _allLines[i].StartsWith("//"))
					continue;

				indexStop = i;
				break;
			}

			if (indexStop > -1) {
				_allLines = _allLines.Take(indexStop).ToList();
			}
		}

		public void Write(string key, string line) {
			int? index = _ids.IndexOf(key);

			if (index > -1) {
				_allLines[index.Value] = line;
			}
			else {
				var tempList = _ids.Concat(new string[] { key }).Where(p => p != null).OrderBy(p => p, StringComparer.InvariantCulture).ToList();
				int tempIndex = tempList.IndexOf(key) + 1;

				if (tempIndex < 0)
					tempIndex = 0;

				if (tempIndex >= tempList.Count)
					tempIndex = tempList.Count - 1;

				string sindex = tempList[tempIndex];

				if (sindex == key) {
					index = _ids.Count;
				}
				else if (sindex != null) {
					index = _ids.IndexOf(sindex);
				}
				else {
					index = _ids.Count;
				}

				if (index < 0)
					index = 0;

				_allLines.Insert(index.Value, line);
				_ids.Insert(index.Value, key);
			}
		}

		public void Append(string line) {
			_allLines.Add(line);
		}

		public void Delete(string key) {
			int index = _ids.IndexOf(key);

			while (index > -1) {
				_ids.RemoveAt(index);
				_allLines.RemoveAt(index);
				index = _ids.IndexOf(key);
			}
		}

		public string[] ToArray() {
			return _allLines.ToArray();
		}

		public void WriteFile(string path) {
			StringBuilder builder = new StringBuilder();
			string[] array = ToArray();

			for (int index = 0; index < array.Length; index++) {
				string line = array[index];
				builder.Append(line);

				//if (index < array.Length - 1)
					builder.Append(_newLine);
			}

			File.WriteAllText(path, builder.ToString());
			//File.WriteAllLines(path, ToArray(), EncodingService.Ansi);
		}

		public void Remove(BaseDb gdb) {
			if (!gdb.IsModified)
				return;

			AbstractDb<string> db = gdb.To<string>();

			if (db.Table.Commands.GetUndoCommands() == null)
				return;

			foreach (GroupCommand<string, ReadableTuple<string>> command in db.Table.Commands.GetUndoCommands().OfType<GroupCommand<string, ReadableTuple<string>>>()) {
				foreach (DeleteTuple<string, ReadableTuple<string>> deleteCommand in command.Commands.OfType<DeleteTuple<string, ReadableTuple<string>>>()) {
					Delete(deleteCommand.Key);
				}
			}
		}
	}
}