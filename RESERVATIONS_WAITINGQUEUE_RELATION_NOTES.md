# Reservations 與 WaitingQueue 的關係說明筆記

這份文件用來說明：
- `Reservations` 這張表負責什麼
- `WaitingQueue` 這張表負責什麼
- 兩張表在不同情境下怎麼連動

目的：
- 避免把「預約」和「排隊」當成同一件事
- 幫助後續資料表重構
- 幫助後續 Service 與 API 拆分

---

## 1. 先講最核心的概念

### `Reservations`

這張表記錄的是：
- 一次正式的設備使用申請
- 或一次未來時段預約
- 或一次已開始的使用紀錄

白話來說：
- 它代表「這個人要用這台設備」這件主體事件

---

### `WaitingQueue`

這張表記錄的是：
- 這個人目前正在排隊等設備

白話來說：
- 它代表「這個人現在還不能用，正在等」這件事

---

## 2. 為什麼要拆成兩張表

因為「預約」和「排隊」不是完全一樣的事。

### 預約主要在回答
- 誰預約了
- 預約哪一台設備
- 預約什麼時間
- 最後有沒有真的開始使用
- 最後是完成、取消，還是轉成其他狀態

### 排隊主要在回答
- 誰正在排隊
- 排哪一台設備
- 排隊順位是多少
- 是立即使用排隊，還是預約到點後轉入排隊
- 後來是排到、取消，還是還在等待

如果硬塞在同一張表，之後很容易出現這些問題：
- 同一筆資料同時像預約又像排隊
- 狀態欄位會越來越混亂
- 排隊順位與預約時間互相干擾
- 未來 API 很難拆

所以建議：
- `Reservations` 管「預約本體」
- `WaitingQueue` 管「排隊行為」

---

## 3. 兩張表的角色分工

### Reservations 的責任

- 記錄一次設備使用申請
- 記錄是否為立即使用或未來預約
- 記錄預約時段
- 記錄實際開始與結束時間
- 記錄最後狀態

### WaitingQueue 的責任

- 記錄是否正在排隊
- 記錄排隊來源
- 記錄排隊順位
- 記錄排隊進入時間
- 記錄是否排到或取消

---

## 4. 關聯方式

建議關係：
- 一筆 `Reservation`
- 在某些情況下，可能對應到 0 或 1 筆目前有效的 `WaitingQueue`

也就是：
- 不是每筆預約都一定有排隊資料
- 只有在需要等待時，才會產生排隊資料

簡化理解：
- 有預約，不一定有排隊
- 有排隊，背後最好能追到是哪一筆預約或使用申請產生的

---

## 5. 建議的關聯欄位

### `WaitingQueue.ReservationId`

用途：
- 指向對應的 `Reservations.Id`

好處：
- 可以追溯這筆排隊是從哪次預約或使用申請產生
- 之後查歷程比較清楚
- 排隊轉成使用中時，知道要更新哪筆預約

---

## 6. 各情境下兩張表怎麼配合

### 情境 A：立即使用，設備有空位

流程：
1. 建立一筆 `Reservations`
2. 狀態直接進入 `InProgress`
3. 不建立 `WaitingQueue`

說明：
- 因為沒有等待，所以只需要預約本體

---

### 情境 B：立即使用，但設備已滿

流程：
1. 建立一筆 `Reservations`
2. 這筆資料代表本次使用申請
3. 同時建立一筆 `WaitingQueue`
4. `WaitingQueue.QueueType = Immediate`
5. 等排到時，更新原本那筆 `Reservations` 為 `InProgress`

說明：
- 這樣可以保留「這次申請是什麼時候提出」的完整紀錄
- 也能獨立管理排隊順位

---

### 情境 C：未來預約成功，到點時有空位

流程：
1. 建立一筆 `Reservations`
2. 狀態為 `Scheduled`
3. 到達預約時間時，更新成 `InProgress`
4. 不建立 `WaitingQueue`

說明：
- 這種情況是正常預約成功，不需要排隊

---

### 情境 D：未來預約成功，但到點時沒空位

流程：
1. 先建立一筆 `Reservations`
2. 狀態為 `Scheduled`
3. 到達預約時間時仍無空位
4. 建立一筆 `WaitingQueue`
5. `WaitingQueue.QueueType = FromReservation`
6. 排隊資料關聯原本那筆 `Reservations`
7. 排到後，更新原本的 `Reservations` 為 `InProgress`

