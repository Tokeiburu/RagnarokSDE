using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SDE.ApplicationConfiguration;
using SDE.Core;
using Tamir.SharpSsh.jsch;

namespace SDE.Editor.Engines {
	public abstract class FileManager {
		public string FullPath { get; protected set; }

		protected FileManager(string path) {
			FullPath = path;
		}

		public static FileManager Get(string path) {
			FtpUrlInfo info = new FtpUrlInfo(path);

			if (info.Scheme == "sftp") {
				return new SftpFileManager(path);
			}
			if (info.Scheme == "ftp") {
				throw new Exception("Unsupported protocol (yes, FTP isn't supported, yet).");
				//return new FtpFileManager(path);
				//return new SftpFileManager(path);
			}
			if (info.Scheme == "file") {
				return new SystemFileManager(path);
			}

			return new SystemFileManager(path);
		}

		public abstract void Close();
		public abstract void Open();
		public abstract bool Delete(string path);
		public abstract void Copy(string sourceFile, string destFile);
		public abstract void WriteAllText(string path, string content, Encoding encoding);
		public abstract bool Exists(string path);
		public abstract byte[] ReadAllBytes(string path);
		public abstract Stream OpenRead(string path);
		public abstract bool CanUseBackupEngine();
		public abstract bool HasBeenMapped();
		public abstract bool SameFile(string file1, string file2);
		public abstract List<ChannelSftp.LsEntry> GetDirectories(string path);
	}

	public static class FtpHelper {
		private static FileManager __interface;

		private static FileManager _interface {
			get { return __interface ?? (__interface = new SystemFileManager("")); }
			set { __interface = value; }
		}

		public static string Slash {
			get { return _interface is SystemFileManager ? "\\" : "/"; }
		}

		public static void SetupFileManager() {
			if (_interface != null) {
				_interface.Close();
			}

			_interface = FileManager.Get(ProjectConfiguration.DatabasePath);
			_interface.Open();
		}

		public static bool Delete(string path) {
			return _interface.Delete(path);
		}

		public static void Copy(string sourceFile, string destFile) {
			_interface.Copy(sourceFile, destFile);
		}

		public static void WriteAllText(string path, string content) {
			Encoding encoding = SdeAppConfiguration.EncodingServer;

			if (encoding.CodePage == Encoding.UTF8.CodePage)
				encoding = Extensions.Utf8NoBom;

			_interface.WriteAllText(path, content, encoding);
		}

		public static bool Exists(string path) {
			return _interface.Exists(path);
		}

		public static byte[] ReadAllBytes(string path) {
			return _interface.ReadAllBytes(path);
		}

		public static Stream OpenRead(string path) {
			return _interface.OpenRead(path);
		}

		public static void Close() {
			_interface.Close();
			_interface = null;
		}

		public static bool CanBackup() {
			return _interface.CanUseBackupEngine();
		}

		public static bool IsSystemFile(string path) {
			var fm = FileManager.Get(path);
			return fm is SystemFileManager;
		}

		public static bool HasBeenMapped() {
			return _interface.HasBeenMapped();
		}

		public static bool SameFile(string file1, string file2) {
			return _interface.SameFile(file1, file2);
		}
	}
}