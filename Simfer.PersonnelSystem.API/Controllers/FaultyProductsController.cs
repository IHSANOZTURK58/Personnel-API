using Microsoft.AspNetCore.Authorization; // 1. EKLENEN YER: Güvenlik kütüphanesi buraya eklendi
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Simfer.PersonnelSystem.API.Data;
using Simfer.PersonnelSystem.API.DTOs;
using Simfer.PersonnelSystem.API.Entities;
using Simfer.PersonnelSystem.API.Services;

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
        public async Task<IActionResult> GetImageUrl(string fileName)
        {
            try
            {
                string url = await _minioService.GetFileUrlAsync(fileName);
                return Ok(new { TemporaryUrl = url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }

        [HttpGet("get-all")]
        [Authorize(Roles = "Admin, Yönetici")] // 2. EKLENEN YER: Sadece bu rütbeler listeyi görebilir kuralı eklendi
        public async Task<IActionResult> GetAllFaultyProducts()
        {
            try
            {
                var products = await _context.FaultyProducts
                    .OrderByDescending(p => p.CreatedDate)
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }
    }
}