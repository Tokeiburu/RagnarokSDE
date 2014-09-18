using System;
using System.Linq;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders.Writers {
	public class StringLineStream : LineStream<string> {
		public StringLineStream(string path, char separator = '\t' ) : base(path, separator) {
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

		public override string Default {
			get { return null; }
		}
	}
}