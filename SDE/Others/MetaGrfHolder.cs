using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ErrorManager;
using GRF.ContainerFormat.Commands;
using GRF.Core;
using SDE.Tools.DatabaseEditor.CDECore;
using Utilities;

namespace SDE.Others {
	public class VirtualFileTable : FileTable {
		private readonly MetaGrfHolder _grf;

		public VirtualFileTable(GrfHeader header, MetaGrfHolder grf) : base(header) {
			_grf = grf;
		}

		public override List<string> Files {
			get { return _grf.Paths; }
		}

		public override FileEntry this[string fileName] {
			get {
				fileName = fileName.ToLower();

				string tkPath = _grf[fileName];

				if (tkPath != null) {
					TkPath path = new TkPath(tkPath);

					if (String.IsNullOrEmpty(path.RelativePath)) {
						FileEntry tempEntry = new FileEntry();
						tempEntry.SetModificationFlags(Modification.Added);
						tempEntry.SourceFilePath = path.FilePath;
						tempEntry.Header = _header;

						return tempEntry;
					}

					return _grf.Grfs.First(p => p.FileName == path.FilePath).FileTable[path.RelativePath];
				}

				return null;
			}
		}

		public override bool Contains(string filename) {
			return _grf[filename] != null;
		}
	}

	public class MetaGrfHolder : GrfHolder, IDisposable {
		private readonly Dictionary<string, byte[]> _bufferedData = new Dictionary<string, byte[]>();
		private bool _disposed;
		private GrfHolder _extraGrf;
		private VirtualFileTable _fileTable;
		private Dictionary<string, GrfHolder> _openedGrfs = new Dictionary<string, GrfHolder>();
		private Dictionary<string, string> _resourcePaths = new Dictionary<string, string>();

		public MetaGrfHolder() {
			_grfClosed = false;
		}

		public override FileTable FileTable {
			get { return _fileTable ?? (_fileTable = new VirtualFileTable(Header, this)); }
		}

		public List<string> Paths {
			get { return _resourcePaths.Values.ToList(); }
		}
		public List<string> RelativePaths {
			get { return _resourcePaths.Keys.ToList(); }
		}
		public List<GrfHolder> Grfs {
			get { return _openedGrfs.Values.ToList(); }
		}

		public string this[string relativePath] {
			get {
				if (_resourcePaths.ContainsKey(relativePath.ToLower()))
					return _resourcePaths[relativePath.ToLower()];

				if (File.Exists(relativePath))
					return new FileInfo(relativePath).FullName;

				return null;
			}
		}

		#region IDisposable Members

		public override void Dispose() {
			Dispose(true);
		}

		#endregion

		public override byte[] GetDecompressedData(FileEntry node) {
			return GetData(node.RelativePath);
		}

