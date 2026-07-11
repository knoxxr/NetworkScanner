using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace NetworkScanner.Avalonia.Helpers
{
    // IPInfo.StatusKey("good"/"warn"/"bad")를 상태 배지 색상으로 변환한다.
    public class StatusColorConverter : IValueConverter
    {
        private static readonly ISolidColorBrush Good = new SolidColorBrush(Color.Parse("#FF34C777"));
        private static readonly ISolidColorBrush Warn = new SolidColorBrush(Color.Parse("#FFF3A53E"));
        private static readonly ISolidColorBrush Bad = new SolidColorBrush(Color.Parse("#FFEF5875"));

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (value as string) switch
            {
                "good" => Good,
                "warn" => Warn,
                _ => Bad,
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
