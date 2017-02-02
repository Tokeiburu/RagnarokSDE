using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using SDE.ApplicationConfiguration;

namespace SDE.Tools.ActViewer.DrawingComponents {
	/// <summary>
	/// Drawing component for the selection rectangle.
	/// </summary>
	public class SelectionDraw : DrawingComponent {
		public const string SelectionBorder = "Selection_Border";
		public const string SelectionOverlay = "Selection_Overlay";
		private Rectangle _line;
		private bool _visible = true;

		static SelectionDraw() {
			BufferedBrushes.Register(SelectionBorder, () => SdeAppConfiguration.ActEditorSelectionBorder);
			BufferedBrushes.Register(SelectionOverlay, () => SdeAppConfiguration.ActEditorSelectionBorderOverlay);
		}

		public SelectionDraw() {
			IsHitTestVisible = false;
		}

		public bool Visible {
			get { return _visible; }
			set {
				_visible = value;

				if (_line != null) {
					_line.Visibility = value ? Visibility.Visible : Visibility.Hidden;
				}
			}
		}

		public override void Render(IPreview frameEditor) {
		}

		public void Render(IPreview frameEditor, Rect rect) {
			if (_line == null) {
				_line = new Rectangle();
				frameEditor.Canva.Children.Add(_line);
				_line.StrokeThickness = 1;
				_line.SnapsToDevicePixels = true;
				_line.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
			}

			_line.Fill = BufferedBrushes.GetBrush(SelectionOverlay);
			_line.Height = (int) rect.Height;
			_line.Width = (int) rect.Width;

			_line.Margin = new Thickness((int) rect.X, (int) rect.Y, 0, 0);
			_line.Stroke = BufferedBrushes.GetBrush(SelectionBorder);
		}

		public override void QuickRender(IPreview frameEditor) {
		}

		public override void Remove(IPreview frameEditor) {
			if (_line != null)
				frameEditor.Canva.Children.Remove(_line);
		}
	}
}