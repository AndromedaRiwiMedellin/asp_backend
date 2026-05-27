using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_backend.models; 
using asp_backend.Data;   
using BCrypt.Net;

namespace asp_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // 1. MÉTODO DE REGISTRO (Ya funciona 10/10)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { Message = "Los datos de registro son inválidos." });
            }

            var user = new User()
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                Phone = "",
                GoogleId = "",
                ProfileImage = ""
            };

            _context.Users.Add(user); 
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Account created successfully." });
        }

        // 2. NUEVO MÉTODO: INICIO DE SESIÓN (Para solucionar el Error 404)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { Message = "El correo y la contraseña son requeridos." });
            }

            // Buscamos el usuario en la base de datos por su email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            // Si no existe o la contraseña encriptada no coincide, rechazamos el acceso
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { Message = "Credenciales inválidas. Verifica tu correo o contraseña." });
            }

            // Login exitoso: Devolvemos los datos del usuario (puedes expandir esto luego con un Token JWT)
            return Ok(new { 
                Message = "¡Inicio de sesión exitoso!", 
                User = new {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email
                }
            });
        }
    }

    // Estructura para capturar los datos de registro
    public class RegisterRequest
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    // NUEVA ESTRUCTURA: Para capturar los datos de inicio de sesión
    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}