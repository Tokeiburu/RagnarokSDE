using SDE.Core;

namespace SDE.Tools.DatabaseEditor.Engines.DatabaseEngine {
	/// <summary>
	/// Base class which is responsible to load or save the entire database
	/// </summary>
	public class BaseGenericDatabase {
		#region Delegates

		public delegate void ClientDatabaseEventHandler(object sender);

		#endregion

		protected MetaGrfHolder _metaGrf;

		public MetaGrfHolder MetaGrf {
			get { return _metaGrf; }
		}

		public event ClientDatabaseEventHandler Reloaded;
		public event ClientDatabaseEventHandler PreviewReloaded;

		public void OnPreviewReloaded() {
			ClientDatabaseEventHandler handler = PreviewReloaded;
			if (handler != null) handler(this);
		}

		public void OnReloaded() {
			ClientDatabaseEventHandler handler = Reloaded;
			if (handler != null) handler(this);
		}
	}
}
