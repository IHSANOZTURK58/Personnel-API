using System.ComponentModel.DataAnnotations;

namespace Simfer.PersonnelSystem.API.DTOs
{
    public class UserCreateDto
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Kullanıcı rolü (Yetki) zorunludur.")]
        public int RoleId { get; set; }
    }
}