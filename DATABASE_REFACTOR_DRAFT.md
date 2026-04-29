# 資料表重構草案

這份文件是根據目前已確認的需求，整理出的第一版資料表重構方向。

目的：
- 讓資料表能支援未來時段預約
- 讓資料表能支援即時排隊與預約轉排隊
- 讓登入、會員、設備、管理者操作都有擴充空間
- 作為後續真正修改 SQL / Model / Service 的依據

---

## 1. 重構原則

這次資料表重構先遵守 4 個原則：

1. 主體資料和附屬行為分開
- 預約主體放 `Reservations`
- 排隊行為放 `WaitingQueue`

2. 狀態要能支援未來功能
- 不能只夠目前即時預約使用

3. 所有關聯盡量用主鍵
- `UserId` 用會員主鍵
- `EquipmentId` 用設備主鍵

4. 先支援核心流程，再考慮細節優化
- 先讓資料能正確表達流程
- 不急著一次塞所有進階功能

---

## 2. 建議保留與調整的主要資料表

目前建議的核心表如下：

### 保留並重構
- `member`
- `Equipment`
- `Reservations`
- `WaitingQueue`

### 建議新增
- `PasswordResetCodes`
- `AdminActionLogs`

---

## 3. member 重構草案

用途：
- 記錄住戶 / 會員基本資料
- 作為登入、預約、管理操作的使用者來源

### 建議欄位

#### `Id`
- 型別：`int`
- 說明：會員主鍵

#### `UserName`
- 型別：`nvarchar(50)`
- 說明：登入帳號或顯示帳號

#### `PasswordHash`
- 型別：`nvarchar(255)`
- 說明：密碼雜湊值

#### `DisplayName`
- 型別：`nvarchar(100)`
- 說明：顯示名稱
- 備註：
  - 若目前沒有額外需求，也可先沿用 `UserName`

#### `Email`
- 型別：`nvarchar(100)`
- 說明：找回密碼寄信用

#### `Phone`
- 型別：`nvarchar(20)`

#### `Role`
- 型別：`int`
- 說明：角色 enum
- 建議值：
  - `1 = Resident`
  - `2 = Manager`
  - `3 = Admin`

#### `IsActive`
- 型別：`bit`
- 說明：帳號是否啟用

#### `CreatedAt`
- 型別：`datetime`

#### `UpdatedAt`
- 型別：`datetime`

### 與現況差異

目前已有：
- `id`
- `userName`
- `password`
- `email`
- `phone`
- `role`

需要調整：
- `password` 改為 `PasswordHash`
- `role` 建議逐步轉成 int enum
- 補 `IsActive`
- 補 `CreatedAt`
- 補 `UpdatedAt`

---

## 4. Equipment 重構草案

用途：
- 記錄設備設定

### 建議欄位

#### `Id`
- 型別：`tinyint`
- 說明：設備主鍵

#### `EquipmentName`
- 型別：`nvarchar(100)`

#### `MaxUsers`
- 型別：`tinyint`
- 說明：同時可使用人數

#### `AvailableTime`
- 型別：`smallint`
- 說明：單次可使用分鐘數

#### `OpenTime`
- 型別：`time`

#### `CloseTime`
- 型別：`time`

#### `IsEnabled`
- 型別：`bit`
- 說明：設備是否開放使用

#### `CreatedAt`
- 型別：`datetime`

#### `UpdatedAt`
- 型別：`datetime`

### 與現況差異

目前已有：
- `Id`
- `equipmentName`
- `MaxUsers`
- `AvailableTime`
- `OpenTime`
- `CloseTime`

建議補：
- `IsEnabled`
- `CreatedAt`
- `UpdatedAt`

---

## 5. Reservations 重構草案

用途：
- 記錄一次完整的設備使用申請
- 支援立即使用、未來預約、完成、取消

### 建議欄位

#### `Id`
- 型別：`int`
- 說明：預約主鍵

#### `EquipmentId`
- 型別：`tinyint`
- 說明：對應設備

#### `UserId`
- 型別：`int`
- 說明：對應會員主鍵

#### `CreatedAt`
- 型別：`datetime`
- 說明：建立這筆預約的時間

#### `ReservedStartTime`
- 型別：`datetime`
- 說明：預約開始時間

#### `ReservedEndTime`
- 型別：`datetime`
- 說明：預約預計結束時間

#### `DurationMinutes`
- 型別：`int`
- 說明：本次預約可使用分鐘數

#### `ActualStartTime`
- 型別：`datetime`
- 可空：是
- 說明：實際開始使用時間

#### `ActualEndTime`
- 型別：`datetime`
- 可空：是
- 說明：實際結束使用時間

#### `Status`
- 型別：`int`
- 說明：預約狀態 enum
- 建議值：
  - `1 = Scheduled`
  - `2 = InProgress`
  - `3 = Completed`
  - `4 = Cancelled`

#### `ReservationType`
- 型別：`int`
- 說明：預約建立類型
- 建議值：
  - `1 = Immediate`
  - `2 = Future`

#### `CancelledAt`
- 型別：`datetime`
- 可空：是

#### `CancelReason`
- 型別：`nvarchar(255)`
- 可空：是

#### `CancelledByUserId`
- 型別：`int`
- 可空：是

#### `EndedByType`
- 型別：`int`
- 可空：是
- 建議值：
  - `1 = User`
  - `2 = Admin`
  - `3 = System`

#### `EndedByUserId`
- 型別：`int`
- 可空：是

#### `Notes`
- 型別：`nvarchar(500)`
- 可空：是

### 與現況差異

目前已有：
- `Id`
- `EquipmentId`
- `UserId`
- `StartTime`
- `EndTime`
- `ReservationTime`
- `Status`

