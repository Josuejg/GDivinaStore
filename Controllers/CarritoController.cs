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

        // Obtiene o crea el GUID del carrito en cookie
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
                cmd => cmd.Parameters.Add(new SqlParameter("@CarritoID", SqlDbType.UniqueIdentifier) { Value = carritoId }));

            return new CartVM { Items = items };
        }

        // GET /Carrito
        public async Task<IActionResult> Index()
        {
            var id = GetOrCreateCarritoId();
            var vm = await CargarCarrito(id);
            return View(vm);
        }

        // POST /Carrito/Agregar  (desde Producto/Details)
        [HttpPost]
        public async Task<IActionResult> Agregar(int varianteId, int cantidad = 1, string? returnUrl = null)
        {
            var id = GetOrCreateCarritoId();
            await _db.EjecutarAsync("gd_sp_Carrito_AgregarItem", cmd =>
            {
                cmd.Parameters.Add(new SqlParameter("@CarritoID", SqlDbType.UniqueIdentifier) { Value = id });
                cmd.Parameters.Add(new SqlParameter("@VarianteID", SqlDbType.Int) { Value = varianteId });
                cmd.Parameters.Add(new SqlParameter("@Cantidad", SqlDbType.Int) { Value = Math.Max(1, cantidad) });
            });

            if (!string.IsNullOrWhiteSpace(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        // POST /Carrito/Actualizar
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

        // POST /Carrito/Quitar
        [HttpPost]
        public async Task<IActionResult> Quitar(long itemId)
        {
            await _db.EjecutarAsync("gd_sp_Carrito_QuitarItem",
                cmd => cmd.Parameters.AddWithValue("@CarritoItemID", itemId));
            return RedirectToAction(nameof(Index));
        }

        // GET /Carrito/Confirmar  (captura datos)
        public async Task<IActionResult> Confirmar()
        {
            var id = GetOrCreateCarritoId();
            var vm = new CheckoutVM { Carrito = await CargarCarrito(id) };
            if (!vm.Carrito.Items.Any()) return RedirectToAction(nameof(Index));
            return View(vm);
        }

        // POST /Carrito/Confirmar  (crea pedido y vacía carrito)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(CheckoutVM model)
        {
            // 1) Normaliza teléfono a solo dígitos para que cumpla el [RegularExpression] del VM
            model.Telefono = new string(model.Telefono?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());

            // 2) Revalida con DataAnnotations después de normalizar
            ModelState.Clear();
            TryValidateModel(model);
            if (!ModelState.IsValid)
                return View(model);

            var id = GetOrCreateCarritoId();

            try
            {
                // 3) Ejecuta el SP que crea el pedido desde el carrito
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

                model.PedidoID = (int)pId.Value;

                // 4) Limpia la cookie del carrito (pedido creado)
                Response.Cookies.Delete(CART_COOKIE);

                // 5) Redirige a resumen del pedido
                return RedirectToAction("Resumen", "Pedido", new { id = model.PedidoID });
            }
            catch (Exception ex)
            {
                // Mensaje simple para el usuario; loggea 'ex' internamente si tienes logger
                ModelState.AddModelError(string.Empty, "No fue posible crear el pedido. Intenta de nuevo.");
                return View(model);
            }
        }

    }
}
