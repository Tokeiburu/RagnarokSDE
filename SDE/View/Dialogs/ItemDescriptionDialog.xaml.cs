using SDE.Editor;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Items;
using SDE.Editor.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;

namespace SDE.View.Dialogs
{
    /// <summary>
    /// Interaction logic for ItemDescriptionDialog.xaml
    /// </summary>
    public partial class ItemDescriptionDialog : TkWindow
    {
        private bool _eventsDisabled = true;
        private ReadableTuple<int> _item;
        private bool _enableSubTypeEvents = true;

        public event RoutedEventHandler Apply;

        public void OnApply(RoutedEventArgs e)
        {
            RoutedEventHandler handler = Apply;
            if (handler != null) handler(this, e);
        }

        public ItemDescriptionDialog() : base("Item description", "properties.png")
        {
            InitializeComponent();
            _loadComboBoxes();
        }

        public ItemDescriptionDialog(ReadableTuple<int> item) : base("Item description", "properties.png")
        {
            InitializeComponent();
            _loadComboBoxes();
            LoadItem(item);
        }

        public string Output { get; set; }

        public ReadableTuple<int> Item { get; private set; }

        public bool? Result
        {
            get;
            set;
        }

        public void LoadItem(ReadableTuple<int> item)
        {
            _item = item;
            _eventsDisabled = true;

            _clearFields();

            _onlyShow("Class", "Weight");

            if (item != null)
            {
                foreach (var keyPair in item.GetValue<ParameterHolder>(ClientItemAttributes.Parameters).Values)
                {
                    switch (keyPair.Key)
                    {
                        case "Description":
                            _tbDescription.Text = ParameterHolder.ClearDescription(keyPair.Value);
                            break;

                        case "Class":
                            _tbClass.Text = keyPair.Value;
                            break;

                        case "Compound on":
                            _tbCompoundOn.Text = keyPair.Value;
                            break;

                        case "Attack":
                            _tbAttack.Text = keyPair.Value;
                            break;

                        case "Defense":
                            _tbDefense.Text = keyPair.Value;
                            break;

                        case "Location":
                        case "Equipped on":
                            _tbEquippedOn.Text = keyPair.Value;
                            break;

                        case "Weight":
                            _tbWeight.Text = keyPair.Value;
                            break;

                        case "Property":
                            _tbProperty.Text = keyPair.Value;
                            break;

                        case "Weapon Level":
                            _tbWeaponLevel.Text = keyPair.Value;
                            break;

                        case "Required Level":
                            _tbRequiredLevel.Text = keyPair.Value;
                            break;

                        case "Applicable Job":
                            _tbApplicableJob.Text = keyPair.Value;
                            break;
                    }
                }
            }

            _showPreview();
            _eventsDisabled = false;
        }

        private void _clearFields()
        {
            _tbDescription.Text = "";
            _tbClass.Text = "";
            _tbCompoundOn.Text = "";
            _tbAttack.Text = "";
            _tbDefense.Text = "";
            _tbEquippedOn.Text = "";
            _tbWeight.Text = "";
            _tbProperty.Text = "";
            _tbWeaponLevel.Text = "";
            _tbRequiredLevel.Text = "";
            _tbApplicableJob.Text = "";
        }

        private void _cbCompoundOn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_eventsDisabled) return;

