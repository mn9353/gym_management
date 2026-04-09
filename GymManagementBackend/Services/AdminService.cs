using GymManagementBackend.Constants;
using GymManagementBackend.Data;
using GymManagementBackend.DTOs;
using GymManagementBackend.Models;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace GymManagementBackend.Services
{
    public interface IAdminService
    {
        Task<List<GymDto>> GetGymsAsync();
        Task<GymDto> CreateGymAsync(CreateGymDto request);
        Task<GymDto> UpdateGymAsync(Guid gymId, UpdateGymDto request);
        Task<List<AppUserDto>> GetUsersAsync(Guid? gymId = null);
        Task<AppUserDto> CreateUserAsync(CreateUserDto request);
        Task<AppUserDto> CreateUserForGymAsync(Guid gymId, OwnerCreateUserDto request);
        Task<AppUserDto> UpdateUserAsync(Guid userId, UpdateUserDto request);
    }

    public class AdminService : IAdminService
    {
        private readonly GymDbContext _context;
        private readonly ILogger<AdminService> _logger;

        public AdminService(GymDbContext context, ILogger<AdminService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<GymDto>> GetGymsAsync()
        {
            return await GetGymsWithCountsAsync();
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
            return (await GetGymsWithCountsAsync(gym.Id)).Single();
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

            var user = new User
            {
                GymId = request.GymId,
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                Phone = request.Phone?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = role,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User created: {UserId}", user.Id);
            return MapUser(user);
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
            if (role != AppRoles.Staff)
            {
                throw new InvalidOperationException("Owners can only create STAFF users.");
            }

            var user = new User
            {
                GymId = gymId,
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                Phone = request.Phone?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = role,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Owner-created staff user: {UserId}", user.Id);
            return MapUser(user);
        }

        public async Task<AppUserDto> UpdateUserAsync(Guid userId, UpdateUserDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (!string.IsNullOrWhiteSpace(request.FullName)) user.FullName = request.FullName.Trim();
            if (request.Phone is not null) user.Phone = request.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var newRole = request.Role.ToUpperInvariant();
                if (newRole == AppRoles.Admin)
                {
                    throw new InvalidOperationException("Assigning ADMIN role is not allowed via this endpoint.");
                }
                user.Role = newRole;
            }
            if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapUser(user);
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

            var memberCounts = await _context.Members
                .Where(m => gymIds.Contains(m.GymId))
                .GroupBy(m => m.GymId)
                .Select(g => new { GymId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GymId, x => x.Count);

            foreach (var gym in gyms)
            {
                gym.UsersCount = userCounts.TryGetValue(gym.Id, out var uc) ? uc : 0;
                gym.MembersCount = memberCounts.TryGetValue(gym.Id, out var mc) ? mc : 0;
            }

            return gyms;
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
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
