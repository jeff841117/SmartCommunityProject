using DeliveySystem2.Models;
using Microsoft.AspNetCore.Mvc;

namespace DeliveySystem2.Controllers
{
    public class SecurityController : Controller
    {
        public IActionResult Index()
        {
            DBmanager dbmanager = new DBmanager();
            List<Package> packages = dbmanager.getPackages();

            var model = new PackageViewModel
            {
                Packages = packages
            };
            return View(model);
        }
        [HttpPost]

        public IActionResult Index(Package user)
        {

            DBmanager dbmanager = new DBmanager();
            try
            {
                user.PID = Guid.NewGuid().ToString().Substring(0, 8);
                user.STA = false;
                dbmanager.newPackage(user);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return RedirectToAction("Index");
        }

        public IActionResult UpdateStatusBatch([FromBody] List<PackageUpdateModel> updates)
        {
            try
            {
                DBmanager db = new DBmanager();

                foreach (var item in updates)
                {
                    db.UpdatePackageStatus(item.Id, item.Status);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        public class PackageUpdateModel
        {
            public int Id { get; set; }
            public bool Status { get; set; }
        }
    }
}
