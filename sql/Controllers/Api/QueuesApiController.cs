using Microsoft.AspNetCore.Mvc;
using sql.Models;
using sql.Services;

namespace sql.Controllers.Api
{
    [ApiController]
    [Route("api/queues")]
    public class QueuesApiController : ControllerBase
    {
        private readonly QueueService _queueService;
        private readonly CurrentUserService _currentUserService;

        public QueuesApiController(QueueService queueService, CurrentUserService currentUserService)
        {
            _queueService = queueService;
            _currentUserService = currentUserService;
        }

        [HttpGet("{equipmentId}")]
        public IActionResult GetQueueInfo(byte equipmentId)
        {
            var result = _queueService.GetQueueInfo(equipmentId);
            return Ok(ApiResponseFactory.DataSuccess(result));
        }

        [HttpPost("{queueId:int}/cancel")]
        public IActionResult CancelQueue(int queueId)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsAuthenticated)
            {
                return Unauthorized(ApiResponseFactory.OperationFailure("請先登入後再取消排隊。"));
            }

            var success = _queueService.CancelQueue(queueId, currentUser);
            return Ok(success
                ? ApiResponseFactory.OperationSuccess("取消排隊成功。")
                : ApiResponseFactory.OperationFailure("取消排隊失敗。"));
        }
    }
}
