using System.Windows.Controls;
using Database;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;

namespace SDE.Tools.DatabaseEditor.Generic.Lists.FormatConverters {
	/// <summary>
	/// A format converter changes the default behavior of how an attribute in the
	/// database is displayed. It's a much easier way to create customized fields rather
	/// than using a custom control (ICustomControl). Format converters do the conversion
	/// between an attribute and their input.
	/// 
	/// A format converter will be inserted in the display grid automatically without
	/// having to write extra code. The controls should be instantiated in the Init method.
	/// See CustomProperty for the base class of most customized controls used by the program.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	public abstract class FormatConverter<TKey, TValue> where TValue : Tuple {
		protected DisplayableProperty<TKey, TValue> _displayableProperty;
		protected DbAttribute _attribute;
		protected int _column;
		protected int _row;
		protected Grid _parent;

		public void Initialize(DbAttribute attribute, int row, int column, object displayableProperty, Grid parent = null) {
			_row = row;
			_column = column;
			_attribute = attribute;
			_parent = parent;
			_displayableProperty = (DisplayableProperty<TKey, TValue>) displayableProperty;
			OnInitialized();
		}

		public virtual void OnInitialized() { }
		public abstract void Init(GDbTabWrapper<TKey, TValue> tab, DisplayableProperty<TKey, TValue> displayableProperty);
	}
}
