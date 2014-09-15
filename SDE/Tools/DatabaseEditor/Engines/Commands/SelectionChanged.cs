using System.Windows.Controls;
using Database;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;

namespace SDE.Tools.DatabaseEditor.Engines.Commands {
	public class SelectionChanged : INagivationCommand {
		private readonly GDbTab _tab;
		private readonly string _tabName;
		private readonly object _tuple;
		private readonly ListView _view;

		public SelectionChanged(string tabName, object tuple, ListView view, GDbTab tab) {
			_tabName = tabName;
			_tuple = tuple;
			_view = view;
			_tab = tab;
		}

		public object Tuple {
			get { return _tuple; }
		}

		public SelectionChanged PreviousPosition { get; set; }
		public ListView View {
			get {
				return _view;
			}
		}

		#region INagivationCommand Members

		public void Execute(TabNavigationEngine navEngine) {
			navEngine.Select(_tabName, _tuple, _view);
		}

		public void Undo(TabNavigationEngine navEngine) {
			if (PreviousPosition != null) {
				PreviousPosition.Execute(navEngine);
			}
		}

		public string CommandDescription {
			get {
				if (_tuple is Tuple) {
					if (_tab != null) {
						return "[" + _tabName + "] - [" + ((Tuple)_tuple).GetValue(_tab.IdAttribute) + "], '" + ((Tuple)_tuple).GetValue(_tab.DisplayAttribute) + "'";
					}

					return "[" + _tabName + "] - [" + ((Tuple)_tuple).GetValue(0) + "], '" + ((Tuple)_tuple).GetValue(1) + "'";
				}
				return "[" + _tabName + "] - '" + _tuple + "'";
			}
		}

		#endregion
	}
}