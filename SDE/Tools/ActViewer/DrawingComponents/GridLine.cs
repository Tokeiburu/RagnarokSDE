using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SDE.ApplicationConfiguration;

namespace SDE.Tools.ActViewer.DrawingComponents {
	/// <summary>
	/// Drawing component for a grid line.
	/// </summary>
	public class GridLine : DrawingComponent {
		public const string GridLineHorizontalBrush = "Horizontal";
		public const string GridLineVerticalBrush = "Vertical";
		private readonly Orientation _orientation;
		private Rectangle _line;
		private bool _visible = true;

		static GridLine() {
			BufferedBrushes.Register(GridLineHorizontalBrush, () => SdeAppConfiguration.ActEditorGridLineHorizontal);
			BufferedBrushes.Register(GridLineVerticalBrush, () => SdeAppConfiguration.ActEditorGridLineVertical);
		}

		public GridLine(Orientation orientation) {
			_orientation = orientation;
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
			if (_line != null) {
				if (_orientation == Orientation.Horizontal)
					_line.Visibility = SdeAppConfiguration.ActEditorGridLineHVisible ? Visibility.Visible : Visibility.Hidden;
				else
					_line.Visibility = SdeAppConfiguration.ActEditorGridLineVVisible ? Visibility.Visible : Visibility.Hidden;
			}

			if (_line == null) {
				_line = new Rectangle();
				frameEditor.Canva.Children.Add(_line);
				_line.StrokeThickness = 1;
				_line.SnapsToDevicePixels = true;
				_line.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
				_line.Height = 1;
				_line.Width = 1;
			}

			if (_orientation == Orientation.Horizontal) {
				_line.Margin = new Thickness(0, frameEditor.CenterY, 0, 0);
				_line.Width = frameEditor.Canva.ActualWidth + 50;
				_line.Stroke = _getColor();
			}
			else if (_orientation == Orientation.Vertical) {
				_line.Margin = new Thickness(frameEditor.CenterX, 0, 0, 0);
				_line.Height = frameEditor.Canva.ActualHeight + 50;
				_line.Stroke = _getColor();
			}
		}

		public override void QuickRender(IPreview frameEditor) {
			if (_line == null) {
				Render(frameEditor);
			}
			else {
				if (_orientation == Orientation.Horizontal) {
					_line.Margin = new Thickness(0, frameEditor.CenterY, 0, 0);
					_line.Width = frameEditor.Canva.ActualWidth + 50;
				}
				else if (_orientation == Orientation.Vertical) {
					_line.Margin = new Thickness(frameEditor.CenterX, 0, 0, 0);
					_line.Height = frameEditor.Canva.ActualHeight + 50;
				}

				_line.Stroke = _getColor();
			}
		}

		private Brush _getColor() {
			return BufferedBrushes.GetBrush(_orientation == Orientation.Horizontal ? GridLineHorizontalBrush : GridLineVerticalBrush);
		}

		public override void Remove(IPreview frameEditor) {
			if (_line != null)
				frameEditor.Canva.Children.Remove(_line);
		}
	}
}