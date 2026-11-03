using System.Security.Claims;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private IList<string> GetUserRoles() =>
        User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

    private string? GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, int page = 1)
    {
        var model = await _customerService.GetCustomersAsync(
            GetCurrentUserId(), GetUserRoles(), searchTerm, statusFilter, page, 10);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _customerService.GetCustomerDetailsAsync(
            id, GetCurrentUserId(), GetUserRoles());

        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new CustomerCreateEditViewModel
        {
            CustomerCode = $"CUST-{DateTime.UtcNow:MMddHHmm}"
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error, customer) = await _customerService.CreateCustomerAsync(
            model, GetCurrentUserId(), GetIpAddress());

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to create customer.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Customer '{customer!.CustomerName}' was created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(
            id, GetCurrentUserId(), GetUserRoles());

        if (customer == null)
            return NotFound();

        var model = new CustomerCreateEditViewModel
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
            Status = customer.Status
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error) = await _customerService.UpdateCustomerAsync(
            model, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to update customer.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Customer '{model.CustomerName}' was updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _customerService.DeleteCustomerAsync(
            id, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to deactivate customer.";
        }
        else
        {
            TempData["SuccessMessage"] = "Customer has been deactivated successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
