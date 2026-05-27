using asp_backend.Data;
using asp_backend.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_backend.models; // Aquí busca tu modelo User.cs
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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // 1. Validación para que no llegue nada vacío
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { Message = "Los datos de registro son inválidos." });
            }

            // 2. Mapeo de datos usando PascalCase como lo tienes en tu User.cs
            var user = new User()
            {
                FullName = request.FullName, // ¡Ahora sí existe en el Request!
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                
                // Valores por defecto para que Postgres no chille por campos requeridos
                Phone = "",
                GoogleId = "",
                ProfileImage = ""
            };

            // 3. Guardar en la base de datos
            // Nota: Si 'Users' te sale en rojo, cámbialo por 'users' en minúscula
            _context.Users.Add(user); 
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Account created successfully." });
        }
    }

    // CLASE CLAVE: Colocándola aquí nos aseguramos de que el controlador funcione al 100%
    public class RegisterRequest
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}