using Database;
using SDE.Editor.Generic.TabsMakerCore;

namespace SDE.Editor.Generic.UI.CustomControls
{
    /// <summary>
    /// A custom control isn't bound with any database attribue while a format converter
    /// is. For that reason, they're useful for custom controls within a table. A good
    /// example would be showing an image based on a sprite name.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public interface ICustomControl<TKey, TValue> where TValue : Tuple
    {
        void Init(GDbTabWrapper<TKey, TValue> tab, DisplayableProperty<TKey, TValue> dp);
    }
}