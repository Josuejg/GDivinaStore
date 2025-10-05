using System.ComponentModel.DataAnnotations;

namespace GraciaDivina.Models.ViewModels
{
    public class CheckoutVM
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(120, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 120 caracteres.")]
        // Solo letras (con tildes/ñ), espacios y - . '
        [RegularExpression(@"^[\p{L}\p{M} .'-]+$",
            ErrorMessage = "El nombre solo debe contener letras y espacios.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [DataType(DataType.PhoneNumber)]
        // Solo números, entre 8 y 15 dígitos
        [RegularExpression(@"^\d{8,15}$",
            ErrorMessage = "El teléfono debe contener solo números (8 a 15 dígitos).")]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "La dirección no debe superar 300 caracteres.")]
        public string? Direccion { get; set; }

        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        // Requiere dominio con punto y TLD de 2+ letras
        [RegularExpression(@"^[^@\s]+@([A-Za-z0-9-]+\.)+[A-Za-z]{2,}$",
            ErrorMessage = "El correo debe tener un dominio válido (ej. usuario@dominio.com).")]
        [StringLength(120, ErrorMessage = "El correo no debe superar 120 caracteres.")]
        public string? Email { get; set; }

        // Resumen
        public CartVM Carrito { get; set; } = new();
        public int? PedidoID { get; set; }
    }
}
