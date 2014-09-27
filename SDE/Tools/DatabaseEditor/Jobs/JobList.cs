using System;
using System.Collections.Generic;
using System.Linq;

namespace SDE.Tools.DatabaseEditor.Jobs {
	public class JobList {
		//public static Job Gangsi = new Job(1 << 26, new string[] { "Gangsi", null }, null);
		//public static Job DeathKnight = new Job(1 << 27, new string[] { "Death Knight", null }, null);
		//public static Job DarkCollector = new Job(1 << 28, new string[] { "Dark Collector", null }, null);
		//public static Job Kangerou = new Job(1 << 29, new string[] { "Kagerou", "Oboro" }, null);
		//public static Job Rebellion = new Job(1 << 30, new string[] { "Rebellion" }, null);

		public static Job AllJobOld = new Job((((1 << 26) - 1) ^ (1 << 13)) ^ (1 << 20), new string[] { "All Jobs " }, null);

		public static List<Job> AllJobs = new List<Job>();

		public static Job Novice = new Job(1 << 0, new string[] { "Novice", null });
		public static Job HighNovice = new Job(1 << 0, new string[] { "High Novice", null });

		// 1st jobs
		public static Job Swordman = new Job(1 << 1, new string[] { "Swordman", "Swordsman" }, Novice);
		public static Job Mage = new Job(1 << 2, new string[] { "Mage", null }, Novice);
		public static Job Archer = new Job(1 << 3, new string[] { "Archer", null }, Novice);
		public static Job Acolyte = new Job(1 << 4, new string[] { "Acolyte", null }, Novice);
		public static Job Merchant = new Job(1 << 5, new string[] { "Merchant", null }, Novice);
		public static Job Thief = new Job(1 << 6, new string[] { "Thief", null }, Novice);

		// 2nd_1 jobs
		public static Job Knight = new Job(1 << 7, new string[] { "Knight", null }, Swordman);
		public static Job Priest = new Job(1 << 8, new string[] { "Priest", null }, Acolyte);
		public static Job Wizard = new Job(1 << 9, new string[] { "Wizard", null }, Mage);
		public static Job Blacksmith = new Job(1 << 10, new string[] { "Blacksmith", null }, Merchant);
		public static Job Hunter = new Job(1 << 11, new string[] { "Hunter", null }, Archer);
		public static Job Assassin = new Job(1 << 12, new string[] { "Assassin", null }, Thief);

		// 2nd_1 trans jobs
		public static Job LordKnight = new Job(1 << 7, new string[] { "Lord Knight", null }, Swordman);
		public static Job HighPriest = new Job(1 << 8, new string[] { "High Priest", null }, Priest);
		public static Job HighWizard = new Job(1 << 9, new string[] { "High Wizard", null }, Wizard);
		public static Job Whitesmith = new Job(1 << 10, new string[] { "Whitesmith", null }, Blacksmith);
		public static Job Sniper = new Job(1 << 11, new string[] { "Sniper", null }, Hunter);
		public static Job AssassinCross = new Job(1 << 12, new string[] { "Assassin Cross", null }, Assassin);

		// 3rd_1 jobs
		public static Job RuneKnight = new Job(1 << 7, new string[] { "Rune Knight", null }, LordKnight);
		public static Job ArchBishop = new Job(1 << 8, new string[] { "Arch Bishop", null }, HighPriest);
		public static Job Warlock = new Job(1 << 9, new string[] { "Warlock", null }, HighWizard);
		public static Job Mechanic = new Job(1 << 10, new string[] { "Mechanic", null }, Whitesmith);
		public static Job Ranger = new Job(1 << 11, new string[] { "Ranger", null }, Sniper);
		public static Job GuillotineCross = new Job(1 << 12, new string[] { "Guillotine Cross", null }, AssassinCross);

		// 2nd_2 jobs
		public static Job Crusader = new Job(1 << 14, new string[] { "Crusader", null }, Swordman);
		public static Job Monk = new Job(1 << 15, new string[] { "Monk", null }, Acolyte);
		public static Job Sage = new Job(1 << 16, new string[] { "Sage", null }, Mage);
		public static Job Rogue = new Job(1 << 17, new string[] { "Rogue", null }, Thief);
		public static Job Alchemist = new Job(1 << 18, new string[] { "Alchemist", null }, Merchant);
		public static Job BardDancer = new Job(1 << 19, new string[] { "Bard", "Dancer" }, Archer);

