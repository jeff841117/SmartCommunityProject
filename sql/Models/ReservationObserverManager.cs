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

        private ReservationObserverManager()
        {
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

        // 這一輪讓 ObserverManager 只負責「保存與提供 subject」，
        // 不再自己建立 DBmanager 去查資料。
        // 這樣設備狀態資料由外部提供，責任會更單純。
        public ReservationSubject GetOrCreateSubject(byte equipmentId)
        {
            if (_subjects.TryGetValue(equipmentId, out var subject))
            {
                return subject;
            }

            subject = new ReservationSubject
            {
                EquipmentId = equipmentId
            };
            subject.Attach(new QueueUpdateObserver());
            _subjects[equipmentId] = subject;
            return subject;
        }
    }
}
