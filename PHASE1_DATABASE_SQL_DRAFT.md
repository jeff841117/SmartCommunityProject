# 第一階段資料庫 SQL 變更草案

這份文件是第一階段重構用的 SQL 方向草案。

目的：
- 把前面確認過的資料表設計，轉成較接近實作的 SQL 變更方向
- 先支援新流程，不急著一次刪掉所有舊欄位
- 降低重構初期的風險

注意：
- 這份是草案，不是最終可直接上正式環境的 migration script
- 真正執行前，仍需先確認你目前資料庫實際欄位與資料型態

---

## 1. 第一階段 SQL 原則

第一階段建議採用以下原則：

1. 先 `ADD COLUMN`，不要急著 `DROP COLUMN`
2. 先新增新表，再逐步改程式接新表
3. 先讓新舊欄位並存一段時間
4. 等程式全面切到新欄位後，再做清理

---

## 2. `member` 資料表變更草案

## 建議新增欄位

```sql
ALTER TABLE member ADD PasswordHash NVARCHAR(255) NULL;
ALTER TABLE member ADD DisplayName NVARCHAR(100) NULL;
ALTER TABLE member ADD IsActive BIT NOT NULL CONSTRAINT DF_member_IsActive DEFAULT 1;
ALTER TABLE member ADD CreatedAt DATETIME NULL;
ALTER TABLE member ADD UpdatedAt DATETIME NULL;
```

## 建議資料初始化

如果現有資料要先過渡，可考慮：

```sql
UPDATE member
SET DisplayName = userName
WHERE DisplayName IS NULL;
```

```sql
UPDATE member
SET CreatedAt = GETDATE()
WHERE CreatedAt IS NULL;
```

```sql
UPDATE member
SET UpdatedAt = GETDATE()
WHERE UpdatedAt IS NULL;
```

## `role` 欄位處理方向

若目前 `role` 是字串，可先保留，後續新增整數欄位做過渡：

```sql
ALTER TABLE member ADD RoleCode INT NULL;
```

建議過渡對應：

```sql
UPDATE member
SET RoleCode =
    CASE role
        WHEN 'user' THEN 1
        WHEN 'manager' THEN 2
        WHEN 'admin' THEN 3
        ELSE 1
    END
WHERE RoleCode IS NULL;
```

---

## 3. `Equipment` 資料表變更草案

## 建議新增欄位

```sql
ALTER TABLE Equipment ADD IsEnabled BIT NOT NULL CONSTRAINT DF_Equipment_IsEnabled DEFAULT 1;
ALTER TABLE Equipment ADD CreatedAt DATETIME NULL;
ALTER TABLE Equipment ADD UpdatedAt DATETIME NULL;
```

## 建議資料初始化

```sql
UPDATE Equipment
SET CreatedAt = GETDATE()
WHERE CreatedAt IS NULL;
```

```sql
UPDATE Equipment
SET UpdatedAt = GETDATE()
WHERE UpdatedAt IS NULL;
```

---

## 4. `Reservations` 資料表變更草案

## 建議新增欄位

```sql
ALTER TABLE Reservations ADD CreatedAt DATETIME NULL;
ALTER TABLE Reservations ADD ReservedStartTime DATETIME NULL;
ALTER TABLE Reservations ADD ReservedEndTime DATETIME NULL;
ALTER TABLE Reservations ADD DurationMinutes INT NULL;
ALTER TABLE Reservations ADD ActualStartTime DATETIME NULL;
ALTER TABLE Reservations ADD ActualEndTime DATETIME NULL;
ALTER TABLE Reservations ADD ReservationType INT NULL;
ALTER TABLE Reservations ADD CancelledAt DATETIME NULL;
ALTER TABLE Reservations ADD CancelReason NVARCHAR(255) NULL;
ALTER TABLE Reservations ADD CancelledByUserId INT NULL;
ALTER TABLE Reservations ADD EndedByType INT NULL;
ALTER TABLE Reservations ADD EndedByUserId INT NULL;
ALTER TABLE Reservations ADD Notes NVARCHAR(500) NULL;
```

## 舊資料過渡初始化

如果目前欄位有：
- `ReservationTime`
- `StartTime`
- `EndTime`

可先做這種過渡：

```sql
UPDATE Reservations
SET CreatedAt = ReservationTime
WHERE CreatedAt IS NULL;
```

```sql
UPDATE Reservations
SET ReservedStartTime = StartTime
WHERE ReservedStartTime IS NULL AND StartTime IS NOT NULL;
```

```sql
UPDATE Reservations
SET ActualStartTime = StartTime
WHERE ActualStartTime IS NULL
  AND StartTime IS NOT NULL
  AND Status = 2;
```

```sql
UPDATE Reservations
SET ActualEndTime = EndTime
WHERE ActualEndTime IS NULL AND EndTime IS NOT NULL;
```

## `ReservationType` 初始化方向

第一階段可先全部補成 `Immediate`：

```sql
UPDATE Reservations
SET ReservationType = 1
WHERE ReservationType IS NULL;
```

## `Status` 說明

第一階段狀態建議：
- `1 = Scheduled`
- `2 = InProgress`
- `3 = Completed`
- `4 = Cancelled`

如果舊資料目前使用的數值不同，執行前要先確認映射。

---

## 5. `WaitingQueue` 資料表變更草案

## 建議新增欄位

```sql
ALTER TABLE WaitingQueue ADD ReservationId INT NULL;
ALTER TABLE WaitingQueue ADD QueueType INT NULL;
ALTER TABLE WaitingQueue ADD QueueStatus INT NULL;
ALTER TABLE WaitingQueue ADD QueuedAt DATETIME NULL;
ALTER TABLE WaitingQueue ADD QueuePosition INT NULL;
ALTER TABLE WaitingQueue ADD ExpectedAvailableTime DATETIME NULL;
ALTER TABLE WaitingQueue ADD ConvertedToInProgressAt DATETIME NULL;
ALTER TABLE WaitingQueue ADD CancelledAt DATETIME NULL;
ALTER TABLE WaitingQueue ADD CancelReason NVARCHAR(255) NULL;
```

