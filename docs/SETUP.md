# 環境與啟動說明

本文件說明如何在本機啟動 `ObserverPattern` 作品集版本。

---

## 1. 環境需求

- Windows
- .NET 8 SDK
- SQL Server LocalDB
- Visual Studio 2022 或 .NET CLI
- 若要測忘記密碼真實寄信：
  - Python
  - Gmail app password

---

## 2. 專案位置

主要 ASP.NET Core 專案位於：

- [sql](../sql)

---

## 3. 資料庫

目前開發 / 測試環境預設使用：

- `MSSQLLocalDB`

請確認資料庫連線與資料表已建立完成。
如需測試寄信、正式展示或特殊本機設定，請另外設定 secrets。

---

## 4. 本機私密設定

本專案不使用 `.env`，而是使用：

- `sql/appsettings.LocalSecrets.json`

請先複製：

- `sql/appsettings.LocalSecrets.example.json`

為：

- `sql/appsettings.LocalSecrets.json`

可設定項目包含：
- Gmail SMTP / app password
- 本機資料庫或其他展示用私密設定

---

## 5. 建置與啟動

進入專案：

```powershell
cd sql
```

建置：

```powershell
dotnet build
```

執行：

```powershell
dotnet run
```

預設本機網址：
- `http://localhost:5099`
- `https://localhost:7141`

---

## 6. Swagger

本機 Swagger 入口：
- `https://localhost:7141/swagger/index.html`

---

## 7. 建議人工驗證

啟動後建議至少確認：
- 登入頁
- 設備預約頁
- 我的預約頁
- 設備管理頁
- 預約與排隊總覽頁
- 管理者操作紀錄頁
- Swagger 頁面

---

## 8. 忘記密碼寄信測試

如果要測試 Gmail SMTP：
1. 先準備 Gmail app password
2. 設定 `appsettings.LocalSecrets.json`
3. 確認以下欄位：
   - `EnablePythonBridge = true`
   - `SenderEmail`
   - `SenderPassword`
   - `SmtpHost`
   - `SmtpPort`

---

## 9. 常見問題

### `localhost:5099 address already in use`
代表舊的 `dotnet` / `sql.exe` 仍在執行，請先停止舊程序再重新啟動。

### 密碼欄位長度不足
目前系統已加入相容處理，會在需要時自動擴充 `member.password` 欄位長度。

### CSS / JS 404
若本機或部署後靜態資源找不到，請確認：
- `wwwroot` 已正確發佈
- 啟動工作目錄正確
- `pm2` 有帶 `--cwd`
