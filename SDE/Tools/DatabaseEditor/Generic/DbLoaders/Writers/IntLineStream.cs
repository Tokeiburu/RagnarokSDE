using System.Linq;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders.Writers {
	public class IntLineStream : LineStream<int> {

		public IntLineStream(string path) : base(path, ',') {
		}

		public override int Default {
			get { return -1; }
		}

		public override void Write(int key, string line) {
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
	}
}