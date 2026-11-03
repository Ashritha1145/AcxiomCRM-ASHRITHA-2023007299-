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
public class FollowUpsController : Controller
{
    private readonly IFollowUpService _followUpService;
    private readonly ApplicationDbContext _context;

    public FollowUpsController(IFollowUpService followUpService, ApplicationDbContext context)
    {
        _followUpService = followUpService;
        _context = context;
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private IList<string> GetUserRoles() =>
        User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

    private string? GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private async Task PopulateDropDowns(FollowUpCreateEditViewModel model)
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
    public async Task<IActionResult> Index(string? statusFilter, string? typeFilter, string? dateFilter, int page = 1)
    {
        var model = await _followUpService.GetFollowUpsAsync(
            GetCurrentUserId(), GetUserRoles(), statusFilter, typeFilter, dateFilter, page, 10);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var model = new FollowUpCreateEditViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            FollowUpDate = DateTime.UtcNow.AddDays(1),
            FollowUpType = "Call",
            Status = "Planned"
        };

        await PopulateDropDowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpCreateEditViewModel model)
    {
        // Business rule: Follow-up date cannot be earlier than today for a new/planned follow-up
        if (model.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDowns(model);
            return View(model);
        }

        var (success, error, followUp) = await _followUpService.CreateFollowUpAsync(
            model, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to create follow-up.");
            await PopulateDropDowns(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Follow-up scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _followUpService.GetFollowUpByIdAsync(
            id, GetCurrentUserId(), GetUserRoles());

        if (item == null)
            return NotFound();

        var model = new FollowUpCreateEditViewModel
        {
            FollowUpId = item.FollowUpId,
            CustomerId = item.CustomerId,
            LeadId = item.LeadId,
            FollowUpDate = item.FollowUpDate,
            FollowUpType = item.FollowUpType,
            Remarks = item.Remarks,
            Status = item.Status,
            AssignedTo = item.AssignedTo
        };

        await PopulateDropDowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(FollowUpCreateEditViewModel model)
    {
        if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDowns(model);
            return View(model);
        }

        var (success, error) = await _followUpService.UpdateFollowUpAsync(
            model, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to update follow-up.");
            await PopulateDropDowns(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Follow-up updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var (success, error) = await _followUpService.CompleteFollowUpAsync(
            id, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to complete follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = "Follow-up marked as Completed!";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _followUpService.DeleteFollowUpAsync(
            id, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = "Follow-up deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
