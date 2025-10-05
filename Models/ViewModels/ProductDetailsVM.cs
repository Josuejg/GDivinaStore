namespace GraciaDivina.Models.ViewModels
{
    public class ProductDetailsVM
    {
        // Datos base
        public int ProductoID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public string? ImagenUrl { get; set; }
        public int? CategoriaID { get; set; }
        public string? Categoria { get; set; }

        // Variantes y relacionados
        public List<ProductVariantVM> Variantes { get; set; } = new();
        public List<ProductCardVM> Relacionados { get; set; } = new();
    }
}
