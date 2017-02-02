namespace SDE.Editor.Jobs {
	public class BaseJob {
		public int Id { get; private set; }
		public string BaseName { get; private set; }

		public BaseJob(int id, string baseName) {
			Id = id;
			BaseName = baseName;
		}
	}
}