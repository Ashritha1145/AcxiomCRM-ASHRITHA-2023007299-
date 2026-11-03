using System.Security.Claims;
using AcxiomCRM.DTOs;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Authorize]
public class LeadsApiController : ControllerBase
{
    private readonly ILeadService _leadService;

    public LeadsApiController(ILeadService leadService)
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
    public async Task<ActionResult<List<LeadDto>>> GetAll([FromQuery] string? search)
    {
        var dtos = await _leadService.GetAllLeadDtosAsync(
            GetCurrentUserId(), GetUserRoles(), search);
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<LeadDto>> Create([FromBody] CreateLeadDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, error, lead) = await _leadService.CreateFromDtoAsync(
            dto, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetAll), new { id = lead!.LeadId }, lead);
    }
}
