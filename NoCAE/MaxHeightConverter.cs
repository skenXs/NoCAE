using System;
using System.Globalization;
using System.Windows.Data;

namespace BestPractices
{
    public class MaxHeightConverter : IValueConverter
    {
        // Convert method to calculate the maximum height based on a percentage
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Console.WriteLine("Convert method called with value: {0}, parameter: {1}", value, parameter);

            double percentageHeight = (double)parameter;

            // Validate the percentage parameter to ensure it's within the acceptable range
            if (percentageHeight <= 0.0 || percentageHeight > 100.0)
            {
                Console.WriteLine("Invalid parameter value: {0}. Must be in the range (0,100].", percentageHeight);
                throw new Exception("MaxHeightConverter expects parameter in the range (0,100]");
            }

            double result = (double)value * percentageHeight;
            Console.WriteLine("Calculated result: {0}", result);

            return result;
        }

        // ConvertBack method is not implemented as it's not required for this converter
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Console.WriteLine("ConvertBack method called, but not implemented.");
            throw new NotImplementedException();
        }
    }
}