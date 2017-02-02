using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.ActFormat.Commands;
using GRF.Image;
using GRF.IO;
using GRF.Threading;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines.LuaEngine;
using SDE.Editor.Generic.Lists;
using SDE.View;
using SDE.View.Dialogs;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities.Commands;
using Utilities.Services;
using Action = System.Action;

namespace SDE.Tools.ActViewer {
	/// <summary>
	/// Interaction logic for FrameSelector.xaml
	/// </summary>
	public partial class CompactActIndexSelector : UserControl {
		#region Delegates

		public delegate void FrameIndexChangedDelegate(object sender, int actionIndex);

		#endregion

		private readonly List<FancyButton> _fancyButtons;
		private readonly object _lock = new object();
		private bool _eventsEnabled = true;
		private bool _frameChangedEventEnabled = true;
		private bool _handlersEnabled = true;
		private int _pending;
		private Act _act;
		private FrameViewer _viewer;

		public CompactActIndexSelector() {
			InitializeComponent();

			try {
				_fancyButtons = new FancyButton[] {_fancyButton0, _fancyButton1, _fancyButton2, _fancyButton3, _fancyButton4, _fancyButton5, _fancyButton6, _fancyButton7}.ToList();
				byte[] pixels = ApplicationManager.GetResource("arrow.png");
				BitmapSource image = new GrfImage(ref pixels).Cast<BitmapSource>();

				byte[] pixels2 = ApplicationManager.GetResource("arrowoblique.png");
				BitmapSource image2 = new GrfImage(ref pixels2).Cast<BitmapSource>();

				_fancyButton0.ImageIcon.Source = image;
				_fancyButton0.ImageIcon.RenderTransformOrigin = new Point(0.5, 0.5);
				_fancyButton0.ImageIcon.RenderTransform = new RotateTransform {Angle = 90};

				_fancyButton1.ImageIcon.Source = image2;
				_fancyButton1.ImageIcon.RenderTransformOrigin = new Point(0.5, 0.5);
				_fancyButton1.ImageIcon.RenderTransform = new RotateTransform {Angle = 90};

				_fancyButton2.ImageIcon.Source = image;
				_fancyButton2.ImageIcon.RenderTransformOrigin = new Point(0.5, 0.5);
				_fancyButton2.ImageIcon.RenderTransform = new RotateTransform {Angle = 180};

				_fancyButton3.ImageIcon.Source = image2;
				_fancyButton3.ImageIcon.RenderTransformOrigin = new Point(0.5, 0.5);
				_fancyButton3.ImageIcon.RenderTransform = new RotateTransform {Angle = 180};

				_fancyButton4.ImageIcon.Source = image;
				_fancyButton4.ImageIcon.RenderTransformOrigin = new Point(0.5, 0.5);
				_fancyButton4.ImageIcon.RenderTransform = new RotateTransform {Angle = 270};

				_fancyButton5.ImageIcon.Source = image2;
				_fancyButton5.ImageIcon.RenderTransformOrigin = new Point(0.5, 0.5);
				_fancyButton5.ImageIcon.RenderTransform = new RotateTransform {Angle = 270};

				_fancyButton6.ImageIcon.Source = image;
				_fancyButton6.ImageIcon.RenderTransformOrigin = new Point(0.5, 0.5);
				_fancyButton6.ImageIcon.RenderTransform = new RotateTransform {Angle = 360};

				_fancyButton7.ImageIcon.Source = image2;
				_fancyButton7.ImageIcon.RenderTransformOrigin = new Point(0.5, 0.5);
				_fancyButton7.ImageIcon.RenderTransform = new RotateTransform {Angle = 360};

				_fancyButtons.ForEach(p => p.IsButtonEnabled = false);

				_sbFrameIndex.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(_sbFrameIndex_MouseLeftButtonDown);
				_sbFrameIndex.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(_sbFrameIndex_MouseLeftButtonUp);
			}
			catch {
			}

			try {
				_updatePlay();
				_play.Click += new RoutedEventHandler(_play_Click);

				WpfUtilities.AddFocus(_tbFrameIndex);
				_updateRendering();
			}
			catch {
			}

			this.MouseDown += new MouseButtonEventHandler(_actIndexSelector_MouseDown);

			this.Loaded += delegate {
				//_parent = this.Parent as UIElement;
				//_parent.MouseMove += new MouseEventHandler(_actIndexSelector_MouseMove);
			};

			this.MouseEnter += delegate {
				this.Opacity = 1f;
			};

			this.MouseLeave += delegate {
				this.Opacity = 0.8f;
			};
		}

