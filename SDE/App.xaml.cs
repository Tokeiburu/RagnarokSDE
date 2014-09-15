using System;
using System.Reflection;
using System.Windows;
using ErrorManager;
using GRF.IO;
using GRF.Image;
using GRF.System;
using GrfToWpfBridge.Application;
using SDE.ApplicationConfiguration;
using SDE.Tools.DatabaseEditor;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;

namespace SDE {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		public App() {
			Configuration.ConfigAsker = SDEAppConfiguration.ConfigAsker;
			SDEConfiguration.ConfigAsker = SDEAppConfiguration.ConfigAsker;
			Settings.TempPath = GrfPath.Combine(SDEAppConfiguration.ProgramDataPath, "~tmp");
			ErrorHandler.SetErrorHandler(new SDEErrorHandler());
			TemporaryFilesManager.ClearTemporaryFiles();
		}

		protected override void OnStartup(StartupEventArgs e) {
			ApplicationManager.CrashReportEnabled = true;
			ImageConverterManager.AddConverter(new DefaultImageConverter());

			Configuration.SetImageRendering(Resources);

			Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/" + Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "%20") + ";component/WPF/Styles/GRFEditorStyles.xaml", UriKind.RelativeOrAbsolute) });

			if (!CustomStyles.StyleSet) {
				CustomStyles.SetDefault();
			}

			base.OnStartup(e);
		}
	}
}
