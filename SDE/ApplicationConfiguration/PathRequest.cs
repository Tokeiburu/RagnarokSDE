using SDE.Tools.DatabaseEditor;
using TokeiLibrary.Paths;
using Utilities;

namespace SDE.ApplicationConfiguration {
	/// <summary>
	/// Copied from GRF Editor
	/// This class is Used when requesting paths; it saves the latest automatically
	/// </summary>
	public static class PathRequest {
		public static Setting ExtractSetting {
			get { return new Setting(null, typeof(SdeAppConfiguration).GetProperty("AppLastPath")); }
		}
		public static string SaveFileCde(params string[] extra) {
			return TkPathRequest.SaveFile(new Setting(null, typeof(SdeAppConfiguration).GetProperty("AppLastPath")), extra);
		}
		public static string OpenFileCde(params string[] extra) {
			return TkPathRequest.OpenFile(new Setting(null, typeof(ProjectConfiguration).GetProperty("AppLastPath")), extra);
		}
		public static string FolderEditor(params string[] extra) {
			return TkPathRequest.Folder(new Setting(null, typeof(SdeAppConfiguration).GetProperty("AppLastPath")), extra);
		}
		public static string FolderExtractSql(params string[] extra) {
			return TkPathRequest.Folder(new Setting(null, typeof(SdeAppConfiguration).GetProperty("AppLastPathSql")), extra);
		}
		public static string FolderExtractDb(params string[] extra) {
			return TkPathRequest.Folder(new Setting(null, typeof(SdeAppConfiguration).GetProperty("AppLastPathDb")), extra);
		}
	}
}
