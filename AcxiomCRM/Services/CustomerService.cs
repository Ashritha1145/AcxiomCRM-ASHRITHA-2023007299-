using System.Text.RegularExpressions;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    private static readonly Regex PhoneRegex = new(@"^[6-9]\d{9}$", RegexOptions.Compiled);

    public CustomerService(
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

    public async Task<bool> CanAccessCustomerAsync(int id, string currentUserId, IList<string> roles)
    {
        if (IsAdminOrManager(roles)) return true;
        return await _context.Customers.AnyAsync(c => c.CustomerId == id && c.CreatedBy == currentUserId);
    }

    public async Task<CustomerIndexViewModel> GetCustomersAsync(string currentUserId, IList<string> roles, string? searchTerm, string? statusFilter, int page, int pageSize)
    {
        var query = _context.Customers.AsNoTracking().AsQueryable();

        if (!IsAdminOrManager(roles))
        {
            query = query.Where(c => c.CreatedBy == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.CustomerName.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                c.Phone.Contains(term) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)) ||
                c.CustomerCode.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(c => c.Status == statusFilter);
        }

        int totalCount = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;

        var items = await query.OrderByDescending(c => c.CreatedDate)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return new CustomerIndexViewModel
        {
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalCount = totalCount,
            Customers = items
        };
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id, string currentUserId, IList<string> roles)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer == null) return null;

        if (!IsAdminOrManager(roles) && customer.CreatedBy != currentUserId)
        {
            return null; // Scoped access denied
        }

        return customer;
    }

    public async Task<CustomerDetailsViewModel?> GetCustomerDetailsAsync(int id, string currentUserId, IList<string> roles)
    {
        var customer = await _context.Customers
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .Include(c => c.Activities)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null) return null;

        if (!IsAdminOrManager(roles) && customer.CreatedBy != currentUserId)
        {
            return null; // Scoped access denied
        }

        string creatorName = "System";
        var creator = await _userManager.FindByIdAsync(customer.CreatedBy);
        if (creator != null)
        {
            creatorName = $"{creator.FullName} ({creator.Email})";
        }

        return new CustomerDetailsViewModel
        {
            Customer = customer,
            CreatedByName = creatorName,
            Opportunities = customer.Opportunities.OrderByDescending(o => o.CreatedDate).ToList(),
            FollowUps = customer.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList(),
            Activities = customer.Activities.OrderByDescending(a => a.ActivityDate).ToList()
        };
    }

    public async Task<(bool Success, string? Error, Customer? Customer)> CreateCustomerAsync(CustomerCreateEditViewModel model, string currentUserId, string? ipAddress)
    {
        // Server validation: Indian phone regex
        if (string.IsNullOrWhiteSpace(model.Phone) || !PhoneRegex.IsMatch(model.Phone))
        {
            return (false, "Enter a valid 10-digit Indian mobile number (e.g. 9876543210).", null);
        }

        // Server validation: Duplicate Email
        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == model.Email.ToLower()))
        {
            return (false, "A customer with this email address already exists.", null);
        }

        // Server validation: Duplicate Phone
        if (await _context.Customers.AnyAsync(c => c.Phone == model.Phone))
        {
            return (false, "A customer with this phone number already exists.", null);
        }

        // Server validation: Duplicate Code
        if (await _context.Customers.AnyAsync(c => c.CustomerCode.ToLower() == model.CustomerCode.ToLower()))
        {
            return (false, "A customer with this customer code already exists.", null);
        }

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
            Status = model.Status,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserId
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "Create",
            "Customer",
            customer.CustomerId.ToString(),
            null,
            new { customer.CustomerCode, customer.CustomerName, customer.Email, customer.Phone, customer.Status },
            ipAddress,
            "Success");

        return (true, null, customer);
    }

    public async Task<(bool Success, string? Error)> UpdateCustomerAsync(CustomerCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == model.CustomerId);
        if (customer == null)
            return (false, "Customer not found.");

        if (!IsAdminOrManager(roles) && customer.CreatedBy != currentUserId)
            return (false, "You do not have permission to modify this customer.");

        // Phone regex validation
        if (string.IsNullOrWhiteSpace(model.Phone) || !PhoneRegex.IsMatch(model.Phone))
        {
            return (false, "Enter a valid 10-digit Indian mobile number (e.g. 9876543210).");
        }

        // Duplicate Email check excluding current customer
        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == model.Email.ToLower() && c.CustomerId != model.CustomerId))
        {
            return (false, "A customer with this email address already exists.");
        }

        // Duplicate Phone check excluding current customer
        if (await _context.Customers.AnyAsync(c => c.Phone == model.Phone && c.CustomerId != model.CustomerId))
        {
            return (false, "A customer with this phone number already exists.");
        }

        var oldState = new { customer.CustomerName, customer.Email, customer.Phone, customer.Status, customer.CompanyName };

        customer.CustomerName = model.CustomerName.Trim();
        customer.Email = model.Email.Trim().ToLower();
        customer.Phone = model.Phone.Trim();
        customer.CompanyName = model.CompanyName?.Trim();
        customer.Address = model.Address?.Trim();
        customer.City = model.City?.Trim();
        customer.State = model.State?.Trim();
        customer.Status = model.Status;
        customer.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var newState = new { customer.CustomerName, customer.Email, customer.Phone, customer.Status, customer.CompanyName };

        await _auditService.LogAsync(
            currentUserId,
            "Update",
            "Customer",
            customer.CustomerId.ToString(),
            oldState,
            newState,
            ipAddress,
            "Success");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteCustomerAsync(int id, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer == null)
            return (false, "Customer not found.");

        if (!IsAdminOrManager(roles) && customer.CreatedBy != currentUserId)
            return (false, "You do not have permission to delete this customer.");

        var oldState = new { customer.CustomerId, customer.CustomerName, customer.Status };

        // Soft delete: set Status = Inactive
        customer.Status = "Inactive";
        customer.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "Delete",
            "Customer",
            customer.CustomerId.ToString(),
            oldState,
            new { customer.CustomerId, customer.CustomerName, Status = "Inactive" },
            ipAddress,
            "Success");

        return (true, null);
    }

    // REST API methods
    public async Task<List<CustomerDto>> GetAllCustomerDtosAsync(string currentUserId, IList<string> roles, string? search)
    {
        var query = _context.Customers.AsNoTracking().AsQueryable();
        if (!IsAdminOrManager(roles))
        {
            query = query.Where(c => c.CreatedBy == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.CustomerName.ToLower().Contains(term) || c.Email.ToLower().Contains(term) || c.Phone.Contains(term));
        }

        return await query.Select(c => new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            CreatedDate = c.CreatedDate,
            CreatedBy = c.CreatedBy
        }).ToListAsync();
    }

    public async Task<CustomerDto?> GetCustomerDtoByIdAsync(int id, string currentUserId, IList<string> roles)
    {
        var c = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerId == id);
        if (c == null) return null;

        if (!IsAdminOrManager(roles) && c.CreatedBy != currentUserId)
            return null;

        return new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            CreatedDate = c.CreatedDate,
            CreatedBy = c.CreatedBy
        };
    }

    public async Task<(bool Success, string? Error, CustomerDto? Customer)> CreateFromDtoAsync(CreateCustomerDto dto, string currentUserId, string? ipAddress)
    {
        var vm = new CustomerCreateEditViewModel
        {
            CustomerCode = dto.CustomerCode,
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = "Active"
        };

        var (success, error, customer) = await CreateCustomerAsync(vm, currentUserId, ipAddress);
        if (!success || customer == null)
            return (false, error, null);

        return (true, null, new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            CreatedDate = customer.CreatedDate,
            CreatedBy = customer.CreatedBy
        });
    }

    public async Task<(bool Success, string? Error)> UpdateFromDtoAsync(int id, UpdateCustomerDto dto, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var vm = new CustomerCreateEditViewModel
        {
            CustomerId = id,
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = dto.Status
        };

        return await UpdateCustomerAsync(vm, currentUserId, roles, ipAddress);
    }
}
