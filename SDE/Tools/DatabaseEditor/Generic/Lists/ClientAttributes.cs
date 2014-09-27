using Database;

namespace SDE.Tools.DatabaseEditor.Generic.Lists {
	public sealed class ClientResourceAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ClientResourceAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Item ID"));
		public static readonly DbAttribute ResourceName = new ClientResourceAttributes(new DbAttribute("ResourceName", typeof(string), "", "Item name"));

		private ClientResourceAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}
}
