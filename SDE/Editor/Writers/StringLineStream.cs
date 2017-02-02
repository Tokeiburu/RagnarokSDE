using System;
using System.Linq;

namespace SDE.Editor.Writers {
	public class StringLineStream : LineStream<string> {
		private int _rndOffset;

		public StringLineStream(string path, char separator = '\t', bool allowDuplicates = true) : base(path, separator, 0, allowDuplicates) {
		}

		public override string Default {
			get { return null; }
		}

		protected override string _assignId(string id) {
			if (_allowDuplicates) {
				while (_ids.Contains(id)) {
					id = id + "_" + _rndOffset++;
				}
			}

			return id;
		}

		public override void Write(string key, string line) {
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
	}
}