# 測試說明

本文件說明作品集版本目前建議的最小驗證流程。

## 1. 基本建置

```powershell
cd sql
dotnet build
```

預期結果：
- Build 成功
- 無新增 error

## 2. 本機啟動

```powershell
dotnet run
```

預設測試網址：
- `http://localhost:5099`
- `https://localhost:7141`

## 3. 人工測試重點

### 會員功能
- 登入
- 預約設備
- 我的預約
- 取消預約
- 取消排隊
- 忘記密碼流程

### 管理者功能
- 帳戶管理
- 設備管理
- 預約與排隊總覽
- 管理者操作紀錄
- 強制結束使用
- 取消未來預約
- 調整預約時段

## 4. Swagger API 驗證

至少驗證以下 API：
- `POST /api/account/login`
- `GET /api/account/current-user`
- `GET /api/reservations/me`
- `GET /api/reservations/planning`
- `GET /api/queues/{equipmentId}`
- `GET /api/admin/reservations/dashboard`
- `GET /api/admin/action-logs`

Swagger UI：
- `https://localhost:7141/swagger/index.html`

## 5. 注意事項

1. 若 `localhost:5099` 顯示埠號被占用，先停止舊的 `dotnet` / `sql.exe`。
2. 若忘記密碼要測真實寄信，需先設定 `appsettings.LocalSecrets.json`。
3. 測試資料若被修改，需在展示前清理成作品集用資料。