		// 2nd_2 trans jobs
		public static Job Paladin = new Job(1 << 14, new string[] { "Paladin", null }, Crusader);
		public static Job Champion = new Job(1 << 15, new string[] { "Champion", null }, Monk);
		public static Job Professor = new Job(1 << 16, new string[] { "Professor", null }, Sage);
		public static Job Stalker = new Job(1 << 17, new string[] { "Stalker", null }, Rogue);
		public static Job Creator = new Job(1 << 18, new string[] { "Creator", null }, Alchemist);
		public static Job ClowyGypsy = new Job(1 << 19, new string[] { "Clown", "Gypsy" }, BardDancer);

		// 3rd_2 jobs
		public static Job RoyalGuard = new Job(1 << 14, new string[] { "Royal Guard", null }, Paladin);
		public static Job Shura = new Job(1 << 15, new string[] { "Shura", null }, Champion);
		public static Job Sorcerer = new Job(1 << 16, new string[] { "Sorcerer", null }, Professor);
		public static Job ShadowChaser = new Job(1 << 17, new string[] { "Shadow Chaser", null }, Stalker);
		public static Job Generic = new Job(1 << 18, new string[] { "Generic", null }, Creator);
		public static Job MinstrelWanderer = new Job(1 << 19, new string[] { "Minstrel", "Wanderer" }, ClowyGypsy);

		// Extended jobs
		public static Job Taekwon = new Job(1 << 21, new string[] { "Taekwon", "TaeKwon" }, Novice);
		public static Job StarGladiator = new Job(1 << 22, new string[] { "Star Gladiator", null }, Taekwon);
		public static Job SoulLinker = new Job(1 << 23, new string[] { "Soul Linker", null }, Taekwon);
		public static Job Gunslinger = new Job(1 << 24, new string[] { "Gunslinger", null }, Novice);
		public static Job Ninja = new Job(1 << 25, new string[] { "Ninja", null }, Novice);

		public static string GetJobsFromHex(string hex) {
			int hexValue = Convert.ToInt32(hex, 16);

			List<Job> jobs = new List<Job>();

			if (hexValue < 0) {
				hexValue ^= -1;

				jobs.AddRange(AllJobs.Where(job => (job.Id & hexValue) == job.Id));
				jobs = AllJobs.Where(p => jobs.All(q => p.Id != q.Id)).ToList();
				hexValue = jobs.Aggregate(0, (current, job) => current | job.Id);
			}
			else {
				jobs.AddRange(AllJobs.Where(job => (job.Id & hexValue) == job.Id));
			}

			//if (AllJobs.Where(p => p.Parents.Count == 2).All())

			// We start off by making groups with classes
			int archerClass = Archer.Id | Hunter.Id | BardDancer.Id;
			int swordman = Swordman.Id | Crusader.Id | Knight.Id;
			int mage = Mage.Id | Wizard.Id | Sage.Id;
			int acolyte = Acolyte.Id | Priest.Id | Monk.Id;
			int merchant = Merchant.Id | Blacksmith.Id | Alchemist.Id;
			int thief = Thief.Id | Assassin.Id | Rogue.Id;
			int taekwon = Taekwon.Id | StarGladiator.Id | SoulLinker.Id;

			int everyClassExceptNovice = archerClass | swordman | mage | acolyte | merchant | thief | taekwon | Gunslinger.Id | Ninja.Id;

			if (hexValue == everyClassExceptNovice)
				return "Every Job Except Novice";

			if (hexValue == everyClassExceptNovice + 1)
				return "Every Job";

			string output = "";

			if ((hexValue & 1) == 1) {
				output += "Novice, ";
			}
			output = _checkFor(output, hexValue, Swordman.Id, Knight.Id, Crusader.Id);
			output = _checkFor(output, hexValue, Mage.Id, Wizard.Id, Sage.Id);
			output = _checkFor(output, hexValue, Archer.Id, Hunter.Id, BardDancer.Id);
			output = _checkFor(output, hexValue, Acolyte.Id, Priest.Id, Monk.Id);
			output = _checkFor(output, hexValue, Merchant.Id, Blacksmith.Id, Alchemist.Id);
			output = _checkFor(output, hexValue, Thief.Id, Assassin.Id, Rogue.Id);
			output = _checkFor(output, hexValue, Taekwon.Id, StarGladiator.Id, SoulLinker.Id);

			output = _checkFor(output, hexValue, Gunslinger);
			output = _checkFor(output, hexValue, Ninja);
			//output = _checkFor(output, hexValue, Gangsi);
			//output = _checkFor(output, hexValue, DeathKnight);
			//output = _checkFor(output, hexValue, DarkCollector);
			//output = _checkFor(output, hexValue, Kangerou);
			//output = _checkFor(output, hexValue, Rebellion);

			return output.Trim(new char[] { ' ', ',' }).Trim(',');
		}

