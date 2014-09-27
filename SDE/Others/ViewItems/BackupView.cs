using System;
using System.IO;
using SDE.Tools.DatabaseEditor.Engines.BackupsEngine;

namespace SDE.Others.ViewItems {
	/// <summary>
	/// Backup view for list views
	/// </summary>
	public class BackupView {
		public BackupView(Backup backup) {
			if (backup == null) throw new ArgumentNullException("backup");
			
			BackupDate = backup.BackupDate;
			DbPath = backup.Info.DestinationPath;
			DateInt = long.Parse(backup.BackupDate);
			Date = DateTime.FromFileTime(DateInt).ToString("d/M/yyyy HH:mm:ss");
		}

		public string Date { get; set; }
		public string DbPath { get; set; }
		public string BackupDate { get; set; }
		public long DateInt { get; set; }

		public bool Normal {
			get {
				return true;
			}
		}
	}
}