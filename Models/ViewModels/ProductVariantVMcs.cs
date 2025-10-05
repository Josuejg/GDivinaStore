namespace GraciaDivina.Models.ViewModels
{
    public class ProductVariantVM
    {
        public int VarianteID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public int Stock { get; set; }
        public string? Talla { get; set; }
        public string? Color { get; set; }
        public string Display => $"{(Talla ?? "Única")}{(Color != null ? " / " + Color : "")}  (Stock: {Stock})";
    }
}
