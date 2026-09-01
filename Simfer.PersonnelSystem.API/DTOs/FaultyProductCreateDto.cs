using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Simfer.PersonnelSystem.API.DTOs
{
    public class FaultyProductCreateDto
    {
        [Required(ErrorMessage = "Barkod numarası zorunludur.")]
        public string BarcodeNumber { get; set; }

        [Required(ErrorMessage = "Lütfen bir ürün seçin.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Hata açıklaması zorunludur.")]
        public string DefectDescription { get; set; }

        [Required(ErrorMessage = "Lütfen hatalı ürünün fotoğrafını ekleyin.")]
        public IFormFile File { get; set; }

        [Required(ErrorMessage = "Lütfen bir hata kategorisi seçin.")]
        public int FaultCategoryId { get; set; }
    }
}