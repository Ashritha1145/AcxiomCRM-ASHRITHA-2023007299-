using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services;

public interface ICustomerService
{
    Task<CustomerIndexViewModel> GetCustomersAsync(string currentUserId, IList<string> roles, string? searchTerm, string? statusFilter, int page, int pageSize);
    Task<Customer?> GetCustomerByIdAsync(int id, string currentUserId, IList<string> roles);
    Task<CustomerDetailsViewModel?> GetCustomerDetailsAsync(int id, string currentUserId, IList<string> roles);
    Task<(bool Success, string? Error, Customer? Customer)> CreateCustomerAsync(CustomerCreateEditViewModel model, string currentUserId, string? ipAddress);
    Task<(bool Success, string? Error)> UpdateCustomerAsync(CustomerCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress);
    Task<(bool Success, string? Error)> DeleteCustomerAsync(int id, string currentUserId, IList<string> roles, string? ipAddress);
    Task<bool> CanAccessCustomerAsync(int id, string currentUserId, IList<string> roles);

    // API methods
    Task<List<CustomerDto>> GetAllCustomerDtosAsync(string currentUserId, IList<string> roles, string? search);
    Task<CustomerDto?> GetCustomerDtoByIdAsync(int id, string currentUserId, IList<string> roles);
    Task<(bool Success, string? Error, CustomerDto? Customer)> CreateFromDtoAsync(CreateCustomerDto dto, string currentUserId, string? ipAddress);
    Task<(bool Success, string? Error)> UpdateFromDtoAsync(int id, UpdateCustomerDto dto, string currentUserId, IList<string> roles, string? ipAddress);
}
