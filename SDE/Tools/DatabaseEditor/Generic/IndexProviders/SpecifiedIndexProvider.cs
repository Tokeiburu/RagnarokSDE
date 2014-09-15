using System.Collections.Generic;
using System.Linq;

namespace SDE.Tools.DatabaseEditor.Generic.IndexProviders {
	public class SpecifiedIndexProvider : AbstractProvider {
		private readonly IEnumerable<int> _indexes;

		public SpecifiedIndexProvider(IEnumerable<int> indexes) {
			_indexes = indexes;
		}

		public override List<int> GetIndexes() {
			return _indexes.ToList();
		}
	}
}