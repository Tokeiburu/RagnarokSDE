using System;
using System.Collections.Generic;
using ErrorManager;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines.Parsers;
using SDE.View;
using TokeiLibrary;
using Utilities.CommandLine;

namespace SDE.Editor.Generic.Parsers.Generic {
	public interface IErrorListener {
		void Handle(Exception err, string exception);
		void Handle(Exception err, string exception, ErrorLevel errorLevel);
	}

	public class DbIOErrorHandler {
		private static readonly List<IErrorListener> _listeners = new List<IErrorListener>();
		private static int _start;

		public static void AddListener(IErrorListener listener) {
			_listeners.Add(listener);
		}

		public static void ClearListeners() {
			_listeners.Clear();
		}

		public static void Handle(Exception err, string exception, ErrorLevel level) {
			if (_listeners.Count == 0)
				CLHelper.Error = exception;
			else
				_listeners.ForEach(p => p.Handle(err, exception, level));
		}

		public static void Handle(Exception err, string exception) {
			Handle(StackTraceException.GetStrackTraceException(), exception, ErrorLevel.Warning);
		}

		public static void Handle(Exception err, string exception, string id, int line, string file, ErrorLevel level) {
			Handle(err, String.Format("ID: {0}, file: '{1}', line: {2}, exception: '{3}'", id, file, line, exception), level);
		}

		public static void HandleLoader(Exception err, string exception) {
			DebugStreamReader reader = TextFileHelper.LastReader;

			if (reader != null) {
				Handle(err, String.Format("file: '{0}', line: {1}, exception: '{2}'", TextFileHelper.LatestFile, reader.LineNumber, exception), ErrorLevel.Warning);
			}
			else {
				Handle(err, String.Format("exception: '{0}'", exception), ErrorLevel.Warning);
			}
		}

		public static void Handle(Exception err, string exception, string id) {
			Handle(err, exception, id, ErrorLevel.Warning);
		}

		public static void Handle(Exception err, string exception, string id, int line) {
			Handle(err, exception, id, line, ErrorLevel.Warning);
		}

		public static void Handle(Exception err, string exception, string id, int line, ErrorLevel errorLevel) {
			if (line < 0) {
				Handle(err, exception, id, errorLevel);
				return;
			}

			Handle(err, String.Format("ID: {0}, file: '{1}', line: {2}, exception: '{3}'", id, TextFileHelper.LatestFile, line, exception), errorLevel);
		}

		public static void Handle(Exception err, string exception, string id, ErrorLevel errorLevel) {
			DebugStreamReader reader = TextFileHelper.LastReader;

			if (reader != null) {
				Handle(err, String.Format("ID: {0}, file: '{1}', line: {2}, exception: '{3}'", id, TextFileHelper.LatestFile, reader.LineNumber, exception), errorLevel);
			}
			else {
				Handle(err, String.Format("ID: {0}, exception: '{1}'", id, exception), errorLevel);
			}
		}

		public static void Focus() {
			if (_listeners.Count > 0 && _listeners[0] is SdeEditor) {
				((SdeEditor)_listeners[0]).Dispatch(p => p._mainTabControl.SelectedIndex = 1);
			}
		}

		public static void Start() {
			if (_listeners.Count > 0 && _listeners[0] is SdeEditor) {
				_start = ((SdeEditor)_listeners[0]).Dispatch(p => p._debugList.Items.Count);
			}
		}

		public static void Stop() {
			if (_listeners.Count > 0 && _listeners[0] is SdeEditor) {
				if (((SdeEditor)_listeners[0]).Dispatch(p => p._debugList.Items.Count) != _start) {
					Focus();
				}
			}
		}
	}
}