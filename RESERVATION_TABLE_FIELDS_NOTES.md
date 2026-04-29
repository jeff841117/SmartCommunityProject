# 預約資料表欄位清單

這份文件用來整理「預約模組」後續重構時，資料表應具備的核心欄位。

目的：
- 讓預約、排隊、取消、結束使用有明確資料依據
- 支援未來時段預約
- 支援即時排隊與預約排隊
- 支援管理者介入處理

---

## 1. 先講結論

依照目前已確認需求，後續不建議只用現在單一的 `Reservations` 結構硬撐。

比較合理的方向通常是：
- `Reservations`：記錄預約與使用主體
- `WaitingQueue`：記錄排隊資訊

也就是：
- 預約資料與排隊資料應分開
- 但兩者之間需要能關聯

---

## 2. Reservations 建議欄位

以下是第一版建議欄位清單。

### 主鍵與關聯欄位

#### `Id`
- 型別建議：`int`
- 用途：預約主鍵

#### `EquipmentId`
- 型別建議：`int`
- 用途：對應設備

#### `UserId`
- 型別建議：`int`
- 用途：對應會員

### 建立與時段欄位

#### `CreatedAt`
- 型別建議：`datetime`
- 用途：這筆預約是什麼時候建立的

#### `ReservedStartTime`
- 型別建議：`datetime`
- 用途：使用者預約的開始時間
- 說明：
  - 立即使用時，通常會等於建立時間附近
  - 未來預約時，會是使用者選的時段，例如 `2026-04-17 10:15`

#### `ReservedEndTime`
- 型別建議：`datetime`
- 用途：預計結束時間
- 說明：
  - 可由 `ReservedStartTime + DurationMinutes` 計算
  - 也可直接存欄位，方便查詢與比對

#### `DurationMinutes`
- 型別建議：`int`
- 用途：本次預約預計可使用分鐘數
- 說明：
  - 可記錄當下設備規則
  - 避免日後設備規則變更後，舊資料失真

### 實際使用欄位

#### `ActualStartTime`
- 型別建議：`datetime`，可為空
- 用途：實際開始使用時間
- 說明：
  - `Scheduled` 時通常為空
  - 開始使用後才會填入

#### `ActualEndTime`
- 型別建議：`datetime`，可為空
- 用途：實際結束使用時間

### 狀態欄位

#### `Status`
- 型別建議：資料庫用 `int`，程式用 `enum`
- 用途：記錄當前狀態
- 建議值：
  - `Scheduled`
  - `InProgress`
  - `Completed`
  - `Cancelled`
- 白話說明：
  - 這個欄位就是拿來判斷「這筆預約現在走到哪一步」
  - 例如：
    - `Scheduled` = 已預約但還沒開始
    - `InProgress` = 正在使用
    - `Completed` = 已結束
    - `Cancelled` = 已取消
- 主要用途：
  - 判斷使用者現在可以做什麼操作
  - 判斷系統接下來該怎麼處理這筆資料
  - 提供前端顯示正確按鈕與狀態文字
- 實作建議：
  - 資料庫欄位存整數
  - C# 程式內用 `enum ReservationStatus`
  - enum 數值需明確固定，不可隨意重排

#### `ReservationType`
- 型別建議：`varchar` 或 `int enum`
- 用途：記錄這筆預約的建立來源
- 建議值：
  - `Immediate`
  - `Future`
- 說明：
  - 方便區分立即使用與未來時段預約

### 取消與管理欄位

#### `CancelledAt`
- 型別建議：`datetime`，可為空
- 用途：取消時間

#### `CancelReason`
- 型別建議：`nvarchar`
- 用途：取消原因
- 說明：
  - 可記錄使用者取消、系統取消或管理者取消原因

#### `CancelledByUserId`
- 型別建議：`int`，可為空
- 用途：由哪位使用者或管理者取消

#### `EndedByType`
- 型別建議：`varchar` 或 `int enum`
- 用途：記錄結束方式
- 建議值：
  - `User`
  - `Admin`
  - `System`

#### `EndedByUserId`
- 型別建議：`int`，可為空
- 用途：如果是使用者或管理者結束，記錄操作者

### 備註欄位

#### `Notes`
- 型別建議：`nvarchar`
- 用途：保留備註
- 說明：
  - 可用於管理者操作說明
  - 可用於異常狀況記錄

---

## 3. WaitingQueue 建議欄位

這張表建議獨立存在，不要把排隊混在 `Reservations` 內硬處理。

### 主鍵與關聯欄位

#### `Id`
- 型別建議：`int`
- 用途：排隊主鍵

