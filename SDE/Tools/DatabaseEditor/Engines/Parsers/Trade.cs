using System;
using System.Text;
using SDE.Tools.DatabaseEditor.Generic.Lists.DbAttributeHelpers;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Engines.Parsers {
	public class Trade : ISettable {
		public string Noauction = "false";
		public string Nocart = "false";
		public string Nodrop = "false";
		public string Nogstorage = "false";
		public string Nomail = "false";
		public string Noselltonpc = "false";
		public string Nostorage = "false";
		public string Notrade = "false";
		public string Partneroverride = "false";
		private string _override = "100";

		#region ISettable Members

		public string Override {
			get { return _override; }
			set { _override = value; }
		}

		public void Set(object obj) {
			string el1 = obj.ToString();
			Noauction = Parser.GetVal(el1, "noauction", "false");
			Nocart = Parser.GetVal(el1, "nocart", "false");
			Nodrop = Parser.GetVal(el1, "nodrop", "false");
			Nogstorage = Parser.GetVal(el1, "nogstorage", "false");
			Nomail = Parser.GetVal(el1, "nomail", "false");
			Noselltonpc = Parser.GetVal(el1, "noselltonpc", "false");
			Nostorage = Parser.GetVal(el1, "nostorage", "false");
			Notrade = Parser.GetVal(el1, "notrade", "false");
			Override = Parser.GetVal(el1, "\noverride", "100");
			Partneroverride = Parser.GetVal(el1, "partneroverride", "false");
		}

		#endregion

		public override string ToString() {
			StringBuilder builder = new StringBuilder();
			builder.AppendLineUnix("{");

			if (Override != "100") {
				builder.AppendLineUnix("override: " + Override);
			}

			if (Nodrop != "false") builder.AppendLineUnix("nodrop: true");
			if (Notrade != "false") builder.AppendLineUnix("notrade: true");
			if (Partneroverride != "false") builder.AppendLineUnix("partneroverride: true");
			if (Noselltonpc != "false") builder.AppendLineUnix("noselltonpc: true");
			if (Nocart != "false") builder.AppendLineUnix("nocart: true");
			if (Nostorage != "false") builder.AppendLineUnix("nostorage: true");
			if (Nogstorage != "false") builder.AppendLineUnix("nogstorage: true");
			if (Nomail != "false") builder.AppendLineUnix("nomail: true");
			if (Noauction != "false") builder.AppendLineUnix("noauction: true");

			builder.Append("}");
			return builder.ToString();
		}

		public string ToWriteString() {
			StringBuilder builder = new StringBuilder();
			builder.AppendLineUnix("\tTrade: {");

			if (Override != "100") {
				builder.AppendLineUnix("\t\toverride: " + Override);
			}

			if (Nodrop != "false") builder.AppendLineUnix("\t\tnodrop: true");
			if (Notrade != "false") builder.AppendLineUnix("\t\tnotrade: true");
			if (Partneroverride != "false") builder.AppendLineUnix("\t\tpartneroverride: true");
			if (Noselltonpc != "false") builder.AppendLineUnix("\t\tnoselltonpc: true");
			if (Nocart != "false") builder.AppendLineUnix("\t\tnocart: true");
			if (Nostorage != "false") builder.AppendLineUnix("\t\tnostorage: true");
			if (Nogstorage != "false") builder.AppendLineUnix("\t\tnogstorage: true");
			if (Nomail != "false") builder.AppendLineUnix("\t\tnomail: true");
			if (Noauction != "false") builder.AppendLineUnix("\t\tnoauction: true");

			builder.Append("\t}");
			return builder.ToString();
		}

		public bool NeedPrinting() {
			if (Override != "100") return true;
			if (Nodrop != "false") return true;
			if (Notrade != "false") return true;
			if (Partneroverride != "false") return true;
			if (Noselltonpc != "false") return true;
			if (Nocart != "false") return true;
			if (Nostorage != "false") return true;
			if (Nogstorage != "false") return true;
			if (Nomail != "false") return true;
			if (Noauction != "false") return true;

			return false;
		}

		public void Set(string el1, string overr) {
			Override = overr;

			int val;

			if (Int32.TryParse(el1, out val)) {
				if ((val & (1 << 0)) == (1 << 0)) Nodrop = "true";
				if ((val & (1 << 1)) == (1 << 1)) Notrade = "true";
				if ((val & (1 << 2)) == (1 << 2)) Partneroverride = "true";
				if ((val & (1 << 3)) == (1 << 3)) Noselltonpc = "true";
				if ((val & (1 << 4)) == (1 << 4)) Nocart = "true";
				if ((val & (1 << 5)) == (1 << 5)) Nostorage = "true";
				if ((val & (1 << 6)) == (1 << 6)) Nogstorage = "true";
				if ((val & (1 << 7)) == (1 << 7)) Nomail = "true";
				if ((val & (1 << 8)) == (1 << 8)) Noauction = "true";
			}
		}

		public int GetInt() {
			int val = 0;
			if (Nodrop == "true") val |= (1 << 0);
			if (Notrade == "true") val |= (1 << 1);
			if (Partneroverride == "true") val |= (1 << 2);
			if (Noselltonpc == "true") val |= (1 << 3);
			if (Nocart == "true") val |= (1 << 4);
			if (Nostorage == "true") val |= (1 << 5);
			if (Nogstorage == "true") val |= (1 << 6);
			if (Nomail == "true") val |= (1 << 7);
			if (Noauction == "true") val |= (1 << 8);
			return val;
		}
	}
}