# 第一階段重構任務清單

這份文件用來整理：
- 第一階段重構到底要做哪些事
- 每件事為什麼要先做
- 建議的執行順序

目的：
- 避免一開始就直接大改程式
- 先把最影響系統穩定性的部分處理好
- 讓後續第二階段與 API 化更順利

---

## 1. 第一階段目標

第一階段不是把整個智能社區系統一次做完。

第一階段的目標是：
- 先把目前設備預約模組的骨架整理好
- 先讓資料模型、流程、權限方向一致
- 先為未來的前台 / 後台分流打基礎

換句話說：
- 先把地基打穩
- 暫時不追求所有新功能一次完成

---

## 2. 第一階段的範圍

### 本階段要做
- 整理資料表結構
- 整理預約 / 排隊狀態
- 統一 `UserId` 與狀態 enum 概念
- 為忘記密碼預留資料結構
- 為管理者後台預留架構入口
- 開始拆分過胖的資料存取與業務邏輯

### 本階段先不做完
- 快遞寄送模組
- 房間管理模組
- 公告通知模組
- 完整 API 化
- 完整前台 / 後台 UI 美化

---

## 3. 建議任務順序

## 任務 1：凍結目前資料結構與流程基準

### 要做什麼
- 確認目前資料表現況
- 確認目前程式正在使用哪些欄位
- 保留目前可運作版本作為基準

### 為什麼先做
- 避免後面改到一半找不到原本依據
- 方便後續比對新舊流程差異

### 產出
- 已完成的規格文件
- 已完成的落差文件
- 已完成的資料表草案

### 狀態
- 這部分目前已經大致完成

---

## 任務 2：資料表第一階段補欄位 / 新增表

### 要做什麼
- 在不急著刪除舊欄位的前提下，補上新欄位
- 新增支援後續流程的資料表

### 主要項目
- `member` 補：
  - `PasswordHash`
  - `DisplayName`
  - `IsActive`
  - `CreatedAt`
  - `UpdatedAt`

- `Equipment` 補：
  - `IsEnabled`
  - `CreatedAt`
  - `UpdatedAt`

- `Reservations` 補：
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

- `WaitingQueue` 補：
  - `ReservationId`
  - `QueueType`
  - `QueueStatus`
  - `QueuedAt`
  - `QueuePosition`
  - `ExpectedAvailableTime`
  - `ConvertedToInProgressAt`
  - `CancelledAt`
  - `CancelReason`

- 新增：
  - `PasswordResetCodes`
  - `AdminActionLogs`

### 為什麼先做
- 沒有欄位，後面程式就無法正確承接新規格

---

## 任務 3：統一 Model 命名與型別

### 要做什麼
- 統一 `UserId` 使用會員主鍵
- 統一 `EquipmentId` 為 `byte`
- 統一 `Status` 走資料庫 int、程式 enum
- 補上新的 Model / DTO 欄位

### 主要項目
- `Reservation` model 重整
- `WaitingQueue` model 重整
- `Equipment` model 命名調整
- `account` model 命名與欄位整理

### 為什麼這一步重要
- 不先統一型別與命名，後面拆 Service 會一直打架

---

## 任務 4：拆出預約與排隊的核心服務

### 要做什麼
- 不再讓 `DBmanager` 一個類別包全部邏輯
- 先把最複雜的預約 / 排隊拆出來

### 建議先拆的服務
- `ReservationService`
- `QueueService`

### 建議先拆的責任

#### ReservationService
- 建立立即使用預約
- 建立未來時段預約
- 取消預約
- 結束使用
- 自動完成逾時使用

#### QueueService
- 建立即時排隊
- 建立預約轉排隊
- 取消排隊
- 重算順位
- 推進下一位開始使用

### 為什麼這一步重要
- 預約與排隊是目前最容易出錯的核心
- 先拆這兩塊，整體風險最高的地方就會先穩下來

---

## 任務 5：整理登入與會員上下文

### 要做什麼
- 統一登入後 Session 的最小必要資訊
- 為後續忘記密碼與權限分流做準備

### 建議 Session 最小內容
- `UserId`
- `UserName`
- `UserRole`

### 需要同步整理
- `CurrentUser` 概念
- Controller 取得登入者資訊的方式

### 為什麼這一步重要
- 目前 `UserId` / `UserName` 混用，是後面很多流程混亂的根源之一

---

## 任務 6：忘記密碼流程的資料與接口預留

### 要做什麼
- 先把資料表與流程接口留好
- 不一定要在第一階段完整做完寄信細節

### 第一階段至少完成
- `PasswordResetCodes` 表
- 驗證碼流程規格
- Python 寄信方案預留接口

### 可以先不完整做完的部分
- 實際 Email 發送優化
- 完整 UI 流程細節

### 為什麼這一步重要
- 這是登入系統擴充的第一個正式需求

---

## 任務 7：建立前台 / 後台入口結構

### 要做什麼
- 不必馬上做完整介面
- 但要先把路由與角色導向方向定好

### 第一階段建議至少做到
- 登入後依角色導向不同首頁
- 會員前台首頁入口
- 管理者後台首頁入口

### 目標
- 先把「前台」與「後台」概念在程式結構中建立起來

---

## 任務 8：整理 Controller 責任

### 要做什麼
- 讓 Controller 不再直接承接太多邏輯
- 把預約、排隊、錯誤轉換慢慢移到 Service

### 第一階段建議先處理
- `EquipmentController`
- `AccountController`

### 為什麼這一步重要
- 這是後續 API 化的前置條件

---

## 任務 9：補最基本的管理者能力

### 要做什麼
- 在第一階段先確保管理者可以做最重要的控制

### 第一階段建議最少做到
- 管理設備
- 查看預約 / 排隊總覽
- 強制結束使用

### 為什麼這一步重要
- 這是你明確提出的核心管理需求

---

## 任務 10：建立第一階段驗收標準

### 要做什麼
- 定義什麼叫做「第一階段完成」

### 建議驗收標準
- `Reservations` 與 `WaitingQueue` 新欄位已落地
- `UserId` 概念已統一
- `Status` enum 已統一
- 預約與排隊核心邏輯不再全部堆在 `DBmanager`
- 會員 / 管理者入口已分流
- 管理者可強制結束使用
- 忘記密碼資料結構已預留

---

## 4. 第一階段不建議做的事

以下這些事現在先不要混在第一階段一起做：

- 一次全面改 UI
- 一次全面改所有 Controller
- 一次把所有模組都 API 化
- 同時開做快遞、房間管理、公告
- 還沒穩定就先刪掉舊欄位

原因：
- 風險太高
- 很容易把專案拖入長時間不可用狀態

---

## 5. 建議的實際執行順序

如果要真的開始做，我建議照這個順序：

1. 先改資料表
2. 再改 Model / enum
3. 再拆 ReservationService / QueueService
4. 再整理登入與 CurrentUser
5. 再補忘記密碼資料結構
6. 再建立前台 / 後台入口
7. 再整理 Controller
8. 再補管理者必要功能

---

## 6. 一句話版

第一階段不是做完整產品，而是先把：
- 資料表
- 狀態
- 預約 / 排隊核心邏輯
- 前台 / 後台入口

這幾個最關鍵的骨架先立起來。

---

## 7. 下一步建議

這份任務清單確認後，最適合往下做的是：

1. 第一階段資料庫 SQL 變更草案
2. 第一階段 Model / enum 重構清單
3. 第一階段程式改動順序清單

如果你要最直接進入可施工狀態，我建議下一步做：
- `第一階段資料庫 SQL 變更草案`
