using CampusCore.Services;
using Microsoft.AspNetCore.Mvc;
namespace CampusCore.Controllers;

public class HomeController(IAcademicData data) : Controller
{
    [HttpGet] public IActionResult Index() => View(data);
    [IgnoreAntiforgeryToken] public IActionResult Error() { Response.StatusCode = 500; ViewData["Code"] = 500; return View("Status"); }
    [IgnoreAntiforgeryToken] public IActionResult Status(int code = 404) { code = code is >= 400 and <= 599 ? code : 404; Response.StatusCode = code; ViewData["Code"] = code; return View(); }
}
