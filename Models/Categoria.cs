namespace GraciaDivina.Models
{ 
    public class Categoria
    {
        public int CategoriaID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public string? Descripcion { get; set; }   
        public DateTime FechaCreacion { get; set; }
    }
}
