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
public class OpportunitiesController : Controller
{
    private readonly IOpportunityService _opportunityService;
    private readonly ApplicationDbContext _context;

    public OpportunitiesController(
        IOpportunityService opportunityService,
        ApplicationDbContext context)
    {
        _opportunityService = opportunityService;
        _context = context;
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private IList<string> GetUserRoles() =>
        User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

    private string? GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private async Task PopulateDropDowns(OpportunityCreateEditViewModel model)
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
    public async Task<IActionResult> Index(string? searchTerm, string? stageFilter, string? statusFilter, int? customerFilter, int page = 1)
    {
        var model = await _opportunityService.GetOpportunitiesAsync(
            GetCurrentUserId(), GetUserRoles(), searchTerm, stageFilter, statusFilter, customerFilter, page, 10);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _opportunityService.GetOpportunityDetailsAsync(
            id, GetCurrentUserId(), GetUserRoles());

        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var model = new OpportunityCreateEditViewModel
        {
            CustomerId = customerId ?? 0,
            LeadId = leadId,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
            Amount = 100000m,
            Probability = 25
        };

        await PopulateDropDowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityCreateEditViewModel model)
    {
        // Enforce business rules
        if (model.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
        }

        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
        }

        if (model.Status == "Active" && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDowns(model);
            return View(model);
        }

        var (success, error, opp) = await _opportunityService.CreateOpportunityAsync(
            model, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to create opportunity.");
            await PopulateDropDowns(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Opportunity '{opp!.OpportunityName}' was created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var opp = await _opportunityService.GetOpportunityByIdAsync(
            id, GetCurrentUserId(), GetUserRoles());

        if (opp == null)
            return NotFound();

        var model = new OpportunityCreateEditViewModel
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            LeadId = opp.LeadId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            Source = opp.Source,
            Notes = opp.Notes,
            AssignedTo = opp.AssignedTo
        };

        await PopulateDropDowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(OpportunityCreateEditViewModel model)
    {
        if (model.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
        }

        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
        }

        if (model.Status == "Active" && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDowns(model);
            return View(model);
        }

        var (success, error) = await _opportunityService.UpdateOpportunityAsync(
            model, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to update opportunity.");
            await PopulateDropDowns(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Opportunity '{model.OpportunityName}' was updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStage(int id, string stage)
    {
        var (success, error) = await _opportunityService.UpdateStageAsync(
            id, stage, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to update stage.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Stage updated to {stage}.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _opportunityService.DeleteOpportunityAsync(
            id, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete opportunity.";
        }
        else
        {
            TempData["SuccessMessage"] = "Opportunity has been deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
