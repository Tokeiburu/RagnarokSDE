using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Database;
using SDE.Tools.DatabaseEditor.Engines;

namespace SDE.Tools.DatabaseEditor.Generic.TabsMakerCore {
	/// <summary>
	/// Interaction logic for GDbTab.xaml
	/// </summary>
	public partial class GDbTab : TabItem {
		public GDbTab() {
			InitializeComponent();
		}

		public virtual DbAttribute DisplayAttribute { get { throw new InvalidOperationException(); } }
		public virtual DbAttribute IdAttribute { get { throw new InvalidOperationException(); } }

		public BaseGenericDatabase Database { get; set; }

		public bool DelayedReload { get; protected set; }

		public virtual bool IsFiltering { get; set; }

		public GDbTabWrapper<TKey, ReadableTuple<TKey>> To<TKey>() {
			return (GDbTabWrapper<TKey, ReadableTuple<TKey>>)this;
		}

		public virtual void TabSelected() {
		}

		public virtual void Update() {
		}

		public virtual void SetRange(List<int> selectedIds) {
		}

		public virtual void SelectItems(List<Tuple> selectedIds) {
		}

		public virtual void CopyItemTo() {
		}

		public virtual void DeleteItems() {
		}

		public virtual void ChangeId() {
		}

		public virtual void ShowSelectedOnly() {
		}

		public virtual void AddNewItem() {
		}

		public virtual void AddNewItemRaw() {
		}

		public virtual void Undo() {
		}

		public virtual void Redo() {
		}

		public virtual void Search() {
		}

		public virtual void Filter() {
		}
	}
}
