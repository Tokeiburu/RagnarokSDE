using System;
using System.Collections.Generic;
using System.Linq;

namespace SDE.Tools.DatabaseEditor.Generic.IndexProviders {
	public class QuerryIndexProvider : AbstractProvider {
		private readonly List<int> _indexes = new List<int>();

		public QuerryIndexProvider(string querry) {
			string[] subQuerries = querry.Split(';').Where(p => !String.IsNullOrEmpty(p)).ToArray();

			foreach (string subQuerry in subQuerries) {
				if (subQuerry.Contains('-')) {
					int from = Int32.Parse(subQuerry.Split('-')[0]);
					int to = Int32.Parse(subQuerry.Split('-')[1]);

					for (int i = from; i <= to; i++) {
						_indexes.Add(i);
					}
				}
				else {
					_indexes.Add(Int32.Parse(subQuerry));
				}
			}
		}

		public override List<int> GetIndexes() {
			return _indexes;
		}
	}
}