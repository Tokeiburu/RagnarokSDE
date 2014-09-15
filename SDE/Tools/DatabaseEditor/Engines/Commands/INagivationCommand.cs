namespace SDE.Tools.DatabaseEditor.Engines.Commands {
	public interface INagivationCommand {
		string CommandDescription { get; }
		void Execute(TabNavigationEngine navEngine);
		void Undo(TabNavigationEngine navEngine);
	}
}