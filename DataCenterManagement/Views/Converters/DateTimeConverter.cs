using System.Globalization;
using System.Windows.Data;

namespace DataCenterManagement.Views.Converters
{
    public class DateTimeConverter : IValueConverter
    {
        public static readonly DateTimeConverter DateOnlyToDateTime = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateOnly d) return new DateTime(d.Year, d.Month, d.Day);
            return DateTime.Now;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt) return DateOnly.FromDateTime(dt.Date);
            return DateOnly.FromDateTime(DateTime.Today);
        }
    }
}