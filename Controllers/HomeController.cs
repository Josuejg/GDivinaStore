using GraciaDivina;
using GraciaDivina.Models;
using GraciaDivina.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Data;


namespace GraciaDivina.Controllers
{
    public class HomeController : Controller
    {
        private readonly AccesoDatos _db;
        public HomeController(AccesoDatos db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var productos = await _db.ConsultarAsync("gd_sp_Producto_ListarHome",
                dr => new ProductCardVM
                {
                    ProductoID = dr.GetInt32(0),
                    Nombre = dr.GetString(1),
                    Precio = dr.GetDecimal(2),
                    ImagenUrl = dr.IsDBNull(3) ? null : dr.GetString(3),
                    EnStock = true
                },
                cmd => cmd.Parameters.Add(new SqlParameter("@Take", SqlDbType.Int) { Value = 8 })
            );

            return View(productos);
        }
    }
}

