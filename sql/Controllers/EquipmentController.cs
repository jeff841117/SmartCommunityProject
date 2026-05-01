using Microsoft.AspNetCore.Mvc;
using sql.Models;
using sql.Services;
using System.Diagnostics;

namespace sql.Controllers
{
    // EquipmentController 是設備、預約、排隊相關的主要入口。
    // 這次重構的重點之一，是把設備操作也收斂到 EquipmentService，
    // 讓 Controller 更專心處理請求與回應。
    public class EquipmentController : AppControllerBase
    {
        private readonly ILogger<EquipmentController> _logger;
        private readonly EquipmentService _equipmentService;
        private readonly ReservationService _reservationService;
        private readonly QueueService _queueService;
        private readonly FutureReservationPlanningService _futureReservationPlanningService;
        private readonly CurrentUserService _currentUserService;

        public EquipmentController(
            ILogger<EquipmentController> logger,
            EquipmentService equipmentService,
            ReservationService reservationService,
            QueueService queueService,
            FutureReservationPlanningService futureReservationPlanningService,
            CurrentUserService currentUserService)
            : base(currentUserService)
        {
            _logger = logger;
            _equipmentService = equipmentService;
            _reservationService = reservationService;
            _queueService = queueService;
            _futureReservationPlanningService = futureReservationPlanningService;
            _currentUserService = currentUserService;
        }

        public IActionResult Index()
        {
            var accessRedirect = EnsureManagerRedirect();
            if (accessRedirect != null)
            {
                return accessRedirect;
            }

            var viewModel = new EquipmentManagementPageViewModel
            {
                Equipments = _equipmentService.GetAllEquipments()
            };

            return View(viewModel);
        }

        public IActionResult ReservationDashboard()
        {
            var accessRedirect = EnsureManagerRedirect();
            if (accessRedirect != null)
            {
                return accessRedirect;
            }

            var viewModel = new ReservationManagementPageViewModel
            {
                Dashboard = _reservationService.GetReservationDashboard()
            };

            return View(viewModel);
        }

        [HttpGet]
        public JsonResult GetEquipmentReservationChain(byte equipmentId)
        {
            try
            {
                if (!_currentUserService.IsManager())
                {
                    return Json(ApiResponseFactory.DataFailure<EquipmentReservationChainResponse>("您沒有管理員權限"));
                }

                var chain = _reservationService.GetEquipmentReservationChain(equipmentId);
                return Json(ApiResponseFactory.DataSuccess(chain));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得設備 {EquipmentId} 預約鏈資訊時發生錯誤", equipmentId);
                return Json(ApiResponseFactory.DataFailure<EquipmentReservationChainResponse>(
                    ApiExceptionTranslator.ToUserMessage(ex, ex.Message)));
            }
        }

        public IActionResult addEquipment()
        {
            var accessRedirect = EnsureManagerRedirect();
            if (accessRedirect != null)
            {
                return accessRedirect;
            }

            return View(new AddEquipmentFormViewModel());
        }

