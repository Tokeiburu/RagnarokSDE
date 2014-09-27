using System;
using Utilities;

namespace SDE.Tools.DatabaseEditor.Engines.BackupsEngine {
	/// <summary>
	/// Used to load and save data related to a backup.
	/// </summary>
	public class BackupInfo {
		private readonly ConfigAsker _info;

		public BackupInfo(ConfigAsker info) {
			if (info == null) throw new ArgumentNullException("info");

			_info = info;
		}

		public string DestinationPath {
			get { return _info["[Backup - Destination path]", null]; }
			set { _info["[Backup - Destination path]"] = value; }
		}

		public byte[] GetData() {
			return ((TextConfigAsker) _info).GetByteData();
		}
	}
}