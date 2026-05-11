using Microsoft.AspNetCore.Mvc;
using sql.Models;
using sql.Services;

namespace sql.Controllers.Api
{
    [ApiController]
    [Route("api/reservations")]
    public class ReservationsApiController : ControllerBase
    {
        private readonly ReservationService _reservationService;
        private readonly FutureReservationPlanningService _futureReservationPlanningService;
        private readonly CurrentUserService _currentUserService;

        public ReservationsApiController(
            ReservationService reservationService,
            FutureReservationPlanningService futureReservationPlanningService,
            CurrentUserService currentUserService)
        {
            _reservationService = reservationService;
            _futureReservationPlanningService = futureReservationPlanningService;
            _currentUserService = currentUserService;
        }

        [HttpGet("me")]
        public IActionResult GetMyReservations()
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsAuthenticated)
            {
                return Unauthorized(ApiResponseFactory.DataFailure<UserReservationsResponse>("請先登入後再查看我的預約。"));
            }

            var result = _reservationService.GetUserReservations(currentUser);
            return Ok(ApiResponseFactory.DataSuccess(result));
        }

        [HttpGet("planning")]
        public IActionResult GetFuturePlanning([FromQuery] byte equipmentId, [FromQuery] string? reservationDate)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsAuthenticated)
            {
                return Unauthorized(ApiResponseFactory.DataFailure<FutureReservationPlanningResponse>("請先登入後再查看預約規劃。"));
            }

            DateOnly? parsedDate = null;
            if (!string.IsNullOrWhiteSpace(reservationDate) &&
                DateOnly.TryParse(reservationDate, out var date))
            {
                parsedDate = date;
            }

            var planning = _futureReservationPlanningService.BuildPlanning(equipmentId, parsedDate);
            return Ok(ApiResponseFactory.DataSuccess(planning));
        }

        [HttpGet("availability/{equipmentId}")]
        public IActionResult CheckAvailability(byte equipmentId)
        {
            var availability = _reservationService.CheckEquipmentAvailability(equipmentId);
            return Ok(ApiResponseFactory.DataSuccess(availability));
        }

        [HttpPost("immediate")]
        public IActionResult CreateImmediateReservation([FromBody] ApiImmediateReservationRequest request)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsAuthenticated)
            {
                return Unauthorized(ApiResponseFactory.OperationFailure("請先登入後再建立立即使用。"));
            }

            var result = _reservationService.MakeReservation(request.EquipmentId, currentUser);
            return Ok(result);
        }

        [HttpPost("future")]
        public IActionResult CreateFutureReservation([FromBody] ApiFutureReservationRequest request)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsAuthenticated)
            {
                return Unauthorized(ApiResponseFactory.OperationFailure("請先登入後再建立未來預約。"));
            }

            var result = _reservationService.CreateFutureReservation(new FutureReservationRequestViewModel
            {
                EquipmentId = request.EquipmentId,
                ReservationDate = request.ReservationDate,
                SelectedSlotStartTime = request.SelectedSlotStartTime,
                ConfirmQueueExpected = request.ConfirmQueueExpected
            }, currentUser);

            return Ok(result);
        }

        [HttpPost("{reservationId:int}/cancel")]
        public IActionResult CancelReservation(int reservationId)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsAuthenticated)
            {
                return Unauthorized(ApiResponseFactory.OperationFailure("請先登入後再取消預約。"));
            }

            var success = _reservationService.CancelReservation(reservationId, currentUser);
            return Ok(success
                ? ApiResponseFactory.OperationSuccess("取消預約成功。")
                : ApiResponseFactory.OperationFailure("取消預約失敗。"));
        }

        [HttpPost("{reservationId:int}/end-usage")]
        public IActionResult EndUsage(int reservationId)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsAuthenticated)
            {
                return Unauthorized(ApiResponseFactory.OperationFailure("請先登入後再結束使用。"));
            }

            var success = _reservationService.EndUsage(reservationId, currentUser);
            return Ok(success
                ? ApiResponseFactory.OperationSuccess("結束使用成功。")
                : ApiResponseFactory.OperationFailure("結束使用失敗，請確認該筆預約是否仍在使用中。"));
        }
    }
}