        // 設備新增這裡改成走 EquipmentService，
        // 代表 Controller 已經不直接碰資料層。
        [HttpPost]
        public IActionResult addEquipment(AddEquipmentFormViewModel form)
        {
            var accessRedirect = EnsureManagerRedirect();
            if (accessRedirect != null)
            {
                return accessRedirect;
            }

            if (!ModelState.IsValid)
            {
                form.ErrorMessage = "請確認設備資料是否填寫正確";
                return View(form);
            }

            try
            {
                var equipment = new Equipment
                {
                    equipmentName = form.EquipmentName,
                    MaxUsers = form.MaxUsers,
                    AvailableTime = form.AvailableTime,
                    OpenTime = form.OpenTime,
                    CloseTime = form.CloseTime
                };

                _equipmentService.CreateEquipment(equipment);
            }
            catch
            {
                form.ErrorMessage = "新增設備失敗";
                return View(form);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult deleteEquipment(DeleteEquipmentFormViewModel form)
        {
            var accessRedirect = EnsureManagerRedirect();
            if (accessRedirect != null)
            {
                return accessRedirect;
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "刪除設備失敗：缺少設備編號";
                return RedirectToAction("Index");
            }

            try
            {
                _equipmentService.DeleteEquipment(form.Id);
            }
            catch
            {
                return RedirectToAction("Privacy");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateEquipment(UpdateEquipmentFormViewModel form)
        {
            if (EnsureManagerRedirect() != null)
            {
                return Json(ApiResponseFactory.OperationFailure("您沒有管理員權限"));
            }

            if (!ModelState.IsValid)
            {
                return Json(ApiResponseFactory.OperationFailure("請確認設備欄位是否填寫正確"));
            }

            try
            {
                var equipment = new Equipment
                {
                    Id = form.Id,
                    equipmentName = form.EquipmentName,
                    MaxUsers = form.MaxUsers,
                    AvailableTime = form.AvailableTime,
                    OpenTime = form.OpenTime,
                    CloseTime = form.CloseTime
                };

                _equipmentService.UpdateEquipment(equipment);
                return Json(ApiResponseFactory.OperationSuccess("更新設備成功"));
            }
            catch
            {
                return RedirectToAction("Privacy");
            }
        }

        // 預約按鈕按下後，Controller 只做三件事：
        // 1. 取得目前使用者
        // 2. 呼叫 Service
        // 3. 把結果回給前端
        [HttpPost]
        public JsonResult MakeReservation(byte equipmentId)
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                var result = _reservationService.MakeReservation(equipmentId, currentUser);
                return Json(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "處理設備預約時發生未預期錯誤");
                return Json(new ReservationResult
                {
                    Success = false,
                    Message = ApiExceptionTranslator.ToUserMessage(e)
                });
            }
        }

        // 這個入口專門處理未來時段預約。
        // 和立即預約分開後，之後要補排隊轉換規則會比較安全。
        [HttpPost]
        public JsonResult CreateFutureReservation(FutureReservationRequestViewModel form)
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                var result = _reservationService.CreateFutureReservation(form, currentUser);
                return Json(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "建立未來時段預約時發生未預期錯誤");
                return Json(new ReservationResult
                {
                    Success = false,
                    Message = ApiExceptionTranslator.ToUserMessage(e)
                });
            }
        }

        [HttpPost]
        public JsonResult CancelReservation(int reservationId)
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                if (!currentUser.IsAuthenticated)
                {
                    return Json(ApiResponseFactory.OperationFailure("請先登入系統"));
                }

                var success = _reservationService.CancelReservation(reservationId, currentUser);
                return Json(success
                    ? ApiResponseFactory.OperationSuccess("取消成功")
                    : ApiResponseFactory.OperationFailure("取消失敗"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "取消預約時發生錯誤");
                return Json(ApiResponseFactory.OperationFailure(ApiExceptionTranslator.ToUserMessage(e)));
            }
        }

        [HttpPost]
        public JsonResult EndUsage(int reservationId)
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                if (!currentUser.IsAuthenticated)
                {
                    return Json(ApiResponseFactory.OperationFailure("請先登入"));
                }

