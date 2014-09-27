using System.IO;
using System.Linq;
using ErrorManager;
using GRF.IO;
using GRF.System;
using SDE.Tools.DatabaseEditor.Generic.Core;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using Utilities;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders {
	public static class AllLoaders {
		private static readonly TkDictionary<string, string> _storedFiles = new TkDictionary<string, string>();

		static AllLoaders() {
			TemporaryFilesManager.UniquePattern("sdb_store_{0:0000}.dat");
		}


		public static void StoreFile(string path) {
			if (path == null)
				return;

			if (File.Exists(path)) {
				string temp = TemporaryFilesManager.GetTemporaryFilePath("sdb_store_{0:0000}.dat");
				_storedFiles[path] = temp;
				File.Copy(path, temp);
			}
			else {
				_storedFiles[path] = null;
			}
		}

		public static void ClearStoredFiles() {
			_storedFiles.Clear();
		}

		public static void UpdateStoredFiles() {
			foreach (var pair in _storedFiles) {
				if (File.Exists(pair.Key)) {
					if (GrfPath.Delete(pair.Value)) {
						File.Copy(pair.Key, pair.Value);
					}
				}
			}
		}

		public static string GetStoredFile(string path) {
			if (path == null)
				return null;

			return _storedFiles[path];
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

		/// <summary>
		/// Goes up in the parent folder until the path is found.
		/// </summary>
		/// <param name="db">The sub path to find.</param>
		/// <returns>The path found.</returns>
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
			return files.Select(file => Path.Combine(path, file)).FirstOrDefault(File.Exists);
		}

		private static string _getInCurrentPath(string fileInput) {
			string path = SdeFiles.ServerDbPath.Filename;
			string[] files = fileInput.GetExtension() == null ? new string[] {fileInput + ".txt", fileInput + ".conf"} : new string[] {fileInput};
			return files.Select(file => Path.Combine(path, file)).FirstOrDefault(File.Exists);
		}

		public static string DetectPath(string toString) {
			if (File.Exists(toString))
				return toString;

			string path = _getInCurrentPath(toString);

			if (path != null)
				return path;

			path = _getInParentPath(toString);
			return path;
		}

		public static string DetectPath(ServerDbs toString, bool allowAlernative = true) {
			if (File.Exists(toString))
				return toString;

			string path = _getInCurrentPath(toString);

			if (path != null) {
				toString.UseSubPath = true;
				return path;
			}

			path = _getInParentPath(toString);

			if (path == null && toString.AlternativeName != null && allowAlernative) {
				return DetectPath(toString.AlternativeName);
			}

			toString.UseSubPath = false;
			return path;
		}

		/// <summary>
		/// Gets the type of the server of the currently loaded DB.
		/// </summary>
		/// <returns>The current server type</returns>
		public static ServerType GetServerType() {
			return DetectPath(ServerDbs.Items).IsExtension(".conf") ? ServerType.Hercules : ServerType.RAthena;
		}

		/// <summary>
		/// Gets the type of the file based on the path.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns>The file type</returns>
		public static FileType GetFileType(string path) {
			return path.IsExtension(".conf") ? FileType.Conf : FileType.Txt;
		}

		/// <summary>
		/// Determines if the current server is renewal or not.
		/// </summary>
		/// <returns></returns>
		public static bool GetIsRenewal() {
			string path = DetectPath(ServerDbs.Items);

			string parent = Path.GetDirectoryName(path);

			if (parent != null && parent.EndsWith("pre-re"))
				return false;
			return true;
		}
	}
}
