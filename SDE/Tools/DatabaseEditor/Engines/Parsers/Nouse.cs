using System.Text;
using SDE.Tools.DatabaseEditor.Generic.Lists.DbAttributeHelpers;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Engines.Parsers {
	public class Nouse : ISettable {
		public string Sitting = "false";
		private string _override = "100";

		#region ISettable Members

		public string Override {
			get { return _override; }
			set { _override = value; }
		}

		public void Set(object value) {
			string el1 = value.ToString();
			Override = Parser.GetVal(el1, "override", "100");
			Sitting = Parser.GetVal(el1, "sitting", "false");
		}

		#endregion

		public override string ToString() {
			StringBuilder builder = new StringBuilder();
			builder.AppendLineUnix("{");

			if (Override != "100") {
				builder.AppendLineUnix("override: " + Override);
			}

			if (Sitting != "false") builder.AppendLineUnix("sitting: true");

			builder.Append("}");
			return builder.ToString();
		}

		public string ToWriteString() {
			StringBuilder builder = new StringBuilder();
			builder.AppendLineUnix("\tNouse: {");

			if (Override != "100") {
				builder.AppendLineUnix("\t\toverride: " + Override);
			}

			if (Sitting != "false") builder.AppendLineUnix("\t\tsitting: true");

			builder.Append("\t}");
			return builder.ToString();
		}

		public bool NeedPrinting() {
			if (Override != "100") return true;
			if (Sitting != "false") return true;
			return false;
		}

		public int GetInt() {
			int val = 0;
			if (Sitting == "true") val |= (1 << 0);
			return val;
		}
	}
}