using System.Security.Claims;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/followups")]
[Authorize]
public class FollowUpsApiController : ControllerBase
{
    private readonly IFollowUpService _followUpService;

    public FollowUpsApiController(IFollowUpService followUpService)
    {
        _followUpService = followUpService;
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private IList<string> GetUserRoles() =>
        User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

    private string? GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<ActionResult<List<FollowUpDto>>> GetAll()
    {
        var dtos = await _followUpService.GetAllFollowUpDtosAsync(
            GetCurrentUserId(), GetUserRoles());
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<FollowUpDto>> Create([FromBody] CreateFollowUpDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, error, followUp) = await _followUpService.CreateFromDtoAsync(
            dto, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetAll), new { id = followUp!.FollowUpId }, followUp);
    }
}
