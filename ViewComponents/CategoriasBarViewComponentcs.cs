using GraciaDivina.Models;
using Microsoft.AspNetCore.Mvc;

namespace GraciaDivina.ViewComponents
{
    public class CategoriasBarViewComponent : ViewComponent
    {
        private readonly AccesoDatos _db;
        public CategoriasBarViewComponent(AccesoDatos db) => _db = db;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var items = await _db.ConsultarAsync("gd_sp_Categoria_Menu",
                dr => new Categoria
                {
                    CategoriaID = dr.GetInt32(0),
                    Nombre = dr.GetString(1)
                });

            return View(items);
        }
    }
}
