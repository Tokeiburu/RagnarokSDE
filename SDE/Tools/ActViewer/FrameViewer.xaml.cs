using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ErrorManager;
using GRF.FileFormats.ActFormat;
using SDE.ApplicationConfiguration;
using SDE.Tools.ActViewer.DrawingComponents;
using TokeiLibrary;
using Utilities.Tools;
using Frame = GRF.FileFormats.ActFormat.Frame;

namespace SDE.Tools.ActViewer {
	/// <summary>
	/// Interaction logic for FrameViewer.xaml
	/// </summary>
	public partial class FrameViewer : UserControl, IPreview {
		protected readonly List<DrawingComponent> _components = new List<DrawingComponent>();
		private ZoomEngine _zoomEngine = new ZoomEngine();
		private bool _isAnyDown;
		private Point _oldPosition;
		private Point _relativeCenter = new Point(0.5, 0.8);
		private FrameViewerSettings _settings = new FrameViewerSettings();
		private bool _isGarmentMode;

		public FrameViewer() {
			InitializeComponent();

			ReloadSettings();

			_primary.Background = new SolidColorBrush(SdeAppConfiguration.ActEditorBackgroundColor);

			_components.Add(new GridLine(Orientation.Horizontal));
			_components.Add(new GridLine(Orientation.Vertical));

			SizeChanged += new SizeChangedEventHandler(_framePreview_SizeChanged);
			MouseMove += new MouseEventHandler(_framePreview_MouseMove);
			MouseDown += new MouseButtonEventHandler(_framePreview_MouseDown);
			MouseUp += new MouseButtonEventHandler(_framePreview_MouseUp);
			MouseWheel += new MouseWheelEventHandler(_framePreview_MouseWheel);
		}

		public Point RelativeCenter {
			get { return _relativeCenter; }
			set { _relativeCenter = value; }
		}

		public ActDraw MainDrawingComponent {
			get { return _components.OfType<ActDraw>().FirstOrDefault(p => p.Primary); }
		}

		public Grid GridBackground {
			get { return _gridBackground; }
		}

		public void InitComponent(FrameViewerSettings settings) {
			_settings = settings;
		}

		internal void ReloadSettings() {
			_zoomEngine = new ZoomEngine { ZoomInMultiplier = _settings.ZoomInMultipler };
		}

		private void _framePreview_SizeChanged(object sender, SizeChangedEventArgs e) {
			SizeUpdate();
		}

		public void SizeUpdate() {
			_updateBackground();

			foreach (var dc in _components) {
				dc.QuickRender(this);
			}
		}

		private void _updateBackground() {
			try {
				if (ZoomEngine.Scale < 0.45) {
					((VisualBrush)_gridBackground.Background).Viewport = new Rect(RelativeCenter.X, RelativeCenter.Y, 16d / (_gridBackground.ActualWidth), 16d / (_gridBackground.ActualHeight));
				}
				else {
					((VisualBrush)_gridBackground.Background).Viewport = new Rect(RelativeCenter.X, RelativeCenter.Y, 16d / (_gridBackground.ActualWidth / ZoomEngine.Scale), 16d / (_gridBackground.ActualHeight / ZoomEngine.Scale));
				}
			}
			catch {
			}
		}

		public Canvas Canva {
			get { return _primary; }
		}

		public int CenterX {
			get { return (int)(_primary.ActualWidth * _relativeCenter.X); }
		}

		public int CenterY {
			get { return (int)(_primary.ActualHeight * _relativeCenter.Y); }
		}

		public ZoomEngine ZoomEngine {
			get { return _zoomEngine; }
		}

		public Act Act { get { return _settings.Act(); } }
		public int SelectedAction { get { return _settings.SelectedAction(); } }
		public int SelectedFrame { get { return _settings.SelectedFrame(); } }
		public List<DrawingComponent> Components {
			get { return _components; }
		}

		public void Update() {
			_updateBackground();

			while (_components.Count > 2) {
				_components[2].Remove(this);
				_components.RemoveAt(2);
			}

			var relActionIndex = SelectedAction % 8;
			if (_isGarmentMode && (relActionIndex == 0 || relActionIndex == 1 || relActionIndex == 6 || relActionIndex == 7)) {
				if (Act != null) {
					var primary = new ActDraw(Act, this);
					_components.Add(primary);
				}

				foreach (var refFrame in _settings.References.Where(p => p.Show && p.Mode == ZMode.Back)) {
					_components.Add(new ActDraw(refFrame.Act, this));
				}
			}
			else {
				foreach (var refFrame in _settings.References.Where(p => p.Show && p.Mode == ZMode.Back)) {
					_components.Add(new ActDraw(refFrame.Act, this));
				}

				if (Act != null) {
					var primary = new ActDraw(Act, this);
					_components.Add(primary);
				}
			}

			foreach (var refFrame in _settings.References.Where(p => p.Show && p.Mode == ZMode.Front)) {
				_components.Add(new ActDraw(refFrame.Act, this));
			}

			foreach (var dc in _components) {
				dc.Render(this);
			}
		}

