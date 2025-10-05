namespace GraciaDivina.Models
{
    public class ProductoImagen
    {
        public int ImagenID { get; set; }
        public int ProductoID { get; set; }
        public string UrlImagen { get; set; } = "";
        public bool EsPrincipal { get; set; }
    }
}
