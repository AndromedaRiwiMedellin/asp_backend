using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_backend.Models; 
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

        // 1. REGISTRO MANUAL (Adaptado a GUID y tipos de tu DB)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { Message = "Los datos de registro son inválidos." });
            }

            // Verificar si el correo ya existe antes de registrar
            var existingUser = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (existingUser)
            {
                return BadRequest(new { Message = "El correo ya se encuentra registrado." });
            }

            var user = new User()
            {
                Id = Guid.NewGuid(), // <-- CLAVE: Generamos un nuevo GUID único
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

        // 2. LOGIN MANUAL (Adaptado a tu modelo)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { Message = "El correo y la contraseña son requeridos." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { Message = "Credenciales inválidas." });
            }

            return Ok(new { 
                Message = "¡Inicio de sesión exitoso!", 
                User = new { Id = user.Id, FullName = user.FullName, Email = user.Email } 
            });
        }

        // 3. FLUJO DE AUTENTICACIÓN CON GOOGLE 🚀
        [HttpPost("google-auth")]
        public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.GoogleId))
            {
                return BadRequest(new { Message = "Datos de Google inválidos o incompletos." });
            }

            // Buscar si el usuario ya existe por su Email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user != null)
            {
                // [Email existente vincula el google_id al usuario si no lo tenía]
                if (string.IsNullOrEmpty(user.GoogleId))
                {
                    user.GoogleId = request.GoogleId;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { 
                    Message = "Cuenta de Google vinculada exitosamente.", 
                    User = new { Id = user.Id, FullName = user.FullName, Email = user.Email } 
                });
            }
            else
            {
                // [Email nuevo crea cuenta automáticamente]
                var newUser = new User()
                {
                    Id = Guid.NewGuid(), // <-- Generamos el GUID obligatorio para el nuevo usuario
                    FullName = request.FullName,
                    Email = request.Email,
                    GoogleId = request.GoogleId,
                    PasswordHash = null,  // [No requiere password_hash si solo usa Google]
                    CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                    Phone = "",
                    ProfileImage = request.ProfileImage ?? ""
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    Message = "Usuario registrado e iniciado con Google con éxito.", 
                    User = new { Id = newUser.Id, FullName = newUser.FullName, Email = newUser.Email } 
                });
            }
        }
    }

    // --- DTOs ---
    public class RegisterRequest
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class GoogleAuthRequest
    {
        public string GoogleId { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? ProfileImage { get; set; }
    }
}