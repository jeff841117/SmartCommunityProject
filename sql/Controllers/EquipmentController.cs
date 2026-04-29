using Microsoft.AspNetCore.Mvc;
using sql.Models;
using System.Diagnostics;

namespace sql.Controllers
{
    public class EquipmentController : Controller
    {
        private readonly ILogger<EquipmentController> _logger;
        private readonly DBmanager _dbManager;

        public EquipmentController(ILogger<EquipmentController> logger)
        {
            _logger = logger;
            _dbManager = new DBmanager();
        }
        public IActionResult Index()
        {
            // 檢查是否已登入
            var currentUser = GetCurrentUserId();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 檢查是否為管理者
            if (!IsCurrentUserManager())
            {
                // 如果不是管理者，跳轉到預約頁面
                TempData["Error"] = "您沒有管理員權限";
                return RedirectToAction("Reservation", "Equipment");
            }
            var equipments = _dbManager.getEquipment();
            ViewBag.equipments = equipments;
            return View();
        }

        // 輔助方法：檢查是否為管理者
        private bool IsCurrentUserManager()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole == "manager" || userRole == "admin";
        }

        public IActionResult addEquipment()
        {
            return View();
        }

        [HttpPost]
        public IActionResult addEquipment(Equipment user)
        {

            DBmanager dbmanager = new DBmanager();
            try
            {
                dbmanager.newEquipment(user);
            }
            catch (Exception e)
            {
                return RedirectToAction("Privacy");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult deleteEquipment(byte id)
        {
            DBmanager dbmanager = new DBmanager();
            try
            {
                dbmanager.deleteEquipment(id);
            }
            catch (Exception e)
            {
                return RedirectToAction("Privacy");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateEquipment(Equipment equipment)
        {
            DBmanager dbmanager = new DBmanager();
            try
            {
                dbmanager.updateEquipment(equipment);
                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return RedirectToAction("Privacy");
            }
        }

        // 預約設備 // 修改預約相關的 Action 方法，使用 Session 中的用戶資訊
        [HttpPost]
        public JsonResult MakeReservation(byte equipmentId)
        {
            try
            {
                var currentUser = GetCurrentUserId();
                if (currentUser == null)
                {
                    return Json(new ReservationResult
                    {
                        Success = false,
                        Message = "請先登入系統"
                    });
                }

                var result = _dbManager.CreateReservation(equipmentId, currentUser);
                return Json(result);
            }
            catch (Exception e)
            {
                // 過濾掉敏感信息，只顯示用戶友好的錯誤訊息
                var userFriendlyMessage = GetUserFriendlyErrorMessage(e);
                return Json(new ReservationResult
                {
                    Success = false,
                    Message = userFriendlyMessage
                });
            }
        }

        // 添加友好的錯誤訊息處理方法
        private string GetUserFriendlyErrorMessage(Exception e)
        {
            // 根據異常類型返回用戶友好的訊息
            if (e.Message.Contains("開放時間"))
                return e.Message; // 直接顯示業務邏輯錯誤

            if (e.Message.Contains("連接"))
                return "系統暫時無法處理您的請求，請稍後再試";

            if (e.Message.Contains("超時"))
                return "請求超時，請檢查網路連接";

            // 其他未知錯誤
            return "系統發生錯誤，請聯繫管理員";
        }

        // 取消預約
        [HttpPost]
        public JsonResult CancelReservation(int reservationId)
        {
            try
            {
                var currentUser = GetCurrentUserId();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "請先登入系統" });
                }

                var success = _dbManager.CancelReservation(reservationId, currentUser);
                return Json(new { success = success, message = success ? "取消成功" : "取消失敗" });
            }
            catch (Exception e)
            {
                var userFriendlyMessage = GetUserFriendlyErrorMessage(e);
                return Json(new { success = false, message = userFriendlyMessage });
            }
        }

        // 結束使用
        [HttpPost]
        public JsonResult EndUsage(int reservationId)
        {
            try
            {
                var currentUser = GetCurrentUserId();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "請先登入" });
                }

                Console.WriteLine($"=== 結束使用請求 ===");
                Console.WriteLine($"預約ID: {reservationId}");
                Console.WriteLine($"用戶: {currentUser}");

