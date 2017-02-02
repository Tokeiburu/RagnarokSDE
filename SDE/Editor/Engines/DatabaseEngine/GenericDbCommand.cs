using System;
using Database;
using Database.Commands;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;

namespace SDE.Editor.Engines.DatabaseEngine {
	/// <summary>
	/// Holds a table command
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	public class GenericDbCommand<TKey> : IGenericDbCommand {
		private readonly ITableCommand<TKey, ReadableTuple<TKey>> _command;
		private readonly CommandsHolder<TKey, ReadableTuple<TKey>> _commandsList;
		private readonly string _displayName;

		public GenericDbCommand(AbstractDb<TKey> db) {
			_displayName = db.DbSource.DisplayName;
			Table = db.Table;

			_command = Table.Commands.Current;
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
}