using Microsoft.AspNetCore.Mvc;
using sql.Models;
using sql.Services;

namespace sql.Controllers.Api
{
    [ApiController]
    [Route("api/admin")]
    public class AdminApiController : ControllerBase
    {
        private readonly ReservationService _reservationService;
        private readonly QueueService _queueService;
        private readonly AdminActionLogService _adminActionLogService;
        private readonly CurrentUserService _currentUserService;

        public AdminApiController(
            ReservationService reservationService,
            QueueService queueService,
            AdminActionLogService adminActionLogService,
            CurrentUserService currentUserService)
        {
            _reservationService = reservationService;
            _queueService = queueService;
            _adminActionLogService = adminActionLogService;
            _currentUserService = currentUserService;
        }

        [HttpGet("reservations/dashboard")]
        public IActionResult GetDashboard([FromQuery] ReservationDashboardFilter filter)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsManager)
            {
                return Unauthorized(ApiResponseFactory.DataFailure<ReservationDashboardResponse>("您沒有管理員權限。"));
            }

            var result = _reservationService.GetReservationDashboard(filter);
            return Ok(ApiResponseFactory.DataSuccess(result));
        }

        [HttpGet("reservations/equipment/{equipmentId}/chain")]
        public IActionResult GetEquipmentReservationChain(byte equipmentId)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsManager)
            {
                return Unauthorized(ApiResponseFactory.DataFailure<EquipmentReservationChainResponse>("您沒有管理員權限。"));
            }

            var result = _reservationService.GetEquipmentReservationChain(equipmentId);
            return Ok(ApiResponseFactory.DataSuccess(result));
        }

        [HttpPost("reservations/{reservationId:int}/force-end")]
        public IActionResult ForceEndUsage(int reservationId)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsManager)
            {
                return Unauthorized(ApiResponseFactory.OperationFailure("您沒有管理員權限。"));
            }

            var success = _reservationService.ForceEndUsage(reservationId, currentUser);
            return Ok(success
                ? ApiResponseFactory.OperationSuccess("管理者已成功強制結束使用。")
                : ApiResponseFactory.OperationFailure("強制結束失敗，請確認該筆預約是否仍在使用中。"));
        }

        [HttpPost("reservations/{reservationId:int}/cancel-scheduled")]
        public IActionResult ForceCancelScheduledReservation(int reservationId)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsManager)
            {
                return Unauthorized(ApiResponseFactory.OperationFailure("您沒有管理員權限。"));
            }

            var success = _reservationService.ForceCancelScheduledReservation(reservationId, currentUser);
            return Ok(success
                ? ApiResponseFactory.OperationSuccess("管理者已成功取消未來預約。")
                : ApiResponseFactory.OperationFailure("取消未來預約失敗，請確認該筆資料是否仍可取消。"));
        }

        [HttpPost("reservations/{reservationId:int}/reschedule-preview")]
        public IActionResult PreviewReschedule(
            int reservationId,
            [FromBody] AdminRescheduleReservationFormViewModel request)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsManager)
            {
                return Unauthorized(ApiResponseFactory.DataFailure<ReservationAdjustmentPreviewResponse>("您沒有管理員權限。"));
            }

            request.ReservationId = reservationId;
            var result = _reservationService.PreviewRescheduleScheduledReservation(request, currentUser);
            return Ok(ApiResponseFactory.DataSuccess(result));
        }

        [HttpPost("reservations/{reservationId:int}/reschedule")]
        public IActionResult RescheduleReservation(
            int reservationId,
            [FromBody] AdminRescheduleReservationFormViewModel request)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsManager)
            {
                return Unauthorized(ApiResponseFactory.OperationFailure("您沒有管理員權限。"));
            }

            request.ReservationId = reservationId;
            var result = _reservationService.ForceRescheduleScheduledReservation(request, currentUser);
            return Ok(result);
        }

        [HttpPost("queues/{queueId:int}/remove")]
        public IActionResult ForceCancelQueue(int queueId)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsManager)
            {
                return Unauthorized(ApiResponseFactory.OperationFailure("您沒有管理員權限。"));
            }

            var success = _queueService.ForceCancelQueue(queueId, currentUser);
            return Ok(success
                ? ApiResponseFactory.OperationSuccess("管理者已成功移除排隊紀錄。")
                : ApiResponseFactory.OperationFailure("移除排隊紀錄失敗，請確認該筆資料是否仍在排隊中。"));
        }

        [HttpGet("action-logs")]
        public IActionResult GetAdminActionLogs([FromQuery] AdminActionLogFilter filter)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsManager)
            {
                return Unauthorized(ApiResponseFactory.DataFailure<AdminActionLogQueryResult>("您沒有管理員權限。"));
            }

            var result = _adminActionLogService.GetLogs(filter);
            return Ok(ApiResponseFactory.DataSuccess(result));
        }
    }
}
