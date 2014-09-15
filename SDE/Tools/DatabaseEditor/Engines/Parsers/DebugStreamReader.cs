using System.IO;
using System.Text;

namespace SDE.Tools.DatabaseEditor.Engines.Parsers {
	public class DebugStreamReader : StreamReader {
		public DebugStreamReader(Stream stream) : base(stream) {
		}

		public DebugStreamReader(Stream stream, Encoding encoding) : base(stream, encoding) {
		}

		public int LineNumber { get; private set; }

		public override string  ReadLine() {
			LineNumber++;
			return base.ReadLine();
		}
	}
}