# CI / Docker 規劃說明

本文件說明目前作品集專案為何尚未正式加入 CI、Dockerfile 與 docker-compose，並整理後續可行方向。

## 目前狀態

目前專案重點放在：

1. 完成設備預約 / 排隊系統重構
2. 整理前後台與 Swagger 展示
3. 補齊 README、部署文件、測試文件與架構說明

目前尚未正式提供：

- Dockerfile
- docker-compose.yml
- GitHub Actions CI workflow

這不是遺漏，而是因為目前專案仍依賴：

- Windows / LocalDB 測試流程
- 本機 Gmail app password 設定
- pm2 + dotnet + cloudflared 的作品集部署方式

因此若直接補上未驗證的 Docker / CI，反而容易讓作品集看起來完整但實際無法執行。

## 建議的下一步方向

### 1. 先補可驗證的 CI

建議第一步可以先補最小 CI：

- restore
- build
- 測試（若之後補上自動化測試）

範例方向：

- `actions/setup-dotnet`
- `dotnet restore`
- `dotnet build`
- `dotnet test`

### 2. Docker 化前先決定資料庫策略

若要補 Docker，需先決定：

- 是否改用 SQL Server container
- 是否改用外部資料庫連線
- 是否保留 LocalDB 只作本機開發

### 3. 作品集展示優先順序

目前更建議的順序是：

1. 先補 README / Setup / Deployment / Testing / Architecture
2. 再補 screenshots
3. 再補最小 CI
4. 最後才補 Dockerfile / docker-compose

## 面試時可以怎麼說

可以誠實說明：

- 專案目前以作品集展示與系統重構為優先
- 已先補齊文件、部署方式與 Swagger API
- CI / Docker 已有規劃，但會在資料庫與部署策略穩定後再補上

這樣比放一份沒有驗證過的 Docker / CI 更有說服力。
