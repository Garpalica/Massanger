// Massanger/Converters/Base64ToBitmapImageConverter.cs
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Massanger.Converters
{
    public class Base64ToBitmapImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Получаем нашу строку Base64
            string base64String = value as string;
            if (string.IsNullOrEmpty(base64String))
                return null;

            try
            {
                // Преобразуем строку обратно в массив байтов
                byte[] imageBytes = System.Convert.FromBase64String(base64String);
                using (var ms = new MemoryStream(imageBytes))
                {
                    // Создаем из потока байтов изображение, понятное для WPF
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad; // Важно для освобождения потока
                    image.StreamSource = ms;
                    image.EndInit();
                    return image;
                }
            }
            catch (FormatException)
            {
                // Если строка не является валидным Base64, ничего не возвращаем
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Обратное преобразование нам не нужно
            throw new NotImplementedException();
        }
    }
}