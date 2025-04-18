using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Users;
using E_Dukate.Application.DTOs.Common;
using E_Dukate.Application.DTOs.Users;

namespace E_Dukate.Presentation.Controllers.Users;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var (items, totalCount) = await _userService.GetAllUsersAsync(pagination);
        return Ok(new
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
        });
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id, [FromQuery] string role)
    {
        try
        {
            _userService.DeleteUser(id, role);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}