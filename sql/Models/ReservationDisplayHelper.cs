namespace sql.Models
{
    // 這個 helper 的目的是把「狀態代碼 -> 顯示文字 / 顏色」集中管理。
    // 之後前台與後台都盡量從同一個地方取文案，避免同一個狀態出現兩種說法。
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
                (int)ReservationStatus.Scheduled => "已預約未開始",
                (int)ReservationStatus.ScheduledQueueExpected => "已預約，預估到點仍需排隊",
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
                2 => "預約到點後轉排隊",
                _ => "一般即時排隊"
            };
        }
    }
}
