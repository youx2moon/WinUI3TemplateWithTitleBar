using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace $safeprojectname$.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
            {
                // parameterに "Inverse" と入っていれば逆転させる
                if (parameter?.ToString() == "Inverse") b = !b;
                return b ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}