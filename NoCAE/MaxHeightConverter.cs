using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Interop;

namespace BestPractices
{
    public class MaxHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percentageHeight = (double)parameter;

            // Log the percentageHeight parameter value
            Console.WriteLine($"MaxHeightConverter: Received percentageHeight parameter: {percentageHeight}");

            if ((percentageHeight <= 0.0) || (percentageHeight > 100.0))
            {
                // Log an error message before throwing an exception
                Console.WriteLine("MaxHeightConverter: Invalid parameter value. Expected range is (0,100].");
                throw new Exception("MaxHeightConverter expects parameter in the range (0,100]");
            }

            // Calculate and return the converted height
            double convertedHeight = ((double)value * percentageHeight);
            // Log the converted height value
            Console.WriteLine($"MaxHeightConverter: Converted height: {convertedHeight}");
            return convertedHeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This method is not implemented as the conversion is one-way
            throw new NotImplementedException();
        }
    }
}