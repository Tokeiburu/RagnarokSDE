using GRF.IO;
using System;
using System.ComponentModel;
using System.IO;
using Utilities;
using Utilities.Services;

namespace SDE.Tools.SDEMapcache
{
    public class MapInfo : INotifyPropertyChanged
    {
        public string MapName { get; set; }
        public int Xs { get; set; }
        public int Ys { get; set; }
        public byte[] Data { get; set; }

        public int Len
        {
            get { return Data.Length; }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion INotifyPropertyChanged Members

        public bool Normal
        {
            get { return true; }
        }

        public bool Added { get; set; }

        public string DisplaySize
        {
            get { return Methods.FileSizeToString(Len); }
        }

        public MapInfo(string name)
        {
            if (name.Length > 11)
                name = name.Substring(0, 11);
            MapName = name;
        }

        public MapInfo(ByteReader reader)
        {
            MapName = reader.String(12, '\0');
            MapName = MapName.ToLowerInvariant();
            Xs = reader.UInt16();
            Ys = reader.UInt16();
            int len = reader.Int32();
            Data = reader.Bytes(len);
        }

        public void Write(BinaryWriter writer)
        {
            byte[] mapName = EncodingService.Ansi.GetBytes(MapName);
            writer.Write(mapName, 0, mapName.Length);
            if (mapName.Length < 12)
            {
                writer.Write(new byte[12 - mapName.Length]);
            }
            writer.Write((UInt16)Xs);
            writer.Write((UInt16)Ys);
            writer.Write(Data.Length);
            writer.Write(Data);
        }

        public virtual void OnPropertyChanged()
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(""));
        }
    }
}