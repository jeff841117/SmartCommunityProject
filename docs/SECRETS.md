# Secrets 設定說明

本專案目前不使用 `.env` 啟動，而是使用本機私密設定檔：

- `sql/appsettings.LocalSecrets.json`

請先複製：

- `sql/appsettings.LocalSecrets.example.json`

為：

- `sql/appsettings.LocalSecrets.json`

## 目前用途

目前這份私密設定主要提供忘記密碼寄信功能使用，包含：

- `SenderEmail`
- `SenderPassword`
- `SmtpHost`
- `SmtpPort`
- `EnablePythonBridge`

## 注意事項

1. `appsettings.LocalSecrets.json` 不可推上 GitHub。
2. 請使用 Gmail app password，不要使用一般登入密碼。
3. 若只想做本機功能驗證、不測試真正寄信，可保留：
   - `FallbackToLogWhenUnavailable = true`
4. 若未設定寄信帳號，忘記密碼流程仍可能使用 fallback log 模式，不代表 SMTP 已真正打通。

## 範例檔位置

- `sql/appsettings.LocalSecrets.example.json`
