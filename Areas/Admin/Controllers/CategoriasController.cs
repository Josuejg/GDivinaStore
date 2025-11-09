// RUTA: Areas/Admin/Controllers/CategoriasController.cs
using GraciaDivina.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace GraciaDivina.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize] // Si luego configuras roles: [Authorize(Roles = "Admin")]
    public class CategoriasController : Controller
    {
        private readonly AccesoDatos _db;
        public CategoriasController(AccesoDatos db) => _db = db;

        // LISTAR
        // RUTA: Areas/Admin/Controllers/CategoriasController.cs
        // ACCIÓN: Index (listar categorías) — CORREGIDA para mapear tipos/órdenes reales del SP
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var items = await _db.ConsultarAsync("gd_sp_Categoria_ListarAdmin", dr =>
            {
                var oId = dr.GetOrdinal("CategoriaID");
                var oNom = dr.GetOrdinal("Nombre");
                var oAct = dr.GetOrdinal("Activo");
                var oFecha = dr.GetOrdinal("FechaCreacion");

                return new Categoria
                {
                    CategoriaID = dr.GetInt32(oId),
                    Nombre = dr.IsDBNull(oNom) ? "" : dr.GetString(oNom),
                    Descripcion = null, // Este SP no devuelve Descripcion
                    Activo = !dr.IsDBNull(oAct) && dr.GetBoolean(oAct),
                    FechaCreacion = dr.IsDBNull(oFecha) ? DateTime.MinValue : dr.GetDateTime(oFecha)
                };
            });

            return View(items);
        }

        // DETALLE (opcional: si tienes vista Details)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _db.ConsultarUnoAsync("gd_sp_Categoria_Obtener",
                dr => new Categoria
                {
                    CategoriaID = dr.GetInt32(dr.GetOrdinal("CategoriaID")),
                    Nombre = dr.GetString(dr.GetOrdinal("Nombre")),
                    Descripcion = dr.ColumnExists("Descripcion") && !dr.IsDBNull(dr.GetOrdinal("Descripcion"))
                                    ? dr.GetString(dr.GetOrdinal("Descripcion")) : null,
                    Activo = dr.ColumnExists("Activo") && !dr.IsDBNull(dr.GetOrdinal("Activo"))
                                    ? dr.GetBoolean(dr.GetOrdinal("Activo")) : true,
                    FechaCreacion = dr.ColumnExists("FechaCreacion") && !dr.IsDBNull(dr.GetOrdinal("FechaCreacion"))
                                    ? dr.GetDateTime(dr.GetOrdinal("FechaCreacion")) : DateTime.MinValue
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int) { Value = id })
            );

            if (item == null) return NotFound();
            return View(item);
        }

        // CREAR (GET)
        [HttpGet]
        public IActionResult Create() => View(new Categoria { Activo = true });

        // CREAR (POST)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoria model)
        {
            if (!ModelState.IsValid) return View(model);

            // Upsert: @CategoriaID NULL => INSERT
            await _db.EscalarAsync("gd_sp_Categoria_Guardar", cmd =>
            {
                cmd.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int)
                { Direction = ParameterDirection.InputOutput, Value = DBNull.Value });

                cmd.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 100) { Value = model.Nombre });

                // Si tu SP ya soporta Descripcion, este parámetro funciona; si no, elimínalo.
                cmd.Parameters.Add(new SqlParameter("@Descripcion", SqlDbType.NVarChar, -1)
                { Value = (object?)model.Descripcion ?? DBNull.Value });

                cmd.Parameters.Add(new SqlParameter("@Activo", SqlDbType.Bit) { Value = model.Activo });
            });

            return RedirectToAction(nameof(Index));
        }

        // EDITAR (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.ConsultarUnoAsync("gd_sp_Categoria_Obtener",
                dr => new Categoria
                {
                    CategoriaID = dr.GetInt32(dr.GetOrdinal("CategoriaID")),
                    Nombre = dr.GetString(dr.GetOrdinal("Nombre")),
                    Descripcion = dr.ColumnExists("Descripcion") && !dr.IsDBNull(dr.GetOrdinal("Descripcion"))
                                    ? dr.GetString(dr.GetOrdinal("Descripcion")) : null,
                    Activo = dr.ColumnExists("Activo") && !dr.IsDBNull(dr.GetOrdinal("Activo"))
                                    ? dr.GetBoolean(dr.GetOrdinal("Activo")) : true,
                    FechaCreacion = dr.ColumnExists("FechaCreacion") && !dr.IsDBNull(dr.GetOrdinal("FechaCreacion"))
                                    ? dr.GetDateTime(dr.GetOrdinal("FechaCreacion")) : DateTime.MinValue
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int) { Value = id })
            );

            if (item == null) return NotFound();
            return View(item);
        }

        // EDITAR (POST)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Categoria model)
        {
            if (id != model.CategoriaID) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            await _db.EscalarAsync("gd_sp_Categoria_Guardar", cmd =>
            {
                cmd.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int)
                { Direction = ParameterDirection.InputOutput, Value = model.CategoriaID });

                cmd.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 100) { Value = model.Nombre });

                // Quita este parámetro si tu SP no lo acepta aún.
                cmd.Parameters.Add(new SqlParameter("@Descripcion", SqlDbType.NVarChar, -1)
                { Value = (object?)model.Descripcion ?? DBNull.Value });

                cmd.Parameters.Add(new SqlParameter("@Activo", SqlDbType.Bit) { Value = model.Activo });
            });

            return RedirectToAction(nameof(Index));
        }

        // BORRAR (GET) - confirmación
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.ConsultarUnoAsync("gd_sp_Categoria_Obtener",
                dr => new Categoria
                {
                    CategoriaID = dr.GetInt32(dr.GetOrdinal("CategoriaID")),
                    Nombre = dr.GetString(dr.GetOrdinal("Nombre")),
                    Descripcion = dr.ColumnExists("Descripcion") && !dr.IsDBNull(dr.GetOrdinal("Descripcion"))
                                    ? dr.GetString(dr.GetOrdinal("Descripcion")) : null,
                    Activo = dr.ColumnExists("Activo") && !dr.IsDBNull(dr.GetOrdinal("Activo"))
                                    ? dr.GetBoolean(dr.GetOrdinal("Activo")) : true,
                    FechaCreacion = dr.ColumnExists("FechaCreacion") && !dr.IsDBNull(dr.GetOrdinal("FechaCreacion"))
                                    ? dr.GetDateTime(dr.GetOrdinal("FechaCreacion")) : DateTime.MinValue
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int) { Value = id })
            );

            if (item == null) return NotFound();
            return View(item);
        }

        // BORRAR (POST)
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _db.EjecutarAsync("gd_sp_Categoria_Eliminar",
                cmd => cmd.Parameters.AddWithValue("@CategoriaID", id));
            return RedirectToAction(nameof(Index));
        }
    }

   
  
}
