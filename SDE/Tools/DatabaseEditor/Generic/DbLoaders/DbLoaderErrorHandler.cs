using System;
using System.Collections.Generic;
using ErrorManager;
using GRF.System;
using SDE.Tools.DatabaseEditor.Engines.Parsers;
using TokeiLibrary;
using Utilities.CommandLine;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders {
	public class DbLoaderErrorHandler {
		private static readonly List<IErrorListener> _listeners = new List<IErrorListener>();
		private static int _start;

		public static void AddListener(IErrorListener listener) {
			_listeners.Add(listener);
		}

		public static void ClearListeners() {
			_listeners.Clear();
		}

		public static void Handle(string exception, ErrorLevel level) {
			if (_listeners.Count == 0)
				CLHelper.Error = exception;
			else
				_listeners.ForEach(p => p.Handle(exception, level));
		}

		public static void Handle(string exception) {
			Handle(exception, ErrorLevel.Warning);
		}

		public static void Handle(string exception, string id, int line, string file, ErrorLevel level) {
			Handle(String.Format("ID: {0}, file: '{1}', line: {2}, exception: '{3}'", id, file, line, exception), level);
		}

		public static void HandleLoader(string exception) {
			DebugStreamReader reader = TextFileHelper.LastReader;

			if (reader != null) {
				Handle(String.Format("file: '{0}', line: {1}, exception: '{2}'", AllLoaders.LatestFile, reader.LineNumber, exception), ErrorLevel.Warning);
			}
			else {
				Handle(String.Format("exception: '{0}'", exception), ErrorLevel.Warning);
			}
		}

		public static void Handle(string exception, string id) {
			DebugStreamReader reader = TextFileHelper.LastReader;

			if (reader != null) {
				Handle(String.Format("ID: {0}, file: '{1}', line: {2}, exception: '{3}'", id, AllLoaders.LatestFile, reader.LineNumber, exception), ErrorLevel.Warning);
			}
			else {
				Handle(String.Format("ID: {0}, exception: '{1}'", id, exception), ErrorLevel.Warning);
			}
		}

		public static void Focus() {
			if (_listeners.Count > 0 && _listeners[0] is SDEditor) {
				((SDEditor) _listeners[0]).Dispatch(p => p._mainTabControl.SelectedIndex = 1);
			}
		}

		public static void Start() {
			if (_listeners.Count > 0 && _listeners[0] is SDEditor) {
				_start = ((SDEditor)_listeners[0]).Dispatch(p => p._debugList.Items.Count);
			}
		}

		public static void Stop() {
			if (_listeners.Count > 0 && _listeners[0] is SDEditor) {
				if (((SDEditor)_listeners[0]).Dispatch(p => p._debugList.Items.Count) != _start) {
					Focus();
				}
			}
		}
	}
}