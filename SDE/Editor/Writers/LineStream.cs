using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Database.Commands;
using SDE.Editor.Engines;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;

namespace SDE.Editor.Writers {
	/// <summary>
	/// This is a stream writer and reader. It's used to preserve the format of a file.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class LineStream<T> {
		protected List<string> _allLines;
		protected List<T> _ids = new List<T>();
		protected string _newLine;
		protected char _separator;
		protected int _keyIndex = 0;
		protected bool _allowDuplicates;

		protected LineStream(string path, char separator, int key = 0, bool allowDuplicates = true) {
			_keyIndex = key;
			_allowDuplicates = allowDuplicates;
			_separator = separator;
			_allLines = LineStreamReader.ReadAllLines(path, out _newLine).ToList();
			_init();
		}

		public abstract T Default { get; }

		public abstract void Write(T key, string line);

		protected virtual T _assignId(T id) {
			return id;
		}

		protected void _init() {
			string current;
			HashSet<T> keys = new HashSet<T>();

			for (int i = 0; i < _allLines.Count; i++) {
				current = _allLines[i];

				if ((current.Length >= 2 && current[0] == '/' && current[1] == '/') || String.IsNullOrEmpty(current)) {
					_ids.Add(Default);
				}
				else {
					// Using StringSplitOptions.RemoveEmptyEntries would remove useful info
					string[] elements = current.Split(new char[] { _separator });

					if (elements.Length > _keyIndex) {
						if (elements[_keyIndex].IndexOf("//", StringComparison.Ordinal) > -1) {
							elements[_keyIndex] = elements[_keyIndex].Substring(0, elements[_keyIndex].IndexOf("//", StringComparison.Ordinal));
						}

						if (typeof(T) == typeof(int)) {
							int val;

							if (Int32.TryParse(elements[_keyIndex], out val)) {
								_ids.Add((T)(object)val);
								continue;
							}
						}
						else if (typeof(T) == typeof(string)) {
							var lastKey = _assignId((T)(object)elements[_keyIndex]);

							_ids.Add(lastKey);

							if (!_allowDuplicates) {
								if (!keys.Add(lastKey)) {
									var lastIndex = _ids.IndexOf(lastKey);
									_ids[lastIndex] = Default;
									_allLines[lastIndex] = null;
								}
							}

							continue;
						}
						else {
							_ids.Add((T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(elements[_keyIndex]));
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

			IOHelper.WriteAllText(path, builder.ToString());
		}

		public virtual void Remove(BaseDb gdb) {
			AbstractDb<T> db = gdb.To<T>();

			if (db.Table.Commands.GetUndoCommands() == null)
				return;

			foreach (GroupCommand<T, ReadableTuple<T>> commandGroup in db.Table.Commands.GetUndoCommands().OfType<GroupCommand<T, ReadableTuple<T>>>()) {
				foreach (DeleteTuple<T, ReadableTuple<T>> command in commandGroup.Commands.OfType<DeleteTuple<T, ReadableTuple<T>>>()) {
					Delete(command.Key);
				}

				foreach (ChangeTupleKey<T, ReadableTuple<T>> command in commandGroup.Commands.OfType<ChangeTupleKey<T, ReadableTuple<T>>>()) {
					// If the key was changed, the old key must be removed
					Delete(command.Key);
				}

				foreach (ChangeTupleProperty<T, ReadableTuple<T>> command in commandGroup.Commands.OfType<ChangeTupleProperty<T, ReadableTuple<T>>>()) {
					if (command.Attribute.Index == 0)
						Delete(command.Key);
				}
			}

			foreach (ChangeTupleKey<T, ReadableTuple<T>> command in db.Table.Commands.GetUndoCommands().OfType<ChangeTupleKey<T, ReadableTuple<T>>>()) {
				// If the key was changed, the old key must be removed
				Delete(command.Key);
			}

			foreach (ChangeTupleProperty<T, ReadableTuple<T>> command in db.Table.Commands.GetUndoCommands().OfType<ChangeTupleProperty<T, ReadableTuple<T>>>()) {
				if (command.Attribute.Index == 0)
					Delete(command.Key);
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

		public void ClearAll() {
			_allLines = new List<string>();
			_ids = new List<T>();
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
				_ids = _ids.Take(indexStop).ToList();
			}
		}
	}
}