using System;
using System.Diagnostics;

namespace SDE.ApplicationConfiguration {
	public static class StackTraceException {
		public static Exception GetStrackTraceException() {
			return new Exception(new StackTrace().ToString());
		}
	}
}