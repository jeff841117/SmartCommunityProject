using System.ComponentModel.DataAnnotations;

namespace sql.Models
{
    public class ApiLoginRequest
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class ApiForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ApiResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string VerificationCode { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ApiImmediateReservationRequest
    {
        [Required]
        public byte EquipmentId { get; set; }
    }

    public class ApiFutureReservationRequest
    {
        [Required]
        public byte EquipmentId { get; set; }

        [Required]
        public string ReservationDate { get; set; } = string.Empty;

        [Required]
        public string SelectedSlotStartTime { get; set; } = string.Empty;

        public bool ConfirmQueueExpected { get; set; }
    }
}
