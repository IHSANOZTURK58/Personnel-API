using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Simfer.PersonnelSystem.API.Data;
using Simfer.PersonnelSystem.API.DTOs;
using Simfer.PersonnelSystem.API.Entities;
using Simfer.PersonnelSystem.API.Services;
using System.Security.Claims;

namespace Simfer.PersonnelSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaultyProductsController : ControllerBase
    {
        private readonly MinioService _minioService;
        private readonly AppDbContext _context;

        public FaultyProductsController(MinioService minioService, AppDbContext context)
        {
            _minioService = minioService;
            _context = context;
        }

        [HttpPost("report-faulty-product")]
        [Authorize]
        public async Task<IActionResult> ReportFaultyProduct([FromForm] FaultyProductCreateDto request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("Lütfen geçerli bir hatalı ürün fotoğrafı yükleyin.");

            try
            {
                string generatedFileName = await _minioService.UploadFileAsync(request.File);

                var newFaultyProduct = new FaultyProduct
                {
                    ProductName = request.ProductName,
                    BarcodeNumber = request.BarcodeNumber,
                    DefectDescription = request.DefectDescription,
                    ImageFileName = generatedFileName,
                    CreatedDate = DateTime.Now,
                    IsResolved = false
                };

                _context.FaultyProducts.Add(newFaultyProduct);
                await _context.SaveChangesAsync();

                // --- LOGLAMA BAŞLANGIÇ ---
                // İstek atan kişinin Token'ından ID'sini alıyoruz
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
                    {
                        UserId = int.Parse(currentUserId),
                        ActionType = "Hatalı Ürün Kaydı",
                        Details = $"{request.BarcodeNumber} barkodlu '{request.ProductName}' hatalı ürün olarak sisteme eklendi."
                    };
                    _context.UserHistories.Add(historyRecord);
                    await _context.SaveChangesAsync();
                }
                // --- LOGLAMA BİTİŞ ---

                return Ok(new
                {
                    Message = "Harika! Hatalı ürün başarıyla kaydedildi.",
                    SavedFileName = generatedFileName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }

        [HttpGet("get-image-url/{fileName}")]
        [Authorize] // EKLENDİ: Kimin resme baktığını bilmek için token zorunlu kılındı
        public async Task<IActionResult> GetImageUrl(string fileName)
        {
            try
            {
                string url = await _minioService.GetFileUrlAsync(fileName);

                // --- LOGLAMA BAŞLANGIÇ ---
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(currentUserId, out int parsedUserId))
                {
                    var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
                    {
                        UserId = parsedUserId,
                        ActionType = "Görsel Görüntüleme",
                        Details = $"'{fileName}' isimli hatalı ürün görselinin bağlantısı görüntülendi."
                    };
                    _context.UserHistories.Add(historyRecord);
                    await _context.SaveChangesAsync();
                }
                // --- LOGLAMA BİTİŞ ---

                return Ok(new { TemporaryUrl = url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }

        [HttpGet("get-all")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetAllFaultyProducts()
        {
            try
            {
                var products = await _context.FaultyProducts
                    .OrderByDescending(p => p.CreatedDate)
                    .ToListAsync();

                // --- LOGLAMA BAŞLANGIÇ ---
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(currentUserId, out int parsedUserId))
                {
                    var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
                    {
                        UserId = parsedUserId,
                        ActionType = "Hatalı Ürün Listeleme",
                        Details = $"Sistemdeki tüm hatalı ürünler listesi ({products.Count} adet kayıt) görüntülendi."
                    };
                    _context.UserHistories.Add(historyRecord);
                    await _context.SaveChangesAsync();
                }
                // --- LOGLAMA BİTİŞ ---

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }
    }
}