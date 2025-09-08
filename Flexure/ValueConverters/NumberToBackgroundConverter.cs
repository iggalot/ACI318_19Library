using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ACI318_19Library
{
    public class NumberToBackgroundConverter : IValueConverter
    {
        // parameter format: "min1,max1,min2,max2" or "lowColor,mediumColor,highColor"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Brushes.Transparent;

            if (!double.TryParse(value.ToString(), out double number))
                return Brushes.Transparent;

            if (parameter is string param)
            {
                // Example: "0,50,100" => thresholds
                var parts = param.Split(',');
                if (parts.Length == 3 &&
                    double.TryParse(parts[0], out double low) &&
                    double.TryParse(parts[1], out double medium) &&
                    double.TryParse(parts[2], out double high))
                {
                    const double eps = 1e-6;
                    if (number < low+eps) return Brushes.LightCoral;
                    if (number < medium+eps) return Brushes.LightYellow;
                    return Brushes.LightGreen;
                }
            }

            // Default coloring
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
