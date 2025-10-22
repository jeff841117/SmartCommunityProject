using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using WebApplication6.Models;
using 系統端.Models;

namespace WebApplication3.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

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
        //public IActionResult Create(Package package)
        //{
        //    try
        //    {
        //        DBmanager db = new DBmanager();

        //        // 假設 userLineId 從登入資訊或前端送入
        //        string userLineId = package.UserLineId;

        //        db.InsertPackage(package, userLineId);

        //        return RedirectToAction("Index");
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
