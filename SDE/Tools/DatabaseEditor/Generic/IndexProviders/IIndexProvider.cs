using System.Collections.Generic;

namespace SDE.Tools.DatabaseEditor.Generic.IndexProviders {
	public interface IIndexProvider {
		List<int> GetIndexes();
		int Next();
	}

	public abstract class AbstractProvider : IIndexProvider {
		private List<int> _indexes;
		private int _position = -1;

		#region IIndexProvider Members

		public abstract List<int> GetIndexes();
		public int Next() {
			if (_position < 0) {
				_indexes = GetIndexes();
			}

			return _indexes[++_position];
		}

		#endregion
	}
}