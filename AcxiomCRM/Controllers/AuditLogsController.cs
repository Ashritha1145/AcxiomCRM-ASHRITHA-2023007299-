using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin")]
public class AuditLogsController : Controller
{
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuditLogsController(IAuditService auditService, UserManager<ApplicationUser> userManager)
    {
        _auditService = auditService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? userId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        int pageSize = 15;
        var (logs, totalCount) = await _auditService.GetAuditLogsAsync(
            userId, entityName, action, fromDate, toDate, page, pageSize);

        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;

        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        var usersList = users.Select(u => new SelectListItem
        {
            Value = u.Id,
            Text = $"{u.FullName} ({u.Email})"
        }).ToList();

        var model = new AuditLogFilterViewModel
        {
            UserId = userId,
            EntityName = entityName,
            Action = action,
            FromDate = fromDate,
            ToDate = toDate,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalCount = totalCount,
            Logs = logs,
            UsersList = usersList
        };

        return View(model);
    }
}
