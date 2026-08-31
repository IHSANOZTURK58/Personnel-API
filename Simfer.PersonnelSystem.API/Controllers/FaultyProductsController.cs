using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Simfer.PersonnelSystem.API.Data;
using Simfer.PersonnelSystem.API.DTOs;
using Simfer.PersonnelSystem.API.Entities;
using Simfer.PersonnelSystem.API.Services;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed; 
using System.Text.Json; 

namespace Simfer.PersonnelSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaultyProductsController : ControllerBase
    {
        private readonly MinioService _minioService;
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;

        public FaultyProductsController(MinioService minioService, AppDbContext context, IDistributedCache cache)
        {
            _minioService = minioService;
            _context = context;
            _cache = cache;
        }
        [HttpGet("my-daily-reports")]
        [Authorize]
        public async Task<IActionResult> GetMyDailyReports()
        {
            try
            {
                var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(currentUserIdStr, out int parsedUserId))
                {
                    return Unauthorized("Güvenlik İhlali: Geçerli bir kullanıcı kimliği bulunamadı.");
                }

                var today = DateTime.UtcNow.AddHours(3).Date;

                var myDailyFaults = await _context.FaultyProducts
                    .Where(p => p.UserId == parsedUserId && p.CreatedDate.Date == today)
                    .OrderByDescending(p => p.CreatedDate)
                    .Select(p => new
                    {
                        p.Id,
                        p.ProductName,
                        p.BarcodeNumber,
                        p.DefectDescription,
                        p.ImageFileName,
                        p.CreatedDate,
                        p.IsResolved,
                        p.ResolutionDetails,
                        p.FaultCategory
                    })
                    .ToListAsync();

                return Ok(myDailyFaults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }

        [HttpPost("report-faulty-product")]
        [Authorize]
        public async Task<IActionResult> ReportFaultyProduct([FromForm] FaultyProductCreateDto request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("Lütfen geçerli bir hatalı ürün fotoğrafı yükleyin.");

            try
            {
                var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(currentUserIdStr, out int parsedUserId))
                {
                    return Unauthorized("Güvenlik İhlali: Geçerli bir kullanıcı kimliği bulunamadı.");
                }

                var doubleTapCheck = await _context.FaultyProducts
                    .AnyAsync(x => x.BarcodeNumber == request.BarcodeNumber &&
                                   x.CreatedDate > DateTime.UtcNow.AddHours(3).AddSeconds(-5));

                if (doubleTapCheck)
                {
                    return BadRequest("Bu arıza kaydı az önce zaten oluşturuldu. Lütfen bekleyin.");
                }

                string generatedFileName = await _minioService.UploadFileAsync(request.File);

                var newFaultyProduct = new FaultyProduct
                {
                    ProductName = request.ProductName,
                    BarcodeNumber = request.BarcodeNumber,
                    DefectDescription = request.DefectDescription,
                    ImageFileName = generatedFileName,
                    CreatedDate = DateTime.UtcNow.AddHours(3),
                    IsResolved = false,
                    UserId = parsedUserId,
                    FaultCategory = request.FaultCategory,
                };

                _context.FaultyProducts.Add(newFaultyProduct);

                var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
                {
                    UserId = parsedUserId,
                    ActionType = "Hatalı Ürün Kaydı",
                    Details = $"{request.BarcodeNumber} barkodlu '{request.ProductName}' hatalı ürün olarak sisteme eklendi."
                };

                _context.UserHistories.Add(historyRecord);
                await _context.SaveChangesAsync();

                await _cache.RemoveAsync("faults_list");

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
        [Authorize]
        public async Task<IActionResult> GetImageUrl(string fileName)
        {
            // Bu metotta Redis'lik bir durum yok, orijinal haliyle kaldı.
            try
            {
                string url = await _minioService.GetFileUrlAsync(fileName);

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

                return Ok(new { TemporaryUrl = url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }

        [HttpPut("resolve")]
        public async Task<IActionResult> ResolveFault([FromBody] FaultyProductResolveDto request)
        {
            var faultyProduct = await _context.FaultyProducts.FindAsync(request.Id);

            if (faultyProduct == null)
                return NotFound("Belirtilen arıza kaydı bulunamadı.");

            if (faultyProduct.IsResolved)
                return BadRequest("Bu arıza zaten çözülmüş olarak işaretlenmiş.");

            faultyProduct.IsResolved = true;
            faultyProduct.ResolutionDetails = request.ResolutionDetails;
            faultyProduct.ResolvedDate = DateTime.UtcNow.AddHours(3);

            _context.FaultyProducts.Update(faultyProduct);

            var currentUserId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(currentUserId) && int.TryParse(currentUserId, out int parsedUserId))
            {
                var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
                {
                    UserId = parsedUserId,
                    ActionType = "Arıza Çözümü",
                    Details = $"'{faultyProduct.ProductName}' (Barkod: {faultyProduct.BarcodeNumber}) isimli arıza çözüldü. Çözüm Detayı: {request.ResolutionDetails}"
                };

                _context.UserHistories.Add(historyRecord);
            }

            await _context.SaveChangesAsync();

            // YENİ EKLENDİ: Arıza çözüldüğünde Redis'teki eski listeyi temizle
            await _cache.RemoveAsync("faults_list");

            return Ok(new { message = "Arıza başarıyla çözüldü ve detaylar sisteme kaydedildi." });
        }

        [HttpGet("get-all")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetAllFaultyProducts()
        {
            try
            {
                string cacheKey = "faults_list";
                object responseData;

                var cachedData = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedData))
                {
                    responseData = JsonSerializer.Deserialize<object>(cachedData);
                }
                else
                {
                    var products = await _context.FaultyProducts
                        .Include(p => p.User)
                        .OrderByDescending(p => p.CreatedDate)
                      .Select(p => new
                      {
                          p.Id,
                          p.ProductName,
                          p.BarcodeNumber,
                          p.DefectDescription,
                          p.ImageFileName,
                          p.CreatedDate,
                          p.IsResolved,
                          p.ResolutionDetails,
                          p.ResolvedDate,
                          ReporterName = p.User != null ? $"{p.User.FirstName} {p.User.LastName}" : "Bilinmiyor"
                      })
                        .ToListAsync();

                    responseData = products;

                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    };
                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(products), cacheOptions);
                }

                var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(currentUserIdStr, out int parsedUserId))
                {
                    var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
                    {
                        UserId = parsedUserId,
                        ActionType = "Hatalı Ürün Listeleme",
                        Details = $"Sistemdeki tüm hatalı ürünler listesi görüntülendi (Redis Destekli)."
                    };
                    _context.UserHistories.Add(historyRecord);
                    await _context.SaveChangesAsync();
                }

                return Ok(responseData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }
    }
}