using System.Windows.Controls;
using Database;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;

namespace SDE.Tools.DatabaseEditor.Generic.Lists.FormatConverter {
	public abstract class FormatConverter<TKey, TValue> where TValue : Tuple {
		protected DbAttribute _attribute;
		protected int _column;
		protected DisplayableProperty<TKey, TValue> _displayableProperty;
		protected Grid _parent;
		protected int _row;

		public virtual void OnInitialized() { }

		public void Initialize(DbAttribute attribute, int row, int column, object displayableProperty, Grid parent = null) {
			_row = row;
			_column = column;
			_attribute = attribute;
			_parent = parent;
			_displayableProperty = (DisplayableProperty<TKey, TValue>) displayableProperty;
			OnInitialized();
		}

		public abstract void Init(GDbTabWrapper<TKey, TValue> tab, DisplayableProperty<TKey, TValue> displayableProperty);
	}
}
