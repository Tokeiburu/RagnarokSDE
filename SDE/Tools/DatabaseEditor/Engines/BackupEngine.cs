using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GRF.Core;
using GRF.IO;
using GRF.System;
using GRF.Threading;
using SDE.ApplicationConfiguration;
using Utilities;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Engines {
	public sealed class BackupEngine {
		private static readonly BackupEngine _instance = new BackupEngine();
		public const int MaximumNumberOfBackups = 30;
		public const string InfoName = "restore.inf";

		private string _grfPath {
			get { return GrfPath.Combine(SDEAppConfiguration.ProgramDataPath, "_backups.grf"); }
		}

		public GrfHolder Grf {
			get {
				_validateOpened();
				return _grf;
			}
		}

		public static BackupEngine Instance {
			get { return _instance; }
		}

		private BackupEngine() {
		}

		private Dictionary<int, string> _paths = new Dictionary<int, string>();
		private int _currentId;
		private GrfHolder _grf;

		static BackupEngine() {
			Instance.Init();
			TemporaryFilesManager.UniquePattern("backup_local_copy_{0:0000}");
		}

		public void Init() {
			_grf = new GrfHolder(_grfPath, GrfLoadOptions.OpenOrNew);
			_grf.Close();
		}

		private bool _isStarted = false;

		public void Start(string dbPath) {
			if (!SDEAppConfiguration.BackupsManagerState) return;

			_currentId++;

			_validateOpened();

			BackupInfo info = new BackupInfo(new TextConfigAsker(new byte[] {}));
			info.DestinationPath = Path.GetDirectoryName(dbPath);

			if (!_paths.ContainsKey(_currentId)) {
				_paths[_currentId] = _getGrfPath();
			}

			_grf.Commands.AddFileAbsolute(GrfPath.Combine(_paths[_currentId], InfoName), info.GetData());

			List<string> paths = GetBackupFiles().OrderBy(long.Parse).ToList();

			while (paths.Count > MaximumNumberOfBackups) {
				RemoveBackupDelayed(paths[0]);
				paths.RemoveAt(0);
			}

			_isStarted = true;
		}

		private void _validateOpened() {
			if (!_grf.IsOpened)
				_grf.Open(_grfPath, GrfLoadOptions.OpenOrNew);
		}

		public void RemoveBackupDelayed(string backup) {
			_validateOpened();
			_grf.Commands.RemoveFolder(backup);
		}

		public void RemoveBackup(string backup) {
			_validateOpened();

			_grf.Commands.RemoveFolder(backup);
			_grf.SyncQuickMerge(null);
			_grf.Close();
		}

		public void Restore(string backup) {
			_validateOpened();

			BackupInfo info = new BackupInfo(new ReadonlyConfigAsker(_grf.FileTable[GrfPath.Combine(backup, InfoName)].GetDecompressedData()));

			if (!Directory.Exists(info.DestinationPath)) {
				Directory.CreateDirectory(info.DestinationPath);
			}

			foreach (FileEntry entry in _grf.FileTable.EntriesInDirectory(backup, SearchOption.AllDirectories)) {
				if (entry.RelativePath.EndsWith(InfoName))
					continue;

				entry.ExtractFromAbsolute(GrfPath.Combine(info.DestinationPath, entry.RelativePath.ReplaceFirst(backup + "\\", "")));
			}
			
			_grf.Close();
		}

		public void RemoveBackup(string[] backups) {
			_validateOpened();

			_grf.Commands.RemoveFolders(backups);
			_grf.SyncQuickMerge(null);
			_grf.Close();
		}

		public void Stop() {
			if (!SDEAppConfiguration.BackupsManagerState) return;

			_grf.SyncQuickMerge(null);
			_grf.Close();
			_isStarted = false;
		}

		public void Backup(string file) {
			if (!SDEAppConfiguration.BackupsManagerState || !_isStarted) return;

			try {
				string relativePath = file.ReplaceFirst(GrfPath.GetDirectoryName(SdeFiles.ServerDbPath) + "\\", "");

				if (String.IsNullOrEmpty(relativePath)) {
					return;
				}

				_validateOpened();

				if (!_paths.ContainsKey(_currentId)) {
					_paths[_currentId] = _getGrfPath();
				}

				string fullPath = GrfPath.Combine(_paths[_currentId], relativePath);
				string tempFile = TemporaryFilesManager.GetTemporaryFilePath("backup_local_copy_{0:0000}");
				File.Copy(file, tempFile);

				_grf.Commands.AddFileAbsolute(fullPath, tempFile);
			}
			catch { }
		}

		private string _getGrfPath() {
			return DateTime.Now.ToFileTimeUtc().ToString(CultureInfo.InvariantCulture);
		}

		public List<string> GetBackupFiles() {
			_validateOpened();

			return _grf.Container.Diretories.Select(p => GrfPath.SplitDirectories(p)[0]).Distinct().ToList();
		}

		public List<Backup> GetBackups() {
			return GetBackupFiles().Select(p => new Backup(p)).ToList();
		}

		public void Export(string folder, string backup) {
			_validateOpened();

			foreach (FileEntry entry in _grf.FileTable.EntriesInDirectory(backup, SearchOption.AllDirectories)) {
				if (entry.RelativePath.EndsWith(InfoName))
					continue;

				entry.ExtractFromAbsolute(GrfPath.Combine(folder, entry.RelativePath.ReplaceFirst(backup + "\\", "")));
			}

			_grf.Close();
		}
	}
}
