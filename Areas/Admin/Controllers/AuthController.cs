using GraciaDivina.Models;
using GraciaDivina.Models.Auth;
using GraciaDivina.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;



namespace GraciaDivina.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AuthController : Controller
    {
        private readonly AccesoDatos _db;
        public AuthController(AccesoDatos db) => _db = db;
#if DEBUG
        [HttpGet]
        public async Task<IActionResult> SeedAdmin(string u = "admin", string pwd = "Admin123!")
        {
            var hash = GraciaDivina.Security.PasswordHasher.Hash(pwd);
            await _db.EjecutarAsync("gd_sp_AdminUsuario_ActualizarPassword", cmd =>
            {
                cmd.Parameters.AddWithValue("@Usuario", u);
                cmd.Parameters.AddWithValue("@ContrasenaHash", hash);
            });
            return Content($"OK. Usuario={u} actualizado.\nHash={hash}");
        }
#endif


        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
            => View(new LoginVM { ReturnUrl = returnUrl });
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new GraciaDivina.Models.Auth.ChangePasswordVM());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(GraciaDivina.Models.Auth.ChangePasswordVM model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1) Obtener usuario actual (lo pusimos en el claim Name)
            var userName = User?.Identity?.Name ?? string.Empty;

            var dbUser = await _db.ConsultarUnoAsync("gd_sp_AdminUsuario_ObtenerPorUsuario",
                dr => new
                {
                    UsuarioID = dr.GetInt32(0),
                    Usuario = dr.GetString(1),
                    Hash = dr.GetString(2),
                    Activo = dr.GetBoolean(3),
                    Email = dr.IsDBNull(4) ? null : dr.GetString(4)
                },
                cmd => cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@Usuario", System.Data.SqlDbType.NVarChar, 60)
                { Value = userName }));

            if (dbUser == null || !dbUser.Activo)
            {
                ModelState.AddModelError("", "Usuario inválido o inactivo.");
                return View(model);
            }

            // 2) Verificar contraseña actual
            if (!GraciaDivina.Security.PasswordHasher.Verify(model.ContrasenaActual, dbUser.Hash))
            {
                ModelState.AddModelError(nameof(model.ContrasenaActual), "Contraseña actual incorrecta.");
                return View(model);
            }

            // 3) Evitar que la nueva sea igual a la actual
            if (GraciaDivina.Security.PasswordHasher.Verify(model.NuevaContrasena, dbUser.Hash))
            {
                ModelState.AddModelError(nameof(model.NuevaContrasena), "La nueva contraseña no puede ser igual a la actual.");
                return View(model);
            }

            // 4) Generar nuevo hash y guardar
            var newHash = GraciaDivina.Security.PasswordHasher.Hash(model.NuevaContrasena);
            await _db.EjecutarAsync("gd_sp_AdminUsuario_ActualizarPassword", cmd =>
            {
                cmd.Parameters.AddWithValue("@Usuario", dbUser.Usuario);
                cmd.Parameters.AddWithValue("@ContrasenaHash", newHash);
            });

            TempData["Msg"] = "Contraseña actualizada correctamente.";
            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            model.Usuario = (model.Usuario ?? string.Empty).Trim();
            if (!ModelState.IsValid) return View(model);

            // 1) Traer usuario
            var user = await _db.ConsultarUnoAsync("gd_sp_AdminUsuario_ObtenerPorUsuario",
                dr => new
                {
                    UsuarioID = dr.GetInt32(0),
                    Usuario = dr.GetString(1),
                    Hash = dr.GetString(2),
                    Activo = dr.GetBoolean(3),
                    Email = dr.IsDBNull(4) ? null : dr.GetString(4)
                },
                   cmd => cmd.Parameters.Add(new SqlParameter("@Usuario", SqlDbType.NVarChar, 60)
                   {
                       Value = model.Usuario  // ← ya viene Trim() (y ToLowerInvariant() si lo aplicaste)
                   }));

            if (user == null || !user.Activo || !PasswordHasher.Verify(model.Password, user.Hash))
            {
                model.Error = "Usuario o contraseña inválidos.";
                return View(model);
            }

            // 2) Crear identidad y cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UsuarioID.ToString()),
                new Claim(ClaimTypes.Name, user.Usuario),
                new Claim(ClaimTypes.Role, "Admin")
            };
            if (!string.IsNullOrEmpty(user.Email))
                claims.Add(new Claim(ClaimTypes.Email, user.Email));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // 3) Redirigir
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [HttpGet]
        public IActionResult Denied() => Content("Acceso denegado.");
    }
}
