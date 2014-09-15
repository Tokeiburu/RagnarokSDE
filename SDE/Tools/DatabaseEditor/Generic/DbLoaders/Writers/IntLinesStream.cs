using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Database.Commands;
using Utilities.Services;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders.Writers {
	public class IntLinesStream {
		private readonly List<string> _allLines;
		private readonly List<int> _ids = new List<int>();
		private readonly string _newLine;

		public IntLinesStream(string path) {
			_allLines = LineStreamReader.ReadAllLines(path, EncodingService.DisplayEncoding, out _newLine).ToList();

			string current;

			for (int i = 0; i < _allLines.Count; i++) {
				current = _allLines[i];

				if (current.StartsWith("//", StringComparison.Ordinal) || String.IsNullOrEmpty(current)) {
					_ids.Add(-1);
				}
				else {
					string[] elements = current.Split(',');

					if (elements.Length > 0) {
						int val;

						if (elements[0].IndexOf("//", StringComparison.Ordinal) > -1) {
							elements[0] = elements[0].Substring(0, elements[0].IndexOf("//", StringComparison.Ordinal));
						}

						if (Int32.TryParse(elements[0], out val)) {
							_ids.Add(val);
							continue;
						}
					}

					_ids.Add(-1);
				}
			}
		}

		public void Write(int key, string line) {
			int? index = _ids.IndexOf(key);

			if (index > -1) {
				_allLines[index.Value] = line;
			}
			else {
				index = _ids.FirstOrDefault(p => p > -1 && p > key);

				if (index > -1) {
					index = _ids.IndexOf(index.Value);
				}

				if (_ids.All(p => key > p)) {
					index = _ids.Count;
				}

				if (index < 0)
					index = 0;

				_allLines.Insert(index.Value, line);
				_ids.Insert(index.Value, key);
			}
		}

		public void Delete(int key) {
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

			AbstractDb<int> db = gdb.To<int>();

			if (db.Table.Commands.GetUndoCommands() == null)
				return;

			foreach (GroupCommand<int, ReadableTuple<int>> command in db.Table.Commands.GetUndoCommands().OfType<GroupCommand<int, ReadableTuple<int>>>()) {
				foreach (DeleteTuple<int, ReadableTuple<int>> deleteCommand in command.Commands.OfType<DeleteTuple<int, ReadableTuple<int>>>()) {
					Delete(deleteCommand.Key);
				}
			}
		}
	}
}