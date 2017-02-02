using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using GRF.Core.GroupedGrf;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.SprFormat;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.Editor.Jobs;
using SDE.Tools.ActViewer;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WpfBugFix;
using Utilities.Extension;
using Utilities.Services;

namespace SDE.Editor.Engines.PreviewEngine {
	public class PreviewHelper {
		public const string SpriteDefault = "default";
		public const string SpriteNone = "none";
		public const string ViewIdNotSet = "Lua error: view ID not associated.";
		public const string ViewIdIncrease = "Weapon: increase view ID";

		private readonly Act _bodyReferenceDefault;
		private readonly Act _emptyAct = new Act(new Spr());
		private readonly FrameViewer _frameViewer;
		private readonly Border _gridSpriteMissing;
		private readonly Act _headReferenceDefault;
		private readonly RangeObservableCollection<Job> _jobs = new RangeObservableCollection<Job>();
		private readonly ListView _listView;
		private readonly List<IViewIdPreview> _previews = new List<IViewIdPreview>();
		private readonly List<ActReference> _references;
		private readonly CompactActIndexSelector _selector;
		private readonly FrameViewerSettings _settings;
		private readonly TextBox _tbSpriteMissing;
		private Act _act;
		private GDbTab _currentTab;
		private IViewIdPreview _lastMatch;
		private ReadableTuple<int> _lastTuple;
		private object _oldJob;
		private GenderType? _overrideGender;
		private MultiGrfReader _metaGrf;
		private int _viewId;

		public int ViewId {
			get { return _viewId; }
			set {
				_viewId = value;
				LastestViewId = value;
			}
		}

		public static int LastestViewId { get; set; }

		public Act Act {
			get { return _act; }
			set { _act = value; }
		}

		public PreviewHelper(RangeListView listView, AbstractDb<int> db, CompactActIndexSelector selector,
			FrameViewer frameViewer, Border gridSpriteMissing, TextBox tbSpriteMissing
			) {
			_listView = listView;
			_selector = selector;
			_frameViewer = frameViewer;
			_gridSpriteMissing = gridSpriteMissing;
			_tbSpriteMissing = tbSpriteMissing;
			_listView.ItemsSource = _jobs;

			if (db != null)
				_metaGrf = db.ProjectDatabase.MetaGrf;

			Db = db;

			_headReferenceDefault = new Act(ApplicationManager.GetResource("ref_head.act"), new Spr(ApplicationManager.GetResource("ref_head.spr")));
			_bodyReferenceDefault = new Act(ApplicationManager.GetResource("ref_body.act"), new Spr(ApplicationManager.GetResource("ref_body.spr")));

			_settings = new FrameViewerSettings();
			_settings.Act = () => _act;
			_references = new List<ActReference>();
			_references.Add(new ActReference { Act = DefaultBodyReference, Mode = ZMode.Back, Show = true });
			_references.Add(new ActReference { Act = DefaultHeadReference, Mode = ZMode.Back, Show = true });
			_settings.ReferencesGetter = () => _references;

			if (_selector != null) {
				_selector.Init(_frameViewer);
				_selector.Load(null);
				_selector.FrameChanged += (s, p) => _frameViewer.Update();
				_selector.ActionChanged += (s, p) => _frameViewer.Update();
				_selector.SpecialFrameChanged += (s, p) => _frameViewer.Update();
				_settings.SelectedAction = () => _selector.SelectedAction;
				_settings.SelectedFrame = () => _selector.SelectedFrame;
			}

			for (int i = 0; i < 104; i++) _emptyAct.AddAction();

			if (_frameViewer != null) {
				_frameViewer.InitComponent(_settings);
			}

			_listView.SelectionChanged += new SelectionChangedEventHandler(_jobChanged);
			_listView.PreviewMouseDown += _listView_PreviewMouseDown;
			_listView.PreviewMouseUp += _listView_PreviewMouseDown;

			_previews.Add(new HeadgearPreview());
			_previews.Add(new ShieldPreview());
			_previews.Add(new WeaponPreview());
			_previews.Add(new GarmentPreview());
			_previews.Add(new NpcPreview());
			_previews.Add(new NullPreview());
		}

		private Act _bodyReference {
			get { return _settings.References[0].Act; }
			set { _settings.ReferencesGetter()[0].Act = value; }
		}

		private Act _headReference {
			get { return _settings.References[1].Act; }
		}

		public Act DefaultBodyReference {
			get { return _bodyReferenceDefault; }
		}

		public Act DefaultHeadReference {
			get { return _headReferenceDefault; }
		}

		public string PreviewSprite { get; set; }
		public AbstractDb<int> Db { get; private set; }

		public Job Job {
			get { return _listView.SelectedItem as Job; }
		}

