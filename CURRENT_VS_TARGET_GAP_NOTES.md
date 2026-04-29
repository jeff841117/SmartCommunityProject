# 現況 vs 目標落差清單

這份文件用來整理：
- 目前專案實際做到什麼
- 目標系統希望做到什麼
- 兩者之間差在哪裡

目的：
- 幫助後續安排重構順序
- 避免重構時只靠印象改
- 讓每次修改都能對照明確缺口

---

## 1. 核心結論

目前系統比較接近：
- 單一設備即時使用 / 即時排隊系統

目標系統則是：
- 可擴充為智能社區的多模組系統
- 設備預約模組需支援未來時段預約、即時排隊、預約排隊、管理者介入

所以目前不是小修就能達成，而是需要「先補規格，再重整資料模型，再拆架構」。

---

## 2. 資料表層落差

### 2.1 Reservations 欄位不足

#### 現況
- 目前程式中 `Reservations` 主要使用欄位為：
  - `Id`
  - `EquipmentId`
  - `UserId`
  - `StartTime`
  - `EndTime`
  - `ReservationTime`
  - `Status`

#### 目標
- 需支援：
  - 未來時段預約
  - 實際開始 / 實際結束
  - 取消資訊
  - 管理者或系統介入紀錄

#### 落差
- 缺少 `CreatedAt`
- 缺少 `ReservedStartTime`
- 缺少 `ReservedEndTime`
- 缺少 `DurationMinutes`
- 缺少 `ActualStartTime`
- 缺少 `ActualEndTime`
- 缺少 `ReservationType`
- 缺少 `CancelledAt`
- 缺少 `CancelReason`
- 缺少 `CancelledByUserId`
- 缺少 `EndedByType`
- 缺少 `EndedByUserId`
- 缺少 `Notes`

---

### 2.2 WaitingQueue 欄位不足

#### 現況
- 目前程式中 `WaitingQueue` 主要使用欄位為：
  - `Id`
  - `EquipmentId`
  - `UserId`
  - `QueueTime`
  - `Position`

#### 目標
- 需支援：
  - 立即排隊
  - 預約到點後轉排隊
  - 排隊結果追蹤
  - 與預約主體關聯

#### 落差
- 缺少 `ReservationId`
- 缺少 `QueueType`
- 缺少 `QueueStatus`
- 缺少 `QueuedAt` 命名統一欄位
- 缺少 `ExpectedAvailableTime`
- 缺少 `ConvertedToInProgressAt`
- 缺少 `CancelledAt`
- 缺少 `CancelReason`

---

### 2.3 UserId 使用概念不一致

#### 現況
- Session 中存了 `UserId(int)` 與 `UserName(string)`
- 預約流程中，Controller 目前回傳的是 `UserName` 作為預約識別
- `Reservation.UserId` 在模型中是 `string`

#### 目標
- `UserId` 正式統一使用會員主鍵

#### 落差
- 會員主鍵與顯示名稱沒有完全分開
- Controller、Model、資料存取層對 `UserId` 的型別理解不一致

---

### 2.4 EquipmentId 型別方向已確認，但程式仍需統一

#### 現況
- 目前大量使用 `byte equipmentId`

#### 目標
- `EquipmentId` 維持 `byte`

#### 落差
- 方向已確認
- 但後續資料模型、DTO、Service 都仍需明確統一

---

## 3. 狀態設計落差

### 3.1 ReservationStatus 狀態不足

#### 現況
- 目前程式中的狀態主要是：
  - `Waiting`
  - `InProgress`
  - `Completed`
  - `Cancelled`

#### 目標
- 預約主體至少需支援：
  - `Scheduled`
  - `InProgress`
  - `Completed`
  - `Cancelled`

#### 落差
- 缺少 `Scheduled`
- 現有 `Waiting` 混淆了「排隊」與「預約主體狀態」
- `Status` 目前還沒有正式落實成「資料庫 int、程式 enum」的完整設計

---

### 3.2 排隊狀態尚未獨立

#### 現況
- 排隊主要透過 `WaitingQueue` 表存在
- 但排隊本身沒有完整狀態欄位

#### 目標
- 排隊需可區分：
  - `Waiting`
  - `Completed`
  - `Cancelled`
  - 若有需要可加 `Called`

#### 落差
- 目前取消排隊、排到開始使用、歷史追蹤都缺少正式狀態欄位支撐

---

## 4. 流程規則落差

### 4.1 目前只支援「立即使用 / 立即排隊」

#### 現況
- `CreateReservation` 現在的核心邏輯是：
  - 有空位就直接開始使用
  - 沒空位就加入排隊

#### 目標
- 支援 15 分鐘粒度的未來時段預約
- 例如可預約 `10:15`

#### 落差
- 沒有未來預約的建立流程
- 沒有時段選擇邏輯
- 沒有未來時段可用性檢查
- 沒有預約到點轉狀態邏輯

---

### 4.2 不支援預約到點後轉排隊

#### 現況
- 排隊只處理現在設備已滿的情況

#### 目標
- 若預約者到點時仍無法使用，需併入最後方排隊

#### 落差
- 沒有 `Scheduled -> QueueingFromReservation` 流程
- 沒有 `QueueType = FromReservation`
- 沒有預估到點時是否還需等待的計算邏輯

---

### 4.3 CancelReservation 與未來預約尚未成形

#### 現況
- 目前 `CancelReservation` 實際上是對既有 `Reservations` 做取消
- 但系統內尚未存在「已預約但尚未開始」的完整概念

#### 目標
- `CancelReservation` 要處理未來預約的取消

