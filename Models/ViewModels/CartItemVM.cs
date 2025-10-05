namespace GraciaDivina.Models.ViewModels
{
    public class CartItemVM
    {
        public long CarritoItemID { get; set; }
        public int VarianteID { get; set; }
        public int ProductoID { get; set; }
        public string Producto { get; set; } = string.Empty;
        public string? Talla { get; set; }
        public string? Color { get; set; }
        public string SKU { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal { get; set; }
        public string? ImagenUrl { get; set; }
    }
}
