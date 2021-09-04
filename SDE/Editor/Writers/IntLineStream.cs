using Database.Commands;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using System.Linq;

namespace SDE.Editor.Writers
{
    public class IntLineStream : LineStream<int>
    {
        public IntLineStream(string path, int key = 0) : base(path, ',', key)
        {
        }

        public override int Default
        {
            get { return -1; }
        }

        public override void Write(int key, string line)
        {
            int? index = _ids.IndexOf(key);

            if (index > -1)
            {
                _allLines[index.Value] = line;
            }
            else
            {
                index = _ids.FirstOrDefault(p => p > -1 && p > key);

                if (index > -1)
                {
                    index = _ids.IndexOf(index.Value);
                }

                if (_ids.All(p => key > p))
                {
                    index = _ids.Count;
                }

                if (index < 0)
                    index = 0;

                _allLines.Insert(index.Value, line);
                _ids.Insert(index.Value, key);
            }
        }

        public bool Remove2(BaseDb gdb, int parentGroup)
        {
            bool isModified = false;
            AbstractDb<int> db = gdb.To<int>();

            if (db.Table.Commands.GetUndoCommands() == null)
                return false;

            foreach (GroupCommand<int, ReadableTuple<int>> command in db.Table.Commands.GetUndoCommands().OfType<GroupCommand<int, ReadableTuple<int>>>())
            {
                foreach (DeleteTupleDico<int, ReadableTuple<int>> deleteCommand in command.Commands.OfType<DeleteTupleDico<int, ReadableTuple<int>>>())
                {
                    if (deleteCommand.ParentKey == parentGroup)
                    {
                        Delete(deleteCommand.Key);
                        isModified = true;
                    }
                }

                foreach (ChangeTupleKeyDico<int, ReadableTuple<int>> changeTupleKeyCommand in command.Commands.OfType<ChangeTupleKeyDico<int, ReadableTuple<int>>>())
                {
                    // If the key was changed, the old key must be removed
                    if (changeTupleKeyCommand.ParentKey == parentGroup)
                    {
                        Delete(changeTupleKeyCommand.Key);
                        isModified = true;
                    }
                }
            }

            foreach (ChangeTupleKeyDico<int, ReadableTuple<int>> command in db.Table.Commands.GetUndoCommands().OfType<ChangeTupleKeyDico<int, ReadableTuple<int>>>())
            {
                // If the key was changed, the old key must be removed
                if (command.ParentKey == parentGroup)
                {
                    Delete(command.Key);
                    isModified = true;
                }
            }

            return isModified;
        }
    }
}