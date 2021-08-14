using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TokeiLibrary.WpfBugFix;

namespace SDE.Editor.Generic.UI.CustomControls {
	public interface ICustomEditableView {
		int SelectId { get; }
		string GetStringFormat(RangeListView lv, Dictionary<string, MetaTable<int>> dbs);
	}
}
