using System.Collections.Generic;

namespace SDE.Tools.DatabaseEditor.Generic.IndexProviders {
	public class NullIndexProvider : AbstractProvider {
		public override List<int> GetIndexes() {
			return new List<int>();
		}
	}
}