#### 落差
- 缺少 `Scheduled` 型資料
- 缺少取消未來預約的完整規則

---

### 4.4 EndUsage 只考慮目前使用中狀態

#### 現況
- 使用者可結束使用
- 系統也有過期自動完成

#### 目標
- 支援：
  - 使用者結束
  - 管理者強制結束
  - 系統逾時結束

#### 落差
- 目前尚未正式記錄「由誰結束」
- 缺少管理者強制結束的獨立業務欄位與規則紀錄

---

### 4.5 排隊預估時間計算過於簡化

#### 現況
- 目前使用 `queuePosition * (equipment.AvailableTime / 60)` 推估等待時間

#### 目標
- 能判斷未來預約到點時是否仍需等待
- 能支援多人同時使用設備

#### 落差
- 現行計算方式太粗略
- 沒有結合「可同時使用人數」
- 沒有結合「目前使用中剩餘時間」
- 沒有結合「未來預約時間點」做推估

---

## 5. 登入與會員功能落差

### 5.1 忘記密碼功能尚未完成

#### 現況
- `ForgotPassword` 頁面入口存在
- 但沒有完整找回流程

#### 目標
- 使用 Email 找回密碼
- 系統寄送隨機 6 碼驗證碼
- 驗證碼 10 分鐘有效
- 驗證成功後可重設密碼
- 寄信流程希望用 Python 實作

#### 落差
- 沒有驗證碼資料儲存設計
- 沒有寄信流程
- 沒有驗證流程
- 沒有重設密碼流程
- 沒有驗證碼逾時處理

---

### 5.2 密碼安全性不足

#### 現況
- 目前登入直接比對帳號密碼
- 密碼看起來是明文處理

#### 目標
- 至少應支援較安全的密碼儲存與重設流程

#### 落差
- 尚未看到雜湊機制
- 尚未看到重設密碼安全流程

---

## 6. 權限設計落差

### 6.1 管理者權限尚未完整定義

#### 現況
- 目前有 `manager` / `admin` 角色判斷
- 可進行設備新增、修改、刪除

#### 目標
- 管理者可：
  - 設備設定與修改
  - 強制將會員結束使用

#### 落差
- 缺少正式的管理者強制結束流程
- 缺少操作紀錄
- 缺少是否可取消預約 / 取消排隊 / 調整隊列的規則定義

---

## 7. 架構層落差

### 7.1 DBmanager 過度肥大

#### 現況
- `DBmanager` 同時負責：
  - 帳號
  - 設備
  - 預約
  - 排隊
  - 背景清理
  - 業務規則
  - 資料存取

#### 目標
- 至少應拆分為：
  - Repository / Data Access
  - Reservation Service
  - Queue Service
  - Account Service
  - Equipment Service

#### 落差
- 業務規則與 SQL 完全耦合
- 類別責任過多
- 後續 API 化與測試都會困難

---

### 7.2 Controller 過胖

#### 現況
- `EquipmentController` 同時處理：
  - MVC 頁面
  - AJAX JSON
  - Session 驗證
  - 預約邏輯
  - 排隊邏輯
  - 錯誤訊息整理

#### 目標
- Controller 只負責接收請求與回應
- 業務規則放到 Service

#### 落差
- Controller 目前承擔太多責任
- 不利於後續 API 化

---

### 7.3 依賴注入未落實

#### 現況
- `Program.cs` 有註冊 `DBmanager`
- 但 Controller 裡仍大量直接 `new DBmanager()`

#### 目標
- 透過 DI 管理服務與資料存取物件

#### 落差
- 設計不一致
- 測試與替換實作困難

---

### 7.4 MVC 與 API 尚未分層

#### 現況
- 同一個 Controller 混合頁面與 JSON 回傳

#### 目標
- 之後應逐步 API 化
- 規則由 Service 共用
- 頁面與 API 僅做不同入口

#### 落差
- 缺少清楚的 DTO
- 缺少 API 專用分層
- 回傳格式不穩定，部分資料仍用 `Dictionary<string, object>`

---

## 8. 擴充能力落差

### 8.1 專案目前仍是單功能結構

#### 現況
- 主要圍繞設備預約功能

#### 目標
- 未來擴充成智能社區系統
- 可能新增：
  - 快遞寄送
  - 房間管理系統
  - 公告通知

#### 落差
- 目前資料模型與架構都偏單模組
- 尚未預留多模組切分方式

---

## 9. 已確認方向

以下是目前已確定、可以作為重構基準的內容：

- `UserId` 正式改為會員主鍵
- `EquipmentId` 維持 `byte`
- `Status` 正式採用「資料庫用 int、程式用 enum」
- 不需要預約報到欄位
- 預約系統要支援未來時段預約
- 時段粒度為 15 分鐘
- 排隊需區分即時排隊與預約轉排隊
- 管理者可強制結束使用

---

## 10. 建議的實作優先順序

如果要從這份落差清單往下走，建議順序如下：

1. 先做資料表重構草案
- 先把 `Reservations` / `WaitingQueue` / 找回密碼相關資料需求定清楚

2. 再做狀態與流程對照表
- 把每個流程會改哪些欄位列清楚

3. 再做程式重構計畫
- 先拆預約與排隊服務
- 再拆帳號與設備服務

4. 最後才進入 API 化
- 避免把現在混亂邏輯直接包成 API

---

## 11. 下一步建議

現在最適合接著做的是：

1. 資料表重構草案
2. 流程對欄位異動表
3. 重構分階段計畫

如果你想照最穩的方式前進，我建議下一步做 `資料表重構草案`。
