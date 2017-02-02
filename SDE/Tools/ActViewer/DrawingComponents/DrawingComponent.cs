using System.Windows.Controls;

namespace SDE.Tools.ActViewer.DrawingComponents {
	/// <summary>
	/// The drawing component class is used to display items
	/// in the FramePreview.
	/// </summary>
	public abstract class DrawingComponent : Control {
		#region Delegates

		public delegate void DrawingComponentDelegate(object sender, int index, bool selected);

		#endregion

		private bool _isSelected;
		public virtual bool IsSelectable { get; set; }

		public virtual bool IsSelected {
			get { return _isSelected; }
			set {
				bool raise = _isSelected != value;

				_isSelected = value;

				if (raise)
					OnSelected(-1, _isSelected);
			}
		}

		public event DrawingComponentDelegate Selected;

		public virtual void OnSelected(int index, bool isSelected) {
			DrawingComponentDelegate handler = Selected;
			if (handler != null) handler(this, index, isSelected);
		}

		/// <summary>
		/// Renders the element in the IPreview object.
		/// </summary>
		/// <param name="frameEditor">The frame editor.</param>
		public abstract void Render(IPreview frameEditor);

		/// <summary>
		/// Renders only the essential parts without reloading the elements.
		/// </summary>
		/// <param name="frameEditor">The frame editor.</param>
		public abstract void QuickRender(IPreview frameEditor);

		/// <summary>
		/// Removes the element from the IPreview object.
		/// </summary>
		/// <param name="frameEditor">The frame editor.</param>
		public abstract void Remove(IPreview frameEditor);


		/// <summary>
		/// Selects the element (if the component supports this operation).
		/// </summary>
		public virtual void Select() {
		}
	}
}