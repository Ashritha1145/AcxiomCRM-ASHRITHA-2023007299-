using System.Security.Claims;
using AcxiomCRM.DTOs;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersApiController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private IList<string> GetUserRoles() =>
        User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

    private string? GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll([FromQuery] string? search)
    {
        var dtos = await _customerService.GetAllCustomerDtosAsync(
            GetCurrentUserId(), GetUserRoles(), search);
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id)
    {
        var dto = await _customerService.GetCustomerDtoByIdAsync(
            id, GetCurrentUserId(), GetUserRoles());

        if (dto == null)
            return NotFound(new { message = $"Customer with ID {id} not found or access denied." });

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, error, customer) = await _customerService.CreateFromDtoAsync(
            dto, GetCurrentUserId(), GetIpAddress());

        if (!success)
        {
            if (error != null && error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { message = error });

            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(GetById), new { id = customer!.CustomerId }, customer);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var exists = await _customerService.GetCustomerByIdAsync(id, GetCurrentUserId(), GetUserRoles());
        if (exists == null)
            return NotFound(new { message = $"Customer with ID {id} not found or access denied." });

        var (success, error) = await _customerService.UpdateFromDtoAsync(
            id, dto, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
        {
            if (error != null && error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { message = error });

            return BadRequest(new { message = error });
        }

        return Ok(new { message = "Customer updated successfully." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _customerService.DeleteCustomerAsync(
            id, GetCurrentUserId(), GetUserRoles(), GetIpAddress());

        if (!success)
            return NotFound(new { message = error ?? $"Customer with ID {id} not found or access denied." });

        return Ok(new { message = "Customer deactivated successfully." });
    }
}
