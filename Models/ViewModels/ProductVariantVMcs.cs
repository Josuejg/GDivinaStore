namespace GraciaDivina.Models.ViewModels
{
    public class ProductVariantVM
    {
        public int VarianteID { get; set; }
        public string SKU { get; set; } = "";
        public int Stock { get; set; }

        public int? TallaID { get; set; }
        public string? Talla { get; set; }

        public int? ColorID { get; set; }
        public string? Color { get; set; }

        public string? ImagenUrl { get; set; }
        public bool? Activo { get; internal set; }
    }
}
