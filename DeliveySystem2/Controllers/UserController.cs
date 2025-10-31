using DeliveySystem2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DeliveySystem2.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Delivery()
        {
            DBmanager db = new DBmanager();
            var vm = new DeliveryViewModel();
            vm.DetailList = db.getDetails();
            return View(vm);
        }


        public IActionResult Search(string searchKeyword)
        {
            DBmanager db = new DBmanager();

            // 查詢 PID，STA = false
            var packages = db.GetPendingPackagesByPID(searchKeyword);

            var model = new SearchViewModel
            {
                SearchKeyword = searchKeyword,
                Packages = packages
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult GoToSearch()
        {
            return RedirectToAction("Search", "User");
        }
        [HttpPost]
        public IActionResult Delivery(DeliveryViewModel model)
        {
            Console.WriteLine("==== 進入 Delivery POST ====");
            Console.WriteLine($"SenderName = {model.SenderName}");
            Console.WriteLine($"SendDateSrting = {model.SendDateString}");
            DBmanager db = new DBmanager();
            if (!DateTime.TryParse(model.SendDateString, out DateTime sendDate))
            {
                ModelState.AddModelError("SendDateString", "日期格式錯誤或不可為空白");
                model.DetailList = new DBmanager().getDetails();
                return View(model);
            }

            model.SendDate = sendDate;
            Console.WriteLine($"SendDate = {model.SendDate}");

            if (model.SendDate < DateTime.Today)
            {
                ModelState.AddModelError("SendDateString", "送達日期不可早於今天");
                model.DetailList = new DBmanager().getDetails();
                return View(model);
            }
            if (string.IsNullOrEmpty(model.SenderName))
            {
                ModelState.AddModelError("SenderName", "寄件者姓名不可空白");

                model.DetailList = db.getDetails();

                return View(model);
            }
            if (string.IsNullOrEmpty(model.SenderPhn))
            {
                ModelState.AddModelError("SenderPhn", "寄件者電話不可空白");

                model.DetailList = db.getDetails();

                return View(model);
            }
            if (string.IsNullOrEmpty(model.SenderAddress))
            {
                ModelState.AddModelError("SenderAddress", "寄件者地址不可空白");

                model.DetailList = db.getDetails();

                return View(model);
            }
            if (string.IsNullOrEmpty(model.RecipientName))
            {
                ModelState.AddModelError("RecipientName", "收件者姓名不可空白");

                model.DetailList = db.getDetails();

                return View(model);
            }
            if (string.IsNullOrEmpty(model.RecipientPhn))
            {
                ModelState.AddModelError("RecipientPhn", "收件者電話不可空白");

                model.DetailList = db.getDetails();

                return View(model);
            }
            if (string.IsNullOrEmpty(model.RecipientAddress))
            {
                ModelState.AddModelError("RecipientAddress", "收件者地址不可空白");

                model.DetailList = db.getDetails();

                return View(model);
            }

            try
            {
                db.InsertDelivery(
                    model.SenderName,
                    model.SenderPhn,
                    model.SenderAddress,
                    model.RecipientName,
                    model.RecipientPhn,
                    model.RecipientAddress,
                    model.SendDate
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }


            // 重新載入最新資料
            model.DetailList = db.getDetails();

            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
