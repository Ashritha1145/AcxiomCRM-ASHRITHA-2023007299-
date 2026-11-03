using System.Security.Claims;
using AcxiomCRM.DTOs;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Authorize]
public class OpportunitiesApiController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;

    public OpportunitiesApiController(IOpportunityService opportunityService)
    {
        _opportunityService = opportunityService;
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private IList<string> GetUserRoles() =>
        User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

    private string? GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<ActionResult<List<OpportunityDto>>> GetAll([FromQuery] string? search)
    {
        var dtos = await _opportunityService.GetAllOpportunityDtosAsync(
            GetCurrentUserId(), GetUserRoles(), search);
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<OpportunityDto>> Create([FromBody] CreateOpportunityDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, error, opp) = await _opportunityService.CreateFromDtoAsync(
            dto, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetAll), new { id = opp!.OpportunityId }, opp);
    }
}
