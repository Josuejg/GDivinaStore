using System.ComponentModel.DataAnnotations;

namespace GraciaDivina.Models.Auth
{
    public class LoginVM
    {
        [Required(ErrorMessage = "Usuario requerido")]
        [StringLength(60)]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contraseña requerida")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
        public string? Error { get; set; }
    }
}
