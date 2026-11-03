using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public class OpportunityService : IOpportunityService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OpportunityService(
        ApplicationDbContext context,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _auditService = auditService;
        _userManager = userManager;
    }

    private static bool IsAdminOrManager(IList<string> roles) =>
        roles.Contains("Admin") || roles.Contains("Manager");

    public async Task<OpportunityIndexViewModel> GetOpportunitiesAsync(string currentUserId, IList<string> roles, string? searchTerm, string? stageFilter, string? statusFilter, int? customerFilter, int page, int pageSize)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .AsNoTracking()
            .AsQueryable();

        if (!IsAdminOrManager(roles))
        {
            query = query.Where(o => o.AssignedTo == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(o =>
                o.OpportunityName.ToLower().Contains(term) ||
                (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)) ||
                (o.Lead != null && o.Lead.LeadName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(stageFilter))
        {
            query = query.Where(o => o.Stage == stageFilter);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(o => o.Status == statusFilter);
        }

        if (customerFilter.HasValue && customerFilter.Value > 0)
        {
            query = query.Where(o => o.CustomerId == customerFilter.Value);
        }

        // Metrics across scoped query
        var allScoped = await query.ToListAsync();
        var openOpportunities = allScoped.Where(o => o.Status == "Active").ToList();
        decimal totalPipelineValue = openOpportunities.Sum(o => o.Amount);
        decimal weightedPipelineValue = openOpportunities.Sum(o => o.WeightedValue);
        int wonCount = allScoped.Count(o => o.Stage == "Won" || o.Status == "Won");
        int lostCount = allScoped.Count(o => o.Stage == "Lost" || o.Status == "Lost");

        int totalCount = allScoped.Count;
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;

        var items = allScoped
            .OrderByDescending(o => o.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var customers = await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
        var customersList = customers.Select(c => new SelectListItem
        {
            Value = c.CustomerId.ToString(),
            Text = $"{c.CustomerName} ({c.CustomerCode})"
        }).ToList();

        return new OpportunityIndexViewModel
        {
            SearchTerm = searchTerm,
            StageFilter = stageFilter,
            StatusFilter = statusFilter,
            CustomerFilter = customerFilter,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalCount = totalCount,
            Opportunities = items,
            CustomersList = customersList,
            TotalPipelineValue = totalPipelineValue,
            WeightedPipelineValue = weightedPipelineValue,
            TotalOpportunities = totalCount,
            WonCount = wonCount,
            LostCount = lostCount
        };
    }

    public async Task<Opportunity?> GetOpportunityByIdAsync(int id, string currentUserId, IList<string> roles)
    {
        var opp = await _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opp == null) return null;

        if (!IsAdminOrManager(roles) && opp.AssignedTo != currentUserId)
            return null;

        return opp;
    }

    public async Task<OpportunityDetailsViewModel?> GetOpportunityDetailsAsync(int id, string currentUserId, IList<string> roles)
    {
        var opp = await GetOpportunityByIdAsync(id, currentUserId, roles);
        if (opp == null) return null;

        string assignedName = "Unassigned";
        var user = await _userManager.FindByIdAsync(opp.AssignedTo);
        if (user != null)
        {
            assignedName = $"{user.FullName} ({user.Email})";
        }

        return new OpportunityDetailsViewModel
        {
            Opportunity = opp,
            AssignedToName = assignedName
        };
    }

    public async Task<(bool Success, string? Error, Opportunity? Opportunity)> CreateOpportunityAsync(OpportunityCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(model.OpportunityName))
            return (false, "Opportunity Name is required.", null);

        // Foreign key check
        var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == model.CustomerId);
        if (!customerExists)
            return (false, "Specified Customer does not exist.", null);

        // Business rule 1: Amount > 0
        if (model.Amount <= 0)
            return (false, "Opportunity Amount must be greater than 0.", null);

        // Business rule 2: Probability 0 - 100
        if (model.Probability < 0 || model.Probability > 100)
            return (false, "Probability must be between 0 and 100.", null);

        // Business rule 3: ExpectedCloseDate cannot be in the past for active opportunities
        if (model.Status == "Active" && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            return (false, "Expected Close Date cannot be in the past.", null);

        string assignedTo = model.AssignedTo;
        if (!IsAdminOrManager(roles) || string.IsNullOrWhiteSpace(assignedTo))
        {
            assignedTo = currentUserId;
        }

        var opp = new Opportunity
        {
            OpportunityName = model.OpportunityName.Trim(),
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            Amount = model.Amount,
            Stage = model.Stage,
            Probability = model.Probability,
            ExpectedCloseDate = model.ExpectedCloseDate,
            Status = model.Status,
            Source = model.Source?.Trim(),
            Notes = model.Notes?.Trim(),
            CreatedDate = DateTime.UtcNow,
            AssignedTo = assignedTo
        };

        _context.Opportunities.Add(opp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "Create",
            "Opportunity",
            opp.OpportunityId.ToString(),
            null,
            new { opp.OpportunityName, opp.CustomerId, opp.Amount, opp.Stage, opp.Probability },
            ipAddress,
            "Success");

        return (true, null, opp);
    }

    public async Task<(bool Success, string? Error)> UpdateOpportunityAsync(OpportunityCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var opp = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == model.OpportunityId);
        if (opp == null)
            return (false, "Opportunity not found.");

        if (!IsAdminOrManager(roles) && opp.AssignedTo != currentUserId)
            return (false, "You do not have permission to modify this opportunity.");

        var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == model.CustomerId);
        if (!customerExists)
            return (false, "Specified Customer does not exist.");

        if (model.Amount <= 0)
            return (false, "Opportunity Amount must be greater than 0.");

        if (model.Probability < 0 || model.Probability > 100)
            return (false, "Probability must be between 0 and 100.");

        if (model.Status == "Active" && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            return (false, "Expected Close Date cannot be in the past.");

        var oldState = new { opp.OpportunityName, opp.CustomerId, opp.Amount, opp.Stage, opp.Probability, opp.Status, opp.ExpectedCloseDate };

        opp.OpportunityName = model.OpportunityName.Trim();
        opp.CustomerId = model.CustomerId;
        opp.LeadId = model.LeadId;
        opp.Amount = model.Amount;
        opp.Stage = model.Stage;
        opp.Probability = model.Probability;
        opp.ExpectedCloseDate = model.ExpectedCloseDate;
        opp.Status = model.Status;
        opp.Source = model.Source?.Trim();
        opp.Notes = model.Notes?.Trim();

        if (IsAdminOrManager(roles) && !string.IsNullOrWhiteSpace(model.AssignedTo))
        {
            opp.AssignedTo = model.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newState = new { opp.OpportunityName, opp.CustomerId, opp.Amount, opp.Stage, opp.Probability, opp.Status, opp.ExpectedCloseDate };

        await _auditService.LogAsync(
            currentUserId,
            "Update",
            "Opportunity",
            opp.OpportunityId.ToString(),
            oldState,
            newState,
            ipAddress,
            "Success");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateStageAsync(int id, string newStage, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var opp = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == id);
        if (opp == null) return (false, "Opportunity not found.");

        if (!IsAdminOrManager(roles) && opp.AssignedTo != currentUserId)
            return (false, "You do not have permission to modify this opportunity.");

        var oldStage = opp.Stage;
        opp.Stage = newStage;

        if (newStage == "Won")
        {
            opp.Status = "Won";
            opp.Probability = 100;
        }
        else if (newStage == "Lost")
        {
            opp.Status = "Lost";
            opp.Probability = 0;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "Stage Change",
            "Opportunity",
            opp.OpportunityId.ToString(),
            new { Stage = oldStage },
            new { Stage = newStage, opp.Status, opp.Probability },
            ipAddress,
            "Success");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteOpportunityAsync(int id, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var opp = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == id);
        if (opp == null)
            return (false, "Opportunity not found.");

        if (!IsAdminOrManager(roles) && opp.AssignedTo != currentUserId)
            return (false, "You do not have permission to delete this opportunity.");

        var oldState = new { opp.OpportunityId, opp.OpportunityName, opp.Amount, opp.Stage };

        _context.Opportunities.Remove(opp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "Delete",
            "Opportunity",
            id.ToString(),
            oldState,
            null,
            ipAddress,
            "Success");

        return (true, null);
    }

    public async Task<List<OpportunityDto>> GetAllOpportunityDtosAsync(string currentUserId, IList<string> roles, string? search)
    {
        var query = _context.Opportunities.Include(o => o.Customer).AsNoTracking().AsQueryable();
        if (!IsAdminOrManager(roles))
        {
            query = query.Where(o => o.AssignedTo == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(o => o.OpportunityName.ToLower().Contains(term) || (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)));
        }

        return await query.Select(o => new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer != null ? o.Customer.CustomerName : "Unknown",
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            WeightedValue = (o.Amount * o.Probability) / 100m,
            CreatedDate = o.CreatedDate,
            AssignedTo = o.AssignedTo
        }).ToListAsync();
    }

    public async Task<(bool Success, string? Error, OpportunityDto? Opportunity)> CreateFromDtoAsync(CreateOpportunityDto dto, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var vm = new OpportunityCreateEditViewModel
        {
            OpportunityName = dto.OpportunityName,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = dto.Status,
            Source = dto.Source,
            Notes = dto.Notes,
            AssignedTo = dto.AssignedTo ?? currentUserId
        };

        var (success, error, opp) = await CreateOpportunityAsync(vm, currentUserId, roles, ipAddress);
        if (!success || opp == null)
            return (false, error, null);

        var customer = await _context.Customers.FindAsync(opp.CustomerId);

        return (true, null, new OpportunityDto
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            CustomerName = customer?.CustomerName ?? string.Empty,
            LeadId = opp.LeadId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            WeightedValue = opp.WeightedValue,
            CreatedDate = opp.CreatedDate,
            AssignedTo = opp.AssignedTo
        });
    }
}
