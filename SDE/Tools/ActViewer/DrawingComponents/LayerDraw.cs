using GRF.FileFormats.ActFormat;
using GRF.Image;
using SDE.ApplicationConfiguration;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Frame = GRF.FileFormats.ActFormat.Frame;
using Point = System.Windows.Point;

namespace SDE.Tools.ActViewer.DrawingComponents
{
    /// <summary>
    /// Drawing component for a frame's layer.
    /// </summary>
    public class LayerDraw : DrawingComponent
    {
        public const string SelectionBorderBrush = "SelectionBorderBrush";
        public const string SelectionOverlayBrush = "SelectionOverlayBrush";

        private static readonly Thickness _bufferedThickness = new Thickness(1);

        private readonly TransformGroup _borderTransformGroup = new TransformGroup();
        private readonly RotateTransform _rotate = new RotateTransform();
        private readonly ScaleTransform _scale = new ScaleTransform();
        private readonly TransformGroup _transformGroup = new TransformGroup();
        private readonly TranslateTransform _translateFrame = new TranslateTransform();
        private readonly TranslateTransform _translateToCenter = new TranslateTransform();

        private readonly IPreview _preview;
        private Act _act;
        private Border _border;
        private Image _image;
        private Layer _layer;
        private Layer _layerCopy;
        private ScaleTransform _scalePreview = new ScaleTransform();
        private TranslateTransform _translatePreview = new TranslateTransform();

        static LayerDraw()
        {
            BufferedBrushes.Register(SelectionBorderBrush, () => SdeAppConfiguration.ActEditorSpriteSelectionBorder);
            BufferedBrushes.Register(SelectionOverlayBrush, () => SdeAppConfiguration.ActEditorSpriteSelectionBorderOverlay);
        }

        public LayerDraw()
        {
            _transformGroup.Children.Add(_translateToCenter);
            _transformGroup.Children.Add(_scale);
            _transformGroup.Children.Add(_rotate);
            _transformGroup.Children.Add(_translateFrame);
            _transformGroup.Children.Add(_scalePreview);
            _transformGroup.Children.Add(_translatePreview);

            _borderTransformGroup.Children.Add(_translateToCenter);
            _borderTransformGroup.Children.Add(_scale);
            _borderTransformGroup.Children.Add(_rotate);
            _borderTransformGroup.Children.Add(_translateFrame);
            _borderTransformGroup.Children.Add(_scalePreview);
            _borderTransformGroup.Children.Add(_translatePreview);
        }

        public LayerDraw(IPreview preview, Act act, int layerIndex) : this()
        {
            _preview = preview;
            _act = act;
            LayerIndex = layerIndex;
        }

        public int LayerIndex { get; private set; }

        public Layer Layer
        {
            get { return _act.TryGetLayer(_preview.SelectedAction, _preview.SelectedFrame, LayerIndex); }
        }

        public override bool IsSelected
        {
            get { return base.IsSelected; }
            set
            {
                base.IsSelected = value;
                _border.Visibility = IsSelected ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public override bool IsSelectable
        {
            get { return base.IsSelectable; }
            set
            {
                if (base.IsSelectable && base.IsSelectable == value ||
                    !base.IsSelectable && base.IsSelectable == value)
                    return;

                base.IsSelectable = value;

                _initBorder();
            }
        }

        private Brush _getBorderBrush()
        {
            return BufferedBrushes.GetBrush(SelectionBorderBrush);
        }

        private Brush _getBorderBackgroundBrush()
        {
            return BufferedBrushes.GetBrush(SelectionOverlayBrush);
        }

        public void Init(Act act, int layerIndex)
        {
            _act = act;
            LayerIndex = layerIndex;
        }

        public override void OnSelected(int index, bool isSelected)
        {
            base.OnSelected(LayerIndex, isSelected);
        }

        public override void Select()
        {
            _initBorder();

            if (!IsSelectable)
            {
                IsSelected = false;
                return;
            }

            IsSelected = true;
        }

        private void _initBorder()
        {
            if (_border == null)
            {
                _border = new Border();
                _border.BorderThickness = _bufferedThickness;
                _border.BorderBrush = _getBorderBrush();
                _border.Background = _getBorderBackgroundBrush();
                _border.SnapsToDevicePixels = true;
                _border.IsHitTestVisible = false;
                _border.Visibility = Visibility.Hidden;
            }

            if (!IsSelectable)
            {
                IsSelected = false;
                IsHitTestVisible = false;
                _border.IsHitTestVisible = false;

                if (_image != null)
                {
                    _image.IsHitTestVisible = false;
                }
            }
        }

        private void _initImage()
        {
            if (_image == null)
            {
                _image = new Image();
                _image.SnapsToDevicePixels = true;

                if (!IsSelectable)
                {
                    _image.IsHitTestVisible = false;
                }
                else
                {
                    _image.PreviewMouseLeftButtonUp += _image_MouseLeftButtonUp;
                }
            }
        }

        private void _initDc(IPreview frameEditor)
        {
            if (!frameEditor.Canva.Children.Contains(_image))
                frameEditor.Canva.Children.Add(_image);

            if (!frameEditor.Canva.Children.Contains(_border))
                frameEditor.Canva.Children.Add(_border);
        }

        private bool _valideMouseOperation()
        {
            if (!IsSelectable)
            {
                IsSelected = false;
                return false;
            }

            return true;
        }

        private void _image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_valideMouseOperation()) return;

            IsSelected = !IsSelected;

            ReleaseMouseCapture();
            e.Handled = true;
        }

