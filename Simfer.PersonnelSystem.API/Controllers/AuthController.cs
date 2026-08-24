using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Simfer.PersonnelSystem.API.Data;

namespace Simfer.PersonnelSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // 1. DEĞİŞİKLİK: Veritabanında artık sadece kullanıcı adını arıyoruz. 
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            // 2. DEĞİŞİKLİK: Kullanıcı yoksa VEYA girilen düz şifre, veritabanındaki hash'lenmiş şifreyle uyuşmuyorsa yetkisiz giriş!
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Kullanıcı adı veya şifre hatalı.");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("Username", user.Username),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
            };

            if (user.Role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Name));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(10),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
            {
                UserId = user.Id,
                ActionType = "Sisteme Giriş",
                Details = $"{user.Username} adlı kullanıcı sisteme başarıyla giriş yaptı."
            };
            _context.UserHistories.Add(historyRecord);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Token = tokenString,
                Role = user.Role != null ? user.Role.Name : "Atanmadı",
                FullName = $"{user.FirstName} {user.LastName}"
            });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}