說明：
- 預約本身沒有消失
- 只是進入「等待可開始使用」階段
- 排隊只是附加行為，不是新的主體

---

### 情境 E：取消未來預約

流程：
1. 更新 `Reservations.Status = Cancelled`
2. 不建立 `WaitingQueue`

說明：
- 因為這筆資料從未進入排隊

---

### 情境 F：取消即時排隊

流程：
1. 更新 `WaitingQueue.QueueStatus = Cancelled`
2. 同時更新關聯的 `Reservations.Status = Cancelled`

說明：
- 排隊取消後，這次使用申請也等於結束
- 所以兩張表都要反映結果

---

### 情境 G：取消預約排隊

流程：
1. 更新 `WaitingQueue.QueueStatus = Cancelled`
2. 同時更新 `Reservations.Status = Cancelled`

說明：
- 原本是未來預約
- 但既然最後進入排隊又被取消，主體預約也應視為取消

---

### 情境 H：排到後開始使用

流程：
1. 更新 `WaitingQueue.QueueStatus = Completed`
2. 更新 `WaitingQueue.ConvertedToInProgressAt`
3. 更新 `Reservations.Status = InProgress`
4. 更新 `Reservations.ActualStartTime`

說明：
- 排隊完成不代表整個流程完成
- 而是代表「等待這件事完成」
- 真正的使用仍由 `Reservations` 繼續記錄

---

### 情境 I：使用結束

流程：
1. 更新 `Reservations.Status = Completed`
2. 更新 `Reservations.ActualEndTime`
3. 若有下一位排隊者，再處理下一筆 `WaitingQueue`

說明：
- 使用結束這件事只影響 `Reservations`
- 但可能會連帶觸發下一位排隊者

---

## 7. 誰是主體，誰是附屬

這裡很重要。

建議你之後都用這個觀念思考：

- `Reservations` 是主體
- `WaitingQueue` 是附屬

意思是：
- 一次設備使用申請，核心紀錄永遠在 `Reservations`
- 如果中間需要等待，才額外有 `WaitingQueue`

這樣做的好處是：
- 取消、完成、歷史查詢都比較一致
- API 也比較好設計
- 之後想查某人所有使用歷程時，不會只剩排隊資料

---

## 8. 建議的查詢思路

### 查某會員目前有哪些事情

可以分成三類查：
- `Reservations` 中 `Scheduled`
- `Reservations` 中 `InProgress`
- `WaitingQueue` 中 `Waiting`

這樣可以明確知道：
- 有哪些未來預約
- 有哪些正在使用
- 有哪些還在排隊

---

### 查某次預約完整歷程

以 `Reservations.Id` 為主體：
- 預約何時建立
- 原定幾點開始
- 是否進入排隊
- 排了多久
- 實際幾點開始
- 幾點結束
- 最終是否取消

這時就可以透過 `WaitingQueue.ReservationId` 回查完整歷程。

---

## 9. 後續實作時的注意點

### 1. 不要把排隊順位直接塞進 Reservations

因為順位是排隊行為的一部分，不是預約本體的一部分。

### 2. 不要讓 WaitingQueue 單獨代表整個使用申請

不然後面歷史紀錄會斷裂。

### 3. 取消時要注意兩張表是否都要更新

不是每次都更新兩張表，但要看當時有沒有排隊資料。

### 4. 排隊完成不等於預約完成

排到只是開始使用，真正完成要等使用結束。

---

## 10. 建議的重構理解方式

之後你可以這樣理解：

- `Reservations` 像主單
- `WaitingQueue` 像附屬等待紀錄

主單負責記錄整筆業務的開始到結束  
附屬紀錄負責記錄中間有沒有卡住、等了多久、怎麼排到

---

## 11. 下一步建議

這份關係確認後，最適合往下做的是：

1. 直接整理「資料表重構草案」
2. 對照目前程式，列出現況缺少哪些欄位與流程
3. 再排正式重構順序

如果你想要最務實的下一步，建議先做第 2 項。  
因為這樣你會先知道「目前程式距離目標差多遠」。
