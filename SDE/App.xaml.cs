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
using Utilities;

namespace SDE {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		public App() {
			Configuration.ConfigAsker = SdeAppConfiguration.ConfigAsker;
			ProjectConfiguration.ConfigAsker = SdeAppConfiguration.ConfigAsker;
			Settings.TempPath = GrfPath.Combine(SdeAppConfiguration.ProgramDataPath, "~tmp");
			ErrorHandler.SetErrorHandler(new SdeErrorHandler());
			TemporaryFilesManager.ClearTemporaryFiles();
		}

		protected override void OnStartup(StartupEventArgs e) {
			ApplicationManager.CrashReportEnabled = true;
			ImageConverterManager.AddConverter(new DefaultImageConverter());
			Configuration.SetImageRendering(Resources);

			Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/" + Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "%20") + ";component/WPF/Styles/GRFEditorStyles.xaml", UriKind.RelativeOrAbsolute) });
			
			if (!Methods.IsWinVistaOrHigher() && Methods.IsWinXPOrHigher()) {
				// We are on Windows XP, force the style.
				try {
					Uri uri = new Uri("PresentationFramework.Aero;V3.0.0.0;31bf3856ad364e35;component\\themes/aero.normalcolor.xaml", UriKind.Relative);
					Resources.MergedDictionaries.Add(LoadComponent(uri) as ResourceDictionary);
				}
				catch { }
			}

			if (!CustomStyles.StyleSet) {
				CustomStyles.SetDefault();
			}

			base.OnStartup(e);
		}
	}
}
