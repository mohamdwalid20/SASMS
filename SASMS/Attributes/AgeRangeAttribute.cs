using System;
using System.ComponentModel.DataAnnotations;

namespace SASMS.Attributes
{
    public class AgeRangeAttribute : ValidationAttribute
    {
        private readonly int _minAge;
        private readonly int _maxAge;

        public AgeRangeAttribute(int minAge, int maxAge)
        {
            _minAge = minAge;
            _maxAge = maxAge;
            ErrorMessage = $"Age must be between {minAge} and {maxAge} years.";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime dateOfBirth)
            {
                var today = DateTime.Today;
                var age = today.Year - dateOfBirth.Year;
                
                // Adjustment for leap years or if birthday hasn't occurred yet this year
                if (dateOfBirth.Date > today.AddYears(-age)) age--;

                if (age < _minAge || age > _maxAge)
                {
                    return new ValidationResult(ErrorMessage);
                }

                return ValidationResult.Success;
            }

            return new ValidationResult("Invalid Date of Birth.");
        }
    }
}
