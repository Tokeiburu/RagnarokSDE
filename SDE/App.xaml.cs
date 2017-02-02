using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using ErrorManager;
using GRF.IO;
using GRF.Image;
using GRF.System;
using GRF.Threading;
using GrfToWpfBridge.Application;
using SDE.ApplicationConfiguration;
using SDE.Editor;
using TokeiLibrary;
using Utilities;

namespace SDE {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		public App() {
			Configuration.ConfigAsker = SdeAppConfiguration.ConfigAsker;
			ProjectConfiguration.ConfigAsker = SdeAppConfiguration.ConfigAsker;
			Settings.TempPath = GrfPath.Combine(SdeAppConfiguration.ProgramDataPath, "~tmp\\" + SdeAppConfiguration.ProcessId);
			ErrorHandler.SetErrorHandler(new DefaultErrorHandler());
			ClearTemporaryFiles();
		}

		/// <summary>
		/// Clears the temporary files, this method is different than GRFE's (and it is better).
		/// </summary>
		public static void ClearTemporaryFiles() {
			GrfThread.Start(delegate {
				int errorCount = 0;

				// Clear root files only
				foreach (string file in Directory.GetFiles(GrfPath.Combine(SdeAppConfiguration.ProgramDataPath, "~tmp"), "*", SearchOption.TopDirectoryOnly)) {
					if (!GrfPath.Delete(file)) {
						errorCount++;
					}

					if (errorCount > 20)
						break;
				}

				// There will be no files in the temporary folder anymore
				foreach (var directory in Directory.GetDirectories(GrfPath.Combine(SdeAppConfiguration.ProgramDataPath, "~tmp"), "*")) {
					string folderName = Path.GetFileName(directory);
					int processId;

					if (Int32.TryParse(folderName, out processId)) {
						try {
							Process.GetProcessById(processId);
							// Do not bother if the process exists, even if it's not SDE
						}
						catch (ArgumentException) {
							// The process is not running
							try {
								Directory.Delete(directory, true);
							}
							catch { }
						}
						catch {
							// Do nothing
						}
					}
				}
			}, "GRF - TemporaryFilesManager cleanup");
		}

		protected override void OnStartup(StartupEventArgs e) {
			ApplicationManager.CrashReportEnabled = true;
			ImageConverterManager.AddConverter(new DefaultImageConverter());
			Configuration.SetImageRendering(Resources);

			Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/" + Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "%20") + ";component/WPF/Styles/GRFEditorStyles.xaml", UriKind.RelativeOrAbsolute) });

			if (!Methods.IsWinVistaOrHigher() && Methods.IsWinXPOrHigher()) {
				// We are on Windows XP, force the style.
				_installTheme();
			}

			base.OnStartup(e);
		}

		private bool _installTheme() {
			try {
				Uri uri = new Uri("PresentationFramework.Aero;V3.0.0.0;31bf3856ad364e35;component\\themes/aero.normalcolor.xaml", UriKind.Relative);
				Resources.MergedDictionaries.Add(LoadComponent(uri) as ResourceDictionary);
				return true;
			}
			catch {
				return false;
			}
		}
	}
}
