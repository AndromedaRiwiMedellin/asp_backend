using Microsoft.AspNetCore.Mvc;

namespace asp_backend.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        return Ok(new { message = "login ok", email = request.Email });
    }

    [HttpPost("refresh")]
    public IActionResult Refresh()
    {
        return Ok(new { message = "refresh ok" });
    }
}

public record LoginRequest(string Email, string Password);