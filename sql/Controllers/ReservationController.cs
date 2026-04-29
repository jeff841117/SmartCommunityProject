using Microsoft.AspNetCore.Mvc;

namespace sql.Controllers
{
    public class ReservationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
