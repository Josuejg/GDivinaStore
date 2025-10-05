using GraciaDivina.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace GraciaDivina.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize] // <— PROTEGE ESTE CONTROLADOR
    public class CategoriasController : Controller
    {
        private readonly AccesoDatos _db;
        public CategoriasController(AccesoDatos db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var items = await _db.ConsultarAsync("gd_sp_Categoria_ListarAdmin",
                dr => new Categoria
                {
                    CategoriaID = dr.GetInt32(0),
                    Nombre = dr.GetString(1),
                    Activo = dr.GetBoolean(2),
                    FechaCreacion = dr.GetDateTime(3)
                });
            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var item = await _db.ConsultarUnoAsync("gd_sp_Categoria_Obtener",
                dr => new Categoria
                {
                    CategoriaID = dr.GetInt32(0),
                    Nombre = dr.GetString(1),
                    Activo = dr.GetBoolean(2),
                    FechaCreacion = dr.GetDateTime(3)
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int) { Value = id }));
            if (item == null) return NotFound();
            return View(item);
        }

        public IActionResult Create() => View(new Categoria());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoria model)
        {
            if (!ModelState.IsValid) return View(model);
            await _db.EscalarAsync("gd_sp_Categoria_Guardar", cmd =>
            {
                cmd.Parameters.AddWithValue("@CategoriaID", DBNull.Value);
                cmd.Parameters.AddWithValue("@Nombre", model.Nombre);
                cmd.Parameters.AddWithValue("@Activo", model.Activo);
            });
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.ConsultarUnoAsync("gd_sp_Categoria_Obtener",
                dr => new Categoria
                {
                    CategoriaID = dr.GetInt32(0),
                    Nombre = dr.GetString(1),
                    Activo = dr.GetBoolean(2),
                    FechaCreacion = dr.GetDateTime(3)
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int) { Value = id }));
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Categoria model)
        {
            if (id != model.CategoriaID) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            await _db.EscalarAsync("gd_sp_Categoria_Guardar", cmd =>
            {
                cmd.Parameters.AddWithValue("@CategoriaID", model.CategoriaID);
                cmd.Parameters.AddWithValue("@Nombre", model.Nombre);
                cmd.Parameters.AddWithValue("@Activo", model.Activo);
            });

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.ConsultarUnoAsync("gd_sp_Categoria_Obtener",
                dr => new Categoria
                {
                    CategoriaID = dr.GetInt32(0),
                    Nombre = dr.GetString(1),
                    Activo = dr.GetBoolean(2),
                    FechaCreacion = dr.GetDateTime(3)
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int) { Value = id }));
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _db.EjecutarAsync("gd_sp_Categoria_Eliminar",
                cmd => cmd.Parameters.AddWithValue("@CategoriaID", id));
            return RedirectToAction(nameof(Index));
        }
    }
}
