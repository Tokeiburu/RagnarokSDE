using System.Collections.Generic;
using Utilities.Commands;

namespace SDE.Tools.SDEMapcache.Commands
{
    public class MapcacheGroupCommand : IGroupCommand<IMapcacheCommand>, IMapcacheCommand
    {
        private readonly bool _executeCommandsOnStore;
        private readonly Mapcache _mapcache;
        private readonly List<IMapcacheCommand> _commands = new List<IMapcacheCommand>();
        private bool _firstTimeExecuted;

        public List<IMapcacheCommand> Commands
        {
            get { return _commands; }
        }

        public MapcacheGroupCommand(Mapcache mapcache, bool executeCommandsOnStore = false)
        {
            _mapcache = mapcache;
            _executeCommandsOnStore = executeCommandsOnStore;
        }

        public void Add(IMapcacheCommand command)
        {
            _commands.Add(command);
        }

        public void Processing(IMapcacheCommand command)
        {
            if (_executeCommandsOnStore)
                command.Execute(_mapcache);
        }

        public void AddRange(List<IMapcacheCommand> commands)
        {
            _commands.AddRange(commands);
        }

        public void Close()
        {
        }

        public void Execute(Mapcache mapcache)
        {
            if (_executeCommandsOnStore)
            {
                if (_firstTimeExecuted)
                {
                    _firstTimeExecuted = false;
                    return;
                }
            }

            for (int index = 0; index < _commands.Count; index++)
            {
                var command = _commands[index];
                try
                {
                    command.Execute(mapcache);
                }
                catch (AbstractCommandException)
                {
                    _commands.RemoveAt(index);
                    index--;
                }
            }
        }

        public void Undo(Mapcache mapcache)
        {
            for (int index = _commands.Count - 1; index >= 0; index--)
            {
                _commands[index].Undo(mapcache);
            }
        }

        public string CommandDescription
        {
            get
            {
                if (_commands.Count == 1)
                {
                    return _commands[0].CommandDescription;
                }

                const int DisplayLimit = 2;

                string result = string.Format("Group command ({0}) :\r\n", _commands.Count);

                for (int i = 0; i < DisplayLimit && i < _commands.Count; i++)
                {
                    result += "    " + _commands[i].CommandDescription.Replace("\r\n", "\\r\\n").Replace("\n", "\\n") + "\r\n";
                }

                result = result.Trim(new char[] { '\r', '\n' });

                if (_commands.Count > DisplayLimit)
                {
                    result += "...";
                }

                return result;
            }
        }
    }
}