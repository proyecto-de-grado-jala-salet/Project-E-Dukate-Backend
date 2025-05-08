using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Auth;
using E_Dukate.Application.DTOs.Auth;

namespace E_Dukate.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var result = await _authService.LoginAsync(loginDto);

        if (!result.IsSuccess)
            return BadRequest(new { Error = result.ErrorMessage });

        return Ok(new { Token = result.Value });
    }
}