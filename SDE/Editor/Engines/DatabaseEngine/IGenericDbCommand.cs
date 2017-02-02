namespace SDE.Editor.Engines.DatabaseEngine {
	public interface IGenericDbCommand {
		string CommandDescription { get; }
		void Execute();
		void Undo();
	}
}