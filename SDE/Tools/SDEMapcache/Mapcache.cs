using GRF;
using GRF.IO;
using GRF.System;
using GRF.Threading;
using SDE.Tools.SDEMapcache.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TokeiLibrary.WPF;

namespace SDE.Tools.SDEMapcache
{
    public class Mapcache : IProgress
    {
        static Mapcache()
        {
            TemporaryFilesManager.UniquePattern(Process.GetCurrentProcess().Id + "_mapcache_{0:0000}.dat");
        }

        public CommandsHolder Commands { get; private set; }

        public string LoadedPath { get; set; }
        private List<MapInfo> _maps = new List<MapInfo>();

        public UInt32 FileSize
        {
            get { return (uint)(8 + Maps.Sum(p => p.Len) + Maps.Count * 20); }
        }

        public UInt16 MapCount
        {
            get { return (ushort)Maps.Count; }
        }

        public List<MapInfo> Maps
        {
            get { return _maps; }
            set { _maps = value; }
        }

        public RangeObservableCollection<MapInfo> ViewMaps
        {
            get { return new RangeObservableCollection<MapInfo>(Maps); }
        }

        public Mapcache(MultiType data) : this(new ByteReader(data.Data))
        {
            LoadedPath = data.Path;
        }

        public Mapcache()
        {
            Commands = new CommandsHolder(this);
        }

        private Mapcache(ByteReader reader)
        {
            Commands = new CommandsHolder(this);

            _load(reader);
        }

        private void _load(ByteReader reader)
        {
            reader.UInt32(); // file size
            reader.UInt16(); // map count
            reader.UInt16(); // the header structure is 8 bytes

            while (reader.CanRead)
            {
                Maps.Add(new MapInfo(reader));
            }
        }

        public int GetMapIndex(string name)
        {
            var map = Maps.FirstOrDefault(p => p.MapName == name);

            if (map == null)
                return -1;

            return Maps.IndexOf(map);
        }

        public void Save()
        {
            Save(LoadedPath);
        }

        public void Save(string filename)
        {
            if (!File.Exists(filename))
            {
                GrfPath.CreateDirectoryFromFile(filename);

                using (var writer = File.Create(filename))
                {
                    Save(writer);
                }

                return;
            }

            string tempFile = TemporaryFilesManager.GetTemporaryFilePath(Process.GetCurrentProcess().Id + "_mapcache_{0:0000}.dat");

            using (var writer = File.Create(tempFile))
            {
                Save(writer);
            }

            GrfPath.CreateDirectoryFromFile(filename);
            GrfPath.Delete(filename);
            File.Copy(tempFile, filename);
            GrfPath.Delete(tempFile);
        }

        public void Save(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(FileSize);
            writer.Write(MapCount);
            writer.Write((UInt16)0);

            foreach (var map in Maps)
            {
                map.Write(writer);
            }
        }

        public void Load(string file)
        {
            Maps.Clear();
            _load(new ByteReader(File.ReadAllBytes(file)));
            LoadedPath = file;
            Commands.ClearCommands();
        }

        public void Reset()
        {
            Maps.Clear();
            LoadedPath = null;
            Commands.ClearCommands();
        }

        public float Progress { get; set; }
        public bool IsCancelling { get; set; }
        public bool IsCancelled { get; set; }
    }
}