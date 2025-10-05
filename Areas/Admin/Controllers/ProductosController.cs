using GraciaDivina.Models;
using GraciaDivina.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace GraciaDivina.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ProductosController : Controller
    {
        private readonly AccesoDatos _db;
        private readonly IWebHostEnvironment _env;
        public ProductosController(AccesoDatos db, IWebHostEnvironment env)
        { _db = db; _env = env; }

        // util: cargar categorías para el combo
        private async Task CargarCategorias(int? selected = null)
        {
            var cats = await _db.ConsultarAsync("gd_sp_Categoria_Menu",
                dr => new Categoria { CategoriaID = dr.GetInt32(0), Nombre = dr.GetString(1) });
            ViewBag.Categorias = new SelectList(cats, "CategoriaID", "Nombre", selected);
        }

        public async Task<IActionResult> Index()
        {
            var items = await _db.ConsultarAsync("gd_sp_Producto_ListarAdmin",
                dr => new AdminProductVM
                {
                    ProductoID = dr.GetInt32(0),
                    Nombre = dr.GetString(1),
                    Precio = dr.GetDecimal(2),
                    Activo = dr.GetBoolean(3),
                    ImagenUrl = dr.IsDBNull(5) ? null : dr.GetString(5),
                    CategoriaID = dr.IsDBNull(6) ? null : dr.GetInt32(6),
                    Categoria = dr.IsDBNull(7) ? null : dr.GetString(7)
                });
            return View(items);
        }

        public async Task<IActionResult> Create()
        {
            await CargarCategorias();
            return View(new AdminProductVM());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminProductVM model)
        {
            if (!ModelState.IsValid)
            {
                await CargarCategorias(model.CategoriaID);
                return View(model);
            }

            // guardar imagen si viene
            if (model.Imagen != null && model.Imagen.Length > 0)
            {
                var rel = await GuardarImagen(model.Imagen);
                model.ImagenUrl = rel;
            }

            // ⚠ EscalarAsync devuelve object -> conviértelo a int
            var objId = await _db.EscalarAsync("gd_sp_Producto_Guardar", cmd =>
            {
                cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 120) { Value = model.Nombre });
                cmd.Parameters.Add(new SqlParameter("@Descripcion", SqlDbType.NVarChar, 800) { Value = (object?)model.Descripcion ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Precio", SqlDbType.Decimal) { Precision = 12, Scale = 2, Value = model.Precio });
                cmd.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int) { Value = (object?)model.CategoriaID ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@ImagenUrl", SqlDbType.NVarChar, 300) { Value = (object?)model.ImagenUrl ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Activo", SqlDbType.Bit) { Value = model.Activo });
            });

            // aquí puede venir decimal/object -> conviértelo seguro
            int nuevoId = Convert.ToInt32(objId);

            TempData["Msg"] = "Producto creado.";
            return RedirectToAction(nameof(Edit), new { id = nuevoId });

        }

        public async Task<IActionResult> Edit(int id)
        {
            // 1) Trae el producto
            var item = await _db.ConsultarUnoAsync("gd_sp_Producto_Obtener",
                dr => new AdminProductVM
                {
                    ProductoID = dr.GetInt32(0),
                    Nombre = dr.GetString(1),
                    Descripcion = dr.IsDBNull(2) ? null : dr.GetString(2),
                    Precio = dr.GetDecimal(3),
                    ImagenUrl = dr.IsDBNull(4) ? null : dr.GetString(4),
                    CategoriaID = dr.IsDBNull(5) ? null : dr.GetInt32(5),
                    Activo = dr.GetBoolean(6)
                },
                cmd => cmd.Parameters.AddWithValue("@ProductoID", id));

            if (item == null) return NotFound();

            // 2) ⬅️ NUEVO: carga la galería de imágenes del producto
            var fotos = await _db.ConsultarAsync("gd_sp_ProductoImagen_Listar",
                dr => new ProductoImagen
                {
                    ImagenID = dr.GetInt32(0),
                    ProductoID = dr.GetInt32(1),
                    UrlImagen = dr.GetString(2),
                    EsPrincipal = dr.GetBoolean(3)
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = id })
            );
            ViewBag.Imagenes = fotos; // la vista las recibe por ViewBag

            // 3) combo de categorías y render
            await CargarCategorias(item.CategoriaID);
            return View(item);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImages(int id, List<IFormFile> imagenes)
        {
            if (imagenes != null)
            {
                foreach (var file in imagenes.Where(f => f?.Length > 0))
                {
                    var rel = await GuardarImagen(file); // usa tu helper existente
                    await _db.EjecutarAsync("gd_sp_ProductoImagen_Agregar", cmd =>
                    {
                        cmd.Parameters.AddWithValue("@ProductoID", id);
                        cmd.Parameters.AddWithValue("@UrlImagen", rel);
                        cmd.Parameters.AddWithValue("@EsPrincipal", 0);
                    });
                }
            }
            TempData["Msg"] = "Imágenes cargadas.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrincipal(int id, int imagenId)
        {
            await _db.EjecutarAsync("gd_sp_ProductoImagen_MarcarPrincipal", cmd =>
            {
                cmd.Parameters.AddWithValue("@ProductoID", id);
                cmd.Parameters.AddWithValue("@ImagenID", imagenId);
            });
            TempData["Msg"] = "Imagen principal actualizada.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int imagenId)
        {
            await _db.EjecutarAsync("gd_sp_ProductoImagen_Eliminar", cmd =>
            {
                cmd.Parameters.AddWithValue("@ProductoID", id);
                cmd.Parameters.AddWithValue("@ImagenID", imagenId);
            });
            TempData["Msg"] = "Imagen eliminada.";
            return RedirectToAction(nameof(Edit), new { id });
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminProductVM model)
        {
            if (id != model.ProductoID) return BadRequest();

            if (!ModelState.IsValid)
            {
                await CargarCategorias(model.CategoriaID);
                return View(model);
            }

            // si suben nueva imagen, reemplaza
            if (model.Imagen != null && model.Imagen.Length > 0)
            {
                var rel = await GuardarImagen(model.Imagen);
                model.ImagenUrl = rel;
            }

            await _db.EjecutarAsync("gd_sp_Producto_Guardar", cmd =>
            {
                cmd.Parameters.AddWithValue("@ProductoID", model.ProductoID);
                cmd.Parameters.AddWithValue("@Nombre", model.Nombre);
                cmd.Parameters.AddWithValue("@Descripcion", (object?)model.Descripcion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Precio", model.Precio);
                cmd.Parameters.AddWithValue("@CategoriaID", (object?)model.CategoriaID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ImagenUrl", (object?)model.ImagenUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Activo", model.Activo);
            });

            TempData["Msg"] = "Producto actualizado.";
            return RedirectToAction(nameof(Edit), new { id = model.ProductoID });
        }

        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.ConsultarUnoAsync("gd_sp_Producto_Obtener",
                dr => new AdminProductVM
                {
                    ProductoID = dr.GetInt32(0),
                    Nombre = dr.GetString(1),
                    Precio = dr.GetDecimal(3),
                    ImagenUrl = dr.IsDBNull(4) ? null : dr.GetString(4)
                },
                cmd => cmd.Parameters.AddWithValue("@ProductoID", id));

            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _db.EjecutarAsync("gd_sp_Producto_Eliminar",
                cmd => cmd.Parameters.AddWithValue("@ProductoID", id));
            TempData["Msg"] = "Producto desactivado.";
            return RedirectToAction(nameof(Index));
        }

        // ------------ helpers ------------
        private async Task<string> GuardarImagen(IFormFile file)
        {
            // validaciones básicas
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var ok = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!ok.Contains(ext)) throw new InvalidOperationException("Formato de imagen no permitido.");
            if (file.Length > 3 * 1024 * 1024) throw new InvalidOperationException("Imagen > 3MB.");

            var folder = Path.Combine(_env.WebRootPath, "img", "products");
            Directory.CreateDirectory(folder);

            var name = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(folder, name);

            using (var fs = new FileStream(path, FileMode.Create))
            { await file.CopyToAsync(fs); }

            // ruta relativa para la web
            return $"/img/products/{name}";
        }
    }
}
