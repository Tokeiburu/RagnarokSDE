using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ErrorManager;
using GRF.IO;
using SDE.ApplicationConfiguration;
using SDE.Editor;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.Editor.Items;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Extension;
using Utilities.Services;
using Binder = GrfToWpfBridge.Binder;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class SettingsDialog : TkWindow {
		public SettingsDialog() : base("Advanced settings", "settings.png") {
			InitializeComponent();
			int row;

			Binder.Bind(_pbNotepad.TextBox, () => SdeAppConfiguration.NotepadPath);

			_add(_gridComments, row = 0, "Add comments in item_trade.txt", "Displays the item name as a comment at the end of the line for item_trade.txt.", () => SdeAppConfiguration.AddCommentForItemTrade, v => SdeAppConfiguration.AddCommentForItemTrade = v);
			_add(_gridComments, ++row, "Add comments in item_avail.txt", "Displays the item names as a comment at the end of the line for item_avail.txt.", () => SdeAppConfiguration.AddCommentForItemAvail, v => SdeAppConfiguration.AddCommentForItemAvail = v);
			_add(_gridComments, ++row, "Add comments in item_nouse.txt", "Displays the item names as a comment at the end of the line for item_nouse.txt.", () => SdeAppConfiguration.AddCommentForItemNoUse, v => SdeAppConfiguration.AddCommentForItemNoUse = v);

			_add(_gridGeneral, row = 4, "Switch item types 4 and 5 for text based databases (requires a software restart)", "Switches the armor and weapon types when reading the databases.", () => SdeAppConfiguration.RevertItemTypes, v => SdeAppConfiguration.RevertItemTypes = v);
			_add(_gridGeneral, ++row, "Always reopen the latest opened project", "Always reopen the most recently opened project when starting the application.", () => SdeAppConfiguration.AlwaysReopenLatestProject, v => SdeAppConfiguration.AlwaysReopenLatestProject = v);
			_add(_gridGeneral, ++row, "Associate the .sde file extension with this tool", null, () => (SdeAppConfiguration.FileShellAssociated & FileAssociation.Sde) == FileAssociation.Sde, v => {
				if (v) {
					SdeAppConfiguration.FileShellAssociated |= FileAssociation.Sde;
					ApplicationManager.AddExtension(Methods.ApplicationFullPath, "Server database editor", ".sde", true);
				}
				else {
					SdeAppConfiguration.FileShellAssociated &= ~FileAssociation.Sde;
					GrfPath.Delete(GrfPath.Combine(SdeAppConfiguration.ProgramDataPath, "sde.ico"));
					ApplicationManager.RemoveExtension(Methods.ApplicationFullPath, ".sde");
				}
			});
			_add(_gridGeneral, ++row, "Enable backups manager", null, () => SdeAppConfiguration.BackupsManagerState, v => SdeAppConfiguration.BackupsManagerState = v);
			_add(_gridGeneral, ++row, "Apply modifications to all selected items", "If checked, every field that you edit will modify all the currently selected items.", () => SdeAppConfiguration.EnableMultipleSetters, v => SdeAppConfiguration.EnableMultipleSetters = v);
			_add(_gridGeneral, ++row, "Bind item tabs together", "If checked, both the Client Items and Items tab will sync.", () => SdeAppConfiguration.BindItemTabs, v => SdeAppConfiguration.BindItemTabs = v);
			_add(_gridGeneral, ++row, "Always overwrite non-modified files", "If checked, non-modified files will be overwritten in your db folder.", () => SdeAppConfiguration.AlwaysOverwriteFiles, v => SdeAppConfiguration.AlwaysOverwriteFiles = v);
			
			_add(_gridDbWriter, row = 0, "Remove NoUse entry if the flag is 0 (ignore override)", "This will remove the line regardless of the override property, which must be normally at 100 for the line to be removed.", () => SdeAppConfiguration.DbNouseIgnoreOverride, v => SdeAppConfiguration.DbNouseIgnoreOverride = v);
			_add(_gridDbWriter, ++row, "Remove Trade entry if the flag is 0 (ignore override)", "This will remove the line regardless of the override property, which must be normally at 100 for the line to be removed.", () => SdeAppConfiguration.DbTradeIgnoreOverride, v => SdeAppConfiguration.DbTradeIgnoreOverride = v);
			_add(_gridDbWriter, ++row, "itemInfo.lua : Write unidentifiedDisplayName", null, () => SdeAppConfiguration.DbWriterItemInfoUnDisplayName, v => SdeAppConfiguration.DbWriterItemInfoUnDisplayName = v);
			_add(_gridDbWriter, ++row, "itemInfo.lua : Write unidentifiedResourceName", null, () => SdeAppConfiguration.DbWriterItemInfoUnResource, v => SdeAppConfiguration.DbWriterItemInfoUnResource = v);
			_add(_gridDbWriter, ++row, "itemInfo.lua : Write unidentifiedDescriptionName", null, () => SdeAppConfiguration.DbWriterItemInfoUnDescription, v => SdeAppConfiguration.DbWriterItemInfoUnDescription = v);
			_add(_gridDbWriter, ++row, "itemInfo.lua : Write identifiedDisplayName", null, () => SdeAppConfiguration.DbWriterItemInfoIdDisplayName, v => SdeAppConfiguration.DbWriterItemInfoIdDisplayName = v);
			_add(_gridDbWriter, ++row, "itemInfo.lua : Write identifiedResourceName", null, () => SdeAppConfiguration.DbWriterItemInfoIdResource, v => SdeAppConfiguration.DbWriterItemInfoIdResource = v);
			_add(_gridDbWriter, ++row, "itemInfo.lua : Write identifiedDescriptionName", null, () => SdeAppConfiguration.DbWriterItemInfoIdDescription, v => SdeAppConfiguration.DbWriterItemInfoIdDescription = v);
			_add(_gridDbWriter, ++row, "itemInfo.lua : Write slotCount", null, () => SdeAppConfiguration.DbWriterItemInfoSlotCount, v => SdeAppConfiguration.DbWriterItemInfoSlotCount = v);
			_add(_gridDbWriter, ++row, "itemInfo.lua : Write ClassNum", null, () => SdeAppConfiguration.DbWriterItemInfoClassNum, v => SdeAppConfiguration.DbWriterItemInfoClassNum = v);
			_add(_gridDbWriter, ++row, "itemInfo.lua : Write costume", null, () => SdeAppConfiguration.DbWriterItemInfoIsCostume, v => SdeAppConfiguration.DbWriterItemInfoIsCostume = v);
			_add(_gridDbWriter, ++row, "item_group : force write in single file", "'Import' fields won't be used when saving the item groups", () => SdeAppConfiguration.DbWriterItemInfoClassNum, v => SdeAppConfiguration.DbWriterItemInfoClassNum = v);

			_add(_gridDialogs, row = 0, "Use integrated dialogs for flags", "If unchecked, this will open dialogs on all the '...' buttons.", () => SdeAppConfiguration.UseIntegratedDialogsForFlags, v => SdeAppConfiguration.UseIntegratedDialogsForFlags = v);
			_add(_gridDialogs, ++row, "Use integrated dialogs for scripts", "If unchecked, this will open dialogs on all the '...' buttons.", () => SdeAppConfiguration.UseIntegratedDialogsForScripts, v => SdeAppConfiguration.UseIntegratedDialogsForScripts = v);
			_add(_gridDialogs, ++row, "Use integrated dialogs for levels", "If unchecked, this will open dialogs on all the '...' buttons.", () => SdeAppConfiguration.UseIntegratedDialogsForLevels, v => SdeAppConfiguration.UseIntegratedDialogsForLevels = v);
			_add(_gridDialogs, ++row, "Use integrated dialogs for jobs", "If unchecked, this will open dialogs on all the '...' buttons.", () => SdeAppConfiguration.UseIntegratedDialogsForJobs, v => SdeAppConfiguration.UseIntegratedDialogsForJobs = v);
			_add(_gridDialogs, ++row, "Use integrated dialogs for time", "If unchecked, this will open dialogs on all the '...' buttons.", () => SdeAppConfiguration.UseIntegratedDialogsForTime, v => SdeAppConfiguration.UseIntegratedDialogsForTime = v);

			_add(_gridRAthena, row = 0, "Use old rAthena mob mode", "If checked, this will use the old mob mode.", () => ProjectConfiguration.UseOldRAthenaMode, v => ProjectConfiguration.UseOldRAthenaMode = v);

			Binder.Bind(_comboBoxStyles, () => SdeAppConfiguration.ThemeIndex, v => {
				SdeAppConfiguration.ThemeIndex = v;
				Application.Current.Resources.MergedDictionaries.RemoveAt(Application.Current.Resources.MergedDictionaries.Count - 1);

				var path = "pack://application:,,,/" + Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "%20") + ";component/WPF/Styles/";

				if (SdeAppConfiguration.ThemeIndex == 0) {
					path += "StyleLightBlue.xaml";
				}
				else if (SdeAppConfiguration.ThemeIndex == 1) {
					path += "StyleDark.xaml";
				}

				//Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(path, UriKind.RelativeOrAbsolute) });
				Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(path, UriKind.RelativeOrAbsolute) });
				ErrorHandler.HandleException("For the theme to apply properly, please restart the application.");
			});

			_loadEncoding();
			_comboBoxCompression.Init();
			_loadAutocomplete();
			_loadShortcuts();
		}

		private void _loadShortcuts() {
			int row = 0;

			foreach (var keyPair in ApplicationShortcut.KeyBindings2) {
				string actionName = keyPair.Key;

				if (actionName == SdeStrings.AutoGenerated)
					continue;

				var binding = keyPair.Value;

				Label l = new Label { Content = actionName };
				WpfUtilities.SetGridPosition(l, row, 0);
				_gridShortcuts.Children.Add(l);

				Border b = new Border();
				b.Margin = new Thickness(3);
				b.BorderThickness = new Thickness(1);
				b.BorderBrush = WpfUtilities.LostFocusBrush;

				Grid grid = new Grid();
				grid.ColumnDefinitions.Add(new ColumnDefinition());
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto)});

				TextBox tb = new TextBox { Text = binding.KeyGesture.DisplayString };
				tb.BorderThickness = new Thickness(0);
				tb.Padding = new Thickness(0);
				tb.IsReadOnly = true;
				b.Child = tb;

				grid.Children.Add(b);

				FancyButton button = new FancyButton();
				button.ImagePath = "reset.png";
				button.Width = 20;
				button.Height = 20;
				button.Visibility = Visibility.Collapsed;
				button.Margin = new Thickness(0, 0, 3, 0);
				button.Click += delegate {
					button.Visibility = Visibility.Collapsed;
					binding.Reset();
					tb.Text = binding.KeyGesture.DisplayString;
					SdeAppConfiguration.Remapper.Remove(actionName);
					b.BorderBrush = WpfUtilities.LostFocusBrush;
				};

				if (binding.CanReset) {
					button.Visibility = Visibility.Visible;
				}

				WpfUtilities.SetGridPosition(button, 0, 1);
				grid.Children.Add(button);

				WpfUtilities.SetGridPosition(grid, row, 1);
				_gridShortcuts.Children.Add(grid);
				_gridShortcuts.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto) });

				tb.GotFocus += delegate {
					b.BorderThickness = new Thickness(2);

					if (b.BorderBrush == Brushes.Red) {
						return;
					}

					b.BorderBrush = WpfUtilities.GotFocusBrush;
				};

				tb.LostFocus += delegate {
					b.BorderThickness = new Thickness(1);

					if (b.BorderBrush == Brushes.Red) {
						return;
					}

					b.BorderBrush = WpfUtilities.LostFocusBrush;
				};

				tb.PreviewKeyDown += delegate(object sender, KeyEventArgs e) {
					if (e.Key == Key.Escape || e.Key == Key.Tab) {
						return;
					}

					bool valid;
					tb.Text = _make(e.Key, Keyboard.Modifiers, out valid);
					
					try {
						if (!valid)
							throw new Exception();

						var b2 = binding;
						var shortcut = ApplicationShortcut.Make(null, e.Key, Keyboard.Modifiers);

						while (b2 != null) {
							b2.Set(shortcut);
							b2 = b2.Next;
						}

						if (binding.CanReset) {
							button.Visibility = Visibility.Visible;
						}
						else {
							button.Visibility = Visibility.Collapsed;
						}

						SdeAppConfiguration.Remapper[actionName] = tb.Text;
						ApplicationShortcut.OverrideBindings(SdeAppConfiguration.Remapper);

						b.BorderThickness = new Thickness(2);
						b.BorderBrush = WpfUtilities.GotFocusBrush;
					}
					catch {
						b.BorderThickness = new Thickness(2);
						b.BorderBrush = Brushes.Red;
						button.Visibility = Visibility.Visible;
					}
					e.Handled = true;
				};
				
				row++;
			}
		}

		private static string _make(Key key, ModifierKeys modifiers, out bool valid) {
			string display = "";

			if (modifiers.HasFlags(ModifierKeys.Control)) {
				display += "Ctrl-";
			}
			if (modifiers.HasFlags(ModifierKeys.Shift)) {
				display += "Shift-";
			}
			if (modifiers.HasFlags(ModifierKeys.Alt)) {
				display += "Alt-";
			}
			if (modifiers.HasFlags(ModifierKeys.Windows)) {
				display += "Win-";
			}

			if (key == Key.LeftAlt ||
				key == Key.RightAlt ||
				key == Key.LeftCtrl ||
				key == Key.RightCtrl ||
				key == Key.LeftShift ||
				key == Key.RightShift ||
				key == Key.System ||
				key == Key.LWin ||
				key == Key.RWin) {
				valid = false;
				return display;
			}

			valid = true;
			display += key;
			return display;
		}

		private void _loadAutocomplete() {
			Binder.Bind(_cbAcIdDn, () => ProjectConfiguration.AutocompleteIdDisplayName);
			Binder.Bind(_cbAcUnDn, () => ProjectConfiguration.AutocompleteUnDisplayName);
			Binder.Bind(_cbAcIdRn, () => ProjectConfiguration.AutocompleteIdResourceName);
			Binder.Bind(_cbAcUnRn, () => ProjectConfiguration.AutocompleteUnResourceName);
			Binder.Bind(_cbAcIdDesc, () => ProjectConfiguration.AutocompleteIdDescription);
			Binder.Bind(_cbAcUnDesc, () => ProjectConfiguration.AutocompleteUnDescription);
			Binder.Bind(_tbPropFormat, () => ProjectConfiguration.AutocompleteDescriptionFormat);
			Binder.Bind(_cbAcNumberOfSlot, () => ProjectConfiguration.AutocompleteNumberOfSlot);
			Binder.Bind(_cbAcEmptyFields, () => ProjectConfiguration.AutocompleteFillOnlyEmptyFields);
			Binder.Bind(_tbUnDesc, () => ProjectConfiguration.AutocompleteUnDescriptionFormat);
			Binder.Bind(_tbDescNotSet, () => ProjectConfiguration.AutocompleteDescNotSet);
			Binder.Bind(_cbWriteNeutralProperty, () => ProjectConfiguration.AutocompleteNeutralProperty);
			Binder.Bind(_cbAcViewId, () => ProjectConfiguration.AutocompleteViewId);

			int index = 0;

			List<TextBox> boxes = new List<TextBox>();

			foreach (ParameterHolderKeys property in ParameterHolderKeys.Keys) {
				ParameterHolderKeys key = property;
				Label label = new Label { Padding = new Thickness(0), Margin = new Thickness(3), Content = property.Key };
				TextBox box = new TextBox { Text = SdeAppConfiguration.ConfigAsker["Autocompletion - " + key.Key, key.Key], Margin = new Thickness(3) };

				WpfUtilities.SetGridPosition(label, index, 1, 0, 1);
				WpfUtilities.SetGridPosition(box, index, 1, 2, 1);

				_gridDescProp.Children.Add(label);
				_gridDescProp.Children.Add(box);
				_gridDescProp.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto) });

				SdeAppConfiguration.Bind(box, v => SdeAppConfiguration.ConfigAsker["Autocompletion - " + key.Key] = box.Text, p => p);

				index++;
			}

			index = 0;

			foreach (string property in ParameterHolder.Properties) {
				TextBox box = new TextBox { Text = ProjectConfiguration.AutocompleteProperties[index], Margin = new Thickness(3) };
				TextBlock block = new TextBlock { Text = property, Margin = new Thickness(3), VerticalAlignment = VerticalAlignment.Center };

				WpfUtilities.SetGridPosition(block, index / 2 + 4, 2 * (index % 2));
				WpfUtilities.SetGridPosition(box, index / 2 + 4, 2 * (index % 2) + 1);

				boxes.Add(box);
				_gridDescription.Children.Add(box);
				_gridDescription.Children.Add(block);

				SdeAppConfiguration.Bind(box, v => ProjectConfiguration.AutocompleteProperties = v, q => boxes.Select(p => p.Text).ToList());

				index++;
			}

			DisplayablePropertyHelper.FindAll<CheckBox>(_gridAutocomplete).ForEach(WpfUtils.AddMouseInOutEffectsBox);
		}

		private void _add(Grid grid, int row, string display, string tooltip, Func<bool> get, Action<bool> set) {
			Grid ngrid = new Grid();
			ngrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
			ngrid.ColumnDefinitions.Add(new ColumnDefinition());

			TextBlock label = new TextBlock { Text = display, TextWrapping = System.Windows.TextWrapping.Wrap, Margin = new Thickness(3) };
			ngrid.Children.Add(label);
			//label.SetValue(Grid.RowProperty, row);
			label.SetValue(Grid.ColumnProperty, 1);

			if (tooltip != null) {
				label.ToolTip = new TextBlock { Text = tooltip, MaxWidth = 350, TextWrapping = System.Windows.TextWrapping.Wrap };
				label.SetValue(ToolTipService.ShowDurationProperty, 2000000);
			}

			CheckBox box = new CheckBox { Margin = new Thickness(3) };
			box.VerticalAlignment = System.Windows.VerticalAlignment.Center;
			box.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
			ngrid.Children.Add(box);
			//box.SetValue(Grid.RowProperty, row);
			box.SetValue(Grid.ColumnProperty, 0);

			label.MouseEnter += delegate {
				Mouse.OverrideCursor = Cursors.Hand;
				label.Foreground = Application.Current.Resources["MouseOverTextBrush"] as SolidColorBrush;
				label.SetValue(TextBlock.TextDecorationsProperty, TextDecorations.Underline);
				label.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e) {
					if (sender != label) return;
					box.IsChecked = !box.IsChecked;
					e.Handled = true;
				};
			};

			label.MouseLeave += delegate {
				Mouse.OverrideCursor = null;
				label.Foreground = Application.Current.Resources["TextForeground"] as SolidColorBrush;
				label.SetValue(TextBlock.TextDecorationsProperty, null);
			};

			Binder.Bind(box, get, set);
			grid.Children.Add(ngrid);
			ngrid.SetValue(Grid.RowProperty, row);
			ngrid.SetValue(Grid.ColumnSpanProperty, 2);
		}

		private void _loadEncoding() {
			_comboBoxResEncoding.ItemsSource = new[] {
				"Client encoding (" + SdeAppConfiguration.EncodingCodepageClient + ")",
				"Server encoding (" + SdeAppConfiguration.EncodingServer.CodePage + ")",
				"Defaullt (codeage 1252 - Western European [Windows])",
				"Korean (codepage 949 - ANSI/OEM Korean [Unified Hangul Code])",
				"Other..."
			};

			_comboBoxResEncoding.SelectedIndex = SdeAppConfiguration.EncodingResIndex;
			_comboBoxResEncoding.SelectionChanged += new SelectionChangedEventHandler(_comboBoxResEncoding_SelectionChanged);
			_comboBoxResEncoding_SelectionChanged(null, null);
		}

		private void _comboBoxResEncoding_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			object oldSelected = null;
			bool cancel = false;

			if (e == null) {
				oldSelected = _comboBoxResEncoding.SelectedIndex;
			}
			else if (e.RemovedItems.Count > 0)
				oldSelected = e.RemovedItems[0];

			try {
				switch (_comboBoxResEncoding.SelectedIndex) {
					case 4:
						if (sender == null) {
							break;
						}

						InputDialog dialog = WindowProvider.ShowWindow<InputDialog>(
							new InputDialog(
								"Using an unsupported encoding may cause unexpected results.\n" +
								"Enter the codepage number for the encoding :",
								"Encoding", SdeAppConfiguration.EncodingResCodePage.ToString(CultureInfo.InvariantCulture)), this);

						if (dialog.DialogResult == true) {
							bool pageExists = EncodingService.EncodingExists(dialog.Input);

							if (!pageExists) {
								cancel = true;
							}
							else {
								SdeAppConfiguration.EncodingResCodePage = Int32.Parse(dialog.Input);
							}
						}
						else {
							cancel = true;
						}
						break;
					case 0:
						SdeAppConfiguration.EncodingResCodePage = SdeAppConfiguration.EncodingCodepageClient;
						break;
					case 1:
						SdeAppConfiguration.EncodingResCodePage = SdeAppConfiguration.EncodingServer.CodePage;
						break;
					case 2:
						SdeAppConfiguration.EncodingResCodePage = 1252;
						break;
					case 3:
						SdeAppConfiguration.EncodingResCodePage = 949;
						break;
					case -1:
						return;
				}
			}
			catch {
				cancel = true;
			}

			if (cancel) {
				_comboBoxResEncoding.SelectionChanged -= _comboBoxResEncoding_SelectionChanged;

				if (oldSelected != null) {
					_comboBoxResEncoding.SelectedItem = oldSelected;
				}

				_comboBoxResEncoding.SelectionChanged += _comboBoxResEncoding_SelectionChanged;
			}

			if (_comboBoxResEncoding.SelectedIndex > -1 && !cancel) {
				SdeAppConfiguration.EncodingResIndex = _comboBoxResEncoding.SelectedIndex;
			}
		}

		private void _fbResetShortcuts_Click(object sender, RoutedEventArgs e) {
			SdeAppConfiguration.Remapper.Clear();
			_gridShortcuts.Children.Clear();
			ApplicationShortcut.ResetBindings();
			ApplicationShortcut.OverrideBindings(SdeAppConfiguration.Remapper);
			_loadShortcuts();
		}

		private void _fbRefreshhortcuts_Click(object sender, RoutedEventArgs e) {
			_gridShortcuts.Children.Clear();
			_loadShortcuts();
		}
	}
}