		public void Update(List<TkPath> paths, GrfHolder extraGrf = null) {
			try {
				_resourcePaths.Clear();
				_extraGrf = extraGrf;

				string toLower;
				_openGrfs(paths, extraGrf);

				foreach (TkPath resource in paths) {
					if (File.Exists(resource.FilePath)) {
						GrfHolder grf = _openedGrfs[resource.FilePath];

						foreach (string file in grf.FileTable.Files) {
							toLower = file.ToLower();

							if (!_resourcePaths.ContainsKey(toLower)) {
								_resourcePaths.Add(toLower, grf.FileName + "?" + file);
							}
						}
					}
					else if (resource.FilePath.StartsWith("Currently opened GRF : ")) {
						foreach (string file in extraGrf.FileTable.Files) {
							toLower = file.ToLower();

							if (!_resourcePaths.ContainsKey(toLower)) {
								_resourcePaths.Add(toLower, extraGrf.FileName + "?" + file);
							}
						}
					}
					else {
						string relativePath;
						string parentFolder = Path.GetDirectoryName(resource.FilePath);

						if (parentFolder != null && Directory.Exists(parentFolder)) {
							foreach (string file in Directory.GetFiles(parentFolder, "*.*", SearchOption.AllDirectories)) {
								relativePath = file.Replace(parentFolder + "\\", "");

								toLower = relativePath.ToLower();

								if (!_resourcePaths.ContainsKey(toLower)) {
									_resourcePaths.Add(toLower, file + "?");
								}
							}
						}
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
		public byte[] GetData(string relativePath) {
			relativePath = relativePath.ToLower();

			if (_resourcePaths.ContainsKey(relativePath)) {
				return _getData(new TkPath(_resourcePaths[relativePath]));
			}

			if (File.Exists(relativePath)) {
				return File.ReadAllBytes(relativePath);
			}

			return null;
		}

		public byte[] GetDataBuffered(string relativePath) {
			relativePath = relativePath.ToLower();

			if (_bufferedData.ContainsKey(relativePath))
				return _bufferedData[relativePath];

			if (_bufferedData.Count > 15)
				_bufferedData.Clear();

			byte[] data;

			if (_resourcePaths.ContainsKey(relativePath)) {
				data = _getData(new TkPath(_resourcePaths[relativePath]));
				_bufferedData[relativePath] = data;
				return data;
			}

			if (File.Exists(relativePath)) {
				data = File.ReadAllBytes(relativePath);
				_bufferedData[relativePath] = data;
				return data;
			}

			_bufferedData[relativePath] = null;
			return null;
		}
		public void SetData(string relativePath, string dataPath) {
			relativePath = relativePath.ToLower();

			if (File.Exists(relativePath)) {
				File.WriteAllBytes(relativePath, File.ReadAllBytes(dataPath));
			}
			else if (_resourcePaths.ContainsKey(relativePath)) {
				TkPath path = new TkPath(_resourcePaths[relativePath]);

				try {
					if (String.IsNullOrEmpty(path.RelativePath)) {
						File.WriteAllBytes(path.FilePath, File.ReadAllBytes(dataPath));
					}
					else {
						_openedGrfs[path.FilePath].StoreAndExecute(new AddFiles<FileEntry>(Path.GetDirectoryName(relativePath), dataPath));
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}
		public object GetEntry(string relativePath) {
			relativePath = relativePath.ToLower();

			if (_resourcePaths.ContainsKey(relativePath)) {
				TkPath path = new TkPath(_resourcePaths[relativePath]);

				if (String.IsNullOrEmpty(path.RelativePath)) {
					return new GrfShellFile(path.FilePath);
				}

				return _openedGrfs[path.FilePath].FileTable[path.RelativePath];
			}
			return null;
		}

		private byte[] _getData(TkPath path) {
			try {
				if (File.Exists(path.FilePath) && String.IsNullOrEmpty(path.RelativePath)) {
					return File.ReadAllBytes(path.FilePath);
				}

				return _openedGrfs[path.FilePath].GetDecompressedData(_openedGrfs[path.FilePath].FileTable[path.RelativePath]);
			}
			catch (Exception) {
				//ErrorHandler.HandleException(err);
				return null;
			}
		}
		private void _openGrfs(IEnumerable<TkPath> paths, GrfHolder extraGrf) {
			try {
				foreach (TkPath resource in paths) {
					if ((!String.IsNullOrEmpty(resource.FilePath)) && File.Exists(resource.FilePath)) {
						if (!_openedGrfs.ContainsKey(resource.FilePath)) {
							GrfHolder grf = new GrfHolder();
							grf.Open(resource.FilePath);
							_openedGrfs.Add(resource.FilePath, grf);
						}
					}
					else if (resource.FilePath.StartsWith("Currently opened GRF : ")) {
						if (!_openedGrfs.ContainsKey(extraGrf.FileName)) {
							//extraGrf.FileTable.HasBeenChanged += new FileTable.FileTableEventHandler(_fileTable_HasBeenChanged);
							_openedGrfs.Add(extraGrf.FileName, extraGrf);
						}
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public GrfHolder GetGrf(string file) {
			if (_openedGrfs.ContainsKey(file))
				return _openedGrfs[file];

			return null;
		}

		protected override void Dispose(bool disposing) {
			if (!_disposed) {
				if (disposing) {
					if (_resourcePaths != null) {
						_resourcePaths.Clear();
					}

					if (_openedGrfs != null) {
						foreach (GrfHolder grf in _openedGrfs.Values) {
							if (grf == _extraGrf)
								continue;

							grf.Close();
						}

						_openedGrfs.Clear();
						_openedGrfs = null;
					}

					if (_resourcePaths != null) {
						_resourcePaths.Clear();
						_resourcePaths = null;
					}

					if (_fileTable != null) {
						_fileTable.Delete();
						_fileTable = null;
					}
				}

				_disposed = true;
			}
		}
	}
}
