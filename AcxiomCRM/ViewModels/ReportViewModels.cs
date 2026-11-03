using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class ReportsDashboardViewModel
{
    public string ActiveTab { get; set; } = "Pipeline";

    // Pipeline Report data
    public List<StagePipelineSummary> StagePipelineSummaries { get; set; } = new();
    public decimal TotalPipelineValue { get; set; }
    public decimal TotalWeightedPipelineValue { get; set; }

    // Conversion Report data
    public int TotalLeadsCount { get; set; }
    public int ConvertedLeadsCount { get; set; }
    public double ConversionRate => TotalLeadsCount > 0 ? (ConvertedLeadsCount * 100.0 / TotalLeadsCount) : 0;
    public int WonDealsCount { get; set; }
    public int LostDealsCount { get; set; }
    public decimal WonAmount { get; set; }

    // Reports lists
    public List<Customer> Customers { get; set; } = new();
    public List<Lead> Leads { get; set; } = new();
    public List<Opportunity> Opportunities { get; set; } = new();
    public List<FollowUp> FollowUps { get; set; } = new();
}

public class StagePipelineSummary
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}
