using System;
using System.Collections.Generic;
using System.Linq;
using SDE.Editor.Generic.Lists;
using Utilities.Services;

namespace SDE.Editor.Jobs {
	public sealed class JobGroup {
		public static readonly JobGroup Normal2 = new JobGroup(1 << 0);
		public static readonly JobGroup Trans2 = new JobGroup(1 << 1);
		public static readonly JobGroup Baby2 = new JobGroup(1 << 2);
		public static readonly JobGroup Normal3 = new JobGroup(1 << 3);
		public static readonly JobGroup Trans3 = new JobGroup(1 << 4);
		public static readonly JobGroup Baby3 = new JobGroup(1 << 5);

		public static readonly JobGroup TransM = new JobGroup(Trans2 | Normal3 | Trans3 | Baby3);
		public static readonly JobGroup Trans = new JobGroup(Trans2 | Trans3);
		public static readonly JobGroup Baby = new JobGroup(Baby2 | Baby3);
		public static readonly JobGroup Renewal = new JobGroup(Normal3 | Trans3 | Baby3);
		public static readonly JobGroup PreRenewal = new JobGroup(Normal2 | Trans2 | Baby2);
		public static readonly JobGroup None = new JobGroup(0);
		public static readonly JobGroup All = new JobGroup(63);

		public int Id { get; private set; }

		private JobGroup(int id) {
			Id = id;
		}

		private JobGroup(JobGroup jobGroup) {
			Id = jobGroup.Id;
		}

		public bool IsSubset(Job job) {
			return (Id & job.Upper) == Id;
		}

		public bool IsSubset(JobGroup group) {
			return (Id & group.Id) == Id;
		}

		public bool IsOnlySubsetOf(JobGroup group) {
			return (Id & ~group.Id) == 0;
		}

		public bool IsOnlySubsetOf(int groupId) {
			return (Id & ~groupId) == 0;
		}

		public bool Is(JobGroup group) {
			return Id == group.Id;
		}

		public bool Is(int groupId) {
			return Id == groupId;
		}

		public static JobGroup Get(int id) {
			return new JobGroup(id);
		}

		public static JobGroup operator |(JobGroup group1, JobGroup group2) {
			return new JobGroup(group1.Id | group2.Id);
		}

		public bool IsBetween(JobGroup g1, JobGroup g2) {
			return IsOnlySubsetOf(g2) && !IsOnlySubsetOf(g1);
		}

