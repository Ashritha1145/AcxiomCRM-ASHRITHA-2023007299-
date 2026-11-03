using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels;

public class LeadIndexViewModel
{
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public string? AssignedToFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public List<Lead> Leads { get; set; } = new();
    public List<SelectListItem> UsersList { get; set; } = new();
}

public class LeadCreateEditViewModel
{
    public int LeadId { get; set; }

    [Required(ErrorMessage = "Lead Code is required.")]
    [StringLength(50, ErrorMessage = "Lead Code cannot exceed 50 characters.")]
    [Display(Name = "Lead Code")]
    public string LeadCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number (e.g. 9876543210).")]
    [Display(Name = "Mobile Phone")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [Required]
    [Display(Name = "Lead Source")]
    public string Source { get; set; } = "Website";

    [Required]
    [Display(Name = "Status")]
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

    [Required]
    [Display(Name = "Priority")]
    public string Priority { get; set; } = "Medium"; // Low, Medium, High

    [Range(0, 100000000, ErrorMessage = "Expected Value must be between 0 and 100,000,000.")]
    [Display(Name = "Expected Value (INR)")]
    public decimal ExpectedValue { get; set; } = 0;

    [Display(Name = "Assigned To")]
    public string AssignedTo { get; set; } = string.Empty;

    public List<SelectListItem> UsersList { get; set; } = new();
}

public class LeadDetailsViewModel
{
    public Lead Lead { get; set; } = default!;
    public string AssignedToName { get; set; } = string.Empty;
    public List<Opportunity> Opportunities { get; set; } = new();
    public List<FollowUp> FollowUps { get; set; } = new();
    public List<Activity> Activities { get; set; } = new();
}

public class ConvertLeadViewModel
{
    public int LeadId { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer Code is required.")]
    [Display(Name = "Customer Code")]
    public string CustomerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer Name is required.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    [Display(Name = "Create Opportunity from this Lead?")]
    public bool CreateOpportunity { get; set; } = true;

    [Display(Name = "Opportunity Name")]
    public string? OpportunityName { get; set; }

    [Display(Name = "Opportunity Amount (INR)")]
    [Range(1, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal OpportunityAmount { get; set; }

    [Display(Name = "Expected Close Date")]
    public DateTime? ExpectedCloseDate { get; set; }
}