        public override void Render(IPreview frameEditor)
        {
            _initBorder();
            _initImage();
            _initDc(frameEditor);

            Act act = _act ?? frameEditor.Act;

            int actionIndex = frameEditor.SelectedAction;
            int frameIndex = frameEditor.SelectedFrame;
            int? anchorFrameIndex = null;

            if (actionIndex >= act.NumberOfActions) return;
            if (act.Name == "Head" || act.Name == "Body")
            {
                bool handled = false;

                if (act[actionIndex].NumberOfFrames == 3 &&
                    (0 <= actionIndex && actionIndex < 8) ||
                    (16 <= actionIndex && actionIndex < 24))
                {
                    if (frameEditor.Act != null)
                    {
                        Act editorAct = frameEditor.Act;

                        int group = editorAct[actionIndex].NumberOfFrames / 3;

                        if (group != 0)
                        {
                            anchorFrameIndex = frameIndex;

                            if (frameIndex < group)
                            {
                                frameIndex = 0;
                                handled = true;
                            }
                            else if (frameIndex < 2 * group)
                            {
                                frameIndex = 1;
                                handled = true;
                            }
                            else if (frameIndex < 3 * group)
                            {
                                frameIndex = 2;
                                handled = true;
                            }
                            else
                            {
                                frameIndex = 2;
                                handled = true;
                            }
                        }
                    }
                }

                if (!handled)
                {
                    if (frameIndex >= act[actionIndex].NumberOfFrames)
                    {
                        if (act[actionIndex].NumberOfFrames > 0)
                            frameIndex = frameIndex % act[actionIndex].NumberOfFrames;
                        else
                            frameIndex = 0;
                    }
                }
            }
            else
            {
                if (frameIndex >= act[actionIndex].NumberOfFrames)
                {
                    if (act[actionIndex].NumberOfFrames > 0)
                        frameIndex = frameIndex % act[actionIndex].NumberOfFrames;
                    else
                        frameIndex = 0;
                }
            }

            Frame frame = act[actionIndex, frameIndex];
            if (LayerIndex >= frame.NumberOfLayers) return;

            _layer = act[actionIndex, frameIndex, LayerIndex];

            if (_layer.SpriteIndex < 0)
            {
                _image.Source = null;
                return;
            }

            int index = _layer.IsBgra32() ? _layer.SpriteIndex + act.Sprite.NumberOfIndexed8Images : _layer.SpriteIndex;

            if (index < 0 || index >= act.Sprite.Images.Count)
            {
                _image.Source = null;
                return;
            }

            GrfImage img = act.Sprite.Images[index];

            if (img.GrfImageType == GrfImageType.Indexed8)
            {
                img = img.Copy();
                img.Palette[3] = 0;
            }

            int diffX = 0;
            int diffY = 0;

            if (act.AnchoredTo != null && frame.Anchors.Count > 0)
            {
                Frame frameReference;

                if (anchorFrameIndex != null && act.Name != null && act.AnchoredTo.Name != null)
                {
                    frameReference = act.AnchoredTo.TryGetFrame(actionIndex, frameIndex);

                    if (frameReference == null)
                    {
                        frameReference = act.AnchoredTo.TryGetFrame(actionIndex, anchorFrameIndex.Value);
                    }
                }
                else
                {
                    frameReference = act.AnchoredTo.TryGetFrame(actionIndex, anchorFrameIndex ?? frameIndex);
                }

                if (frameReference != null && frameReference.Anchors.Count > 0)
                {
                    diffX = frameReference.Anchors[0].OffsetX - frame.Anchors[0].OffsetX;
                    diffY = frameReference.Anchors[0].OffsetY - frame.Anchors[0].OffsetY;

                    if (act.AnchoredTo.AnchoredTo != null)
                    {
                        frameReference = act.AnchoredTo.AnchoredTo.TryGetFrame(actionIndex, anchorFrameIndex ?? frameIndex);

                        if (frameReference != null && frameReference.Anchors.Count > 0)
                        {
                            diffX = frameReference.Anchors[0].OffsetX - frame.Anchors[0].OffsetX;
                            diffY = frameReference.Anchors[0].OffsetY - frame.Anchors[0].OffsetY;
                        }
                    }
                }
            }

            int extraX = _layer.Mirror ? -(img.Width + 1) % 2 : 0;

            _translateToCenter.X = -((img.Width + 1) / 2) + extraX;
            _translateToCenter.Y = -((img.Height + 1) / 2);
            _translateFrame.X = _layer.OffsetX + diffX;
            _translateFrame.Y = _layer.OffsetY + diffY;

            _scale.ScaleX = _layer.ScaleX * (_layer.Mirror ? -1 : 1);
            _scale.ScaleY = _layer.ScaleY;

            _rotate.Angle = _layer.Rotation;

            _image.RenderTransform = _transformGroup;
            _image.SetValue(RenderOptions.BitmapScalingModeProperty, SdeAppConfiguration.ActEditorScalingMode);

            img = img.Copy();
            img.ApplyChannelColor(_layer.Color);
            _image.Source = img.Cast<BitmapSource>();
            _image.VerticalAlignment = VerticalAlignment.Top;
            _image.HorizontalAlignment = HorizontalAlignment.Left;

            _border.Width = img.Width;
            _border.Height = img.Height;
            _border.RenderTransform = _borderTransformGroup;
            //_border.RenderTransformOrigin = new Point(0.5, 0.5);

            QuickRender(frameEditor);
        }

