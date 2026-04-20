using GymManagementBackend.Constants;
using GymManagementBackend.Data;
using GymManagementBackend.DTOs;
using GymManagementBackend.Models;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace GymManagementBackend.Services
{
    public interface IAdminService
    {
        Task<List<GymDto>> GetGymsAsync();
        Task<List<GymMonthlyRevenuePointDto>> GetGymMonthlyRevenueAsync(Guid gymId, int months = 12);
        Task<GymDto> CreateGymAsync(CreateGymDto request);
        Task<GymWithOwnersDto> CreateGymWithOwnersAsync(CreateGymWithOwnersDto request);
        Task<GymDto> UpdateGymAsync(Guid gymId, UpdateGymDto request);
        Task DeleteGymAsync(Guid gymId);
        Task<List<AppUserDto>> GetUsersAsync(Guid? gymId = null);
        Task<AppUserDto> CreateUserAsync(CreateUserDto request);
        Task<AppUserDto> CreateUserForGymAsync(Guid gymId, OwnerCreateUserDto request);
        Task<AppUserDto> UpdateUserAsync(Guid userId, UpdateUserDto request);
        Task DeleteUserAsync(Guid userId);
    }

    public class AdminService : IAdminService
    {
        private static readonly HashSet<string> AllowedManagedRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            AppRoles.Owner,
            AppRoles.Staff,
            AppRoles.Trainer,
            AppRoles.Member
        };

        private readonly GymDbContext _context;
        private readonly ILogger<AdminService> _logger;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IProfileImageStorageService _profileImageStorageService;

        public AdminService(
            GymDbContext context,
            ILogger<AdminService> logger,
            IEmailNotificationService emailNotificationService,
            IProfileImageStorageService profileImageStorageService)
        {
            _context = context;
            _logger = logger;
            _emailNotificationService = emailNotificationService;
            _profileImageStorageService = profileImageStorageService;
        }

        public async Task<List<GymDto>> GetGymsAsync()
        {
            return await GetGymsWithCountsAsync();
        }

        public async Task<List<GymMonthlyRevenuePointDto>> GetGymMonthlyRevenueAsync(Guid gymId, int months = 12)
        {
            var normalizedMonths = Math.Clamp(months, 1, 36);
            var today = GetTodayIndia();
            var from = new DateOnly(today.Year, today.Month, 1).AddMonths(-(normalizedMonths - 1));
            var to = new DateOnly(today.Year, today.Month, 1).AddMonths(1).AddDays(-1);

            var points = await _context.Payments
                .AsNoTracking()
                .Where(p => p.GymId == gymId && p.PaymentDate >= from && p.PaymentDate <= to)
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new GymMonthlyRevenuePointDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Amount = g.Sum(p => p.Amount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            return points;
        }

        public async Task<GymDto> CreateGymAsync(CreateGymDto request)
        {
            var email = request.Email?.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(email))
            {
                var exists = await _context.Gyms.AnyAsync(g => g.Email != null && g.Email.ToLower() == email);
                if (exists)
                {
                    throw new InvalidOperationException("Gym email already exists.");
                }
            }

            var gym = new Gym
            {
                GymName = request.GymName.Trim(),
                OwnerName = request.OwnerName.Trim(),
                Phone = request.Phone?.Trim(),
                Email = email,
                Address = request.Address?.Trim(),
                City = request.City?.Trim(),
                State = request.State?.Trim(),
                SubscriptionPlan = string.IsNullOrWhiteSpace(request.SubscriptionPlan) ? "basic" : request.SubscriptionPlan.Trim().ToLowerInvariant(),
                IsActive = true
            };

            _context.Gyms.Add(gym);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Gym created: {GymId}", gym.Id);
            EmailDeliveryResult? gymEmailResult = null;
            if (!string.IsNullOrWhiteSpace(gym.Email))
            {
                gymEmailResult = await _emailNotificationService.SendGymCreatedEmailAsync(
                    gym.Email,
                    gym.GymName,
                    gym.OwnerName);
            }
            var createdGym = (await GetGymsWithCountsAsync(gym.Id)).Single();
            if (gymEmailResult is not null)
            {
                createdGym.NotificationEmailSent = gymEmailResult.Success;
                createdGym.NotificationEmailMessage = gymEmailResult.Message;
            }
            return createdGym;
        }

        public async Task<GymWithOwnersDto> CreateGymWithOwnersAsync(CreateGymWithOwnersDto request)
        {
            if (request.Gym is null)
            {
                throw new InvalidOperationException("Gym details are required.");
            }

            if (request.Owners is null || request.Owners.Count == 0)
            {
                throw new InvalidOperationException("At least one owner is required.");
            }

            if (request.Owners.Count > 2)
            {
                throw new InvalidOperationException("Each gym can have at most 2 owners.");
            }

            var normalizedOwnerEmails = request.Owners
                .Select(o => o.Email.Trim().ToLowerInvariant())
                .ToList();

            if (normalizedOwnerEmails.Distinct().Count() != normalizedOwnerEmails.Count)
            {
                throw new InvalidOperationException("Owner emails must be unique.");
            }

            var existingOwnerEmail = await _context.Users
                .AnyAsync(u => normalizedOwnerEmails.Contains(u.Email.ToLower()));
            if (existingOwnerEmail)
            {
                throw new InvalidOperationException("One or more owner emails already exist.");
            }

            var gymEmail = request.Gym.Email?.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(gymEmail))
            {
                var gymEmailExists = await _context.Gyms.AnyAsync(g => g.Email != null && g.Email.ToLower() == gymEmail);
                if (gymEmailExists)
                {
                    throw new InvalidOperationException("Gym email already exists.");
                }
            }

            var gym = new Gym
            {
                GymName = request.Gym.GymName.Trim(),
                OwnerName = request.Gym.OwnerName.Trim(),
                Phone = request.Gym.Phone?.Trim(),
                Email = gymEmail,
                Address = request.Gym.Address?.Trim(),
                City = request.Gym.City?.Trim(),
                State = request.Gym.State?.Trim(),
                SubscriptionPlan = string.IsNullOrWhiteSpace(request.Gym.SubscriptionPlan) ? "basic" : request.Gym.SubscriptionPlan.Trim().ToLowerInvariant(),
                IsActive = true
            };

            var ownerPasswords = request.Owners.Select(_ => GenerateTemporaryPassword()).ToList();
            var ownerUsers = request.Owners.Select((o, index) => new User
            {
                GymId = gym.Id,
                FullName = o.FullName.Trim(),
                Email = o.Email.Trim().ToLowerInvariant(),
                Phone = o.Phone?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(ownerPasswords[index]),
                Role = AppRoles.Owner,
                IsActive = true
            }).ToList();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            _context.Gyms.Add(gym);
            _context.Users.AddRange(ownerUsers);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Gym with owners created: {GymId} (Owners: {OwnerCount})", gym.Id, ownerUsers.Count);

            var ownerMailStatuses = new List<EmailDeliveryResult>();
            for (var i = 0; i < ownerUsers.Count; i++)
            {
                var ownerUser = ownerUsers[i];
                var result = await _emailNotificationService.SendUserWelcomeEmailAsync(
                    ownerUser.Email,
                    ownerUser.FullName,
                    ownerUser.Role,
                    ownerUser.Email,
                    ownerPasswords[i],
                    gym.GymName);
                ownerMailStatuses.Add(result);
            }

            EmailDeliveryResult? gymEmailResult = null;
            if (!string.IsNullOrWhiteSpace(gym.Email))
            {
                gymEmailResult = await _emailNotificationService.SendGymCreatedEmailAsync(
                    gym.Email,
                    gym.GymName,
                    gym.OwnerName);
            }

            var createdGym = (await GetGymsWithCountsAsync(gym.Id)).Single();
            if (gymEmailResult is not null)
            {
                createdGym.NotificationEmailSent = gymEmailResult.Success;
                createdGym.NotificationEmailMessage = gymEmailResult.Message;
            }

            var ownerDtos = ownerUsers.Select(MapUser).ToList();
            for (var i = 0; i < ownerDtos.Count && i < ownerMailStatuses.Count; i++)
            {
                ownerDtos[i].WelcomeEmailSent = ownerMailStatuses[i].Success;
                ownerDtos[i].WelcomeEmailMessage = ownerMailStatuses[i].Message;
            }

            return new GymWithOwnersDto
            {
                Gym = createdGym,
                Owners = ownerDtos
            };
        }

        public async Task<GymDto> UpdateGymAsync(Guid gymId, UpdateGymDto request)
        {
            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == gymId)
                ?? throw new KeyNotFoundException("Gym not found.");

            if (!string.IsNullOrWhiteSpace(request.GymName)) gym.GymName = request.GymName.Trim();
            if (!string.IsNullOrWhiteSpace(request.OwnerName)) gym.OwnerName = request.OwnerName.Trim();
            if (request.Phone is not null) gym.Phone = request.Phone.Trim();
            if (request.Email is not null) gym.Email = request.Email.Trim().ToLowerInvariant();
            if (request.Address is not null) gym.Address = request.Address.Trim();
            if (request.City is not null) gym.City = request.City.Trim();
            if (request.State is not null) gym.State = request.State.Trim();
            if (request.SubscriptionPlan is not null) gym.SubscriptionPlan = request.SubscriptionPlan.Trim().ToLowerInvariant();
            if (request.IsActive.HasValue) gym.IsActive = request.IsActive.Value;
            gym.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (await GetGymsWithCountsAsync(gym.Id)).Single();
        }

        public async Task DeleteGymAsync(Guid gymId)
        {
            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == gymId)
                ?? throw new KeyNotFoundException("Gym not found.");

            _context.Gyms.Remove(gym);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AppUserDto>> GetUsersAsync(Guid? gymId = null)
        {
            var query = _context.Users.AsQueryable();
            if (gymId.HasValue)
            {
                query = query.Where(u => u.GymId == gymId.Value);
            }

            return await query
                .OrderByDescending(u => u.CreatedAt)
                .Select(MapUserProjection())
                .ToListAsync();
        }

        public async Task<AppUserDto> CreateUserAsync(CreateUserDto request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
            {
                throw new InvalidOperationException("Email already exists.");
            }

            if (!await _context.Gyms.AnyAsync(g => g.Id == request.GymId))
            {
                throw new KeyNotFoundException("Gym not found.");
            }

            var role = request.Role.ToUpperInvariant();
            if (role == AppRoles.Admin)
            {
                throw new InvalidOperationException("Use platform provisioning to create ADMIN users.");
            }
            if (!AllowedManagedRoles.Contains(role))
            {
                throw new InvalidOperationException("Invalid role. Allowed roles: OWNER, STAFF, TRAINER, MEMBER.");
            }
            if (role == AppRoles.Owner)
            {
                await EnsureOwnerCapacityAsync(request.GymId, 1);
            }

            var temporaryPassword = GenerateTemporaryPassword();
            var user = new User
            {
                GymId = request.GymId,
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                Phone = request.Phone?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
                Role = role,
                IsActive = true
            };
            user.ProfileImageUrl = await _profileImageStorageService.StoreUserImageAsync(user.Id, request.ProfileImageUrl);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User created: {UserId}", user.Id);
            var gymName = await _context.Gyms
                .Where(g => g.Id == request.GymId)
                .Select(g => g.GymName)
                .FirstOrDefaultAsync() ?? "Gym";
            var emailResult = await _emailNotificationService.SendUserWelcomeEmailAsync(
                user.Email,
                user.FullName,
                user.Role,
                user.Email,
                temporaryPassword,
                gymName);
            var dto = MapUser(user);
            dto.WelcomeEmailSent = emailResult.Success;
            dto.WelcomeEmailMessage = emailResult.Message;
            return dto;
        }

        public async Task<AppUserDto> CreateUserForGymAsync(Guid gymId, OwnerCreateUserDto request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
            {
                throw new InvalidOperationException("Email already exists.");
            }

            if (!await _context.Gyms.AnyAsync(g => g.Id == gymId))
            {
                throw new KeyNotFoundException("Gym not found.");
            }

            var role = request.Role.ToUpperInvariant();
            if (role != AppRoles.Staff && role != AppRoles.Trainer)
            {
                throw new InvalidOperationException("Owners can only create STAFF or TRAINER users.");
            }

            var temporaryPassword = GenerateTemporaryPassword();
            var user = new User
            {
                GymId = gymId,
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                Phone = request.Phone?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
                Role = role,
                IsActive = true
            };
            user.ProfileImageUrl = await _profileImageStorageService.StoreUserImageAsync(user.Id, request.ProfileImageUrl);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Owner-created staff user: {UserId}", user.Id);
            var gymName = await _context.Gyms
                .Where(g => g.Id == gymId)
                .Select(g => g.GymName)
                .FirstOrDefaultAsync() ?? "Gym";
            var emailResult = await _emailNotificationService.SendUserWelcomeEmailAsync(
                user.Email,
                user.FullName,
                user.Role,
                user.Email,
                temporaryPassword,
                gymName);
            var dto = MapUser(user);
            dto.WelcomeEmailSent = emailResult.Success;
            dto.WelcomeEmailMessage = emailResult.Message;
            return dto;
        }

        public async Task<AppUserDto> UpdateUserAsync(Guid userId, UpdateUserDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (!string.IsNullOrWhiteSpace(request.FullName)) user.FullName = request.FullName.Trim();
            if (request.Email is not null)
            {
                var normalizedEmail = request.Email.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(normalizedEmail))
                {
                    throw new InvalidOperationException("Email cannot be empty.");
                }

                var duplicate = await _context.Users.AnyAsync(u => u.Id != userId && u.Email.ToLower() == normalizedEmail);
                if (duplicate)
                {
                    throw new InvalidOperationException("Email already exists.");
                }

                user.Email = normalizedEmail;
            }
            if (request.Phone is not null) user.Phone = request.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var newRole = request.Role.ToUpperInvariant();
                if (newRole == AppRoles.Admin)
                {
                    throw new InvalidOperationException("Assigning ADMIN role is not allowed via this endpoint.");
                }
                if (!AllowedManagedRoles.Contains(newRole))
                {
                    throw new InvalidOperationException("Invalid role. Allowed roles: OWNER, STAFF, TRAINER, MEMBER.");
                }
                if (newRole == AppRoles.Owner && !string.Equals(user.Role, AppRoles.Owner, StringComparison.OrdinalIgnoreCase))
                {
                    if (!user.GymId.HasValue)
                    {
                        throw new InvalidOperationException("Cannot assign OWNER role to a user without a gym.");
                    }
                    await EnsureOwnerCapacityAsync(user.GymId.Value, 1);
                }
                user.Role = newRole;
            }
            if (request.ProfileImageUrl is not null)
            {
                user.ProfileImageUrl = await _profileImageStorageService.StoreUserImageAsync(user.Id, request.ProfileImageUrl);
            }
            if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapUser(user);
        }

        public async Task DeleteUserAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (string.Equals(user.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Deleting ADMIN users is not allowed via this endpoint.");
            }

            if (string.Equals(user.Role, AppRoles.Trainer, StringComparison.OrdinalIgnoreCase))
            {
                await _context.TrainerAssignments
                    .Where(x => x.TrainerUserId == userId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.TrainerUserId, (Guid?)null));

                await _context.SubscriptionMonthServices
                    .Where(x => x.TrainerUserId == userId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.TrainerUserId, (Guid?)null)
                        .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        private async Task<List<GymDto>> GetGymsWithCountsAsync(Guid? gymId = null)
        {
            var gymsQuery = _context.Gyms.AsQueryable();
            if (gymId.HasValue)
            {
                gymsQuery = gymsQuery.Where(g => g.Id == gymId.Value);
            }

            var gyms = await gymsQuery
                .OrderBy(g => g.GymName)
                .Select(g => new GymDto
                {
                    Id = g.Id,
                    GymName = g.GymName,
                    OwnerName = g.OwnerName,
                    Phone = g.Phone,
                    Email = g.Email,
                    Address = g.Address,
                    City = g.City,
                    State = g.State,
                    SubscriptionPlan = g.SubscriptionPlan,
                    IsActive = g.IsActive,
                    UsersCount = 0,
                    MembersCount = 0,
                    CreatedAt = g.CreatedAt
                })
                .ToListAsync();

            if (gyms.Count == 0)
            {
                return gyms;
            }

            var gymIds = gyms.Select(g => g.Id).ToList();

            var userCounts = await _context.Users
                .Where(u => u.GymId.HasValue && gymIds.Contains(u.GymId.Value))
                .GroupBy(u => u.GymId!.Value)
                .Select(g => new { GymId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GymId, x => x.Count);

            var activeUserCounts = await _context.Users
                .Where(u => u.GymId.HasValue && gymIds.Contains(u.GymId.Value) && u.IsActive)
                .GroupBy(u => u.GymId!.Value)
                .Select(g => new { GymId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GymId, x => x.Count);

            var inactiveUserCounts = await _context.Users
                .Where(u => u.GymId.HasValue && gymIds.Contains(u.GymId.Value) && !u.IsActive)
                .GroupBy(u => u.GymId!.Value)
                .Select(g => new { GymId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GymId, x => x.Count);

            var memberCounts = await _context.Members
                .Where(m => gymIds.Contains(m.GymId))
                .GroupBy(m => m.GymId)
                .Select(g => new { GymId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GymId, x => x.Count);

            var today = GetTodayIndia();
            var currentMonthStart = new DateOnly(today.Year, today.Month, 1);
            var nextMonthStart = currentMonthStart.AddMonths(1);
            var lastMonthStart = currentMonthStart.AddMonths(-1);

            var revenueGrouped = await _context.Payments
                .Where(p => gymIds.Contains(p.GymId))
                .GroupBy(p => p.GymId)
                .Select(g => new
                {
                    GymId = g.Key,
                    Total = g.Sum(p => p.Amount),
                    ThisMonth = g.Where(p => p.PaymentDate >= currentMonthStart && p.PaymentDate < nextMonthStart).Sum(p => (decimal?)p.Amount) ?? 0m,
                    LastMonth = g.Where(p => p.PaymentDate >= lastMonthStart && p.PaymentDate < currentMonthStart).Sum(p => (decimal?)p.Amount) ?? 0m
                })
                .ToListAsync();

            var revenueByGym = revenueGrouped.ToDictionary(x => x.GymId);

            foreach (var gym in gyms)
            {
                gym.UsersCount = userCounts.TryGetValue(gym.Id, out var uc) ? uc : 0;
                gym.ActiveUsersCount = activeUserCounts.TryGetValue(gym.Id, out var auc) ? auc : 0;
                gym.InactiveUsersCount = inactiveUserCounts.TryGetValue(gym.Id, out var iuc) ? iuc : 0;
                gym.MembersCount = memberCounts.TryGetValue(gym.Id, out var mc) ? mc : 0;
                if (revenueByGym.TryGetValue(gym.Id, out var rev))
                {
                    gym.RevenueTotal = rev.Total;
                    gym.RevenueThisMonth = rev.ThisMonth;
                    gym.RevenueLastMonth = rev.LastMonth;
                }
            }

            return gyms;
        }

        private static DateOnly GetTodayIndia()
        {
            var utcNow = DateTime.UtcNow;
            try
            {
                var india = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcNow, india));
            }
            catch
            {
                try
                {
                    var india = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
                    return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcNow, india));
                }
                catch
                {
                    return DateOnly.FromDateTime(utcNow);
                }
            }
        }

        private static AppUserDto MapUser(User user)
        {
            return new AppUserDto
            {
                Id = user.Id,
                GymId = user.GymId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                ProfileImageUrl = user.ProfileImageUrl,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        private static Expression<Func<User, AppUserDto>> MapUserProjection()
        {
            return user => new AppUserDto
            {
                Id = user.Id,
                GymId = user.GymId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                ProfileImageUrl = user.ProfileImageUrl,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        private async Task EnsureOwnerCapacityAsync(Guid gymId, int newOwnersCount)
        {
            var existingOwnerCount = await _context.Users
                .CountAsync(u => u.GymId == gymId && u.Role == AppRoles.Owner);

            if (existingOwnerCount + newOwnersCount > 2)
            {
                throw new InvalidOperationException("Each gym can have at most 2 owners.");
            }
        }

        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "@#$%&*!";
            var all = upper + lower + digits + special;

            Span<char> password = stackalloc char[10];
            password[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
            password[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
            password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            password[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

            for (var i = 4; i < password.Length; i++)
            {
                password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            }

            for (var i = password.Length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }
    }
}
