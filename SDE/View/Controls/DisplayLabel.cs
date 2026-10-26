using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using SDE.Databases;
using SDE.Editor.Database;
using TokeiLibrary;
using Utilities;

namespace SDE.View.Controls {
	public enum DisplayLabelState {
		Normal,
		MouseOver,
		Selected,
		Disabled
	}

	public class DisplayLabel : TextBlock {
		public static DependencyProperty DisplayTextProperty = DependencyProperty.Register("DisplayText", typeof(string), typeof(DisplayLabel), new PropertyMetadata(new PropertyChangedCallback(OnDisplayTextChanged)));
		private readonly BaseDatabase _db;
		private readonly DataSource _source;
		private bool _isLoaded;

		private string _toString;
		private TabItem _tabItem;

		public bool IsSelected {
			get => (bool)GetValue(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.Register(
				nameof(IsSelected),
				typeof(bool),
				typeof(DisplayLabel),
				new PropertyMetadata(false, OnVisualStateChanged));

		public bool IsContentEnabled {
			get => (bool)GetValue(IsContentEnabledProperty);
			set => SetValue(IsContentEnabledProperty, value);
		}

		public static readonly DependencyProperty IsContentEnabledProperty =
			DependencyProperty.Register(
				nameof(IsContentEnabled),
				typeof(bool),
				typeof(DisplayLabel),
				new PropertyMetadata(true, OnVisualStateChanged));

		protected override void OnMouseLeave(MouseEventArgs e) {
			base.OnMouseLeave(e);
			UpdateForeground();
		}

		protected override void OnMouseEnter(MouseEventArgs e) {
			base.OnMouseEnter(e);
			UpdateToolTip();
			UpdateForeground();
		}

		private static void OnVisualStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			((DisplayLabel)d).UpdateForeground();
		}

		private void UpdateToolTip() {
			if (_source != null) {
				ToolTip = DbPathLocator.DetectPath(_source);
			}

			if (ToolTip == null) {
				ToolTip = "File not found. This database will be disabled.";
			}
		}

		public DisplayLabel() {
			FocusVisualStyle = null;
			Padding = new Thickness(5, 3, 5, 3);
			VerticalAlignment = VerticalAlignment.Center;
			FontSize = 12;

			ApplicationManager.ThemeChanged += delegate {
				UpdateForeground();
			};

			SetBinding(DisplayLabel.IsSelectedProperty, new Binding(nameof(IsSelected)) {
				RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TabItem), 1)
			});

			SizeChanged += delegate {
				if (!_isLoaded) {
					_tabItem = WpfUtilities.FindParentControl<TabItem>(this);
					UpdateForeground();
					_isLoaded = true;
				}
			};
		}

		public DisplayLabel(DataSource source, BaseDatabase db) : this() {
			_source = source;
			_db = db;
			_toString = source.UidName;
			Text = source.IsImport ? "imp" : source.DisplayName;
			
			if (_db != null) {
				_db.IsEnabledChanged += (e, v) => {
					this.Dispatch(p => p.IsContentEnabled = v);
				};
			}

			bool isSet = false;

			this.Loaded += delegate {
				if (isSet)
					return;

				isSet = true;

				if (_source.IsImport) {
					Grid presenter = WpfUtilities.FindParentControl<Grid>(this);

					var templateGrid = presenter.Parent as Grid;
					templateGrid.Margin = new Thickness(0, -presenter.ActualHeight, 0, 0);
				}
			};
		}

		public string DisplayText {
			get { return (string)GetValue(DisplayTextProperty); }
			set { SetValue(DisplayTextProperty, value); }
		}

		public void ResetEnabled() {
			IsContentEnabled = true;
		}

		public static void OnDisplayTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			DisplayLabel label = d as DisplayLabel;

			if (label != null) {
				if (label._source != null)
					label.Text = label._source.IsImport ? "import" : e.NewValue.ToString();
				else
					label.Text = e.NewValue.ToString();

				label._toString = e.NewValue.ToString();
			}
		}

		public void UpdateForeground() {
			var tabItem = WpfUtilities.FindParentControl<TabItem>(this);
			Brush brush;

			if (!IsContentEnabled) {
				brush = Brushes.Red;
			}
			else if (_tabItem != null && _tabItem.IsSelected) {
				brush = (Brush)FindResource("TabItemTextSelectedForeground");
			}
			else if (IsMouseOver) {
				brush = (Brush)FindResource("TabItemTextNormalForeground");
			}
			else {
				brush = (Brush)FindResource("TabItemTextNormalForeground");
			}

			Foreground = brush;
		}

		public override string ToString() {
			return _toString;
		}
	}
}