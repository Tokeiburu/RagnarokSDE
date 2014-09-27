using System;
using System.IO;
using ErrorManager;
using GRF.IO;
using SDE.Tools.DatabaseEditor.Engines.BackupsEngine;
using SDE.Tools.DatabaseEditor.Engines.Parsers;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Generic.Core {
	/// <summary>
	/// Class used to validate load or write operations.
	/// It loads parameters that will be used by the DbWriter methods
	/// and it also calls the backup engine. This object must always
	/// be called before proceeding to any load or write operations.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	public class DbDebugItem<TKey> {
		public const int MaximumNumberOfAllowedExceptions = 10;

		private readonly AbstractDb<TKey> _db;

		public DbDebugItem(AbstractDb<TKey> db) {
			_db = db;
			DbSource = _db.DbSource;
			NumberOfErrors = MaximumNumberOfAllowedExceptions;
			TextFileHelper.LatestFile = null;
			TextFileHelper.LastReader = null;
		}

		public AbstractDb<TKey> AbsractDb {
			get { return _db; }
		}
		public int NumberOfErrors { get; private set; }
		public bool IsRenewal { get; private set; }
		public string FilePath { get; set; }
		public string OldPath { get; private set; }
		public string SubPath { get; private set; }
		public ServerDbs DbSource { get; set; }
		public FileType FileType { get; set; }
		public ServerType DestinationServer { get; private set; }

		public bool Load(ServerDbs dbSource) {
			DbSource = dbSource;
			string path = AllLoaders.DetectPath(DbSource);

			TextFileHelper.LatestFile = path;

			if (String.IsNullOrEmpty(path)) {
				if (_db.ThrowFileNotFoundException) {
					DbLoaderErrorHandler.Handle("File not found '" + DbSource + "'.", ErrorLevel.NotSpecified);
				}

				return false;
			}

			FileType = AllLoaders.GetFileType(path);
			FilePath = path;
			AllLoaders.StoreFile(FilePath);
			return true;
		}

		public bool Load() {
			return Load(DbSource);
		}

		public bool ReportException(Exception item) {
			DbLoaderErrorHandler.HandleLoader(item.Message);
			NumberOfErrors--;

			if (NumberOfErrors < 0) {
				DbLoaderErrorHandler.Handle("Failed to read too many items, the db will stop loading.", ErrorLevel.Critical);
				return false;
			}

			return true;
		}

		public void ReportIdException(string exception, object item, ErrorLevel errorLevel = ErrorLevel.Warning) {
			DbLoaderErrorHandler.Handle(exception, item.ToString(), errorLevel);
		}

		public bool ReportIdExceptionWithError(string exception, object item) {
			DbLoaderErrorHandler.Handle(exception, item.ToString());
			NumberOfErrors--;

			if (NumberOfErrors < 0) {
				DbLoaderErrorHandler.Handle("Failed to read too many items, the db [" + DbSource + "] will stop loading.", ErrorLevel.Critical);
				return false;
			}

			return true;
		}

		public bool ReportIdException(object item) {
			DbLoaderErrorHandler.Handle("Failed to read an item.", item.ToString());
			NumberOfErrors--;

			if (NumberOfErrors < 0) {
				DbLoaderErrorHandler.Handle("Failed to read too many items, the db [" + DbSource + "] will stop loading.", ErrorLevel.Critical);
				return false;
			}

			return true;
		}

		public bool ReportException(string item) {
			DbLoaderErrorHandler.Handle(item);
			NumberOfErrors--;

			if (NumberOfErrors < 0) {
				DbLoaderErrorHandler.Handle("Failed to read too many items, the db [" + DbSource + "] will stop loading.", ErrorLevel.Critical);
				return false;
			}

			return true;
		}

		public bool Write(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			SubPath = subPath;
			string filename = DbSource.Filename;
			DestinationServer = serverType;

			FileType = fileType;

			if ((fileType & FileType.Detect) == FileType.Detect) {
				if ((DbSource.SupportedFileType & FileType.Txt) == FileType.Txt) {
					FileType = FileType.Txt;
				}

				if ((DbSource.SupportedFileType & FileType.Conf) == FileType.Conf) {
					if (serverType == ServerType.Hercules) {
						FileType = FileType.Conf;
						filename = DbSource.AlternativeName ?? filename;
					}
				}

				if (FileType == FileType.Detect)
					FileType = FileType.Error;
			}

			if (FileType == FileType.Error)
				return false;

			if ((DbSource.SupportedFileType & FileType) != FileType) {
				return false;
			}

			string ext = "." + FileType.ToString().ToLower();

			IsRenewal = false;

			if ((FileType & FileType.Sql) == FileType.Sql) {
				if (subPath == "re") {
					FilePath = GrfPath.Combine(dbPath, filename + "_re" + ext);
				}
				else {
					FilePath = GrfPath.Combine(dbPath, filename + ext);
				}
			}
			else {
				if (DbSource.UseSubPath) {
					if (subPath == "re")
						IsRenewal = true;

					FilePath = GrfPath.Combine(dbPath, subPath, filename + ext);
				}
				else {
					FilePath = GrfPath.Combine(dbPath, filename + ext);
				}
			}

			TextFileHelper.LatestFile = FilePath;

			string logicalPath = AllLoaders.DetectPath(DbSource);
			OldPath = AllLoaders.GetStoredFile(logicalPath);

			if (OldPath == null || !File.Exists(OldPath)) {
				return false;
			}

			if (_db.Attached["IsEnabled"] != null && !(bool)_db.Attached["IsEnabled"])
				return false;

			GrfPath.CreateDirectoryFromFile(FilePath);

			if (!_db.Table.Commands.IsModified && logicalPath.IsExtension(FilePath.GetExtension())) {
				BackupEngine.Instance.Backup(logicalPath);
				_db.DbDirectCopy(this, _db);
				return false;
			}

			BackupEngine.Instance.Backup(logicalPath);
			return true;
		}
	}
}