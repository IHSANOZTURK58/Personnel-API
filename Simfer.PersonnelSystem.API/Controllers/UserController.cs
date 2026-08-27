using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Simfer.PersonnelSystem.API.Data;
using System.Security.Claims;
using Simfer.PersonnelSystem.API.DTOs;

namespace Simfer.PersonnelSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {

        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("profil")]
        [Authorize]
        public IActionResult GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue("Username");
            var fullName = User.FindFirstValue(ClaimTypes.Name);
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "Standart Yetki";

            return Ok(new
            {
                Success = true,
                Message = "Kullanıcı profil bilgileri başarıyla getirildi.",
                Data = new
                {
                    Id = userId,
                    Username = username,
                    FullName = fullName,
                    Role = role
                }
            });
        }

        [HttpGet("Employee-listesi")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetPersonnelList()
        {
            var personnelList = await _context.Users
                .Include(u => u.Role)
                .Select(u => new
                {
                    Id = u.Id,
                    FullName = $"{u.FirstName} {u.LastName}",
                    Username = u.Username,
                    Role = u.Role != null ? u.Role.Name : "Atanmadı"
                })
                .ToListAsync();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(currentUserId, out int parsedUserId))
            {
                var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
                {
                    UserId = parsedUserId,
                    ActionType = "Personel Listeleme",
                    Details = $"Sistemdeki personel listesi ({personnelList.Count} adet çalışan) görüntülendi."
                };
                _context.UserHistories.Add(historyRecord);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                Success = true,
                TotalRecords = personnelList.Count,
                Data = personnelList
            });
        }

        [HttpPost("add-user")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> AddUser([FromBody] UserCreateDto request)
        {
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            if (currentUserRole == "Manager" && request.RoleName != "Employee")
            {
                return StatusCode(403, "Yetki Hatası: Yöneticiler (Manager) sadece Personel (Employee) ekleyebilir.");
            }

            var userExists = await _context.Users.AnyAsync(u => u.Username == request.Username);
            if (userExists)
            {
                return BadRequest("Bu kullanıcı adı zaten sistemde kayıtlı.");
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.RoleName);
            if (role == null)
            {
                return BadRequest($"'{request.RoleName}' adında bir yetki bulunamadı. Lütfen geçerli bir rol girin (Örn: Admin, Manager, Employee).");
            }

            var newUser = new Simfer.PersonnelSystem.API.Entities.User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Username = request.Username,

                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),

                RoleId = role.Id
            };

            _context.Users.Add(newUser);

            var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
            {
                UserId = int.Parse(currentUserId),
                ActionType = "Kullanıcı Ekleme",
                Details = $"'{request.Username}' kullanıcı adıyla yeni bir personel sisteme eklendi."
            };
            _context.UserHistories.Add(historyRecord);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Yeni kullanıcı sisteme başarıyla eklendi."
            });
        }
        [HttpDelete("delete-user/{id}")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var userToDelete = await _context.Users.FindAsync(id);
            if (userToDelete == null)
            {
                return NotFound("Silinmek istenen kullanıcı bulunamadı.");
            }

            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserId == id.ToString())
            {
                return BadRequest("Kendi hesabınızı silemezsiniz.");
            }

            if (currentUserRole == "Manager" && userToDelete.RoleId != 3)
            {
                return StatusCode(403, "Yetki Hatası: Yöneticiler (Manager) sadece Personel (Employee) silebilir.");
            }

            userToDelete.IsDeleted = true; 

            var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
            {
                UserId = int.Parse(currentUserId),
                ActionType = "Kullanıcı Silme",
                Details = $"{userToDelete.FirstName} {userToDelete.LastName} ({userToDelete.Username}) adlı kullanıcı sistemden pasife alındı (Soft Delete)."
            };

            _context.UserHistories.Add(historyRecord);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Kullanıcı sistemden başarıyla silindi (Pasife alındı)."
            });
        }
    }
}