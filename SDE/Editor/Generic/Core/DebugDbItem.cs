using System;
using System.IO;
using System.Linq;
using Database;
using ErrorManager;
using GRF.IO;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines;
using SDE.Editor.Engines.BackupsEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using Utilities.Extension;

namespace SDE.Editor.Generic.Core {
	public static class DbDebugHelper {
		#region Delegates
		public delegate void DbEventHandler(object sender, ServerDbs primaryTable, string subFile, BaseDb db);

		public delegate void DbUpdateEventHandler(object sender, string message);

		public delegate void DbWriteUpdateEventHandler(object sender, ServerDbs primaryTable, string subFile, BaseDb db, string message);
		#endregion

		public static event DbEventHandler Saved;
		public static event DbEventHandler Loaded;
		public static event DbEventHandler Cleared;
		public static event DbUpdateEventHandler Update;
		public static event DbUpdateEventHandler SftpUpdate;
		public static event DbWriteUpdateEventHandler Update2;
		public static event DbEventHandler ExceptionThrown;
		public static event DbEventHandler StoppedLoading;
		public static event DbWriteUpdateEventHandler WriteStatusUpdate;

		public static void OnSaved(ServerDbs primarytable, string subfile, BaseDb db) {
			DbEventHandler handler = Saved;
			if (handler != null) handler(null, primarytable, subfile, db);
		}

		public static void OnLoaded(ServerDbs primarytable, string subfile, BaseDb db) {
			DbEventHandler handler = Loaded;
			if (handler != null) handler(null, primarytable, subfile, db);
		}

		public static void OnUpdate(string message) {
			DbUpdateEventHandler handler = Update;
			if (handler != null) handler(null, message);
		}

		public static void OnSftpUpdate(string message) {
			DbUpdateEventHandler handler = SftpUpdate;
			if (handler != null) handler(null, message);
		}

		public static void OnUpdate(ServerDbs primarytable, string subfile, string message) {
			DbWriteUpdateEventHandler handler = Update2;
			if (handler != null) handler(null, primarytable, subfile, null, message);
		}

		public static void OnCleared(ServerDbs primarytable, string subfile, BaseDb db) {
			DbEventHandler handler = Cleared;
			if (handler != null) handler(null, primarytable, subfile, db);
		}

		public static void OnWriteStatusUpdate(ServerDbs primarytable, string subfile, BaseDb db, string message) {
			DbWriteUpdateEventHandler handler = WriteStatusUpdate;
			if (handler != null) handler(null, primarytable, subfile, db, message);
		}

		public static void OnStoppedLoading(ServerDbs primarytable, string subfile, BaseDb db) {
			DbEventHandler handler = StoppedLoading;
			if (handler != null) handler(null, primarytable, subfile, db);
		}

		public static void OnExceptionThrown(ServerDbs primarytable, string subfile, BaseDb db) {
			DbEventHandler handler = ExceptionThrown;
			if (handler != null) handler(null, primarytable, subfile, db);
		}

		public static void DetachEvents() {
			Saved = null;
			Loaded = null;
			Cleared = null;
			Update = null;
			SftpUpdate = null;
			Update2 = null;
			ExceptionThrown = null;
			StoppedLoading = null;
			WriteStatusUpdate = null;
		}
	}

	public abstract class DbDebugItemBase {
		public int NumberOfErrors { get; protected set; }
		public bool IsRenewal { get; protected set; }
		public string FilePath { get; set; }
		public string OldPath { get; protected set; }
		public string SubPath { get; protected set; }
		public ServerDbs DbSource { get; set; }
		public FileType FileType { get; set; }
		public ServerType DestinationServer { get; protected set; }

		protected abstract BaseDb _bdb { get; }

		public bool ReportException(Exception item) {
			DbIOErrorHandler.HandleLoader(item, item.Message);
			NumberOfErrors--;

			if (NumberOfErrors < 0) {
				DbIOErrorHandler.Handle(item, "Failed to read too many items, the db will stop loading.", ErrorLevel.Critical);
				return false;
			}

			return true;
		}

