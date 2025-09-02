using System.Globalization;
using System.Windows.Controls;

namespace ACI318_19Library
{
    public class DoubleValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return new ValidationResult(false, "Value required");

            if (double.TryParse(value.ToString(), out double result))
                return ValidationResult.ValidResult;

            return new ValidationResult(false, "Invalid number");
        }
    }

}
