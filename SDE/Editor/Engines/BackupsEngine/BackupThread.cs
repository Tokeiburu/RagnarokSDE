using System;
using System.Collections.Generic;
using System.Linq;
using ErrorManager;
using GRF.Core;
using GRF.IO;
using GRF.System;
using GRF.Threading;

namespace SDE.Editor.Engines.BackupsEngine {
	public class BackupThread : PausableThread {
		private int _backupId;
		private readonly Dictionary<int, Dictionary<string, string>> _pendingBackups = new Dictionary<int, Dictionary<string, string>>();

		public bool IsCrashed { get; set; }

		public void AddNewBackup(int backupId, Dictionary<string, string> dictionary) {
			_pendingBackups[backupId] = dictionary;
			Resume();
		}

		public void Start() {
			GrfThread.Start(_start);
		}

		public void _start() {
			try {
				while (true) {
					Pause();

					var keys = _pendingBackups.Keys.ToList();

					foreach (var key in keys.Where(p => p > _backupId).OrderBy(p => p)) {
						string grfPath = GrfPath.Combine(Settings.TempPath, "backup_" + _backupId + ".grf");

						using (GrfHolder grf = new GrfHolder(grfPath, GrfLoadOptions.New)) {
							foreach (var entry in _pendingBackups[key]) {
								grf.Commands.AddFileAbsolute(entry.Value, entry.Key);
							}

							grf.Save();
							grf.Reload();

							// Currently saving another file, stop before breaking anything
							if (BackupEngine.Instance.IsStarted)
								break;

							// Save to primary backup file
							BackupEngine.Instance.Grf.QuickMerge(grf);
							BackupEngine.Instance.Grf.Reload();
						}

						GrfPath.Delete(grfPath);

						// Deletes unused files, it's not necessary but it can pile up quickly
						foreach (var file in _pendingBackups[key].Keys) {
							GrfPath.Delete(file);
						}

						_backupId = key;
					}

					// Remove old backups
					if (!BackupEngine.Instance.IsStarted) {
						List<string> paths = BackupEngine.Instance.GetBackupFiles().OrderBy(long.Parse).ToList();

						// Only delete if it's worth it.
						if (paths.Count > BackupEngine.MaximumNumberOfBackups + 15) {
							while (paths.Count > BackupEngine.MaximumNumberOfBackups) {
								BackupEngine.Instance.Grf.Commands.RemoveFolder(paths[0]);
								paths.RemoveAt(0);
							}

							BackupEngine.Instance.Grf.QuickSave();
						}

						// The GRF must always be closed
						BackupEngine.Instance.Grf.Close();
					}
				}
			}
			catch (Exception err) {
				IsCrashed = true;
				ErrorHandler.HandleException(err);
				ErrorHandler.HandleException("The backup engine has failed to save your files. It will be disabled until you reload the application.", ErrorLevel.NotSpecified);
			}
		}
	}
}