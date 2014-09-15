using System.Collections.Generic;
using System.Linq;

namespace SDE.Tools.DatabaseEditor.Objects.Jobs {
	public class Job {
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