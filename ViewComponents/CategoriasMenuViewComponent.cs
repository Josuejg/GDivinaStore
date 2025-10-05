using GraciaDivina.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace GraciaDivina.ViewComponents
{
    public class CategoriasMenuViewComponent : ViewComponent
    {
        private readonly AccesoDatos _db;
        public CategoriasMenuViewComponent(AccesoDatos db) => _db = db;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var Lista = await _db.ConsultarAsync("gd_sp_Categoria_Menu",
                dr => new Categoria
                {
                    CategoriaID = dr.GetInt32(0),
                    Nombre = dr.GetString(1)
                });
            return View(Lista);
        }
    }
}
