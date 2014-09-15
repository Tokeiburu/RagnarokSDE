using Database;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;

namespace SDE.Tools.DatabaseEditor.Generic.CustomControls {
	public interface ICustomProperty<TKey, TValue> where TValue : Tuple {
		void Init(GDbTabWrapper<TKey, TValue> tab, DisplayableProperty<TKey, TValue> dp);
	}
}