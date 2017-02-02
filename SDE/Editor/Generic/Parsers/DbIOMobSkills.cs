using System;
using System.Linq;
using SDE.Editor.Generic.Core;
using SDE.Editor.Writers;

namespace SDE.Editor.Generic.Parsers {
	public sealed class DbIOMobSkills {
		public static void Writer(DbDebugItem<string> debug, AbstractDb<string> db) {
			try {
				StringLineStream lines = new StringLineStream(debug.OldPath);
				lines.Remove(db);
				string line;

				foreach (ReadableTuple<string> tuple in db.Table.FastItems.Where(p => !p.Normal).OrderBy(p => p.GetValue<int>(1))) {
					line = string.Join(",", tuple.GetRawElements().Skip(1).Select(p => (p ?? "").ToString()).ToArray());
					lines.Write(tuple.Key, line);
				}

				lines.WriteFile(debug.FilePath);
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}
	}
}