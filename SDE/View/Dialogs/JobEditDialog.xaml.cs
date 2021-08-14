using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GrfToWpfBridge;
using SDE.ApplicationConfiguration;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Jobs;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class JobEditDialog : TkWindow, IInputWindow {
		private readonly ReadableTuple<int> _tuple;
		private readonly List<CheckBox> _boxes = new List<CheckBox>();
		private bool _boxEventsEnabled = true;
		private bool _previewEventsEnabled = true;
		private bool _dbEventsEnabled = true;
		private int _upper = 63;
		private int _gender = 2;
		private bool _checkAllEnabled = true;

		public JobEditDialog(string text, ReadableTuple<int> tuple)
			: base("Job edit", "cde.ico", SizeToContent.Height, ResizeMode.CanResize) {
			_tuple = tuple;
			InitializeComponent();

			Binder.Bind(_cbRestrictClasses, () => SdeAppConfiguration.RestrictToAllowedJobs, _classRestrict);

			if (tuple != null) {
				_upper = tuple.GetValue<int>(ServerItemAttributes.Upper);
				_gender = tuple.GetValue<int>(ServerItemAttributes.Gender);
			}

			if (text == "") {
				text = "00000000";
			}

			text = text.Replace("0x", "").Replace("0X", "");

			_addJob(_jobGrid1, JobList.Novice);
			_addJob(_jobGrid1, JobList.SuperNovice);
			_addJob(_jobGrid1, JobList.Summoner);
			_addJob(_jobGrid1, null);
			_addJob(_jobGrid1, null);
			_addJob(_jobGrid1, null);
			_addJob(_jobGrid1, null);
			_addJob(_jobGrid1, null);
			_addJob(_jobGrid1, null);

			_addJob(_jobGrid2, JobList.Swordman);
			_addJob(_jobGrid2, JobList.Acolyte);
			_addJob(_jobGrid2, JobList.Mage);
			_addJob(_jobGrid2, JobList.Archer);
			_addJob(_jobGrid2, JobList.Merchant);
			_addJob(_jobGrid2, JobList.Thief);
			_addJob(_jobGrid2, JobList.Taekwon);
			_addJob(_jobGrid2, JobList.Gunslinger);
			_addJob(_jobGrid2, JobList.Ninja);

			_addJob(_jobGrid3, JobList.Knight);
			_addJob(_jobGrid3, JobList.Priest);
			_addJob(_jobGrid3, JobList.Wizard);
			_addJob(_jobGrid3, JobList.Hunter);
			_addJob(_jobGrid3, JobList.Blacksmith);
			_addJob(_jobGrid3, JobList.Assassin);
			_addJob(_jobGrid3, JobList.StarGladiator);
			_addJob(_jobGrid3, JobList.Rebellion);
			_addJob(_jobGrid3, JobList.KagerouOboro);

			_addJob(_jobGrid4, JobList.Crusader);
			_addJob(_jobGrid4, JobList.Monk);
			_addJob(_jobGrid4, JobList.Sage);
			_addJob(_jobGrid4, JobList.BardDancer);
			_addJob(_jobGrid4, JobList.Alchemist);
			_addJob(_jobGrid4, JobList.Rogue);
			_addJob(_jobGrid4, JobList.SoulLinker);
			_addJob(_jobGrid4, null);
			_addJob(_jobGrid4, null);

			_preview.TextChanged += new TextChangedEventHandler(_preview_TextChanged);
			_cbJobs.SelectionChanged += new SelectionChangedEventHandler(_cbJobs_SelectionChanged);
			_preview.Text = text;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			_classRestrict();
			_cbJobMode.SelectionChanged += new SelectionChangedEventHandler(_cbJobMode_SelectionChanged);

			_cbSelectAll.IsChecked = _boxes.All(p => p.IsEnabled && p.IsChecked == true);
			_cbSelectAll.Checked += (sender, e) => _selectAll(true);
			_cbSelectAll.Unchecked += (sender, e) => _selectAll(false);

			WpfUtils.AddMouseInOutEffectsBox(_cbRestrictClasses, _cbSelectAll);
		}

		private void _selectAll(bool v) {
			if (!_boxEventsEnabled) return;
			if (!_checkAllEnabled) return;
			_checkAllEnabled = false;
			_boxes.Where(p => p.IsEnabled).ToList().ForEach(p => p.IsChecked = v);
			_checkAllEnabled = true;
		}

		private void _cbJobMode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (!_boxEventsEnabled) return;

			try {
				//int result = Convert.ToInt32(text, 16);
				long result = JobList.JobAggregate(0, _boxes.Where(p => p.IsChecked == true).Select(p => (Job)p.Tag).ToList(), true);

				if (_cbJobMode.SelectedIndex == 0) {
					result = JobList.AllJobOld.Id - result;
				}
				else {
					result = -1 - (JobList.AllJobOld.Id - result);
				}

				if (_previewEventsEnabled)
					_preview.Text = String.Format("0x{0:X8}", (int)result);
			}
			catch {
			}
		}

		private void _classRestrict() {
			_boxes.ForEach(p => p.IsEnabled = true);
			JobGroup group = JobGroup.Get(_upper);

			_setJob(0, _jobGrid1, Job.Get(JobList.Novice.Id, group));

			_setJob(0, _jobGrid2, Job.Get(JobList.Swordman.Id, group));
			_setJob(1, _jobGrid2, Job.Get(JobList.Acolyte.Id, group));
			_setJob(2, _jobGrid2, Job.Get(JobList.Mage.Id, group));
			_setJob(3, _jobGrid2, Job.Get(JobList.Archer.Id, group));
			_setJob(4, _jobGrid2, Job.Get(JobList.Merchant.Id, group));
			_setJob(5, _jobGrid2, Job.Get(JobList.Thief.Id, group));
			_setJob(6, _jobGrid2, Job.Get(JobList.Taekwon.Id, group));
			_setJob(7, _jobGrid2, Job.Get(JobList.Gunslinger.Id, group));
			_setJob(8, _jobGrid2, Job.Get(JobList.Ninja.Id, group));

			_setJob(0, _jobGrid3, Job.Get(JobList.Knight.Id, group));
			_setJob(1, _jobGrid3, Job.Get(JobList.Priest.Id, group));
			_setJob(2, _jobGrid3, Job.Get(JobList.Wizard.Id, group));
			_setJob(3, _jobGrid3, Job.Get(JobList.Hunter.Id, group));
			_setJob(4, _jobGrid3, Job.Get(JobList.Blacksmith.Id, group));
			_setJob(5, _jobGrid3, Job.Get(JobList.Assassin.Id, group));
			_setJob(6, _jobGrid3, Job.Get(JobList.StarGladiator.Id, group));
			_setJob(7, _jobGrid3, Job.Get(JobList.Rebellion.Id, group));
			_setJob(8, _jobGrid3, Job.Get(JobList.KagerouOboro.Id, group));

			_setJob(0, _jobGrid4, Job.Get(JobList.Crusader.Id, group));
			_setJob(1, _jobGrid4, Job.Get(JobList.Monk.Id, group));
			_setJob(2, _jobGrid4, Job.Get(JobList.Sage.Id, group));
			_setJob(3, _jobGrid4, Job.Get(JobList.BardDancer.Id, group));
			_setJob(4, _jobGrid4, Job.Get(JobList.Alchemist.Id, group));
			_setJob(5, _jobGrid4, Job.Get(JobList.Rogue.Id, group));
			_setJob(6, _jobGrid4, Job.Get(JobList.SoulLinker.Id, group));

			if (SdeAppConfiguration.RestrictToAllowedJobs) {
				_restrict(0, _jobGrid1, JobList.Novice.Id, group);

				_restrict(0, _jobGrid2, JobList.Swordman.Id, group);
				_restrict(1, _jobGrid2, JobList.Acolyte.Id, group);
				_restrict(2, _jobGrid2, JobList.Mage.Id, group);
				_restrict(3, _jobGrid2, JobList.Archer.Id, group);
				_restrict(4, _jobGrid2, JobList.Merchant.Id, group);
				_restrict(5, _jobGrid2, JobList.Thief.Id, group);
				_restrict(6, _jobGrid2, JobList.Taekwon.Id, group);
				_restrict(7, _jobGrid2, JobList.Gunslinger.Id, group);
				_restrict(8, _jobGrid2, JobList.Ninja.Id, group);

				_restrict(0, _jobGrid3, JobList.Knight.Id, group);
				_restrict(1, _jobGrid3, JobList.Priest.Id, group);
				_restrict(2, _jobGrid3, JobList.Wizard.Id, group);
				_restrict(3, _jobGrid3, JobList.Hunter.Id, group);
				_restrict(4, _jobGrid3, JobList.Blacksmith.Id, group);
				_restrict(5, _jobGrid3, JobList.Assassin.Id, group);
				_restrict(6, _jobGrid3, JobList.StarGladiator.Id, group);
				_restrict(7, _jobGrid3, JobList.Rebellion.Id, group);
				_restrict(8, _jobGrid3, JobList.KagerouOboro.Id, group);

				_restrict(0, _jobGrid4, JobList.Crusader.Id, group);
				_restrict(1, _jobGrid4, JobList.Monk.Id, group);
				_restrict(2, _jobGrid4, JobList.Sage.Id, group);
				_restrict(3, _jobGrid4, JobList.BardDancer.Id, group);
				_restrict(4, _jobGrid4, JobList.Alchemist.Id, group);
				_restrict(5, _jobGrid4, JobList.Rogue.Id, group);
				_restrict(6, _jobGrid4, JobList.SoulLinker.Id, group);
			}

			foreach (var box in _boxes)
				WpfUtils.AddMouseInOutEffectsBox(box);

			_update();
		}

		private void _restrict(int index, Grid grid, long id, JobGroup group) {
			List<Job> jobs = JobList.AllJobs.Where(p => p.Id == id && (p.Upper & group.Id) == p.Upper).ToList();

			var box = (CheckBox) grid.Children[index];
			box.IsEnabled = jobs.Count != 0;

			if (jobs.Count == 0)
				box.IsChecked = false;
		}

		public string Text {
			get {
				return _preview.Text.Replace("0X", "").Replace("0x", "");
			}
		}

		public Grid Footer { get { return _footerGrid; } }
		public event Action ValueChanged;

		public void OnValueChanged() {
			Action handler = ValueChanged;
			if (handler != null) handler();
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

		private void _preview_TextChanged(object sender, TextChangedEventArgs e) {
			if (!_boxEventsEnabled)
				return;

			if (!_previewEventsEnabled)
				return;

			try {
				_boxEventsEnabled = false;
				_previewEventsEnabled = false;
				int value;

				try {
					value = Convert.ToInt32(_preview.Text, 16);
					_preview.Dispatch(p => p.Background = Application.Current.Resources["GSearchEngineOk"] as Brush);
				}
				catch {
					_preview.Dispatch(p => p.Background = Application.Current.Resources["GSearchEngineError"] as Brush);
					return;
				}

				if (Job.IsExcept(value)) {
					value ^= -1;

					foreach (CheckBox box in _boxes) {
						Job job = (Job)box.Tag;

						if (!box.IsEnabled) {
							box.IsChecked = false;
							continue;
						}

						if ((job.Id & value) == job.Id) {
							box.IsChecked = true;
						}
						else {
							box.IsChecked = false;
						}
					}

					_cbJobMode.SelectedIndex = 1;
				}
				else {
					foreach (CheckBox box in _boxes) {
						Job job = (Job)box.Tag;

						if (!box.IsEnabled) {
							box.IsChecked = false;
							continue;
						}

						if ((job.Id & value) == job.Id) {
							box.IsChecked = true;
						}
						else {
							box.IsChecked = false;
						}
					}

					_cbJobMode.SelectedIndex = 0;
				}
			}
			finally {
				_boxEventsEnabled = true;
				_update();
				_previewEventsEnabled = true;
			}
		}

		private void _updateClassPreview() {
			_previewClass.Text = JobList.GetStringJobFromHex(_preview.Text, _upper, _gender).Replace("0x", "").Replace("0X", "");

			if (_dbEventsEnabled)
				_cbJobs.SelectedItem = _previewClass.Text;
		}

		private void _addJob(Grid grid, Job job) {
			if (job == null) {
				grid.RowDefinitions.Add(new RowDefinition());
				//grid.Children.Add(new Label() { Background = Brushes.Orange });
				return;
			}

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

			//WpfUtils.AddMouseInOutEffectsBox(box);
			_boxes.Add(box);

			grid.RowDefinitions.Add(new RowDefinition());
			grid.Children.Add(box);
		}

		private void _setJob(int index, Grid grid, Job job) {
			((CheckBox)grid.Children[index]).Content = job.GetName(_gender);
		}

		private void _update() {
			if (!_boxEventsEnabled)
				return;

			try {
				_boxEventsEnabled = false;

				string hexJob;

				if (_cbJobMode.SelectedIndex == 1) {
					hexJob = JobList.GetNegativeHexJob(_boxes.Where(p => p.IsChecked == true).Select(p => (Job)p.Tag).ToList());
				}
				else {
					hexJob = JobList.GetHexJob(_boxes.Where(p => p.IsChecked == true).Select(p => (Job)p.Tag).ToList());
				}

				if (_previewEventsEnabled)
					_preview.Text = hexJob;

				_updateClassPreview();
				_preview.Dispatch(p => p.Background = Application.Current.Resources["GSearchEngineOk"] as Brush);
				_checkAllEnabled = false;
				_cbSelectAll.IsChecked = _boxes.All(p => p.IsEnabled && p.IsChecked == true);
				_checkAllEnabled = true;
				OnValueChanged();
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
			if (!SdeAppConfiguration.UseIntegratedDialogsForJobs)
				DialogResult = true;
			Close();
		}
	}
}
