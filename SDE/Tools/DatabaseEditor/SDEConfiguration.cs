using System.Collections.Generic;
using System.IO;
using SDE.ApplicationConfiguration;
using Utilities;

namespace SDE.Tools.DatabaseEditor {
	/// <summary>
	/// Contains all the configuration information
	/// The ConfigAsker shouldn't be used manually to store variable,
	/// make a new property instead. The properties should also always
	/// have a default value.
	/// </summary>
	public static class SDEConfiguration {
		public static string DefaultFileName = Path.Combine(SDEAppConfiguration.ProgramDataPath, "default.sde");
		private static ConfigAsker _configAsker;

		public static ConfigAsker ConfigAsker {
			get { return _configAsker ?? (_configAsker = new ConfigAsker(Path.Combine(Methods.ApplicationPath, "config.txt"))); }
			set { _configAsker = value; }
		}

		public static List<string> CustomTabs {
			get { return Methods.StringToList(ConfigAsker["[Server database editor - Custom tables]", ""]); }
			set { ConfigAsker["[Server database editor - Custom tables]"] = Methods.ListToString(value); }
		}

		public static string AppLastPath {
			get { return ConfigAsker["[Server database editor - Application latest file name]", SDEAppConfiguration.AppLastPath]; }
			set { ConfigAsker["[Server database editor - Application latest file name]"] = value; }
		}

		public static string DatabasePath {
			get { return ConfigAsker["[Server database editor - Database path]", @"C:\RO\db\pre-re"]; }
			set { ConfigAsker["[Server database editor - Database path]"] = value; }
		}

		public static string SDEditorResources {
			get { return ConfigAsker["[Server database editor - Resources]", ""]; }
			set { ConfigAsker["[Server database editor - Resources]"] = value; }
		}
	}
}
