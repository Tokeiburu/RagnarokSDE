namespace SDE.Editor.Engines.TabNavigationEngine {
	public interface INagivationCommand {
		string CommandDescription { get; }
		void Execute(TabNavigation navEngine);
		void Undo(TabNavigation navEngine);
	}
}