using System;
using System.IO;
using System.Linq;
using Database;
using ErrorManager;
using GRF.IO;
using GRF.System;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.View;
using Utilities;
using Utilities.Extension;

namespace SDE.Editor.Generic.Core {
	public static class DbPathLocator {
		private static readonly TkDictionary<string, string> _storedFiles = new TkDictionary<string, string>();
		private static readonly TkDictionary<string, DateTime> _lastModified = new TkDictionary<string, DateTime>();

		static DbPathLocator() {
			TemporaryFilesManager.UniquePattern("sdb_store_{0:0000}.dat");
		}

		public static void StoreFile(string path) {
			if (path == null)
				return;

			if (IOHelper.Exists(path)) {
				string temp = TemporaryFilesManager.GetTemporaryFilePath("sdb_store_{0:0000}.dat");
				_storedFiles[path] = temp;
				_lastModified[path] = new FileInfo(temp).LastWriteTime;
				IOHelper.Copy(path, temp);
			}
			else {
				_lastModified[path] = default(DateTime);
				_storedFiles[path] = null;
			}
		}

		public static void ClearStoredFiles() {
			_storedFiles.Clear();
			_lastModified.Clear();
		}

		public static string GetStoredFile(string path) {
			if (path == null)
				return null;

			return _storedFiles[path];
		}

		public static DateTime GetLastModifiedTime(string path) {
			if (path == null)
				return default(DateTime);

			return _lastModified[path];
		}

		public static void SetLastModifiedTime(string path, DateTime time) {
			if (path == null)
				return;

			_lastModified[path] = time;
		}

		public static bool GenericErrorHandler(ref int numError, object item) {
			DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "Failed to read an item.", item.ToString());
			numError--;
			if (numError < -10) {
				DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "Failed to read too many items, the db will stop loading.", ErrorLevel.Critical);
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
				string path = ProjectConfiguration.DatabasePath;

				while (path != null) {
					if (IOHelper.Exists(GrfPath.Combine(path, db))) {
						return GrfPath.Combine(path, db);
					}

					path = GrfPath.GetDirectoryName(path);
				}

				return null;
			}
			catch {
				return null;
			}
		}

		private static string _getInParentPath(string fileInput) {
			string path = GrfPath.GetDirectoryName(ProjectConfiguration.DatabasePath);
			string[] files = fileInput.GetExtension() == null ? new string[] { fileInput + ".yml", fileInput + ".txt", fileInput + ".conf" } : new string[] { fileInput };
			return files.Select(file => GrfPath.CombineUrl(path, file)).FirstOrDefault(IOHelper.Exists);
		}

		private static string _getInCurrentPath(string fileInput) {
			string path = ProjectConfiguration.DatabasePath;
			string[] files = fileInput.GetExtension() == null ? new string[] { fileInput + ".yml", fileInput + ".txt", fileInput + ".conf" } : new string[] { fileInput };
			return files.Select(file => GrfPath.CombineUrl(path, file)).FirstOrDefault(IOHelper.Exists);
		}

		public static string DetectPath(string toString) {
			if (IOHelper.Exists(toString))
				return toString;

			string path = _getInCurrentPath(toString);

			if (path != null)
				return path;

			path = _getInParentPath(toString);
			return path;
		}

		private static bool _yamlMob = false;

		public static void SetYamlMob(bool val) {
			_yamlMob = val;
		}

		public static bool IsYamlMob() {
			return _yamlMob;
		}

		public static string DetectPath(ServerDbs toString, bool allowAlernative = true) {
			if (toString == null)
				return null;

			if (toString.IsClientSide && toString.ClientSidePath != null) {
				string dest = toString.ClientSidePath();

				if (!String.IsNullOrEmpty(dest)) {
					var tkpath = SdeEditor.Instance.ProjectDatabase.MetaGrf.FindTkPath(dest);

					if (tkpath == null)
						return null;

					return tkpath.FilePath;
				}
			}

			if (IOHelper.Exists(toString))
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
			if (GetIsNova())
				return ServerType.RAthena;

			return DetectPath(ServerDbs.Items).IsExtension(".conf") ? ServerType.Hercules : ServerType.RAthena;
		}

		/// <summary>
		/// Gets the type of the file based on the path.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns>The file type</returns>
		public static FileType GetFileType(string path) {
			return path.IsExtension(".conf") ? FileType.Conf : (path.IsExtension(".yml") ? FileType.Yaml : FileType.Txt);
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

		/// <summary>
		/// Determines if the current server is NovaRO or not.
		/// </summary>
		/// <returns></returns>
		public static bool GetIsNova() {
			return File.Exists(GrfPath.CombineUrl(Path.GetDirectoryName(ProjectConfiguration.DatabasePath), "lapine_db.conf"));
		}

		public static bool UseAlternative(ServerDbs toString) {
			if (IOHelper.Exists(toString))
				return false;

			string path = _getInCurrentPath(toString);

			if (path != null) {
				toString.UseSubPath = true;
				return false;
			}

			path = _getInParentPath(toString);

			if (path == null && toString.AlternativeName != null) {
				path = DetectPath(toString.AlternativeName);

				return path != null;
			}

			toString.UseSubPath = false;
			return false;
		}
	}
}