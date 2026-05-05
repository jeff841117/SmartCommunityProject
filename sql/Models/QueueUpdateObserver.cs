using Microsoft.AspNetCore.Mvc;
using System;

namespace sql.Models
{
    public class QueueUpdateObserver : IReservationObserver
    {
        public void Update(ReservationSubject subject)
        {
            Console.WriteLine($"設備 {subject.EquipmentId} 狀態更新，當前使用人數: {subject.CurrentUsers}，排隊人數: {subject.WaitingQueue.Count}");
            UpdateQueuePositions(subject);
        }

        private void UpdateQueuePositions(ReservationSubject subject)
        {
            for (int i = 0; i < subject.WaitingQueue.Count; i++)
            {
                subject.WaitingQueue[i].Position = i + 1;
            }
        }
    }
}
