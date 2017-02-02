namespace SDE.Editor.Engines {
	//public class FtpFileManager : FileManager {
	//	private readonly FtpClient _ftp;
	//	private readonly FtpUrlInfo _info;
	//	private bool _hasBeenMapped;
	//	private SftpDb _db;
	//	private readonly Dictionary<string, ChannelSftp.LsEntry> _entries = new Dictionary<string, ChannelSftp.LsEntry>();
	//
	//	static FtpFileManager() {
	//		TemporaryFilesManager.UniquePattern("ftp_{0:0000}.file");
	//	}
	//
	//	private string _getTempPath() {
	//		return TemporaryFilesManager.GetTemporaryFilePath("ftp_{0:0000}.file");
	//	}
	//
	//	public FtpFileManager(string path)
	//		: base(path) {
	//		_info = new FtpUrlInfo(path);
	//		_ftp = new FtpClient();
	//	}
	//
	//	public override bool Delete(string path) {
	//		return GrfPath.Delete(path);
	//	}
	//
	//	public override void Close() {
	//		_ftp.Dispose();
	//	}
	//
	//	public override void Open() {
	//		_ftp.Port = _info.Port;
	//		_ftp.Credentials = new System.Net.NetworkCredential(_info.Username, _info.Password);
	//		_ftp.Host = _info.Host;
	//		_ftp.Connect();
	//		_db = new SftpDb(FullPath);
	//	}
	//
	//	private string _convertPath(string path) {
	//		return new FtpUrlInfo(path).Path;
	//	}
	//
	//	public override void Copy(string sourceFile, string destFile) {
	//		_map();
	//		FtpUrlInfo urlSource = new FtpUrlInfo(sourceFile);
	//		FtpUrlInfo urlDest = new FtpUrlInfo(destFile);
	//
	//		if (urlSource.Scheme == "sftp") {
	//			var entry = _entries[urlSource.Path];
	//
	//			if (_db.Exists(urlSource.Path, entry)) {
	//				var data = _db.Get(urlSource.Path);
	//				File.WriteAllBytes(destFile, data);
	//			}
	//			else {
	//				_ftp.OpenRead()
	//				_sftp.Get(urlSource.Path, destFile);
	//				_db.Set(destFile, urlSource.Path, entry);
	//			}
	//		}
	//		else {
	//			if (_entries.ContainsKey(urlDest.Path)) {
	//				var entry = _entries[urlDest.Path];
	//
	//				if (_db.Exists(urlDest.Path, entry)) {
	//					byte[] dataDest = _db.Get(urlDest.Path);
	//					byte[] dataSource = File.ReadAllBytes(sourceFile);
	//
	//					Crc32Hash hash = new Crc32Hash();
	//
	//					if (dataDest.Length == dataSource.Length && hash.ComputeHash(dataDest) == hash.ComputeHash(dataSource)) {
	//						// do not upload the same file
	//					}
	//					else {
	//						_sftp.Put(sourceFile, urlDest.Path);
	//						var newEntry = _sftp.GetFileListAdv(urlDest.Path)[0];
	//						_db.Set(sourceFile, urlDest.Path, newEntry);
	//						_entries[urlDest.Path] = newEntry;
	//					}
	//				}
	//			}
	//			else {
	//				_sftp.Put(sourceFile, urlDest.Path);
	//			}
	//		}
	//	}
	//
	//	public override void WriteAllText(string path, string content, Encoding encoding) {
	//		var dest = _getTempPath();
	//		File.WriteAllText(dest, content, encoding);
	//		Copy(dest, path);
	//	}
	//
	//	private void _map() {
	//		if (_hasBeenMapped) return;
	//
	//		// List all files
	//		var basePath = GrfPath.GetDirectoryName(_info.Path).TrimEnd('/');
	//		List<ChannelSftp.LsEntry> files = _sftp.GetFileListAdv(basePath);
	//
	//		foreach (var file in files) {
	//			var cur = file.getFilename();
	//
	//			if (file.getAttrs().isDir() && cur != "." && cur != ".." &&
	//				(cur == "re" || cur == "pre-re" || cur == "import")) {
	//				var subPath = basePath + "/" + cur;
	//
	//				List<ChannelSftp.LsEntry> filesSub = _sftp.GetFileListAdv(subPath);
	//
	//				foreach (var subfile in filesSub) {
	//					if (!subfile.getAttrs().isDir())
	//						_entries[subPath + "/" + subfile.getFilename()] = subfile;
	//				}
	//			}
	//			else {
	//				_entries[basePath + "/" + cur] = file;
	//			}
	//		}
	//
	//		_hasBeenMapped = true;
	//	}
	//
	//	public override bool Exists(string path) {
	//		_map();
	//
	//		try {
	//			return _entries.ContainsKey(_convertPath(path).Replace("\\", "/"));
	//		}
	//		catch {
	//			FtpUrlInfo url = new FtpUrlInfo(path);
	//			return _entries.ContainsKey(url.Path.Replace("\\", "/"));
	//		}
	//	}
	//
	//	public override byte[] ReadAllBytes(string path) {
	//		var temp = _getTempPath();
	//
	//		FtpUrlInfo urlSource = new FtpUrlInfo(path);
	//		var entry = _entries[urlSource.Path];
	//
	//		if (_db.Exists(urlSource.Path, entry)) {
	//			return _db.Get(urlSource.Path);
	//		}
	//
	//		_sftp.Get(urlSource.Path, temp);
	//		_db.Set(temp, urlSource.Path, entry);
	//
	//		return File.ReadAllBytes(temp);
	//	}
	//
	//	public override Stream OpenRead(string path) {
	//		var temp = _getTempPath();
	//
	//		FtpUrlInfo urlSource = new FtpUrlInfo(path);
	//		var entry = _entries[urlSource.Path];
	//
	//		if (_db.Exists(urlSource.Path, entry)) {
	//			File.WriteAllBytes(temp, _db.Get(urlSource.Path));
	//			return File.OpenRead(temp);
	//		}
	//
	//		_sftp.Get(urlSource.Path, temp);
	//		_db.Set(temp, urlSource.Path, entry);
	//
	//		return File.OpenRead(temp);
	//	}
	//
	//	public override bool CanUseBackupEngine() {
	//		return false;
	//	}
	//
	//	public override bool HasBeenMapped() {
	//		return _hasBeenMapped;
	//	}
	//
	//	public override List<ChannelSftp.LsEntry> GetDirectories(string path) {
	//		path = path.Replace("\\", "/");
	//		return _sftp.GetFileListAdv(path).Where(p => p.getAttrs().isDir()).ToList();
	//	}
	//}
}