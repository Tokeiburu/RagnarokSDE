using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities.Commands;
using Utilities.Extension;

namespace SDE.Tools.SDEMapcache.Commands
{
    public class CommandsHolder : AbstractCommand<IMapcacheCommand>
    {
        private readonly Mapcache _mapcache;

        public CommandsHolder(Mapcache mapcache)
        {
            _mapcache = mapcache;
        }

        protected override void _execute(IMapcacheCommand command)
        {
            command.Execute(_mapcache);
        }

        protected override void _undo(IMapcacheCommand command)
        {
            command.Undo(_mapcache);
        }

        protected override void _redo(IMapcacheCommand command)
        {
            command.Execute(_mapcache);
        }

        /// <summary>
        /// Begins the commands stack grouping.
        /// </summary>
        public void Begin()
        {
            _mapcache.Commands.BeginEdit(new MapcacheGroupCommand(_mapcache, false));
        }

        /// <summary>
        /// Begins the commands stack grouping and apply commands as soon as they're received.
        /// </summary>
        public void BeginNoDelay()
        {
            _mapcache.Commands.BeginEdit(new MapcacheGroupCommand(_mapcache, true));
        }

        /// <summary>
        /// Ends the commands stack grouping.
        /// </summary>
        public void End()
        {
            _mapcache.Commands.EndEdit();
        }

        public void AddMap(string gatfile)
        {
            if (!gatfile.IsExtension(".gat"))
                throw new Exception("Unknown extension, expected a .gat file.");

            string rswfile = gatfile.ReplaceExtension(".rsw");

            if (!File.Exists(gatfile))
                throw new FileNotFoundException(String.Format("File not found: {0}.", gatfile), gatfile);

            if (!File.Exists(rswfile))
                throw new FileNotFoundException(String.Format("File not found: {0}.", rswfile), rswfile);

            _mapcache.Commands.StoreAndExecute(new AddMapCommand(Path.GetFileNameWithoutExtension(gatfile), File.ReadAllBytes(gatfile), File.ReadAllBytes(rswfile)));
        }

        public void AddMapRaw(string name, MapInfo map)
        {
            _mapcache.Commands.StoreAndExecute(new AddMapCommand(name, map));
        }

        public void AddMap(string name, byte[] gatdata, byte[] rswdata)
        {
            _mapcache.Commands.StoreAndExecute(new AddMapCommand(name, gatdata, rswdata));
        }

        public void DeleteMap(string name)
        {
            _mapcache.Commands.StoreAndExecute(new DeleteMapCommand(name));
        }

        public void DeleteMaps(List<string> names)
        {
            if (names.Count == 0)
                return;

            if (names.Count == 1)
            {
                DeleteMap(names[0]);
                return;
            }

            Begin();

            foreach (var map in names)
            {
                DeleteMap(map);
            }

            End();
        }

        public void AddMaps(List<string> names)
        {
            HashSet<string> files = new HashSet<string>();

            foreach (var file in names.Where(p => p.IsExtension(".gat", ".rsw", ".gnd")))
            {
                files.Add(file.ReplaceExtension(".gat"));
            }

            names = files.ToList();

            if (names.Count == 0)
                return;

            if (names.Count == 1)
            {
                AddMap(names[0]);
                return;
            }

            Begin();

            foreach (var map in names)
            {
                AddMap(map);
            }

            End();
        }
    }
}