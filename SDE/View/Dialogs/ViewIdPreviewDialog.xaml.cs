using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Database;
using ErrorManager;
using SDE.Core;
using SDE.Editor.Engines.PreviewEngine;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class ViewIdPreviewDialog : TkWindow {
		public static bool IsOpened = false;
		private readonly SdeEditor _editor;
		private GDbTab _tab;
		private Database.Tuple _lastTuple;
		private readonly PreviewHelper _helper;

		public ViewIdPreviewDialog(SdeEditor editor, GDbTab tab) : base("View ID preview", "eye.png", SizeToContent.Manual, ResizeMode.CanResize) {
			_tab = tab;
			_editor = editor;
			_editor._mainTabControl.SelectionChanged += _mainTabControl_SelectionChanged;

			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			Owner = WpfUtilities.TopWindow;
			WindowStyle = WindowStyle.ToolWindow;

			_helper = new PreviewHelper(_listView, _tab.DbComponent.To<int>(), _selector, _frameViewer, _gridSpriteMissing, _tbSpriteMissing);
			
			this.Loaded += delegate {
				Width = 400;
				Height = 300;
				IsOpened = true;
			};

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_listView, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.RangeColumnInfo {Header = "Job Name", DisplayExpression = "Name", SearchGetAccessor = "Name", IsFill = true, ToolTipBinding = "Name", TextWrapping = TextWrapping.Wrap}
			}, null, new string[] { "Normal", "{DynamicResource TextForeground}" });

			_tupleUpdate();
		}

		public static ReadableTuple<int> LatestTupe { get; set; }

		private void _tupleUpdate(bool bypass = false) {
			try {
				if ((_tab.DbComponent.DbSource & ServerDbs.ServerItems) != 0) {
					var tuple = _tab._listView.SelectedItem as ReadableTuple<int>;

					ViewIdPreviewDialog.LatestTupe = tuple;

					if (tuple == null) return;
					if (!bypass) {
						if (tuple == _lastTuple) return;

						if (_lastTuple != null) {
							_lastTuple.TupleModified -= _tupleUpdate;
						}

						_lastTuple = tuple;
						_lastTuple.TupleModified += _tupleUpdate;
					}

					_helper.Read(tuple, _tab);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _tupleUpdate(object sender, bool value) {
			_tupleUpdate(true);
		}
		private void _mainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				if (_editor._mainTabControl.SelectedIndex >= 0 && _editor._mainTabControl.Items[_editor._mainTabControl.SelectedIndex] is GDbTab) {
					_tab = (GDbTab)_editor._mainTabControl.Items[_editor._mainTabControl.SelectedIndex];
					_tupleUpdate();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e) {
			_editor._mainTabControl.SelectionChanged -= _mainTabControl_SelectionChanged;
			if (_lastTuple != null) {
				_lastTuple.TupleModified -= _tupleUpdate;
			}
			base.OnClosing(e);
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}
	}
}
