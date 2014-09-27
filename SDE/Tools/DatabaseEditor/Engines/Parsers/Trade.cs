using System;
using System.Text;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Engines.Parsers {
	[Flags]
	public enum TradeFlag {
		Nodrop = 1 << 0,
		Notrade = 1 << 1,
		Partneroverride = 1 << 2,
		Noselltonpc = 1 << 3,
		Nocart = 1 << 4,
		Nostorage = 1 << 5,
		Nogstorage = 1 << 6,
		Nomail = 1 << 7,
		Noauction = 1 << 8
	}

	public class Trade : ISettable {
		public TradeFlag Flag { get; set; }
		private string _override = "100";

		#region ISettable Members

		public string Override {
			get { return _override; }
			set { _override = value; }
		}

		public void Set(object obj) {
			string el1 = obj.ToString();
			Flag = 0;
			Flag |= ParserHelper.GetVal(el1, "noauction", "false") == "true" ? TradeFlag.Noauction : 0;
			Flag |= ParserHelper.GetVal(el1, "nocart", "false") == "true" ? TradeFlag.Nocart : 0;
			Flag |= ParserHelper.GetVal(el1, "nodrop", "false") == "true" ? TradeFlag.Nodrop : 0;
			Flag |= ParserHelper.GetVal(el1, "nogstorage", "false") == "true" ? TradeFlag.Nogstorage : 0;
			Flag |= ParserHelper.GetVal(el1, "nomail", "false") == "true" ? TradeFlag.Nomail : 0;
			Flag |= ParserHelper.GetVal(el1, "noselltonpc", "false") == "true" ? TradeFlag.Noselltonpc : 0;
			Flag |= ParserHelper.GetVal(el1, "nostorage", "false") == "true" ? TradeFlag.Nostorage : 0;
			Flag |= ParserHelper.GetVal(el1, "notrade", "false") == "true" ? TradeFlag.Notrade : 0;
			Flag |= ParserHelper.GetVal(el1, "partneroverride", "false") == "true" ? TradeFlag.Partneroverride : 0;
			Override = ParserHelper.GetVal(el1, "\noverride", "100");
		}

		public int GetInt() {
			return (int) Flag;
		}

		#endregion

		public override string ToString() {
			StringBuilder builder = new StringBuilder();
			builder.AppendLineUnix("{");

			if (Override != "100") {
				builder.AppendLineUnix("override: " + Override);
			}

			if ((Flag & TradeFlag.Noauction) == TradeFlag.Noauction) builder.AppendLineUnix("noauction: true");
			if ((Flag & TradeFlag.Nocart) == TradeFlag.Nocart) builder.AppendLineUnix("nocart: true");
			if ((Flag & TradeFlag.Nodrop) == TradeFlag.Nodrop) builder.AppendLineUnix("nodrop: true");
			if ((Flag & TradeFlag.Nogstorage) == TradeFlag.Nogstorage) builder.AppendLineUnix("nogstorage: true");
			if ((Flag & TradeFlag.Nomail) == TradeFlag.Nomail) builder.AppendLineUnix("nomail: true");
			if ((Flag & TradeFlag.Noselltonpc) == TradeFlag.Noselltonpc) builder.AppendLineUnix("noselltonpc: true");
			if ((Flag & TradeFlag.Nostorage) == TradeFlag.Nostorage) builder.AppendLineUnix("nostorage: true");
			if ((Flag & TradeFlag.Notrade) == TradeFlag.Notrade) builder.AppendLineUnix("notrade: true");
			if ((Flag & TradeFlag.Partneroverride) == TradeFlag.Partneroverride) builder.AppendLineUnix("partneroverride: true");

			builder.Append("}");
			return builder.ToString();
		}

		public string ToWriteString() {
			StringBuilder builder = new StringBuilder();
			builder.AppendLineUnix("\tTrade: {");

			if (Override != "100") {
				builder.AppendLineUnix("\t\toverride: " + Override);
			}

			if ((Flag & TradeFlag.Noauction) == TradeFlag.Noauction) builder.AppendLineUnix("\t\tnoauction: true");
			if ((Flag & TradeFlag.Nocart) == TradeFlag.Nocart) builder.AppendLineUnix("\t\tnocart: true");
			if ((Flag & TradeFlag.Nodrop) == TradeFlag.Nodrop) builder.AppendLineUnix("\t\tnodrop: true");
			if ((Flag & TradeFlag.Nogstorage) == TradeFlag.Nogstorage) builder.AppendLineUnix("\t\tnogstorage: true");
			if ((Flag & TradeFlag.Nomail) == TradeFlag.Nomail) builder.AppendLineUnix("\t\tnomail: true");
			if ((Flag & TradeFlag.Noselltonpc) == TradeFlag.Noselltonpc) builder.AppendLineUnix("\t\tnoselltonpc: true");
			if ((Flag & TradeFlag.Nostorage) == TradeFlag.Nostorage) builder.AppendLineUnix("\t\tnostorage: true");
			if ((Flag & TradeFlag.Notrade) == TradeFlag.Notrade) builder.AppendLineUnix("\t\tnotrade: true");
			if ((Flag & TradeFlag.Partneroverride) == TradeFlag.Partneroverride) builder.AppendLineUnix("\t\tpartneroverride: true");

			builder.Append("\t}");
			return builder.ToString();
		}

		public bool NeedPrinting() {
			if (Override != "100") return true;
			if ((Flag & TradeFlag.Noauction) == TradeFlag.Noauction) return true;
			if ((Flag & TradeFlag.Nocart) == TradeFlag.Nocart) return true;
			if ((Flag & TradeFlag.Nodrop) == TradeFlag.Nodrop) return true;
			if ((Flag & TradeFlag.Nogstorage) == TradeFlag.Nogstorage) return true;
			if ((Flag & TradeFlag.Nomail) == TradeFlag.Nomail) return true;
			if ((Flag & TradeFlag.Noselltonpc) == TradeFlag.Noselltonpc) return true;
			if ((Flag & TradeFlag.Nostorage) == TradeFlag.Nostorage) return true;
			if ((Flag & TradeFlag.Notrade) == TradeFlag.Notrade) return true;
			if ((Flag & TradeFlag.Partneroverride) == TradeFlag.Partneroverride) return true;

			return false;
		}

		public void Set(string el1, string overr) {
			Override = overr;

			int val;

			if (Int32.TryParse(el1, out val)) {
				Flag = (TradeFlag) val;
			}
		}
	}
}