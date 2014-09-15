using System.IO;
using Utilities;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.CDECore {
	public class GrfShellFile {
		public GrfShellFile(string filePath) {
			RelativePath = filePath;
			DisplayRelativePath = Path.GetFileName(filePath);
			FileSize = (int)new FileInfo(filePath).Length;
			DisplaySize = Methods.FileSizeToString(FileSize);

			try {
				FileType = filePath.GetExtension().Remove(0, 1).ToUpper();
			}
			catch {
				FileType = "";
			}
		}

		public string RelativePath { get; set; }
		public string DisplayRelativePath { get; set; }
		public string DisplaySize { get; set; }
		public string FileType { get; set; }
		public int FileSize { get; set; }
	}
}
