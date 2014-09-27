using System.Collections.Generic;
using System.Linq;

namespace SDE.Tools.DatabaseEditor.Jobs {
	public class Job {
		public static string[] Jobs = {
			"--- Specials and generic ---",
			"Novice",
			"Every Job",
			"Every 2nd Job or above",
			"Every 3rd Job",
			"Every Job Except Novice",
			"Every Rebirth Job except High Novice",
			"Every Rebirth Job or above",
			"Every 2nd Class or above",
			"Rebirth 2nd Class or above",
			" ",
			"--- 1st Job Class ---",
			"Acolyte Class",
			"Archer Class",
			"Mage Class",
			"Merchant Class",
			"Swordman Class",
			"Thief Class",
			"  ",
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
			"   ",
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
			"    ",
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
			"     ",
			"--- Extended Class ---",
			"Gunslinger",
			"Ninja",
			"Soul Linker",
			"Super Novice",
			"Taekwon",
			"Taekwon Master",
			"Gangsi",
			"Death Knight",
			"Dark Collector",
			"Kagerou",
			"Oboro",
			"Rebellion"
		};

		public Job(int id, string[] names, Job parent = null) {
			Id = id;
			Name = names[0];
			Names = names.ToList();
			Parent = parent;

			List<Job> parents = new List<Job>();

			if (parent != null) {
				parents.Add(parent);
				parents.AddRange(parent.Parents);
			}

			Parents = parents;

			if (JobList.AllJobs != null) {
				JobList.AllJobs.Add(this);
			}
		}

		public int Id { get; internal set; }
		public string Name { get; private set; }
		public Job Parent { get; private set; }
		public List<string> Names { get; private set; }
		public List<Job> Parents { get; private set; }
	}
}