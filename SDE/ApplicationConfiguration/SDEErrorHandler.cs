using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using ErrorManager;
using SDE.WPF;
using TokeiLibrary;
using TokeiLibrary.WPF;

namespace SDE.ApplicationConfiguration {
	public class SDEErrorHandler : IErrorHandler {
		private static DebuggerDialog _debugDialog;
		private string _latestException;

		static SDEErrorHandler() {
			if (Configuration.EnableDebuggerTrace) {
				if (_debugDialog == null)
					_debugDialog = new DebuggerDialog();
				
				if (!_debugDialog.IsLoaded)
					_debugDialog.Show();

				if (!_debugDialog.IsVisible) {
					_debugDialog.Visibility = Visibility.Visible;
				}
			}
		}

		public bool IgnoreNoMainWindow { get; set; }

		#region IErrorHandler Members

		public void Handle(Exception exception, ErrorLevel errorLevel) {
			if (exception is WrappedException)
				_reportAnyManagedExceptions(exception.Message, ((WrappedException) exception).Exception, errorLevel);
			else
				_reportAnyManagedExceptions(exception.Message, exception, errorLevel);

			if (errorLevel < SDEAppConfiguration.WarningLevel) return;

			if (_exceptionAlreadyShown(exception.Message)) return;

			if (Application.Current != null) {
				_checkMainWindow();

				if (IgnoreNoMainWindow) {
					WindowProvider.ShowWindow(new ErrorDialog("Information", _getHeader(errorLevel) + exception.Message, errorLevel), WpfUtilities.TopWindow);
				}
				else {
					if (!(bool)Application.Current.Dispatcher.Invoke(new Func<bool>(() => Application.Current.MainWindow != null && Application.Current.MainWindow.IsLoaded))) {
						_showBasicError(_getHeader(errorLevel) + exception.Message, "Information");
						return;
					}

					Application.Current.Dispatcher.Invoke((Action)(() => WindowProvider.ShowWindow(new ErrorDialog("Information", _getHeader(errorLevel) + exception.Message, errorLevel), Application.Current.MainWindow)));
				}
			}
		}

		public void Handle(string exception, ErrorLevel errorLevel) {
			_reportAnyManagedExceptions(exception, null, errorLevel);

			if (errorLevel < SDEAppConfiguration.WarningLevel) return;
			if (_exceptionAlreadyShown(exception)) return;

			if (Application.Current != null) {
				_checkMainWindow();

				if (IgnoreNoMainWindow) {
					WindowProvider.ShowWindow(new ErrorDialog("Information", _getHeader(errorLevel) + exception, errorLevel), WpfUtilities.TopWindow);
				}
				else {
					if (!(bool) Application.Current.Dispatcher.Invoke(new Func<bool>(() => Application.Current.MainWindow != null && Application.Current.MainWindow.IsLoaded))) { _showBasicError(_getHeader(errorLevel) + exception, "Information");
						return;
					}

					Application.Current.Dispatcher.Invoke((Action) (() => WindowProvider.ShowWindow(new ErrorDialog("Information", _getHeader(errorLevel) + exception, errorLevel), Application.Current.MainWindow)));
				}
			}
		}

		public void Handle(object caller, Exception exception, ErrorLevel errorLevel) {
			_reportAnyManagedExceptions(exception.Message, exception, errorLevel);
			Handle(caller, exception.Message, errorLevel);
		}

		public void Handle(object caller, string exception, ErrorLevel errorLevel) {
			_reportAnyManagedExceptions(exception, null, errorLevel);
			if (errorLevel < SDEAppConfiguration.WarningLevel) return;
			if (Application.Current != null) {
				if (!(bool) Application.Current.Dispatcher.Invoke(new Func<bool>(() => Application.Current.MainWindow != null && Application.Current.MainWindow.IsLoaded))) {
					_showBasicError(_getHeader(errorLevel) + exception, "Service report - " + Path.GetExtension(caller.GetType().ToString()).Remove(0, 1));
					return;
				}

				Application.Current.Dispatcher.Invoke((Action) (() => {
					WindowProvider.ShowWindow(new ErrorDialog("Service report - " + Path.GetExtension(caller.GetType().ToString()).Remove(0, 1),
					                                          _getHeader(errorLevel) + exception, errorLevel), Application.Current.MainWindow);
				}));
			}
		}

