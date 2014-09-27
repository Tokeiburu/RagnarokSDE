using System.ComponentModel;
using Database;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Generic {
	/// <summary>
	/// Tuple view item (to be displayed in a list view)
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
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
			bool sameValue;

			try {
				sameValue = GetValue(attribute.Index).ToString() == value.ToString();
			}
			catch {
				sameValue = false;
			}

			try {
				base.SetValue(attribute, value);
			}
			catch {
				DbLoaderErrorHandler.Handle(("Failed to set or parse the value for [" + GetKey<TKey>() + "] at '" + attribute.DisplayName + "'. Value entered is : " + (value ?? "")).RemoveBreakLines(), ErrorLevel.NotSpecified);
				base.SetValue(attribute, attribute.Default);
			}

			if (!sameValue) {
				Modified = true;
			}
		}

		protected virtual void OnPropertyChanged(string propertyName) {
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Copy(ReadableTuple<TKey> tuple) {
			_elements = new object[tuple._elements.Length];

			for (int i = 0; i < tuple._elements.Length; i++) {
				_elements[i] = tuple._elements[i];
			}
		}
	}
}
