using System.ComponentModel.DataAnnotations;

namespace GymManagementBackend.DTOs
{
    public class GymDto
    {
        public Guid Id { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string SubscriptionPlan { get; set; } = "basic";
        public bool IsActive { get; set; }
        public int UsersCount { get; set; }
        public int ActiveUsersCount { get; set; }
        public int InactiveUsersCount { get; set; }
        public int MembersCount { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueLastMonth { get; set; }
        public decimal RevenueTotal { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GymMonthlyRevenuePointDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Amount { get; set; }
    }

    public class CreateGymDto
    {
        [Required]
        [StringLength(150)]
        public string GymName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string OwnerName { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(50)]
        public string SubscriptionPlan { get; set; } = "basic";
    }

    public class UpdateGymDto
    {
        [StringLength(150)]
        public string? GymName { get; set; }

        [StringLength(100)]
        public string? OwnerName { get; set; }

        [StringLength(15)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(50)]
        public string? SubscriptionPlan { get; set; }

        public bool? IsActive { get; set; }
    }

    public class AppUserDto
    {
        public Guid Id { get; set; }
        public Guid? GymId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateUserDto
    {
        [Required]
        public Guid GymId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Phone { get; set; }

        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(ADMIN|OWNER|STAFF|TRAINER|MEMBER)$")]
        public string Role { get; set; } = "OWNER";
    }

    public class OwnerCreateUserDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Phone { get; set; }

        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(STAFF|TRAINER)$")]
        public string Role { get; set; } = "STAFF";
    }

    public class UpdateUserDto
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(15)]
        public string? Phone { get; set; }

        [RegularExpression("^(ADMIN|OWNER|STAFF|TRAINER|MEMBER)$")]
        public string? Role { get; set; }

        public bool? IsActive { get; set; }
    }
}
