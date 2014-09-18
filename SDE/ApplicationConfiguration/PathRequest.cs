using SDE.Tools.DatabaseEditor;
using TokeiLibrary.Paths;
using Utilities;

namespace SDE.ApplicationConfiguration {
	public static class PathRequest {
		public static Setting ExtractSetting {
			get { return new Setting(null, typeof(SDEAppConfiguration).GetProperty("AppLastPath")); }
		}

		public static string SaveFileEditor(params string[] extra) {
			return TkPathRequest.SaveFile(new Setting(null, typeof(SDEAppConfiguration).GetProperty("AppLastPath")), extra);
		}
		public static string SaveFileCde(params string[] extra) {
			return TkPathRequest.SaveFile(new Setting(null, typeof(SDEAppConfiguration).GetProperty("AppLastPath")), extra);
		}
		public static string SaveFileSql(params string[] extra) {
			return TkPathRequest.SaveFile(new Setting(null, typeof(SDEAppConfiguration).GetProperty("AppLastPathSql")), extra);
		}
		public static string SaveFileExtract(params string[] extra) {
			return TkPathRequest.SaveFile(ExtractSetting, extra);
		}
		public static string OpenFileEditor(params string[] extra) {
			return TkPathRequest.OpenFile(new Setting(null, typeof(SDEAppConfiguration).GetProperty("AppLastPath")), extra);
		}
		public static string OpenFileExtract(params string[] extra) {
			return TkPathRequest.OpenFile(ExtractSetting, extra);
		}
		public static string OpenFileCde(params string[] extra) {
			return TkPathRequest.OpenFile(new Setting(null, typeof(SDEConfiguration).GetProperty("AppLastPath")), extra);
		}
		public static string FolderEditor(params string[] extra) {
			return TkPathRequest.Folder(new Setting(null, typeof(SDEAppConfiguration).GetProperty("AppLastPath")), extra);
		}
		public static string FolderExtract(params string[] extra) {
			return TkPathRequest.Folder(ExtractSetting);
		}
		public static string FolderExtractSql(params string[] extra) {
			return TkPathRequest.Folder(new Setting(null, typeof(SDEAppConfiguration).GetProperty("AppLastPathSql")), extra);
		}
		public static string FolderExtractDb(params string[] extra) {
			return TkPathRequest.Folder(new Setting(null, typeof(SDEAppConfiguration).GetProperty("AppLastPathDb")), extra);
		}
	}
}
