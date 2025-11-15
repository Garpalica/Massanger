
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace WpfMessenger.Converters
{
    // Этот конвертер будет сравнивать два значения и возвращать true, если они равны
    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Проверяем, что нам пришли два значения и они не пустые
            if (values == null || values.Length < 2 || values[0] == null || values[1] == null)
            {
                return false;
            }
            // Сравниваем их
            return values[0].Equals(values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}