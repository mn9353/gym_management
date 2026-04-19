using GymManagementBackend.Data;
using GymManagementBackend.DTOs;
using GymManagementBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagementBackend.Services
{
    public interface IEnquiryService
    {
        Task<PagedResponseDto<EnquiryListItemDto>> GetEnquiriesAsync(Guid gymId, EnquiryListQueryDto queryDto);
        Task<EnquiryDetailsDto> GetEnquiryByIdAsync(Guid gymId, Guid enquiryId);
        Task<EnquiryDetailsDto> CreateEnquiryAsync(Guid gymId, Guid createdByUserId, CreateEnquiryDto dto);
        Task<EnquiryDetailsDto> AddFollowupAsync(Guid gymId, Guid enquiryId, Guid createdByUserId, AddEnquiryFollowupDto dto);
        Task<EnquiryDetailsDto> UpdateStageAsync(Guid gymId, Guid enquiryId, Guid changedByUserId, UpdateEnquiryStageDto dto);
    }

    public class EnquiryService : IEnquiryService
    {
        private static readonly HashSet<string> AllowedStages = new(StringComparer.OrdinalIgnoreCase)
        {
            "NEW", "CONTACTED", "FOLLOW_UP", "TRIAL", "CONVERTED", "LOST"
        };

        private readonly GymDbContext _context;

        public EnquiryService(GymDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponseDto<EnquiryListItemDto>> GetEnquiriesAsync(Guid gymId, EnquiryListQueryDto queryDto)
        {
            var pageSize = Math.Clamp(queryDto.PageSize, 1, 100);
            var pageNumber = Math.Max(1, queryDto.PageNumber);

            var query = _context.Enquiries
                .AsNoTracking()
                .Where(x => x.GymId == gymId);

            if (!string.IsNullOrWhiteSpace(queryDto.SearchTerm))
            {
                var term = queryDto.SearchTerm.Trim();
                query = query.Where(x =>
                    EF.Functions.ILike(x.FullName, $"%{term}%") ||
                    EF.Functions.ILike(x.Phone, $"%{term}%") ||
                    (x.Email != null && EF.Functions.ILike(x.Email, $"%{term}%")));
            }

            if (!string.IsNullOrWhiteSpace(queryDto.Stage))
            {
                var stage = queryDto.Stage.Trim().ToUpperInvariant();
                query = query.Where(x => x.Stage == stage);
            }

            if (queryDto.NextFollowupFrom.HasValue)
            {
                var from = queryDto.NextFollowupFrom.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(x => x.NextFollowupAt != null && x.NextFollowupAt >= from);
            }

            if (queryDto.NextFollowupTo.HasValue)
            {
                var to = queryDto.NextFollowupTo.Value.ToDateTime(TimeOnly.MaxValue);
                query = query.Where(x => x.NextFollowupAt != null && x.NextFollowupAt <= to);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.UpdatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EnquiryListItemDto
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    Phone = x.Phone,
                    Email = x.Email,
                    Source = x.Source,
                    Stage = x.Stage,
                    NextFollowupAt = x.NextFollowupAt,
                    InterestedServiceTypeId = x.InterestedServiceTypeId,
                    AssignedToUserId = x.AssignedToUserId,
                    FollowupCount = _context.EnquiryFollowups.Count(f => f.EnquiryId == x.Id),
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return new PagedResponseDto<EnquiryListItemDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize))
            };
        }

        public async Task<EnquiryDetailsDto> GetEnquiryByIdAsync(Guid gymId, Guid enquiryId)
        {
            var enquiry = await _context.Enquiries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.GymId == gymId && x.Id == enquiryId)
                ?? throw new KeyNotFoundException("Enquiry not found");

            return await MapEnquiryDetails(gymId, enquiry);
        }

        public async Task<EnquiryDetailsDto> CreateEnquiryAsync(Guid gymId, Guid createdByUserId, CreateEnquiryDto dto)
        {
            var now = DateTime.UtcNow;
            var entity = new Enquiry
            {
                GymId = gymId,
                FullName = dto.FullName.Trim(),
                Phone = dto.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                Source = string.IsNullOrWhiteSpace(dto.Source) ? null : dto.Source.Trim(),
                InterestedServiceTypeId = dto.InterestedServiceTypeId,
                Stage = "NEW",
                NextFollowupAt = dto.NextFollowupAt?.ToUniversalTime(),
                AssignedToUserId = dto.AssignedToUserId,
                Notes = dto.Notes,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Enquiries.Add(entity);
            _context.EnquiryStageHistories.Add(new EnquiryStageHistory
            {
                GymId = gymId,
                EnquiryId = entity.Id,
                FromStage = null,
                ToStage = "NEW",
                ChangedByUserId = createdByUserId,
                Reason = "Enquiry created",
                ChangedAt = now
            });

            await _context.SaveChangesAsync();
            return await GetEnquiryByIdAsync(gymId, entity.Id);
        }

        public async Task<EnquiryDetailsDto> AddFollowupAsync(Guid gymId, Guid enquiryId, Guid createdByUserId, AddEnquiryFollowupDto dto)
        {
            var enquiry = await _context.Enquiries
                .FirstOrDefaultAsync(x => x.GymId == gymId && x.Id == enquiryId)
                ?? throw new KeyNotFoundException("Enquiry not found");

            var now = DateTime.UtcNow;
            _context.EnquiryFollowups.Add(new EnquiryFollowup
            {
                GymId = gymId,
                EnquiryId = enquiryId,
                FollowupAt = (dto.FollowupAt ?? now).ToUniversalTime(),
                NextFollowupAt = dto.NextFollowupAt?.ToUniversalTime(),
                Outcome = dto.Outcome,
                Notes = dto.Notes,
                CreatedByUserId = createdByUserId,
                CreatedAt = now
            });

            if (dto.NextFollowupAt.HasValue)
            {
                enquiry.NextFollowupAt = dto.NextFollowupAt.Value.ToUniversalTime();
            }
            enquiry.UpdatedAt = now;

            await _context.SaveChangesAsync();
            return await GetEnquiryByIdAsync(gymId, enquiryId);
        }

        public async Task<EnquiryDetailsDto> UpdateStageAsync(Guid gymId, Guid enquiryId, Guid changedByUserId, UpdateEnquiryStageDto dto)
        {
            var enquiry = await _context.Enquiries
                .FirstOrDefaultAsync(x => x.GymId == gymId && x.Id == enquiryId)
                ?? throw new KeyNotFoundException("Enquiry not found");

            var toStage = dto.ToStage.Trim().ToUpperInvariant();
            if (!AllowedStages.Contains(toStage))
            {
                throw new InvalidOperationException("Invalid enquiry stage.");
            }

            var fromStage = enquiry.Stage;
            if (string.Equals(fromStage, toStage, StringComparison.OrdinalIgnoreCase))
            {
                return await GetEnquiryByIdAsync(gymId, enquiryId);
            }

            var now = DateTime.UtcNow;
            enquiry.Stage = toStage;
            enquiry.UpdatedAt = now;
            _context.EnquiryStageHistories.Add(new EnquiryStageHistory
            {
                GymId = gymId,
                EnquiryId = enquiryId,
                FromStage = fromStage,
                ToStage = toStage,
                ChangedByUserId = changedByUserId,
                Reason = dto.Reason,
                ChangedAt = now
            });

            await _context.SaveChangesAsync();
            return await GetEnquiryByIdAsync(gymId, enquiryId);
        }

        private async Task<EnquiryDetailsDto> MapEnquiryDetails(Guid gymId, Enquiry enquiry)
        {
            var followups = await _context.EnquiryFollowups
                .AsNoTracking()
                .Where(x => x.GymId == gymId && x.EnquiryId == enquiry.Id)
                .OrderByDescending(x => x.FollowupAt)
                .Select(x => new EnquiryTimelineItemDto
                {
                    Type = "FOLLOWUP",
                    At = x.FollowupAt,
                    Outcome = x.Outcome,
                    Notes = x.Notes,
                    ActorUserId = x.CreatedByUserId
                })
                .ToListAsync();

            var stageHistory = await _context.EnquiryStageHistories
                .AsNoTracking()
                .Where(x => x.GymId == gymId && x.EnquiryId == enquiry.Id)
                .OrderByDescending(x => x.ChangedAt)
                .Select(x => new EnquiryTimelineItemDto
                {
                    Type = "STAGE",
                    At = x.ChangedAt,
                    FromStage = x.FromStage,
                    ToStage = x.ToStage,
                    Notes = x.Reason,
                    ActorUserId = x.ChangedByUserId
                })
                .ToListAsync();

            var timeline = followups.Concat(stageHistory).OrderByDescending(x => x.At).ToList();

            return new EnquiryDetailsDto
            {
                Id = enquiry.Id,
                FullName = enquiry.FullName,
                Phone = enquiry.Phone,
                Email = enquiry.Email,
                Source = enquiry.Source,
                Stage = enquiry.Stage,
                NextFollowupAt = enquiry.NextFollowupAt,
                InterestedServiceTypeId = enquiry.InterestedServiceTypeId,
                AssignedToUserId = enquiry.AssignedToUserId,
                FollowupCount = followups.Count,
                CreatedAt = enquiry.CreatedAt,
                Notes = enquiry.Notes,
                ConvertedMemberId = enquiry.ConvertedMemberId,
                Timeline = timeline
            };
        }
    }
}