		private void _actIndexSelector_MouseDown(object sender, MouseButtonEventArgs e) {
			//_mousePosition = e.GetPosition(_parent);
		}

		public int SelectedAction { get; set; }
		public int SelectedFrame { get; set; }

		public event FrameIndexChangedDelegate ActionChanged;
		public event FrameIndexChangedDelegate FrameChanged;
		public event FrameIndexChangedDelegate SpecialFrameChanged;

		public void OnSpecialFrameChanged(int actionindex) {
			if (!_handlersEnabled) return;
			FrameIndexChangedDelegate handler = SpecialFrameChanged;
			if (handler != null) handler(this, actionindex);
		}

		public event FrameIndexChangedDelegate AnimationPlaying;

		public void OnAnimationPlaying(int actionindex) {
			FrameIndexChangedDelegate handler = AnimationPlaying;
			if (handler != null) handler(this, actionindex);
		}

		public void OnFrameChanged(int actionindex) {
			if (!_handlersEnabled) return;
			if (!_frameChangedEventEnabled) {
				OnSpecialFrameChanged(actionindex);
				return;
			}
			FrameIndexChangedDelegate handler = FrameChanged;
			if (handler != null) handler(this, actionindex);
		}

		public void OnActionChanged(int actionindex) {
			if (!_handlersEnabled) return;
			FrameIndexChangedDelegate handler = ActionChanged;
			if (handler != null) handler(this, actionindex);
		}

		private void _sbFrameIndex_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			lock (_lock) {
				_pending++;
			}

			OnAnimationPlaying(0);

			GrfThread.Start(delegate {
				int max = 20;

				while (max-- > 0) {
					if (e.LeftButton == MouseButtonState.Pressed)
						return;

					Thread.Sleep(100);
				}

				// Resets the mouse operations to 0
				lock (_lock) {
					_pending = 0;
				}
			});
		}

		public void SetAction(int index) {
			if (index < _comboBoxActionIndex.Items.Count && index > -1) {
				_comboBoxActionIndex.SelectedIndex = index;
			}
		}

		public void SetFrame(int index) {
			_sbFrameIndex.Value = index;
		}

		private void _sbFrameIndex_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if (_act == null) {
				lock (_lock) {
					_pending--;
				}
				return;
			}

			Point position = e.GetPosition(_sbFrameIndex);

			bool isLeft = position.X > 0 && position.Y > 0 && position.Y < _sbFrameIndex.ActualHeight && position.X < SystemParameters.HorizontalScrollBarButtonWidth;
			bool isRight = position.X > (_sbFrameIndex.ActualWidth - SystemParameters.HorizontalScrollBarButtonWidth) && position.Y > 0 && position.Y < _sbFrameIndex.ActualHeight && position.X < _sbFrameIndex.ActualWidth;
			bool isWithin = position.X > 0 && position.Y > 0 && position.X < _sbFrameIndex.ActualWidth && position.Y < _sbFrameIndex.ActualHeight;

			if (isWithin) {
				OnAnimationPlaying(2);
			}

			if (!isLeft && !isRight) {
				lock (_lock) {
					_pending--;
				}
				return;
			}

			GrfThread.Start(delegate {
				int count = 0;
				while (this.Dispatch(() => Mouse.LeftButton) == MouseButtonState.Pressed) {
					_sbFrameIndex.Dispatch(delegate {
						position = e.GetPosition(_sbFrameIndex);

						isLeft = position.X > 0 && position.Y > 0 && position.Y < _sbFrameIndex.ActualHeight && position.X < SystemParameters.HorizontalScrollBarButtonWidth;
						isRight = position.X > (_sbFrameIndex.ActualWidth - SystemParameters.HorizontalScrollBarButtonWidth) && position.Y > 0 && position.Y < _sbFrameIndex.ActualHeight && position.X < _sbFrameIndex.ActualWidth;
					});

					if (isLeft) {
						SelectedFrame--;
						if (SelectedFrame < 0)
							SelectedFrame = _act[SelectedAction].NumberOfFrames - 1;
					}

					if (isRight) {
						SelectedFrame++;
						if (SelectedFrame >= _act[SelectedAction].NumberOfFrames)
							SelectedFrame = 0;
					}

					_sbFrameIndex.Dispatch(p => p.Value = SelectedFrame);

					Thread.Sleep(count == 0 ? 400 : 50);

					lock (_lock) {
						if (_pending > 0) {
							_pending--;
							return;
						}
					}

					count++;
				}
			});

