using Database;
using System;
using System.Windows.Controls;
using Utilities;

namespace SDE.Editor.Generic.TabsMakerCore
{
    /// <summary>
    /// This class saves the search settings to the current ConfigAsker
    /// associated with the project.
    /// </summary>
    public class GSearchSettings
    {
        #region Delegates

        public delegate void SearchSettingsEventHandler(object sender);

        #endregion Delegates

        public const string TupleAdded = "Tuple added";
        public const string TupleModified = "Tuple modified";
        public const string TupleRange = "Tuple range";
        public const string Mode = "Mode";

        private readonly ConfigAsker _configAsker;
        private readonly string _settingsGroupName;

        public GSearchSettings(ConfigAsker configAsker, string settingsGroupName)
        {
            _configAsker = configAsker;
            _settingsGroupName = settingsGroupName;
        }

        public ConfigAsker ConfigAsker
        {
            get { return _configAsker; }
        }

        public bool this[DbAttribute attribute]
        {
            get { return this[attribute.DisplayName]; }
            set { this[attribute.DisplayName] = value; }
        }

        public bool this[string attribute]
        {
            get { return Boolean.Parse(_configAsker["[Search settings - " + _settingsGroupName + " - " + attribute + "]", false.ToString()]); }
            set
            {
                _configAsker["[Search settings - " + _settingsGroupName + " - " + attribute + "]"] = value.ToString();
                OnModified();
            }
        }

        public event SearchSettingsEventHandler Modified;

        public bool Link(CheckBox box, DbAttribute attribute, bool? defaultValue = null)
        {
            return Link(box, attribute.DisplayName, defaultValue);
        }

        public bool Link(CheckBox box, string attribute, bool? defaultValue = null)
        {
            if (defaultValue != null)
            {
                box.IsChecked = defaultValue;
                this[attribute] = defaultValue == true;
            }
            else
            {
                box.IsChecked = this[attribute];
            }

            box.Checked += (e, args) => this[attribute] = true;
            box.Unchecked += (e, args) => this[attribute] = false;
            return true;
        }

        public virtual void OnModified()
        {
            if (Modified != null)
                Modified(this);
        }

        public void Set(string mode, object value)
        {
            _configAsker["[Search settings - " + _settingsGroupName + " - " + mode + "]"] = value.ToString();
            OnModified();
        }

        public void Set(DbAttribute mode, object value)
        {
            Set(mode.DisplayName, value);
        }

        public string Get(DbAttribute mode)
        {
            return Get(mode.DisplayName);
        }

        public string Get(string mode)
        {
            return _configAsker["[Search settings - " + _settingsGroupName + " - " + mode + "]"];
        }

        internal void InternalClear()
        {
            _configAsker.DeleteKeys("[Search settings - " + _settingsGroupName);
        }
    }
}