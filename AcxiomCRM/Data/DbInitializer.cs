using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        string[] roles = { "Admin", "Manager", "SalesExecutive" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Default Users
        var usersToSeed = new (string Email, string FullName, string Role)[]
        {
            ("admin@acxiom.com", "System Admin", "Admin"),
            ("manager@acxiom.com", "Sales Manager", "Manager"),
            ("sales@acxiom.com", "Sales Executive", "SalesExecutive")
        };

        foreach (var u in usersToSeed)
        {
            var user = await userManager.FindByEmailAsync(u.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = u.Email,
                    Email = u.Email,
                    EmailConfirmed = true,
                    FullName = u.FullName,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, "Test@1234");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, u.Role);
                }
            }
        }

        // Retrieve seeded user IDs
        var adminUser = await userManager.FindByEmailAsync("admin@acxiom.com");
        var managerUser = await userManager.FindByEmailAsync("manager@acxiom.com");
        var salesUser = await userManager.FindByEmailAsync("sales@acxiom.com");

        if (adminUser == null || managerUser == null || salesUser == null)
            return;

        // 3. Seed Customers if empty
        if (!context.Customers.Any())
        {
            var c1 = new Customer
            {
                CustomerCode = "CUST-1001",
                CustomerName = "Tata Consultancy Services",
                Email = "procurement@tcs.com",
                Phone = "9876543210",
                CompanyName = "TCS Ltd.",
                Address = "TCS House, Raveline Street",
                City = "Mumbai",
                State = "Maharashtra",
                Status = "Active",
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                CreatedBy = salesUser.Id
            };

            var c2 = new Customer
            {
                CustomerCode = "CUST-1002",
                CustomerName = "Infosys Enterprise",
                Email = "contact@infosys.com",
                Phone = "9823456789",
                CompanyName = "Infosys Ltd.",
                Address = "Electronics City, Hosur Road",
                City = "Bengaluru",
                State = "Karnataka",
                Status = "Active",
                CreatedDate = DateTime.UtcNow.AddDays(-20),
                CreatedBy = managerUser.Id
            };

            var c3 = new Customer
            {
                CustomerCode = "CUST-1003",
                CustomerName = "Wipro Solutions",
                Email = "sales@wipro.com",
                Phone = "9988776655",
                CompanyName = "Wipro Ltd.",
                Address = "Doddakannelli, Sarjapur Road",
                City = "Bengaluru",
                State = "Karnataka",
                Status = "Active",
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                CreatedBy = salesUser.Id
            };

            context.Customers.AddRange(c1, c2, c3);
            await context.SaveChangesAsync();

            // 4. Seed Leads
            var l1 = new Lead
            {
                LeadCode = "LEAD-1001",
                LeadName = "Reliance Retail Group",
                Email = "partners@reliance.com",
                Phone = "9711223344",
                CompanyName = "Reliance Industries",
                Source = "Website",
                Status = "Qualified",
                Priority = "High",
                ExpectedValue = 500000m,
                CreatedDate = DateTime.UtcNow.AddDays(-15),
                AssignedTo = salesUser.Id
            };

            var l2 = new Lead
            {
                LeadCode = "LEAD-1002",
                LeadName = "Adani Logistics",
                Email = "ops@adani.com",
                Phone = "9655443322",
                CompanyName = "Adani Ports",
                Source = "Referral",
                Status = "Contacted",
                Priority = "Medium",
                ExpectedValue = 750000m,
                CreatedDate = DateTime.UtcNow.AddDays(-8),
                AssignedTo = salesUser.Id
            };

            var l3 = new Lead
            {
                LeadCode = "LEAD-1003",
                LeadName = "HCL Technologies",
                Email = "growth@hcl.com",
                Phone = "9123456780",
                CompanyName = "HCL Tech",
                Source = "Cold Call",
                Status = "New",
                Priority = "Low",
                ExpectedValue = 300000m,
                CreatedDate = DateTime.UtcNow.AddDays(-2),
                AssignedTo = managerUser.Id
            };

            var l4 = new Lead
            {
                LeadCode = "LEAD-1004",
                LeadName = "Mahindra Digital",
                Email = "it@mahindra.com",
                Phone = "9899001122",
                CompanyName = "Mahindra Group",
                Source = "Social Media",
                Status = "Converted",
                Priority = "High",
                ExpectedValue = 1200000m,
                CreatedDate = DateTime.UtcNow.AddDays(-40),
                AssignedTo = salesUser.Id,
                ConvertedCustomerId = c1.CustomerId
            };

            context.Leads.AddRange(l1, l2, l3, l4);
            await context.SaveChangesAsync();

            // 5. Seed Opportunities
            var o1 = new Opportunity
            {
                OpportunityName = "TCS Cloud Migration Deal",
                CustomerId = c1.CustomerId,
                LeadId = l4.LeadId,
                Amount = 850000m,
                Stage = "Negotiation",
                Probability = 80,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(14),
                Status = "Active",
                Source = "Direct Expansion",
                Notes = "Final pricing negotiation with VP of Infrastructure.",
                CreatedDate = DateTime.UtcNow.AddDays(-12),
                AssignedTo = salesUser.Id
            };

            var o2 = new Opportunity
            {
                OpportunityName = "Infosys Security Suite",
                CustomerId = c2.CustomerId,
                Amount = 450000m,
                Stage = "Proposal",
                Probability = 50,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(25),
                Status = "Active",
                Source = "Web Inbound",
                Notes = "RFP submitted, awaiting feedback from committee.",
                CreatedDate = DateTime.UtcNow.AddDays(-5),
                AssignedTo = managerUser.Id
            };

            var o3 = new Opportunity
            {
                OpportunityName = "Wipro Automation Framework",
                CustomerId = c3.CustomerId,
                Amount = 1200000m,
                Stage = "Won",
                Probability = 100,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(-2),
                Status = "Won",
                Source = "Referral",
                Notes = "Contract signed and PO received.",
                CreatedDate = DateTime.UtcNow.AddDays(-25),
                AssignedTo = salesUser.Id
            };

            var o4 = new Opportunity
            {
                OpportunityName = "TCS AI Assistant POC",
                CustomerId = c1.CustomerId,
                Amount = 250000m,
                Stage = "Qualification",
                Probability = 30,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                Status = "Active",
                Source = "Event",
                Notes = "Initial requirements gathering scheduled.",
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                AssignedTo = salesUser.Id
            };

            context.Opportunities.AddRange(o1, o2, o3, o4);
            await context.SaveChangesAsync();

            // 6. Seed Follow-ups
            var f1 = new FollowUp
            {
                CustomerId = c1.CustomerId,
                FollowUpDate = DateTime.UtcNow.AddDays(1),
                FollowUpType = "Meeting",
                Remarks = "Review contract terms with the legal department.",
                Status = "Planned",
                AssignedTo = salesUser.Id
            };

            var f2 = new FollowUp
            {
                LeadId = l1.LeadId,
                FollowUpDate = DateTime.UtcNow.AddDays(2),
                FollowUpType = "Call",
                Remarks = "Follow up on product demonstration feedback.",
                Status = "Planned",
                AssignedTo = salesUser.Id
            };

            var f3 = new FollowUp
            {
                CustomerId = c2.CustomerId,
                FollowUpDate = DateTime.UtcNow.AddDays(-3),
                FollowUpType = "Email",
                Remarks = "Sent executive summary and pricing matrix.",
                Status = "Completed",
                AssignedTo = managerUser.Id
            };

            context.FollowUps.AddRange(f1, f2, f3);
            await context.SaveChangesAsync();

            // 7. Seed Activities
            var a1 = new Activity
            {
                ActivityType = "Call",
                Subject = "Quarterly Business Review Prep",
                Description = "Discussed Q4 expansion and resource requirements with technical lead.",
                ActivityDate = DateTime.UtcNow.AddDays(-4),
                CustomerId = c1.CustomerId,
                AssignedTo = salesUser.Id,
                Status = "Completed"
            };

            var a2 = new Activity
            {
                ActivityType = "Meeting",
                Subject = "Architecture Alignment Call",
                Description = "Technical alignment workshop regarding deployment security and integrations.",
                ActivityDate = DateTime.UtcNow.AddDays(-2),
                CustomerId = c2.CustomerId,
                AssignedTo = managerUser.Id,
                Status = "Completed"
            };

            var a3 = new Activity
            {
                ActivityType = "Task",
                Subject = "Prepare Quotation and Proposal Deck",
                Description = "Customized proposal deck for Reliance Retail pilot phase.",
                ActivityDate = DateTime.UtcNow.AddDays(2),
                LeadId = l1.LeadId,
                AssignedTo = salesUser.Id,
                Status = "Pending"
            };

            context.Activities.AddRange(a1, a2, a3);
            await context.SaveChangesAsync();

            // 8. Seed Initial Audit Logs
            var audit1 = new AuditLog
            {
                UserId = adminUser.Id,
                Action = "Login",
                EntityName = "Auth",
                RecordId = adminUser.Id,
                NewValue = "{\"Email\":\"admin@acxiom.com\"}",
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                IpAddress = "127.0.0.1",
                Result = "Success"
            };

            var audit2 = new AuditLog
            {
                UserId = salesUser.Id,
                Action = "Create",
                EntityName = "Customer",
                RecordId = c1.CustomerId.ToString(),
                NewValue = "{\"CustomerCode\":\"CUST-1001\",\"CustomerName\":\"Tata Consultancy Services\"}",
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                IpAddress = "127.0.0.1",
                Result = "Success"
            };

            context.AuditLogs.AddRange(audit1, audit2);
            await context.SaveChangesAsync();
        }
    }
}
