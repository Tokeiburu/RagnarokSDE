namespace SDE.Tools.DatabaseEditor.Engines.TabNavigationEngine {
	public interface INagivationCommand {
		string CommandDescription { get; }
		void Execute(TabNavigation navEngine);
		void Undo(TabNavigation navEngine);
	}
}