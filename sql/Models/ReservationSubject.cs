
namespace sql.Models
{
    public class ReservationSubject
    {
        private List<IReservationObserver> _observers = new List<IReservationObserver>();
        public byte EquipmentId { get; set; }
        public int CurrentUsers { get; set; }
        public List<WaitingQueue> WaitingQueue { get; set; } = new List<WaitingQueue>();

        public void Attach(IReservationObserver observer)
        {
            _observers.Add(observer);
        }

        public void Detach(IReservationObserver observer)
        {
            _observers.Remove(observer);
        }

        public void Notify()
        {
            foreach (var observer in _observers)
            {
                observer.Update(this);
            }
        }

        public void StateChanged()
        {
            Notify();
        }
    }
}
