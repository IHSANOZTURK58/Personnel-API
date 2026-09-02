using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Sockets;

namespace Simfer.PersonnelSystem.API.Entities
{
    [Table("FaultyProducts")]
    public class FaultyProduct
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Barkod numarası zorunludur.")]
        [MaxLength(50)]
        public string BarcodeNumber { get; set; }

        [Required(ErrorMessage = "Hata açıklaması zorunludur.")]
        [MaxLength(500)]
        public string DefectDescription { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string ImageFileName { get; set; }

        public string? ResolutionDetails { get; set; }
        public DateTime? ResolvedDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsResolved { get; set; } = false;

        public int UserId { get; set; }
        public User User { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int FaultCategoryId { get; set; }
        public FaultCategory FaultCategory { get; set; }

        public int? ResolvedByUserId { get; set; }

        [ForeignKey("ResolvedByUserId")]
        public User? ResolvedByUser { get; set; }
    }
}
