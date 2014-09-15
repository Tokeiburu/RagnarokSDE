using GRF.Core;
using GRF.IO;
using Utilities;

namespace SDE.Tools.DatabaseEditor.Engines {
	public class Backup {
		public string BackupDate { get; set; }
		public FileEntry Entry { get; set; }
		public BackupInfo Info { get; set; }

		public Backup(string backup) {
			BackupDate = backup;
			Entry = BackupEngine.Instance.Grf.FileTable[GrfPath.Combine(BackupDate, BackupEngine.InfoName)];
			Info = new BackupInfo(new ReadonlyConfigAsker(Entry.GetDecompressedData()));
		}
	}
}