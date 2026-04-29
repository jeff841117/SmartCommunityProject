using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace sql.Models
{
    public class ReservationObserverManager
    {
        private static ReservationObserverManager _instance;
        private static readonly object _lock = new object();
        private Dictionary<byte, ReservationSubject> _subjects = new Dictionary<byte, ReservationSubject>();
        private DBmanager _dbManager = new DBmanager();

        private ReservationObserverManager()
        {
            var equipments = _dbManager.getEquipment();
            foreach (var equipment in equipments)
            {
                var subject = new ReservationSubject
                {
                    EquipmentId = equipment.Id,
                    CurrentUsers = _dbManager.GetCurrentUsers(equipment.Id),
                    WaitingQueue = _dbManager.GetWaitingQueue(equipment.Id)
                };

                subject.Attach(new QueueUpdateObserver(_dbManager));
                _subjects.Add(equipment.Id, subject);
            }
        }

        public static ReservationObserverManager GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ReservationObserverManager();
                    }
                }
            }
            return _instance;
        }

        public ReservationSubject GetSubject(byte equipmentId)
        {
            _subjects.TryGetValue(equipmentId, out var subject);
            return subject;
        }
    }
}