		public string GetRestrictedString(Job groupedJob) {
			if (All.And(Id)) return "";

			// Trans restrictions
			if (!Baby2.And(Id) && !Normal2.And(Id) && Trans2.And(Id)) {
				if (Normal3.And(Id) || Trans3.And(Id) || Baby3.And(Id)) {
					if (Normal3.And(Id) && Trans3.And(Id) && Baby3.And(Id)) {
						if (groupedJob.Is(JobList.EveryRenewalJob))
							return "Trans 2nd Class or above ";

						// Only return 'Trans' because this is not restricted
						// to only 2nd jobs
						// Note: This is still not accurate, because a baby
						// third class could also wear this.
						return "Trans Class "; // Also includes all 3rd classes
					}

					// This section gets weird - because it shouldn't exist
					if (Normal3.And(Id)) {
						if (groupedJob.Is(JobList.EveryRenewalJob))
							return "Trans 2nd Class or above ";

						return "Trans Class "; // Also includes all 3rd classes
					}

					// Should not happen
					if (Trans3.And(Id) && !Normal3.And(Id) && !Baby3.And(Id)) {
						if (groupedJob.Is(JobList.EveryRenewalJob))
							return "Trans 2nd or 3rd Class ";

						return "Trans Class "; // Also includes all 3rd classes
					}

					// Should not happen
					if (Baby3.And(Id)) {
						// This is a contradiction...
						if (groupedJob.Is(JobList.EveryRenewalJob))
							return "Trans 2nd Class or Baby 3rd Class ";

						return "Trans Class or Baby 3rd Class ";
					}

					return "Trans Class "; // Also includes all 3rd classes
				}
				// 3rd classe are not included
				if (groupedJob.Is(JobList.EveryRenewalJob))
					return "Trans 2nd Class ";

				// Should not happen
				//return "Trans Class (excluding 3rd) ";
				return "Trans Class ";
			}

			// Baby restrictions - Shouldn't happen
			if (!Trans2.And(Id) && !Normal2.And(Id) && Baby2.And(Id)) {
				if (Normal3.And(Id) || Trans3.And(Id) || Baby3.And(Id)) {
					if (Normal3.And(Id) && Trans3.And(Id) && Baby3.And(Id)) {
						if (groupedJob.Is(JobList.EveryRenewalJob)) {
							return "Baby 2nd Class or above ";
						}

						return "Baby Class or above ";
					}

					if (Baby3.And(Id) && Normal3.And(Id)) {
						if (groupedJob.Is(JobList.EveryRenewalJob))
							return "Baby 2nd Class or above ";

						return "Baby Class or above ";
					}

					if (Baby3.And(Id) && !Normal3.And(Id) && !Trans3.And(Id)) {
						if (groupedJob.Is(JobList.EveryRenewalJob))
							return "Baby 2nd or 3rd Class ";

						return "Baby Class ";
					}

					if (Normal3.And(Id)) {
						if (groupedJob.Is(JobList.EveryRenewalJob))
							return "Baby 2nd Class or above ";

						return "Baby Class or above ";
					}

					if (Trans3.And(Id)) {
						if (groupedJob.Is(JobList.EveryRenewalJob))
							return "Baby 2nd Class or Trans 3rd Class ";

						return "Baby Class or Trans 3rd Class ";
					}

					return "Baby Class ";
				}
				if (groupedJob.Is(JobList.EveryRenewalJob))
					return "Baby 2nd Class ";

				//return "Baby Class (excluding 3rd) ";
				return "Baby Class ";
			}

			// 3rd restrictions (only)
			if (!Trans2.And(Id) && !Normal2.And(Id) && !Baby2.And(Id)) {
				// No such things as 2nd class here
				if (Renewal.And(Id)) {
					return "3rd Class ";
				}

				if (Trans3.Id == Id)
					return "Trans 3rd Class ";

				if (Baby3.Id == Id)
					return "Baby 3rd Class ";

				if (Normal3.And(Id) && Baby3.And(Id))
					return "3rd Class ";

				if (Normal3.Id == Id)
					return "3rd Class (excluding Baby) ";

				return "3rd Class ";
			}

			return null;
		}

		public bool And(int id) {
			return (this.Id & id) == this.Id;
		}

		public bool TryDetect(Job current, int gender, out string output) {
			output = "";

			if (IsOnlySubsetOf(Renewal)) {
				// Some of these can also be handled below
				if (current.Is(JobList.EveryRenewalJob)) {
					if (IsOnlySubsetOf(Baby3)) {
						output = JobList.GenderString(gender) + "Baby 3rd Class";
						return true;
					}

					if (IsOnlySubsetOf(Trans3)) {
						output = JobList.GenderString(gender) + "Trans 3rd Class";
						return true;
					}

					if (IsOnlySubsetOf(Normal3)) {
						output = JobList.GenderString(gender) + "Every 3rd Job (excluding Baby)";
						return true;
					}

					if (IsBetween(PreRenewal, Renewal)) {
						output = JobList.GenderString(gender) + "Every 3rd Job";
						return true;
					}
				}
			}

			if (IsOnlySubsetOf(Trans) || IsOnlySubsetOf(TransM)) {
				if (current.Is(JobList.EverySecondJobOld) || current.Is(JobList.EverySecondJob) ||
				    current.Is(JobList.EveryRenewalJob)) {
					if (IsOnlySubsetOf(Trans2)) {
						output = JobList.GenderString(gender) + "Every Trans 2nd Job";
						return true;
					}

					if (IsOnlySubsetOf(Trans3)) {
						output = JobList.GenderString(gender) + "Every Trans 3rd Job";
						return true;
					}

					if (IsOnlySubsetOf(Trans3) && IsOnlySubsetOf(Trans2)) {
						output = JobList.GenderString(gender) + "Every Trans 2nd or 3rd Job";
						return true;
					}

					output = JobList.GenderString(gender) + "Every Trans 2nd Job or above";
					return true;
				}

				if (JobList.EveryJobExceptNoviceOld.Is(current) || JobList.EveryJobExceptNovice.Is(current) ||
				    JobList.EveryTransJobExceptNoviceOld.Is(current) || JobList.EveryTransJobExceptNovice.Is(current)) {
					output = JobList.GenderString(gender) + "Every Trans Job except High Novice";
					return true;
				}

				if (JobList.EveryJobOld.Is(current) || JobList.EveryJob.Is(current) ||
				    JobList.EveryTransJobOld.Is(current) || JobList.EveryTransJob.Is(current)) {
					output = JobList.GenderString(gender) + "Every Trans Job";
					return true;
				}
			}

			if (IsOnlySubsetOf(Baby)) {
				if (current.Is(JobList.EveryRenewalJob)) {
					if (IsOnlySubsetOf(Baby2)) {
						output = JobList.GenderString(gender) + "Every Baby 2nd Job";
						return true;
					}

					if (IsOnlySubsetOf(Baby3)) {
						output = JobList.GenderString(gender) + "Every Baby 3rd Job";
						return true;
					}

					output = JobList.GenderString(gender) + "Every Baby 2nd or 3rd Job";
					return true;
				}

				if (JobList.EveryJobExceptNoviceOld.Is(current) || JobList.EveryJobExceptNovice.Is(current)) {
					output = JobList.GenderString(gender) + "Every Baby Job except Baby Novice";
					return true;
				}

				if (JobList.EveryJobOld.Is(current) || JobList.EveryJob.Is(current)) {
					output = JobList.GenderString(gender) + "Every Baby Job";
					return true;
				}
			}

			if (IsOnlySubsetOf(All)) {
				if (JobList.EveryJobExceptNoviceOld.Is(current) || JobList.EveryJobExceptNovice.Is(current)) {
					output = JobList.GenderString(gender) + "Every Job except Novice";
					return true;
				}

				if (JobList.EveryJobOld.Is(current) || JobList.EveryJob.Is(current)) {
					output = JobList.GenderString(gender) + "Every Job";
					return true;
				}

				if (JobList.EveryRenewalJob.Is(current)) {
					output = JobList.GenderString(gender) + "Every 2nd or 3rd Job";
					return true;
				}

				if (JobList.EverySecondJob.Is(current) || JobList.EverySecondJobOld.Is(current)) {
					output = JobList.GenderString(gender) + "Every 2nd or 3rd Job";
					return true;
				}
			}

			return false;
		}
	}

