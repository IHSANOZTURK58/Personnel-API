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
using ClosedXML.Excel;

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
                    .Include(p => p.Product)
                    .Include(p => p.FaultCategory)
                    .Include(p => p.ResolvedByUser)
                    .Where(p => p.UserId == parsedUserId && p.CreatedDate.Date == today)
                    .OrderByDescending(p => p.CreatedDate)
                    .Select(p => new
                    {
                        p.Id,
                        ProductName = p.Product.Name,
                        p.BarcodeNumber,
                        p.DefectDescription,
                        p.ImageFileName,
                        p.CreatedDate,
                        p.IsResolved,
                        p.ResolutionDetails,
                        FaultCategory = p.FaultCategory.Name,
                        ResolvedByName = p.ResolvedByUser != null ? p.ResolvedByUser.FirstName + " " + p.ResolvedByUser.LastName : null
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
                    ProductId = request.ProductId,
                    FaultCategoryId = request.FaultCategoryId,
                    BarcodeNumber = request.BarcodeNumber,
                    DefectDescription = request.DefectDescription,
                    ImageFileName = generatedFileName,
                    CreatedDate = DateTime.UtcNow.AddHours(3),
                    IsResolved = false,
                    UserId = parsedUserId
                };

                _context.FaultyProducts.Add(newFaultyProduct);

                var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
                {
                    UserId = parsedUserId,
                    ActionType = "Hatalı Ürün Kaydı",
                    Details = $"{request.BarcodeNumber} barkodlu hatalı ürün sisteme eklendi."
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
        [Authorize]
        public async Task<IActionResult> ResolveFault([FromBody] FaultyProductResolveDto request)
        {
            var faultyProduct = await _context.FaultyProducts
                                              .Include(f => f.Product)
                                              .FirstOrDefaultAsync(f => f.Id == request.Id);

            if (faultyProduct == null)
                return NotFound("Belirtilen arıza kaydı bulunamadı.");

            if (faultyProduct.IsResolved)
                return BadRequest("Bu arıza zaten çözülmüş olarak işaretlenmiş.");

            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(currentUserIdStr, out int parsedUserId))
            {
                return Unauthorized("Güvenlik İhlali: Geçerli bir kullanıcı kimliği bulunamadı.");
            }

            faultyProduct.IsResolved = true;
            faultyProduct.ResolutionDetails = request.ResolutionDetails;
            faultyProduct.ResolvedDate = DateTime.UtcNow.AddHours(3);
            faultyProduct.ResolvedByUserId = parsedUserId;

            _context.FaultyProducts.Update(faultyProduct);

            var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
            {
                UserId = parsedUserId,
                ActionType = "Arıza Çözümü",
                Details = $"'{faultyProduct.Product.Name}' (Barkod: {faultyProduct.BarcodeNumber}) isimli arıza çözüldü. Çözüm Detayı: {request.ResolutionDetails}"
            };

            _context.UserHistories.Add(historyRecord);

            await _context.SaveChangesAsync();
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
                        .IgnoreQueryFilters()
                        .Include(p => p.User)
                        .Include(p => p.Product)
                        .Include(p => p.FaultCategory)
                        .Include(p => p.ResolvedByUser)
                        .OrderByDescending(p => p.CreatedDate)
                      .Select(p => new
                      {
                          p.Id,
                          ProductName = p.Product.Name,
                          p.BarcodeNumber,
                          p.DefectDescription,
                          p.ImageFileName,
                          p.CreatedDate,
                          p.IsResolved,
                          p.ResolutionDetails,
                          p.ResolvedDate,
                          ReporterName = p.User != null ? $"{p.User.FirstName} {p.User.LastName}" : "Bilinmiyor",
                          FaultCategory = p.FaultCategory.Name,
                          ResolvedByName = p.ResolvedByUser != null ? p.ResolvedByUser.FirstName + " " + p.ResolvedByUser.LastName : null
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

        [HttpGet("faulty-products-excel")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> ExportFaultyProductsToExcel(
    [FromQuery] string tab = "bekleyen",
    [FromQuery] string search = "",
    [FromQuery] string category = "Tümü",
    [FromQuery] string product = "Tümü",
    [FromQuery] string time = "tumu",
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null)
        {
            var query = _context.FaultyProducts
                .IgnoreQueryFilters()
                .Include(p => p.Product)
                .Include(p => p.User)
                .Include(p => p.FaultCategory)
                .Include(p => p.ResolvedByUser)
                .AsQueryable();

            if (tab == "bekleyen") query = query.Where(q => !q.IsResolved);
            else if (tab == "cozulen") query = query.Where(q => q.IsResolved);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(q => q.BarcodeNumber.Contains(search));
            }

            if (!string.IsNullOrEmpty(product) && product != "Tümü")
            {
                query = query.Where(q => q.Product.Name == product);
            }

            if (!string.IsNullOrEmpty(category) && category != "Tümü")
            {
                query = query.Where(q => q.FaultCategory.Name == category);
            }

            var now = DateTime.UtcNow.AddHours(3);
            if (time == "7gun") query = query.Where(q => q.CreatedDate >= now.AddDays(-7));
            else if (time == "1ay") query = query.Where(q => q.CreatedDate >= now.AddMonths(-1));
            else if (time == "6ay") query = query.Where(q => q.CreatedDate >= now.AddMonths(-6));
            else if (time == "ozel" && startDate.HasValue && endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(q => q.CreatedDate >= startDate.Value.Date && q.CreatedDate <= endOfDay);
            }

            var faults = await query
                .OrderByDescending(p => p.CreatedDate)
                .Select(f => new
                {
                    Ürün_Adı = f.Product != null ? f.Product.Name : "Bilinmiyor",
                    Barkod_Numarası = f.BarcodeNumber,
                    Kategori = f.FaultCategory != null ? f.FaultCategory.Name : "Belirtilmemiş",
                    Arıza_Detayı = f.DefectDescription,
                    Durum = f.IsResolved ? "Çözüldü" : "Bekliyor",
                    Bildiren_Personel = f.User != null ? f.User.FirstName + " " + f.User.LastName : "Bilinmeyen Personel",
                    Kayıt_Tarihi = f.CreatedDate.ToString("dd.MM.yyyy HH:mm"),
                    Çözüm_Detayı = f.IsResolved && f.ResolutionDetails != null ? f.ResolutionDetails : "-",
                    Çözen_Personel = f.ResolvedByUser != null ? f.ResolvedByUser.FirstName + " " + f.ResolvedByUser.LastName : "-",
                    Çözüm_Tarihi = f.ResolvedDate != null ? f.ResolvedDate.Value.ToString("dd.MM.yyyy HH:mm") : "-"
                    
                })
                .ToListAsync();
            try
            {
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                ?? User.FindFirst("nameid")?.Value;

                if (int.TryParse(userIdString, out int parsedUserId))
                {
                    var currentUser = await _context.Users.FindAsync(parsedUserId);
                    string fullName = currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : "Bilinmeyen Personel";

                    var historyRecord = new Simfer.PersonnelSystem.API.Entities.UserHistory
                    {
                        UserId = parsedUserId,
                        ActionType = "Filtreli Excel Dışa Aktarım",
                        Details = $"{fullName}, arıza listesini Excel olarak indirdi. (Filtreler -> Ürün: {product}, Kategori: {category}, Zaman: {time})"
                    };

                    _context.UserHistories.Add(historyRecord);
                    await _context.SaveChangesAsync();
                }
            }
            catch { }

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Arıza Raporu");
                worksheet.Cell("A1").InsertTable(faults);
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Simfer_Ariza_Raporu.xlsx");
                }
            }
        }
    }
}