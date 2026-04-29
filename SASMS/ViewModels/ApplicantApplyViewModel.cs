using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SASMS.ViewModels
{
    public class ApplicantApplyViewModel
    {
        // Personal Information
        [Required(ErrorMessage = "Full Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "National ID is required.")]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "National ID must be exactly 14 digits.")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "National ID must contain only numbers.")]
        public string NationalId { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [RegularExpression("^01[0-9]{9}$", ErrorMessage = "Phone number must be a valid Egyptian mobile number (01xxxxxxxxx).")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        [SASMS.Attributes.AgeRange(15, 18, ErrorMessage = "Applicant age must be between 15 and 18 years.")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; }

        // Academic Information
        [Required(ErrorMessage = "Previous School Name is required.")]
        public string Grade { get; set; } // This is actually the school name in the form

        [Required(ErrorMessage = "Preparatory score is required.")]
        [Range(0, 280, ErrorMessage = "Score must be between 0 and 280.")]
        public double GradeOfPrimarySchool { get; set; }

        [Required(ErrorMessage = "English score is required.")]
        [Range(0, 60, ErrorMessage = "Score must be between 0 and 60.")]
        public double GradeOfEnglishExam { get; set; }

        [Required(ErrorMessage = "Math score is required.")]
        [Range(0, 60, ErrorMessage = "Score must be between 0 and 60.")]
        public double GradeOfMathExam { get; set; }

        [Required(ErrorMessage = "Science score is required.")]
        [Range(0, 60, ErrorMessage = "Score must be between 0 and 60.")]
        public double GradeOfScienceExam { get; set; }

        [Required(ErrorMessage = "Preferred Department is required.")]
        public int PreferredDepartmentId { get; set; }

        // Guardian Information
        [Required(ErrorMessage = "Guardian Name is required.")]
        public string ParentName { get; set; }

        [Required(ErrorMessage = "Guardian Phone is required.")]
        public string ParentPhone { get; set; }

        [Required(ErrorMessage = "Relationship is required.")]
        public string Kinship { get; set; }

        public string ParentMajor { get; set; }

        // Dynamic Fields Metadata
        public Dictionary<string, string> DynamicValues { get; set; } = new Dictionary<string, string>();
    }
}