		public IEnumerable<Job> AllJobs {
			get { return _jobs; }
		}

		public Job PreferredJob { get; set; }
		protected bool KeepPreviousPreviewPosition { get; private set; }

		public MultiGrfReader Grf {
			get { return _metaGrf; }
		}

		public GenderType Gender {
			get {
				if (_overrideGender != null)
					return _overrideGender.Value;

				var gender = _lastTuple.GetValue<GenderType>(ServerItemAttributes.Gender);

				if (gender == GenderType.Both || gender == GenderType.Undefined)
					return GenderType.Male;
				return gender;
			}
		}

		public string GenderString {
			get { return EncodingService.FromAnyToDisplayEncoding(Gender == GenderType.Male ? "≥≤" : "ø©"); }
		}

		private void _listView_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
			try {
				ListViewItem item = _listView.GetObjectAtPoint<ListViewItem>(e.GetPosition(_listView));

				if (item != null)
					PreferredJob = item.Content as Job;
			}
			catch {
			}
		}

		public void SetJobs(List<Job> jobs) {
			_jobs.Clear();

			List<Job> j1 = jobs.Where(p => p.Name.StartsWith("Baby ")).ToList();
			List<Job> j2 = jobs.Where(p => !p.Name.StartsWith("Baby ")).ToList();

			_jobs.AddRange(j2);
			_jobs.AddRange(j1);
		}

		public void RemoveJobs() {
			_oldJob = _listView.SelectedItem;
			_jobs.Clear();
			ResetPreview();
		}

		public void ResetPreview() {
			_bodyReference = _bodyReferenceDefault;
		}

