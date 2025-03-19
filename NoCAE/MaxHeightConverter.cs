using System;
using System.Globalization;
using System.Windows.Data;

namespace BestPractices
{
    public class MaxHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percentageHeight = (double)parameter;

            if (percentageHeight <= 0.0 || percentageHeight > 100.0)
                throw new Exception("MaxHeightConverter expects parameter in the range (0,100]");

            return (double)value * percentageHeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}