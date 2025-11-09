using GraciaDivina.Models;
using GraciaDivina.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;

namespace GraciaDivina.Controllers
{
    public class CarritoController : Controller
    {
        private const string CART_COOKIE = "GD_CART";
        private readonly AccesoDatos _db;
        public CarritoController(AccesoDatos db) => _db = db;

        private Guid GetOrCreateCarritoId()
        {
            if (Request.Cookies.TryGetValue(CART_COOKIE, out var v) && Guid.TryParse(v, out var g))
                return g;
            var nuevo = Guid.NewGuid();
            Response.Cookies.Append(CART_COOKIE, nuevo.ToString(), new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
            return nuevo;
        }

        private async Task<CartVM> CargarCarrito(Guid carritoId)
        {
            var items = await _db.ConsultarAsync("gd_sp_Carrito_Obtener",
                dr => new CartItemVM
                {
                    CarritoItemID = dr.GetInt64(0),
                    VarianteID = dr.GetInt32(1),
                    Cantidad = dr.GetInt32(2),
                    Precio = dr.GetDecimal(3),
                    Subtotal = dr.GetDecimal(4),
                    ProductoID = dr.GetInt32(5),
                    Producto = dr.GetString(6),
                    SKU = dr.GetString(7),
                    Talla = dr.IsDBNull(8) ? null : dr.GetString(8),
                    Color = dr.IsDBNull(9) ? null : dr.GetString(9),
                    ImagenUrl = dr.IsDBNull(10) ? null : dr.GetString(10)
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@CarritoID", SqlDbType.UniqueIdentifier) { Value = carritoId })
            );
            return new CartVM { Items = items };
        }

        // GET /Carrito
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var id = GetOrCreateCarritoId();
            var vm = await CargarCarrito(id);
            return View(vm);
        }

    
        // POST /Carrito/Agregar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(int? varianteId, int? productoId, int cantidad = 1, string? returnUrl = null)
        {
            var id = GetOrCreateCarritoId();
            var cant = Math.Max(1, cantidad);

            if (varianteId.HasValue)
            {
                // Agrega por VarianteID (flujo con talla/color)
                await _db.EjecutarAsync("gd_sp_Carrito_AgregarItem", cmd =>
                {
                    cmd.Parameters.Add(new SqlParameter("@CarritoID", SqlDbType.UniqueIdentifier) { Value = id });
                    cmd.Parameters.Add(new SqlParameter("@VarianteID", SqlDbType.Int) { Value = varianteId.Value });
                    cmd.Parameters.Add(new SqlParameter("@Cantidad", SqlDbType.Int) { Value = cant });
                });
            }
            else if (productoId.HasValue)
            {
                // Si el producto tiene variantes, exigir varianteId
                // (La vista ya lo impide, pero protegemos el POST directo)
                // Puedes descomentar el chequeo si quieres reforzar:
                // var tieneVariantes = await _db.EscalarAsync<int>("gd_sp_Producto_TieneVariantes", cmd =>
                //     cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = productoId.Value }));
                // if (tieneVariantes == 1) { TempData["Error"] = "Elige una combinación disponible."; return RedirectToAction("Details","Producto", new { id = productoId.Value }); }

                // Por compatibilidad: permitir agregar por producto cuando no hay variantes
                await _db.EjecutarAsync("gd_sp_Carrito_AgregarProducto", cmd =>

                {
                    cmd.Parameters.Add(new SqlParameter("@CarritoID", SqlDbType.UniqueIdentifier) { Value = id });
                    cmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = productoId.Value });
                    cmd.Parameters.Add(new SqlParameter("@Cantidad", SqlDbType.Int) { Value = cant });
                });
            }
            else
            {
                // No llegó ni varianteId ni productoId -> regresar al home o a donde estabas
                TempData["Error"] = "Elige una combinación disponible.";
                return !string.IsNullOrWhiteSpace(returnUrl)
                    ? Redirect(returnUrl)
                    : RedirectToAction("Index", "Catalogo");
            }

            // Redirección
            if (!string.IsNullOrWhiteSpace(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> Actualizar(long itemId, int cantidad)
        {
            await _db.EjecutarAsync("gd_sp_Carrito_ActualizarCantidad", cmd =>
            {
                cmd.Parameters.AddWithValue("@CarritoItemID", itemId);
                cmd.Parameters.AddWithValue("@Cantidad", Math.Max(1, cantidad));
            });
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Quitar(long itemId)
        {
            await _db.EjecutarAsync("gd_sp_Carrito_QuitarItem",
                cmd => cmd.Parameters.AddWithValue("@CarritoItemID", itemId));
            return RedirectToAction(nameof(Index));
        }

        // Checkout (datos cliente + crea pedido)
        [HttpGet]
        public async Task<IActionResult> Confirmar()
        {
            var id = GetOrCreateCarritoId();
            var vm = new CheckoutVM { Carrito = await CargarCarrito(id) };
            if (!vm.Carrito.Items.Any()) return RedirectToAction(nameof(Index));
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(CheckoutVM model)
        {
            model.Telefono = new string((model.Telefono ?? "").Where(char.IsDigit).ToArray());
            if (!Regex.IsMatch(model.Telefono, @"^\d{8,15}$"))
                ModelState.AddModelError(nameof(model.Telefono), "El teléfono debe contener solo números (8 a 15 dígitos).");
            if (!ModelState.IsValid) return View(model);

            var id = GetOrCreateCarritoId();
            var pId = new SqlParameter("@PedidoID", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _db.EjecutarAsync("gd_sp_Pedido_CrearDesdeCarrito", cmd =>
            {
                cmd.Parameters.Add(new SqlParameter("@CarritoID", SqlDbType.UniqueIdentifier) { Value = id });
                cmd.Parameters.AddWithValue("@Nombre", model.Nombre);
                cmd.Parameters.AddWithValue("@Telefono", model.Telefono);
                cmd.Parameters.AddWithValue("@Direccion", (object?)model.Direccion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
                cmd.Parameters.Add(pId);
            });

            Response.Cookies.Delete(CART_COOKIE);
            return RedirectToAction("Resumen", "Pedido", new { id = (int)pId.Value });
        }
    }
}
