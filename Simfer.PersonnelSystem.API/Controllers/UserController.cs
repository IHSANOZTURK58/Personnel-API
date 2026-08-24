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
        // VERİTABANI BAĞLANTISI
        private readonly AppDbContext _context; // Kendi DbContext adını buraya yazmalısın!

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // 1. PROFİL SAYFASI (Herkes Girebilir)
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

        // 2. KİLİTLİ SAYFA (Sadece Adminler Girebilir - Gerçek SQL'den Çeker)
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

            // --- LOGLAMA BAŞLANGIÇ ---
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
            // --- LOGLAMA BİTİŞ ---

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
            // İsteği atan kişinin rolünü ve ID'sini okuyoruz (ID'yi tarihçe için kullanacağız)
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserRole == "Manager" && request.RoleId != 3)
            {
                return StatusCode(403, "Yetki Hatası: Yöneticiler (Manager) sadece Personel (Employee) ekleyebilir.");
            }

            var userExists = await _context.Users.AnyAsync(u => u.Username == request.Username);
            if (userExists)
            {
                return BadRequest("Bu kullanıcı adı zaten sistemde kayıtlı.");
            }

            var newUser = new Simfer.PersonnelSystem.API.Entities.User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Username = request.Username,
                PasswordHash = request.Password,
                RoleId = request.RoleId
            };

            // 1. Yeni kullanıcıyı tabloya eklemeye hazırla
            _context.Users.Add(newUser);

            // 2. YENİ: Tarihçe (History) defterine de bir kayıt düş
            var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
            {
                UserId = int.Parse(currentUserId), // Eklemeyi yapan kişinin ID'si
                ActionType = "Kullanıcı Ekleme",
                Details = $"'{request.Username}' kullanıcı adıyla yeni bir personel sisteme eklendi."
            };
            _context.UserHistories.Add(historyRecord);

            // 3. Her iki işlemi de tek seferde veritabanına kaydet!
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

            // İsteği atan kişinin (Token sahibinin) bilgilerini al
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

            // 1. Adamı veritabanından silinmeye hazırla
            _context.Users.Remove(userToDelete);

            // 2. YENİ: Tarihçe (History) defterine silindiğine dair kayıt düş
            var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
            {
                UserId = int.Parse(currentUserId), // Silme işlemini yapan yöneticinin ID'si
                ActionType = "Kullanıcı Silme",
                Details = $"{userToDelete.FirstName} {userToDelete.LastName} ({userToDelete.Username}) adlı kullanıcı sistemden silindi."
            };
            _context.UserHistories.Add(historyRecord);

            // 3. Hem silme işlemini hem de tarihçe kaydını tek seferde veritabanına işle
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Kullanıcı sistemden başarıyla silindi."
            });
        }



    }
}