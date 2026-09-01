using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Simfer.PersonnelSystem.API.Data;

namespace Simfer.PersonnelSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin, Manager")] 
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
                    .Include(h => h.User) 
                    .IgnoreQueryFilters() 
                    .OrderByDescending(h => h.Id) 
                    .Select(h => new
                    {
                        h.Id,
                        h.ActionType,
                        h.Details,

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