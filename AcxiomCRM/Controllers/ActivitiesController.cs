using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly IActivityService _activityService;
    private readonly ApplicationDbContext _context;

    public ActivitiesController(IActivityService activityService, ApplicationDbContext context)
    {
        _activityService = activityService;
        _context = context;
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private IList<string> GetUserRoles() =>
        User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

    private string? GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private async Task PopulateDropDowns(ActivityCreateEditViewModel model)
    {
        var customers = await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
        model.CustomersList = customers.Select(c => new SelectListItem
        {
            Value = c.CustomerId.ToString(),
            Text = $"{c.CustomerName} ({c.CustomerCode})"
        }).ToList();

        var leads = await _context.Leads.OrderBy(l => l.LeadName).ToListAsync();
        model.LeadsList = leads.Select(l => new SelectListItem
        {
            Value = l.LeadId.ToString(),
            Text = $"{l.LeadName} ({l.LeadCode})"
        }).ToList();
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? typeFilter, string? statusFilter, int page = 1)
    {
        var model = await _activityService.GetActivitiesAsync(
            GetCurrentUserId(), GetUserRoles(), searchTerm, typeFilter, statusFilter, page, 10);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var model = new ActivityCreateEditViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            ActivityDate = DateTime.UtcNow,
            ActivityType = "Call",
            Status = "Pending"
        };

        await PopulateDropDowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropDowns(model);
            return View(model);
        }

        var (success, error, activity) = await _activityService.CreateActivityAsync(
            model, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to log activity.");
            await PopulateDropDowns(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Activity logged successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _activityService.GetActivityByIdAsync(
            id, GetCurrentUserId(), GetUserRoles());

        if (item == null)
            return NotFound();

        var model = new ActivityCreateEditViewModel
        {
            ActivityId = item.ActivityId,
            ActivityType = item.ActivityType,
            Subject = item.Subject,
            Description = item.Description,
            ActivityDate = item.ActivityDate,
            CustomerId = item.CustomerId,
            LeadId = item.LeadId,
            Status = item.Status,
            AssignedTo = item.AssignedTo
        };

        await PopulateDropDowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ActivityCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropDowns(model);
            return View(model);
        }

        var (success, error) = await _activityService.UpdateActivityAsync(
            model, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to update activity.");
            await PopulateDropDowns(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Activity updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _activityService.DeleteActivityAsync(
            id, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete activity.";
        }
        else
        {
            TempData["SuccessMessage"] = "Activity deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
