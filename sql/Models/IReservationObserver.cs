using Microsoft.AspNetCore.Mvc;

namespace sql.Models
{
    public interface IReservationObserver
    {
        void Update(ReservationSubject subject);
    }
}
