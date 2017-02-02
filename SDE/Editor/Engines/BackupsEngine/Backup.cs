using System;
using GRF.Core;
using GRF.IO;
using Utilities;

namespace SDE.Editor.Engines.BackupsEngine {
	public class Backup {
		public Backup(string backup) {
			if (backup == null) throw new ArgumentNullException("backup");

			BackupDate = backup;
			Entry = BackupEngine.Instance.Grf.FileTable[GrfPath.Combine(BackupDate, BackupEngine.InfoName)];
			Info = new BackupInfo(new ReadonlyConfigAsker(Entry.GetDecompressedData()));
		}

		public string BackupDate { get; private set; }
		public FileEntry Entry { get; private set; }
		public BackupInfo Info { get; private set; }
	}
}