                var success = _reservationService.EndUsage(reservationId, currentUser);
                return Json(success
                    ? ApiResponseFactory.OperationSuccess("結束使用成功")
                    : ApiResponseFactory.OperationFailure("結束使用失敗，請檢查預約狀態"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "結束設備使用時發生錯誤");
                return Json(ApiResponseFactory.OperationFailure("結束使用失敗: " + ApiExceptionTranslator.ToUserMessage(e, e.Message)));
            }
        }

        [HttpPost]
        public JsonResult ForceEndUsage(int reservationId)
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                if (!currentUser.IsManager)
                {
                    return Json(ApiResponseFactory.OperationFailure("您沒有管理員權限"));
                }

                var success = _reservationService.ForceEndUsage(reservationId, currentUser);
                return Json(success
                    ? ApiResponseFactory.OperationSuccess("已由管理者強制結束使用")
                    : ApiResponseFactory.OperationFailure("強制結束失敗，請確認該預約是否仍在使用中"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "管理者強制結束使用時發生錯誤");
                return Json(ApiResponseFactory.OperationFailure(ApiExceptionTranslator.ToUserMessage(e, e.Message)));
            }
        }

        [HttpPost]
        public JsonResult ForceCancelScheduledReservation(int reservationId)
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                if (!currentUser.IsManager)
                {
                    return Json(ApiResponseFactory.OperationFailure("您沒有管理員權限"));
                }

                var success = _reservationService.ForceCancelScheduledReservation(reservationId, currentUser);
                return Json(success
                    ? ApiResponseFactory.OperationSuccess("已由管理者取消未來預約")
                    : ApiResponseFactory.OperationFailure("取消失敗，請確認該預約是否尚未開始"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "管理者取消未來預約時發生錯誤");
                return Json(ApiResponseFactory.OperationFailure(ApiExceptionTranslator.ToUserMessage(e, e.Message)));
            }
        }

        [HttpPost]
        public JsonResult ForceRescheduleScheduledReservation(AdminRescheduleReservationFormViewModel form)
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                if (!currentUser.IsManager)
                {
                    return Json(new ReservationResult
                    {
                        Success = false,
                        Message = "您沒有管理員權限"
                    });
                }

                if (!ModelState.IsValid)
                {
                    return Json(new ReservationResult
                    {
                        Success = false,
                        Message = "請確認新的日期與時段是否填寫完整"
                    });
                }

                var result = _reservationService.ForceRescheduleScheduledReservation(form, currentUser);
                return Json(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "管理者調整未來預約時段時發生錯誤");
                return Json(new ReservationResult
                {
                    Success = false,
                    Message = ApiExceptionTranslator.ToUserMessage(e, e.Message)
                });
            }
        }

        [HttpGet]
        public JsonResult PreviewRescheduleScheduledReservation(
            int reservationId,
            string reservationDate,
            string selectedSlotStartTime)
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                var preview = _reservationService.PreviewRescheduleScheduledReservation(
                    new AdminRescheduleReservationFormViewModel
                    {
                        ReservationId = reservationId,
                        ReservationDate = reservationDate,
                        SelectedSlotStartTime = selectedSlotStartTime
                    },
                    currentUser);

                return Json(ApiResponseFactory.DataSuccess(preview));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "預覽管理者調整未來預約時段影響時發生錯誤");
                return Json(ApiResponseFactory.DataFailure<ReservationAdjustmentPreviewResponse>(
                    ApiExceptionTranslator.ToUserMessage(e, e.Message)));
            }
        }

        [HttpPost]
        public JsonResult ProcessAllQueues()
        {
            try
            {
                _queueService.ProcessAllQueues();
                return Json(ApiResponseFactory.OperationSuccess("已處理所有排隊隊列"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理所有排隊隊列時發生錯誤");
                return Json(ApiResponseFactory.OperationFailure(ApiExceptionTranslator.ToUserMessage(ex, ex.Message)));
            }
        }

        [HttpPost]
        public JsonResult ProcessEquipmentQueue(byte equipmentId)
        {
            try
            {
                _queueService.ProcessEquipmentQueue(equipmentId);
                return Json(ApiResponseFactory.OperationSuccess("已處理設備排隊隊列"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理設備 {EquipmentId} 排隊隊列時發生錯誤", equipmentId);
                return Json(ApiResponseFactory.OperationFailure(ApiExceptionTranslator.ToUserMessage(ex, ex.Message)));
            }
        }

        [HttpGet]
        public JsonResult GetQueueDebugInfo(byte equipmentId)
        {
            try
            {
                var debugInfo = _queueService.GetQueueDebugInfo(equipmentId);
                return Json(ApiResponseFactory.DataSuccess(debugInfo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得設備 {EquipmentId} 排隊偵錯資訊時發生錯誤", equipmentId);
                return Json(ApiResponseFactory.DataFailure<QueueDebugInfoResponse>(ex.Message));
            }
        }

        [HttpGet]
        public IActionResult CheckAvailability(byte equipmentId)
        {
            try
            {
                var result = _equipmentService.CheckAvailability(equipmentId);
                return Json(ApiResponseFactory.DataSuccess(new AvailabilitySummaryResponse
                {
                    IsAvailable = result.IsAvailable,
                    Message = result.Message
                }));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "檢查設備 {EquipmentId} 可用性時發生錯誤", equipmentId);
                return Json(ApiResponseFactory.DataFailure(
                    "檢查失敗: " + ApiExceptionTranslator.ToUserMessage(e, e.Message),
                    new AvailabilitySummaryResponse
                    {
                        IsAvailable = false,
                        Message = "檢查失敗: " + ApiExceptionTranslator.ToUserMessage(e, e.Message)
                    }));
            }
        }

        [HttpGet]
        public JsonResult GetQueueInfo(byte equipmentId)
        {
            try
            {
                var queueInfo = _queueService.GetQueueInfo(equipmentId);
                return Json(ApiResponseFactory.DataSuccess(queueInfo));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "取得設備 {EquipmentId} 排隊資訊時發生錯誤", equipmentId);
                return Json(ApiResponseFactory.DataFailure<QueueInfoResponse>(e.Message));
            }
        }

        [HttpGet]
        public JsonResult GetCurrentUser()
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsAuthenticated)
            {
                return Json(new CurrentUserResponse
                {
                    IsLoggedIn = false
                });
            }

            return Json(new CurrentUserResponse
            {
                IsLoggedIn = true,
                UserId = currentUser.UserId,
                UserName = currentUser.UserName
            });
        }

        public IActionResult Reservation()
        {
            var accessRedirect = EnsureAuthenticatedRedirect();
            if (accessRedirect != null)
            {
                return accessRedirect;
            }

            var viewModel = new EquipmentReservationPageViewModel
            {
                Equipments = _equipmentService.GetAllEquipments(),
                SlotIntervalMinutes = _futureReservationPlanningService.GetSlotIntervalMinutes(),
                AdvanceReservationDays = _futureReservationPlanningService.GetAdvanceReservationDays()
            };

            return View(viewModel);
        }

        // 第二階段的未來預約先從這個規劃入口開始。
        // 建立流程會另外走 CreateFutureReservation，避免查詢和寫入混在一起。
        [HttpGet]
        public JsonResult GetFutureReservationPlanning(byte equipmentId, string? reservationDate)
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                if (!currentUser.IsAuthenticated)
                {
                    return Json(ApiResponseFactory.DataFailure<FutureReservationPlanningResponse>("請先登入"));
                }

                DateOnly? parsedDate = null;
                if (!string.IsNullOrWhiteSpace(reservationDate)
                    && DateOnly.TryParse(reservationDate, out var date))
                {
                    parsedDate = date;
                }

                var planning = _futureReservationPlanningService.BuildPlanning(equipmentId, parsedDate);
                return Json(ApiResponseFactory.DataSuccess(planning));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "取得設備 {EquipmentId} 未來預約規劃時發生錯誤", equipmentId);
                return Json(ApiResponseFactory.DataFailure<FutureReservationPlanningResponse>(
                    ApiExceptionTranslator.ToUserMessage(e, e.Message)));
            }
        }

        public IActionResult MyReservations()
        {
            var accessRedirect = EnsureAuthenticatedRedirect();
            if (accessRedirect != null)
            {
                return accessRedirect;
            }

            return View();
        }

        // 「我的預約」頁面的主要資料來源。
        // Controller 不直接查資料庫，而是交給 Service 整理成前端需要的格式。
        [HttpGet]
        public JsonResult GetMyReservations()
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                if (!currentUser.IsAuthenticated)
                {
                    return Json(ApiResponseFactory.DataFailure<UserReservationsResponse>("請先登入"));
                }

                var result = _reservationService.GetUserReservations(currentUser);
                return Json(ApiResponseFactory.DataSuccess(result));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "取得目前使用者預約資料時發生錯誤");
                return Json(ApiResponseFactory.DataFailure<UserReservationsResponse>(e.Message));
            }
        }

        [HttpGet]
        public JsonResult TestDataFormat()
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                if (!currentUser.IsAuthenticated)
                {
                    return Json(ApiResponseFactory.Error("請先登入"));
                }

                var reservations = _reservationService.GetUserReservations(currentUser);

                return Json(new ReservationDataFormatResponse
                {
                    ActiveReservationsCount = reservations.ActiveReservations.Count,
                    WaitingReservationsCount = reservations.WaitingReservations.Count,
                    HistoryReservationsCount = reservations.HistoryReservations.Count,
                    SampleActive = reservations.ActiveReservations.FirstOrDefault(),
                    SampleWaiting = reservations.WaitingReservations.FirstOrDefault(),
                    SampleHistory = reservations.HistoryReservations.FirstOrDefault(),
                    DataTypes = new ReservationDataTypesInfo
                    {
                        ActiveType = reservations.ActiveReservations.GetType().Name,
                        WaitingType = reservations.WaitingReservations.GetType().Name,
                        HistoryType = reservations.HistoryReservations.GetType().Name
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "測試預約資料格式時發生錯誤");
                return Json(ApiResponseFactory.Error(e.Message, e.StackTrace));
            }
        }

        [HttpGet]
        public JsonResult CheckEquipmentAvailability(byte equipmentId)
        {
            try
            {
                var availability = _reservationService.CheckEquipmentAvailability(equipmentId);
                return Json(ApiResponseFactory.DataSuccess(availability));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "檢查設備 {EquipmentId} 預約可用性時發生錯誤", equipmentId);
                var userMessage = ApiExceptionTranslator.ToUserMessage(e, e.Message);
                return Json(ApiResponseFactory.DataFailure(
                    "檢查失敗: " + userMessage,
                    new EquipmentAvailabilityResponse
                    {
                        IsAvailable = false,
                        CanReserve = false,
                        IsFull = false,
                        Message = "檢查失敗: " + userMessage,
                        ServerTaiwanTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    }));
            }
        }

        [HttpPost]
        public JsonResult ManualCleanExpiredReservations()
        {
            try
            {
                _reservationService.AutoCompleteExpiredReservations();
                return Json(ApiResponseFactory.OperationSuccess("已手動清理過期預約"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "手動清理過期預約時發生錯誤");
                return Json(ApiResponseFactory.OperationFailure($"清理失敗: {ApiExceptionTranslator.ToUserMessage(ex, ex.Message)}"));
            }
        }

        [HttpPost]
        public JsonResult CancelQueue(int queueId)
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                if (!currentUser.IsAuthenticated)
                {
                    return Json(ApiResponseFactory.OperationFailure("請先登入"));
                }

                var success = _queueService.CancelQueue(queueId, currentUser);
                return Json(success
                    ? ApiResponseFactory.OperationSuccess("取消排隊成功")
                    : ApiResponseFactory.OperationFailure("取消排隊失敗"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "取消排隊時發生錯誤");
                return Json(ApiResponseFactory.OperationFailure("取消排隊失敗: " + ApiExceptionTranslator.ToUserMessage(e, e.Message)));
            }
        }

        [HttpPost]
        public JsonResult ForceCancelQueue(int queueId)
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                if (!currentUser.IsManager)
                {
                    return Json(ApiResponseFactory.OperationFailure("您沒有管理員權限"));
                }

                var success = _queueService.ForceCancelQueue(queueId, currentUser);
                return Json(success
                    ? ApiResponseFactory.OperationSuccess("已由管理者移除排隊紀錄")
                    : ApiResponseFactory.OperationFailure("移除排隊失敗，請確認該紀錄是否仍存在"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "管理者移除排隊時發生錯誤");
                return Json(ApiResponseFactory.OperationFailure(ApiExceptionTranslator.ToUserMessage(e, e.Message)));
            }
        }

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
