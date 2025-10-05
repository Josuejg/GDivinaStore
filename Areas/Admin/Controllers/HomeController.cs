using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GraciaDivina.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize] // si quieres que pida login
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
