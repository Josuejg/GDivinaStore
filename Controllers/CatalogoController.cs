using GraciaDivina.Models;
using GraciaDivina.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;

namespace GraciaDivina.Controllers
{
    public class CatalogoController : Controller
    {
        private readonly AccesoDatos _db;
        public CatalogoController(AccesoDatos db) => _db = db;

        public async Task<IActionResult> Index(int? categoriaId)
        {
            string catNombre = "Todas";
            if (categoriaId.HasValue)
            {
                var cat = await _db.ConsultarUnoAsync("gd_sp_Categoria_Obtener",
                    dr => new Categoria { CategoriaID = dr.GetInt32(0), Nombre = dr.GetString(1) },
                    cmd => cmd.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int) { Value = categoriaId.Value }));
                if (cat != null) catNombre = cat.Nombre;
            }
            ViewData["CategoriaNombre"] = catNombre;

            var productos = await _db.ConsultarAsync("gd_sp_Catalogo_Listar",
                dr => new ProductCardVM
                {
                    ProductoID = dr.GetInt32(0),
                    Nombre = dr.GetString(1),
                    Precio = dr.GetDecimal(2),
                    ImagenUrl = dr.IsDBNull(3) ? null : dr.GetString(3),
                    EnStock = true
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int)
                { Value = (object?)categoriaId ?? DBNull.Value })
            );

            return View(productos);
        }

    }
}
