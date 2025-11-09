using GraciaDivina.Models;
using GraciaDivina.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // <-- agregado para SelectListItem
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text.Json;

namespace GraciaDivina.Controllers
{
    public class ProductoController : Controller
    {
        private readonly AccesoDatos _db;
        public ProductoController(AccesoDatos db) => _db = db;



        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // ===== 1) Producto =====
            var prod = await _db.ConsultarUnoAsync("gd_sp_Producto_Obtener",
                dr => new ProductDetailsVM
                {
                    ProductoID = dr.GetInt32(dr.GetOrdinal("ProductoID")),
                    Nombre = dr.GetString(dr.GetOrdinal("Nombre")),
                    Descripcion = dr.IsDBNull(dr.GetOrdinal("Descripcion"))
                                  ? null
                                  : dr.GetString(dr.GetOrdinal("Descripcion")),
                    Precio = dr.ColumnExists("Precio") && !dr.IsDBNull(dr.GetOrdinal("Precio"))
                                  ? dr.GetDecimal(dr.GetOrdinal("Precio"))
                                  : 0m,
                    ImagenUrl = dr.ColumnExists("ImagenUrl") && !dr.IsDBNull(dr.GetOrdinal("ImagenUrl"))
                                  ? dr.GetString(dr.GetOrdinal("ImagenUrl"))
                                  : null,
                    CategoriaID = dr.ColumnExists("CategoriaID") && !dr.IsDBNull(dr.GetOrdinal("CategoriaID"))
                                  ? dr.GetInt32(dr.GetOrdinal("CategoriaID"))
                                  : (int?)null,
                    Categoria = dr.ColumnExists("Categoria") && !dr.IsDBNull(dr.GetOrdinal("Categoria"))
                                  ? dr.GetString(dr.GetOrdinal("Categoria"))
                                  : null
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = id })
            );

            if (prod == null) return NotFound();

