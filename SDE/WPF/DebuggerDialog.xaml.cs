using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ErrorManager;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Extensions = SDE.Core.Extensions;

namespace SDE.WPF {
	/// <summary>
	/// This window runs in the background and shows any catched exceptions if it's activated.
	/// Interaction logic for DebuggerDialog.xaml
	/// </summary>
	public partial class DebuggerDialog : Window {
		private const int GWL_STYLE = -16;
		private const int WS_SYSMENU = 0x80000;
		private readonly DebuggerParameters _recentDebugInfo = new DebuggerParameters();
		private bool _backgroundThreadRunning = true;
		private bool _enableClosing;

		public DebuggerDialog(bool isMain = true) {
			InitializeComponent();

			string icon = "cde.ico";
			Stream bitmapStream;
			try {
				bitmapStream = new MemoryStream(ApplicationManager.GetResource(icon));
			}
			catch {
				bitmapStream = null;
				MessageBox.Show("Couldn't find the icon file in the program's resources. The icon must be a .ico file and it must be placed within the application's resources (embedded resource).");
				if (TkWindow.ShutDownOnInvalidIcons)
					ApplicationManager.Shutdown();
			}
			if (bitmapStream == null) {
				MessageBox.Show("Couldn't find the icon file in the program's resources.");
				if (!TkWindow.ShutDownOnInvalidIcons)
					return;
				ApplicationManager.Shutdown();
			}
			else {
				try {
					Icon = new IconBitmapDecoder(bitmapStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None).Frames[0];
				}
				catch (Exception) {
					try {
						Icon = ApplicationManager.GetResourceImage(icon);
						return;
					}
					catch {
					}
					MessageBox.Show("Invalid icon file.");
					if (!TkWindow.ShutDownOnInvalidIcons)
						return;
					Application.Current.Shutdown();
				}
			}

			Extensions.GenerateListViewTemplate(_listViewStackTrace, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "File name", DisplayExpression = "FileName", ToolTipBinding="FileName", TextAlignment = TextAlignment.Right, FixedWidth = 180 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Line", DisplayExpression = "Line", ToolTipBinding="Line", TextAlignment = TextAlignment.Left, FixedWidth = 60 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Method", DisplayExpression = "Method", ToolTipBinding="Method", TextAlignment = TextAlignment.Left, IsFill = true}
			}, null, new string[] { "Default", "Black" });

			Loaded += _loaded;
			Left = 0;
			Top = 0;

			if (!isMain)
				_enableClosing = true;

