using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AcademiaCoursePortal.API.Data;
using AcademiaCoursePortal.API.Models;

namespace AcademiaCoursePortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuthenticationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IConfiguration _configuration;

        public AuthenticationController(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthenticationController> logger)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        // POST: api/authentication/login
        [HttpPost("login")]
        [AllowAnonymous]
        public ActionResult<string> Login([FromBody] LoginModel login)
        {
            if (login.Username == null || login.Password == null)
            {
                return BadRequest("Username and password are required.");
            }

            try
            {
                var student = _context.Students
                    .FirstOrDefault(s => s.Username == login.Username);

                if (student == null || !BCrypt.Net.BCrypt.Verify(login.Password, student.Password))
                {
                    return Unauthorized("Invalid credentials.");
                }

                // Retrieve JWT key from configuration
                var jwtKey = _configuration["Jwt:Key"];
                if (string.IsNullOrEmpty(jwtKey))
                {
                    _logger.LogError("JWT Key is missing or empty.");
                    return StatusCode(500, "Internal server error: Missing JWT key.");
                }

                // Generate JWT token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(jwtKey);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                new Claim(ClaimTypes.Name, student.Username),
                new Claim(ClaimTypes.NameIdentifier, student.Id.ToString()) 
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return Ok(new { Token = tokenString });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login.");
                return StatusCode(500, "Internal server error occurred.");
            }
        }



        // POST: api/authentication/register
        [HttpPost("register")]
        [AllowAnonymous]
        public ActionResult<string> Register([FromBody] RegisterModel register)
        {
            if (register.Username == null || register.Password == null)
            {
                return BadRequest("Username and password are required.");
            }

            try
            {
                if (_context.Students.Any(s => s.Username == register.Username))
                {
                    return Conflict("Username already exists.");
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(register.Password);
                var student = new Student
                {
                    Name = register.Name,
                    Username = register.Username,
                    Email = register.Email,
                    Password = hashedPassword
                };

                _context.Students.Add(student);
                _context.SaveChanges();

                return Ok("Student registered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during registration.");
                return StatusCode(500, "Internal server error occurred.");
            }
        }
    }

    public class LoginModel
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class RegisterModel
    {
        public string?  Name { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
    }
}
