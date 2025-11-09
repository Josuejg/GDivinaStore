namespace GraciaDivina.Models.ViewModels
{
    public class BuscarProductVM
    {

        // Parámetros de búsqueda/paginación
        public string? Q { get; set; }                 // término buscado
        public int Page { get; set; } = 1;             // página (1-based)
        public int PageSize { get; set; } = 24;        // tamaño de página

        // Resultado
        public int Total { get; set; }                 // total de coincidencias
        public IEnumerable<ProductCardVM> Resultados { get; set; }
            = Enumerable.Empty<ProductCardVM>();
    }
}
