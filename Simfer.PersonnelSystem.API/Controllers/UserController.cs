using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Simfer.PersonnelSystem.API.Data;
using System.Security.Claims;

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
        [HttpGet("personel-listesi")]
        [Authorize(Roles = "Admin, Yönetici")]
        public async Task<IActionResult> GetPersonnelList()
        {
            // Kendi yerel veritabanındaki (Local SQL) personelleri çekiyoruz
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

            return Ok(new
            {
                Success = true,
                TotalRecords = personnelList.Count,
                Data = personnelList
            });
        }
    }
}