using System.Collections.Generic;
using System.ComponentModel;
using Database;

namespace SDE.Tools.DatabaseEditor.Generic {
	public class ReadableTuple<TKey> : Tuple, INotifyPropertyChanged {
		public ReadableTuple(TKey key, AttributeList list) : base(key, list) { }

		public override bool Default {
			get { return false; }
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		public override void OnTupleModified(bool value) {
			base.OnTupleModified(value);
			OnPropertyChanged("");
		}

		public int GetIntValue(int index) {
			return (int)GetValue(index);
		}

		public string GetStringValue(int index) {
			return (string)GetValue(index);
		}

		public override void SetValue(DbAttribute attribute, object value) {
			bool sameValue = GetValue(attribute.Index).ToString() == value.ToString();

			base.SetValue(attribute, value);

			if (!sameValue) {
				Modified = true;
			}
		}

		protected virtual void OnPropertyChanged(string propertyName) {
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Copy(ReadableTuple<TKey> tuple) {
			_elements = new List<object>(tuple._elements);
		}
	}
}
