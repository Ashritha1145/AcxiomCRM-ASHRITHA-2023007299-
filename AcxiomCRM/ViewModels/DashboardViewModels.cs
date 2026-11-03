namespace AcxiomCRM.ViewModels;

public class DashboardViewModel
{
    public string DateFilter { get; set; } = "All"; // Today, Week, Month, All

    // KPI Cards
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public int PendingFollowUps { get; set; }

    // Chart Data Models
    public List<string> LeadStatusLabels { get; set; } = new();
    public List<int> LeadStatusCounts { get; set; } = new();

    public List<string> PipelineStageLabels { get; set; } = new();
    public List<decimal> PipelineStageValues { get; set; } = new();

    public List<string> MonthlySalesLabels { get; set; } = new();
    public List<decimal> MonthlySalesValues { get; set; } = new();

    // Recent items for quick activity overview
    public List<Models.FollowUp> UpcomingFollowUps { get; set; } = new();
    public List<Models.Opportunity> RecentOpportunities { get; set; } = new();
    public List<Models.Lead> RecentLeads { get; set; } = new();
}
