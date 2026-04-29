# 實際資料庫變更清單

這份文件是把「資料表重構草案」轉成較實際的資料庫變更方向。

目的：
- 讓後續真正修改資料庫時有清單可照
- 幫助判斷哪些是新增、哪些是修改、哪些是保留

---

## 1. 變更原則

目前建議先採用：
- 以「保留舊表、逐步補欄位 / 新增表」為主
- 先不要一開始就大量刪欄位
- 等新流程穩定後，再處理舊欄位淘汰

原因：
- 風險較低
- 比較適合目前這種持續開發中的專案

---

## 2. `member` 資料表

### 建議保留
- `id`
- `userName`
- `email`
- `phone`
- `role`

### 建議新增
- `PasswordHash`
- `DisplayName`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`

### 建議調整
- `password`
  - 後續改為以 `PasswordHash` 為主
  - `password` 可先保留過渡，等登入流程完成後再淘汰

- `role`
  - 後續改成 int enum 邏輯
  - 資料內容需統一對應：
    - `1 = Resident`
    - `2 = Manager`
    - `3 = Admin`

### 建議後續淘汰
- `password`
  - 等新登入與忘記密碼流程穩定後移除

---

## 3. `Equipment` 資料表

### 建議保留
- `Id`
- `equipmentName`
- `MaxUsers`
- `AvailableTime`
- `OpenTime`
- `CloseTime`

### 建議新增
- `IsEnabled`
- `CreatedAt`
- `UpdatedAt`

### 建議調整
- `equipmentName`
  - 程式命名建議逐步統一為 `EquipmentName`

---

## 4. `Reservations` 資料表

### 建議保留
- `Id`
- `EquipmentId`
- `UserId`
- `Status`

### 建議新增
- `CreatedAt`
- `ReservedStartTime`
- `ReservedEndTime`
- `DurationMinutes`
- `ActualStartTime`
- `ActualEndTime`
- `ReservationType`
- `CancelledAt`
- `CancelReason`
- `CancelledByUserId`
- `EndedByType`
- `EndedByUserId`
- `Notes`

### 建議調整
- `StartTime`
  - 後續角色應拆為：
    - `ReservedStartTime`
    - `ActualStartTime`

- `EndTime`
  - 後續角色應拆為：
    - `ReservedEndTime`
    - `ActualEndTime`

- `ReservationTime`
  - 後續建議改為 `CreatedAt`

- `Status`
  - 正式採用資料庫 `int`
  - 程式採用 enum
  - 建議值：
    - `1 = Scheduled`
    - `2 = InProgress`
    - `3 = Completed`
    - `4 = Cancelled`

### 建議後續淘汰
- `StartTime`
- `EndTime`
- `ReservationTime`

前提：
- 新欄位正式接手流程後再移除

---

## 5. `WaitingQueue` 資料表

### 建議保留
- `Id`
- `EquipmentId`
- `UserId`

### 建議新增
- `ReservationId`
- `QueueType`
- `QueueStatus`
- `QueuedAt`
- `QueuePosition`
- `ExpectedAvailableTime`
- `ConvertedToInProgressAt`
- `CancelledAt`
- `CancelReason`

### 建議調整
- `QueueTime`
  - 改名方向：`QueuedAt`

- `Position`
  - 改名方向：`QueuePosition`

### 建議後續淘汰
- `QueueTime`
- `Position`

前提：
- 程式已全面改用新欄位後再移除

---

## 6. 建議新增資料表：`PasswordResetCodes`

### 新增原因
- 支援 Email 找回密碼
- 儲存驗證碼與有效時間

### 建議欄位
- `Id`
- `UserId`
- `Email`
- `Code`
- `ExpiredAt`
- `UsedAt`
- `Status`
- `CreatedAt`

---

## 7. 建議新增資料表：`AdminActionLogs`

### 新增原因
- 記錄管理者操作
- 保留後台操作歷史

### 建議欄位
- `Id`
- `AdminUserId`
- `ActionType`
- `TargetType`
- `TargetId`
- `Reason`
- `CreatedAt`

---

## 8. 建議分階段執行

### 第一階段：新增，不刪除

- `member` 補新欄位
- `Equipment` 補新欄位
- `Reservations` 補新欄位
- `WaitingQueue` 補新欄位
- 新增 `PasswordResetCodes`
- 新增 `AdminActionLogs`

### 第二階段：程式改接新欄位

- 修改 Model
- 修改 DB 存取
- 修改流程邏輯

### 第三階段：淘汰舊欄位

- 移除不再使用的舊欄位
- 清理舊流程與舊命名

---

## 9. 優先順序建議

若要開始真的施工，建議優先順序如下：

1. `Reservations` 補欄位
2. `WaitingQueue` 補欄位
3. `PasswordResetCodes` 新增
4. `member` 補安全相關欄位
5. `AdminActionLogs` 新增
6. `Equipment` 補管理欄位

---

## 10. 已確認事項

- `UserId` 使用會員主鍵
- `EquipmentId` 維持 `byte / tinyint`
- `Status` 使用資料庫 `int`、程式 `enum`
- 不需要預約報到欄位
- 後續系統需要後台頁面與前台頁面分流

---

## 11. 下一步建議

這份確認後，最適合往下做的是：

1. 前台 / 後台架構筆記
2. 第一階段重構任務清單
3. 實際 SQL 變更草案
