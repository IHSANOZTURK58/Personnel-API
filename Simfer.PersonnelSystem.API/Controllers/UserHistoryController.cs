using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Simfer.PersonnelSystem.API.Data;

namespace Simfer.PersonnelSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin, Manager")] // 🚀 Güvenlik: Sadece yetkililer görebilir!
    public class UserHistoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserHistoryController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAllLogs()
        {
            try
            {
                var logs = await _context.UserHistories
                    .Include(h => h.User) // Kullanıcı bilgilerini birleştir
                    .IgnoreQueryFilters() // 🚀 SİHİR BURADA: Silinmiş personelleri de getir!
                    .OrderByDescending(h => h.Id) // En son yapılan işlem en üstte görünsün
                    .Select(h => new
                    {
                        h.Id,
                        h.ActionType,
                        h.Details,
                        // Tarih kolonunun adını sen ne yaptıysan (örn: CreatedAt, Date vs) buraya ekleyebilirsin

                        // İşten çıkanın yanına "(Silinmiş/Pasif)" yazdıralım:
                        PersonnelName = h.User != null
                            ? $"{h.User.FirstName} {h.User.LastName} {(h.User.IsDeleted ? "(Silinmiş/Pasif)" : "")}"
                            : "Sistem / Bilinmeyen"
                    })
                    .ToListAsync();

                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }
    }
}