			new Thread(_backgroundWindowOwnership) { Name = "GrfEditor - Debugger primary thread" }.Start();
		}

		public Window DispatchedOwner {
			get { return this.Dispatch(() => Owner); }
		}

		private void _backgroundWindowOwnership() {
			while (_backgroundThreadRunning) {
				try {
					Thread.Sleep(700);

					if (!Configuration.EnableDebuggerTrace)
						continue;

					Application.Current.Dispatcher.Invoke(new Action(delegate {
						if (_enableClosing || Application.Current.MainWindow == null)
							return;

						foreach (Window window in Application.Current.MainWindow.OwnedWindows) {
							if (window is ErrorDialog) {
								if (((ErrorDialog)window).ErrorLevel == ErrorLevel.NotSpecified)
									return;

								_enableClosing = true;

								DebuggerDialog dialog = new DebuggerDialog(false);
								
								Visibility = Visibility.Hidden;
								dialog.Width = Width;
								dialog.Height = Height;
								dialog.Left = Left;
								dialog.Top = Top;

								window.Closed += delegate {
									Width = dialog.Width;
									Height = dialog.Height;
									Left = dialog.Left;
									Top = dialog.Top;
									dialog.Close();
									Visibility = Visibility.Visible;
									_enableClosing = false;

									Application.Current.Dispatcher.Invoke(new Action(delegate {
										Application.Current.MainWindow.Activate();
										Application.Current.MainWindow.Topmost = true;
										Application.Current.MainWindow.Topmost = false;
										Application.Current.MainWindow.Focus();
									}));
								};
								dialog.Update(_recentDebugInfo);
								dialog.Show();
							}
						}
					}));
				}
				catch (Exception err) {
					MessageBox.Show(err.Message);
				}
			}
		}

		private void _loaded(object sender, RoutedEventArgs e) {
			Title += " - Running";
			var hwnd = new WindowInteropHelper(this).Handle;
			NativeMethods.SetWindowLong(hwnd, GWL_STYLE, NativeMethods.GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
		}

		protected override void OnClosing(CancelEventArgs e) {
			if (!_enableClosing) {
				e.Cancel = true;
			}

			_backgroundThreadRunning = false;
			base.OnClosing(e);
		}

		public void Update(DebuggerParameters parameters) {
			Update(parameters.Time, parameters.Exception, parameters.StackTrace, parameters.Message);
		}

		public void Update(DateTime now, Exception exception, StackTrace st, string message) {
			_recentDebugInfo.Time = now;
			_recentDebugInfo.Exception = exception;
			_recentDebugInfo.StackTrace = st;
			_recentDebugInfo.Message = message;

			_tbTime.Text = now.ToString(CultureInfo.InvariantCulture);

			if (message != null) {
				_tbMessage.Text = message;
			}
			else if (exception != null) {
				_tbMessage.Text = exception.Message;
			}
			else {
				_tbMessage.Text = "#Exception parameter is null - no exception message available";
			}

			_tbStackTrace.Text =
				ApplicationManager.PrettyLine("Stack trace") + Environment.NewLine + st + Environment.NewLine +
				ApplicationManager.PrettyLine("Exception") + Environment.NewLine + exception + Environment.NewLine +
				ApplicationManager.PrettyLine("Inner exception") + Environment.NewLine + (exception == null ? "" : (exception.InnerException == null ? "" : exception.InnerException.ToString()));
			
			_listViewStackTrace.Items.Clear();

			_listViewStackTrace.Items.Add(new DebuggerStackTraceLineView { Method = ApplicationManager.PrettyLine("Stack trace"), FileName = "------------------------------", Line = "-" });

			for (int i = 0; i < st.FrameCount; i++) {
				StackFrame sf = st.GetFrame(i);
				_listViewStackTrace.Items.Add(new DebuggerStackTraceLineView { Method = _removeNamespaces(sf.GetMethod()), FileName = Path.GetFileName(sf.GetFileName()), Line = sf.GetFileLineNumber().ToString(CultureInfo.InvariantCulture) });
			}

			_listViewStackTrace.Items.Add(new DebuggerStackTraceLineView { Method = ApplicationManager.PrettyLine("Exception"), FileName = "------------------------------", Line = "-" });
			
			if (exception != null) {
				st = new StackTrace(exception, true);

				for (int i = 0; i < st.FrameCount; i++) {
					StackFrame sf = st.GetFrame(i);
					_listViewStackTrace.Items.Add(new DebuggerStackTraceLineView { Method = _removeNamespaces(sf.GetMethod()), FileName = Path.GetFileName(sf.GetFileName()), Line = sf.GetFileLineNumber().ToString(CultureInfo.InvariantCulture) });
				}
			}

			_listViewStackTrace.Items.Add(new DebuggerStackTraceLineView { Method = ApplicationManager.PrettyLine("Inner exception"), FileName = "------------------------------", Line = "-" });

			if (exception != null && exception.InnerException != null) {
				st = new StackTrace(exception.InnerException, true);

				for (int i = 0; i < st.FrameCount; i++) {
					StackFrame sf = st.GetFrame(i);
					_listViewStackTrace.Items.Add(new DebuggerStackTraceLineView { Method = _removeNamespaces(sf.GetMethod()), FileName = Path.GetFileName(sf.GetFileName()), Line = sf.GetFileLineNumber().ToString(CultureInfo.InvariantCulture) });
				}
			}
		}

		private string _removeNamespaces(MethodBase method) {
			string toOutput = method.Name + "(";
			foreach (ParameterInfo x in method.GetParameters()) {
				toOutput += x.ParameterType.ToString().Split('.').Last() + ", ";
			}
			toOutput += ")";

			if (toOutput.EndsWith(", )"))
				toOutput = toOutput.Remove(toOutput.Length - 3);

			return toOutput + ")";
		}

		private void _buttonCopy_Click(object sender, RoutedEventArgs e) {
			Clipboard.SetText(_tbStackTrace.Text);
			MessageBox.Show("Raw exception copied.");
		}

		private void _buttonTerminate_Click(object sender, RoutedEventArgs e) {
			Process.GetCurrentProcess().Kill();
			Application.Current.Shutdown(-1);
		}

		#region Nested type: DebuggerParameters

		public class DebuggerParameters {
			public DateTime Time { get; set; }
			public Exception Exception { get; set; }
			public StackTrace StackTrace { get; set; }
			public string Message { get; set; }
		}

		#endregion

		#region Nested type: DebuggerStackTraceLineView

		public class DebuggerStackTraceLineView {
			public string Method { get; set; }
			public string FileName { get; set; }
			public string Line { get; set; }

			public bool Default {
				get { return true; }
			}
		}

		#endregion
	}
}
