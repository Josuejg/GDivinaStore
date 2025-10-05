using System.ComponentModel.DataAnnotations;

namespace GraciaDivina.Models.Auth
{
    public class ChangePasswordVM
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [StringLength(64, MinimumLength = 8,
            ErrorMessage = "La contraseña debe tener entre 8 y 64 caracteres.")]
        // al menos: 1 minúscula, 1 mayúscula, 1 dígito y 1 símbolo
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,64}$",
            ErrorMessage = "Debe incluir mayúscula, minúscula, número y símbolo.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmNewPassword { get; set; } = string.Empty;

        public string? Message { get; set; } // para feedback
    }
}