            _tbCompoundOn.Text = _cbCompoundOn.SelectedItem as string;
        }

        private void _cbProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_eventsDisabled) return;

            _tbProperty.Text = _cbProperty.SelectedItem as string;
        }

        private void _cbApplicableJob_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_eventsDisabled) return;

            _tbApplicableJob.Text = _cbApplicableJob.SelectedItem as string;
        }

        private void _tbApplicableJob_TextChanged(object sender, TextChangedEventArgs e)
        {
            try { _cbApplicableJob.SelectedItem = _tbApplicableJob.Text; } catch { }
            try
            {
                _tbHexApplicableJob.TextChanged -= _tbHexApplicableJob_TextChanged;
                _tbHexApplicableJob.Text = JobList.GetHexJob(_tbApplicableJob.Text);
            }
            catch { }
            finally
            {
                _tbHexApplicableJob.TextChanged += _tbHexApplicableJob_TextChanged;
            }

            if (_eventsDisabled) return;

            _showPreview();
        }

        private void _tbProperty_TextChanged(object sender, TextChangedEventArgs e)
        {
            try { _cbProperty.SelectedItem = _tbProperty.Text; } catch { }
            if (_eventsDisabled) return;

            _showPreview();
        }

        private void _tbEquippedOn_TextChanged(object sender, TextChangedEventArgs e)
        {
            _setEquippedOnBoxes();

            if (_eventsDisabled) return;

            _showPreview();
        }

        private void _tbCompoundOn_TextChanged(object sender, TextChangedEventArgs e)
        {
            try { _cbCompoundOn.SelectedItem = _tbCompoundOn.Text; } catch { }
            if (_eventsDisabled) return;

            _showPreview();
        }

        private void _tbClass_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _setClassCbs(_tbClass.Text);

                var type = _cbType.SelectedItem as ItemTypeStructure;

                if (type != null)
                {
                    if (type == ItemTypeStructure.Headgear)
                        _onlyShow("Class", "Defense", "Equipped on", "Applicable Job", "Required Level", "Weight");
                    else if (type == ItemTypeStructure.Card)
                        _onlyShow("Class", "Compound on", "Weight");
                    else if (type == ItemTypeStructure.Weapon)
                        _onlyShow("Class", "Attack", "Weapon Level", "Property", "Required Level", "Applicable Job", "Weight");
                    else if (type == ItemTypeStructure.Armor)
                        _onlyShow("Class", "Defense", "Required Level", "Applicable Job", "Weight");
                    else if (type == ItemTypeStructure.Ammunation)
                        _onlyShow("Class", "Attack", "Property", "Weight");
                    else if (
                        type == ItemTypeStructure.TamingItem ||
                        type == ItemTypeStructure.MonsterEgg ||
                        type == ItemTypeStructure.PetArmor
                        )
                        _onlyShow("Class", "Weight");
                    else
                    {
                        _onlyShow(true);
                    }
                }
                else
                {
                    _onlyShow(true);
                }
            }
            catch { }
            if (_eventsDisabled) return;

            _showPreview();
        }

        private void _setClassCbs(string text)
        {
            var type = ItemTypeStructure.AllTypes.FirstOrDefault(p => String.CompareOrdinal(text, p.Type) == 0 || p.SubItems.Any(s => String.CompareOrdinal(text, s) == 0));

            _enableSubTypeEvents = false;
            _cbType.SelectedItem = type ?? ItemTypeStructure.NotSpecified;

            if (type != null)
            {
                var subType = type.SubItems.FirstOrDefault(p => String.CompareOrdinal(text, p) == 0);

                if (subType != null)
                {
                    var lSt = _cbSubType.Items.Cast<string>().ToList().FirstOrDefault(p => String.CompareOrdinal(text, p) == 0);

                    if (lSt != null)
                        _cbSubType.SelectedItem = lSt;
                }
            }

            _enableSubTypeEvents = true;
        }

        private void _tbDescription_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_eventsDisabled) return;

            _showPreview();
        }

        private void _tbAttack_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_eventsDisabled) return;

            _showPreview();
        }

        private void _tbDefense_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_eventsDisabled) return;

            _showPreview();
        }

        private void _tbWeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_eventsDisabled) return;

            _showPreview();
        }

        private void _tbWeaponLevel_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_eventsDisabled) return;

            _showPreview();
        }

        private void _tbRequiredLevel_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_eventsDisabled) return;

            _showPreview();
        }

        private void _tbHexApplicableJob_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_eventsDisabled) return;

            try
            {
                _tbApplicableJob.TextChanged -= _tbApplicableJob_TextChanged;
                string jobs = JobList.GetStringJobFromHex(_tbHexApplicableJob.Text, _item.GetValue<int>(ServerItemAttributes.Upper), _item.GetValue<int>(ServerItemAttributes.Gender));
                _tbApplicableJob.Text = jobs;
            }
            catch { }
            finally
            {
                _tbApplicableJob.TextChanged += _tbApplicableJob_TextChanged;
            }

            _showPreview();
        }

        private void _cbEquipOnLower_Checked(object sender, RoutedEventArgs e)
        {
            if (_eventsDisabled) return;

            _setEquippedOnText();
        }

        private void _cbEquipOnLower_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_eventsDisabled) return;

            _setEquippedOnText();
        }

        private void _cbEquipOnMiddle_Checked(object sender, RoutedEventArgs e)
        {
            if (_eventsDisabled) return;

            _setEquippedOnText();
        }

        private void _cbEquipOnMiddle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_eventsDisabled) return;

            _setEquippedOnText();
        }

        private void _cbEquipOnUpper_Checked(object sender, RoutedEventArgs e)
        {
            if (_eventsDisabled) return;

            _setEquippedOnText();
        }

        private void _cbEquipOnUpper_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_eventsDisabled) return;

            _setEquippedOnText();
        }

        private void _buttonOk_Click(object sender, RoutedEventArgs e)
        {
            Result = true;

            if (_item == null)
            {
                Close();
                return;
            }

            Item = new ReadableTuple<int>(_item.GetValue<int>(0), ClientItemAttributes.AttributeList);
            Item.Copy(_item);

            Output = _trimReturns(_generateDescription());
            Close();
        }

        private void _buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }

        private void _onlyShow(params string[] values)
        {
            for (int i = 1; i < 10; i++)
            {
                _hideRow(i);
            }
            foreach (string value in values)
            {
                for (int i = 0; i < ParameterHolder.KnownItemParameters.Length; i++)
                {
                    if (ParameterHolder.KnownItemParameters[i] == value)
                        _showRow(i + 1);
                }
            }
        }

        private void _onlyShow(bool showAll)
        {
            for (int i = 1; i < 10; i++)
            {
                if (showAll)
                    _showRow(i);
                else
                    _hideRow(i);
            }
        }

        private void _hideRow(int index)
        {
            _mainGrid.RowDefinitions[index].Height = new GridLength(0);
        }

        private void _showRow(int index)
        {
            _mainGrid.RowDefinitions[index].Height = new GridLength(1, GridUnitType.Star);
        }

        private void _setEquippedOnBoxes()
        {
            bool eventDisabled = _eventsDisabled;
            _eventsDisabled = true;
            string equippedOn = _tbEquippedOn.Text;
            string[] items = equippedOn.Split(',').Select(p => p.Trim(' ')).ToArray();

            _cbEquipOnLower.IsChecked = false;
            _cbEquipOnMiddle.IsChecked = false;
            _cbEquipOnUpper.IsChecked = false;

            foreach (string item in items)
            {
                if (item.ToLower() == "lower")
                    _cbEquipOnLower.IsChecked = true;
                if (item.ToLower() == "mid" || item.ToLower() == "middle")
                    _cbEquipOnMiddle.IsChecked = true;
                if (item.ToLower() == "upper")
                    _cbEquipOnUpper.IsChecked = true;
            }
            _eventsDisabled = false;
            _eventsDisabled = eventDisabled;
        }

        private void _setEquippedOnText()
        {
            bool eventsDisabled = _eventsDisabled;
            _eventsDisabled = true;
            List<string> items = new List<string>();

            if (_cbEquipOnUpper.IsChecked == true)
            {
                items.Add("Upper");
            }

            if (_cbEquipOnMiddle.IsChecked == true)
            {
                items.Add("Mid");
            }

            if (_cbEquipOnLower.IsChecked == true)
            {
                items.Add("Lower");
            }

            _tbEquippedOn.Text = items.Count > 0 ? items.Aggregate((a, b) => a + ", " + b) : "";
            _eventsDisabled = eventsDisabled;
            _showPreview();
        }

        private void _showPreview()
        {
            WpfUtilities.UpdateRtb(_rtbItemDescription, _generateDescription(), true);
        }

        private void _loadComboBoxes()
        {
            _cbType.ItemsSource = ItemTypeStructure.AllTypes;
            _cbType.SelectedIndex = 0;
            _cbCompoundOn.ItemsSource = ParameterHolder.CompoundOn;
            _cbProperty.ItemsSource = ParameterHolder.Properties;
            _cbApplicableJob.ItemsSource = ParameterHolder.Jobs;
        }

        private void _addDescFor(ref string description, string text, int i)
        {
            text = text.Trim(' ');

            if (text == "")
                return;

            if (_mainGrid.RowDefinitions[i].Height.Value > 0)
                description += ParameterHolder.KnownItemParameters[i - 1] + " :^777777 " + text + "^000000" + "\r\n";
        }

        private string _trimReturns(string description)
        {
            while (description.EndsWith("\r\n"))
            {
                description = description.Remove(description.Length - 2, 2);
            }

            while (description.EndsWith("\n"))
            {
                description = description.Remove(description.Length - 1, 1);
            }

            return description;
        }

        private string _generateDescription()
        {
            string description = _trimReturns(_tbDescription.Text);

            description += "\r\n";

            _addDescFor(ref description, _tbClass.Text, 1);
            _addDescFor(ref description, _tbCompoundOn.Text, 2);
            _addDescFor(ref description, _tbAttack.Text, 3);
            _addDescFor(ref description, _tbDefense.Text, 4);
            _addDescFor(ref description, _tbEquippedOn.Text, 5);
            _addDescFor(ref description, _tbWeight.Text, 6);

            if (_mainGrid.RowDefinitions[7].Height.Value > 0)
            {
                string[] properties = _tbProperty.Text.Split(',').Select(p => p.Trim(' ')).ToArray();

                properties = properties.Where(p => p != "").ToArray();

                if (properties.Length > 0)
                {
                    description += ParameterHolder.KnownItemParameters[6] + " :";

                    foreach (string prop in properties)
                    {
                        try
                        {
                            description += ProjectConfiguration.AutocompleteProperties[ParameterHolder.Properties.ToList().IndexOf(prop)];
                        }
                        catch
                        {
                            description += "^000000";
                        }
                        description += " " + prop + "^000000, ";
                    }

                    description = description.Remove(description.Length - 2, 2);
                    description += "\r\n";
                }
            }

            _addDescFor(ref description, _tbWeaponLevel.Text, 8);
            _addDescFor(ref description, _tbRequiredLevel.Text, 9);
            _addDescFor(ref description, _tbApplicableJob.Text, 10);

            return description;
        }

        private void _cbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var type = _cbType.SelectedItem as ItemTypeStructure;

            if (type == null || (type).Type == "Not specified")
            {
                _onlyShow(true);
                _cbSubType.Visibility = Visibility.Hidden;
            }
            else
            {
                if (type.SubItems.Count == 0)
                {
                    _cbSubType.Visibility = Visibility.Hidden;
                    _tbClass.Text = type.Type;
                }
                else
                {
                    _cbSubType.Visibility = Visibility.Visible;
                    _cbSubType.ItemsSource = type.SubItems;

                    var value = _cbSubType.Items.Cast<string>().FirstOrDefault(p => String.CompareOrdinal(_tbClass.Text, p) == 0);

                    if (value != null)
                    {
                        _cbSubType.SelectedItem = value;
                    }
                    else
                    {
                        _cbSubType.SelectedIndex = 0;
                    }
                }
            }
        }

        private void _cbSubType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_enableSubTypeEvents) return;

            var type = _cbSubType.SelectedItem as string;

            if (type == null)
            {
                _onlyShow(true);
            }
            else
            {
                _tbClass.Text = type;
            }
        }

        private void _buttonApply_Click(object sender, RoutedEventArgs e)
        {
            Result = true;

            if (_item == null)
            {
                return;
            }

            Item = new ReadableTuple<int>(_item.GetValue<int>(0), ClientItemAttributes.AttributeList);
            Item.Copy(_item);

            Output = _trimReturns(_generateDescription());
            OnApply(null);
        }
    }
}