using System;
using System.ComponentModel.DataAnnotations;

namespace SASMS.Attributes
{
    public class TimeGreaterThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public TimeGreaterThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) return ValidationResult.Success;

            var currentValue = (TimeOnly)value;
            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);

            if (property == null)
                throw new ArgumentException("Property with this name not found");

            var comparisonValueRaw = property.GetValue(validationContext.ObjectInstance);
            if (comparisonValueRaw == null) return ValidationResult.Success;

            var comparisonValue = (TimeOnly)comparisonValueRaw;

            // We only validate time if it's on the same day, but this attribute doesn't know about the date.
            // Usually, if EndTime < StartTime, it's invalid unless it spans across midnight, 
            // but for SASMS activities, they usually happen in one day or have explicit dates.
            
            // To handle the "logic in all pages", we check if EndTime is after StartTime.
            if (currentValue < comparisonValue)
            {
                return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} must be after {_comparisonProperty}");
            }

            return ValidationResult.Success;
        }
    }
}
