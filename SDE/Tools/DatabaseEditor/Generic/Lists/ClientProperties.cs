using Database;

namespace SDE.Tools.DatabaseEditor.Generic.Lists {
	public sealed class ClientResourceProperties : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ClientResourceProperties(new PrimaryAttribute("Id", typeof(int), 0, "Item ID"));
		public static readonly DbAttribute ResourceName = new ClientResourceProperties(new DbAttribute("ResourceName", typeof(string), "", "Item name"));

		private ClientResourceProperties(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}
}
