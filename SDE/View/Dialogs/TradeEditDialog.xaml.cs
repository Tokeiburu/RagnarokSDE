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
    public partial class TradeEditDialog : TkWindow, IInputWindow
    {
        private readonly List<CheckBox> _boxes = new List<CheckBox>();
        private readonly List<RadioButton> _radios = new List<RadioButton>();
        private int _flag;
        private int _eventId;
        private bool _rbEvents;

        public TradeEditDialog(ReadableTuple<int> tuple) : base("Trade edit", "cde.ico", SizeToContent.Height, ResizeMode.CanResize)
        {
            InitializeComponent();

            ToolTipsBuilder.Initialize(new string[] {
                "Item can't be droped",
                "Item can't be traded (nor vended)",
                "Wedded partner can override restriction 2.",
                "Item can't be sold to npcs",
                "Item can't be placed in the cart",
                "Item can't be placed in the storage",
                "Item can't be placed in the guild storage",
                "Item can't be attached to mail",
                "Item can't be auctioned"
            }, this);

            _flag = tuple.GetIntNoThrow(ServerItemAttributes.TradeFlag);

            _cbUpper1.Tag = 1 << 0;
            _cbUpper2.Tag = 1 << 1;
            _cbUpper3.Tag = 1 << 2;
            _cbUpper4.Tag = 1 << 3;
            _cbUpper5.Tag = 1 << 4;
            _cbUpper6.Tag = 1 << 5;
            _cbUpper7.Tag = 1 << 6;
            _cbUpper8.Tag = 1 << 7;
            _cbUpper9.Tag = 1 << 8;

            _rbMet1.Tag = 467;
            _rbMet2.Tag = 475;
            _rbMet3.Tag = 483;
            _rbMet4.Tag = 491;
            _rbMet5.Tag = 507;

            _boxes.Add(_cbUpper1);
            _boxes.Add(_cbUpper2);
            _boxes.Add(_cbUpper3);
            _boxes.Add(_cbUpper4);
            _boxes.Add(_cbUpper5);
            _boxes.Add(_cbUpper6);
            _boxes.Add(_cbUpper7);
            _boxes.Add(_cbUpper8);
            _boxes.Add(_cbUpper9);

            _radios.Add(_rbMet1);
            _radios.Add(_rbMet2);
            _radios.Add(_rbMet3);
            _radios.Add(_rbMet4);
            _radios.Add(_rbMet5);

            _eventId = 0;

            _boxes.ForEach(_addEvents);
            _radios.ForEach(_addEvents);

            _rbEvents = true;
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

        private void _addEvents(RadioButton rb)
        {
            rb.Checked += delegate
            {
                if (!_rbEvents)
                    return;

                _rbEvents = false;

                _radios.ForEach(delegate (RadioButton p)
                {
                    if (p != rb)
                        p.IsChecked = false;
                });

                foreach (var cb in _boxes)
                {
                    if (((int)cb.Tag & (int)rb.Tag) == (int)cb.Tag)
                    {
                        cb.IsChecked = true;
                    }
                    else
                    {
                        cb.IsChecked = false;
                    }
                }

                _rbEvents = true;
            };

            if ((int)rb.Tag == _flag)
                rb.IsChecked = true;
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

                foreach (var rb in _radios)
                {
                    if ((int)rb.Tag == _flag)
                        rb.IsChecked = true;
                }

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