using Database;
using Database.Commands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Utilities.Commands;

namespace SDE.Editor.Generic
{
    /// <summary>
    /// A database table which holds multiple tables at the same time.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    public class MetaTable<TKey> : Table<TKey, ReadableTuple<TKey>>, IEnumerable<ReadableTuple<TKey>>
    {
        private readonly MetaCommandsHolder<TKey> _commands;
        private readonly List<Table<TKey, ReadableTuple<TKey>>> _tables = new List<Table<TKey, ReadableTuple<TKey>>>();
        private List<ReadableTuple<TKey>> _bufferedItems = new List<ReadableTuple<TKey>>();
        private bool _bufferedTable;

        public int TablesCount
        {
            get { return _tables.Count; }
        }

        public MetaTable(AttributeList list, bool unsafeContext = false) : base(list, unsafeContext)
        {
            _commands = new MetaCommandsHolder<TKey>(null);
        }

        public new int Count
        {
            get { return FastItems.Count; }
        }

        public override CommandsHolder<TKey, ReadableTuple<TKey>> Commands
        {
            get { return _commands; }
        }

        public override List<ReadableTuple<TKey>> FastItems
        {
            get
            {
                if (_bufferedTable)
                {
                    return _bufferedItems;
                }

                Dictionary<TKey, ReadableTuple<TKey>> values = new Dictionary<TKey, ReadableTuple<TKey>>(_tables.Last().Tuples);

                for (int i = _tables.Count - 2; i >= 0; i--)
                {
                    foreach (var pair in _tables[i].Tuples)
                    {
                        values[pair.Key] = pair.Value;
                    }
                }

                return values.Values.ToList();
            }
        }

        #region IEnumerable<ReadableTuple<TKey>> Members

        IEnumerator<ReadableTuple<TKey>> IEnumerable<ReadableTuple<TKey>>.GetEnumerator()
        {
            return FastItems.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return FastItems.GetEnumerator();
        }

        #endregion IEnumerable<ReadableTuple<TKey>> Members

        public void AddTable(Table<TKey, ReadableTuple<TKey>> table)
        {
            _tables.Insert(0, table);
            _commands.AddTable(table);
        }

        public override bool ContainsKey(TKey key)
        {
            return _tables.Any(table => table.ContainsKey(key));
        }

        public override T Get<T>(TKey key, DbAttribute attribute)
        {
            return _tables.First(p => p.ContainsKey(key)).Get<T>(key, attribute);
        }

        public override object Get(TKey key, DbAttribute attribute)
        {
            return _tables.First(p => p.ContainsKey(key)).Get(key, attribute);
        }

        public override ReadableTuple<TKey> TryGetTuple(TKey key)
        {
            ReadableTuple<TKey> tuple;

            for (int i = 0; i < _tables.Count; i++)
            {
                tuple = _tables[i].TryGetTuple(key);

                if (tuple != null)
                    return tuple;
            }

            return null;
        }

        public override object GetRaw(TKey key, DbAttribute attribute)
        {
            return _tables.First(p => p.ContainsKey(key)).GetRaw(key, attribute);
        }

        public override ReadableTuple<TKey> GetTuple(TKey key)
        {
            return _tables.First(p => p.ContainsKey(key)).GetTuple(key);
        }

        public void MergeOnce()
        {
            _bufferedTable = true;

            Dictionary<TKey, ReadableTuple<TKey>> values = new Dictionary<TKey, ReadableTuple<TKey>>(_tables.Last().Tuples);

            for (int i = _tables.Count - 2; i >= 0; i--)
            {
                foreach (var pair in _tables[i].Tuples)
                {
                    values[pair.Key] = pair.Value;
                }
            }

            _bufferedItems = values.Values.ToList();
        }
    }

    public class MetaCommandsHolder<TKey> : CommandsHolder<TKey, ReadableTuple<TKey>>
    {
        private readonly List<Table<TKey, ReadableTuple<TKey>>> _tables = new List<Table<TKey, ReadableTuple<TKey>>>();

        public MetaCommandsHolder(Table<TKey, ReadableTuple<TKey>> table) : base(table)
        {
        }

        public void AddTable(Table<TKey, ReadableTuple<TKey>> table)
        {
            _tables.Insert(0, table);
        }

        public override void Store(ITableCommand<TKey, ReadableTuple<TKey>> command)
        {
            throw new NotImplementedException();
        }

        public override void BeginEdit(IGroupCommand<ITableCommand<TKey, ReadableTuple<TKey>>> command)
        {
            _tables.ForEach(p => p.Commands.Begin());
        }

        public override void EndEdit()
        {
            _tables.ForEach(p => p.Commands.EndEdit());
        }

        public override void StoreAndExecute(ITableCommand<TKey, ReadableTuple<TKey>> command)
        {
            var cmd1 = command as ChangeTupleProperty<TKey, ReadableTuple<TKey>>;

            if (cmd1 != null)
            {
                for (int i = 0; i < _tables.Count; i++)
                {
                    if (_tables[i].ContainsKey(command.Key))
                    {
                        _tables[i].Commands.StoreAndExecute(command);
                        return;
                    }
                }

                return;
            }

            var cmd2 = command as DeleteTuple<TKey, ReadableTuple<TKey>>;

            if (cmd2 != null)
            {
                for (int i = 0; i < _tables.Count; i++)
                {
                    if (_tables[i].ContainsKey(command.Key))
                    {
                        _tables[i].Commands.StoreAndExecute(command);
                        //return; Removes in all tables
                    }
                }

                return;
            }

            throw new NotImplementedException();
        }
    }
}