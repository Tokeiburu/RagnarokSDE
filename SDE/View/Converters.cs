using SDE.View.Controls;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TokeiLibrary;

namespace SDE.View
{
    /// <summary>
    /// Adjusts the width for the PatcherErrorView items in the ListView
    /// </summary>
    public class WidthAdjusterConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(double)) { return null; }
            if (!(value is Border)) { return 0; }

            Border text = (Border)value;

            TextViewItem res = WpfUtilities.FindDirectParentControl<TextViewItem>(text);

            if (res == null)
                return text.ActualWidth;

            ListView view = res.ListView;

            if (view == null)
                return text.ActualWidth;

            if (view.ActualWidth < 0)
                return 0;

            double dParentWidth = view.ActualWidth;
            double dToAdjust = parameter == null ? 0 : double.Parse(parameter.ToString());
            double dAdjustedWidth = dParentWidth + dToAdjust - 10;

            if (_isScrollBarVisible(view))
            {
                dAdjustedWidth -= SystemParameters.VerticalScrollBarWidth;
            }

            return (dAdjustedWidth < 0 ? 0 : dAdjustedWidth);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion IValueConverter Members

        private static bool _isScrollBarVisible(DependencyObject view)
        {
            ScrollViewer[] sv = WpfUtilities.FindChildren<ScrollViewer>(view);

            if (sv.Length > 0)
                return sv[0].ComputedVerticalScrollBarVisibility == Visibility.Visible;

            return false;
        }
    }
}