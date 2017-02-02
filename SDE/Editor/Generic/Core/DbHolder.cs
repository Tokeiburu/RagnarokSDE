using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Generic.TabsMakerCore;

namespace SDE.Editor.Generic.Core {
	/// <summary>
	/// Instantiates all the tables and loads them. This class can
	/// also be used to load more tables after the instantiation.
	/// </summary>
	public class DbHolder {
		protected readonly List<BaseDb> _dbs = new List<BaseDb>();

		public SdeDatabase Database { get; private set; }

		public List<BaseDb> DBs {
			get { return _dbs; }
		}

		public virtual void Instantiate(SdeDatabase database) {
			Database = database;

			_dbs.Add(new DbClientResource());
			_dbs.Add(new DbItems());
			_dbs.Add(new DbItems2());
			_dbs.Add(new DbClientItems());
			_dbs.Add(new DbClientQuest());
			_dbs.Add(new DbClientCheevo());
			_dbs.Add(new DbItemCombos());
			_dbs.Add(new DbItemCombos2());
			//_dbs.Add(new DbItemGroups());
			_dbs.Add(new DbMobGroups());
			_dbs.Add(new DbSkills());
			_dbs.Add(new DbSkillRequirements());
			_dbs.Add(new DbMobs());
			_dbs.Add(new DbMobs2());
			_dbs.Add(new DbMobSkills());
			_dbs.Add(new DbMobSkills2());
			_dbs.Add(new DbQuest());
			_dbs.Add(new DbQuest2());
			_dbs.Add(new DbCheevo());
			_dbs.Add(new DbHomuns());
			_dbs.Add(new DbHomuns2());
			_dbs.Add(new DbPet());
			_dbs.Add(new DbPet2());
			_dbs.Add(new DbCastle());
			_dbs.Add(new DbCastle2());
			_dbs.Add(new DbConstants());

			_dbs.ForEach(p => p.Holder = this);
			_dbs.ForEach(p => p.Init(database));
		}

		public void AddTable(BaseDb db) {
			_dbs.Add(db);
			db.Holder = this;
			db.Init(Database);
		}

		public GDbTab GetTab(BaseDb db, TabControl control) {
			return db.IsGenerateTab ? db.GenerateTab(Database, control, db) : null;
		}

		public List<GDbTab> GetTabs(TabControl control) {
			return _dbs.Where(p => p.IsGenerateTab).Select(p => p.GenerateTab(Database, control, p)).ToList();
		}

		public void RemoveTable(BaseDb db) {
			Database.AllTables.Remove(db.DbSource);
			_dbs.Remove(db);
		}
	}
}