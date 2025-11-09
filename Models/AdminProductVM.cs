using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace GraciaDivina.Models.ViewModels
{
    public class AdminProductVM
    {
        public int? ProductoID { get; set; }

        [Required, StringLength(120)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(800)]
        public string? Descripcion { get; set; }

        [Range(0, 999999999.99)]
        public decimal Precio { get; set; }

        public int? CategoriaID { get; set; }
        public string? Categoria { get; set; }

        public bool Activo { get; set; } = true;

        public string? ImagenUrl { get; set; }

        // Upload
        public IFormFile? Imagen { get; set; }
     

    }
}