                var success = _dbManager.EndUsage(reservationId, currentUser);

                Console.WriteLine($"結束使用結果: {success}");

                return Json(new
                {
                    success = success,
                    message = success ? "結束使用成功" : "結束使用失敗，請檢查預約狀態"
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"結束使用錯誤: {e}");
                return Json(new
                {
                    success = false,
                    message = "結束使用失敗: " + e.Message
                });
            }
        }
        [HttpPost]
        public JsonResult ProcessAllQueues()
        {
            try
            {
                // 替代方案：循環處理所有設備的排隊
                var equipments = _dbManager.getEquipment();
                foreach (var equipment in equipments)
                {
                    _dbManager.ProcessWaitingQueue(equipment.Id);
                }

                return Json(new { success = true, message = "已處理所有排隊隊列" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        

        [HttpPost]
        public JsonResult ProcessEquipmentQueue(byte equipmentId)
        {
            
            try
            {
                _dbManager.ProcessWaitingQueue(equipmentId);
                return Json(new { success = true, message = "已處理設備排隊隊列" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetQueueDebugInfo(byte equipmentId)
        {
            try
            {
                // 替代方案：手動構建調試信息
                var equipment = _dbManager.GetEquipmentById(equipmentId);
                var currentUsers = _dbManager.GetCurrentUsers(equipmentId);
                var waitingList = _dbManager.GetWaitingQueue(equipmentId);

                var debugInfo = new
                {
                    Equipment = equipment?.equipmentName,
                    CurrentUsers = currentUsers,
                    MaxUsers = equipment?.MaxUsers ?? 0,
                    QueueCount = waitingList.Count,
                    HasVacancy = currentUsers < (equipment?.MaxUsers ?? 0),
                    WaitingUsers = waitingList.Select(q => new { q.UserId, q.Position })
                };

                return Json(new { success = true, data = debugInfo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        

        // 檢查設備可用性
        [HttpGet]
        public IActionResult CheckAvailability(byte equipmentId)
        {
            try
            {
                var equipment = _dbManager.GetEquipmentById(equipmentId);
                if (equipment == null)
                {
                    return Json(new { isAvailable = false, message = "設備不存在" });
                }

                var currentTime = DateTime.Now.TimeOfDay;
                var isInOperatingHours = currentTime >= equipment.OpenTime && currentTime <= equipment.CloseTime;
                var currentUsers = _dbManager.GetCurrentUsers(equipmentId);
                var isWithinCapacity = currentUsers < equipment.MaxUsers;

                return Json(new
                {
                    isAvailable = isInOperatingHours && isWithinCapacity,
                    message = isInOperatingHours ?
                             (isWithinCapacity ? "可預約" : "設備已滿") :
                             "非開放時間"
                });
            }
            catch (Exception e)
            {
                return Json(new { isAvailable = false, message = "檢查失敗: " + e.Message });
            }
        }

        // 添加台灣時間輔助方法
        private DateTime GetTaiwanTime()
        {
            try
            {
                var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taiwanTimeZone);
            }
            catch
            {
                return DateTime.UtcNow.AddHours(8);
            }
        }

        // 獲取排隊信息
        [HttpGet]
        public JsonResult GetQueueInfo(byte equipmentId)
        {
            try
            {
                var queue = _dbManager.GetWaitingQueue(equipmentId);
                var currentUsers = _dbManager.GetCurrentUsers(equipmentId);
                var equipment = _dbManager.GetEquipmentById(equipmentId);

                return Json(new
                {
                    waitingCount = queue.Count,
                    currentUsers = currentUsers,
                    maxUsers = equipment?.MaxUsers ?? 0,
                    queueList = queue.Select(q => new
                    {
                        userId = q.UserId,
                        position = q.Position,
                        queueTime = q.QueueTime
                    })
                });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        // API Action - 返回 JsonResult（供前端 JavaScript 調用）
        [HttpGet]
        public JsonResult GetCurrentUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userName = HttpContext.Session.GetString("UserName");

            if (userId == null || string.IsNullOrEmpty(userName))
            {
                return Json(new { isLoggedIn = false });
            }

            return Json(new
            {
                isLoggedIn = true,
                userId = userId,
                userName = userName
            });
        }

        // 添加登入檢查的輔助方法
        private string GetCurrentUserId()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userName = HttpContext.Session.GetString("UserName");

            if (userId == null || string.IsNullOrEmpty(userName))
            {
                return null;
            }

            return userName; // 返回用戶名作為字符串
        }

        // 預約頁面
        public IActionResult Reservation()
        {
            var currentUser = GetCurrentUserId();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var equipments = _dbManager.getEquipment();
            ViewBag.equipments = equipments;
            return View(equipments);
        }

        // 我的預約頁面
        public IActionResult MyReservations()
        {
            var currentUser = GetCurrentUserId();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        // 獲取用戶的預約信息
        [HttpGet]
        public JsonResult GetMyReservations()
        {
            try
            {
                // 替代方案：只處理相關設備的排隊，而不是全部
                // 或者直接移除這行，因為排隊處理應該由其他機制觸發
                // _dbManager.ProcessAllWaitingQueues();

                var currentUser = GetCurrentUserId();
                if (currentUser == null)
                {
                    return Json(new { error = "請先登入" });
                }

                var taiwanTime = GetTaiwanTime();

                var activeReservations = _dbManager.GetActiveReservations(currentUser);
                var waitingReservations = _dbManager.GetWaitingQueues(currentUser);
                var historyReservations = _dbManager.GetHistoryReservations(currentUser);

                // 修正：確保剩餘時間正確計算
                var activeWithRemainingTime = activeReservations.Select(r =>
                {
                    var remainingTime = CalculateRemainingTimeTaiwan(
                        (DateTime)r["StartTime"],
                        Convert.ToInt32(r["AvailableTime"])
                    );

                    return new Dictionary<string, object>
                    {
                        ["Id"] = r["Id"],
                        ["EquipmentId"] = r["EquipmentId"],
                        ["EquipmentName"] = r["EquipmentName"],
                        ["UserId"] = r["UserId"],
                        ["StartTime"] = r["StartTime"],
                        ["AvailableTime"] = r["AvailableTime"],
                        ["ReservationTime"] = r["ReservationTime"],
                        ["Status"] = r["Status"],
                        ["RemainingTime"] = remainingTime
                    };
                }).ToList();

                var result = new
                {
                    activeReservations = activeWithRemainingTime,
                    waitingReservations = waitingReservations,
                    historyReservations = historyReservations,
                    serverTaiwanTime = taiwanTime.ToString("yyyy-MM-dd HH:mm:ss")
                };

                return Json(result);
            }
            catch (Exception e)
            {
                Console.WriteLine($"GetMyReservations 錯誤: {e}");
                return Json(new { error = e.Message });
            }
        }
        

        [HttpGet]
        public JsonResult TestDataFormat()
        {
            try
            {
                var currentUser = GetCurrentUserId();
                if (currentUser == null)
                {
                    return Json(new { error = "請先登入" });
                }

                var activeReservations = _dbManager.GetActiveReservations(currentUser);
                var waitingReservations = _dbManager.GetWaitingQueues(currentUser);
                var historyReservations = _dbManager.GetHistoryReservations(currentUser);

                // 返回數據類型和格式信息
                return Json(new
                {
                    activeReservationsCount = activeReservations.Count,
                    waitingReservationsCount = waitingReservations.Count,
                    historyReservationsCount = historyReservations.Count,
                    sampleActive = activeReservations.FirstOrDefault(),
                    sampleWaiting = waitingReservations.FirstOrDefault(),
                    sampleHistory = historyReservations.FirstOrDefault(),
                    dataTypes = new
                    {
                        activeType = activeReservations.GetType().Name,
                        waitingType = waitingReservations.GetType().Name,
                        historyType = historyReservations.GetType().Name
                    }
                });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message, stackTrace = e.StackTrace });
            }
        }

        [HttpGet]
        public JsonResult CheckEquipmentAvailability(byte equipmentId)
        {
            try
            {
                var equipment = _dbManager.GetEquipmentById(equipmentId);
                if (equipment == null)
                {
                    return Json(new
                    {
                        isAvailable = false,
                        message = "設備不存在",
                        serverTime = GetTaiwanTime().ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }

                // 使用台灣時間檢查
                DateTime taiwanTime = GetTaiwanTime();
                TimeSpan currentTimeOfDay = taiwanTime.TimeOfDay;

                bool isInOperatingHours = currentTimeOfDay >= equipment.OpenTime &&
                                        currentTimeOfDay <= equipment.CloseTime;

                var currentUsers = _dbManager.GetCurrentUsers(equipmentId);
                bool isWithinCapacity = currentUsers < equipment.MaxUsers;

                // 修改：即使人數已滿，按鈕也不禁用，而是進入排隊
                bool isAvailable = isInOperatingHours;

                string message;
                if (!isInOperatingHours)
                {
                    message = $"非開放時間（台灣時間: {taiwanTime:HH:mm}，開放時間: {equipment.OpenTime:hh\\:mm}-{equipment.CloseTime:hh\\:mm}）";
                }
                else if (!isWithinCapacity)
                {
                    message = "設備已滿，點擊預約將加入排隊";
                }
                else
                {
                    message = "可預約";
                }

                return Json(new
                {
                    isAvailable = isAvailable,
                    canReserve = isInOperatingHours, // 新增：是否可預約（包括排隊）
                    isFull = !isWithinCapacity, // 新增：是否已滿
                    message = message,
                    currentUsers = currentUsers,
                    maxUsers = equipment.MaxUsers,
                    averageUsageTime = equipment.AvailableTime,
                    serverTaiwanTime = taiwanTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    openTime = equipment.OpenTime.ToString(@"hh\:mm"),
                    closeTime = equipment.CloseTime.ToString(@"hh\:mm")
                });
            }
            catch (Exception e)
            {
                return Json(new
                {
                    isAvailable = false,
                    canReserve = false,
                    isFull = false,
                    message = "檢查失敗: " + e.Message,
                    serverTime = GetTaiwanTime().ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }
        // 計算基於台灣時間的剩餘時間
        private int CalculateRemainingTimeTaiwan(DateTime startTime, int availableTime)
        {
            try
            {
                var taiwanTime = GetTaiwanTime();

                // 如果開始時間是 UTC，轉換為台灣時間
                if (startTime.Kind == DateTimeKind.Utc)
                {
                    var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
                    startTime = TimeZoneInfo.ConvertTimeFromUtc(startTime, taiwanTimeZone);
                }

                var elapsedMinutes = (int)(taiwanTime - startTime).TotalMinutes;
                var remaining = availableTime - elapsedMinutes;

                Console.WriteLine($"=== 剩餘時間計算 ===");
                Console.WriteLine($"開始時間: {startTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"台灣時間: {taiwanTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"可用時間: {availableTime} 分鐘");
                Console.WriteLine($"經過時間: {elapsedMinutes} 分鐘");
                Console.WriteLine($"剩餘時間: {remaining} 分鐘");

                return Math.Max(0, remaining);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"計算剩餘時間錯誤: {ex.Message}");
                return 0;
            }
        }

        [HttpPost]
        public JsonResult ManualCleanExpiredReservations()
        {
            try
            {
                _dbManager.AutoCompleteExpiredReservations();
                return Json(new { success = true, message = "已手動清理過期預約" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"清理失敗: {ex.Message}" });
            }
        }

        // 取消排隊
        [HttpPost]
        public JsonResult CancelQueue(int queueId)
        {
            try
            {
                var currentUser = GetCurrentUserId();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "請先登入" });
                }

                var success = _dbManager.CancelWaitingQueue(queueId, currentUser);
                return Json(new { success = success, message = success ? "取消排隊成功" : "取消排隊失敗" });
            }
            catch (Exception e)
            {
                return Json(new { success = false, message = "取消排隊失敗: " + e.Message });
            }
        }

        [HttpPost]
        public JsonResult TestEndUsage(int reservationId)
        {
            try
            {
                Console.WriteLine($"測試結束使用 - 預約ID: {reservationId}");
                return Json(new
                {
                    success = true,
                    message = "測試成功，預約ID: " + reservationId,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            catch (Exception e)
            {
                return Json(new
                {
                    success = false,
                    message = "測試失敗: " + e.Message
                });
            }
        }

        // 其他原有方法...
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
