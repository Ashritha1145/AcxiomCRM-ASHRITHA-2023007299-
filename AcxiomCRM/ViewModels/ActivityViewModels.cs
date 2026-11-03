using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels;

public class ActivityIndexViewModel
{
    public string? SearchTerm { get; set; }
    public string? TypeFilter { get; set; }
    public string? StatusFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public List<Activity> Activities { get; set; } = new();
}

public class ActivityCreateEditViewModel
{
    public int ActivityId { get; set; }

    [Required]
    [Display(Name = "Activity Type")]
    public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    [Display(Name = "Subject")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Activity Date")]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Related Customer (Optional)")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead (Optional)")]
    public int? LeadId { get; set; }

    [Required]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed, Cancelled

    [Display(Name = "Assigned To")]
    public string AssignedTo { get; set; } = string.Empty;

    public List<SelectListItem> CustomersList { get; set; } = new();
    public List<SelectListItem> LeadsList { get; set; } = new();
    public List<SelectListItem> UsersList { get; set; } = new();
}
