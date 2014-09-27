using System;
using System.Collections.Generic;

namespace SDE.Tools.DatabaseEditor.Generic.IndexProviders {
	public interface IIndexProvider {
		List<int> GetIndexes();
	}

	public abstract class AbstractProvider : IIndexProvider {
		private List<int> _indexes;
		protected int _position = -1;

		public Type GroupAs { get; set; }

		#region IIndexProvider Members

		public abstract List<int> GetIndexes();
		public T Next<T>() {
			return (T) (Next() ?? default(T));
		}

		public virtual object Next() {
			if (_position < 0) {
				_indexes = GetIndexes();
			}

			if (++_position >= _indexes.Count)
				return null;

			return _indexes[_position];
		}

		public virtual IEnumerable<IIndexProvider> Providers {
			get {
				yield return this;
			}
		}

		public int this[int index] {
			get {
				if (_indexes == null)
					_indexes = GetIndexes();

				return _indexes[index];
			}
		}

		#endregion

		public static AbstractProvider GetProvider(object input) {
			if (input == null)
				return new NullIndexProvider();

			if (input is string) {
				return new QueryIndexProvider((string) input);
			}

			if (input is int[]) {
				return new SpecifiedRangeIndexProvider(input as int[]);
			}

			if (input is int[][]) {
				return new GroupIndexProvider(input as int[][]);
			}

			return null;
		}
	}
}