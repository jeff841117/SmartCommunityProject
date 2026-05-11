# 畫面清單與檔名規範

本文件整理作品集 README 建議使用的畫面清單，以及建議的檔名與正式採用版本。

---

## README 正式展示版本

目前 README 正式採用的圖片如下：

1. `login.png`
2. `reservation.png`
3. `my-reservations-1.png`
4. `equipment-admin.png`
5. `dashboard-2.png`
6. `action-logs.png`
7. `swagger.png`
8. `forgot-password.png`
9. `reschedule-or-chain.png`

---

## 候選版本比較建議

### Dashboard
- `dashboard.png`：空狀態版，不建議當 README 主展示圖
- `dashboard-1.png`：只有未來預約資料，可作備用
- `dashboard-2.png`：同時有未來預約、使用中與排隊資訊，最適合作為 README 正式版

### My Reservations
- `my-reservations.png`：偏空狀態
- `my-reservations-1.png`：展示使用中資訊，最適合作為 README 正式版
- `my-reservations-2.png`：展示未來預約，可作備用
- `my-reservations-3.png`：展示排隊中，可作備用

### Swagger
- `swagger.png`：總覽版，最適合作為 README 正式版
- `swagger-1.png`：展開 endpoint 的細節版，可作補充說明圖

### 管理者進階畫面
- `forgot-password.png`：適合展示忘記密碼與驗證碼重設流程
- `reschedule-or-chain.png`：適合展示設備預約 / 排隊鏈
- `reschedule-or-chain-1.png`：適合展示調整未來預約時段的管理介面

---

## 建議放置路徑

請將截圖放在：

```text
docs/screenshots/
```

---

## 每張圖建議呈現的內容

### 1. 登入頁
- 專案標題
- 帳號 / 密碼欄位
- 作品集版視覺風格

### 2. 設備預約頁
- 設備卡片
- 立即使用按鈕
- 預約時間查詢
- 篩選區塊

### 3. 我的預約頁
- 使用中 / 未來預約 / 排隊中的任一代表性狀態
- 清楚的時間與操作按鈕

### 4. 設備管理頁
- 設備表格
- 編輯 / 刪除
- 設備種類欄位

### 5. 預約與排隊總覽頁
- 設備摘要
- 未來預約
- 使用中
- 排隊中
- 高風險設備篩選

### 6. 管理者操作紀錄頁
- 篩選表單
- 分頁
- 匯出功能

### 7. Swagger
- Swagger 首頁
- 至少展開 1~2 個代表性 endpoint（可額外用 `swagger-1.png` 補充）

### 8. 忘記密碼
- 驗證碼申請
- 重設密碼欄位
- 完整流程視覺

### 9. 預約鏈 / 調整預約
- 管理者可查看設備鏈
- 或展示調整預約時段的預覽與操作能力
