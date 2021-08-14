using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.View.Dialogs;
using Utilities.Extension;

namespace SDE.View.ObjectView {
	public class EvolutionTarget {
		private List<Utilities.Extension.Tuple<object, int>> _itemRequirements = new List<Utilities.Extension.Tuple<object, int>>();

		public List<Utilities.Extension.Tuple<object, int>> ItemRequirements {
			get { return _itemRequirements; }
			set { _itemRequirements = value; }
		}

		public string Target { get; set; }

		public override string ToString() {
			StringBuilder b = new StringBuilder();

			b.Append("#");
			b.Append(Target);
			b.Append(",");

			foreach (Utilities.Extension.Tuple<object, int> requirement in _itemRequirements) {
				b.Append(requirement.Item1);
				b.Append(",");
				b.Append(requirement.Item2);
				b.Append(",");
			}

			return b.ToString();
		}

		public EvolutionTarget Copy() {
			EvolutionTarget target = new EvolutionTarget();

			target.Target = this.Target;
			target.ItemRequirements = new List<Utilities.Extension.Tuple<object, int>>(this.ItemRequirements.Count);

			foreach (var requirement in ItemRequirements) {
				target.ItemRequirements.Add(new Utilities.Extension.Tuple<object, int>(requirement.Item1, requirement.Item2));
			}

			return target;
		}
	}

	public class Evolution {
		private List<EvolutionTarget> _targets = new List<EvolutionTarget>();

		public List<EvolutionTarget> Targets {
			get { return _targets; }
			set { _targets = value; }
		}

		public Evolution() {
		}

		public Evolution(string evolutionStringFormat) {
			if (String.IsNullOrEmpty(evolutionStringFormat))
				return;

			string[] data = evolutionStringFormat.Trim(',').Split(',');

			for (int i = 0; i < data.Length; i++) {
				var field = data[i];
				
				if (field.StartsWith("#")) {
					var target = new EvolutionTarget();
					target.Target = field.Substring(1);
					Targets.Add(target);
				}
				else {
					var last = Targets.Last();
					last.ItemRequirements.Add(new Utilities.Extension.Tuple<object, int>(field, Int32.Parse(data[i + 1])));
					i++;
				}
			}
		}

		public Evolution(string evolutionStringFormat, MetaTable<int> itemDb, MetaTable<int> mobDb) {
			if (String.IsNullOrEmpty(evolutionStringFormat))
				return;

			string[] data = evolutionStringFormat.Trim(',').Split(',');

			for (int i = 0; i < data.Length; i++) {
				var field = data[i];

				if (field.StartsWith("#")) {
					var target = new EvolutionTarget();
					target.Target = DbIOUtils.Name2Id(mobDb, ServerMobAttributes.AegisName, field.Substring(1), "mob_db", true).ToString();
					Targets.Add(target);
				}
				else {
					var last = Targets.Last();
					last.ItemRequirements.Add(new Utilities.Extension.Tuple<object, int>(DbIOUtils.Name2Id(itemDb, ServerItemAttributes.AegisName, data[i], "item_db", true), Int32.Parse(data[i + 1])));
					i++;
				}
			}
		}

		public Evolution Copy() {
			Evolution evolution = new Evolution();

			evolution.Targets = new List<EvolutionTarget>(evolution.Targets.Count);

			foreach (var target in this.Targets) {
				evolution.Targets.Add(target.Copy());
			}

			return evolution;
		}

		public override string ToString() {
			StringBuilder b = new StringBuilder();

			foreach (var target in Targets) {
				b.Append(target);
			}

			return b.ToString();
		}
	}

	public class EvolutionView : IEditableObject, INotifyPropertyChanged {
		private bool _inTxn;
		private AccEditDialog.AccessoryItem _custData;
		private AccEditDialog.AccessoryItem _backupData;

		public EvolutionView(AccEditDialog.AccessoryItem item) {
			_custData = item;
		}

		public int Id {
			get { return this._custData.Id; }
			set {
				this._custData.Id = value;
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
