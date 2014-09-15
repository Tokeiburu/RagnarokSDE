using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SDE.Others;
using SDE.Tools.DatabaseEditor.Objects.Jobs;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using Utilities;

namespace SDE.Tools.DatabaseEditor.WPF {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class JobEditDialog : TkWindow {
		private readonly List<CheckBox> _boxes = new List<CheckBox>();
		private bool _boxEventsEnabled = true;
		private bool _dbEventsEnabled = true;

		public JobEditDialog(string text)
			: base("Job edit", "cde.ico", SizeToContent.Height, ResizeMode.CanResize) {
			InitializeComponent();
			Extensions.SetMinimalSize(this);

			if (text == "") {
				text = "FFFFFFFF";
			}

			text = text.Replace("0x", "").Replace("0X", "");

			_addJob(_jobGrid1, JobList.Novice);
			_addJob(_jobGrid1, JobList.Taekwon);
			_addJob(_jobGrid1, JobList.SoulLinker);
			_addJob(_jobGrid1, JobList.StarGladiator);
			_addJob(_jobGrid1, JobList.Gunslinger);
			_addJob(_jobGrid1, JobList.Ninja);

			_addJob(_jobGrid2, JobList.Swordman);
			_addJob(_jobGrid2, JobList.Acolyte);
			_addJob(_jobGrid2, JobList.Mage);
			_addJob(_jobGrid2, JobList.Archer);
			_addJob(_jobGrid2, JobList.Merchant);
			_addJob(_jobGrid2, JobList.Thief);

			_addJob(_jobGrid3, JobList.Knight);
			_addJob(_jobGrid3, JobList.Priest);
			_addJob(_jobGrid3, JobList.Wizard);
			_addJob(_jobGrid3, JobList.Hunter);
			_addJob(_jobGrid3, JobList.Blacksmith);
			_addJob(_jobGrid3, JobList.Assassin);

			_addJob(_jobGrid4, JobList.Crusader);
			_addJob(_jobGrid4, JobList.Monk);
			_addJob(_jobGrid4, JobList.Sage);
			_addJob(_jobGrid4, JobList.BardDancer);
			_addJob(_jobGrid4, JobList.Alchemist);
			_addJob(_jobGrid4, JobList.Rogue);

			_preview.TextChanged += new TextChangedEventHandler(_preview_TextChanged);
			_cbJobs.SelectionChanged += new SelectionChangedEventHandler(_cbJobs_SelectionChanged);
			_preview.Text = text;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			ShowInTaskbar = true;
		}

		private void _cbJobs_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (!_boxEventsEnabled)
				return;

			if (_cbJobs.SelectedItem == null || String.IsNullOrEmpty(_cbJobs.SelectedItem.ToString()) || _cbJobs.SelectedItem.ToString().Trim(' ') == "")
				return;

			try {
				_dbEventsEnabled = false;
				_preview.Text = JobList.GetHexJob(_cbJobs.SelectedItem.ToString()).Replace("0X", "").Replace("0x", "");
			}
			finally {
				_dbEventsEnabled = true;
			}
		}

		public string Text {
			get {
				return _preview.Text.Replace("0X", "").Replace("0x", "");
			}
		}

		private void _preview_TextChanged(object sender, TextChangedEventArgs e) {
			if (!_boxEventsEnabled)
				return;

			try {
				_boxEventsEnabled = false;
				int value;

				try {
					value = Convert.ToInt32(_preview.Text, 16);
					WpfUtilities.TextBoxOk(_preview);
				}
				catch {
					WpfUtilities.TextBoxError(_preview);
					return;
				}

				foreach (CheckBox box in _boxes) {
					Job job = (Job) box.Tag;

					if ((job.Id & value) == job.Id) {
						box.IsChecked = true;
					}
					else {
						box.IsChecked = false;
					}
				}

				_updateClassPreview();
			}
			finally {
				_boxEventsEnabled = true;
			}
		}

		private void _updateClassPreview() {
			_previewClass.Text = JobList.GetJobsFromHex(_preview.Text).Replace("0x", "").Replace("0X", "");

			if (_dbEventsEnabled)
				_cbJobs.SelectedItem = _previewClass.Text;
		}

		private void _addJob(Grid grid, Job job) {
			CheckBox box = new CheckBox();
			box.Margin = new Thickness(7, 6, 7, 6);
			box.Content = job == JobList.BardDancer ? Methods.Aggregate(job.Names, ", ") : job.Name;
			//box.Content = (job == JobList.BardDancer || job == JobList.Kangerou) ? Methods.Aggregate(job.Names, ", ") : job.Name;
			box.SetValue(Grid.RowProperty, grid.RowDefinitions.Count);
			box.VerticalAlignment = VerticalAlignment.Center;
			box.Tag = job;

			box.Checked += delegate {
				_update();
			};

			box.Unchecked += delegate {
				_update();
			};

			_boxes.Add(box);

			grid.RowDefinitions.Add(new RowDefinition());
			grid.Children.Add(box);
		}

		private void _update() {
			if (!_boxEventsEnabled)
				return;

			try {
				_boxEventsEnabled = false;

				string hexJob = JobList.GetHexJob(_boxes.Where(p => p.IsChecked == true).Select(p => (Job) p.Tag).ToList());
				_preview.Text = hexJob;
				_updateClassPreview();
				WpfUtilities.TextBoxOk(_preview);
			}
			finally {
				_boxEventsEnabled = true;
			}
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			DialogResult = true;
			Close();
		}
	}
}
