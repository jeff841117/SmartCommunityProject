namespace sql.Models
{
    // 這個 helper 專門把資料庫裡的狀態數字，
    // 轉成前後台都能共用的中文文字與樣式 class。
    public static class ReservationDisplayHelper
    {
        public static string GetStatusText(int status)
        {
            return status switch
            {
                (int)ReservationStatus.Waiting => "排隊中",
                (int)ReservationStatus.InProgress => "使用中",
                (int)ReservationStatus.Completed => "已完成",
                (int)ReservationStatus.Cancelled => "已取消",
                (int)ReservationStatus.Scheduled => "已預約",
                (int)ReservationStatus.ScheduledQueueExpected => "已預約，預計需排隊",
                _ => "未知狀態"
            };
        }

        public static string GetStatusCssClass(int status)
        {
            return status switch
            {
                (int)ReservationStatus.Waiting => "status-waiting",
                (int)ReservationStatus.InProgress => "status-inprogress",
                (int)ReservationStatus.Completed => "status-completed",
                (int)ReservationStatus.Cancelled => "status-cancelled",
                (int)ReservationStatus.Scheduled => "status-scheduled",
                (int)ReservationStatus.ScheduledQueueExpected => "status-queue-expected",
                _ => "status-unknown"
            };
        }

        public static string GetQueueTypeText(int queueType)
        {
            return queueType switch
            {
                2 => "預約轉排隊",
                _ => "一般即時排隊"
            };
        }
    }
}