#### `ReservationId`
- 型別建議：`int`，可為空或必填視設計而定
- 用途：關聯到原始預約資料
- 說明：
  - 若為立即排隊，是否先建立 `Reservations` 主體，可再決定
  - 若為預約到點後轉排隊，則很適合關聯原本的預約資料

#### `EquipmentId`
- 型別建議：`int`
- 用途：對應設備

#### `UserId`
- 型別建議：`int`
- 用途：對應會員

### 排隊資訊欄位

#### `QueueType`
- 型別建議：`varchar` 或 `int enum`
- 用途：區分排隊來源
- 建議值：
  - `Immediate`
  - `FromReservation`

#### `QueueStatus`
- 型別建議：`varchar` 或 `int enum`
- 用途：排隊狀態
- 建議值：
  - `Waiting`
  - `Called`
  - `Cancelled`
  - `Completed`

#### `QueuedAt`
- 型別建議：`datetime`
- 用途：進入排隊的時間

#### `QueuePosition`
- 型別建議：`int`
- 用途：當下順位

#### `ExpectedAvailableTime`
- 型別建議：`datetime`，可為空
- 用途：系統推估可能可使用的時間

### 排隊結果欄位

#### `ConvertedToInProgressAt`
- 型別建議：`datetime`，可為空
- 用途：從排隊轉成使用中的時間

#### `CancelledAt`
- 型別建議：`datetime`，可為空
- 用途：取消排隊時間

#### `CancelReason`
- 型別建議：`nvarchar`，可為空
- 用途：取消排隊原因

---

## 4. 哪些欄位是目前最重要、應先決定的

如果你不想一次看太多，可以先抓最關鍵的第一批欄位。

### Reservations 第一批必備欄位

- `Id`
- `EquipmentId`
- `UserId`
- `CreatedAt`
- `ReservedStartTime`
- `ReservedEndTime`
- `DurationMinutes`
- `ActualStartTime`
- `ActualEndTime`
- `Status`
- `ReservationType`

### WaitingQueue 第一批必備欄位

- `Id`
- `ReservationId`
- `EquipmentId`
- `UserId`
- `QueueType`
- `QueueStatus`
- `QueuedAt`
- `QueuePosition`

---

## 5. 建議的狀態搭配方式

### Reservations

建議主要處理：
- `Scheduled`
- `InProgress`
- `Completed`
- `Cancelled`

### WaitingQueue

建議主要處理：
- `Immediate` 排隊
- `FromReservation` 排隊

也就是說：
- 「預約本體」放在 `Reservations`
- 「排隊行為」放在 `WaitingQueue`

這樣未來比較容易擴充，也比較適合 API 化。

---

## 6. 為什麼不要只靠目前欄位

如果只靠現在的欄位，之後會遇到這些問題：

- 無法清楚區分預約時間與實際使用時間
- 無法支援 15 分鐘時段預約
- 無法記錄預約到點後轉排隊
- 無法知道取消是誰做的
- 無法知道結束使用是使用者、管理者還是系統處理
- 未來 API 回傳很難穩定

所以這一步其實是在幫未來的重構減少返工。

---

## 7. 目前仍待確認的欄位問題

以下幾點還可以再一起確認：

### 1. `UserId` 是否正式改為會員主鍵

目前系統混用了 `int UserId` 與 `string UserName` 概念。

建議方向：
- 正式以 `UserId` 主鍵作為所有關聯欄位
- `UserName` 只做顯示用途

目前確認：
- `UserId` 改為會員主鍵

### 2. `EquipmentId` 是否仍用 `byte`

目前程式裡設備 `Id` 常用 `byte`。

若未來是完整智能社區系統，建議考慮：
- 改為 `int`

目前確認：
- `EquipmentId` 仍使用 `byte`
- 原因：目前僅用於設備編號，數量上限 255 以現階段需求足夠

### 3. `Status` 要用字串還是整數

建議：
- 程式內用 enum
- 資料庫用 int

重點是要全系統一致。

目前確認：
- `Status` 正式採用「資料庫用 int、程式用 enum」

### 4. 是否需要預約報到欄位

若未來設計成「預約到了還要報到」，可能還要加：
- `CheckedInAt`
- `NoShowAt`

目前確認：
- 不需要預約報到欄位
- 因此現階段不新增：
  - `CheckedInAt`
  - `NoShowAt`

---

## 8. 下一步建議

你確認完這份欄位清單後，接下來最適合做的是：

1. 整理「WaitingQueue 資料表欄位是否需要再細化」
2. 畫出「Reservations 與 WaitingQueue 的關係」
3. 對照目前程式，列出哪些欄位現在完全沒有
4. 再往下做資料表重構草案