			e.Handled = true;
		}

		private void _play_Click(object sender, RoutedEventArgs e) {
			_play.Dispatch(delegate {
				_play.IsPressed = !_play.IsPressed;
				_sbFrameIndex.IsEnabled = !_play.IsPressed;
				_updatePlay();
			});

			if (_play.Dispatch(() => _play.IsPressed)) {
				GrfThread.Start(_playAnimation);
			}
		}

		private void _playAnimation() {
			Act act = _act;

			if (act == null) {
				_play_Click(null, null);
				return;
			}

			if (act[SelectedAction].NumberOfFrames <= 1) {
				_play_Click(null, null);
				return;
			}

			if (act[SelectedAction].AnimationSpeed < 0.8f) {
				_play_Click(null, null);
				ErrorHandler.HandleException("The animation speed is too fast and might cause issues. The animation will not be displayed.", ErrorLevel.NotSpecified);
				return;
			}

			Stopwatch watch = new Stopwatch();
			SelectedFrame--;

			int interval = (int) (act[SelectedAction].AnimationSpeed * 25f);

			int intervalsToShow = 1;
			int intervalsToHide = 0;

			if (interval <= 50) {
				intervalsToShow = 1;
				intervalsToHide = 1;
			}

			if (interval <= 25) {
				intervalsToShow = 1;
				intervalsToHide = 2;
			}

			if (intervalsToShow + intervalsToHide == act[SelectedAction].NumberOfFrames) {
				intervalsToShow++;
			}

			int currentIntervalShown = -intervalsToHide;

			try {
				OnAnimationPlaying(2);

				while (_play.Dispatch(p => p.IsPressed)) {
					watch.Reset();
					watch.Start();

					interval = (int) (act[SelectedAction].AnimationSpeed * 25f);

					if (act[SelectedAction].AnimationSpeed < 0.8f) {
						_play_Click(null, null);
						ErrorHandler.HandleException("The animation speed is too fast and might cause issues. The animation will not be displayed.", ErrorLevel.NotSpecified);
						return;
					}

					SelectedFrame++;

					if (SelectedFrame >= act[SelectedAction].NumberOfFrames) {
						SelectedFrame = 0;
					}

					if (currentIntervalShown < 0) {
						_frameChangedEventEnabled = false;
						this.Dispatch(() => _sbFrameIndex.Value = SelectedFrame);
						_frameChangedEventEnabled = true;
					}
					else {
						this.Dispatch(() => _sbFrameIndex.Value = SelectedFrame);
					}

					currentIntervalShown++;

					if (currentIntervalShown >= intervalsToShow) {
						currentIntervalShown = -intervalsToHide;
					}

					if (!_play.Dispatch(p => p.IsPressed))
						return;

					watch.Stop();

					Thread.Sleep(interval);
				}
			}
			finally {
				_frameChangedEventEnabled = true;
				OnAnimationPlaying(0);
			}
		}

		private void _updatePlay() {
			if (_play.IsPressed) {
				_play.ImagePath = "stop2.png";
				_play.ImageIcon.Width = 16;
				_play.ImageIcon.Stretch = Stretch.Fill;
			}
			else {
				_play.ImagePath = "play.png";
				_play.ImageIcon.Width = 16;
				_play.ImageIcon.Stretch = Stretch.Fill;
			}
		}

		public void Init(FrameViewer viewer) {
			_viewer = viewer;
			ActionChanged += _frameSelector_ActionChanged;
			_sbFrameIndex.ValueChanged += _sbFrameIndex_ValueChanged;
			_tbFrameIndex.TextChanged += _tbFrameIndex_TextChanged;

			_sbFrameIndex.SmallChange = 1;
			_sbFrameIndex.LargeChange = 1;
		}

		private void _tbFrameIndex_TextChanged(object sender, TextChangedEventArgs e) {
			if (!_eventsEnabled) return;
			if (_act == null) return;

			int ival;

			Int32.TryParse(_tbFrameIndex.Text, out ival);

			if (ival > _act[SelectedAction].NumberOfFrames || ival < 0) {
				ival = 0;
			}

			_sbFrameIndex.Value = ival;
		}

		private void _sbFrameIndex_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			if (!_eventsEnabled) return;
			if (_act == null) return;

			int value = (int) Math.Round(_sbFrameIndex.Value);

			_eventsEnabled = false;
			_sbFrameIndex.Value = value;
			_tbFrameIndex.Text = value.ToString(CultureInfo.InvariantCulture);
			_eventsEnabled = true;
			SelectedFrame = value;
			OnFrameChanged(value);
		}

		private void _updateAction() {
			if (_act == null) return;
			if (SelectedAction >= _act.NumberOfActions) return;

			_eventsEnabled = false;

			while (SelectedFrame >= _act[SelectedAction].NumberOfFrames && SelectedFrame > 0) {
				SelectedFrame--;
			}

			_eventsEnabled = true;

			int max = _act[SelectedAction].NumberOfFrames - 1;
			max = max < 0 ? 0 : max;

			_sbFrameIndex.Minimum = 0;
			_sbFrameIndex.Maximum = max;

			_labelFrameIndex.Text = "/ " + max + " frame" + (max > 1 ? "s" : "");
		}

		private void _frameSelector_ActionChanged(object sender, int actionindex) {
			_updateAction();
			_sbFrameIndex.Value = 0;
		}

		public void Load(Act act) {
			_act = act;
			_fancyButtons.ForEach(p => p.IsPressed = false);

			int oldAction = _comboBoxActionIndex.SelectedIndex;
			int oldFrame = SelectedFrame;

			_comboBoxActionIndex.ItemsSource = null;
			_comboBoxActionIndex.Items.Clear();

			_comboBoxAnimationIndex.ItemsSource = null;
			_comboBoxAnimationIndex.Items.Clear();

			_eventsEnabled = false;
			_eventsEnabled = true;

			if (_act != null) {
				int actions = _act.NumberOfActions;

				_comboBoxAnimationIndex.ItemsSource = _act.GetAnimationStrings();
				_comboBoxActionIndex.ItemsSource = Enumerable.Range(0, actions);

				if (actions != 0) {
					_comboBoxActionIndex.SelectedIndex = 0;
				}

				_act.VisualInvalidated += s => Update();
				_act.Commands.CommandIndexChanged += new AbstractCommand<IActCommand>.AbstractCommandsEventHandler(_commands_CommandUndo);

				if (oldAction < _act.NumberOfActions) {
					_comboBoxActionIndex.SelectedIndex = oldAction;
				}

				if (_comboBoxActionIndex.SelectedIndex > 0 && _comboBoxActionIndex.SelectedIndex < _act.NumberOfActions) {
					if (_act.TryGetFrame(_comboBoxActionIndex.SelectedIndex, oldFrame) != null) {
						_tbFrameIndex.Text = oldFrame.ToString(CultureInfo.InvariantCulture);
					}
				}
			}

			if (_comboBoxActionIndex.Items.Count > 0 && _comboBoxActionIndex.SelectedIndex < 0) {
				_comboBoxActionIndex.SelectedIndex = 0;
			}
		}

		private void _commands_CommandUndo(object sender, IActCommand command) {
			this.Dispatch(delegate {
				try {
					var actionCmd = _getCommand<ActionCommand>(command);

					if (actionCmd != null) {
						if (actionCmd.Executed &&
						    (actionCmd.Edit == ActionCommand.ActionEdit.CopyAt ||
						     actionCmd.Edit == ActionCommand.ActionEdit.InsertAt ||
						     actionCmd.Edit == ActionCommand.ActionEdit.ReplaceTo ||
						     actionCmd.Edit == ActionCommand.ActionEdit.InsertAt)) {
							SelectedAction = actionCmd.ActionIndexTo;
						}

						if (SelectedAction < 0)
							SelectedAction = 0;

						if (SelectedAction >= _act.NumberOfActions)
							SelectedAction = _act.NumberOfActions - 1;

						_updateActionSelection();
					}

					var frameCmd = _getCommand<FrameCommand>(command);

					if (frameCmd != null) {
						if (frameCmd.Executed) {
							if ((frameCmd.ActionIndexTo == SelectedAction && frameCmd.Edit == FrameCommand.FrameEdit.ReplaceTo) ||
							    (frameCmd.ActionIndexTo == SelectedAction && frameCmd.Edit == FrameCommand.FrameEdit.Switch) ||
							    (frameCmd.ActionIndexTo == SelectedAction && frameCmd.Edit == FrameCommand.FrameEdit.CopyTo)
								) {
								SelectedFrame = frameCmd.FrameIndexTo;
							}
							else if (frameCmd.ActionIndex == SelectedAction && frameCmd.Edit == FrameCommand.FrameEdit.InsertTo) {
								SelectedFrame = frameCmd.FrameIndex;
							}

							if (SelectedFrame != (int) _sbFrameIndex.Value) {
								_sbFrameIndex.Value = SelectedFrame;
							}
						}
					}

					//_updateInfo();
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			});
		}

		private T _getCommand<T>(IActCommand command) where T : class, IActCommand {
			var cmd = command as ActGroupCommand;

			if (cmd != null) {
				return cmd.Commands.FirstOrDefault(p => p.GetType() == typeof (T)) as T;
			}

			if (command is T) {
				return command as T;
			}

			return null;
		}

		private void _updateActionSelection() {
			try {
				int selectedAction = SelectedAction;

				_comboBoxAnimationIndex.ItemsSource = null;
				_comboBoxAnimationIndex.ItemsSource = _act.GetAnimationStrings();
				_comboBoxActionIndex.ItemsSource = null;
				_comboBoxActionIndex.ItemsSource = Enumerable.Range(0, _act.NumberOfActions);

				if (selectedAction >= _comboBoxActionIndex.Items.Count) {
					_comboBoxActionIndex.SelectedIndex = _comboBoxActionIndex.Items.Count - 1;
				}

				_comboBoxActionIndex.SelectedIndex = selectedAction;

				//Call update?
				Update();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _updateInfo() {
			_play.IsPressed = false;
			_updatePlay();
			_updateAction();
		}

		private void _fancyButton_Click(object sender, RoutedEventArgs e) {
			int animationIndex = _comboBoxActionIndex.SelectedIndex / 8;

			_fancyButtons.ForEach(p => p.IsPressed = false);
			((FancyButton) sender).IsPressed = true;

			_comboBoxActionIndex.SelectedIndex = animationIndex * 8 + Int32.Parse(((FancyButton) sender).Tag.ToString());
		}

		private void _setDisabledButtons() {
			Dispatcher.Invoke(new Action(delegate {
				int animationIndex = _comboBoxActionIndex.SelectedIndex / 8;

				if ((animationIndex + 1) * 8 > _act.NumberOfActions) {
					_fancyButtons.ForEach(p => p.IsButtonEnabled = true);

					int toDisable = (animationIndex + 1) * 8 - _act.NumberOfActions;

					for (int i = 0; i < toDisable; i++) {
						int disabledIndex = 7 - i;
						_fancyButtons.First(p => Int32.Parse(p.Tag.ToString()) == disabledIndex).IsButtonEnabled = false;
					}
				}
				else {
					_fancyButtons.ForEach(p => p.IsButtonEnabled = true);
				}
			}));
		}

		private void _comboBoxAnimationIndex_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_comboBoxAnimationIndex.SelectedIndex < 0) return;

			int direction = _comboBoxActionIndex.SelectedIndex % 8;

			if (8 * _comboBoxAnimationIndex.SelectedIndex + direction >= _act.NumberOfActions) {
				_comboBoxActionIndex.SelectedIndex = 8 * _comboBoxAnimationIndex.SelectedIndex;
			}
			else {
				_comboBoxActionIndex.SelectedIndex = 8 * _comboBoxAnimationIndex.SelectedIndex + direction;
			}
		}

		private void _comboBoxActionIndex_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_comboBoxActionIndex.SelectedIndex < 0) return;
			if (_comboBoxActionIndex.SelectedIndex >= _act.NumberOfActions) return;

			int actionIndex = _comboBoxActionIndex.SelectedIndex;
			int animationIndex = actionIndex / 8;
			_disableEvents();
			_comboBoxAnimationIndex.SelectedIndex = animationIndex;
			_fancyButton_Click(_fancyButtons.First(p => p.Tag.ToString() == (actionIndex % 8).ToString(CultureInfo.InvariantCulture)), null);
			_setDisabledButtons();
			SelectedAction = _comboBoxActionIndex.SelectedIndex;
			SelectedFrame = 0;
			OnActionChanged(SelectedAction);
			_enableEvents();
		}

		public void Update() {
			try {
				int oldFrame = SelectedFrame;
				bool differedUpdate = oldFrame != 0;

				if (differedUpdate) {
					_handlersEnabled = false;
				}

				_comboBoxActionIndex_SelectionChanged(null, null);

				if (_act == null) return;

				if (SelectedAction >= 0 && SelectedAction < _act.NumberOfActions) {
					if (oldFrame < _act[SelectedAction].NumberOfFrames) {
						if (differedUpdate) {
							_handlersEnabled = true;
							SelectedFrame = oldFrame;

							if (_sbFrameIndex.Value == oldFrame) {
								OnFrameChanged(oldFrame);
							}
							else {
								_sbFrameIndex.Value = oldFrame;
							}
						}
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _disableEvents() {
			_comboBoxAnimationIndex.SelectionChanged -= _comboBoxAnimationIndex_SelectionChanged;
			_fancyButtons.ForEach(p => p.Click -= _fancyButton_Click);
		}

		private void _enableEvents() {
			_comboBoxAnimationIndex.SelectionChanged += _comboBoxAnimationIndex_SelectionChanged;
			_fancyButtons.ForEach(p => p.Click += _fancyButton_Click);
		}

		public void Reset() {
			_eventsEnabled = false;

			_fancyButtons.ForEach(p => p.IsPressed = false);
			_fancyButtons.ForEach(p => p.IsButtonEnabled = false);

			_comboBoxActionIndex.ItemsSource = null;
			_comboBoxActionIndex.Items.Clear();

			_comboBoxAnimationIndex.ItemsSource = null;
			_comboBoxAnimationIndex.Items.Clear();

			_sbFrameIndex.Value = 0;
			_tbFrameIndex.Text = "0";
			_labelFrameIndex.Text = "/ 0 frame";
			_sbFrameIndex.Maximum = 0;

			_play.IsPressed = false;
			_updatePlay();

			_eventsEnabled = true;
		}

		private void _buttonRenderMode_Click(object sender, RoutedEventArgs e) {
			SdeAppConfiguration.ActEditorScalingMode = SdeAppConfiguration.ActEditorScalingMode == BitmapScalingMode.NearestNeighbor ? BitmapScalingMode.Fant : BitmapScalingMode.NearestNeighbor;
			_updateRendering();
			OnFrameChanged(SelectedFrame);
		}

		private void _updateRendering() {
			bool isEditor = SdeAppConfiguration.ActEditorScalingMode == BitmapScalingMode.NearestNeighbor;

			_buttonRenderMode.ImagePath = isEditor ? "editor.png" : "ingame.png";
			_buttonRenderMode.IsPressed = !isEditor;

			_buttonRenderMode.ToolTip = isEditor ? "Render mode is currently set to \"Editor\"." : "Render mode is currently set to \"Ingame\".";
		}

		private void _buttonSettings_Click(object sender, RoutedEventArgs e) {
			WindowProvider.Show(new PreviewSettingsDialog(() => OnFrameChanged(SelectedFrame), c => _viewer._primary.Background = new SolidColorBrush(c)), _buttonSettings, WpfUtilities.FindDirectParentControl<Window>(this));
		}

		private void _buttonExport_Click(object sender, RoutedEventArgs e) {
			try {

				var tuple = ViewIdPreviewDialog.LatestTupe;

				if (tuple == null)
					return;

				var sprite = LuaHelper.GetSpriteFromViewId(tuple.GetIntNoThrow(ServerItemAttributes.ClassNumber), LuaHelper.ViewIdTypes.Headgear, SdeEditor.Instance.ProjectDatabase.GetDb<int>(ServerDbs.Items), tuple);

				string[] files = new string[] {
					@"data\sprite\¾ÆÀÌÅÛ\" + sprite + ".spr",
					@"data\sprite\¾ÆÀÌÅÛ\" + sprite + ".act",
					@"data\sprite\¾Ç¼¼»ç¸®\³²\³²_" + sprite + ".spr",
					@"data\sprite\¾Ç¼¼»ç¸®\³²\³²_" + sprite + ".act",
					@"data\sprite\¾Ç¼¼»ç¸®\¿©\¿©_" + sprite + ".spr",
					@"data\sprite\¾Ç¼¼»ç¸®\¿©\¿©_" + sprite + ".act",
					@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\collection\" + sprite + ".bmp",
					@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\item\" + sprite + ".bmp"
				};

				string path = PathRequest.FolderEditor();

				if (path == null)
					return;

				var grf = SdeEditor.Instance.ProjectDatabase.MetaGrf;

				foreach (var file in files) {
					var data = grf.GetData(file);

					if (data != null) {
						string subPath = GrfPath.Combine(path, file);
						GrfPath.CreateDirectoryFromFile(subPath);
						File.WriteAllBytes(subPath, data);
					}
				}

				OpeningService.OpenFolder(path);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}