		private static string _checkFor(string output, int hexValue, Job job) {
			if ((hexValue & job.Id) == job.Id) {
				output += job.Name + ", ";
			}

			return output;
		}

		private static string _checkFor(string output, int hexValue, int parentId, int child1Id, int child2Id) {
			int familyId = parentId | child1Id | child2Id;

			if ((hexValue & familyId) == familyId) {
				string toAdd = AllJobs.First(p => p.Id == parentId).Name + " Class, ";

				if (output.EndsWith(" Class, ")) {
					return output + toAdd;
				}

				if (!output.Contains(" Class, ")) {
					if (output.StartsWith("Novice, ")) {
						return output.Insert("Novice, ".Length, toAdd);
					}
					return toAdd + output;
				}

				int lastIndex = output.LastIndexOf(" Class, ", StringComparison.Ordinal);
				return output.Insert(lastIndex + " Class, ".Length, toAdd);
			}

			if ((hexValue & child1Id) == child1Id && (hexValue & child2Id) == child2Id) {
				return output + AllJobs.First(p => p.Id == child1Id).Name + ", " + AllJobs.First(p => p.Id == child2Id).Name + ", ";
			}

			if ((hexValue & child1Id) == child1Id && (hexValue & parentId) == parentId) {
				return output + AllJobs.First(p => p.Id == parentId).Name + ", " + AllJobs.First(p => p.Id == child1Id).Name + ", ";
			}
			
			if ((hexValue & child2Id) == child2Id && (hexValue & parentId) == parentId) {
				return output + AllJobs.First(p => p.Id == parentId).Name + ", " + AllJobs.First(p => p.Id == child2Id).Name + ", ";
			}
			
			if ((hexValue & child1Id) == child1Id) {
				return output + AllJobs.First(p => p.Id == child1Id).Name + ", ";
			}
			
			if ((hexValue & child2Id) == child2Id) {
				return output + AllJobs.First(p => p.Id == child2Id).Name + ", ";
			}
			
			if ((hexValue & parentId) == parentId) {
				return output + AllJobs.First(p => p.Id == parentId).Name + ", ";
			}

			return output;
		}

		public static string GetHexJob(string value) {
			int result = GetApplicableJobs(value).Aggregate(0, (current, job) => current | job.Id);

			if (result == AllJobOld.Id) {
				return String.Format("0x{0:X8}", -1);
			}

			return String.Format("0x{0:X8}", result);
		}

		public static string GetHexJob(List<Job> jobs) {
			int result = jobs.Aggregate(0, (current, job) => current | job.Id);
			return String.Format("0x{0:X8}", result);
		}

		public static string GetHexJobReverse(string value) {
			List<Job> jobs = GetApplicableJobs(value);

			List<Job> notApplicableJobs = AllJobs.Where(p => p.Parents.Contains(Novice)).Concat(new Job[] { Novice }).Where(p => !jobs.Contains(p)).ToList();

			int result = notApplicableJobs.Aggregate(0, (current, job) => current | job.Id);
			result ^= -1;
			return String.Format("0x{0:X8}", result);
		}

		private static bool _isClass(string value) {
			return value.EndsWith(" Class");
		}

		private static bool _isClassRestricted(string value) {
			return value.EndsWith(" Only");
		}

		private static IEnumerable<Job> _getAllChildrenJobs(string value) {
			return AllJobs.Where(p => p.Parents.Any(q => q.Names.Contains(value)));
		}

		private static IEnumerable<Job> _getAllSecondJobs() {
			return AllJobs.Where(p => p.Parents.Count >= 2);
		}

		private static IEnumerable<Job> _getAllThirdJobs() {
			return AllJobs.Where(p => p.Parents.Count >= 3);
		}

