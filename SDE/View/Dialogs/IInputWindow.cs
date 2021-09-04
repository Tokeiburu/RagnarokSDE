using GRF.Threading;
using SDE.ApplicationConfiguration;
using SDE.Core;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TokeiLibrary;
using Utilities;

namespace SDE.View.Dialogs
{
    public interface IInputWindow
    {
        string Text { get; }
        Grid Footer { get; }

        event Action ValueChanged;
    }

    public static class InputWindowHelper
    {
        public static void Edit(Window dialog, TextBox tb, Button button, bool canIntegrated = true)
        {
            IInputWindow inputWindow = (IInputWindow)dialog;

            bool isScript = dialog is ScriptEditDialog && SdeAppConfiguration.UseIntegratedDialogsForScripts;
            bool isLevel = dialog is LevelEditDialog && SdeAppConfiguration.UseIntegratedDialogsForLevels;
            bool isFlag = dialog is GenericFlagDialog && SdeAppConfiguration.UseIntegratedDialogsForFlags;
            bool isJob = dialog is JobEditDialog && SdeAppConfiguration.UseIntegratedDialogsForJobs;
            bool isTime = dialog is TimeEditDialog && SdeAppConfiguration.UseIntegratedDialogsForTime;
            bool isRate = dialog is RateEditDialog;
            bool isOther = !(dialog is ScriptEditDialog || dialog is LevelEditDialog || dialog is GenericFlagDialog || dialog is JobEditDialog || dialog is TimeEditDialog) && SdeAppConfiguration.UseIntegratedDialogsForFlags;

            if (canIntegrated && (isScript || isLevel || isFlag || isJob || isTime || isRate || isOther))
            {
                inputWindow.Footer.Visibility = Visibility.Collapsed;
                dialog.WindowStyle = WindowStyle.None;
                var content = dialog.Content;

                Border border = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
                dialog.Content = null;
                border.Child = content as UIElement;
                dialog.Content = border;

                dialog.Owner = null;

                Extensions.SetMinimalSize(dialog);
                dialog.ResizeMode = ResizeMode.NoResize;

                Point p = button.PointToScreen(new Point(0, 0));
                var par = WpfUtilities.FindParentControl<Window>(button);

                dialog.Loaded += delegate
                {
                    if (dialog == null) return;

                    button.IsEnabled = false;
                    dialog.WindowStartupLocation = WindowStartupLocation.Manual;

                    int dpiXI = (int)typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null);
                    double dpiX = dpiXI;
                    double ratio = dpiX / 96;

                    p.X /= ratio;
                    p.Y /= ratio;

                    // The dialog's position scales with the DPI
                    dialog.Left = p.X - dialog.MinWidth + button.ActualWidth;
                    dialog.Top = p.Y + button.ActualHeight;

                    if (dialog.Left < 0)
                    {
                        dialog.Left = 0;
                    }

                    if (dialog.Top + dialog.Height > SystemParameters.WorkArea.Bottom)
                    {
                        dialog.Top = p.Y - dialog.MinHeight;
                    }

                    if (dialog.Top < 0)
                    {
                        dialog.Top = 0;
                    }

                    dialog.Owner = par;
                };

                inputWindow.ValueChanged += () => tb.Text = inputWindow.Text;
                dialog.Closed += delegate
                {
                    button.IsEnabled = true;
                };
                dialog.Deactivated += (sender, args) => GrfThread.Start(() => dialog.Dispatch(() => Debug.Ignore(dialog.Close)));

                dialog.Show();
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                Extensions.SetMinimalSize(dialog);

                dialog.Loaded += delegate
                {
                    dialog.SizeToContent = SizeToContent.Manual;
                    dialog.Left = dialog.Owner.Left + (dialog.Owner.Width - dialog.MinWidth) / 2;
                    dialog.Top = dialog.Owner.Top + (dialog.Owner.Height - dialog.MinHeight) / 2;
                };

                dialog.Owner = WpfUtilities.FindParentControl<Window>(button);

                if (dialog.ShowDialog() == true)
                {
                    tb.Text = ((IInputWindow)dialog).Text;
                }
            }
        }
    }
}