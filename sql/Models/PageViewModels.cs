namespace sql.Models
{
    // 這份檔案放的是「整個頁面要用到的資料模型」。
    // 它和 API 回應模型不同，重點不是給前端 AJAX 用，
    // 而是讓 Razor 頁面在一開始載入時，有一個明確的資料入口。
    public class EquipmentManagementPageViewModel
    {
        public List<Equipment> Equipments { get; set; } = new();
        public string CurrentUserName { get; set; } = string.Empty;
        public bool IsManager { get; set; }
        public AddEquipmentFormViewModel AddEquipmentForm { get; set; } = new();
    }

    public class EquipmentReservationPageViewModel
    {
        public List<Equipment> Equipments { get; set; } = new();
        public int SlotIntervalMinutes { get; set; }
        public int AdvanceReservationDays { get; set; }
        public string CurrentUserName { get; set; } = string.Empty;
        public bool IsManager { get; set; }
    }

    public class AccountManagementPageViewModel
    {
        public List<account> Accounts { get; set; } = new();
        public string CurrentUserName { get; set; } = string.Empty;
        public bool IsManager { get; set; }
        public AddAccountFormViewModel AddAccountForm { get; set; } = new();
    }

    public class ReservationManagementPageViewModel
    {
        public ReservationDashboardFilter Filter { get; set; } = new();
        public List<ReservationDashboardFilterOption> ScheduledStatusOptions { get; set; } = new();
        public List<ReservationDashboardFilterOption> WaitingQueueTypeOptions { get; set; } = new();
        public ReservationDashboardResponse Dashboard { get; set; } = new();
        public string CurrentUserName { get; set; } = string.Empty;
        public bool IsManager { get; set; }
    }

    public class AdminActionLogPageViewModel
    {
        public AdminActionLogFilter Filter { get; set; } = new();
        public List<AdminActionFilterOption> ActionTypeOptions { get; set; } = new();
        public List<AdminActionFilterOption> TargetTypeOptions { get; set; } = new();
        public AdminActionLogQueryResult QueryResult { get; set; } = new();
        public string CurrentUserName { get; set; } = string.Empty;
        public bool IsManager { get; set; }
    }

    public class MyReservationsPageViewModel
    {
        public string CurrentUserName { get; set; } = string.Empty;
        public bool IsManager { get; set; }
    }
}
