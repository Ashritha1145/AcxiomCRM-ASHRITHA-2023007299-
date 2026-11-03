using System.Security.Claims;
using AcxiomCRM.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipelineReport()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
        bool isAdminOrMgr = roles.Contains("Admin") || roles.Contains("Manager");

        var oppQuery = _context.Opportunities.Include(o => o.Customer).AsNoTracking().AsQueryable();
        if (!isAdminOrMgr)
        {
            oppQuery = oppQuery.Where(o => o.AssignedTo == currentUserId);
        }

        var opportunities = await oppQuery.ToListAsync();
        var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };

        var stageSummaries = stages.Select(s =>
        {
            var stageOpps = opportunities.Where(o => string.Equals(o.Stage, s, StringComparison.OrdinalIgnoreCase)).ToList();
            return new
            {
                stage = s,
                count = stageOpps.Count,
                totalAmount = stageOpps.Sum(o => o.Amount),
                weightedAmount = stageOpps.Sum(o => o.WeightedValue)
            };
        });

        var openOpps = opportunities.Where(o => o.Status == "Active").ToList();

        return Ok(new
        {
            totalPipelineValue = openOpps.Sum(o => o.Amount),
            totalWeightedPipelineValue = openOpps.Sum(o => o.WeightedValue),
            stageBreakdown = stageSummaries
        });
    }
}