            // ===== 2) Variantes (REUTILIZA tu SP admin) =====
            // Esperado: VarianteID, SKU, Stock, TallaID, Talla, ColorID, Color, UrlImagen, (opcional Activo)
            var variantes = await _db.ConsultarAsync("gd_sp_Variantes_PorProducto",
                dr => new ProductVariantVM
                {
                    VarianteID = dr.GetInt32(dr.GetOrdinal("VarianteID")),
                    SKU = dr.ColumnExists("SKU") && !dr.IsDBNull(dr.GetOrdinal("SKU"))
                                   ? dr.GetString(dr.GetOrdinal("SKU"))
                                   : string.Empty,
                    Stock = dr.ColumnExists("Stock") && !dr.IsDBNull(dr.GetOrdinal("Stock"))
                                   ? dr.GetInt32(dr.GetOrdinal("Stock"))
                                   : 0,

                    TallaID = dr.ColumnExists("TallaID") && !dr.IsDBNull(dr.GetOrdinal("TallaID"))
                                   ? dr.GetInt32(dr.GetOrdinal("TallaID"))
                                   : (int?)null,
                    Talla = dr.ColumnExists("Talla") && !dr.IsDBNull(dr.GetOrdinal("Talla"))
                                   ? dr.GetString(dr.GetOrdinal("Talla"))
                                   : null,

                    ColorID = dr.ColumnExists("ColorID") && !dr.IsDBNull(dr.GetOrdinal("ColorID"))
                                   ? dr.GetInt32(dr.GetOrdinal("ColorID"))
                                   : (int?)null,
                    Color = dr.ColumnExists("Color") && !dr.IsDBNull(dr.GetOrdinal("Color"))
                                   ? dr.GetString(dr.GetOrdinal("Color"))
                                   : null,

                    ImagenUrl = dr.ColumnExists("UrlImagen") && !dr.IsDBNull(dr.GetOrdinal("UrlImagen"))
                                   ? dr.GetString(dr.GetOrdinal("UrlImagen"))
                                   : null,

                    // si tu SP expone Activo, lo tomamos; si no, queda null (no rompe)
                    Activo = dr.ColumnExists("Activo") && !dr.IsDBNull(dr.GetOrdinal("Activo"))
                                   ? dr.GetBoolean(dr.GetOrdinal("Activo"))
                                   : (bool?)null
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = id })
            );

            // Filtro público en C#: si existe Activo, respétalo; si no, no filtramos por Activo.
            var variantesPublicas = variantes
              .Where(v => v != null /* && v.Stock > 0 */)
              .ToList();

            prod.Variantes = variantesPublicas;

            // << NUEVO: json compacto para el JS de la vista >>
            var map = variantesPublicas.Select(v => new {
                VarianteID = v.VarianteID,
                TallaID = v.TallaID,
                ColorID = v.ColorID,
                Stock = v.Stock
            }).ToList();
            ViewBag.VariantesJson = JsonSerializer.Serialize(map);

            // Combos (no cambia)
            ViewBag.Tallas = variantesPublicas
                .Where(v => v.TallaID.HasValue && !string.IsNullOrWhiteSpace(v.Talla))
                .GroupBy(v => new { v.TallaID, v.Talla })
                .Select(g => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                { Value = g.Key.TallaID!.Value.ToString(), Text = g.Key.Talla! })
                .OrderBy(x => x.Text)
                .ToList();

            ViewBag.Colores = variantesPublicas
                .Where(v => v.ColorID.HasValue && !string.IsNullOrWhiteSpace(v.Color))
                .GroupBy(v => new { v.ColorID, v.Color })
                .Select(g => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                { Value = g.Key.ColorID!.Value.ToString(), Text = g.Key.Color! })
                .OrderBy(x => x.Text)
                .ToList();

            // ===== 4) Relacionados (sin cambios) =====
            prod.Relacionados = await _db.ConsultarAsync("gd_sp_Producto_Relacionados",
                dr => new ProductCardVM
                {
                    ProductoID = dr.GetInt32(dr.GetOrdinal("ProductoID")),
                    Nombre = dr.GetString(dr.GetOrdinal("Nombre")),
                    Precio = dr.ColumnExists("Precio") && !dr.IsDBNull(dr.GetOrdinal("Precio"))
                                   ? dr.GetDecimal(dr.GetOrdinal("Precio"))
                                   : 0m,
                    ImagenUrl = dr.ColumnExists("ImagenUrl") && !dr.IsDBNull(dr.GetOrdinal("ImagenUrl"))
                                   ? dr.GetString(dr.GetOrdinal("ImagenUrl"))
                                   : null
                },
                cmd =>
                {
                    cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = id });
                    cmd.Parameters.Add(new SqlParameter("@Take", SqlDbType.Int) { Value = 4 });
                }
            );

            return View(prod);
        }
        // Acción de búsqueda con paginación         
        [HttpGet]
        public async Task<IActionResult> Buscar(string? q, int page = 1, int pageSize = 24)
        {
            // 1) Lista paginada
            var productos = await _db.ConsultarAsync(
                "gd_sp_Producto_Buscar",
                dr => new ProductCardVM
                {
                    ProductoID = dr.GetInt32(dr.GetOrdinal("ProductoID")),
                    Nombre = dr.GetString(dr.GetOrdinal("Nombre")),
                    Precio = dr.IsDBNull(dr.GetOrdinal("Precio")) ? 0m : dr.GetDecimal(dr.GetOrdinal("Precio")),
                    ImagenUrl = dr.IsDBNull(dr.GetOrdinal("ImagenUrl")) ? null : dr.GetString(dr.GetOrdinal("ImagenUrl"))
                },
                cmd => {
                    cmd.Parameters.AddWithValue("@q", (object?)q ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@page", page);
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);
                }
            );

            // 2) Total (usa un SP de conteo simple; si no lo tienes, te lo paso)
            var total = await _db.EscalarAsync<int>(
                "gd_sp_Producto_Buscar_Total",
                cmd => {
                    cmd.Parameters.AddWithValue("@q", (object?)q ?? DBNull.Value);
                }
            );

            var vm = new BuscarProductVM
            {
                Q = q,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Resultados = productos
            };
            ViewBag.Query = q;
            ViewBag.Count = total;

            return View(vm); // Views/Producto/Buscar.cshtml
        }



    }


    // Helper para evitar IndexOutOfRange cuando falte una columna
    internal static class DataRecordExt
    {
        public static bool ColumnExists(this IDataRecord dr, string name)
        {
            for (int i = 0; i < dr.FieldCount; i++)
                if (dr.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