		public Act GetBodySprite(Job job, string gender = "≥≤") {
			var jobActionData = Grf.GetData(EncodingService.FromAnyToDisplayEncoding(@"data\sprite\¿Œ∞£¡∑\∏ˆ≈Î\" + gender + "\\" + job.GetSpriteName(Gender) + EncodingService.FromAnyToDisplayEncoding("_" + gender + ".act")));
			var jobSpriteData = Grf.GetData(EncodingService.FromAnyToDisplayEncoding(@"data\sprite\¿Œ∞£¡∑\∏ˆ≈Î\" + gender + "\\" + job.GetSpriteName(Gender) + EncodingService.FromAnyToDisplayEncoding("_" + gender + ".spr")));

			if (jobActionData == null || jobSpriteData == null) {
				AddError("resource error: sprite for job '" + job.Name + "' not found.");
				return DefaultBodyReference;
			}

			return new Act(jobActionData, new Spr(jobSpriteData));
		}

		public List<string> TestItem(ReadableTuple<int> tuple, MultiGrfReader grf, Type compare = null) {
			var result = new List<string>();
			_metaGrf = grf;
			_lastTuple = tuple;

			foreach (var preview in _previews) {
				if (preview.CanRead(tuple) && !(preview is NullPreview) && (compare == null || preview.GetType() == compare)) {
					string jobt = tuple.GetValue<string>(ServerItemAttributes.ApplicableJob);
					var jobs = JobList.GetJobsFromHex("0x" + ((jobt == "") ? "FFFFFFFF" : jobt), tuple.GetIntNoThrow(ServerItemAttributes.Upper));
					preview.Read(tuple, this, jobs);

					_jobs.Clear();
					_jobs.AddRange(jobs);

					if (PreviewSprite == SpriteNone)
						return result;

					var gender = _lastTuple.GetValue<GenderType>(ServerItemAttributes.Gender);

					foreach (var job in jobs) {
						_listView.SelectedItem = job;

						if (_listView.SelectedItem == null) {
							continue;
						}

						if (gender == GenderType.Undefined)
							gender = GenderType.Both;

						if (gender == GenderType.Both || gender == GenderType.Female) {
							_overrideGender = GenderType.Female;

							var act = preview.GetSpriteFromJob(tuple, this);
							var spr = act.ReplaceExtension(".spr");

							result.Add(act);
							result.Add(spr);
						}

						if (gender == GenderType.Both || gender == GenderType.Male) {
							_overrideGender = GenderType.Male;

							var act = preview.GetSpriteFromJob(tuple, this);
							var spr = act.ReplaceExtension(".spr");

							result.Add(act);
							result.Add(spr);
						}

						_overrideGender = null;
					}
					break;
				}
			}

			return result;
		}

		public void Read(ReadableTuple<int> tuple, GDbTab tab) {
			PreviewSprite = null;
			KeepPreviousPreviewPosition = true;
			RemoveJobs();
			RemoveError();
			List<Job> jobs;
			_lastTuple = tuple;
			_metaGrf = tab.ProjectDatabase.MetaGrf;
			_currentTab = tab;

			foreach (var preview in _previews) {
				if (preview.CanRead(tuple)) {
					if (_lastMatch != preview) {
						KeepPreviousPreviewPosition = false;
					}

					_lastMatch = preview;
					string job = tuple.GetValue<string>(ServerItemAttributes.ApplicableJob);
					jobs = JobList.GetJobsFromHex("0x" + ((job == "") ? "FFFFFFFF" : job), tuple.GetIntNoThrow(ServerItemAttributes.Upper));
					preview.Read(tuple, this, jobs);
					break;
				}
			}

			if (_listView.Items.Count > 0) {
				_listView.SelectedItem = PreferredJob;

				if (_listView.SelectedItem == null) {
					if (_oldJob != null)
						_listView.SelectedItem = _oldJob;

					if (_listView.SelectedItem == null)
						_listView.SelectedIndex = 0;
				}
			}
			else {
				_updatePreview(SpriteDefault);
			}

			if (!KeepPreviousPreviewPosition) {
				_selector.SetAction(_lastMatch.SuggestedAction);
			}
		}

		public void RemoveError() {
			_gridSpriteMissing.Visibility = Visibility.Collapsed;
			_tbSpriteMissing.Text = "";
		}

		public void SetError(string message) {
			if (_gridSpriteMissing == null) return;

			// The error needs to be removed to update the error again
			if (_gridSpriteMissing.Visibility != Visibility.Visible) {
				_gridSpriteMissing.Visibility = Visibility.Visible;
				_tbSpriteMissing.Text = message;
			}
		}

		public void AddError(string message) {
			_gridSpriteMissing.Visibility = Visibility.Visible;
			_tbSpriteMissing.Text += _tbSpriteMissing.Text == "" ? message : "\n" + message;
		}

		private void _jobChanged(object sender, SelectionChangedEventArgs e) {
			if (_gridSpriteMissing == null) return;
			Job job = _listView.SelectedItem as Job;
			if (job == null) return;
			RemoveError();

			try {
				_bodyReference = GetBodySprite(job, GenderString);
				_updatePreview(_lastMatch.GetSpriteFromJob(_lastTuple, this));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _updatePreview(string sprite) {
			byte[] headActionData;
			byte[] headSpriteData;
			_act = _emptyAct;
			_references.RemoveRange(2, _references.Count - 2);

			// Sprite has 3 states :
			// correct path - may not be found
			// null - do not update and show error
			// none - do not update and do not show error
			if (sprite == null) {
				AddError("Resource error : couldn't find the specified sprite.");
			}
			else if (sprite == SpriteDefault) {
				_bodyReference = DefaultBodyReference;
			}
			else if (sprite == SpriteNone) {
			}
			else {
				if (sprite.GetExtension() != null) {
					headActionData = Grf.GetData(sprite);
					headSpriteData = Grf.GetData(sprite.ReplaceExtension(".spr"));

					if (headActionData != null && headSpriteData != null)
						_act = new Act(headActionData, new Spr(headSpriteData));

					if (headActionData == null || headSpriteData == null) {
						SetError(String.Format("Resource error : sprite(s) not found \n{0} - {1}\n{2} - {3}",
							sprite, headActionData == null ? "#MISSING" : "#FOUND", sprite.ReplaceExtension(".spr"), headSpriteData == null ? "#MISSING" : "#FOUND"));
					}

					//sprite = sprite.ReplaceExtension(EncodingService.FromAnyToDisplayEncoding("_∞À±§.act"));

					//headActionData = Grf.GetData(sprite);
					//headSpriteData = Grf.GetData(sprite.ReplaceExtension(".spr"));
					//
					//if (headActionData != null && headSpriteData != null) {
					//	Act act = new Act(headActionData, new Spr(headSpriteData));
					//	act.AnchoredTo = _bodyReference;
					//	_references.Add(new ActReference { Act = act, Mode = ZMode.Front, Show = true });
					//}
				}
			}

			_headReference.AnchoredTo = _bodyReference;
			_act.AnchoredTo = _headReference;
			_selector.Load(_act);
			_frameViewer.SetGarmentMode(false);

			_headReference.Commands.UndoAll();
			_bodyReference.Commands.UndoAll();

			if (_act != null) {
				_act.Commands.UndoAll();
			}

			if (Job != null && Job.Name.StartsWith("Baby ")) {
				_headReference.Commands.Backup(a => a.Magnify(0.75f, true));
				_bodyReference.Commands.Backup(a => a.Magnify(0.75f, true));

				if (_act != null)
					_act.Commands.Backup(a => a.Magnify(0.75f, true));
			}

			if (_lastMatch is GarmentPreview) {
				_frameViewer.SetGarmentMode(true);
			}

			_frameViewer.Update();
		}
	}
}