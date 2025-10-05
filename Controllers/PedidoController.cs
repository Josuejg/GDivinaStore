using GraciaDivina.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace GraciaDivina.Controllers
{
    public class PedidoController : Controller
    {
        private readonly AccesoDatos _db;
        public PedidoController(AccesoDatos db) => _db = db;

        public async Task<IActionResult> Resumen(int id)
        {
            // Encabezado
            var header = await _db.ConsultarUnoAsync("gd_sp_Pedido_Obtener",
                dr => new {
                    PedidoID = dr.GetInt32(0),
                    Fecha = dr.GetDateTime(1),
                    Estado = dr.GetString(2),
                    Total = dr.GetDecimal(3),
                    Nombre = dr.GetString(4),
                    Telefono = dr.GetString(5),
                    Direccion = dr.IsDBNull(6) ? null : dr.GetString(6),
                    Email = dr.IsDBNull(7) ? null : dr.GetString(7)
                },
                cmd => cmd.Parameters.AddWithValue("@PedidoID", id));

            if (header == null) return NotFound();

            // Detalle
            var detalles = await _db.ConsultarAsync("gd_sp_Pedido_Obtener",
                dr => new {
                    DetalleID = dr.GetInt64(0),
                    VarianteID = dr.GetInt32(1),
                    Cantidad = dr.GetInt32(2),
                    PrecioUnitario = dr.GetDecimal(3),
                    Subtotal = dr.GetDecimal(4),
                    Producto = dr.GetString(5),
                    Talla = dr.IsDBNull(6) ? null : dr.GetString(6),
                    Color = dr.IsDBNull(7) ? null : dr.GetString(7),
                    SKU = dr.GetString(8)
                },
                cmd => cmd.Parameters.AddWithValue("@PedidoID", id));

            ViewData["Header"] = header;
            return View(detalles);
        }
    }
}
