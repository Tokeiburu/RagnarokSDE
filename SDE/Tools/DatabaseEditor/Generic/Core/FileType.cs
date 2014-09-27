using System;

namespace SDE.Tools.DatabaseEditor.Generic.Core {
	[Flags]
	public enum FileType {
		Error = 0,
		Txt = 1 << 1,
		Conf = 1 << 2,
		Sql = 1 << 3,
		Detect = 1 << 4,
	}
}