建議調整：
- `ReservationTime` 拆解為 `CreatedAt`
- `StartTime` 改為更清楚的：
  - `ReservedStartTime`
  - `ActualStartTime`
- `EndTime` 改為更清楚的：
  - `ReservedEndTime`
  - `ActualEndTime`
- 補 `DurationMinutes`
- 補 `ReservationType`
- 補取消與結束操作欄位

---

## 6. WaitingQueue 重構草案

用途：
- 記錄等待設備的排隊資訊
- 支援即時排隊與預約轉排隊

### 建議欄位

#### `Id`
- 型別：`int`

#### `ReservationId`
- 型別：`int`
- 說明：關聯 `Reservations.Id`

#### `EquipmentId`
- 型別：`tinyint`

#### `UserId`
- 型別：`int`

#### `QueueType`
- 型別：`int`
- 說明：排隊來源
- 建議值：
  - `1 = Immediate`
  - `2 = FromReservation`

#### `QueueStatus`
- 型別：`int`
- 說明：排隊狀態
- 建議值：
  - `1 = Waiting`
  - `2 = Completed`
  - `3 = Cancelled`

#### `QueuedAt`
- 型別：`datetime`
- 說明：加入排隊時間

#### `QueuePosition`
- 型別：`int`

#### `ExpectedAvailableTime`
- 型別：`datetime`
- 可空：是

#### `ConvertedToInProgressAt`
- 型別：`datetime`
- 可空：是

#### `CancelledAt`
- 型別：`datetime`
- 可空：是

#### `CancelReason`
- 型別：`nvarchar(255)`
- 可空：是

### 與現況差異

目前已有：
- `Id`
- `EquipmentId`
- `UserId`
- `QueueTime`
- `Position`

建議調整：
- `QueueTime` 改名為 `QueuedAt`
- `Position` 改名為 `QueuePosition`
- 補 `ReservationId`
- 補 `QueueType`
- 補 `QueueStatus`
- 補 `ExpectedAvailableTime`
- 補 `ConvertedToInProgressAt`
- 補取消欄位

---

## 7. PasswordResetCodes 新增草案

用途：
- 支援 Email 找回密碼
- 儲存驗證碼與有效時間

### 建議欄位

#### `Id`
- 型別：`int`

#### `UserId`
- 型別：`int`

#### `Email`
- 型別：`nvarchar(100)`

#### `Code`
- 型別：`nvarchar(6)`
- 說明：6 碼數字驗證碼

#### `ExpiredAt`
- 型別：`datetime`
- 說明：10 分鐘到期

#### `UsedAt`
- 型別：`datetime`
- 可空：是

#### `Status`
- 型別：`int`
- 建議值：
  - `1 = Active`
  - `2 = Verified`
  - `3 = Expired`
  - `4 = Cancelled`

#### `CreatedAt`
- 型別：`datetime`

### 備註

- Python 寄信流程可讀寫這張表
- 驗證成功後再允許重設密碼

---

## 8. AdminActionLogs 新增草案

用途：
- 記錄管理者操作
- 後續方便追蹤責任與還原問題

### 建議欄位

#### `Id`
- 型別：`int`

#### `AdminUserId`
- 型別：`int`

#### `ActionType`
- 型別：`int`
- 建議值可包含：
  - 強制結束使用
  - 取消預約
  - 取消排隊
  - 修改設備

#### `TargetType`
- 型別：`int`
- 建議值可包含：
  - Member
  - Equipment
  - Reservation
  - Queue

#### `TargetId`
- 型別：`int`

#### `Reason`
- 型別：`nvarchar(255)`
- 可空：是

#### `CreatedAt`
- 型別：`datetime`

---

## 9. 建議關聯

### member
- `member.Id` -> `Reservations.UserId`
- `member.Id` -> `WaitingQueue.UserId`
- `member.Id` -> `PasswordResetCodes.UserId`
- `member.Id` -> `AdminActionLogs.AdminUserId`

### Equipment
- `Equipment.Id` -> `Reservations.EquipmentId`
- `Equipment.Id` -> `WaitingQueue.EquipmentId`

### Reservations
- `Reservations.Id` -> `WaitingQueue.ReservationId`

---

## 10. 建議的 enum 規劃

### ReservationStatus
- `1 = Scheduled`
- `2 = InProgress`
- `3 = Completed`
- `4 = Cancelled`

### ReservationType
- `1 = Immediate`
- `2 = Future`

### QueueType
- `1 = Immediate`
- `2 = FromReservation`

### QueueStatus
- `1 = Waiting`
- `2 = Completed`
- `3 = Cancelled`

### MemberRole
- `1 = Resident`
- `2 = Manager`
- `3 = Admin`

### EndedByType
- `1 = User`
- `2 = Admin`
- `3 = System`

---

## 11. 建議的重構方式

這份草案不建議一次硬切。

比較穩的方式是：

1. 先調整 Model 與規格文件
2. 再建立新欄位或新表
3. 再修改資料存取層
4. 最後修正 Controller / Service 流程

---

## 12. 目前不先處理的部分

這版先不處理：
- 快遞寄送詳細資料表
- 房間管理資料表
- 公告通知資料表

原因：
- 先把設備預約模組穩定
- 再擴充其他模組會比較安全

---

## 13. 下一步建議

這份草案確認後，最適合往下做的是：

1. 畫出資料表之間的簡易 ER 關係
2. 列出目前實際資料庫要新增 / 修改 / 刪除哪些欄位
3. 拆成第一階段重構任務清單

如果你要最實用的下一步，我建議直接做第 2 項：
- 「實際資料庫變更清單」
