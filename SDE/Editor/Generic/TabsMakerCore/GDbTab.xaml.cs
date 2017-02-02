using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Database;
using ErrorManager;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Generic.Core;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;

namespace SDE.Editor.Generic.TabsMakerCore {
	/// <summary>
	/// Interaction logic for GDbTab.xaml
	/// </summary>
	public partial class GDbTab : TabItem {
		public static Tuple LastSelectedTuple;

		public GDbTab() {
			InitializeComponent();

			_miDelete.InputGestureText = ApplicationShortcut.FindDislayName(ApplicationShortcut.Delete);
			_miChangeId.InputGestureText = ApplicationShortcut.FindDislayName(ApplicationShortcut.Change);
			_miCopyTo.InputGestureText = ApplicationShortcut.FindDislayName(ApplicationShortcut.CopyTo);
			_miCut.InputGestureText = ApplicationShortcut.FindDislayName(ApplicationShortcut.Cut);
			_miShowSelected.InputGestureText = ApplicationShortcut.FindDislayName(ApplicationShortcut.Restrict);
			_miSelectInNotepad.InputGestureText = ApplicationShortcut.FindDislayName(ApplicationShortcut.FromString("Ctrl-W", "Open in Notepad++"));
			//_miChangeId.InputGestureText = ApplicationShortcut.FindDislayName(ApplicationShortcut.);
		}

		public virtual DbAttribute DisplayAttribute {
			get { throw new InvalidOperationException(); }
		}

		public virtual DbAttribute IdAttribute {
			get { throw new InvalidOperationException(); }
		}

		public SdeDatabase ProjectDatabase { get; set; }

		public BaseDb DbComponent { get; set; }

		public bool DelayedReload { get; protected set; }

		public virtual bool IsFiltering { get; set; }

		public new virtual bool IsSelected {
			get { return base.IsSelected; }
			set { base.IsSelected = value; }
		}

		public GDbTabWrapper<TKey, ReadableTuple<TKey>> To<TKey>() {
			return (GDbTabWrapper<TKey, ReadableTuple<TKey>>)this;
		}

		public virtual void Update() {
		}

		public virtual void SetRange(List<int> selectedIds) {
		}

		public virtual void SelectItems(List<Tuple> selectedIds) {
		}

		public virtual void CopyItemTo() {
		}

		public virtual void CopyItemTo(BaseDb db) {
		}

		public virtual void DeleteItems() {
		}

		public virtual void ChangeId() {
		}

		public virtual void ImportFromFile(string fileDefault = null, bool autoIncrement = false) {
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

		public virtual void IgnoreFilterOnce() {
		}

		public virtual void ReplaceFromFile() {
		}

		public virtual void TabChanged() {
		}

		public void SelectNext() {
			try {
				if (_listView.SelectedItems.Count <= 1) {
					_listView.ScrollToCenterOfView(_listView.SelectedItem);
					return;
				}

				var item = _listView.SelectedItem;
				_listView.SelectedItems.Remove(item);
				_listView.SelectedItems.Add(item);

				_listView.ScrollToCenterOfView(_listView.SelectedItem);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void SelectPrevious() {
			try {
				if (_listView.SelectedItems.Count <= 1) {
					_listView.ScrollToCenterOfView(_listView.SelectedItem);
					return;
				}

				var last = _listView.SelectedItems.OfType<Tuple>().Last();
				_listView.SelectedItems.Remove(last);
				_listView.SelectedItems.Insert(0, last);

				var items = new List<Tuple>(_listView.SelectedItems.OfType<Tuple>());

				_listView.SelectedItem = null;

				foreach (var i in items) {
					_listView.SelectedItems.Add(i);
				}

				_listView.ScrollToCenterOfView(_listView.SelectedItem);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}