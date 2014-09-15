namespace SDE.Tools.DatabaseEditor {
	public sealed class SdeFiles {
		public static readonly SdeFiles ServerDbPath = new SdeFiles();

		private string _filename = "null";

		public string Filename {
			get { return _filename; }
			set { _filename = value; }
		}

		public static implicit operator string(SdeFiles item) {
			return item.Filename;
		}
	}
}