	public class Job {
		private readonly string _spriteName;

		protected bool Equals(Job other) {
			return Id == other.Id && Upper == other.Upper;
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Job)obj);
		}

		public override int GetHashCode() {
			unchecked {
				return (Id * 397) ^ Upper;
			}
		}

		public bool Normal {
			get { return true; }
		}

		public Job(int jobId, int upper, string[] names) : this(jobId, upper, names, null, null) {
		}

		public Job(int jobId, int upper, string[] names, Job parent) : this(jobId, upper, names, parent, null) {
		}

		public Job(int jobId, int upper, string[] names, string spriteName) : this(jobId, upper, names, null, spriteName) {
		}

		public Job(int jobId, int upper, string[] names, Job parent, string spriteName) {
			Id = jobId;
			Upper = upper;
			Name = names[0];
			Names = names.ToList();
			Parent = parent;
			_spriteName = spriteName;

			List<Job> parents = new List<Job>();

			if (parent != null) {
				parents.Add(parent);
				parents.AddRange(parent.Parents);
			}

			Parents = parents;

			if (JobList.IsOpened && JobList.AllJobs != null) {
				JobList.AllJobs.Add(this);
			}
		}

		public int Id { get; internal set; }
		public int Upper { get; private set; }
		public string Name { get; private set; }
		public Job Parent { get; private set; }

		public string SpriteName {
			get {
				if (_spriteName == null) return "";
				return EncodingService.FromAnsiToDisplayEncoding(_spriteName);
			}
		}

		public string GetSpriteName(GenderType gender) {
			var spriteName = SpriteName;

			if (spriteName.Contains(":")) {
				if (gender == GenderType.Female) {
					return spriteName.Split(':')[1];
				}
				return spriteName.Split(':')[0];
			}

			return spriteName;
		}

		public List<string> Names { get; private set; }
		public List<Job> Parents { get; private set; }

		public bool IsIgnoreUpper(Job job) {
			return job.Id == Id;
		}

		public bool Is(Job job) {
			return job.Id == Id;
		}

		public static bool operator ==(Job job1, Job job2) {
			if (ReferenceEquals(job1, job2)) return true;
			if (ReferenceEquals(job1, null)) return false;
			if (ReferenceEquals(job2, null)) return false;
			return job1.Id == job2.Id && job1.Upper == job2.Upper;
		}

		public static bool operator !=(Job job1, Job job2) {
			return !(job1 == job2);
		}

		public bool IsSubsetIgnoreUpper(Job current) {
			return (Id & current.Id) == Id;
		}

		public string GetName(int gender) {
			if (String.IsNullOrEmpty(Names[1]))
				return Name;
			if (gender == 1)
				return Name;
			if (gender == 0)
				return String.IsNullOrEmpty(Names[1]) ? Names[0] : Names[1];
			if (gender == 2) {
				return Names[0] + ", " + Names[1];
			}
			return Name;
		}

		public static Job Get(int id) {
			var job = JobList.AllJobs.FirstOrDefault(p => p.Id == id && p.Upper == 1);

			if (job != null) return job;

			return JobList.AllJobs.FirstOrDefault(p => p.Id == id);
		}

		public static Job Get(int id, JobGroup group) {
			Job job;

			if (_returnIf(id, group.Id, out job)) return job;

			if (group.IsOnlySubsetOf(JobGroup.Baby)) {
				if (group.IsOnlySubsetOf(JobGroup.Baby3))
					if (_returnIf(id, JobGroup.Baby3.Id, out job)) return job;
				if (_returnIf(id, JobGroup.Baby2.Id, out job)) return job;
			}

			if (group.Is(JobGroup.Renewal)) {
				if (_returnIf(id, JobGroup.Normal3.Id, out job)) return job;
			}

			if (group.Is(JobGroup.Trans3)) {
				if (_returnIf(id, JobGroup.Trans3.Id, out job)) return job;
			}

			if (group.Is(JobGroup.TransM)) {
				if (_returnIf(id, JobGroup.Trans2.Id, out job)) return job;
			}

			if (group.Is(JobGroup.Trans2)) {
				if (_returnIf(id, JobGroup.Trans2.Id, out job)) return job;
			}

			if (group.Is(JobGroup.Trans)) {
				if (_returnIf(id, JobGroup.Trans2.Id, out job)) return job;
			}

			if (group.IsBetween(JobGroup.PreRenewal, JobGroup.Renewal)) {
				if (_returnIf(id, JobGroup.Normal3.Id, out job)) return job;
			}

			if (_returnIf(id, JobGroup.Normal2.Id, out job)) return job;

			return Get(id);
		}

		public override string ToString() {
			return Name;
		}

		private static bool _returnIf(int id, int upper, out Job job) {
			job = JobList.AllJobs.FirstOrDefault(p => p.Id == id && p.Upper == upper);

			if (job == null)
				return false;
			return true;
		}

		public static string[] Jobs = {
			"--- Specials and generic ---",
			"Every Job",
			"Every Rebirth Job",
			"Every 2nd Job or above",
			"Every 3rd Job",
			"Every Job Except Novice",
			"Every Rebirth Job except High Novice",
			"Every Rebirth Job or above",
			"Female Only, Every Job",
			"Male Only, Every Job",
			"",
			"--- 1st Job Class ---",
			"Acolyte Class",
			"Archer Class",
			"Mage Class",
			"Merchant Class",
			"Swordman Class",
			"Thief Class",
			"",
			"--- 2nd Job Class ---",
			"Alchemist",
			"Assassin",
			"Bard",
			"Blacksmith",
			"Crusader",
			"Dancer",
			"Hunter",
			"Knight",
			"Monk",
			"Priest",
			"Rogue",
			"Sage",
			"Wizard",
			"",
			"--- Rebirth 2nd Job Class ---",
			"Assassin Cross",
			"Biochemist",
			"Champion",
			"Clown",
			"Gypsy",
			"High Priest",
			"High Wizard",
			"Lord Knight",
			"Paladin",
			"Scholar",
			"Sniper",
			"Stalker",
			"Whitesmith",
			"",
			"--- 3rd Job Class ---",
			"Arch Bishop",
			"Geneticist",
			"Guillotine Cross",
			"Maestro",
			"Mechanic",
			"Ranger",
			"Royal Guard",
			"Rune Knight",
			"Shadow Chaser",
			"Sorcerer",
			"Sura",
			"Wanderer",
			"Warlock",
			"",
			"--- Extended Class ---",
			"Gunslinger",
			"Kagerou",
			"Ninja",
			"Oboro",
			"Soul Linker",
			"Super Novice",
			"Taekwon",
			"Taekwon Master"
		};
	}
}