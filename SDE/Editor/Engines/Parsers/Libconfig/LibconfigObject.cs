using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDE.Editor.Engines.Parsers.Libconfig {
	public class LibconfigObject : IEnumerable<LibconfigObject> {
		public LibconfigTypes ConfType { get; private set; }
		public LibconfigObject Parent { get; set; }
		public List<string> Lines;
		public int Line { get; private set; }
		public int Length { get; set; }

		public bool Added { get; set; }
		public bool Modified { get; set; }

		public string ObjectValue {
			get {
				var confString = this as LibconfigString;

				if (confString != null)
					return confString.Value;

				var confAggregate = this as LibconfigAggregate;

				if (confAggregate != null) {
					StringBuilder builder = new StringBuilder();
					builder.Append("[");

					for (int i = 0; i < confAggregate.Objects.Count; i++) {
						builder.Append(confAggregate.Objects[i]);

						if (i != confAggregate.Objects.Count - 1)
							builder.Append(", ");
					}

					builder.Append("]");
					return builder.ToString();
				}

				var confKeyValue = this as LibconfigKeyValue;

				if (confKeyValue != null)
					return confKeyValue.Value;

				return null;
			}
		}

		protected LibconfigObject(LibconfigTypes confType, int line) {
			ConfType = confType;
			Line = line;
			//Length = 1;
		}

		public LibconfigObject this[string key] {
			get {
				if (key.Contains(".")) {
					string[] keys = key.Split(new char[] { '.' }, 2);

					var obj = this[keys[0]];

					if (obj == null)
						return null;

					return obj[keys[1]];
				}

				var keyValue = this as LibconfigKeyValue;

				if (keyValue != null && keyValue.Key == key)
					return keyValue.Value;

				var arrayBase = this as LibconfigArrayBase;

				if (arrayBase != null && (keyValue = arrayBase.Objects.OfType<LibconfigKeyValue>().FirstOrDefault(p => p.Key == key)) != null) {
					return keyValue.Value;
				}

				return null;
			}
		}

		public T To<T>() where T : class {
			return this as T;
		}

		public IEnumerator<LibconfigObject> GetEnumerator() {
			var arrayBase = this as LibconfigArrayBase;

			if (arrayBase != null)
				return arrayBase.Objects.GetEnumerator();

			return new List<LibconfigObject>().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public static implicit operator string(LibconfigObject item) {
			return item.ObjectValue;
		}
	}

	public class LibconfigString : LibconfigObject {
		public string Value { get; set; }

		public LibconfigString(string value, int line)
			: base(LibconfigTypes.String, line) {
			Value = value;
		}

		public override string ToString() {
			return "String: " + Value;
		}
	}

	public class LibconfigArrayBase : LibconfigObject {
		public List<LibconfigObject> Objects = new List<LibconfigObject>();

		protected LibconfigArrayBase(LibconfigTypes confType, int line) : base(confType, line) {
		}

		public void AddElement(LibconfigObject obj) {
			Objects.Add(obj);
		}
	}

	public class LibconfigArray : LibconfigArrayBase {
		public LibconfigArray(int line)
			: base(LibconfigTypes.Array, line) {
		}

		public override string ToString() {
			return "Array: " + Objects.Count + " elements.";
		}
	}

	public class LibconfigList : LibconfigArrayBase {
		public LibconfigList(int line)
			: base(LibconfigTypes.List, line) {
		}

		public override string ToString() {
			return "List: " + Objects.Count + " elements.";
		}
	}

	public class LibconfigAggregate : LibconfigArrayBase {
		public LibconfigAggregate(int line)
			: base(LibconfigTypes.Aggregate, line) {
		}

		public override string ToString() {
			return "Aggregate: " + Objects.Count + " elements.";
		}
	}

	public class LibconfigKeyValue : LibconfigObject {
		public string Key { get; private set; }
		public LibconfigObject Value { get; set; }

		public LibconfigKeyValue(string key, int line)
			: base(LibconfigTypes.KeyValue, line) {
			Key = key;
		}

		public override string ToString() {
			return "Key: " + Key + ", Value: { " + Value + " }";
		}
	}

	public enum LibconfigTypes {
		List,
		KeyValue,
		String,
		Array,
		Number,
		Aggregate,
		Null
	}
}