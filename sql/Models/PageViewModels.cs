namespace sql.Models
{
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
        public AccountManagementFilter Filter { get; set; } = new();
        public AccountManagementQueryResult QueryResult { get; set; } = new();
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

    public class AccountManagementFilter
    {
        public string SearchField { get; set; } = "userName";
        public string? Keyword { get; set; }
        public bool ExactMatch { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class AccountManagementQueryResult
    {
        public List<account> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => PageSize <= 0
            ? 1
            : (int)Math.Ceiling(TotalCount / (double)PageSize);

        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    public class AccountManagementOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public static class AccountManagementOptions
    {
        public static List<AccountManagementOption> GetSearchFieldOptions()
        {
            return
            [
                new AccountManagementOption { Value = "userName", Text = "帳號" },
                new AccountManagementOption { Value = "email", Text = "電子郵箱" },
                new AccountManagementOption { Value = "phone", Text = "手機號碼" }
            ];
        }

        public static List<AccountManagementOption> GetRoleOptions()
        {
            return
            [
                new AccountManagementOption { Value = "manager-group", Text = "管理員" },
                new AccountManagementOption { Value = "user", Text = "普通會員" }
            ];
        }

        public static List<AccountManagementOption> GetStatusOptions()
        {
            return
            [
                new AccountManagementOption { Value = "active", Text = "啟用" },
                new AccountManagementOption { Value = "inactive", Text = "停用" }
            ];
        }
    }

    public static class AccountDisplayHelper
    {
        public static bool IsManagerRole(string? role)
        {
            return string.Equals(role, "manager", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetRoleText(string? role)
        {
            return IsManagerRole(role) ? "管理員" : "普通會員";
        }

        public static string GetStatusText(bool isActive)
        {
            return isActive ? "啟用" : "停用";
        }
    }
}
