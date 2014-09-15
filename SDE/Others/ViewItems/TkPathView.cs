using System;
using System.ComponentModel;
using System.Windows.Media;
using TokeiLibrary;
using Utilities;
using Utilities.Extension;

namespace SDE.Others.ViewItems {
	/// <summary>
	/// The view object of a TkPath (used in lists)
	/// </summary>
	public class TkPathView :  INotifyPropertyChanged {
		private bool _fileNotFound;
		private TkPath _path;

		public TkPathView(TkPath path) {
			_path = path;

			ImageSource image;

			if (path.FilePath.IsExtension(".grf")) {
				image = (ImageSource) ApplicationManager.PreloadResourceImage("grf-16.png");
			}
			else if (path.FilePath.IsExtension(".gpf")) {
				image = (ImageSource)ApplicationManager.PreloadResourceImage("gpf-16.png");
			}
			else if (path.FilePath.IsExtension(".rgz")) {
				image = (ImageSource)ApplicationManager.PreloadResourceImage("rgz-16.png");
			}
			else {
				image = (ImageSource)ApplicationManager.PreloadResourceImage("folderClosed.png");
			}
			
			DataImage = image;
		}

		public ImageSource DataImage { get; set; }

		public TkPath Path {
			get { return _path; }
			set { _path = value; }
		}

		public string DisplayFileName {
			get {
				return String.IsNullOrEmpty(_path.FilePath) ? _path.RelativePath : _path.FilePath;
			}
		}

		public bool FileNotFound {
			get { return _fileNotFound; }
			set {
				_fileNotFound = value;
				Update();
			}
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		/// <summary>
		/// Forces a refresh of all the visible property for this item in the list.
		/// </summary>
		public void Update() {
			OnPropertyChanged("");
		}

		protected virtual void OnPropertyChanged(string propertyName) {
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
