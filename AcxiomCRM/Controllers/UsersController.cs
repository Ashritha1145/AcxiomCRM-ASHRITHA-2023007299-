using System.Security.Claims;
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
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private string? GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private async Task<List<SelectListItem>> GetRolesSelectList()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        return roles.Select(r => new SelectListItem
        {
            Value = r.Name,
            Text = r.Name
        }).ToList();
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? roleFilter)
    {
        var query = _userManager.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                (u.Email != null && u.Email.ToLower().Contains(term)));
        }

        var users = await query.OrderByDescending(u => u.CreatedDate).ToListAsync();
        var userItems = new List<UserItemViewModel>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var roleName = roles.FirstOrDefault() ?? "None";

            if (!string.IsNullOrWhiteSpace(roleFilter) && !string.Equals(roleName, roleFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            bool isLocked = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow;

            userItems.Add(new UserItemViewModel
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FullName = u.FullName,
                Role = roleName,
                IsActive = u.IsActive,
                IsLockedOut = isLocked,
                AccessFailedCount = u.AccessFailedCount,
                LockoutEnd = u.LockoutEnd?.UtcDateTime,
                CreatedDate = u.CreatedDate
            });
        }

        return View(new UserIndexViewModel
        {
            SearchTerm = searchTerm,
            RoleFilter = roleFilter,
            Users = userItems
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new UserCreateViewModel
        {
            AvailableRoles = await GetRolesSelectList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetRolesSelectList();
            return View(model);
        }

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            ModelState.AddModelError("Email", "A user with this email address already exists.");
            model.AvailableRoles = await GetRolesSelectList();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            IsActive = true,
            EmailConfirmed = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);

            var ip = GetIpAddress();
            await _auditService.LogAsync(
                GetCurrentUserId(),
                "Create User",
                "User",
                user.Id,
                null,
                new { user.Email, user.FullName, Role = model.Role },
                ip,
                "Success");

            TempData["SuccessMessage"] = $"User '{user.Email}' created with role '{model.Role}'.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        model.AvailableRoles = await GetRolesSelectList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        var model = new UserEditViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            IsActive = user.IsActive,
            Role = roles.FirstOrDefault() ?? "SalesExecutive",
            AvailableRoles = await GetRolesSelectList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetRolesSelectList();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        var oldRoles = await _userManager.GetRolesAsync(user);
        var oldRole = oldRoles.FirstOrDefault() ?? "None";
        bool oldActive = user.IsActive;

        user.FullName = model.FullName.Trim();
        user.IsActive = model.IsActive;

        await _userManager.UpdateAsync(user);

        // Role update
        if (oldRole != model.Role)
        {
            if (oldRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, oldRoles);
            }
            await _userManager.AddToRoleAsync(user, model.Role);

            await _auditService.LogAsync(
                GetCurrentUserId(),
                "Role Change",
                "User",
                user.Id,
                new { Role = oldRole },
                new { Role = model.Role },
                GetIpAddress(),
                "Success");
        }

        // Optional password reset
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!resetResult.Succeeded)
            {
                foreach (var err in resetResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                model.AvailableRoles = await GetRolesSelectList();
                return View(model);
            }

            await _auditService.LogAsync(
                GetCurrentUserId(),
                "Password Reset",
                "User",
                user.Id,
                null,
                new { Action = "Password reset by administrator" },
                GetIpAddress(),
                "Success");
        }

        TempData["SuccessMessage"] = $"User '{user.Email}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        await _auditService.LogAsync(
            GetCurrentUserId(),
            "Unlock Account",
            "User",
            user.Id,
            new { Lockout = "Active" },
            new { Lockout = "Unlocked" },
            GetIpAddress(),
            "Success");

        TempData["SuccessMessage"] = $"User '{user.Email}' has been unlocked.";
        return RedirectToAction(nameof(Index));
    }
}
