namespace SDE.Tools.SDEMapcache.Commands
{
    public class DeleteMapCommand : IMapcacheCommand
    {
        private readonly string _name;
        private MapInfo _conflict;
        private int _index;

        public DeleteMapCommand(string name)
        {
            _name = name;
        }

        public void Execute(Mapcache mapcache)
        {
            _index = mapcache.GetMapIndex(_name);
            _conflict = mapcache.Maps[_index];
            mapcache.Maps.RemoveAt(_index);
        }

        public void Undo(Mapcache mapcache)
        {
            mapcache.Maps.Insert(_index, _conflict);
        }

        public string CommandDescription
        {
            get { return "Deleted map '" + _name + "'"; }
        }
    }
}