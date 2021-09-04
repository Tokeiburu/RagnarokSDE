using SDE.ApplicationConfiguration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using Utilities.Extension;

namespace SDE.View.Dialogs
{
    /// <summary>
    /// Interaction logic for ScriptEditDialog.xaml
    /// </summary>
    public partial class TimeEditDialog : TkWindow, IInputWindow
    {
        private readonly List<TextBox> _boxes = new List<TextBox>();
        private int _value;
        private bool _seconds;
        private readonly bool _allowFixed;

        public TimeEditDialog(string text, bool seconds = false, bool allowFixed = false)
            : base("Time edit", "cde.ico", SizeToContent.WidthAndHeight, ResizeMode.CanResize)
        {
            InitializeComponent();

            _value = text.ToInt();

            _boxes.Add(_tbMiliseconds);
            _boxes.Add(_tbSeconds);
            _boxes.Add(_tbMinutes);
            _boxes.Add(_tbHours);
            _boxes.Add(_tbDays);

            _seconds = seconds;
            _allowFixed = allowFixed;

            if (_allowFixed)
            {
                _cbFixedTime.Visibility = Visibility.Visible;
                WpfUtilities.AddFocus(_cbFixedTime);

                _tbMiliseconds.Visibility = Visibility.Collapsed;
                _lms.Visibility = Visibility.Collapsed;
                _lse.Content = "s";

                _upperGrid.ColumnDefinitions[9] = new ColumnDefinition { Width = new GridLength(0) };
                _upperGrid.ColumnDefinitions[10] = new ColumnDefinition { Width = new GridLength(0) };

                _upperGrid.Width = 300;

                bool isFixed = true;
                string day = "";
                string hour = "";
                string minute = "";
                string second = "";

                _extractTime(text, ref day, ref hour, ref minute, ref second, ref isFixed);

                _tbDays.Text = day;
                _tbHours.Text = hour;
                _tbMinutes.Text = minute;
                _tbSeconds.Text = second;
                _cbFixedTime.IsChecked = isFixed;

                _cbFixedTime.Checked += delegate
                {
                    _update();
                };

                _cbFixedTime.Unchecked += delegate
                {
                    _update();
                };
            }
            else if (seconds)
            {
                _tbMiliseconds.Visibility = Visibility.Collapsed;
                _lms.Visibility = Visibility.Collapsed;
                _lse.Content = "s";

                _upperGrid.ColumnDefinitions[9] = new ColumnDefinition { Width = new GridLength(0) };
                _upperGrid.ColumnDefinitions[10] = new ColumnDefinition { Width = new GridLength(0) };

                _upperGrid.Width = 300;

                _tbSeconds.Text = (_value % 60).ToString(CultureInfo.InvariantCulture);
                _tbMinutes.Text = (_value % 3600 / 60).ToString(CultureInfo.InvariantCulture);
                _tbHours.Text = (_value % 86400 / 3600).ToString(CultureInfo.InvariantCulture);
                _tbDays.Text = (_value / 86400).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                _tbHours.Visibility = Visibility.Collapsed;
                _tbDays.Visibility = Visibility.Collapsed;
                _lhr.Visibility = Visibility.Collapsed;
                _lda.Visibility = Visibility.Collapsed;

                _upperGrid.ColumnDefinitions[1] = new ColumnDefinition { Width = new GridLength(0) };
                _upperGrid.ColumnDefinitions[2] = new ColumnDefinition { Width = new GridLength(0) };
                _upperGrid.ColumnDefinitions[3] = new ColumnDefinition { Width = new GridLength(0) };
                _upperGrid.ColumnDefinitions[4] = new ColumnDefinition { Width = new GridLength(0) };

                _upperGrid.Width = 240;

                _tbMiliseconds.Text = (_value % 1000).ToString(CultureInfo.InvariantCulture);
                _tbSeconds.Text = (_value / 1000 % 60).ToString(CultureInfo.InvariantCulture);
                _tbMinutes.Text = (_value / 60000).ToString(CultureInfo.InvariantCulture);
            }

            _boxes.ForEach(_addEvents);

            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _tbMinutes.Loaded += delegate
            {
                _tbMinutes.Focus();
                _tbMinutes.SelectAll();
            };
        }

        private void _extractTime(string text, ref string day, ref string hour, ref string minute, ref string second, ref bool isFixed)
        {
            int value = 0;
            int index = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '+')
                {
                    isFixed = false;
                }
                else if (char.IsDigit(text[i]))
                {
                    value = value * 10 * index + (text[i] - '0');
                    index++;
                }
                else
                {
                    switch (text[i])
                    {
                        case 'd':
                            day = value.ToString(CultureInfo.InvariantCulture);
                            value = index = 0;
                            break;

                        case 'h':
                            hour = value.ToString(CultureInfo.InvariantCulture);
                            value = index = 0;
                            break;

                        case 'm':
                            if (i + 1 >= text.Length || text[i + 1] != 'n')
                                break;

                            minute = value.ToString(CultureInfo.InvariantCulture);
                            value = index = 0;
                            break;

                        case 's':
                            if (i + 1 >= text.Length || text[i + 1] != 'n')
                                break;

                            second = value.ToString(CultureInfo.InvariantCulture);
                            value = index = 0;
                            break;

                        default:
                            value = index = 0;
                            break;
                    }
                }
            }
        }

        public string Text
        {
            get
            {
                if (_allowFixed)
                {
                    return (_cbFixedTime.IsChecked == true ? "" : "+") +
                        (_tbDays.Text != "" ? _tbDays.Text + "d" : "") +
                        (_tbHours.Text != "" ? _tbHours.Text + "h" : "") +
                        (_tbMinutes.Text != "" ? _tbMinutes.Text + "mn" : "") +
                        (_tbSeconds.Text != "" ? _tbSeconds.Text + "s" : "")
                        ;
                }

                return _value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public Grid Footer { get { return _footerGrid; } }

        public event Action ValueChanged;

        public void OnValueChanged()
        {
            Action handler = ValueChanged;
            if (handler != null) handler();
        }

        private void _addEvents(TextBox cb)
        {
            cb.TextChanged += (e, a) => _update();
        }

        private void _update()
        {
            if (!_allowFixed)
            {
                int mil = _tbMiliseconds.Text.ToInt();
                int sec = _tbSeconds.Text.ToInt();
                int min = _tbMinutes.Text.ToInt();
                int hrs = _tbHours.Text.ToInt();
                int day = _tbDays.Text.ToInt();

                _value = min * 60000 + sec * 1000 + mil;

                if (_seconds)
                {
                    _value = day * 86400 + hrs * 3600 + min * 60 + sec;
                }
            }

            OnValueChanged();
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