using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SDE.Tools.DatabaseEditor.Generic.Core;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using TokeiLibrary;

namespace SDE.Tools.DatabaseEditor.WPF {
	public class DisplayLabel : Label {
		public static DependencyProperty DisplayTextProperty = DependencyProperty.Register("DisplayText", typeof(string), typeof(DisplayLabel), new PropertyMetadata(new PropertyChangedCallback(OnDisplayTextChanged)));
		private readonly BaseDb _db;
		private readonly ServerDbs _dbSource;
		private bool _isLoaded;

		private Brush _stateBrush = Brushes.Black;
		private Brush _stateInactiveBrush = new SolidColorBrush(Color.FromArgb(255, 98, 98, 98));
		private string _toString;

		public DisplayLabel() {
			FocusVisualStyle = null;
			Margin = new Thickness(-4, -4, 0, -4);
			Padding = new Thickness(0);

			FontSize = 12;

			MouseEnter += delegate {
				if (_dbSource != null)
					ToolTip = AllLoaders.DetectPath(_dbSource);

				if (ToolTip == null) {
					ToolTip = "File not found. This database will be disabled.";
				}
			};

			SizeChanged += delegate {
				if (!_isLoaded) {
					Grid presenter = WpfUtilities.FindParentControl<Grid>(this);
					TextBox box = (TextBox)presenter.Children[2];

					if (box.Text == "Visible") {
						Foreground = _stateBrush;
					}
					else {
						Foreground = _stateInactiveBrush;
					}

					box.TextChanged += delegate {
						if (box.Text == "Visible") {
							Foreground = _stateBrush;
						}
						else {
							Foreground = _stateInactiveBrush;
						}
					};

					_isLoaded = true;
				}
			};
		}

		public DisplayLabel(ServerDbs dbSource, BaseDb db) : this() {
			_dbSource = dbSource;
			_db = db;
			_toString = dbSource.Filename;
			Content = dbSource.DisplayName;

			if (_db != null) {
				_db.Attached.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e) {
					if (e.NewItems != null && e.NewItems.Count > 0 && e.Action == NotifyCollectionChangedAction.Replace || e.Action == NotifyCollectionChangedAction.Add) {
						if (e.NewItems[0] is KeyValuePair<string, object>) {
							var newItem = (KeyValuePair<string, object>) e.NewItems[0];

							if (newItem.Key == "IsEnabled") {
								if (_db.Attached["IsEnabled"] == null || (bool) _db.Attached["IsEnabled"]) {
									_stateBrush = Brushes.Black;
									_stateInactiveBrush = new SolidColorBrush(Color.FromArgb(255, 98, 98, 98));
								}
								else {
									_stateBrush = Brushes.Red;
									_stateInactiveBrush = Brushes.Red;
								}

								Grid presenter = WpfUtilities.FindParentControl<Grid>(this);
								TextBox box = (TextBox)presenter.Children[2];

								if (box.Text == "Visible") {
									Foreground = _stateBrush;
								}
								else {
									Foreground = _stateInactiveBrush;
								}
							}
						}
					}
				};
			}
		}

		public string DisplayText {
			get { return (string)GetValue(DisplayTextProperty); }
			set { SetValue(DisplayTextProperty, value); }
		}

		public void ResetEnabled() {
			_stateBrush = Brushes.Black;
			_stateInactiveBrush = new SolidColorBrush(Color.FromArgb(255, 98, 98, 98));

			Grid presenter = WpfUtilities.FindParentControl<Grid>(this);

			if (presenter == null)
				return;

			if (!(presenter.Children[2] is TextBox))
				return;

			TextBox box = (TextBox)presenter.Children[2];
			if (box.Text == "Visible") {
				Foreground = _stateBrush;
			}
			else {
				Foreground = _stateInactiveBrush;
			}
		}

		public static void OnDisplayTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			DisplayLabel label = d as DisplayLabel;

			if (label != null) {
				label.Content = e.NewValue;
				label._toString = e.NewValue.ToString();
			}
		}

		public override string ToString() {
			return _toString;
		}
	}
}