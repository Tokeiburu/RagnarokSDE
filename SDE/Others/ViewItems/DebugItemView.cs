using System;
using System.IO;
using System.Windows.Media.Imaging;
using ErrorManager;
using TokeiLibrary;

namespace SDE.Others.ViewItems {
	public class DebugItemView {
		public DebugItemView(int errorNumber, string exception, ErrorLevel errorLevel) {
			OriginalException = exception;
			ErrorNumber = errorNumber;
			Exception = exception;

			string line = "line: ";
			int index = Exception.IndexOf(line, 0, StringComparison.Ordinal);

			if (index > -1) {
				int indexOfComa = Exception.IndexOf(',', index + 1);
				indexOfComa = indexOfComa < 0 ? Exception.Length : indexOfComa;
				Line = Exception.Substring(index + line.Length, indexOfComa - index - line.Length).Trim(' ', '\'');
				Exception = Exception.Remove(index, indexOfComa - index);

				if (index > 0) {
					Exception = Exception.Remove(index - 2, 2);
				}

				if (index + 2 < Exception.Length && Exception[index] == ',') {
					Exception = Exception.Remove(index, 2);
				}
			}

			line = "file: ";
			index = Exception.IndexOf(line, 0, StringComparison.Ordinal);

			if (index > -1) {
				int indexOfComa = Exception.IndexOf(',', index + 1);
				indexOfComa = indexOfComa < 0 ? Exception.Length : indexOfComa;
				FilePath = Exception.Substring(index + line.Length, indexOfComa - index - line.Length).Trim(' ', '\'');
				FileName = Path.GetFileName(FilePath);
				Exception = Exception.Remove(index, indexOfComa - index);

				if (index > 0) {
					Exception = Exception.Remove(index - 2, 2);
				}

				if (index + 2 < Exception.Length  && Exception[index] == ',') {
					Exception = Exception.Remove(index, 2);
				}
			}

			line = "ID: ";
			index = Exception.IndexOf(line, 0, StringComparison.Ordinal);

			if (index > -1) {
				int indexOfComa = Exception.IndexOf(',', index + 1);
				indexOfComa = indexOfComa < 0 ? Exception.Length : indexOfComa;
				Id = Exception.Substring(index + line.Length, indexOfComa - index - line.Length).Trim(' ', '\'');
				Exception = Exception.Remove(index, indexOfComa - index);

				if (index > 0) {
					Exception = Exception.Remove(index - 2, 2);
				}

				if (index + 2 < Exception.Length && Exception[index] == ',') {
					Exception = Exception.Remove(index, 2);
				}
			}

			line = "exception: ";
			index = Exception.IndexOf(line, 0, StringComparison.Ordinal);

			if (index > -1) {
				int indexOfComa = Exception.IndexOf(',', index + 1);
				indexOfComa = indexOfComa < 0 ? Exception.Length : indexOfComa;
				Exception = Exception.Substring(index + line.Length, indexOfComa - index - line.Length);
				Exception = Exception.Substring(1, Exception.Length - 2);

				if (index > 0) {
					Exception = Exception.Remove(index - 2, 2);
				}

				if (index + 2 < Exception.Length && Exception[index] == ',') {
					Exception = Exception.Remove(index, 2);
				}
			}

			switch (errorLevel) {
				case ErrorLevel.Critical: DataImage = ApplicationManager.PreloadResourceImage("error16.png") as BitmapSource; break;
				case ErrorLevel.Low: DataImage = ApplicationManager.PreloadResourceImage("validity.png") as BitmapSource; break;
				case ErrorLevel.NotSpecified: DataImage = ApplicationManager.PreloadResourceImage("help.png") as BitmapSource; break;
				case ErrorLevel.Warning: DataImage = ApplicationManager.PreloadResourceImage("warning16.png") as BitmapSource; break;
			}
		}

		public string Exception { get; set; }
		public string OriginalException { get; set; }
		public BitmapSource DataImage { get; set; }

		public int ErrorNumber { get; set; }

		public string FileName { get; set; }
		public string FilePath { get; set; }
		public string Id { get; set; }

		public bool Default {
			get { return true; }
		}

		public string Line { get; set; }

		public override string ToString() {
			return OriginalException;
		}
	}
}