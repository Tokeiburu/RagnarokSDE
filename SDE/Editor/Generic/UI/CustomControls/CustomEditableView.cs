using System.Collections.Generic;
using TokeiLibrary.WpfBugFix;

namespace SDE.Editor.Generic.UI.CustomControls
{
    public interface ICustomEditableView
    {
        int SelectId { get; }

        string GetStringFormat(RangeListView lv, Dictionary<string, MetaTable<int>> dbs);
    }
}