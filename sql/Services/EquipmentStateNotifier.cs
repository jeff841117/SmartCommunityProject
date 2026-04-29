using sql.Models;
using sql.Repositories;

namespace sql.Services
{
    // EquipmentStateNotifier 負責把設備目前狀態同步到 observer subject。
    // 這一輪再往前一步：
    // 它不再直接呼叫舊 DBmanager 的 GetCurrentUsers / GetWaitingQueue，
    // 而是用共用 helper 自己查出快照資料。
    public class EquipmentStateNotifier
    {
        private readonly DBmanager _dbManager;

        public EquipmentStateNotifier(DBmanager dbManager)
        {
            _dbManager = dbManager;
        }

        public void Notify(byte equipmentId)
        {
            var observerManager = ReservationObserverManager.GetInstance();
            var subject = observerManager.GetOrCreateSubject(equipmentId);

            using var connection = _dbManager.CreateConnection();
            connection.Open();

            subject.CurrentUsers = RepositorySqlHelper.GetCurrentUsers(connection, equipmentId);
            subject.WaitingQueue = RepositorySqlHelper.GetWaitingQueue(connection, equipmentId);
            subject.StateChanged();
        }
    }
}
