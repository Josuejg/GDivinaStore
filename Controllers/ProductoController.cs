using GraciaDivina.Models;
using GraciaDivina.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace GraciaDivina.Controllers
{
    public class ProductoController : Controller
    {
        private readonly AccesoDatos _db;
        public ProductoController(AccesoDatos db) => _db = db;

        public async Task<IActionResult> Details(int id)
        {
            // 1) Producto
            var prod = await _db.ConsultarUnoAsync("gd_sp_Producto_Obtener",
                dr => new ProductDetailsVM
                {
                    ProductoID = dr.GetInt32(0),
                    Nombre = dr.GetString(1),
                    Descripcion = dr.IsDBNull(2) ? null : dr.GetString(2),
                    Precio = dr.GetDecimal(3),
                    ImagenUrl = dr.IsDBNull(4) ? null : dr.GetString(4),
                    CategoriaID = dr.IsDBNull(5) ? null : dr.GetInt32(5),
                    Categoria = dr.IsDBNull(6) ? null : dr.GetString(6)
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = id }));

            if (prod == null) return NotFound();

            // 2) Variantes
            prod.Variantes = await _db.ConsultarAsync("gd_sp_Variantes_PorProducto",
                dr => new ProductVariantVM
                {
                    VarianteID = dr.GetInt32(0),
                    SKU = dr.GetString(1),
                    Stock = dr.GetInt32(2),
                    Talla = dr.IsDBNull(4) ? null : dr.GetString(4),
                    Color = dr.IsDBNull(6) ? null : dr.GetString(6)
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = id }));

            // 3) Relacionados
            prod.Relacionados = await _db.ConsultarAsync("gd_sp_Producto_Relacionados",
                dr => new ProductCardVM
                {
                    ProductoID = dr.GetInt32(0),
                    Nombre = dr.GetString(1),
                    Precio = dr.GetDecimal(2),
                    ImagenUrl = dr.IsDBNull(3) ? null : dr.GetString(3)
                },
                cmd =>
                {
                    cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = id });
                    cmd.Parameters.Add(new SqlParameter("@Take", SqlDbType.Int) { Value = 4 });
                });

            return View(prod);
        }
    }
}
