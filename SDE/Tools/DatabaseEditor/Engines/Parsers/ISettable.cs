namespace SDE.Tools.DatabaseEditor.Engines.Parsers {
	/// <summary>
	/// Used by object parsers which have an override property.
	/// TODO : Remove this interface and use a flag in the table instead
	/// </summary>
	public interface ISettable {
		string Override { get; set; }
		void Set(object value);
		int GetInt();
	}
}