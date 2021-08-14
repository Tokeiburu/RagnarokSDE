using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using GRF.Core;
using GRF.IO;
using GRF.Threading;
using GrfToWpfBridge;
using GrfToWpfBridge.Application;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.RepairEngine;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.View.ObjectView;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using GDbTab = SDE.Editor.Generic.TabsMakerCore.GDbTab;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class ValidationDialog : TkWindow {
		private readonly string[] _advancedView = new string[] { "List view", "Show the list view" };
		private readonly string[] _rawView = new string[] { "Raw view", "Show the raw text view" };
		private List<ValidationErrorView> _errors;
		private AsyncOperation _asyncOperation;
		private SdeDatabase _sdb;
		private DbValidationEngine _validation;

		public ValidationDialog(GDbTab tab) : base("Table validation", "validity.png", SizeToContent.Manual, ResizeMode.CanResize) {
			//_editor = editor;

			InitializeComponent();

			_asyncOperation = new AsyncOperation(_progressBar);

			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			Owner = WpfUtilities.TopWindow;

			_loadSettingsUI();

			_sdb = tab.ProjectDatabase;
			_validation = new DbValidationEngine(_sdb);

			_addSelectAll(_findErrors, _cbResSelectAll);
			_addSelectAll(_scanClientItems, _cbCiSelectAll);

			Binder.Bind(_cbResCollection, () => SdeAppConfiguration.VaResCollection);
			Binder.Bind(_cbResDrag, () => SdeAppConfiguration.VaResDrag);
			Binder.Bind(_cbResExistingOnly, () => SdeAppConfiguration.VaResExistingOnly);
			Binder.Bind(_cbResHeadger, () => SdeAppConfiguration.VaResHeadgear);
			Binder.Bind(_cbResIllust, () => SdeAppConfiguration.VaResIllustration);
			Binder.Bind(_cbResItem, () => SdeAppConfiguration.VaResInventory);
			Binder.Bind(_cbResMonster, () => SdeAppConfiguration.VaResMonster);
			Binder.Bind(_cbResNpc, () => SdeAppConfiguration.VaResNpc);
			Binder.Bind(_cbResShield, () => SdeAppConfiguration.VaResShield);
			Binder.Bind(_cbResWeapon, () => SdeAppConfiguration.VaResWeapon);
			Binder.Bind(_cbResGarment, () => SdeAppConfiguration.VaResGarment);
			Binder.Bind(_cbResInvalidFormat, () => SdeAppConfiguration.VaResInvalidFormat);
			Binder.Bind(_cbResEmpty, () => SdeAppConfiguration.VaResEmpty);
			Binder.Bind(_cbResInvalidCharacters, () => SdeAppConfiguration.VaResInvalidCharacters);
			Binder.Bind(_cbResClientMissing, () => SdeAppConfiguration.VaResClientItemMissing);
			
			Binder.Bind(_cbCiClassNumber, () => SdeAppConfiguration.VaCiViewId);
			Binder.Bind(_cbCiDescription, () => SdeAppConfiguration.VaCiDescription);
			Binder.Bind(_cbCiItemType, () => SdeAppConfiguration.VaCiItemRange);
			Binder.Bind(_cbCiClass, () => SdeAppConfiguration.VaCiClass);
			Binder.Bind(_cbCiAttack, () => SdeAppConfiguration.VaCiAttack);
			Binder.Bind(_cbCiDefense, () => SdeAppConfiguration.VaCiDefense);
			Binder.Bind(_cbCiProperty, () => SdeAppConfiguration.VaCiProperty);
			Binder.Bind(_cbCiRequiredLevel, () => SdeAppConfiguration.VaCiRequiredLevel);
			Binder.Bind(_cbCiWeaponLevel, () => SdeAppConfiguration.VaCiWeaponLevel);
			Binder.Bind(_cbCiWeight, () => SdeAppConfiguration.VaCiWeight);
			Binder.Bind(_cbCiLocation, () => SdeAppConfiguration.VaCiLocation);
			Binder.Bind(_cbCiCompoundOn, () => SdeAppConfiguration.VaCiCompoundOn);
			Binder.Bind(_cbCiNumberOfSlots, () => SdeAppConfiguration.VaCiNumberOfSlots);
			Binder.Bind(_cbCiIsCard, () => SdeAppConfiguration.VaCiIsCard);
			Binder.Bind(_cbCiName, () => SdeAppConfiguration.VaCiName);
			Binder.Bind(_cbCiJob, () => SdeAppConfiguration.VaCiJob);
			
			_listViewResults.MouseDoubleClick += (s, e) => _select();
			_listViewResults.KeyUp += (s, e) => {
				if (e.Key == Key.Enter)
					_select();
			};
		}

		private void _addSelectAll(Grid grid, CheckBox selectAll) {
			List<CheckBox> boxes = DisplayablePropertyHelper.FindAll<CheckBox>(grid);
			boxes.ForEach(WpfUtils.AddMouseInOutEffectsBox);
			bool eventsActive = true;
			WpfUtils.AddMouseInOutEffectsBox(selectAll);

			foreach (var box in boxes) {
				box.Checked += delegate {
					if (eventsActive)
						_checkAllSelected(boxes, selectAll, true);
				};

				box.Unchecked += delegate {
					if (eventsActive)
						_checkAllSelected(boxes, selectAll, false);
				};
			}

			selectAll.Checked += delegate {
				eventsActive = false;

				foreach (var box in boxes) {
					if (box.IsChecked == false) {
						box.IsChecked = true;
					}
				}

				eventsActive = true;
			};

			selectAll.Unchecked += delegate {
				eventsActive = false;

				foreach (var box in boxes) {
					if (box.IsChecked == true) {
						box.IsChecked = false;
					}
				}

				eventsActive = true;
			};
		}

		private void _checkAllSelected(List<CheckBox> boxes, CheckBox selectAll, bool state) {
			bool current = true;

			foreach (var box in boxes) {
				if (box.IsChecked != state) {
					current = false;
					break;
				}
			}

			if (current) {
				selectAll.IsChecked = state;
			}
		}

		private void _select() {
			var item = _listViewResults.SelectedItem as ValidationErrorView;

			if (item != null) {
				var commands = new HashSet<CmdInfo>();
				item.GetCommands(commands);
				commands.First().Execute(item, _listViewResults.SelectedItems.OfType<ValidationErrorView>().ToList());
			}
		}

		private void _loadSettingsUI() {
			_changeRawViewButton();

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_listViewResults, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "Error", FixedWidth = 20, MaxHeight = 24 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Error", DisplayExpression = "ErrorString", FixedWidth = 120, ToolTipBinding = "ErrorString", TextAlignment = TextAlignment.Left },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Id", DisplayExpression = "Id", FixedWidth = 50, ToolTipBinding = "Id", TextAlignment = TextAlignment.Right },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Db", DisplayExpression = "Db", FixedWidth = 100, ToolTipBinding = "Db", TextAlignment = TextAlignment.Left },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "Message", DisplayExpression = "Message", MinWidth = 150, ToolTipBinding = "Message", TextAlignment = TextAlignment.Left, IsFill = true, TextWrapping = TextWrapping.Wrap },
			}, new DefaultListViewComparer<ValidationErrorView>(), new string[] { "Default", "{DynamicResource TextForeground}" });

			_errors = new List<ValidationErrorView>();
			_listViewResults.ItemsSource = _errors;
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _buttonFindErrors_Click(object sender, RoutedEventArgs e) {
			List<ValidationErrorView> errors = new List<ValidationErrorView>();
			_asyncOperation.SetAndRunOperation(new GrfThread(() => _validation.FindResourceErrors(errors), _validation, 200, errors, true, true), _updateErrors);
		}

		private void _updateErrors(object state) {
			try {
				_lbResults.Dispatch(p => p.Content = "Results");

				var errors = (List<ValidationErrorView>)state;
				errors = errors.Where(p => p != null).ToList();
				
				StringBuilder builder = new StringBuilder();

				for (int index = 0; index < errors.Count; index++) {
					builder.Append(errors[index] + "\r\n");
				}

				_tbResults.Dispatch(p => p.Text = builder.ToString());
				_listViewResults.Dispatch(p => p.ItemsSource = errors);
				_tabControl.Dispatch(p => p.SelectedItem = _tabItemResults);
				_lbResults.Dispatch(p => p.Content = "Results (found " + errors.Count + String.Format(" error{0})", errors.Count == 1 ? "" : "s"));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonRawView_Click(object sender, RoutedEventArgs e) {
			SdeAppConfiguration.ValidationRawView = !SdeAppConfiguration.ValidationRawView;
			_changeRawViewButton();
		}

		private void _changeRawViewButton() {
			if (SdeAppConfiguration.ValidationRawView) {
				_buttonRawView.Dispatch(p => p.TextHeader = _advancedView[0]);
				_buttonRawView.Dispatch(p => p.TextDescription = _advancedView[1]);
				_listViewResults.Visibility = Visibility.Hidden;
				_tbResults.Visibility = Visibility.Visible;
			}
			else {
				_buttonRawView.Dispatch(p => p.TextHeader = _rawView[0]);
				_buttonRawView.Dispatch(p => p.TextDescription = _rawView[1]);
				_listViewResults.Visibility = Visibility.Visible;
				_tbResults.Visibility = Visibility.Hidden;
			}
		}

		private void _listViewResults_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			var item = _listViewResults.GetObjectAtPoint<ListViewItem>(e.GetPosition(_listViewResults));

			if (item != null) {
				HashSet<CmdInfo> commands = new HashSet<CmdInfo>();

				foreach (ValidationErrorView error in _listViewResults.SelectedItems) {
					error.GetCommands(commands);
				}

				ContextMenu menu = new ContextMenu();

				foreach (var cmd in commands) {
					var lcmd = cmd;

					MenuItem mitem = new MenuItem { Header = cmd.DisplayName, Icon = new Image { Source = ApplicationManager.GetResourceImage(cmd.Icon) } };
					mitem.Click += delegate {
						var items = _listViewResults.SelectedItems.Cast<ValidationErrorView>().ToList();

						_asyncOperation.SetAndRunOperation(new GrfThread(delegate {
							List<object> dbs = new List<object>();

							foreach (var serverDb in ServerDbs.ListDbs) {
								var db = _sdb.TryGetDb(serverDb);

								if (db != null) {
									if (db.AttributeList.PrimaryAttribute.DataType == typeof(int)) {
										var adb = (AbstractDb<int>)db;
										dbs.Add(adb);
									}
									else if (db.AttributeList.PrimaryAttribute.DataType == typeof(string)) {
										var adb = (AbstractDb<string>)db;
										dbs.Add(adb);
									}
								}
							}

							foreach (var db in dbs) {
								_to<int>(db, _onBegin);
								_to<string>(db, _onBegin);
							}

							try {
								AProgress.Init(_validation);

								_validation.Grf.Close();
								_validation.Grf.Open(GrfPath.Combine(SdeAppConfiguration.ProgramDataPath, "missing_resources.grf"), GrfLoadOptions.OpenOrNew);

								for (int i = 0; i < items.Count; i++) {
									AProgress.IsCancelling(_validation);
									if (!lcmd.Execute(items[i], items))
										return;
									_validation.Progress = (float)i / items.Count * 100f;
								}

								if (_validation.Grf.IsModified) {
									_validation.Progress = -1;
									_validation.Grf.QuickSave();
									_validation.Grf.Reload();
									_validation.Grf.Compact();
								}
							}
							catch (OperationCanceledException) {
							}
							catch (Exception err) {
								ErrorHandler.HandleException(err);
							}
							finally {
								foreach (var db in dbs) {
									_to<int>(db, _onEnd);
									_to<string>(db, _onEnd);
								}

								_validation.Grf.Close();
								AProgress.Finalize(_validation);
							}
						}, _validation, 200, null, true, true));
					};

					menu.Items.Add(mitem);
				}

				item.ContextMenu = menu;
				item.ContextMenu.IsOpen = true;
			}
			else {
				e.Handled = true;
			}
		}

		private void _to<T>(object db, Action<AbstractDb<T>> func) {
			if (db is AbstractDb<T>)
				func(((AbstractDb<T>)db));
		}

		private void _onBegin<T>(AbstractDb<T> p) {
			p.Table.Commands.BeginNoDelay();
		}

		private void _onEnd<T>(AbstractDb<T> p) {
			p.Table.Commands.End();
		}

		private void _buttonScanClientItems_Click(object sender, RoutedEventArgs e) {
			List<ValidationErrorView> errors = new List<ValidationErrorView>();
			_asyncOperation.SetAndRunOperation(new GrfThread(() => _validation.FindClientItemErrors(errors), _validation, 200, errors, true, true), _updateErrors);
		}

		private void _buttonTableErrors_Click(object sender, RoutedEventArgs e) {
			List<ValidationErrorView> errors = new List<ValidationErrorView>();
			_asyncOperation.SetAndRunOperation(new GrfThread(() => _validation.FindTableErrors(errors), _validation, 200, errors, true, true), _updateErrors);
		}
	}
}
