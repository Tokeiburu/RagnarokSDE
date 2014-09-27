namespace SDE.Tools.DatabaseEditor.Engines.DatabaseEngine {
	public interface IGenericDbCommand {
		string CommandDescription { get; }
		void Execute();
		void Undo();
	}
}