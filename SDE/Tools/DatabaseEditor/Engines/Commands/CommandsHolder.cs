using System;
using Database;
using Database.Commands;
using SDE.Tools.DatabaseEditor.Generic;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using Utilities;

namespace SDE.Tools.DatabaseEditor.Engines.Commands {
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

		#region Nested type: GenericDbCommand

		public class GenericDbCommand<TKey> : IGenericDbCommand {
			private readonly ITableCommand<TKey, ReadableTuple<TKey>> _command;
			private readonly CommandsHolder<TKey, ReadableTuple<TKey>> _commandsList;
			private readonly string _displayName;

			public GenericDbCommand(AbstractDb<TKey> db) {
				_displayName = db.DbSource.DisplayName;
				Table = db.Table;

				_command = Table.Commands.Last();
				_commandsList = Table.Commands;
			}

			public Table<TKey, ReadableTuple<TKey>> Table { get; private set; }

			#region IGenericDbCommand Members

			public void Execute() {
				_commandsList.Redo();
			}

			public void Undo() {
				_commandsList.Undo();
			}

			public string CommandDescription {
				get { return String.Format("[{0}], {1}", _displayName, _command.CommandDescription); }
			}

			#endregion
		}

		#endregion
	}
}