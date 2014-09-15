using SDE.Others;

namespace SDE.Tools.DatabaseEditor.Engines {
	public class BaseGenericDatabase {
		#region Delegates

		public delegate void ClientDatabaseEventHandler(object sender);

		#endregion

		protected MetaGrfHolder _metaGrf;

		public MetaGrfHolder MetaGrf {
			get { return _metaGrf; }
		}

		public event ClientDatabaseEventHandler Reloaded;

		public void OnReloaded() {
			ClientDatabaseEventHandler handler = Reloaded;
			if (handler != null) handler(this);
		}
	}
}
