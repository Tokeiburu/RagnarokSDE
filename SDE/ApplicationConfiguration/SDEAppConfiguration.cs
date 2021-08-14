using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using ErrorManager;
using GRF.IO;
using GRF.Image;
using GRF.System;
using GrfToWpfBridge;
using ICSharpCode.AvalonEdit;
using TokeiLibrary;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace SDE.ApplicationConfiguration {
	/// <summary>
	/// Configuration binder for Avalon's Text Editor
	/// </summary>
	public class AvalonBinder : BinderAbstract<TextEditor, string> {
		public override void Bind(TextEditor element, Func<string> get, Action<string> set, Action extra, bool execute) {
			element.Text = get();

			element.TextChanged += delegate {
				set(element.Text);

				if (extra != null)
					extra();
			};

			if (execute) {
				if (extra != null)
					extra();
			}
		}
	}

	/// <summary>
	/// Program's configuration (stored in config.txt)
	/// </summary>
	public static class SdeAppConfiguration {
		private static ConfigAsker _configAsker;
		private static Encoding _encodingResDisplay;
		public static AvalonBinder Ab = new AvalonBinder();
		public static Encoding EncodingServer { get; set; }
		public static Encoding EncodingMetaGrfView { get; set; }

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
			get { return "1.2.1.3"; }
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

		private static ObservableDictionary<string, string> _remapper;
		private static readonly BufferedProperty<bool> _dbWriterItemInfoIdDisplayName = new BufferedProperty<bool>(ConfigAsker, "[Server database editor - Db Writer - ItemInfo id display name]", true, FormatConverters.BooleanConverter);
		private static readonly BufferedProperty<bool> _dbWriterItemInfoUnDisplayName = new BufferedProperty<bool>(ConfigAsker, "[Server database editor - Db Writer - ItemInfo un display name]", true, FormatConverters.BooleanConverter);
		private static readonly BufferedProperty<bool> _dbWriterItemInfoIdResource = new BufferedProperty<bool>(ConfigAsker, "[Server database editor - Db Writer - ItemInfo id resource]", true, FormatConverters.BooleanConverter);
		private static readonly BufferedProperty<bool> _dbWriterItemInfoUnResource = new BufferedProperty<bool>(ConfigAsker, "[Server database editor - Db Writer - ItemInfo un resource]", true, FormatConverters.BooleanConverter);
		private static readonly BufferedProperty<bool> _dbWriterItemInfoIdDescription = new BufferedProperty<bool>(ConfigAsker, "[Server database editor - Db Writer - ItemInfo id desc]", true, FormatConverters.BooleanConverter);
		private static readonly BufferedProperty<bool> _dbWriterItemInfoUnDescription = new BufferedProperty<bool>(ConfigAsker, "[Server database editor - Db Writer - ItemInfo un desc]", true, FormatConverters.BooleanConverter);
		private static readonly BufferedProperty<bool> _dbWriterItemInfoSlotCount = new BufferedProperty<bool>(ConfigAsker, "[Server database editor - Db Writer - ItemInfo slot count]", true, FormatConverters.BooleanConverter);
		private static readonly BufferedProperty<bool> _dbWriterItemInfoIsCostume = new BufferedProperty<bool>(ConfigAsker, "[Server database editor - Db Writer - ItemInfo is costume]", true, FormatConverters.BooleanConverter);
		private static readonly BufferedProperty<bool> _dbWriterItemInfoClassNum = new BufferedProperty<bool>(ConfigAsker, "[Server database editor - Db Writer - ItemInfo class num]", true, FormatConverters.BooleanConverter);
		private static readonly BufferedProperty<bool> _dbWriterGroupItemSingle = new BufferedProperty<bool>(ConfigAsker, "[Server database editor - Db Writer - group_item single]", true, FormatConverters.BooleanConverter);

		public static string TempPath {
			get { return Settings.TempPath; }
		}

		public static bool AttemptingCustomDllLoad {
			get { return Boolean.Parse(ConfigAsker["[GRFEditor - Loading custom DLL state]", false.ToString()]); }
			set { ConfigAsker["[GRFEditor - Loading custom DLL state]"] = value.ToString(); }
		}

		public static int CompressionMethod {
			get { return Int32.Parse(ConfigAsker["[GRFEditor - Compression method index]", "0"]); }
			set { ConfigAsker["[GRFEditor - Compression method index]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static string CustomCompressionMethod {
			get { return ConfigAsker["[GRFEditor - Custom compression library]", ""]; }
			set { ConfigAsker["[GRFEditor - Custom compression library]"] = value; }
		}

		public static string ProgramDataPath {
			get { return GrfPath.Combine(Configuration.ApplicationDataPath, ProgramName); }
		}

		public static ErrorLevel WarningLevel {
			get { return (ErrorLevel) Int32.Parse(ConfigAsker["[Server database editor - Warning level]", "0"]); }
			set { ConfigAsker["[Server database editor - Warning level]"] = ((int) value).ToString(CultureInfo.InvariantCulture); }
		}

		public static int PatchId {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Patch]", "0"]); }
			set { ConfigAsker["[Server database editor - Patch]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int ThemeIndex {
			get { return Int32.Parse(ConfigAsker["[Server database editor - ThemeIndex]", "0"]); }
			set { ConfigAsker["[Server database editor - ThemeIndex]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int EncodingCodepageClient {
			get {
				if (PatchId == 0) {
					EncodingCodepageClient = 1252;
					PatchId++;
				}

				return Int32.Parse(ConfigAsker["[Server database editor - Encoding codepage]", "1252"]);
			}
			set { ConfigAsker["[Server database editor - Encoding codepage]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int EncodingCodepageServer {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Encoding server codepage]", "65001"]); }
			set { ConfigAsker["[Server database editor - Encoding server codepage]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int DbValidMaxItemDbId {
			get { return ConfigAsker["[Server database editor - Db validation - Max item db]", "0x8000"].ToInt(); }
			set { ConfigAsker["[Server database editor - Db validation - Max item db]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int DbValidMaxMobDbId {
			get { return ConfigAsker["[Server database editor - Db validation - Max mob db]", "5000"].ToInt(); }
			set { ConfigAsker["[Server database editor - Db validation - Max mob db]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int DbValidMaxMobDbElement {
			get { return ConfigAsker["[Server database editor - Db validation - Max mob element]", "10"].ToInt(); }
			set { ConfigAsker["[Server database editor - Db validation - Max mob element]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int DbValidMaxSlotCount {
			get { return ConfigAsker["[Server database editor - Db validation - Max slot count]", "4"].ToInt(); }
			set { ConfigAsker["[Server database editor- Db validation - Max slot count]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static bool DbValidate {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Db validation - Validate]", "false"]); }
			set { ConfigAsker["[Server database editor - Db validation - Validate]"] = value.ToString(); }
		}

		public static bool ValidationRawView {
			get { return Boolean.Parse(ConfigAsker["[Validation - Show raw view]", false.ToString()]); }
			set { ConfigAsker["[Validation - Show raw view]"] = value.ToString(); }
		}

		public static bool BindItemTabs {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Bind item tabs]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Bind item tabs]"] = value.ToString(); }
		}

		public static bool CmdCopyToOverwrite {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Cmd CopyTo - Overwrite]", "true"]); }
			set { ConfigAsker["[Server database editor - Cmd CopyTo - Overwrite]"] = value.ToString(); }
		}

		public static bool CmdCopyToClientItems {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Cmd CopyTo - Client items]", "true"]); }
			set { ConfigAsker["[Server database editor - Cmd CopyTo - Client items]"] = value.ToString(); }
		}

		public static bool CmdCopyToAegisNameEnabled {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Cmd CopyTo - AegisName enabled]", "false"]); }
			set { ConfigAsker["[Server database editor - Cmd CopyTo - AegisName enabled]"] = value.ToString(); }
		}

		public static bool UseZenyColors {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Use zeny colors]", "false"]); }
			set { ConfigAsker["[Server database editor - Use zeny colors]"] = value.ToString(); }
		}

		public static bool UseDiscount {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Use discount]", "false"]); }
			set { ConfigAsker["[Server database editor - Use discount]"] = value.ToString(); }
		}

		public static bool AlwaysUseViewId {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - AlwaysUseViewId]", "false"]); }
			set { ConfigAsker["[Server database editor - AlwaysUseViewId]"] = value.ToString(); }
		}
		
		public static string CmdCopyToAegisNameFormatInput {
			get { return ConfigAsker["[Server database editor - Cmd CopyTo - AegisName format input]", @"([a-zA-Z_\ \-'0-9]+)(\d+)?"]; }
			set { ConfigAsker["[Server database editor - Cmd CopyTo - AegisName format input]"] = value; }
		}

		public static string CmdCopyToAegisNameFormatOutput {
			get { return ConfigAsker["[Server database editor - Cmd CopyTo - AegisName format output]", @"\1\2"]; }
			set { ConfigAsker["[Server database editor - Cmd CopyTo - AegisName format output]"] = value; }
		}

		public static bool CmdCopyToNameEnabled {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Cmd CopyTo - Name enabled]", "false"]); }
			set { ConfigAsker["[Server database editor - Cmd CopyTo - Name enabled]"] = value.ToString(); }
		}

		public static string CmdCopyToNameFormatInput {
			get { return ConfigAsker["[Server database editor - Cmd CopyTo - Name format input]", @"([a-zA-Z_\ \-'0-9]+)(\d+)?"]; }
			set { ConfigAsker["[Server database editor - Cmd CopyTo - Name format input]"] = value; }
		}

		public static string CmdCopyToNameFormatOutput {
			get { return ConfigAsker["[Server database editor - Cmd CopyTo - Name format output]", @"\1\2"]; }
			set { ConfigAsker["[Server database editor - Cmd CopyTo - Name format output]"] = value; }
		}

		public static bool VaResCollection {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - Collection]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - Collection]"] = value.ToString(); }
		}

		public static bool VaResExistingOnly {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - Existing only]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - Existing only]"] = value.ToString(); }
		}

		public static bool VaResInventory {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - Inventory]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - Inventory]"] = value.ToString(); }
		}

		public static bool VaResDrag {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - Drag]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - Drag]"] = value.ToString(); }
		}

		public static bool VaResIllustration {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - Illustration]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - Illustration]"] = value.ToString(); }
		}

		public static bool VaResNpc {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - Npcs]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - Npcs]"] = value.ToString(); }
		}

		public static bool VaResShield {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - Shields]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - Shields]"] = value.ToString(); }
		}

		public static bool VaResWeapon {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - Weapons]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - Weapons]"] = value.ToString(); }
		}

		public static bool VaResGarment {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - Garments]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - Garments]"] = value.ToString(); }
		}

		public static bool VaResInvalidFormat {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - Invalid format]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - Invalid format]"] = value.ToString(); }
		}

		public static bool VaCiViewId {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - VaResViewId]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - VaResViewId]"] = value.ToString(); }
		}

		public static bool VaCiNumberOfSlots {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - VaCiNumberOfSlots]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - VaCiNumberOfSlots]"] = value.ToString(); }
		}

		public static bool VaCiIsCard {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - VaCiIsCard]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - VaCiIsCard]"] = value.ToString(); }
		}

		public static bool VaCiName {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - VaCiName]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - VaCiName]"] = value.ToString(); }
		}

		public static bool VaCiDescription {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - VaResDescription]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - VaResDescription]"] = value.ToString(); }
		}

		public static bool VaCiItemRange {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - VaResItemRange]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - VaResItemRange]"] = value.ToString(); }
		}

		public static bool VaCiClass {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - Class]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - Class]"] = value.ToString(); }
		}

		public static bool VaCiAttack {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - Attack]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - Attack]"] = value.ToString(); }
		}

		public static bool VaCiDefense {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - Defense]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - Defense]"] = value.ToString(); }
		}

		public static bool VaCiProperty {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - Property]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - Property]"] = value.ToString(); }
		}

		public static bool VaCiRequiredLevel {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - RequiredLevel]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - RequiredLevel]"] = value.ToString(); }
		}

		public static bool VaCiWeaponLevel {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - WeaponLevel]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - WeaponLevel]"] = value.ToString(); }
		}

		public static bool VaCiWeight {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - Weight]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - Weight]"] = value.ToString(); }
		}

		public static bool VaCiLocation {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - Location]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - Location]"] = value.ToString(); }
		}

		public static bool VaCiCompoundOn {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - CompoundOn]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - CompoundOn]"] = value.ToString(); }
		}

		public static bool VaCiJob {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation ci - VaCiJob]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation ci - VaCiJob]"] = value.ToString(); }
		}
		
		public static bool VaResHeadgear {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - Headgears]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - Headgears]"] = value.ToString(); }
		}

		public static bool VaResEmpty {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - Empty]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - Empty]"] = value.ToString(); }
		}

		public static bool VaResInvalidCharacters {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - Invalid chars]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - Invalid chars]"] = value.ToString(); }
		}

		public static bool VaResClientItemMissing {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - ClientItem missing]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - ClientItem missing]"] = value.ToString(); }
		}
		
		public static bool VaResMonster {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Validation resource - Monsters]", "true"]); }
			set { ConfigAsker["[Server database editor - Validation resource - Monsters]"] = value.ToString(); }
		}

		public static Encoding EncodingResDisplay {
			get {
				if (_encodingResDisplay == null) {
					if (EncodingResCodePage < 0) {
						_encodingResDisplay = EncodingService.DisplayEncoding;
						EncodingResCodePage = EncodingService.DisplayEncoding.CodePage;
					}
					else {
						try {
							_encodingResDisplay = Encoding.GetEncoding(EncodingResCodePage);
						}
						catch {
							_encodingResDisplay = EncodingService.DisplayEncoding;
							EncodingResIndex = 0;
							EncodingResCodePage = EncodingService.DisplayEncoding.CodePage;
						}
					}
				}
				else if (_encodingResDisplay.CodePage != EncodingResCodePage) {
					try {
						_encodingResDisplay = Encoding.GetEncoding(EncodingResCodePage);
					}
					catch {
						_encodingResDisplay = EncodingService.DisplayEncoding;
						EncodingResIndex = 0;
						EncodingResCodePage = EncodingService.DisplayEncoding.CodePage;
					}
				}

				return _encodingResDisplay;
			}
		}

		public static int EncodingResCodePage {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Encoding res codepage]", "-1"]); }
			set { ConfigAsker["[Server database editor - Encoding res codepage]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int EncodingResIndex {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Encoding res index]", "0"]); }
			set { ConfigAsker["[Server database editor - Encoding res index]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static int EncodingCodepageView {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Encoding grf explorer view codepage]", "949"]); }
			set { ConfigAsker["[Server database editor - Encoding grf explorer view codepage]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		public static string AppLastPath {
			get { return ConfigAsker["[Server database editor - Application latest file name]", Configuration.ApplicationPath]; }
			set { ConfigAsker["[Server database editor - Application latest file name]"] = value; }
		}

		public static string MapCachePath {
			get { return ConfigAsker["[Server database editor - Mapcache latest file name]", Configuration.ApplicationPath]; }
			set { ConfigAsker["[Server database editor - Mapcache latest file name]"] = value; }
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
			get { return (FileAssociation) Enum.Parse(typeof (FileAssociation), ConfigAsker["[Server database editor - File type associated]", "0"]); }
			set { ConfigAsker["[Server database editor - File type associated]"] = value.ToString(); }
		}

		public static string IronPythonScript {
			get { return ConfigAsker["[Server database editor - IronPythonScript]", "# Welcome to the scripting console! \r\n# For samples, visit the Tutorials menu."]; }
			set { ConfigAsker["[Server database editor - IronPythonScript]"] = value; }
		}

		public static ObservableDictionary<string, string> Remapper {
			get {
				if (_remapper != null)
					return _remapper;

				var value = ConfigAsker["[Server database editor - Remapper]", ""];

				var gestures = new ObservableDictionary<string, string>();
				string[] groups = value.Split('%');

				foreach (var sub in groups) {
					if (sub.Length < 1)
						continue;

					string[] values = sub.Split('|');

					gestures[values[0]] = values[1];
				}

				_remapper = gestures;

				_remapper.CollectionChanged += delegate {
					StringBuilder b = new StringBuilder();

					foreach (var keyPair in _remapper) {
						b.Append(keyPair.Key);
						b.Append("|");
						b.Append(keyPair.Value);
						b.Append("%");
					}

					ConfigAsker["[Server database editor - Remapper]"] = b.ToString();
				};

				return gestures;
			}
		}

		public static bool DbWriterItemInfoIdDisplayName {
			get { return _dbWriterItemInfoIdDisplayName.Get(); }
			set { _dbWriterItemInfoIdDisplayName.Set(value); }
		}

		public static bool DbWriterItemInfoUnDisplayName {
			get { return _dbWriterItemInfoUnDisplayName.Get(); }
			set { _dbWriterItemInfoUnDisplayName.Set(value); }
		}

		public static bool DbWriterItemInfoIdResource {
			get { return _dbWriterItemInfoIdResource.Get(); }
			set { _dbWriterItemInfoIdResource.Set(value); }
		}

		public static bool DbWriterItemInfoUnResource {
			get { return _dbWriterItemInfoUnResource.Get(); }
			set { _dbWriterItemInfoUnResource.Set(value); }
		}

		public static bool DbWriterItemInfoIdDescription {
			get { return _dbWriterItemInfoIdDescription.Get(); }
			set { _dbWriterItemInfoIdDescription.Set(value); }
		}

		public static bool DbWriterItemInfoUnDescription {
			get { return _dbWriterItemInfoUnDescription.Get(); }
			set { _dbWriterItemInfoUnDescription.Set(value); }
		}

		public static bool DbWriterItemInfoSlotCount {
			get { return _dbWriterItemInfoSlotCount.Get(); }
			set { _dbWriterItemInfoSlotCount.Set(value); }
		}

		public static bool DbWriterItemInfoIsCostume {
			get { return _dbWriterItemInfoIsCostume.Get(); }
			set { _dbWriterItemInfoIsCostume.Set(value); }
		}

		public static bool DbWriterItemInfoClassNum {
			get { return _dbWriterItemInfoClassNum.Get(); }
			set { _dbWriterItemInfoClassNum.Set(value); }
		}

		public static bool DbWriterGroupItemSingle {
			get { return _dbWriterGroupItemSingle.Get(); }
			set { _dbWriterGroupItemSingle.Set(value); }
		}

		public static bool AlwaysOverwriteFiles {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Always overwrite files]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Always overwrite files]"] = value.ToString(); }
		}

		public static bool RestrictToAllowedJobs {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Restrict to allowed classes]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Restrict to allowed classes]"] = value.ToString(); }
		}

		public static bool AlwaysReopenLatestProject {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Always reopen latest project]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - Always reopen latest project]"] = value.ToString(); }
		}

		public static bool RateIncrementBy1 {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Increment by 1%]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - Increment by 1%]"] = value.ToString(); }
		}

		public static bool RateIncrementBy5 {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Increment by 5%]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Increment by 5%]"] = value.ToString(); }
		}

		public static bool UseIntegratedDialogsForFlags {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Use integrated dialogs for flags]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Use integrated dialogs for flags]"] = value.ToString(); }
		}

		public static bool UseIntegratedDialogsForScripts {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Use integrated dialogs for scripts]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Use integrated dialogs for scripts]"] = value.ToString(); }
		}

		public static bool UseIntegratedDialogsForLevels {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Use integrated dialogs for levels]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Use integrated dialogs for levels]"] = value.ToString(); }
		}

		public static bool UseIntegratedDialogsForJobs {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Use integrated dialogs for jobs]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Use integrated dialogs for jobs]"] = value.ToString(); }
		}

		public static bool UseIntegratedDialogsForTime {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Use integrated dialogs for time]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Use integrated dialogs for time]"] = value.ToString(); }
		}

		public static bool EnableMultipleSetters {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Enable multiple setters]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - Enable multiple setters]"] = value.ToString(); }
		}

		public static bool DbNouseIgnoreOverride {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - No use ignore override]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - No use ignore override]"] = value.ToString(); }
		}

		public static bool DbTradeIgnoreOverride {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Trade ignore override]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - Trade ignore override]"] = value.ToString(); }
		}

		public static bool AddCommentForItemTrade {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Client - Add comment for item trade]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Client - Add comment for item trade]"] = value.ToString(); }
		}

		public static bool AddCommentForItemAvail {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Client - Add comment for item avail]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Client - Add comment for item avail]"] = value.ToString(); }
		}

		public static bool AddCommentForItemNoUse {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Client - Add comment for item nouse]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Client - Add comment for item nouse]"] = value.ToString(); }
		}

		public static bool AddCommentForMobAvail {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Client - Add comment for mob avail]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Client - Add comment for mob avail]"] = value.ToString(); }
		}

		public static bool IronPythonAutocomplete {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - IronPython - Autocomplete]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - IronPython - Autocomplete]"] = value.ToString(); }
		}

		public static string NotepadPath {
			get { return ConfigAsker["[GRFEditor - Notepad++ path]", ""]; }
			set { ConfigAsker["[GRFEditor - Notepad++ path]"] = value; }
		}

		public static bool RevertItemTypes {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Revert item types]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - Revert item types]"] = value.ToString(); }
		}

		public class BufferedProperty<T> {
			private readonly ConfigAsker _ca;
			private readonly Func<string, T> _converter;
			private readonly T _def;
			private readonly string _prop;
			private bool _isSet;
			private T _value;

			public BufferedProperty(ConfigAsker ca, string prop, T def, Func<string, T> converter) {
				_ca = ca;
				_prop = prop;
				_def = def;
				_converter = converter;
			}

			public T Get() {
				if (_isSet)
					return _value;

				_isSet = true;
				_value = _converter(_ca[_prop, _def.ToString()]);
				return _value;
			}

			public void Set(T value) {
				_value = value;
				_isSet = true;
				_ca[_prop] = value.ToString();
			}

			public void Reset() {
				_isSet = false;
				_value = _def;
			}
		}

		#endregion

		#region Act Editor

		private static bool? _useAliasing;
		private static BitmapScalingMode? _mode;

		public static GrfColor ActEditorGridLineHorizontal {
			get { return new GrfColor((ConfigAsker["[ActEditor - Grid line horizontal color]", GrfColor.ToHex(255, 0, 0, 0)])); }
			set { ConfigAsker["[ActEditor - Grid line horizontal color]"] = value.ToHexString(); }
		}

		public static GrfColor ActEditorGridLineVertical {
			get { return new GrfColor((ConfigAsker["[ActEditor - Grid line vertical color]", GrfColor.ToHex(255, 0, 0, 0)])); }
			set { ConfigAsker["[ActEditor - Grid line vertical color]"] = value.ToHexString(); }
		}

		public static GrfColor ActEditorSpriteSelectionBorder {
			get { return new GrfColor((ConfigAsker["[ActEditor - Selected sprite border color]", GrfColor.ToHex(255, 255, 0, 0)])); }
			set { ConfigAsker["[ActEditor - Selected sprite border color]"] = value.ToHexString(); }
		}

		public static GrfColor ActEditorSpriteSelectionBorderOverlay {
			get { return new GrfColor((ConfigAsker["[ActEditor - Selected sprite overlay color]", GrfColor.ToHex(0, 255, 255, 255)])); }
			set { ConfigAsker["[ActEditor - Selected sprite overlay color]"] = value.ToHexString(); }
		}

		public static GrfColor ActEditorSelectionBorder {
			get { return new GrfColor((ConfigAsker["[ActEditor - Selection border color]", GrfColor.ToHex(255, 0, 0, 255)])); }
			set { ConfigAsker["[ActEditor - Selection border color]"] = value.ToHexString(); }
		}

		public static GrfColor ActEditorSelectionBorderOverlay {
			get { return new GrfColor((ConfigAsker["[ActEditor - Selection overlay color]", GrfColor.ToHex(50, 128, 128, 255)])); }
			set { ConfigAsker["[ActEditor - Selection overlay color]"] = value.ToHexString(); }
		}

		public static GrfColor ActEditorAnchorColor {
			get { return new GrfColor((ConfigAsker["[ActEditor - Anchor color]", GrfColor.ToHex(200, 255, 255, 0)])); }
			set { ConfigAsker["[ActEditor - Anchor color]"] = value.ToHexString(); }
		}

		public static bool ActEditorGridLineHVisible {
			get { return Boolean.Parse(ConfigAsker["[ActEditor - Grid line horizontal visible]", true.ToString()]); }
			set { ConfigAsker["[ActEditor - Grid line horizontal visible]"] = value.ToString(); }
		}

		public static bool ActEditorGridLineVVisible {
			get { return Boolean.Parse(ConfigAsker["[ActEditor - Grid line vertical visible]", true.ToString()]); }
			set { ConfigAsker["[ActEditor - Grid line vertical visible]"] = value.ToString(); }
		}

		public static Color ActEditorBackgroundColor {
			get { return new GrfColor((ConfigAsker["[ActEditor - Background preview color]", GrfColor.ToHex(150, 0, 0, 0)])).ToColor(); }
			set { ConfigAsker["[ActEditor - Background preview color]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B); }
		}

		public static BitmapScalingMode ActEditorScalingMode {
			get {
				if (_mode != null) {
					return _mode.Value;
				}

				var value = (BitmapScalingMode) Enum.Parse(typeof (BitmapScalingMode), ConfigAsker["[ActEditor - Scale mode]", BitmapScalingMode.NearestNeighbor.ToString()], true);
				_mode = value;
				return value;
			}
			set {
				ConfigAsker["[ActEditor - Scale mode]"] = value.ToString();
				_mode = value;
			}
		}

		public static bool UseAliasing {
			get {
				if (_useAliasing == null)
					_useAliasing = Boolean.Parse(ConfigAsker["[ActEditor - Use aliasing]", false.ToString()]);

				return _useAliasing.Value;
			}
			set {
				ConfigAsker["[ActEditor - Use aliasing]"] = value.ToString();
				_useAliasing = value;
			}
		}

		private static int _processId;

		public static int ProcessId {
			get {
				if (_processId == 0)
					_processId = Process.GetCurrentProcess().Id;

				return _processId;
			}
		}

		#endregion

		public static void Bind<T>(TextBox tb, Action<T> set, Func<string, T> converter) {
			tb.TextChanged += delegate {
				try {
					set(converter(tb.Text));
				}
				catch {
				}
			};
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