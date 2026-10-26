using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ErrorManager;
using GRF.IO;
using SDE.ApplicationConfiguration;
using SDE.Databases.ClientItems.Common;
using SDE.Editor;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Binder = GrfToWpfBridge.Binder;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class SettingsDialog : TkWindow {
		public SettingsDialog() : base("Advanced settings", "settings.png") {
			InitializeComponent();
			Binder.Bind(_pbNotepad.TextBox, () => SdeAppConfiguration.NotepadPath);
			Binder.Bind(_comboBoxStyles, () => SdeAppConfiguration.ThemeIndex, v => _changeStyle(v));

			_comboBoxCompression.Init();
			_loadAutocomplete();

			LoadShortcuts();
		}

		private void _changeStyle(int themeIndex) {
			try {
				SdeAppConfiguration.ThemeIndex = themeIndex;
				Application.Current.Resources.MergedDictionaries.RemoveAt(Application.Current.Resources.MergedDictionaries.Count - 1);

				var path = "pack://application:,,,/" + Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "%20") + ";component/WPF/Styles/";

				if (SdeAppConfiguration.ThemeIndex == 0) {
					path += "StyleLightBlue.xaml";
				}
				else if (SdeAppConfiguration.ThemeIndex == 1) {
					path += "StyleDark.xaml";
				}

				Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(path, UriKind.RelativeOrAbsolute) });
				ApplicationManager.OnThemeChanged();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private Dictionary<string, SettingsShortcutGenerator.ShortcutVisual> _shortcuts;

		public void LoadShortcuts() {
			_gridShortcuts.Children.Clear();
			_shortcuts = SettingsShortcutGenerator.CreateGrid(SdeAppConfiguration.Remapper, _gridShortcuts);
		}

		private void _loadAutocomplete() {
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
		}

		private void _fbResetShortcuts_Click(object sender, RoutedEventArgs e) {
			SdeAppConfiguration.Remapper.Clear();
			ApplicationShortcut.ResetBindings();
			ApplicationShortcut.OverrideBindings(SdeAppConfiguration.Remapper);
			LoadShortcuts();
		}

		private void _fbRefreshhortcuts_Click(object sender, RoutedEventArgs e) {
			LoadShortcuts();
		}

		private void _tbSearch_TextChanged(object sender, TextChangedEventArgs e) {
			if (_tbSearch.Text == "") {
				foreach (var key in _shortcuts.Values) {
					key.Show();
				}
			}
			else {
				foreach (var key in _shortcuts) {
					if (key.Key.IndexOf(_tbSearch.Text, StringComparison.OrdinalIgnoreCase) > -1) {
						key.Value.Show();
					}
					else {
						key.Value.Hide();
					}
				}
			}
		}

		private void _cbSdeShell_Click(object sender, RoutedEventArgs e) {
			if (SdeAppConfiguration.SdeShellAssociated) {
				ApplicationManager.AddExtension(Methods.ApplicationFullPath, "Server database editor", ".sde", true);
			}
			else {
				GrfPath.Delete(GrfPath.Combine(SdeAppConfiguration.ProgramDataPath, "sde.ico"));
				ApplicationManager.RemoveExtension(Methods.ApplicationFullPath, ".sde");
			}
		}
	}
}
