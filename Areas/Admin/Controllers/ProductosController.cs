using GraciaDivina.Models;
using GraciaDivina.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using System.IO; // <-- necesario para Path/FileStream

namespace GraciaDivina.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductosController : Controller
    {
        private readonly AccesoDatos _db;
        public ProductosController(AccesoDatos db) => _db = db;

        // ================= Helpers =================

        private async Task<SelectList> CargarCategoriasAsync()
        {
            var cats = await _db.ConsultarAsync("gd_sp_Categoria_Menu",
                dr => new { CategoriaID = dr.GetInt32(0), Nombre = dr.GetString(1) });
            return new SelectList(cats, "CategoriaID", "Nombre");
        }

        /// <summary>
        /// Carga combos (Tallas, Colores) y, si corresponde, la lista de variantes del producto.
        /// Deja la data en ViewBag.Tallas / ViewBag.Colores / ViewBag.Variantes.
        /// </summary>
        private async Task CargarCatalogosOpcionesAsync(int? productoId = null)
        {
            // Tallas (SP)
            ViewBag.Tallas = await _db.ConsultarAsync(
                "gd_sp_Talla_Listar",
                dr => new SelectListItem
                {
                    Value = dr.GetInt32(0).ToString(),
                    Text = dr.GetString(1)
                });

            // Colores (SP)
            ViewBag.Colores = await _db.ConsultarAsync(
                "gd_sp_Color_Listar",
                dr => new SelectListItem
                {
                    Value = dr.GetInt32(0).ToString(),
                    Text = dr.GetString(1)
                });

            // Variantes (SP) — solo si hay producto
            if (productoId.HasValue)
            {
                ViewBag.Variantes = await _db.ConsultarAsync(
                    "gd_sp_Variantes_PorProducto",
                    dr => new ProductVariantVM
                    {
                        VarianteID = dr.GetInt32(dr.GetOrdinal("VarianteID")),
                        SKU = dr.GetString(dr.GetOrdinal("SKU")),
                        Stock = dr.IsDBNull(dr.GetOrdinal("Stock")) ? 0 : dr.GetInt32(dr.GetOrdinal("Stock")),
                        TallaID = dr.IsDBNull(dr.GetOrdinal("TallaID")) ? (int?)null : dr.GetInt32(dr.GetOrdinal("TallaID")),
                        Talla = dr.IsDBNull(dr.GetOrdinal("Talla")) ? null : dr.GetString(dr.GetOrdinal("Talla")),
                        ColorID = dr.IsDBNull(dr.GetOrdinal("ColorID")) ? (int?)null : dr.GetInt32(dr.GetOrdinal("ColorID")),
                        Color = dr.IsDBNull(dr.GetOrdinal("Color")) ? null : dr.GetString(dr.GetOrdinal("Color"))
                    },
                    cmd => cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = productoId.Value })
                );
            }
            else
            {
                ViewBag.Variantes = Enumerable.Empty<ProductVariantVM>();
            }
        }

        // ================= Listado =================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var lista = await _db.ConsultarAsync("gd_sp_Producto_ListarAdmin",
             dr => new AdminProductVM
             {
                 ProductoID = dr.GetInt32(dr.GetOrdinal("ProductoID")),
                 Nombre = dr.GetString(dr.GetOrdinal("Nombre")),
                 CategoriaID = dr.GetInt32(dr.GetOrdinal("CategoriaID")),
                 Precio = dr.GetDecimal(dr.GetOrdinal("Precio")),
                 Activo = dr.GetBoolean(dr.GetOrdinal("Activo")),
                 ImagenUrl = dr.IsDBNull(dr.GetOrdinal("ImagenUrl")) ? null : dr.GetString(dr.GetOrdinal("ImagenUrl")),
                 Descripcion = dr.IsDBNull(dr.GetOrdinal("Descripcion")) ? null : dr.GetString(dr.GetOrdinal("Descripcion"))
             });

            return View(lista);
        }

        // ================= Crear (GET) =================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categorias = await CargarCategoriasAsync();
            ViewBag.Tallas = await CargarTallasAsync();
            ViewBag.Colores = await CargarColoresAsync();
            return View("Crear", new AdminProductVM { Activo = true });
        }

        // ================= Editar (GET) =================

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var vm = await _db.ConsultarUnoAsync(
                "gd_sp_Producto_Obtener",
                dr =>
                {
                    bool ReadBool(string col, bool def = false)
                    {
                        if (!dr.ColumnExists(col)) return def;
                        var o = dr.GetOrdinal(col);
                        return dr.IsDBNull(o) ? def : dr.GetBoolean(o);
                    }
                    int? ReadIntNullable(string col)
                    {
                        if (!dr.ColumnExists(col)) return null;
                        var o = dr.GetOrdinal(col);
                        return dr.IsDBNull(o) ? (int?)null : dr.GetInt32(o);
                    }
                    decimal ReadDecimal(string col, decimal def = 0m)
                    {
                        if (!dr.ColumnExists(col)) return def;
                        var o = dr.GetOrdinal(col);
                        return dr.IsDBNull(o) ? def : dr.GetDecimal(o);
                    }
                    string? ReadString(string col)
                    {
                        if (!dr.ColumnExists(col)) return null;
                        var o = dr.GetOrdinal(col);
                        return dr.IsDBNull(o) ? null : dr.GetString(o);
                    }

                    return new AdminProductVM
                    {
                        ProductoID = dr.GetInt32(dr.GetOrdinal("ProductoID")),
                        Nombre = ReadString("Nombre") ?? "",
                        CategoriaID = ReadIntNullable("CategoriaID") ?? 0,
                        Precio = ReadDecimal("Precio", 0m),
                        ImagenUrl = ReadString("ImagenUrl"),
                        Activo = ReadBool("Activo", true),
                        Descripcion = ReadString("Descripcion")
                    };
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = id })
            );

            if (vm is null) return NotFound();

            ViewBag.Categorias = await CargarCategoriasAsync();
            ViewBag.Tallas = await CargarTallasAsync();
            ViewBag.Colores = await CargarColoresAsync();

            return View("Editar", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarVariante(int varianteId, string? sku, int? tallaId, int? colorId, bool activo)
        {
            await _db.EjecutarAsync("gd_sp_Variante_Actualizar", cmd =>
            {
                cmd.Parameters.AddWithValue("@VarianteID", varianteId);
                cmd.Parameters.AddWithValue("@SKU", (object?)sku ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TallaID", (object?)tallaId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ColorID", (object?)colorId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Activo", activo ? 1 : 0);
            });

            TempData["ok"] = "Variante actualizada.";
            var referer = Request.Headers["Referer"].ToString();
            return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(AdminProductVM vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = await CargarCategoriasAsync();
                ViewBag.Tallas = await CargarTallasAsync();
                ViewBag.Colores = await CargarColoresAsync();
                return View("Crear", vm);
            }

            // ---- Subir imagen (opcional) ----
            if (vm.Imagen != null && vm.Imagen.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "products");
                Directory.CreateDirectory(uploads);

                var ext = Path.GetExtension(vm.Imagen.FileName);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(uploads, fileName);

                using (var fs = new FileStream(fullPath, FileMode.Create))
                    await vm.Imagen.CopyToAsync(fs);

                vm.ImagenUrl = $"/img/products/{fileName}";
            }

            // ---- Guardar/actualizar producto ----
            var id = await _db.EscalarAsync<int>("gd_sp_Producto_Guardar", cmd =>
            {
                cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int)
                { Direction = ParameterDirection.InputOutput, Value = (object?)vm.ProductoID ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 200) { Value = vm.Nombre });
                cmd.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int) { Value = vm.CategoriaID });
                cmd.Parameters.Add(new SqlParameter("@Precio", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = vm.Precio });
                cmd.Parameters.Add(new SqlParameter("@ImagenUrl", SqlDbType.NVarChar, 500) { Value = (object?)vm.ImagenUrl ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Activo", SqlDbType.Bit) { Value = vm.Activo });
                cmd.Parameters.Add(new SqlParameter("@Descripcion", SqlDbType.NVarChar, -1) { Value = (object?)vm.Descripcion ?? DBNull.Value });
            });
            // ========== Variantes iniciales (PRE-GUARDAR) ==========
            try
            {
                // 1) Tallas marcadas: acepta "tallasPre" y/o "tallas"
                var tallasMarcadas = Request.Form["tallasPre"].Concat(Request.Form["tallas"])
                    .Select(v => int.TryParse(v, out var x) ? x : 0)
                    .Where(x => x > 0)
                    .Distinct()
                    .ToArray();

                // 2) Campos opcionales
                string colorTexto =
                    (Request.Form["colorPreNombre"].FirstOrDefault()
                     ?? Request.Form["colorNombre"].FirstOrDefault()
                     ?? "").Trim();

                string skuPre =
                    (Request.Form["skuPre"].FirstOrDefault()
                     ?? Request.Form["sku"].FirstOrDefault()
                     ?? "").Trim();

                int stockPre = 0;
                int.TryParse((Request.Form["stockPre"].FirstOrDefault()
                              ?? Request.Form["stock"].FirstOrDefault()), out stockPre);
                if (stockPre < 0) stockPre = 0;

                // 3) Si se escribió color en texto libre → obtener/crear
                int? colorId = null;
                if (!string.IsNullOrWhiteSpace(colorTexto))
                {
                    colorId = await _db.EscalarAsync<int>("gd_sp_Color_ObtenerOCrear", cmd =>
                    {
                        cmd.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 80) { Value = colorTexto });
                    });
                }

                // 4) Crear variantes (una por talla) con SKU único
                var creadas = 0;
                if (tallasMarcadas.Length > 0)
                {
                    foreach (var tallaId in tallasMarcadas)
                    {
                        // Si teclearon un SKU base, hazlo único por talla (y color si aplica).
                        // Si lo dejan vacío, el SP generará uno automáticamente.
                        var sku = string.IsNullOrWhiteSpace(skuPre)
                            ? null
                            : $"{skuPre}-T{tallaId:00}" + (colorId.HasValue ? $"-C{colorId.Value:00}" : "");

                        await _db.EjecutarAsync("gd_sp_Variante_Guardar", cmd =>
                        {
                            cmd.Parameters.AddWithValue("@ProductoID", id);
                            cmd.Parameters.AddWithValue("@SKU", (object?)sku ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TallaID", tallaId);
                            cmd.Parameters.AddWithValue("@ColorID", (object?)colorId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Stock", stockPre);
                            cmd.Parameters.AddWithValue("@Activo", vm.Activo);
                        });

                        creadas++;
                    }

                    // 5) Si creaste al menos una talla, elimina el placeholder "ÚNICA"
                    if (creadas > 0)
                    {
                        await _db.EjecutarAsync("__raw_sql__", cmd =>
                        {
                            cmd.CommandType = CommandType.Text;   // << importante
                            cmd.CommandText = @"
                                DELETE FROM dbo.gd_ProductoVariante
                                WHERE ProductoID = @p
                                  AND TallaID IS NULL
                                  AND ColorID IS NULL;";
                            cmd.Parameters.Add(new SqlParameter("@p", SqlDbType.Int) { Value = id });
                        });
                    }

                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "El producto se guardó, pero no se pudieron crear las variantes iniciales: " + ex.Message;
            }
            // ========== FIN Variantes iniciales ==========



            TempData["ok"] = "Producto guardado correctamente.";
            return RedirectToAction(nameof(Editar), new { id });
        }

        // ========== FIN BLOQUE ==========


        // ================= Eliminar producto =================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var pOut = new SqlParameter("@Resultado", SqlDbType.Int)
                { Direction = ParameterDirection.Output };

                await _db.EjecutarAsync("gd_sp_Producto_Eliminar", cmd =>
                {
                    cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = id });
                    cmd.Parameters.Add(pOut);
                });

                var result = (pOut.Value is int v) ? v : 0;

                TempData["ok"] = result switch
                {
                    1 => "Producto eliminado correctamente.",
                    2 => "El producto tiene ventas asociadas. Se inactivó junto con sus variantes.",
                    _ => "No se encontró el producto o no se eliminó."
                };
            }
            catch (SqlException ex)
            {
                TempData["Error"] = "No se pudo eliminar: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // ================= Variantes =================

        [HttpGet]
        public async Task<IActionResult> Variantes(int id)
        {
            var variantes = await _db.ConsultarAsync(
                "gd_sp_Variantes_PorProducto",
                dr => new ProductVariantVM
                {
                    VarianteID = dr.GetInt32(dr.GetOrdinal("VarianteID")),
                    SKU = dr.GetString(dr.GetOrdinal("SKU")),
                    Stock = dr.IsDBNull(dr.GetOrdinal("Stock")) ? 0 : dr.GetInt32(dr.GetOrdinal("Stock")),
                    Talla = dr.IsDBNull(dr.GetOrdinal("Talla")) ? null : dr.GetString(dr.GetOrdinal("Talla")),
                    Color = dr.IsDBNull(dr.GetOrdinal("Color")) ? null : dr.GetString(dr.GetOrdinal("Color"))
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = id })
            );

            ViewBag.ProductoID = id;
            return PartialView("_VariantesTable", variantes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarVariante(int productoId, string? sku, int? tallaId, int? colorId, int stock, bool activo)
        {
            if (string.IsNullOrWhiteSpace(sku))
            {
                var t = tallaId.GetValueOrDefault(0);
                var c = colorId.GetValueOrDefault(0);
                sku = $"P{productoId}-T{t.ToString().PadLeft(2, '0')}-C{c.ToString().PadLeft(2, '0')}";
            }

            // ===== llamada con OUTPUT =====
            var outId = new SqlParameter("@VarianteID", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _db.EjecutarAsync("gd_sp_Variante_Guardar", cmd =>
            {
                cmd.Parameters.AddWithValue("@ProductoID", productoId);
                cmd.Parameters.AddWithValue("@SKU", (object?)sku ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TallaID", (object?)tallaId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ColorID", (object?)colorId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Stock", stock);
                cmd.Parameters.AddWithValue("@Activo", activo);
                cmd.Parameters.Add(outId);
            });

            // int varianteId = (outId.Value is int v) ? v : 0;

            TempData["ok"] = "Variante guardada.";
            return RedirectToAction(nameof(Editar), new { id = productoId });
        }

        private async Task<SelectList> CargarTallasAsync()
        {
            var tallas = await _db.ConsultarAsync("gd_sp_Talla_Listar",
                dr => new { TallaID = dr.GetInt32(0), Nombre = dr.GetString(1) });
            return new SelectList(tallas, "TallaID", "Nombre");
        }

        private async Task<SelectList> CargarColoresAsync()
        {
            var colores = await _db.ConsultarAsync("gd_sp_Color_Listar",
                dr => new { ColorID = dr.GetInt32(0), Nombre = dr.GetString(1) });
            return new SelectList(colores, "ColorID", "Nombre");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarVariante(int productoId, int varianteId)
        {
            await _db.EjecutarAsync("gd_sp_Variante_Eliminar", cmd =>
                cmd.Parameters.AddWithValue("@VarianteID", varianteId)
            );

            return RedirectToAction(nameof(Editar), new { id = productoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarVariantes(
            int productoId,
            [FromForm(Name = "stock")] int stockInicial = 0,
            [FromForm(Name = "activo")] bool activo = true)
        {
            var tallas = Request.Form["tallas"]
                .Select(v => { int.TryParse(v, out var x); return x; })
                .Where(x => x > 0)
                .Distinct()
                .ToArray();

            var colores = Request.Form["colores"]
                .Select(v => { int.TryParse(v, out var x); return x; })
                .Where(x => x > 0)
                .Distinct()
                .ToArray();

            if (colores.Length == 0) colores = new[] { 0 };

            if (tallas.Length == 0)
            {
                TempData["Error"] = "Debes seleccionar al menos una talla.";
                return RedirectToAction(nameof(Editar), new { id = productoId });
            }

            foreach (var t in tallas)
                foreach (var c in colores)
                {
                    int? tallaId = t == 0 ? (int?)null : t;
                    int? colorId = c == 0 ? (int?)null : c;
                    var sku = $"P{productoId}-T{(tallaId ?? 0):00}-C{(colorId ?? 0):00}";

                    // ===== llamada con OUTPUT =====
                    var outId = new SqlParameter("@VarianteID", SqlDbType.Int) { Direction = ParameterDirection.Output };

                    await _db.EjecutarAsync("gd_sp_Variante_Guardar", cmd =>
                    {
                        cmd.Parameters.AddWithValue("@ProductoID", productoId);
                        cmd.Parameters.AddWithValue("@SKU", sku);
                        cmd.Parameters.AddWithValue("@TallaID", (object?)tallaId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ColorID", (object?)colorId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Stock", stockInicial);
                        cmd.Parameters.AddWithValue("@Activo", activo);
                        cmd.Parameters.Add(outId);
                    });

                    // int varianteId = (outId.Value is int v) ? v : 0;
                }

            TempData["ok"] = "Variantes generadas correctamente.";
            return RedirectToAction(nameof(Editar), new { id = productoId });
        }
    }
}
