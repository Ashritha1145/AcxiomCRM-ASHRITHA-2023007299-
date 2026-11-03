using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels;

public class FollowUpIndexViewModel
{
    public string? StatusFilter { get; set; }
    public string? TypeFilter { get; set; }
    public string? DateFilter { get; set; } // Today, Upcoming, Overdue, All
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public List<FollowUp> FollowUps { get; set; } = new();

    public int PlannedCount { get; set; }
    public int CompletedCount { get; set; }
    public int OverdueCount { get; set; }
}

public class FollowUpCreateEditViewModel
{
    public int FollowUpId { get; set; }

    [Display(Name = "Related Customer (Optional)")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead (Optional)")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Follow-up date is required.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Follow-Up Date & Time")]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddHours(2);

    [Required]
    [Display(Name = "Type")]
    public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Demo, Other

    [MaxLength(1000)]
    [Display(Name = "Remarks / Notes")]
    public string Remarks { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

    [Display(Name = "Assigned To")]
    public string AssignedTo { get; set; } = string.Empty;

    public List<SelectListItem> CustomersList { get; set; } = new();
    public List<SelectListItem> LeadsList { get; set; } = new();
    public List<SelectListItem> UsersList { get; set; } = new();
}

public class FollowUpDto
{
    public int FollowUpId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public DateTime FollowUpDate { get; set; }
    public string FollowUpType { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
}

public class CreateFollowUpDto
{
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    [Required]
    public DateTime FollowUpDate { get; set; }
    public string FollowUpType { get; set; } = "Call";
    public string Remarks { get; set; } = string.Empty;
    public string Status { get; set; } = "Planned";
    public string? AssignedTo { get; set; }
}
