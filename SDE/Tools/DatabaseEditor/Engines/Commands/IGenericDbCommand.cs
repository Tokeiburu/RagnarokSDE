namespace SDE.Tools.DatabaseEditor.Engines.Commands {
	public interface IGenericDbCommand {
		string CommandDescription { get; }
		void Execute();
		void Undo();
	}
}