## 舊資料過渡初始化

若目前欄位有：
- `QueueTime`
- `Position`

可先初始化：

```sql
UPDATE WaitingQueue
SET QueuedAt = QueueTime
WHERE QueuedAt IS NULL AND QueueTime IS NOT NULL;
```

```sql
UPDATE WaitingQueue
SET QueuePosition = Position
WHERE QueuePosition IS NULL AND Position IS NOT NULL;
```

第一階段可先把現有資料都視為即時排隊：

```sql
UPDATE WaitingQueue
SET QueueType = 1
WHERE QueueType IS NULL;
```

```sql
UPDATE WaitingQueue
SET QueueStatus = 1
WHERE QueueStatus IS NULL;
```

## 關聯欄位 ReservationId

這個欄位第一階段可以先允許為空，因為舊資料可能還沒有辦法完整回填。

後續當新流程上線後，再逐步改為必要欄位。

---

## 6. 新增 `PasswordResetCodes` 資料表草案

```sql
CREATE TABLE PasswordResetCodes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Code NVARCHAR(6) NOT NULL,
    ExpiredAt DATETIME NOT NULL,
    UsedAt DATETIME NULL,
    Status INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
```

## 建議狀態
- `1 = Active`
- `2 = Verified`
- `3 = Expired`
- `4 = Cancelled`

---

## 7. 新增 `AdminActionLogs` 資料表草案

```sql
CREATE TABLE AdminActionLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AdminUserId INT NOT NULL,
    ActionType INT NOT NULL,
    TargetType INT NOT NULL,
    TargetId INT NOT NULL,
    Reason NVARCHAR(255) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
```

---

## 8. 建議索引草案

這一階段可以先補幾個最基本的查詢索引。

```sql
CREATE INDEX IX_Reservations_UserId_Status ON Reservations(UserId, Status);
CREATE INDEX IX_Reservations_EquipmentId_Status ON Reservations(EquipmentId, Status);
CREATE INDEX IX_Reservations_ReservedStartTime ON Reservations(ReservedStartTime);
```

```sql
CREATE INDEX IX_WaitingQueue_EquipmentId_QueueStatus_QueuePosition
ON WaitingQueue(EquipmentId, QueueStatus, QueuePosition);
```

```sql
CREATE INDEX IX_PasswordResetCodes_UserId_Status ON PasswordResetCodes(UserId, Status);
CREATE INDEX IX_PasswordResetCodes_Email_Status ON PasswordResetCodes(Email, Status);
```

---

## 9. 建議外鍵草案

第一階段如果你要保守，可以先不急著全部上外鍵。  
如果資料品質已經夠穩，可以逐步補：

```sql
ALTER TABLE Reservations
ADD CONSTRAINT FK_Reservations_member
FOREIGN KEY (UserId) REFERENCES member(id);
```

```sql
ALTER TABLE Reservations
ADD CONSTRAINT FK_Reservations_Equipment
FOREIGN KEY (EquipmentId) REFERENCES Equipment(Id);
```

```sql
ALTER TABLE WaitingQueue
ADD CONSTRAINT FK_WaitingQueue_member
FOREIGN KEY (UserId) REFERENCES member(id);
```

```sql
ALTER TABLE WaitingQueue
ADD CONSTRAINT FK_WaitingQueue_Equipment
FOREIGN KEY (EquipmentId) REFERENCES Equipment(Id);
```

```sql
ALTER TABLE WaitingQueue
ADD CONSTRAINT FK_WaitingQueue_Reservations
FOREIGN KEY (ReservationId) REFERENCES Reservations(Id);
```

---

## 10. 第一階段暫不執行的 SQL

目前先不要急著做：

```sql
ALTER TABLE Reservations DROP COLUMN StartTime;
ALTER TABLE Reservations DROP COLUMN EndTime;
ALTER TABLE Reservations DROP COLUMN ReservationTime;
ALTER TABLE WaitingQueue DROP COLUMN QueueTime;
ALTER TABLE WaitingQueue DROP COLUMN Position;
ALTER TABLE member DROP COLUMN password;
```

原因：
- 這些要等程式完全改接新欄位後再做
- 太早刪除會讓現有功能先壞掉

---

## 11. 建議執行順序

若之後真的開始改資料庫，建議順序如下：

1. 先備份資料庫
2. 新增 `member` 新欄位
3. 新增 `Equipment` 新欄位
4. 新增 `Reservations` 新欄位
5. 新增 `WaitingQueue` 新欄位
6. 建立 `PasswordResetCodes`
7. 建立 `AdminActionLogs`
8. 跑初始化 `UPDATE`
9. 視情況補索引
10. 程式改接新欄位
11. 最後才考慮刪除舊欄位

---

## 12. 執行前需要再次確認的事

真正執行前，建議再確認：

1. 目前實際資料庫欄位名稱是否完全一致
2. `Status` 舊值目前對應什麼意思
3. `role` 欄位現在存的是哪些字串
4. 是否已有正式資料，不可直接覆蓋
5. 是否要先在測試資料庫演練一次

---

## 13. 下一步建議

這份 SQL 草案確認後，最適合接著做的是：

1. 第一階段 Model / enum 重構清單
2. 第一階段程式改動順序清單
3. 實際 SQL 腳本初版

如果你要先繼續保持「先規劃、先不動碼」，我建議下一步做：
- `第一階段 Model / enum 重構清單`
