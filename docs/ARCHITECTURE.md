# 系統架構說明

本文件補充作品集版本的系統架構與資料流，讓面試官能快速理解：

- 前台 / 後台如何分工
- MVC 與 API 如何共用同一套服務層
- 預約 / 排隊邏輯如何往下走到資料庫
- 忘記密碼寄信與背景服務如何接入

---

## 1. 整體系統架構

```mermaid
flowchart LR
    User["一般會員 / 管理者"] --> UI["Razor Pages / MVC Views"]
    User --> Swagger["Swagger UI"]

    UI --> Controllers["Controllers"]
    Swagger --> ApiControllers["API Controllers"]

    Controllers --> Services["Services"]
    ApiControllers --> Services

    Services --> Repositories["Repositories"]
    Repositories --> Database["SQL Server LocalDB / SQL Database"]

    Services --> Background["ExpiredReservationCheckerService"]
    Services --> Notifier["EquipmentStateNotifier / Observer"]
    Services --> MailBridge["PasswordResetEmailBridge"]
    MailBridge --> Python["Python SMTP Sender"]

    PM2["pm2"] --> App["dotnet sql.dll"]
    Cloudflared["Cloudflare Tunnel"] --> App
    App --> UI
    App --> Swagger
```

---

## 2. 專案分層

### Controllers
負責：
- 處理前台 / 後台頁面請求
- 回傳 View 或 API response
- 驗證登入與管理者權限

### Services
負責：
- 封裝業務規則
- 協調預約、排隊、管理者干預、寄信等流程
- 統一前台與 Swagger API 的行為

### Repositories
負責：
- 存取資料庫
- 執行查詢、更新、建立、刪除
- 保留必要的舊資料相容修正

### Models / ViewModels
負責：
- 頁面輸入輸出模型
- API request / response 模型
- 預約 / 排隊 / 管理者操作資料形狀

---

## 3. 預約 / 排隊主流程

```mermaid
flowchart TD
    A["使用者選擇設備"] --> B{"是否立即使用?"}
    B -->|是| C["檢查目前可用名額"]
    C -->|有空位| D["建立 InProgress 預約"]
    C -->|無空位| E["建立 WaitingQueue 排隊紀錄"]

    B -->|否，選未來時間| F["檢查未來時段保留名額"]
    F --> G["推算使用中 / 排隊對該時段的影響"]
    G --> H{"到時是否仍可能需排隊?"}
    H -->|否| I["建立 Scheduled 預約"]
    H -->|是| J["提示使用者確認"]
    J --> K["建立 ScheduledQueueExpected 預約"]

    D --> L["到期 / 手動結束使用"]
    L --> M["QueueProcessingCoordinator 遞補下一位"]
    E --> M
    K --> N["到點後轉 InProgress 或 Waiting"]
```

---

## 4. 忘記密碼流程

```mermaid
flowchart TD
    A["輸入 Email"] --> B["產生 6 碼驗證碼"]
    B --> C["寫入 PasswordResetCodes"]
    C --> D["PasswordResetEmailBridge"]
    D --> E["Python SMTP Sender"]
    E --> F["寄出 Gmail 驗證碼"]
    F --> G["使用者輸入驗證碼與新密碼"]
    G --> H["驗證碼是否有效 / 是否過期"]
    H --> I["更新密碼雜湊"]
    I --> J["標記驗證碼已使用"]
```

---

## 5. 作品集亮點

1. 同時保留 MVC 頁面與 Swagger API 展示入口。
2. 預約 / 排隊規則不是單純 CRUD，而有：
   - 立即使用
   - 排隊
   - 未來預約
   - 預約轉排隊
   - 管理者介入
3. 忘記密碼流程包含：
   - 驗證碼資料表
   - Gmail SMTP
   - Python 寄信橋接
4. 後台包含：
   - 設備管理
   - 帳戶管理
   - 預約與排隊總覽
   - 管理者操作紀錄

---

## 6. 待補畫面資源

README 會引用：
- `docs/screenshots/login.png`
- `docs/screenshots/reservation.png`
- `docs/screenshots/my-reservations.png`
- `docs/screenshots/equipment-admin.png`
- `docs/screenshots/dashboard.png`
- `docs/screenshots/action-logs.png`
- `docs/screenshots/swagger.png`

目前若尚未放入圖檔，README 可先保留說明與連結，之後再補實際截圖。