		public bool YesNoRequest(string message, string caption) {
			return WindowProvider.ShowDialog(message, caption, MessageBoxButton.YesNo) == MessageBoxResult.Yes;
		}

		#endregion

		private bool _exceptionAlreadyShown(string exception) {
			try {
				if (_latestException == null) {
					_latestException = exception;
					return false;
				}

				if (_latestException != null) {
					if (exception == _latestException) {
						if (Application.Current != null) {
							bool res = (bool) Application.Current.Dispatcher.Invoke(new Func<bool>(delegate {
								try {
									return Application.Current.Windows.OfType<ErrorDialog>().Any();
								}
								catch {
									return false;
								}
							}));

							if (!res) {
								_latestException = exception;
							}

							return res;
						}
					}
				}

				_latestException = exception;
				return false;
			}
			catch {
				return false;
			}
		}

		private void _checkMainWindow() {
			Application.Current.Dispatcher.Invoke(new Action(delegate {
				if (Application.Current.MainWindow == null || !Application.Current.MainWindow.IsLoaded || Application.Current.MainWindow == _debugDialog) {
					foreach (Window window in Application.Current.Windows) {
						if (window.Visibility == Visibility.Visible && window.IsLoaded && window != _debugDialog) {
							Application.Current.MainWindow = window;
						}
					}
				}
			}));
		}

		public static void ShowStackTraceViewer() {
			if (_debugDialog == null)
				_debugDialog = new DebuggerDialog();
				
			_debugDialog.Dispatcher.Invoke(new Action(delegate {
				if (Configuration.EnableDebuggerTrace) {
					if (!_debugDialog.IsLoaded)
						_debugDialog.Show();

					if (!_debugDialog.IsVisible) {
						_debugDialog.Visibility = Visibility.Visible;
					}
				}
				else {
					if (_debugDialog.IsLoaded) {
						_debugDialog.Visibility = Visibility.Hidden;
					}
				}

				foreach (Window window in Application.Current.Windows) {
					if (window.Visibility == Visibility.Visible && window.IsLoaded && window != _debugDialog) {
						Application.Current.MainWindow = window;
					}
				}
			}));
		}

		private void _reportAnyManagedExceptions(string message, Exception exception, ErrorLevel errorLevel) {
			if (Configuration.LogAnyExceptions) {
				try {
					string crash = "\r\n\r\n\r\n" +
					               ApplicationManager.PrettyLine(DateTime.Now.ToString(CultureInfo.InvariantCulture)) + "\r\n";

					try {
						if (exception != null) {
							crash += exception.ToString();
						}
						else {
							StackTrace st = new StackTrace(true);
							crash += st.ToString();
						}
					}

					catch { }
					try {
						if (exception != null)
							crash += "\r\n" + ApplicationManager.PrettyLine("Inner exception") + "\r\n" + exception.InnerException;
					}
					catch { }
					File.AppendAllText(Path.Combine(SDEAppConfiguration.ProgramDataPath, "debug.log"), crash);
				}
				catch { }
			}

			StackTrace trace = new StackTrace(true);

			if (Configuration.EnableDebuggerTrace) {
				if (_debugDialog == null)
					_debugDialog = new DebuggerDialog();

				if (errorLevel == ErrorLevel.NotSpecified)
					return;

				_debugDialog.Dispatcher.Invoke(new Action(delegate {
					if (!_debugDialog.IsLoaded)
						_debugDialog.Show();

					if (!_debugDialog.IsVisible) {
						_debugDialog.Visibility = Visibility.Visible;
						_debugDialog.Topmost = true;
						_debugDialog.Topmost = false;
					}

					_debugDialog.Update(DateTime.Now, exception, trace, message);
				}));
			}
		}

		private static void _showBasicError(string message, string caption) {
			MessageBox.Show("An error has been encountered before the application could load properly.\n" + message, caption);
		}

		private static string _getHeader(ErrorLevel level) {
			string headerMessage = "";

			switch (level) {
				case ErrorLevel.Warning:
					headerMessage = "An unhandled exception has been thrown :\n\n";
					break;
				case ErrorLevel.Critical:
					headerMessage = "A critical error has been encountered.\n\n";
					break;
			}

			return headerMessage;
		}
	}
}
