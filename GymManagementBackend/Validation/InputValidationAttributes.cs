using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace GymManagementBackend.Validation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class StrictEmailAttribute : ValidationAttribute
    {
        private static readonly Regex EmailRegex = new(
            @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
            {
                return ValidationResult.Success;
            }

            var input = value.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                return ValidationResult.Success;
            }

            return EmailRegex.IsMatch(input)
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage ?? "Invalid email format.");
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class IndianPhoneAttribute : ValidationAttribute
    {
        private static readonly Regex PhoneRegex = new(
            @"^\+91\d{10}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
            {
                return ValidationResult.Success;
            }

            var input = value.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                return ValidationResult.Success;
            }

            return PhoneRegex.IsMatch(input)
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage ?? "Phone must be in +91XXXXXXXXXX format.");
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class StrongPasswordAttribute : ValidationAttribute
    {
        private static readonly Regex PasswordRegex = new(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,100}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
            {
                return ValidationResult.Success;
            }

            var input = value.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                return ValidationResult.Success;
            }

            return PasswordRegex.IsMatch(input)
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage ?? "Password must be 8-100 chars with uppercase, lowercase, number, and special character.");
        }
    }
}
