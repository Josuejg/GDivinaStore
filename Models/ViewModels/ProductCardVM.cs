namespace GraciaDivina.Models.ViewModels
{
    public class ProductCardVM
    {
        public int ProductoID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public string? ImagenUrl { get; set; }
        public bool EnStock { get; set; } = true;     // si luego lees stock por variante
        public string? Badge { get; set; }            // "Nuevo", "Promo", etc. (opcional)
    }
}