        public override void QuickRender(IPreview frameEditor)
        {
            if (_scalePreview == null)
                _scalePreview = new ScaleTransform();

            _scalePreview.CenterX = frameEditor.CenterX;
            _scalePreview.CenterY = frameEditor.CenterY;

            _scalePreview.ScaleX = frameEditor.ZoomEngine.Scale;
            _scalePreview.ScaleY = frameEditor.ZoomEngine.Scale;

            if (_translatePreview == null)
                _translatePreview = new TranslateTransform();

            _translatePreview.X = frameEditor.CenterX * frameEditor.ZoomEngine.Scale;
            _translatePreview.Y = frameEditor.CenterY * frameEditor.ZoomEngine.Scale;

            if (_border != null)
            {
                _border.SetValue(RenderOptions.EdgeModeProperty, SdeAppConfiguration.UseAliasing ? EdgeMode.Aliased : EdgeMode.Unspecified);
                _border.BorderBrush = _getBorderBrush();
                _border.Background = _getBorderBackgroundBrush();

                if (_image.Source == null)
                {
                    _border.BorderThickness = new Thickness(0);
                    _border.Width = 0;
                    _border.Height = 0;
                }
                else
                {
                    double scaleX = Math.Abs(1d / (frameEditor.ZoomEngine.Scale * _scale.ScaleX));
                    double scaleY = Math.Abs(1d / (frameEditor.ZoomEngine.Scale * _scale.ScaleY));

                    if (double.IsInfinity(scaleX) || double.IsNaN(scaleX) ||
                        double.IsInfinity(scaleY) || double.IsNaN(scaleY))
                    {
                        _border.Width = 0;
                        _border.Height = 0;
                    }
                    else
                    {
                        _border.BorderThickness = new Thickness(scaleX, scaleY, scaleX, scaleY);
                    }
                }
            }
        }

        public override void Remove(IPreview frameEditor)
        {
            if (_image != null)
                frameEditor.Canva.Children.Remove(_image);

            if (_border != null)
                frameEditor.Canva.Children.Remove(_border);
        }

        public void SaveInitialData()
        {
            _layerCopy = new Layer(_layer);
        }

        public bool IsMouseUnder(MouseEventArgs e)
        {
            _initImage();

            try
            {
                if (_scale.ScaleX == 0 || _scale.ScaleY == 0) return false;

                return ReferenceEquals(_image.InputHitTest(e.GetPosition(_image)), _image);
            }
            catch
            {
                return false;
            }
        }

        public bool IsMouseUnder(Point point)
        {
            _initImage();

            try
            {
                if (_scale.ScaleX == 0 || _scale.ScaleY == 0) return false;

                return ReferenceEquals(_image.InputHitTest(_image.PointFromScreen(point)), _image);
            }
            catch
            {
                return false;
            }
        }
    }
}