using System;
using GRF.Core;

namespace SDE.Tools.SDEMapcache.Commands {
	public class AddMapCommand : IMapcacheCommand {
		private readonly string _name;
		private byte[] _gat;
		private byte[] _rsw;
		private MapInfo _map;
		private MapInfo _conflictMap;

		public AddMapCommand(string name, byte[] gat, byte[] rsw) {
			_name = name.ToLowerInvariant();
			_gat = gat;
			_rsw = rsw;

			// Execute now to reduce the memory usage
			_setupMap();
		}

		public AddMapCommand(string name, MapInfo map) {
			_name = name.ToLowerInvariant();
			_map = map;
			_map.Added = true;
		}

		private void _setupMap() {
			if (_map == null) {
				bool hasWater = _rsw != null;
				int waterHeight = _rsw != null ? (int)BitConverter.ToSingle(_rsw, 166) : -1;

				_map = new MapInfo(_name);
				_map.Xs = BitConverter.ToUInt16(_gat, 6);
				_map.Ys = BitConverter.ToUInt16(_gat, 10);
				_map.Data = new byte[_map.Xs * _map.Ys];

				int offset = 14;

				for (int i = 0; i < _map.Len; i++) {
					float height = BitConverter.ToSingle(_gat, offset);
					int cellType = BitConverter.ToInt32(_gat, offset + 16);
					offset += 20;

					if (hasWater && cellType == 0 && height > waterHeight)
						cellType = 3;

					_map.Data[i] = (byte)cellType;
				}

				_map.Data = Compression.CompressZlib(_map.Data);
				_map.Added = true;
				_gat = null;
				_rsw = null;
			}
		}

		public void Execute(Mapcache mapcache) {
			_setupMap();
			
			int index = mapcache.GetMapIndex(_name);
			
			if (index > -1) {
				_conflictMap = mapcache.Maps[index];
				mapcache.Maps[index] = _map;
			}
			else {
				mapcache.Maps.Add(_map);
			}
		}

		public void Undo(Mapcache mapcache) {
			if (_conflictMap == null)
				mapcache.Maps.Remove(_map);
			else
				mapcache.Maps[mapcache.GetMapIndex(_name)] = _conflictMap;
		}

		public string CommandDescription {
			get { return "Added map '" + _name + "'"; }
		}
	}
}