		private void _framePreview_MouseDown(object sender, MouseButtonEventArgs e) {
			try {
				_isAnyDown = true;

				if (Keyboard.FocusedElement != _cbZoom)
					Keyboard.Focus(this);

				_oldPosition = e.GetPosition(this);

				if (e.RightButton == MouseButtonState.Pressed) {
					CaptureMouse();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _framePreview_MouseUp(object sender, MouseButtonEventArgs e) {
			try {
				_isAnyDown = false;

				if ((this.GetObjectAtPoint<ComboBox>(e.GetPosition(this)) as ComboBox) != _cbZoom)
					e.Handled = true;

				ReleaseMouseCapture();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _framePreview_MouseWheel(object sender, MouseWheelEventArgs e) {
			if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) return;

			ZoomEngine.Zoom(e.Delta);

			Point mousePosition = e.GetPosition(_primary);

			// The relative center must be moved as well!
			double diffX = mousePosition.X / _primary.ActualWidth - _relativeCenter.X;
			double diffY = mousePosition.Y / _primary.ActualHeight - _relativeCenter.Y;

			_relativeCenter.X = mousePosition.X / _primary.ActualWidth - diffX / ZoomEngine.OldScale * ZoomEngine.Scale;
			_relativeCenter.Y = mousePosition.Y / _primary.ActualHeight - diffY / ZoomEngine.OldScale * ZoomEngine.Scale;

			_cbZoom.SelectedIndex = -1;
			_cbZoom.Text = _zoomEngine.ScaleText;
			SizeUpdate();
		}

		private void _framePreview_MouseMove(object sender, MouseEventArgs e) {
			try {
				if (e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Released)
					return;

				if (ContextMenu != null && ContextMenu.IsOpen) return;

				Point current = e.GetPosition(this);

				double deltaX = (current.X - _oldPosition.X);
				double deltaY = (current.Y - _oldPosition.Y);

				if (deltaX == 0 && deltaY == 0)
					return;

				if ((this.GetObjectAtPoint<ComboBox>(e.GetPosition(this)) as ComboBox) == _cbZoom && !IsMouseCaptured)
					return;

				if (e.RightButton == MouseButtonState.Pressed && _isAnyDown) {
					_relativeCenter.X = _relativeCenter.X + deltaX / Canva.ActualWidth;
					_relativeCenter.Y = _relativeCenter.Y + deltaY / Canva.ActualHeight;

					_oldPosition = current;
					SizeUpdate();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _cbZoom_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_cbZoom.SelectedIndex < 0) return;

			_zoomEngine.SetZoom(double.Parse(((string)((ComboBoxItem)_cbZoom.SelectedItem).Content).Replace(" %", "")) / 100f);
			_cbZoom.Text = _zoomEngine.ScaleText;
			SizeUpdate();
		}

		private void _cbZoom_MouseEnter(object sender, MouseEventArgs e) {
			_cbZoom.Opacity = 1;
			_cbZoom.StaysOpenOnEdit = true;
		}

		private void _cbZoom_MouseLeave(object sender, MouseEventArgs e) {
			if (e.LeftButton == MouseButtonState.Released)
				_cbZoom.Opacity = 0.7;
		}

		private void _cbZoom_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter) {
				try {
					string text = _cbZoom.Text;

					text = text.Replace(" ", "").Replace("%", "");
					_cbZoom.SelectedIndex = -1;

					double value = double.Parse(text);

					_zoomEngine.SetZoom(value / 100f);
					_cbZoom.Text = _zoomEngine.ScaleText;
					SizeUpdate();
					e.Handled = true;
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}

		public void SetGarmentMode(bool state) {
			_isGarmentMode = state;
		}
	}

	public class FrameViewerSettings {
		public Func<double> ZoomInMultipler = () => 1d;
		public Func<Act> Act = () => null;
		public Func<int> SelectedAction = () => 0;
		public Func<int> SelectedFrame = () => 0;
		public Func<bool> ShowAnchors = () => false;
		public Func<List<ActReference>> ReferencesGetter = () => new List<ActReference>();
		public List<ActReference> References {
			get { return ReferencesGetter(); }
		}
	}

	public class ActReference {
		public Act Act { get; set; }
		public bool Show { get; set; }
		public ZMode Mode { get; set; }
	}
}
