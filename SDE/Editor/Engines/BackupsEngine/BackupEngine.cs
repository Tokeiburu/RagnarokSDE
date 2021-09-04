using GRF.Core;
using GRF.Core.GroupedGrf;
using GRF.IO;
using GRF.System;
using SDE.ApplicationConfiguration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Utilities;
using Utilities.Extension;

namespace SDE.Editor.Engines.BackupsEngine
{
    public sealed class BackupEngine
    {
        public const int MaximumNumberOfBackups = 50;
        public const string InfoName = "restore.inf";
        private static readonly BackupEngine _instance = new BackupEngine();

        private readonly Dictionary<int, string> _paths = new Dictionary<int, string>();
        private readonly Dictionary<int, Dictionary<string, string>> _localToGrfPath = new Dictionary<int, Dictionary<string, string>>();
        private int _currentId;
        private GrfHolder _grf;
        public bool IsStarted { get; set; }
        private readonly BackupThread _backupThread = new BackupThread();

        /// <summary>
        /// Initializes the <see cref="BackupEngine"/> class.
        /// </summary>
        static BackupEngine()
        {
            Instance.Init();
            TemporaryFilesManager.UniquePattern("backup_local_copy_{0:0000}");
        }

        private BackupEngine()
        {
        }

        private string _grfPath
        {
            get { return GrfPath.Combine(SdeAppConfiguration.ProgramDataPath, "_backups.grf"); }
        }

        public GrfHolder Grf
        {
            get
            {
                _validateOpened();
                return _grf;
            }
        }

        public static BackupEngine Instance
        {
            get { return _instance; }
        }

        public void Init()
        {
            _grf = new GrfHolder(_grfPath, GrfLoadOptions.OpenOrNew);
            _grf.Close();
            _backupThread.Start();
        }

        public void Start(string dbPath)
        {
            if (!SdeAppConfiguration.BackupsManagerState || _backupThread.IsCrashed) return;
            if (dbPath == null) throw new ArgumentNullException("dbPath");

            _currentId++;

            _validateOpened();

            BackupInfo info = new BackupInfo(new TextConfigAsker(new byte[] { }));
            info.DestinationPath = GrfPath.GetDirectoryName(dbPath);

            if (!_paths.ContainsKey(_currentId))
            {
                _paths[_currentId] = _getGrfPath();
            }

            if (!_localToGrfPath.ContainsKey(_currentId))
            {
                _localToGrfPath[_currentId] = new Dictionary<string, string>();
            }

            string fullPath = GrfPath.CombineUrl(_paths[_currentId], InfoName);
            string tempFile = TemporaryFilesManager.GetTemporaryFilePath("backup_local_copy_{0:0000}");
            File.WriteAllBytes(tempFile, info.GetData());
            _localToGrfPath[_currentId][tempFile] = fullPath;

            IsStarted = true;
        }

        private void _validateOpened()
        {
            if (!_grf.IsOpened)
                _grf.Open(_grfPath, GrfLoadOptions.OpenOrNew);
        }

        /// <summary>
        /// Removes a backup.
        /// </summary>
        /// <param name="backup">The backup path.</param>
        /// <param name="delayed">Save after removing the backup or not.</param>
        /// <exception cref="ArgumentNullException">backup</exception>
        public void RemoveBackup(string backup, bool delayed)
        {
            if (backup == null) throw new ArgumentNullException("backup");

            _validateOpened();
            _grf.Commands.RemoveFolder(backup);

            if (!delayed)
            {
                _grf.QuickSave();
                _grf.Close();
            }
        }

        /// <summary>
        /// Removes backups.
        /// </summary>
        /// <param name="backups">The backup paths.</param>
        /// <exception cref="ArgumentNullException">backups</exception>
        public void RemoveBackup(string[] backups)
        {
            if (backups == null) throw new ArgumentNullException("backups");

            _validateOpened();

            _grf.Commands.RemoveFolders(backups);
            _grf.QuickSave();
            _grf.Close();
        }

