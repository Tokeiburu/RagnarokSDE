using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GRF.IO;
using GRF.System;
using SDE.Editor.Generic.Core;
using Tamir.SharpSsh;
using Tamir.SharpSsh.jsch;
using Utilities.Hash;

namespace SDE.Editor.Engines {
	public class SftpFileManager : FileManager {
		private readonly Sftp _sftp;
		private readonly FtpUrlInfo _info;
		private bool _hasBeenMapped;
		private SftpDb _db;
		private readonly Dictionary<string, ChannelSftp.LsEntry> _entries = new Dictionary<string, ChannelSftp.LsEntry>();

		static SftpFileManager() {
			TemporaryFilesManager.UniquePattern("sftp_{0:0000}.file");
		}

		private string _getTempPath() {
			return TemporaryFilesManager.GetTemporaryFilePath("sftp_{0:0000}.file");
		}

		public SftpFileManager(string path)
			: base(path) {
			_info = new FtpUrlInfo(path);
			_sftp = new Sftp(_info.Host, ProjectConfiguration.FtpUsername, ProjectConfiguration.FtpPassword);
		}

		public override bool Delete(string path) {
			return GrfPath.Delete(path);
		}

		public override void Close() {
			DbDebugHelper.OnSftpUpdate("closed.");
			_sftp.Close();
		}

		public override void Open() {
			DbDebugHelper.OnSftpUpdate("connecting...");
			_sftp.Connect(_info.Port);
			DbDebugHelper.OnSftpUpdate("connected.");
			_db = new SftpDb(FullPath);
		}

		private string _convertPath(string path) {
			return new FtpUrlInfo(path).Path;
		}

		public override void Copy(string sourceFile, string destFile) {
			_map();
			FtpUrlInfo urlSource = new FtpUrlInfo(sourceFile);
			FtpUrlInfo urlDest = new FtpUrlInfo(destFile);

			if (urlSource.Scheme == "sftp") {
				var entry = _entries[urlSource.Path];

				if (_db.Exists(urlSource.Path, entry)) {
					var data = _db.Get(urlSource.Path);
					File.WriteAllBytes(destFile, data);
				}
				else {
					DbDebugHelper.OnSftpUpdate("downloading " + urlSource.Path);
					_sftp.Get(urlSource.Path, destFile);
					DbDebugHelper.OnSftpUpdate("caching " + urlSource.Path);
					_db.Set(destFile, urlSource.Path, entry);
				}
			}
			else {
				if (_entries.ContainsKey(urlDest.Path)) {
					var entry = _entries[urlDest.Path];

					if (_db.Exists(urlDest.Path, entry)) {
						byte[] dataDest = _db.Get(urlDest.Path);
						byte[] dataSource = File.ReadAllBytes(sourceFile);

						Crc32Hash hash = new Crc32Hash();

						if (dataDest.Length == dataSource.Length && hash.ComputeHash(dataDest) == hash.ComputeHash(dataSource)) {
							// do not upload the same file
						}
						else {
							DbDebugHelper.OnSftpUpdate("uploading " + urlDest.Path);
							_sftp.Put(sourceFile, urlDest.Path);
							DbDebugHelper.OnSftpUpdate("updating cached version for " + urlDest.Path);
							var newEntry = _sftp.GetFileListAdv(urlDest.Path)[0];
							_db.Set(sourceFile, urlDest.Path, newEntry);
							_entries[urlDest.Path] = newEntry;
						}
					}
					else {
						DbDebugHelper.OnSftpUpdate("uploading " + urlDest.Path);
						_sftp.Put(sourceFile, urlDest.Path);
						DbDebugHelper.OnSftpUpdate("updating cached version for " + urlDest.Path);
						var newEntry = _sftp.GetFileListAdv(urlDest.Path)[0];
						_db.Set(sourceFile, urlDest.Path, newEntry);
						_entries[urlDest.Path] = newEntry;
					}
				}
				else {
					DbDebugHelper.OnSftpUpdate("uploading new file " + urlDest.Path);
					_sftp.Put(sourceFile, urlDest.Path);
				}
			}
		}

		public override void WriteAllText(string path, string content, Encoding encoding) {
			var dest = _getTempPath();
			File.WriteAllText(dest, content, encoding);
			Copy(dest, path);
		}

		private void _map() {
			if (_hasBeenMapped) return;

			// List all files
			var basePath = GrfPath.GetDirectoryName(_info.Path).TrimEnd('/');

			DbDebugHelper.OnSftpUpdate("listing files...");
			List<ChannelSftp.LsEntry> files = _sftp.GetFileListAdv(basePath);

			DbDebugHelper.OnSftpUpdate("listing " + basePath);

			foreach (var file in files) {
				var cur = file.getFilename();

				if (file.getAttrs().isDir() && cur != "." && cur != ".." &&
				    (cur == "re" || cur == "pre-re" || cur == "import")) {
					var subPath = basePath + "/" + cur;
					DbDebugHelper.OnSftpUpdate("listing " + subPath);

					List<ChannelSftp.LsEntry> filesSub = _sftp.GetFileListAdv(subPath);

					foreach (var subfile in filesSub) {
						if (!subfile.getAttrs().isDir())
							_entries[subPath + "/" + subfile.getFilename()] = subfile;
					}
				}
				else {
					_entries[basePath + "/" + cur] = file;
				}
			}

			DbDebugHelper.OnSftpUpdate("" + _entries.Count + " files found.");
			_hasBeenMapped = true;
		}

		public override bool Exists(string path) {
			_map();

			try {
				return _entries.ContainsKey(_convertPath(path).Replace("\\", "/"));
			}
			catch {
				FtpUrlInfo url = new FtpUrlInfo(path);
				return _entries.ContainsKey(url.Path.Replace("\\", "/"));
			}
		}

		public override byte[] ReadAllBytes(string path) {
			var temp = _getTempPath();

			FtpUrlInfo urlSource = new FtpUrlInfo(path);
			var entry = _entries[urlSource.Path];

			if (_db.Exists(urlSource.Path, entry)) {
				return _db.Get(urlSource.Path);
			}

			DbDebugHelper.OnSftpUpdate("downloading " + urlSource.Path);
			_sftp.Get(urlSource.Path, temp);
			DbDebugHelper.OnSftpUpdate("caching " + urlSource.Path);
			_db.Set(temp, urlSource.Path, entry);

			return File.ReadAllBytes(temp);
		}

		public override Stream OpenRead(string path) {
			var temp = _getTempPath();

			FtpUrlInfo urlSource = new FtpUrlInfo(path);
			var entry = _entries[urlSource.Path];

			if (_db.Exists(urlSource.Path, entry)) {
				File.WriteAllBytes(temp, _db.Get(urlSource.Path));
				return File.OpenRead(temp);
			}

			DbDebugHelper.OnSftpUpdate("downloading " + urlSource.Path);
			_sftp.Get(urlSource.Path, temp);
			DbDebugHelper.OnSftpUpdate("caching " + urlSource.Path);
			_db.Set(temp, urlSource.Path, entry);

			return File.OpenRead(temp);
		}

		public override bool CanUseBackupEngine() {
			return false;
		}

		public override bool HasBeenMapped() {
			return _hasBeenMapped;
		}

		public override bool SameFile(string file1, string file2) {
			return false;
		}

		public override List<ChannelSftp.LsEntry> GetDirectories(string path) {
			path = path.Replace("\\", "/");
			DbDebugHelper.OnSftpUpdate("listing " + path);
			return _sftp.GetFileListAdv(path).Where(p => p.getAttrs().isDir()).ToList();
		}
	}
}