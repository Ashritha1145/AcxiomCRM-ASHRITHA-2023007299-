using System.Security.Claims;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class LeadsController : Controller
{
    private readonly ILeadService _leadService;

    public LeadsController(ILeadService leadService)
    {
        _leadService = leadService;
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private IList<string> GetUserRoles() =>
        User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

    private string? GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, string? assignedToFilter, int page = 1)
    {
        var model = await _leadService.GetLeadsAsync(
            GetCurrentUserId(), GetUserRoles(), searchTerm, statusFilter, assignedToFilter, page, 10);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _leadService.GetLeadDetailsAsync(
            id, GetCurrentUserId(), GetUserRoles());

        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new LeadCreateEditViewModel
        {
            LeadCode = $"LEAD-{DateTime.UtcNow:MMddHHmm}"
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error, lead) = await _leadService.CreateLeadAsync(
            model, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to create lead.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Lead '{lead!.LeadName}' was created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lead = await _leadService.GetLeadByIdAsync(
            id, GetCurrentUserId(), GetUserRoles());

        if (lead == null)
            return NotFound();

        var model = new LeadCreateEditViewModel
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
            AssignedTo = lead.AssignedTo
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LeadCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error) = await _leadService.UpdateLeadAsync(
            model, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to update lead.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Lead '{model.LeadName}' was updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await _leadService.GetLeadByIdAsync(
            id, GetCurrentUserId(), GetUserRoles());

        if (lead == null)
            return NotFound();

        if (lead.Status != "Qualified")
        {
            TempData["ErrorMessage"] = "Only Qualified leads can be converted into a Customer.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new ConvertLeadViewModel
        {
            LeadId = lead.LeadId,
            LeadName = lead.LeadName,
            CompanyName = lead.CompanyName ?? string.Empty,
            Email = lead.Email,
            Phone = lead.Phone,
            CustomerCode = $"CUST-{DateTime.UtcNow:MMddHHmm}",
            CustomerName = lead.CompanyName ?? lead.LeadName,
            OpportunityName = $"{lead.CompanyName ?? lead.LeadName} - Expansion Deal",
            OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 100000m,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(ConvertLeadViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error, customerId) = await _leadService.ConvertLeadAsync(
            model, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to convert lead.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Lead successfully converted to Customer!";
        return RedirectToAction("Details", "Customers", new { id = customerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _leadService.DeleteLeadAsync(
            id, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete lead.";
        }
        else
        {
            TempData["SuccessMessage"] = "Lead has been deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
