using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace NetworkScanner.Avalonia.Helpers
{
    public class AliveColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool alive = value is bool b && b;
            return alive ? Brushes.DodgerBlue : Brushes.OrangeRed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
