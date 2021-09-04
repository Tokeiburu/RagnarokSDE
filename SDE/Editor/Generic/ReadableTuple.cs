using Database;
using ErrorManager;
using SDE.Editor.Generic.Parsers.Generic;
using System;
using System.ComponentModel;
using Utilities;
using Utilities.Extension;
using Tuple = Database.Tuple;

namespace SDE.Editor.Generic
{
    /// <summary>
    /// Tuple view item (to be displayed in a list view)
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    public class ReadableTuple<TKey> : Tuple, INotifyPropertyChanged
    {
        public ReadableTuple(TKey key, AttributeList list) : base(key, list)
        {
        }

        public TKey Key
        {
            get { return GetKey<TKey>(); }
        }

        public override bool Default
        {
            get { return false; }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion INotifyPropertyChanged Members

        public override void OnTupleModified(bool value)
        {
            base.OnTupleModified(value);
            OnPropertyChanged("");
        }

        public int GetIntValue(int index)
        {
            return (int)GetValue(index);
        }

        // Using these methods are not allowed; they will crash the filter engines
        //public int GetIntValue(DbAttribute attribute) {
        //    return (int)GetValue(attribute.Index);
        //}
        //public string GetStringValue(DbAttribute attribute) {
        //    return (string)GetValue(attribute.Index);
        //}

        public string GetStringValue(int index)
        {
            return (string)GetValue(index);
        }

        public int GetIntNoThrow(DbAttribute attibute)
        {
            object obj = GetValue(attibute.Index);
            if (obj is int) return (int)obj;
            return FormatConverters.IntOrHexConverter((string)obj);
        }

        public int GetIntNoThrow(int index)
        {
            object obj = GetValue(index);
            if (obj is int) return (int)obj;
            return FormatConverters.IntOrHexConverter((string)obj);
        }

        public override void SetValue(DbAttribute attribute, object value)
        {
            bool sameValue;

            try
            {
                sameValue = GetValue(attribute.Index).ToString() == value.ToString();
            }
            catch
            {
                sameValue = false;
            }

            try
            {
                base.SetValue(attribute, value);
            }
            catch (Exception err)
            {
                DbIOErrorHandler.Handle(err, ("Failed to set or parse the value for [" + GetKey<TKey>() + "] at '" + attribute.DisplayName + "'. Value entered is : " + (value ?? "")).RemoveBreakLines(), ErrorLevel.NotSpecified);
                base.SetValue(attribute, attribute.Default);
            }

            if (!sameValue)
            {
                Modified = true;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}