		public void ReportIdException(string exception, object item, ErrorLevel errorLevel = ErrorLevel.Warning) {
			DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), exception, item.ToString(), errorLevel);
			DbDebugHelper.OnExceptionThrown(DbSource, FilePath, _bdb);
		}

		public bool ReportIdExceptionWithError(string exception, object item, int line = -1) {
			DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), exception, item.ToString(), line);
			DbDebugHelper.OnExceptionThrown(DbSource, FilePath, _bdb);
			NumberOfErrors--;

			if (NumberOfErrors < 0) {
				DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "Failed to read too many items, the db [" + DbSource + "] will stop loading.", ErrorLevel.Critical);
				DbDebugHelper.OnStoppedLoading(DbSource, FilePath, _bdb);
				return false;
			}

			return true;
		}

		public bool ReportIdException(object item) {
			return ReportIdException(item, -1);
		}

		public bool ReportIdException(object item, int line) {
			DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "Failed to read an item.", item.ToString(), line);
			DbDebugHelper.OnExceptionThrown(DbSource, FilePath, _bdb);
			NumberOfErrors--;

			if (NumberOfErrors < 0) {
				DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "Failed to read too many items, the db [" + DbSource + "] will stop loading.", ErrorLevel.Critical);
				DbDebugHelper.OnStoppedLoading(DbSource, FilePath, _bdb);
				return false;
			}

			return true;
		}

		public bool ReportException(string item) {
			DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), item);
			DbDebugHelper.OnExceptionThrown(DbSource, FilePath, _bdb);
			NumberOfErrors--;

			if (NumberOfErrors < 0) {
				DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "Failed to read too many items, the db [" + DbSource + "] will stop loading.", ErrorLevel.Critical);
				DbDebugHelper.OnStoppedLoading(DbSource, FilePath, _bdb);
				return false;
			}

			return true;
		}
	}

	/// <summary>
	/// Class used to validate load or write operations.
	/// It loads parameters that will be used by the DbWriter methods
	/// and it also calls the backup engine. This object must always
	/// be called before proceeding to any load or write operations.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	public class DbDebugItem<TKey> : DbDebugItemBase {
		public const int MaximumNumberOfAllowedExceptions = 10;

		private readonly AbstractDb<TKey> _db;

		protected override BaseDb _bdb {
			get { return _db; }
		}

		public DbDebugItem(AbstractDb<TKey> db) {
			_db = db;

			if (_db != null)
				DbSource = _db.DbSource;

			NumberOfErrors = MaximumNumberOfAllowedExceptions;
			TextFileHelper.LatestFile = null;
			TextFileHelper.LastReader = null;
		}

		public bool ForceWrite { get; set; }

		public AbstractDb<TKey> AbsractDb {
			get { return _db; }
		}

		public bool Load(ServerDbs dbSource) {
			DbSource = dbSource;
			string path = DbPathLocator.DetectPath(DbSource);

			TextFileHelper.LatestFile = path;

			if (String.IsNullOrEmpty(path)) {
				if (_db.ThrowFileNotFoundException) {
					DbIOErrorHandler.Handle(StackTraceException.GetStrackTraceException(), "File not found '" + DbSource + "'.", ErrorLevel.NotSpecified);
				}

				return false;
			}

			FileType = DbPathLocator.GetFileType(path);
			FilePath = path;
			DbPathLocator.StoreFile(FilePath);
			DbDebugHelper.OnLoaded(DbSource, FilePath, _db);
			return true;
		}

		public bool Load() {
			return Load(DbSource);
		}

		public bool Write(ServerDbs dbSource, string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect, bool isModifiedCheck = false) {
			DbSource = dbSource;
			return Write(dbPath, subPath, serverType, fileType, isModifiedCheck);
		}

		public bool Write(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect, bool isModifiedCheck = false) {
			SubPath = subPath;
			string filename = DbSource.Filename;
			string logicalPath = DbPathLocator.DetectPath(DbSource);
			DestinationServer = serverType;

			FileType = fileType;

			if ((fileType & FileType.Detect) == FileType.Detect) {
				if ((DbSource.SupportedFileType & FileType.Txt) == FileType.Txt) {
					FileType = FileType.Txt;
				}

				if ((DbSource.SupportedFileType & FileType.Conf) == FileType.Conf) {
					if (serverType == ServerType.Hercules) {
						FileType = FileType.Conf;

						// Alternative name is rAthena specific
						if (DbSource.AlternativeName != null && !DbSource.AlternativeName.StartsWith("import\\")) {
							filename = DbSource.AlternativeName ?? filename;
						}
					}
					else if (FileType == FileType.Detect && serverType == ServerType.RAthena) {
						FileType = FileType.Conf;
					}
				}

				if (FileType == FileType.Detect)
					FileType = FileType.Error;
			}

			if (FileType == FileType.Error) {
				DbDebugHelper.OnWriteStatusUpdate(DbSource, FilePath, _db, "FileType couldn't be detected.");
				return false;
			}

			if (DbSource == ServerDbs.Mobs || DbSource == ServerDbs.Mobs2) {
				// It's resave
				if (DbPathLocator.GetServerType() == ServerType.Hercules && serverType == ServerType.Hercules) {
					FileType = logicalPath.IsExtension(".conf") ? FileType.Conf : logicalPath.IsExtension(".txt") ? FileType.Txt : FileType;
				}
			}

			if ((DbSource.SupportedFileType & FileType) != FileType) {
				DbDebugHelper.OnWriteStatusUpdate(DbSource, FilePath, _db, "FileType not supported.");
				return false;
			}

			string ext = "." + FileType.ToString().ToLower();

			IsRenewal = false;

			if ((FileType & FileType.Sql) == FileType.Sql) {
				if (subPath == "re") {
					FilePath = GrfPath.CombineUrl(dbPath, filename + "_re" + ext);
				}
				else {
					FilePath = GrfPath.CombineUrl(dbPath, filename + ext);
				}
			}
			else {
				if (DbSource.UseSubPath) {
					if (subPath == "re")
						IsRenewal = true;

					if (DbPathLocator.UseAlternative(DbSource)) {
						if ((DbSource.AlternativeName + ext).Contains("import\\"))
							FilePath = GrfPath.CombineUrl(dbPath, DbSource.AlternativeName + ext);
						else
							FilePath = GrfPath.CombineUrl(dbPath, subPath, DbSource.AlternativeName + ext);
					}
					else {
						FilePath = GrfPath.CombineUrl(dbPath, subPath, filename + ext);
					}
				}
				else {
					if (DbPathLocator.UseAlternative(DbSource)) {
						FilePath = GrfPath.CombineUrl(dbPath, DbSource.AlternativeName + ext);
					}
					else {
						FilePath = GrfPath.CombineUrl(dbPath, filename + ext);
					}
				}
			}

			TextFileHelper.LatestFile = FilePath;
			OldPath = DbPathLocator.GetStoredFile(logicalPath);

			if (OldPath == null || !File.Exists(OldPath)) {
				DbDebugHelper.OnWriteStatusUpdate(DbSource, FilePath, _db, "Source path not found: '" + OldPath + "', cannot save this table.");
				return false;
			}

			if (!_db.IsEnabled) {
				DbDebugHelper.OnWriteStatusUpdate(DbSource, FilePath, _db, "Table not enabled.");
				return false;
			}

			if (FtpHelper.IsSystemFile(FilePath))
				GrfPath.CreateDirectoryFromFile(FilePath);

			BackupEngine.Instance.Backup(logicalPath);

			if (ForceWrite) {
				DbDebugHelper.OnWriteStatusUpdate(DbSource, FilePath, _db, "The table is saving...");
				return true;
			}

			if (_db.Table.Commands.CommandIndex == -1 && logicalPath.IsExtension(FilePath.GetExtension())) {
				if (isModifiedCheck && _db.Table.Tuples.Values.Any(p => !p.Normal)) return true;

				//// If we use the previous output, we should never overwrite the file
				//// because it will eat the previous modifications.
				//if (_db.UsePreviousOutput) {
				//	DbDebugHelper.OnWriteStatusUpdate(DbSource, FilePath, _db, "Output from master DB is more recent (will not be saved).");
				//	return false;
				//}

				if (SdeAppConfiguration.AlwaysOverwriteFiles) {
					_db.DbDirectCopy(this, _db);
				}

				DbDebugHelper.OnWriteStatusUpdate(DbSource, FilePath, _db, "Table not modified (will not be saved).");
				return false;
			}

			DbDebugHelper.OnWriteStatusUpdate(DbSource, FilePath, _db, "The table is saving...");
			return true;
		}
	}
}