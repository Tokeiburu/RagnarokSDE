using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Database.Commands;
using Utilities.Services;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders.Writers {
	/// <summary>
	/// This is a stream writer and reader. It's used to preserve the format of a file.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class LineStream<T> {
		protected List<string> _allLines;
		protected string _newLine;
		protected char _separator;
		protected List<T> _ids = new List<T>();

		protected LineStream(string path, char separator) {
			_separator = separator;
			_allLines = LineStreamReader.ReadAllLines(path, EncodingService.DisplayEncoding, out _newLine).ToList();
			Init();
		}

		public void Init() {
			_init();
		}

		public abstract T Default {
			get;
		}

		public abstract void Write(T key, string line);

		protected virtual void _init() {
			string current;

			for (int i = 0; i < _allLines.Count; i++) {
				current = _allLines[i];

				if ((current.Length >= 2 && current[0] == '/' && current[1] == '/') || String.IsNullOrEmpty(current)) {
					_ids.Add(Default);
				}
				else {
					// Using StringSplitOptions.RemoveEmptyEntries would remove useful info
					string[] elements = current.Split(new char[] { _separator });

					if (elements.Length > 0) {
						if (elements[0].IndexOf("//", StringComparison.Ordinal) > -1) {
							elements[0] = elements[0].Substring(0, elements[0].IndexOf("//", StringComparison.Ordinal));
						}

						if (typeof(T) == typeof(int)) {
							int val;

							if (Int32.TryParse(elements[0], out val)) {
								_ids.Add((T) (object) val);
								continue;
							}
						}
						else if (typeof(T) == typeof(string)) {
							_ids.Add((T) (object) elements[0]);
							continue;
						}
						else {
							_ids.Add((T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[0]));
							continue;
						}
					}

					_ids.Add(Default);
				}
			}
		}

		public void WriteFile(string path) {
			StringBuilder builder = new StringBuilder();
			string[] array = ToArray();

			for (int index = 0; index < array.Length; index++) {
				string line = array[index];
				builder.Append(line);
				builder.Append(_newLine);
			}

			File.WriteAllText(path, builder.ToString());
		}

		public void Remove(BaseDb gdb) {
			if (!gdb.IsModified)
				return;

			AbstractDb<T> db = gdb.To<T>();

			if (db.Table.Commands.GetUndoCommands() == null)
				return;

			foreach (GroupCommand<T, ReadableTuple<T>> command in db.Table.Commands.GetUndoCommands().OfType<GroupCommand<T, ReadableTuple<T>>>()) {
				foreach (DeleteTuple<T, ReadableTuple<T>> deleteCommand in command.Commands.OfType<DeleteTuple<T, ReadableTuple<T>>>()) {
					Delete(deleteCommand.Key);
				}
			}
		}

		public void Delete(T key) {
			int index = _ids.IndexOf(key);

			while (index > -1) {
				_ids.RemoveAt(index);
				_allLines.RemoveAt(index);
				index = _ids.IndexOf(key);
			}
		}

		public void Append(string line) {
			_allLines.Add(line);
		}

		public string[] ToArray() {
			return _allLines.ToArray();
		}

		public void ClearAfterComments() {
			int indexStop = 0;
			for (int i = 0; i < _allLines.Count; i++) {
				if (String.IsNullOrEmpty(_allLines[i]) || (_allLines[i].Length >= 2 && _allLines[i][0] == '/' && _allLines[i][1] == '/'))
					continue;

				indexStop = i;
				break;
			}

			if (indexStop > -1) {
				_allLines = _allLines.Take(indexStop).ToList();
			}
		}
	}
}