        /// <summary>
        /// Restores the specified backup.
        /// </summary>
        /// <param name="backup">The backup path.</param>
        /// <exception cref="ArgumentNullException">backup</exception>
        public void Restore(string backup)
        {
            if (backup == null) throw new ArgumentNullException("backup");

            _validateOpened();

            BackupInfo info = new BackupInfo(new ReadonlyConfigAsker(_grf.FileTable[GrfPath.Combine(backup, InfoName)].GetDecompressedData()));

            if (!Directory.Exists(info.DestinationPath))
            {
                Directory.CreateDirectory(info.DestinationPath);
            }

            foreach (FileEntry entry in _grf.FileTable.EntriesInDirectory(backup, SearchOption.AllDirectories))
            {
                if (entry.RelativePath.EndsWith(InfoName))
                    continue;

                entry.ExtractFromAbsolute(GrfPath.Combine(info.DestinationPath, entry.RelativePath.ReplaceFirst(backup + "\\", "")));
            }

            _grf.Close();
        }

        public void Stop()
        {
            if (!SdeAppConfiguration.BackupsManagerState || _backupThread.IsCrashed) return;

            _backupThread.AddNewBackup(_currentId, _localToGrfPath[_currentId]);
            //_grf.QuickSave();
            IsStarted = false;
        }

        public void BackupClient(string file, byte[] data)
        {
            if (!SdeAppConfiguration.BackupsManagerState || !IsStarted || _backupThread.IsCrashed) return;
            if (file == null) throw new ArgumentNullException("file");
            if (data == null) return;

            try
            {
                string relativePath = GrfPath.Combine("client", Path.GetFileName(file)); //.ReplaceFirst(GrfPath.GetDirectoryName(ProjectConfiguration.DatabasePath) + (isFtp ? "/" : "\\"), "");

                if (String.IsNullOrEmpty(relativePath))
                {
                    return;
                }

                _validateOpened();

                string tempFile = TemporaryFilesManager.GetTemporaryFilePath("backup_local_copy_{0:0000}");
                File.WriteAllBytes(tempFile, data);

                string fullPath = GrfPath.Combine(_paths[_currentId], relativePath);
                _localToGrfPath[_currentId][tempFile] = fullPath;
            }
            catch
            {
            }
        }

        public void BackupClient(string file, MultiGrfReader mGrf)
        {
            if (!SdeAppConfiguration.BackupsManagerState || !IsStarted || _backupThread.IsCrashed) return;
            if (file == null) throw new ArgumentNullException("file");

            BackupClient(file, mGrf.GetData(file));
        }

        public void Backup(string file)
        {
            if (!SdeAppConfiguration.BackupsManagerState || !IsStarted || _backupThread.IsCrashed) return;
            if (file == null) throw new ArgumentNullException("file");

            try
            {
                string relativePath = file.ReplaceFirst(GrfPath.GetDirectoryName(ProjectConfiguration.DatabasePath) + IOHelper.Slash, "");

                if (String.IsNullOrEmpty(relativePath))
                {
                    return;
                }

                _validateOpened();

                string fullPath = GrfPath.Combine(_paths[_currentId], relativePath);
                string tempFile = TemporaryFilesManager.GetTemporaryFilePath("backup_local_copy_{0:0000}");
                IOHelper.Copy(file, tempFile);

                _localToGrfPath[_currentId][tempFile] = fullPath;
            }
            catch
            {
            }
        }

        private string _getGrfPath()
        {
            return DateTime.Now.ToFileTimeUtc().ToString(CultureInfo.InvariantCulture);
        }

        public List<string> GetBackupFiles()
        {
            _validateOpened();

            return _grf.FileTable.Directories.Select(p => GrfPath.SplitDirectories(p)[0]).Distinct().ToList();
        }

        public List<Backup> GetBackups()
        {
            return GetBackupFiles().Select(p => new Backup(p)).ToList();
        }

        /// <summary>
        /// Exports the specified folder from the GRF.
        /// </summary>
        /// <param name="folder">The extraction folder path.</param>
        /// <param name="backup">The backup path.</param>
        /// <exception cref="ArgumentNullException">
        /// folder
        /// or
        /// backup
        /// </exception>
        public void Export(string folder, string backup)
        {
            if (folder == null) throw new ArgumentNullException("folder");
            if (backup == null) throw new ArgumentNullException("backup");

            _validateOpened();

            foreach (FileEntry entry in _grf.FileTable.EntriesInDirectory(backup, SearchOption.AllDirectories))
            {
                if (entry.RelativePath.EndsWith(InfoName))
                    continue;

                entry.ExtractFromAbsolute(GrfPath.Combine(folder, entry.RelativePath.ReplaceFirst(backup + "\\", "")));
            }

            _grf.Close();
        }
    }
}