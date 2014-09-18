using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using ErrorManager;
using GRF.IO;
using GRF.System;
using TokeiLibrary;
using Utilities;

namespace SDE.ApplicationConfiguration {
	/// <summary>
	/// Contains all the configuration information
	/// The ConfigAsker shouldn't be used manually to store variable,
	/// make a new property instead. The properties should also always
	/// have a default value.
	/// </summary>
	public static class SDEAppConfiguration {
		private static ConfigAsker _configAsker;

		public static ConfigAsker ConfigAsker {
			get { return _configAsker ?? (_configAsker = new ConfigAsker(GrfPath.Combine(Configuration.ApplicationDataPath, ProgramName, "config.txt"))); }
			set { _configAsker = value; }
		}

		public static Brush UIPanelPreviewBackground {
			get { return (Brush)XamlReader.Parse(ConfigAsker["[Style - Panel preview background]", XamlWriter.Save(new SolidColorBrush(Colors.White)).Replace(Environment.NewLine, "")]); }
			set {
				ConfigAsker["[Style - Panel preview background]"] = XamlWriter.Save(value).Replace(Environment.NewLine, "");
			}
		}

		public static bool BackupsManagerState {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Backups manager enabled]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Backups manager enabled]"] = value.ToString(); }
		}

		#region TreeBehavior
		public static bool TreeBehaviorSaveExpansion {
			get { return Boolean.Parse(ConfigAsker["[TreeBehavior - Save expansion]", true.ToString()]); }
			set { ConfigAsker["[TreeBehavior - Save expansion]"] = value.ToString(); }
		}
		public static string TreeBehaviorSaveExpansionFolders {
			get { return ConfigAsker["[TreeBehavior - Save expansion folders]", ""]; }
			set { ConfigAsker["[TreeBehavior - Save expansion folders]"] = value; }
		}
		public static bool TreeBehaviorExpandSpecificFolders {
			get { return Boolean.Parse(ConfigAsker["[TreeBehavior - Expand specific GRF paths]", true.ToString()]); }
			set { ConfigAsker["[TreeBehavior - Expand specific GRF paths]"] = value.ToString(); }
		}
		public static string TreeBehaviorSpecificFolders {
			get { return ConfigAsker["[TreeBehavior - Specific folders]", "data,root,data\\Example"]; }
			set { ConfigAsker["[TreeBehavior - Specific folders]"] = value; }
		}
		public static bool TreeBehaviorSelectLatest {
			get { return Boolean.Parse(ConfigAsker["[TreeBehavior - Select latest node]", true.ToString()]); }
			set { ConfigAsker["[TreeBehavior - Select latest node]"] = value.ToString(); }
		}
		public static string TreeBehaviorSelectLatestFolders {
			get { return ConfigAsker["[TreeBehavior - Select latest folders]", ""]; }
			set { ConfigAsker["[TreeBehavior - Select latest folders]"] = value; }
		}

		public static string MapExtractorResources {
			get { return ConfigAsker["[MapExtractor - Resources]", ""]; }
			set { ConfigAsker["[MapExtractor - Resources]"] = value; }
		}
		public static bool AutomaticallyPlaySoundFiles {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Automatically read sound files]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Automatically read sound files]"] = value.ToString(); }
		}
		#endregion

		#region Program's configuration and information
		public static string PublicVersion {
			get { return "1.0.1"; }
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

		public static string WebHost {
			get { return @"https://googledrive.com/host/0B8dzg7ZYdSrSYjdyNHpac2hrV1U/"; }
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
		public static bool AlwaysReopenLatestGrf {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Always reopen latest Grf]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - Always reopen latest Grf]"] = value.ToString(); }
		}
		public static bool LockFiles {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Lock added files]", false.ToString()]); }
			set {
				ConfigAsker["[Server database editor - Lock added files]"] = value.ToString();
				Settings.LockFiles = value;
			}
		}
		public static bool PreviewRawGrfProperties {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Preview service - Grf properties - Show raw view]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - Preview service - Grf properties - Show raw view]"] = value.ToString(); }
		}
		public static bool PreviewRawFileStructure {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Preview service - File structure - Show raw view]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - Preview service - File structure - Show raw view]"] = value.ToString(); }
		}
		public static bool PatchingServiceEnabled {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Patching service - Enabled]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - Patching service - Enabled]"] = value.ToString(); }
		}
		public static string EncodingIndex {
			get { return ConfigAsker["[Server database editor - Encoding index]", "0"]; }
			set { ConfigAsker["[Server database editor - Encoding index]"] = value; }
		}
		public static ErrorLevel WarningLevel {
			get { return (ErrorLevel)Int32.Parse(ConfigAsker["[Server database editor - Warning level]", "0"]); }
			set { ConfigAsker["[Server database editor - Warning level]"] = ((int)value).ToString(CultureInfo.InvariantCulture); }
		}
		public static int CompressionMethod {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Compression method index]", "0"]); }
			set { ConfigAsker["[Server database editor - Compression method index]"] = value.ToString(CultureInfo.InvariantCulture); }
		}
		public static string CustomCompressionMethod {
			get { return ConfigAsker["[Server database editor - Custom compression library]", ""]; }
			set { ConfigAsker["[Server database editor - Custom compression library]"] = value; }
		}
		public static int CompressionLevel {
			get {
				int compression = Int32.Parse(ConfigAsker["[Server database editor - Compression level]", "6"]);

				if (compression >= 0 && compression <= 9) {
					return compression;
				}

				ConfigAsker["[Server database editor - Compression level]"] = "6";
				return 6;
			}
			set {
				if (value >= 0 && value <= 9)
					ConfigAsker["[Server database editor - Compression level]"] = value.ToString(CultureInfo.InvariantCulture);
				else
					ConfigAsker["[Server database editor - Compression level]"] = "6";
			}
		}
		public static bool AlwaysOpenAfterExtraction {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - ExtractingService - Always open after extraction]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - ExtractingService - Always open after extraction]"] = value.ToString(); }
		}
		public static bool ShowRealImages {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - PreviewService - Show real images]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - PreviewService - Show real images]"] = value.ToString(); }
		}
		public static int EncodingCodepage {
			get { return Int32.Parse(ConfigAsker["[Server database editor - Encoding codepage]", "1252"]); }
			set { ConfigAsker["[Server database editor - Encoding codepage]"] = value.ToString(CultureInfo.InvariantCulture); }
		}
		public static string ExtractingServiceLastPath {
			get { return ConfigAsker["[Server database editor - ExtractingService - Latest directory]", ""]; }
			set { ConfigAsker["[Server database editor - ExtractingService - Latest directory]"] = value; }
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
		public static bool UseGrfPathToExtract {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - ExtractingService - Use current GRF path]", false.ToString()]); }
			set { ConfigAsker["[Server database editor - ExtractingService - Use current GRF path]"] = value.ToString(); }
		}
		public static bool CpuPerformanceManagement {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Cpu performance management]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Cpu performance management]"] = value.ToString(); }
		}
		public static bool ShowGrfEditorHeader {
			get { return Boolean.Parse(ConfigAsker["[Server database editor - Show header in text files]", true.ToString()]); }
			set { ConfigAsker["[Server database editor - Show header in text files]"] = value.ToString(); }
		}
		public static int MaximumNumberOfThreads {
			get {
				int tmp = Int32.Parse(ConfigAsker["[Server database editor - Maximum number of threads]", "10"]);

				if (tmp < 1 || tmp > 50) {
					ConfigAsker["[Server database editor - Maximum number of threads]"] = "10";
					tmp = 10;
				}

				return tmp;
			}
			set {
				if (value < 1 || value > 50) {
					ConfigAsker["[Server database editor - Maximum number of threads]"] = "10";
				}
				else {
					ConfigAsker["[Server database editor - Maximum number of threads]"] = value.ToString(CultureInfo.InvariantCulture);
				}
			}
		}
		public static FileAssociation FileShellAssociated {
			get { return (FileAssociation)Enum.Parse(typeof(FileAssociation), ConfigAsker["[Server database editor - File type associated]", "0"]); }
			set { ConfigAsker["[Server database editor - File type associated]"] = value.ToString(); }
		}
		#endregion

		#region SpriteMaker
		public static string SpriteMakerPath {
			get {
				if (!Directory.Exists(Path.Combine(Methods.ApplicationPath, "SpriteMaker")))
					Directory.CreateDirectory(Path.Combine(Methods.ApplicationPath, "SpriteMaker"));

				return Path.Combine(Methods.ApplicationPath, "SpriteMaker");
			}
		}
		#endregion

		#region Encryptor
		public static string EncryptorPath {
			get {
				string path = Path.Combine(ProgramDataPath, "Encryption");

				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);

				return path;
			}
		}
		public static string EncryptorClientPath {
			get { return ConfigAsker["[Encryptor - Client path]", ""]; }
			set { ConfigAsker["[Encryptor - Client path]"] = value; }
		}
		public static string EncryptorWrapper {
			get { return ConfigAsker["[Encryptor - Wrapper name]", "cps.dll"]; }
			set { ConfigAsker["[Encryptor - Wrapper name]"] = value; }
		}
		public static byte[] EncryptorPassword {
			get;
			set;
		}
		#endregion

		#region FlatMapsMaker
		public static string FlatMapsMakerInputTexturesPath {
			get { return ConfigAsker["[FlatMapsMaker - Input textures path]", Path.Combine(ProgramDataPath, "FlatMapsMaker\\InputTextures")]; }
			set { ConfigAsker["[FlatMapsMaker - Input textures path]"] = value; }
		}
		public static string FlatMapsMakerInputMapsPath {
			get { return ConfigAsker["[FlatMapsMaker - Input maps path]", GrfPath.Combine(ProgramDataPath, "FlatMapsMaker\\InputMaps")]; }
			set { ConfigAsker["[FlatMapsMaker - Input maps path]"] = value; }
		}
		public static string FlatMapsMakerOutputMapsPath {
			get { return ConfigAsker["[FlatMapsMaker - Output maps path]", Path.Combine(ProgramDataPath, "FlatMapsMaker\\OutputMaps")]; }
			set { ConfigAsker["[FlatMapsMaker - Output maps path]"] = value; }
		}
		public static string FlatMapsMakerOutputTexturesPath {
			get { return ConfigAsker["[FlatMapsMaker - Output textures path]", Path.Combine(ProgramDataPath, "FlatMapsMaker\\OutputTextures")]; }
			set { ConfigAsker["[FlatMapsMaker - Output textures path]"] = value; }
		}
		public static bool ShowGutterLines {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Show gutter lines]", false.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Show gutter lines]"] = value.ToString(); }
		}
		public static bool RemoveAllLighting {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Remove light map]", true.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Remove light map]"] = value.ToString(); }
		}
		public static bool RemoveAllObjects {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Remove objects]", false.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Remove objects]"] = value.ToString(); }
		}
		public static bool FlattenGround {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Flatten ground]", true.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Flatten ground]"] = value.ToString(); }
		}
		public static bool UseCustomTextures {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Use custom textures]", true.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Use custom textures]"] = value.ToString(); }
		}
		public static bool StickGatCellsToGround {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Stick gat cells to the ground]", false.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Stick gat cells to the ground]"] = value.ToString(); }
		}
		public static bool RemoveWater {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Remove water]", false.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Remove water]"] = value.ToString(); }
		}
		public static bool TextureWalls {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Texture walls]", false.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Texture walls]"] = value.ToString(); }
		}
		public static bool ResetGlobalLighting {
			get { return Boolean.Parse(ConfigAsker["[FlatMapsMaker - Reset global lighting]", true.ToString()]); }
			set { ConfigAsker["[FlatMapsMaker - Reset global lighting]"] = value.ToString(); }
		}
		#endregion

		#region Grf validation

		public static bool FeNoExtension {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - No extension]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - No extension]"] = value.ToString(); }
		}
		public static bool FeMissingSprAct {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Missing spr or act files]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Missing spr or act files]"] = value.ToString(); }
		}
		public static bool FeEmptyFiles {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Empty files]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Empty files]"] = value.ToString(); }
		}
		public static bool FeDb {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Existing db files]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Existing db files]"] = value.ToString(); }
		}
		public static bool FeSvn {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Existing svn files]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Existing svn files]"] = value.ToString(); }
		}
		public static bool FeDuplicateFiles {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Duplicate files]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Duplicate files]"] = value.ToString(); }
		}
		public static bool FeDuplicatePaths {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Duplicate paths]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Duplicate paths]"] = value.ToString(); }
		}
		public static bool FeSpaceSaved {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Find space saved by repacking]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Find space saved by repacking]"] = value.ToString(); }
		}
		public static bool FeInvalidFileTable {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Invalid file table]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Invalid file table]"] = value.ToString(); }
		}
		public static bool FeRootFiles {
			get { return Boolean.Parse(ConfigAsker["[Validation - Find errors - Root files]", true.ToString()]); }
			set { ConfigAsker["[Validation - Find errors - Root files]"] = value.ToString(); }
		}

		public static bool VcDecompressEntries {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Decompress entries]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Decompress entries]"] = value.ToString(); }
		}
		public static bool VcLoadEntries {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Load entries]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Load entries]"] = value.ToString(); }
		}
		public static bool VcInvalidEntryMetadata {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect invalid entry metadata]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect invalid entry metadata]"] = value.ToString(); }
		}
		public static bool VcSpriteIssues {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect sprite issues]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect sprite issues]"] = value.ToString(); }
		}
		public static bool VcSpriteIssuesRle {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect early ending RLE encoding]", false.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect early ending RLE encoding]"] = value.ToString(); }
		}
		public static bool VcSpriteSoundIndex {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Invalid sound index]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Invalid sound index]"] = value.ToString(); }
		}
		public static bool VcSpriteSoundMissing {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect sound file not missing]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect sound file not missing]"] = value.ToString(); }
		}
		public static bool VcSpriteIndex {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Invalid sprite index]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Invalid sprite index]"] = value.ToString(); }
		}
		public static bool VcResourcesModelFiles {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect missing resources in model files]", false.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect missing resources in model files]"] = value.ToString(); }
		}
		public static bool VcResourcesMapFiles {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect missing resources in map files]", false.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect missing resources in map files]"] = value.ToString(); }
		}
		public static bool VcInvalidQuadTree {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate content - Detect invalid QuadTree]", false.ToString()]); }
			set { ConfigAsker["[Validation - Validate content - Detect invalid QuadTree]"] = value.ToString(); }
		}

		public static bool VeFilesNotFound {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate extraction - Ignore files not found]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate extraction - Ignore files not found]"] = value.ToString(); }
		}
		public static bool VeFilesDifferentSize {
			get { return Boolean.Parse(ConfigAsker["[Validation - Validate extraction - Ignore files with different size]", true.ToString()]); }
			set { ConfigAsker["[Validation - Validate extraction - Ignore files with different size]"] = value.ToString(); }
		}
		
		public static bool ValidationRawView {
			get { return Boolean.Parse(ConfigAsker["[Validation - Show raw view]", false.ToString()]); }
			set { ConfigAsker["[Validation - Show raw view]"] = value.ToString(); }
		}

		public static string VeFolder {
			get { return ConfigAsker["[Validation - Hard drive folder]", "C:\\RO\\data"]; }
			set { ConfigAsker["[Validation - Hard drive folder]"] = value; }
		}

		#endregion

		#region Lub decompiler
		public static bool UseCustomDecompiler {
			get { return Boolean.Parse(ConfigAsker["[Lub decompiler - Use GRF Editor Decompiler]", true.ToString()]); }
			set { ConfigAsker["[Lub decompiler - Use GRF Editor Decompiler]"] = value.ToString(); }
		}

		public static bool AppendFunctionId {
			get { return Boolean.Parse(ConfigAsker["[Lub decompiler - Append function Id]", true.ToString()]); }
			set { ConfigAsker["[Lub decompiler - Append function Id]"] = value.ToString(); }
		}

		public static bool UseCodeReconstructor {
			get { return Boolean.Parse(ConfigAsker["[Lub decompiler - Use code reconstructor]", true.ToString()]); }
			set { ConfigAsker["[Lub decompiler - Use code reconstructor]"] = value.ToString(); }
		}

		public static bool DecodeInstructions {
			get { return Boolean.Parse(ConfigAsker["[Lub decompiler - Decode instructions]", true.ToString()]); }
			set { ConfigAsker["[Lub decompiler - Decode instructions]"] = value.ToString(); }
		}

		public static bool GroupIfAllValues {
			get { return Boolean.Parse(ConfigAsker["[Lub decompiler - Group if all values]", true.ToString()]); }
			set { ConfigAsker["[Lub decompiler - Group if all values]"] = value.ToString(); }
		}

		public static bool GroupIfAllKeyValues {
			get { return Boolean.Parse(ConfigAsker["[Lub decompiler - Group if all key values]", true.ToString()]); }
			set { ConfigAsker["[Lub decompiler - Group if all key values]"] = value.ToString(); }
		}

		public static int TextLengthLimit {
			get { return Int32.Parse(ConfigAsker["[Lub decompiler - Text length limit]", "80"]); }
			set { ConfigAsker["[Lub decompiler - Text length limit]"] = value.ToString(CultureInfo.InvariantCulture); }
		}

		#endregion

		#region Grf compression
		public static bool CoIdenticalFiles {
			get { return Boolean.Parse(ConfigAsker["[Compression - General compression - Remove identical files]", true.ToString()]); }
			set { ConfigAsker["[Compression - General compression - Remove identical files]"] = value.ToString(); }
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
