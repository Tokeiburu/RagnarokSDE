using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Database;
using GRF.System;
using SDE.Editor.Engines;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic.Core;

namespace SDE.Editor.Writers {
	public class DbSqlWriter {
		#region Delegates
		public delegate string NotEmptyLineMethodDelegate(string builder);
		#endregion

		#region NewLineFormat enum
		public enum NewLineFormat {
			Unix,
			Windows
		}
		#endregion

		public NotEmptyLineMethodDelegate NotEmptyLineMethod;
		private string _tempPath;

		public bool IsHercules { get; set; }
		public bool IsRAthena { get; set; }
		public StringBuilder Builder { get; private set; }
		public string FileOutput { get; set; }
		public string TableName { get; set; }
		public string OriginalTableName { get; set; }

		public string TempPath {
			get { return _tempPath ?? (_tempPath = TemporaryFilesManager.GetTemporaryFilePath("to_sql_{0:0000}")); }
		}

		public NewLineFormat NewLine { get; set; }

		public int NumberOfLinesProcessed { get; set; }
		public bool IsRenewal { get; set; }
		public bool PrintEmptyLines { get; set; }
		public bool AddAdditionalSpaceAfterFirstTablePrinted { get; set; }

		public bool Init<TKey>(DbDebugItem<TKey> debug) {
			FileOutput = debug.FilePath;
			OriginalTableName = TableName = Path.GetFileNameWithoutExtension(FileOutput);

			if (TableName == null || OriginalTableName == null) return false;

			if (TableName.EndsWith("_re")) {
				IsRenewal = true;
			}

			debug.FilePath = TempPath;
			debug.FileType = FileType.Txt;
			Builder = new StringBuilder();

			if (debug.DestinationServer == ServerType.Hercules) {
				NewLine = NewLineFormat.Unix;
				IsHercules = true;
			}
			else {
				NewLine = NewLineFormat.Windows;
				IsRAthena = true;
			}

			if (debug.DestinationServer == ServerType.Hercules) {
				// mob_db2_re > mob_db2
				// item_db2_re > item_db2
				// etc
				TableName = OriginalTableName.Replace("2_re", "2");
				FileOutput = FileOutput.Replace("2_re.sql", "2.sql");
			}

			PrintEmptyLines = true;
			return true;
		}

		public void AppendHeader(string header) {
			Builder.AppendFormat(header, TableName);
		}

		public void AppendHeader(string header, string tableName) {
			Builder.AppendFormat(header, tableName);
		}

		public void Line(string line) {
			if (NewLine == NewLineFormat.Unix)
				Builder.AppendLine(line);
			else
				Builder.AppendLine(line);
		}

		public IEnumerable<string> Read() {
			string line;
			using (StreamReader reader = TextFileHelper.SetAndLoadReader(TempPath)) {
				while (!reader.EndOfStream) {
					line = reader.ReadLine();
					if (line == null) continue;
					if (!String.IsNullOrEmpty(line)) {
						yield return line;
					}
					else {
						if (PrintEmptyLines)
							if (NewLine == NewLineFormat.Unix)
								Builder.AppendLine();
							else
								Builder.AppendLine();
					}
				}
			}
		}

		public void Write() {
			IOHelper.WriteAllText(FileOutput, Builder.ToString());
		}
	}
}