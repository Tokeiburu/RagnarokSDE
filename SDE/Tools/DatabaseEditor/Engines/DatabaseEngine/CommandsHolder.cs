using Utilities;

namespace SDE.Tools.DatabaseEditor.Engines.DatabaseEngine {
	/// <summary>
	/// Holds commands for the database engine
	/// </summary>
	public class CommandsHolder : AbstractCommand<IGenericDbCommand> {
		protected override void _execute(IGenericDbCommand command) {
			command.Execute();
		}

		protected override void _undo(IGenericDbCommand command) {
			command.Undo();
		}

		protected override void _redo(IGenericDbCommand command) {
			command.Execute();
		}
	}
}