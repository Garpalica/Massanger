// Massanger/Converters/EqualityConverter.cs
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Massanger.Converters
{
    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Проверяем, что пришли хотя бы два значения и они не пустые
            if (values == null || values.Length < 2 || values.Any(v => v == null))
            {
                return false;
            }

            // Сравниваем первое значение со вторым
            return values[0].Equals(values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}