using Utilities;

namespace SDE.Tools.DatabaseEditor.Engines {
	public class BackupInfo {
		private readonly ConfigAsker _info;

		public BackupInfo(ConfigAsker info) {
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