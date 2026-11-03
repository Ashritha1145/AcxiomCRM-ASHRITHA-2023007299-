using System.Globalization;
using AcxiomCRM.Data;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    private static bool IsAdminOrManager(IList<string> roles) =>
        roles.Contains("Admin") || roles.Contains("Manager");

    public async Task<DashboardViewModel> GetDashboardDataAsync(string currentUserId, IList<string> roles, string dateFilter)
    {
        bool isAdminOrManager = IsAdminOrManager(roles);

        var custQuery = _context.Customers.AsNoTracking().AsQueryable();
        var leadQuery = _context.Leads.AsNoTracking().AsQueryable();
        var oppQuery = _context.Opportunities.Include(o => o.Customer).AsNoTracking().AsQueryable();
        var followQuery = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsNoTracking().AsQueryable();

        if (!isAdminOrManager)
        {
            custQuery = custQuery.Where(c => c.CreatedBy == currentUserId);
            leadQuery = leadQuery.Where(l => l.AssignedTo == currentUserId);
            oppQuery = oppQuery.Where(o => o.AssignedTo == currentUserId);
            followQuery = followQuery.Where(f => f.AssignedTo == currentUserId);
        }

        DateTime? filterStart = null;
        var now = DateTime.UtcNow;
        if (dateFilter == "Today")
        {
            filterStart = now.Date;
        }
        else if (dateFilter == "Week")
        {
            filterStart = now.Date.AddDays(-(int)now.DayOfWeek);
        }
        else if (dateFilter == "Month")
        {
            filterStart = new DateTime(now.Year, now.Month, 1);
        }

        if (filterStart.HasValue)
        {
            custQuery = custQuery.Where(c => c.CreatedDate >= filterStart.Value);
            leadQuery = leadQuery.Where(l => l.CreatedDate >= filterStart.Value);
            oppQuery = oppQuery.Where(o => o.CreatedDate >= filterStart.Value);
        }

        var customers = await custQuery.ToListAsync();
        var leads = await leadQuery.ToListAsync();
        var opportunities = await oppQuery.ToListAsync();
        var followUps = await followQuery.ToListAsync();

        var openLeads = leads.Count(l => l.Status is "New" or "Contacted" or "Qualified");
        var openOpps = opportunities.Where(o => o.Status == "Active").ToList();
        var wonOpps = opportunities.Where(o => o.Stage == "Won" || o.Status == "Won").ToList();
        var lostOpps = opportunities.Where(o => o.Stage == "Lost" || o.Status == "Lost").ToList();

        decimal totalPipeline = openOpps.Sum(o => o.Amount);
        decimal weightedPipeline = openOpps.Sum(o => o.WeightedValue);

        var pendingFollows = followUps.Count(f => f.Status == "Planned");

        // Chart 1: Lead Status Doughnut
        var leadStatusLabels = new List<string> { "New", "Contacted", "Qualified", "Converted", "Lost" };
        var leadStatusCounts = leadStatusLabels.Select(s => leads.Count(l => string.Equals(l.Status, s, StringComparison.OrdinalIgnoreCase))).ToList();

        // Chart 2: Opportunity Pipeline by Stage
        var pipelineStages = new List<string> { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
        var pipelineStageValues = pipelineStages.Select(s => opportunities.Where(o => string.Equals(o.Stage, s, StringComparison.OrdinalIgnoreCase)).Sum(o => o.Amount)).ToList();

        // Chart 3: Monthly Sales / Won Totals (last 6 months)
        var monthlyLabels = new List<string>();
        var monthlyValues = new List<decimal>();

        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = now.AddMonths(-i);
            var monthName = targetMonth.ToString("MMM yyyy", CultureInfo.InvariantCulture);
            monthlyLabels.Add(monthName);

            var monthTotal = wonOpps
                .Where(o => o.CreatedDate.Year == targetMonth.Year && o.CreatedDate.Month == targetMonth.Month)
                .Sum(o => o.Amount);

            monthlyValues.Add(monthTotal);
        }

        // Recent items
        var upcomingFollowsList = followUps
            .Where(f => f.Status == "Planned" && f.FollowUpDate >= now.Date)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToList();

        var recentOppsList = opportunities
            .OrderByDescending(o => o.CreatedDate)
            .Take(5)
            .ToList();

        var recentLeadsList = leads
            .OrderByDescending(l => l.CreatedDate)
            .Take(5)
            .ToList();

        return new DashboardViewModel
        {
            DateFilter = dateFilter,
            TotalCustomers = customers.Count,
            TotalLeads = leads.Count,
            OpenLeads = openLeads,
            TotalOpportunities = opportunities.Count,
            OpenOpportunities = openOpps.Count,
            WonOpportunities = wonOpps.Count,
            LostOpportunities = lostOpps.Count,
            TotalPipelineValue = totalPipeline,
            WeightedPipelineValue = weightedPipeline,
            PendingFollowUps = pendingFollows,
            LeadStatusLabels = leadStatusLabels,
            LeadStatusCounts = leadStatusCounts,
            PipelineStageLabels = pipelineStages,
            PipelineStageValues = pipelineStageValues,
            MonthlySalesLabels = monthlyLabels,
            MonthlySalesValues = monthlyValues,
            UpcomingFollowUps = upcomingFollowsList,
            RecentOpportunities = recentOppsList,
            RecentLeads = recentLeadsList
        };
    }
}
