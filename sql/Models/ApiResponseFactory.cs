namespace sql.Models
{
    // 這個工廠類的目的，是把 Controller 常見的 JSON 回應集中管理。
    // 以前每個 action 都自己 new 一份 Success / Message / Data，
    // 久了很容易出現欄位一樣、但寫法不一致的情況。
    public static class ApiResponseFactory
    {
        public static ApiOperationResponse OperationSuccess(string message)
        {
            return new ApiOperationResponse
            {
                Success = true,
                Message = message
            };
        }

        public static ApiOperationResponse OperationFailure(string message)
        {
            return new ApiOperationResponse
            {
                Success = false,
                Message = message
            };
        }

        public static ApiDetailedOperationResponse DetailedFailure(string message, int? errorCode = null, string? stackTrace = null)
        {
            return new ApiDetailedOperationResponse
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                StackTrace = stackTrace
            };
        }

        public static ApiDataResponse<T> DataSuccess<T>(T data, string message = "")
        {
            return new ApiDataResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiDataResponse<T> DataFailure<T>(string message, T? data = default)
        {
            return new ApiDataResponse<T>
            {
                Success = false,
                Message = message,
                Data = data
            };
        }

        public static ApiErrorResponse Error(string message, string? stackTrace = null)
        {
            return new ApiErrorResponse
            {
                Error = message,
                StackTrace = stackTrace
            };
        }
    }
}
