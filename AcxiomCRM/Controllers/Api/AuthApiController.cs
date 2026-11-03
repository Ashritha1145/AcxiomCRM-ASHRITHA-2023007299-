using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public AuthApiController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
    }

    public class ApiLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] ApiLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var user = await _userManager.FindByEmailAsync(request.Email);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (user == null)
        {
            await _auditService.LogAsync(null, "Failed Login", "Auth API", null, null, new { request.Email }, ip, "Failed");
            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (!user.IsActive)
        {
            await _auditService.LogAsync(user.Id, "Failed Login", "Auth API", user.Id, null, new { request.Email, Reason = "Inactive" }, ip, "Failed");
            return StatusCode(403, new { message = "Account is inactive." });
        }

        var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(user);
            await _auditService.LogAsync(user.Id, "Login", "Auth API", user.Id, null, new { user.Email }, ip, "Success");

            return Ok(new
            {
                message = "Login successful.",
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    roles = roles
                }
            });
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync(user.Id, "Lockout", "Auth API", user.Id, null, new { user.Email }, ip, "Lockout");
            return StatusCode(423, new { message = "Account is locked due to repeated failed login attempts. Try again later." });
        }

        await _auditService.LogAsync(user.Id, "Failed Login", "Auth API", user.Id, null, new { request.Email }, ip, "Failed");
        return Unauthorized(new { message = "Invalid email or password." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        await _signInManager.SignOutAsync();
        await _auditService.LogAsync(userId, "Logout", "Auth API", userId, null, null, HttpContext.Connection.RemoteIpAddress?.ToString(), "Success");
        return Ok(new { message = "Logged out successfully." });
    }
}
