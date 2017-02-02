using System.ComponentModel;
using SDE.View.Dialogs;

namespace SDE.View.ObjectView {
	public class AccessoryItemView : IEditableObject, INotifyPropertyChanged {
		private bool _inTxn;
		private AccEditDialog.AccessoryItem _custData;
		private AccEditDialog.AccessoryItem _backupData;

		public AccessoryItemView(AccEditDialog.AccessoryItem item) {
			_custData = item;
		}

		public int Id {
			get { return this._custData.Id; }
			set { this._custData.Id = value;
				OnPropertyChanged();
			}
		}

		public string AccId {
			get { return this._custData.AccId; }
			set { this._custData.AccId = value; OnPropertyChanged(); }
		}

		public string Texture {
			get { return this._custData.Texture; }
			set { this._custData.Texture = value; OnPropertyChanged(); }
		}

		public void BeginEdit() {
			if (!_inTxn) {
				this._backupData = _custData;
				_inTxn = true;
			}
		}

		public void EndEdit() {
			if (_inTxn) {
				_backupData = new AccEditDialog.AccessoryItem();
				_inTxn = false;
			}
		}

		public void CancelEdit() {
			if (_inTxn) {
				this._custData = _backupData;
				_inTxn = false;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged() {
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(""));
		}
	}
}