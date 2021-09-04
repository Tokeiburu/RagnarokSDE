using ErrorManager;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;

namespace SDE.View.Dialogs
{
    /// <summary>
    /// Interaction logic for ScriptEditDialog.xaml
    /// </summary>
    public partial class NouseEditDialog : TkWindow, IInputWindow
    {
        private readonly List<CheckBox> _boxes = new List<CheckBox>();
        private int _eventId;
        private int _flag;

        public NouseEditDialog(ReadableTuple<int> tuple)
            : base("NoUse edit", "cde.ico", SizeToContent.Height, ResizeMode.CanResize)
        {
            InitializeComponent();

            ToolTipsBuilder.Initialize(new string[] {
                "Cannot use the item while sitting."
            }, this);

            _flag = tuple.GetIntNoThrow(ServerItemAttributes.NoUseFlag);

            _cbUpper1.Tag = 1 << 0;

            _boxes.Add(_cbUpper1);

            _eventId = 0;
            _boxes.ForEach(_addEvents);

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        public string Text
        {
            get { return _flag.ToString(CultureInfo.InvariantCulture); }
        }

        public Grid Footer { get { return _footerGrid; } }

        public event Action ValueChanged;

        public void OnValueChanged()
        {
            Action handler = ValueChanged;
            if (handler != null) handler();
        }

        private void _addEvents(CheckBox cb)
        {
            ToolTipsBuilder.SetupNextToolTip(cb, this);
            cb.IsChecked = (_flag & (1 << _eventId)) == (1 << _eventId);

            cb.Checked += (e, a) => _update();
            cb.Unchecked += (e, a) => _update();

            WpfUtils.AddMouseInOutEffectsBox(cb);
            _eventId++;
        }

        private void _update()
        {
            try
            {
                int flag = 0;

                foreach (CheckBox box in _boxes)
                {
                    if (box.IsChecked == true)
                    {
                        flag += (int)box.Tag;
                    }
                }

                _flag = flag;
                OnValueChanged();
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void _buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void _buttonOk_Click(object sender, RoutedEventArgs e)
        {
            if (!SdeAppConfiguration.UseIntegratedDialogsForFlags)
                DialogResult = true;
            Close();
        }
    }
}