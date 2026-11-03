using System.Text.RegularExpressions;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public class LeadService : ILeadService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    private static readonly Regex PhoneRegex = new(@"^[6-9]\d{9}$", RegexOptions.Compiled);
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost"
    };

    public LeadService(
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

    private static bool IsValidTransition(string currentStatus, string newStatus)
    {
        if (string.Equals(currentStatus, newStatus, StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(currentStatus, "Converted", StringComparison.OrdinalIgnoreCase))
            return false; // Converted is final

        return currentStatus.ToLower() switch
        {
            "new" => newStatus.ToLower() is "contacted" or "lost" or "unqualified",
            "contacted" => newStatus.ToLower() is "qualified" or "lost" or "unqualified",
            "qualified" => newStatus.ToLower() is "converted" or "lost" or "contacted",
            "unqualified" => newStatus.ToLower() is "contacted" or "lost" or "new",
            "lost" => newStatus.ToLower() is "new" or "contacted",
            _ => false
        };
    }

    public async Task<LeadIndexViewModel> GetLeadsAsync(string currentUserId, IList<string> roles, string? searchTerm, string? statusFilter, string? assignedToFilter, int page, int pageSize)
    {
        var query = _context.Leads.AsNoTracking().AsQueryable();

        if (!IsAdminOrManager(roles))
        {
            query = query.Where(l => l.AssignedTo == currentUserId);
        }
        else if (!string.IsNullOrWhiteSpace(assignedToFilter))
        {
            query = query.Where(l => l.AssignedTo == assignedToFilter);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(l =>
                l.LeadName.ToLower().Contains(term) ||
                l.Email.ToLower().Contains(term) ||
                l.Phone.Contains(term) ||
                (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)) ||
                l.LeadCode.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(l => l.Status == statusFilter);
        }

        int totalCount = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;

        var items = await query.OrderByDescending(l => l.CreatedDate)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
        var usersList = users.Select(u => new SelectListItem
        {
            Value = u.Id,
            Text = $"{u.FullName} ({u.Email})"
        }).ToList();

        return new LeadIndexViewModel
        {
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            AssignedToFilter = assignedToFilter,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalCount = totalCount,
            Leads = items,
            UsersList = usersList
        };
    }

    public async Task<Lead?> GetLeadByIdAsync(int id, string currentUserId, IList<string> roles)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == id);
        if (lead == null) return null;

        if (!IsAdminOrManager(roles) && lead.AssignedTo != currentUserId)
            return null;

        return lead;
    }

    public async Task<LeadDetailsViewModel?> GetLeadDetailsAsync(int id, string currentUserId, IList<string> roles)
    {
        var lead = await _context.Leads
            .Include(l => l.Opportunities)
            .Include(l => l.FollowUps)
            .Include(l => l.Activities)
            .Include(l => l.ConvertedCustomer)
            .FirstOrDefaultAsync(l => l.LeadId == id);

        if (lead == null) return null;

        if (!IsAdminOrManager(roles) && lead.AssignedTo != currentUserId)
            return null;

        string assignedName = "Unassigned";
        var assignedUser = await _userManager.FindByIdAsync(lead.AssignedTo);
        if (assignedUser != null)
        {
            assignedName = $"{assignedUser.FullName} ({assignedUser.Email})";
        }

        return new LeadDetailsViewModel
        {
            Lead = lead,
            AssignedToName = assignedName,
            Opportunities = lead.Opportunities.OrderByDescending(o => o.CreatedDate).ToList(),
            FollowUps = lead.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList(),
            Activities = lead.Activities.OrderByDescending(a => a.ActivityDate).ToList()
        };
    }

    public async Task<(bool Success, string? Error, Lead? Lead)> CreateLeadAsync(LeadCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(model.LeadName))
            return (false, "Lead Name is required.", null);

        if (!ValidStatuses.Contains(model.Status))
            return (false, "Invalid lead status specified.", null);

        if (model.ExpectedValue < 0 || model.ExpectedValue > 100000000)
            return (false, "Expected Value must be between 0 and 100,000,000.", null);

        if (string.IsNullOrWhiteSpace(model.Phone) || !PhoneRegex.IsMatch(model.Phone))
            return (false, "Enter a valid 10-digit Indian mobile number (e.g. 9876543210).", null);

        if (await _context.Leads.AnyAsync(l => l.LeadCode.ToLower() == model.LeadCode.ToLower()))
            return (false, "A lead with this code already exists.", null);

        string assignedTo = model.AssignedTo;
        if (!IsAdminOrManager(roles) || string.IsNullOrWhiteSpace(assignedTo))
        {
            assignedTo = currentUserId;
        }

        var lead = new Lead
        {
            LeadCode = model.LeadCode.Trim().ToUpper(),
            LeadName = model.LeadName.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone.Trim(),
            CompanyName = model.CompanyName?.Trim(),
            Source = model.Source,
            Status = model.Status,
            Priority = model.Priority,
            ExpectedValue = model.ExpectedValue,
            CreatedDate = DateTime.UtcNow,
            AssignedTo = assignedTo
        };

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "Create",
            "Lead",
            lead.LeadId.ToString(),
            null,
            new { lead.LeadCode, lead.LeadName, lead.Email, lead.Status, lead.ExpectedValue },
            ipAddress,
            "Success");

        return (true, null, lead);
    }

    public async Task<(bool Success, string? Error)> UpdateLeadAsync(LeadCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == model.LeadId);
        if (lead == null)
            return (false, "Lead not found.");

        if (!IsAdminOrManager(roles) && lead.AssignedTo != currentUserId)
            return (false, "You do not have permission to modify this lead.");

        if (!ValidStatuses.Contains(model.Status))
            return (false, "Invalid lead status specified.");

        // Check valid status transition
        if (!IsValidTransition(lead.Status, model.Status))
        {
            return (false, $"Invalid status transition from '{lead.Status}' to '{model.Status}'. A New lead cannot jump directly to Converted.");
        }

        if (model.ExpectedValue < 0 || model.ExpectedValue > 100000000)
            return (false, "Expected Value must be between 0 and 100,000,000.");

        if (string.IsNullOrWhiteSpace(model.Phone) || !PhoneRegex.IsMatch(model.Phone))
            return (false, "Enter a valid 10-digit Indian mobile number (e.g. 9876543210).");

        var oldState = new { lead.LeadName, lead.Email, lead.Status, lead.ExpectedValue, lead.AssignedTo };

        lead.LeadName = model.LeadName.Trim();
        lead.Email = model.Email.Trim().ToLower();
        lead.Phone = model.Phone.Trim();
        lead.CompanyName = model.CompanyName?.Trim();
        lead.Source = model.Source;
        lead.Status = model.Status;
        lead.Priority = model.Priority;
        lead.ExpectedValue = model.ExpectedValue;

        if (IsAdminOrManager(roles) && !string.IsNullOrWhiteSpace(model.AssignedTo))
        {
            lead.AssignedTo = model.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newState = new { lead.LeadName, lead.Email, lead.Status, lead.ExpectedValue, lead.AssignedTo };

        await _auditService.LogAsync(
            currentUserId,
            "Update",
            "Lead",
            lead.LeadId.ToString(),
            oldState,
            newState,
            ipAddress,
            "Success");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteLeadAsync(int id, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == id);
        if (lead == null)
            return (false, "Lead not found.");

        if (!IsAdminOrManager(roles) && lead.AssignedTo != currentUserId)
            return (false, "You do not have permission to delete this lead.");

        var oldState = new { lead.LeadId, lead.LeadName, lead.Status };

        _context.Leads.Remove(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "Delete",
            "Lead",
            id.ToString(),
            oldState,
            null,
            ipAddress,
            "Success");

        return (true, null);
    }

    public async Task<(bool Success, string? Error, int? CustomerId)> ConvertLeadAsync(ConvertLeadViewModel model, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == model.LeadId);
        if (lead == null)
            return (false, "Lead not found.", null);

        if (!IsAdminOrManager(roles) && lead.AssignedTo != currentUserId)
            return (false, "You do not have permission to convert this lead.", null);

        if (lead.Status == "Converted")
            return (false, "This lead has already been converted.", null);

        if (!string.Equals(lead.Status, "Qualified", StringComparison.OrdinalIgnoreCase))
            return (false, "Only Qualified leads can be converted into a Customer.", null);

        // Check duplicate Customer Code / Email / Phone
        if (await _context.Customers.AnyAsync(c => c.CustomerCode.ToLower() == model.CustomerCode.ToLower()))
            return (false, "A customer with this code already exists.", null);

        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == model.Email.ToLower()))
            return (false, "A customer with this email already exists.", null);

        if (await _context.Customers.AnyAsync(c => c.Phone == model.Phone))
            return (false, "A customer with this phone number already exists.", null);

        // Create Customer
        var customer = new Customer
        {
            CustomerCode = model.CustomerCode.Trim().ToUpper(),
            CustomerName = model.CustomerName.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone.Trim(),
            CompanyName = model.CompanyName?.Trim(),
            Address = model.Address?.Trim(),
            City = model.City?.Trim(),
            State = model.State?.Trim(),
            Status = "Active",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserId
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // Optionally create Opportunity
        if (model.CreateOpportunity && !string.IsNullOrWhiteSpace(model.OpportunityName))
        {
            if (model.OpportunityAmount <= 0)
                return (false, "Opportunity Amount must be greater than 0.", null);

            var closeDate = model.ExpectedCloseDate ?? DateTime.UtcNow.AddDays(30);
            if (closeDate.Date < DateTime.UtcNow.Date)
                return (false, "Expected Close Date cannot be in the past.", null);

            var opportunity = new Opportunity
            {
                OpportunityName = model.OpportunityName.Trim(),
                CustomerId = customer.CustomerId,
                LeadId = lead.LeadId,
                Amount = model.OpportunityAmount,
                Stage = "Qualification",
                Probability = 25,
                ExpectedCloseDate = closeDate,
                Status = "Active",
                Source = "Lead Conversion",
                CreatedDate = DateTime.UtcNow,
                AssignedTo = lead.AssignedTo
            };

            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();
        }

        // Update lead status to Converted
        lead.Status = "Converted";
        lead.ConvertedCustomerId = customer.CustomerId;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "Lead Conversion",
            "Lead",
            lead.LeadId.ToString(),
            new { Status = "Qualified" },
            new { Status = "Converted", CreatedCustomerId = customer.CustomerId },
            ipAddress,
            "Success");

        return (true, null, customer.CustomerId);
    }

    public async Task<List<LeadDto>> GetAllLeadDtosAsync(string currentUserId, IList<string> roles, string? search)
    {
        var query = _context.Leads.AsNoTracking().AsQueryable();
        if (!IsAdminOrManager(roles))
        {
            query = query.Where(l => l.AssignedTo == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(l => l.LeadName.ToLower().Contains(term) || l.Email.ToLower().Contains(term) || l.Phone.Contains(term));
        }

        return await query.Select(l => new LeadDto
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status,
            Priority = l.Priority,
            ExpectedValue = l.ExpectedValue,
            CreatedDate = l.CreatedDate,
            AssignedTo = l.AssignedTo,
            ConvertedCustomerId = l.ConvertedCustomerId
        }).ToListAsync();
    }

    public async Task<(bool Success, string? Error, LeadDto? Lead)> CreateFromDtoAsync(CreateLeadDto dto, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var vm = new LeadCreateEditViewModel
        {
            LeadCode = dto.LeadCode,
            LeadName = dto.LeadName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Source = dto.Source,
            Status = dto.Status,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            AssignedTo = dto.AssignedTo ?? currentUserId
        };

        var (success, error, lead) = await CreateLeadAsync(vm, currentUserId, roles, ipAddress);
        if (!success || lead == null)
            return (false, error, null);

        return (true, null, new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            CreatedDate = lead.CreatedDate,
            AssignedTo = lead.AssignedTo,
            ConvertedCustomerId = lead.ConvertedCustomerId
        });
    }
}
