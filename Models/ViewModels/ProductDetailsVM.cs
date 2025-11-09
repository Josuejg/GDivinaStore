using System.Collections.Generic;

namespace GraciaDivina.Models.ViewModels
{
    public class ProductDetailsVM
    {
        // ===== Datos base del producto =====
        public int ProductoID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }

        // Si tu SP de producto devuelve Precio, lo mapeamos aquí:
        public decimal Precio { get; set; }

        // Si tu SP devuelve una url principal, mapea aquí (puede ser null)
        public string? ImagenUrl { get; set; }

        public int? CategoriaID { get; set; }
        public string? Categoria { get; set; }

        // ===== Variantes y relacionados =====
        public List<ProductVariantVM> Variantes { get; set; } = new();
        public List<ProductCardVM> Relacionados { get; set; } = new();
    }
}
