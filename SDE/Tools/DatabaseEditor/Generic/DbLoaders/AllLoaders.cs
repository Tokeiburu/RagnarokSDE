using System.IO;
using ErrorManager;
using GRF.IO;
using GRF.System;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using Utilities;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders {
	public static class AllLoaders {
		private static readonly TkDictionary<string, string> _backups = new TkDictionary<string, string>();

		static AllLoaders() {
			TemporaryFilesManager.UniquePattern("sdb_backup_{0:0000}.dat");
		}

		public static string LatestFile { get; set; }

		public static void BackupFile(string path) {
			if (path == null)
				return;

			if (File.Exists(path)) {
				string temp = TemporaryFilesManager.GetTemporaryFilePath("sdb_backup_{0:0000}.dat");
				_backups[path] = temp;
				File.Copy(path, temp);
				//BackupEngine.Instance.Backup(path);
			}
			else {
				_backups[path] = null;
			}
		}

		public static void ClearBackups() {
			_backups.Clear();
		}

		public static void UpdateBackups() {
			foreach (var pair in _backups) {
				if (File.Exists(pair.Key)) {
					if (GrfPath.Delete(pair.Value)) {
						File.Copy(pair.Key, pair.Value);
					}
				}
			}
		}

		public static string GetBackupFile(string path) {
			if (path == null)
				return null;

			return _backups[path];
		}

		public static bool GenericErrorHandler(ref int numError, object item) {
			DbLoaderErrorHandler.Handle("Failed to read an item.", item.ToString());
			numError--;
			if (numError < -10) {
				DbLoaderErrorHandler.Handle("Failed to read too many items, the db will stop loading.", ErrorLevel.Critical);
				return true;
			}

			return false;
		}

		public static string DetectPathAll(string db) {
			try {
				string path = SdeFiles.ServerDbPath.Filename;

				while (path != null) {
					if (File.Exists(GrfPath.Combine(path, db))) {
						return GrfPath.Combine(path, db);
					}

					path = Path.GetDirectoryName(path);
				}

				return null;
			}
			catch {
				return null;
			}
		}

		private static string _getInParentPath(string fileInput) {
			string path = Path.GetDirectoryName(SdeFiles.ServerDbPath.Filename);

			string[] files = fileInput.GetExtension() == null ? new string[] {fileInput + ".txt", fileInput + ".conf"} : new string[] {fileInput};

			foreach (string file in files) {
				var fullpath = Path.Combine(path, file);
				if (File.Exists(fullpath)) {
					return fullpath;
				}
			}

			return null;
		}

		private static string _getInCurrentPath(string fileInput) {
			string path = SdeFiles.ServerDbPath.Filename;

			string[] files = fileInput.GetExtension() == null ? new string[] {fileInput + ".txt", fileInput + ".conf"} : new string[] {fileInput};

			foreach (string file in files) {
				var fullpath = Path.Combine(path, file);
				if (File.Exists(fullpath)) {
					return fullpath;
				}
			}

			return null;
		}

		public static string DetectPath(string toString) {
			string path = _getInParentPath(toString);

			if (path != null)
				return path;

			path = _getInCurrentPath(toString);
			return path;
		}

		public static string DetectPath(ServerDBs toString, bool allowAlernative = true) {
			string path = _getInParentPath(toString);

			if (path != null)
				return path;

			path = _getInCurrentPath(toString);

			if (path == null && toString.AlternativeName != null && allowAlernative) {
				return DetectPath(toString.AlternativeName);
			}

			return path;
		}

		public static ServerType GetServerType() {
			return DetectPath(ServerDBs.Items).GetExtension() == ".conf" ? ServerType.Hercules : ServerType.RAthena;
		}

		public static FileType GetFileType(string path) {
			return path.IsExtension(".conf") ? FileType.Conf : FileType.Txt;
		}
	}
}
