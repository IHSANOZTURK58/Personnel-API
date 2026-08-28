using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Simfer.PersonnelSystem.API.Entities
{
    [Table("FaultyProducts")]
    public class FaultyProduct
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ürün adı boş bırakılamaz.")]
        [MaxLength(100)]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Barkod numarası zorunludur.")]
        [MaxLength(50)]
        public string BarcodeNumber { get; set; }

        [Required(ErrorMessage = "Hata açıklaması zorunludur.")]
        [MaxLength(500)]
        public string DefectDescription { get; set; }
        public string FaultCategory { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string ImageFileName { get; set; }
        public string? ResolutionDetails { get; set; }
        public DateTime? ResolvedDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int UserId { get; set; }

        public User User { get; set; }
        public bool IsResolved { get; set; } = false;
    }
}