		private static bool _compare(string val1, string val2) {
			return String.Compare(val1, val2, StringComparison.OrdinalIgnoreCase) == 0;
		}

		public static List<Job> GetApplicableJobs(string value) {
			string[] stringJobs = value.Split(',').Select(p => p.Trim()).ToArray();
			List<Job> jobs = new List<Job>();
			string previousJob = null;

			foreach (string stringJob in stringJobs) {
				if (_isClass(stringJob)) {
					string metaJob = stringJob.Replace("Trans ", "").Replace(" Class", "");
					previousJob = metaJob;

					if (metaJob == "2nd") {
						jobs.AddRange(_getAllSecondJobs());
					}
					else if (metaJob == "3rd") {
						jobs.AddRange(_getAllThirdJobs());
					}
					else {
						metaJob = metaJob.Replace("2nd ", "").Replace("3rd ", "");
						jobs.AddRange(_getAllChildrenJobs(metaJob).Concat(new Job[] { GetJob(metaJob) }));
					}
				}
				else if (_isClassRestricted(stringJob)) {
					jobs.Add(GetJob(stringJob.Replace(" Only", "").Replace("2nd ", "").Replace("3rd ", "").Replace("Trans ", "")));
				}
				else if (
					_compare(stringJob, "Every 2nd Class") ||
					_compare(stringJob, "Rebirth 2nd Class") ||
					_compare(stringJob, "Trans 2nd Class") ||
					_compare(stringJob, "Every 2nd Class or above") ||
					_compare(stringJob, "Rebirth 2nd Class or above") ||
					_compare(stringJob, "Trans 2nd Class or above")
					) {
					jobs.AddRange(AllJobs.Where(p => p.Parents.Count == 2));
					previousJob = null;
				}
				else if (
					_compare(stringJob, "Every 2nd Job or above") ||
					_compare(stringJob, "Every 3rd Job or above") ||
					_compare(stringJob, "Every Trans Job or above") ||
					_compare(stringJob, "Every Rebirth Job or above") ||
					_compare(stringJob, "Every 2nd Job") ||
					_compare(stringJob, "Every 3rd Job") ||
					_compare(stringJob, "Every Trans Job")  ||
					_compare(stringJob, "Every Rebirth Job") ||
					_compare(stringJob, "Every Job") || 
					_compare(stringJob, "All Job") || 
					_compare(stringJob, "All Jobs")) {
					jobs.AddRange(_getAllChildrenJobs("Novice").Concat(new Job[] { GetJob("Novice") }));
					previousJob = null;
				}
				else if (
					_compare(stringJob, "Every 2nd Job Except Novice") ||
					_compare(stringJob, "Every 3rd Job Except Novice") ||
					_compare(stringJob, "Every Trans Job Except Novice") ||
					_compare(stringJob, "Every Rebirth Job Except Novice") ||
					_compare(stringJob, "Every Job Except Novice") ||
					_compare(stringJob, "All Job Except Novice")  ||
					_compare(stringJob, "All Except Novice")  ||
					_compare(stringJob, "Every Rebirth Job Except Novice")  ||
					_compare(stringJob, "Every Rebirth Job except High Novice")) {
					jobs.AddRange(AllJobs.Where(p => p.Parents.Contains(Novice)));
					previousJob = null;
				}
				else {
					Job currentJob = GetJob(stringJob);

					if (currentJob == null)
						continue;

					if (previousJob != null &&
						currentJob.Parent != null &&
						GetJob(previousJob) != null &&
						currentJob.Parent.Id == GetJob(previousJob).Id) {

						List<Job> newJobs = new List<Job>();

						foreach (Job job in jobs) {
							if (job.Parent == null) {
								newJobs.Add(job);
							}
							else if (job.Parents.All(p => p.Id != currentJob.Parent.Id)) {
								newJobs.Add(job);
							}
						}

						newJobs.Add(currentJob);

						jobs = newJobs;
						previousJob = null;
					}
					else {
						jobs.Add(currentJob);
					}
				}

				jobs = jobs.Where(p => p != null).ToList();
			}

			return jobs.Distinct().ToList();
		}

		public static Job GetJob(string stringJob) {
			return AllJobs.FirstOrDefault(job => (job.Names.Any(p => p == stringJob)));
		}
	}
}
