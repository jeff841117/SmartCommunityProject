# 智能社區收費系統 (SmartCommunity)

Flask + SQL Server 收費系統，用於展示以下功能：

* 使用者註冊與帳單資料管理
* 帳單查詢與繳費（現金 / ATM / LINE Pay）
* 顯示 QR Code（模擬 LINE Pay）

## 📁 專案結構

├── app.py                     	 ← 主 Flask 程式（入口）
├── bill\_model.py              	 ← 處理帳單邏輯
├── db\_connection.py           	 ← SQL Server 連線設定
├── templates/
│   ├── menu.html             	 ← 首頁/選單
│   ├── bills.html            	 ← 帳單查詢（僅顯示未繳）
│   ├── pay\_method.html       	 ← 顯示付款資訊或 QR Code
│   ├── payments.html        	 ← 繳費紀錄
│   ├── result.html           	 ← 繳費成功提示
├── static/
│   └── linepay\_qr.png        	 ← LINE Pay 測試 QR Code
└── SQLserver/
├── 住戶資料表.sql         		 ← 建表＋資料邏輯
├── 帳單計算.sql      	  	 ← 自動計算帳單邏輯
├── 新增帳單用這個.sql     	 	 ← 手動新增帳單
├── 調整用戶、費率資料用這個.sql	 ← 調整用程序
├── 刪除用戶帳單繳費紀錄.sql          ← 刪除用程序
├── 刪除繳費紀錄用這個.sql	         ← 刪除用程序
├── 測試查詢.sql		   	 ← 查詢(住戶表、帳單表、繳費金額(倍率))表



## 🚀 執行方式

1. 啟動伺服器：python app.py
2. 啟動SQLserver 執行"調整用戶、費率資料用這個.sql"
3. 開啟瀏覽器訪問 http://127.0.0.1:5000

## 💳 測試流程

1. 選擇使用者 → 進入功能選單
2. 查詢帳單 → 選擇付款方式 → 確認
3. 查看繳費紀錄（可看到最近繳費方式與時間）

## 🧾 注意

* 管理費固定 3000 元
* 使用SQL可調整住戶資料及收費倍率
* LINE Pay 僅為測試 QR Code，不具實際付款功能
