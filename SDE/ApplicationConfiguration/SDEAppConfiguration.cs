using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using ErrorManager;
using GRF.IO;
using TokeiLibrary;
using Utilities;

namespace SDE.ApplicationConfiguration {
	/// <summary>
	/// Program's configuration (stored in config.txt)
	/// </summary>
	public static class SdeAppConfiguration {
		private static ConfigAsker _configAsker;

		public static ConfigAsker ConfigAsker {
			get { return _configAsker ?? (_configAsker = new ConfigAsker(GrfPath.Combine(Configuration.ApplicationDataPath, ProgramName, "config.txt"))); }
			set { _configAsker = value; }
		}

		public static bool BackupsManagerState {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Backups manager enabled]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Backups manager enabled]"] = value.ToString(); }
		}

		#region Program's configuration and information
		public static string PublicVersion {
			get { return "1.0.7"; }
		}

		public static string Author {
			get { return "Tokeiburu"; }
		}

		public static string ProgramName {
			get { return "Server database editor"; }
		}

		public static string RealVersion {
			get { return Assembly.GetEntryAssembly().GetName().Version.ToString(); }
		}
		#endregion

		#region GRFEditor
		public static string TempPath {
			get {
				//string path = ConfigAsker["[Server database editor - Temporary path]", GrfPath.Combine(Configuration.ApplicationDataPath, ProgramName, "~tmp")];
				string path = GrfPath.Combine(Configuration.ApplicationDataPath, ProgramName, "~tmp");

				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);

				return path;
			}
		}
		public static string ProgramDataPath {
			get { return GrfPath.Combine(Configuration.ApplicationDataPath, ProgramName); }
		}
		public static string EncodingIndex {
			get { return ConfigAsker["[Server database editor - Encoding index]", "0"]; }
			set { ConfigAsker["[Server database editor - Encoding index]"] = value; }
		}
		public static ErrorLevel WarningLevel {
			get { return (ErrorLevel)Int32.Parse(ConfigAsker["[Server database editor - Warning level]", "0"]); }
			set { ConfigAsker["[Server database editor - Warning level]"] = ((int)value).ToString(CultureInfo.InvariantCulture); }
		}
		public static int EncodingCodepage {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Encoding codepage]", "1252"]); }
			set { ConfigAsker["[Server database editor - Encoding codepage]"] = value.ToString(CultureInfo.InvariantCulture); }
		}
		public static string AppLastPath {
			get { return ConfigAsker["[Server database editor - Application latest file name]", Configuration.ApplicationPath]; }
			set { ConfigAsker["[Server database editor - Application latest file name]"] = value; }
		}
		public static string AppLastPathSql {
			get { return ConfigAsker["[Server database editor - Application latest sql]", AppLastPath]; }
			set { ConfigAsker["[Server database editor - Application latest sql]"] = value; }
		}
		public static string AppLastPathDb {
			get { return ConfigAsker["[Server database editor - Application latest db]", AppLastPath]; }
			set { ConfigAsker["[Server database editor - Application latest db]"] = value; }
		}
		public static FileAssociation FileShellAssociated {
			get { return (FileAssociation)Enum.Parse(typeof(FileAssociation), ConfigAsker["[Server database editor - File type associated]", "0"]); }
			set { ConfigAsker["[Server database editor - File type associated]"] = value.ToString(); }
		}
		#endregion

		public static void Bind(CheckBox checkBox, Func<bool> get, Action<bool> set) {
			checkBox.IsChecked = get();
			checkBox.Checked += (e, a) => set(true);
			checkBox.Unchecked += (e, a) => set(false);
		}

		public static void Bind(CheckBox checkBox, Func<bool> get, Action<bool> set, Action extra) {
			checkBox.IsChecked = get();
			checkBox.Checked += (e, a) => { set(true); extra(); };
			checkBox.Unchecked += (e, a) => { set(false); extra(); };
		}
	}

	[Flags]
	public enum FileAssociation {
		Grf = 1 << 1,
		Gpf = 1 << 2,
		Rgz = 1 << 3,
		GrfKey = 1 << 4,
		Spr = 1 << 5,
		All = 1 << 6,
		Cde = 1 << 7,
		Thor = 1 << 8,
		Sde = 1 << 9,
	}
}
