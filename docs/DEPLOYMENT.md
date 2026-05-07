# 部署說明

本專案作品集版本目前的正式站部署方式，不是 IIS 站台直接執行，而是：

- 發佈到資料夾
- 使用 `pm2` 啟動 `dotnet sql.dll`
- 透過 `cloudflared` 對外提供 `https://api.jeffsideproject.com`

## 1. 發佈資料夾

正式發佈目錄：

```text
C:\inetpub\jeffsideproject
```

## 2. 發佈方式

Visual Studio：
1. 對 `sql` 專案按右鍵
2. 選 `發佈`
3. 目標選 `資料夾`
4. 路徑填：

```text
C:\inetpub\jeffsideproject
```

也可使用 CLI：

```powershell
dotnet publish -c Release -o C:\inetpub\jeffsideproject
```

## 3. 啟動方式

```powershell
pm2 start "dotnet" --name "api" -x --cwd "C:\inetpub\jeffsideproject" -- "C:\inetpub\jeffsideproject\sql.dll" --urls "http://localhost:5089"
pm2 save
```

## 4. Swagger 入口

- `https://api.jeffsideproject.com/swagger/index.html`

## 5. 驗證重點

部署後至少確認：

1. 登入頁可開
2. `Equipment/Reservation` 可開
3. `Equipment/MyReservations` 可開
4. 管理者頁面可開
5. Swagger 可開
6. `/css/...`、`/js/...` 靜態資源不再 404

## 6. 常見問題

### 靜態資源 404
請先確認：
- 發佈目錄正確
- `pm2` 啟動時有帶 `--cwd`
- `wwwroot` 已完整發佈

### 發佈時 DLL 被鎖住
通常代表舊的 `dotnet` 程序還在執行。可先停止 `pm2` 的 `api` 行程後再重新發佈。
