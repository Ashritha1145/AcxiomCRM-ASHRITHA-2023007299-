using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels;

public class OpportunityIndexViewModel
{
    public string? SearchTerm { get; set; }
    public string? StageFilter { get; set; }
    public string? StatusFilter { get; set; }
    public int? CustomerFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public List<Opportunity> Opportunities { get; set; } = new();
    public List<SelectListItem> CustomersList { get; set; } = new();

    // Pipeline metrics
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public int TotalOpportunities { get; set; }
    public int WonCount { get; set; }
    public int LostCount { get; set; }
}

public class OpportunityCreateEditViewModel
{
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer is required.")]
    [Display(Name = "Related Customer")]
    public int CustomerId { get; set; }

    [Display(Name = "Related Lead (Optional)")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Display(Name = "Opportunity Amount (INR)")]
    public decimal Amount { get; set; }

    [Required]
    [Display(Name = "Pipeline Stage")]
    public string Stage { get; set; } = "Qualification";

    [Required]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    [Display(Name = "Probability (%)")]
    public int Probability { get; set; } = 20;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [Required]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Active"; // Active, Won, Lost

    [Display(Name = "Source")]
    public string? Source { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Assigned To")]
    public string AssignedTo { get; set; } = string.Empty;

    public List<SelectListItem> CustomersList { get; set; } = new();
    public List<SelectListItem> LeadsList { get; set; } = new();
    public List<SelectListItem> UsersList { get; set; } = new();
}

public class OpportunityDetailsViewModel
{
    public Opportunity Opportunity { get; set; } = default!;
    public string AssignedToName { get; set; } = string.Empty;
}
