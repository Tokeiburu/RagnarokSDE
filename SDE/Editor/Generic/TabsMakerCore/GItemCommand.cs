using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Database;
using Database.Commands;

namespace SDE.Editor.Generic.TabsMakerCore {
	/// <summary>
	/// Custom menu item to add to a tab's list view
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	public class GItemCommand<TKey, TValue> where TValue : Tuple {
		#region Delegates
		public delegate void GenericCommandDelegate(List<TValue> toList);
		#endregion

		public GenericCommandDelegate GenericCommand;
		private bool _addToCommandsStack = true;
		private Visibility _visibility = Visibility.Visible;

		public string DisplayName { get; set; }
		public string ImagePath { get; set; }
		public int InsertIndex { get; set; }
		public bool AllowMultipleSelection { get; set; }
		public bool RemoveAfterCommand { get; set; }
		public Func<TValue, ITableCommand<TKey, TValue>> Command { get; set; }

		public bool AddToCommandsStack {
			get { return _addToCommandsStack; }
			set { _addToCommandsStack = value; }
		}

		public KeyGesture Shortcut { get; set; }

		public Visibility Visibility {
			get { return _visibility; }
			set { _visibility = value; }
		}
	}
}