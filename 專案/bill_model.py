# -*- coding: utf-8 -*-

"""
bill_model.py
---------------------------------
提供帳單查詢、繳費、紀錄查詢、後台新增帳單等功能
由 app.py 呼叫
"""

from db_connection import get_connection  # 匯入資料庫連線函式


# ==========================================================
# 1️⃣ 查帳單：僅顯示「未繳」帳單
# ==========================================================
def get_bills_by_user(user_id):
    """
    回傳 (BillID, RoomNumber, ItemName, Amount, Status)，只列未繳帳單
    """
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("""
        SELECT b.BillID, u.RoomNumber, f.ItemName, b.Amount, b.Status
        FROM Bills b
        JOIN Users u ON b.UserID = u.UserID
        JOIN FeeItems f ON b.FeeItemID = f.FeeItemID
        WHERE b.UserID = ? AND b.Status = N'未繳'
    """, (user_id,))
    rows = cursor.fetchall()
    conn.close()
    return rows


# ==========================================================
# 2️⃣ 繳費功能：防止重複、檢查金額、更新狀態
# ==========================================================
def pay_bill(bill_id, pay_method, pay_amount):
    """
    繳費邏輯：
    - 檢查帳單是否存在
    - 防止重複繳款
    - 驗證金額是否正確
    - 寫入 Payments 並更新 Bills 狀態為「已繳」
    """
    conn = get_connection()
    cursor = conn.cursor()

    # 查帳單狀態與金額
    cursor.execute("SELECT Amount, Status FROM Bills WHERE BillID = ?", (bill_id,))
    row = cursor.fetchone()
    if not row:
        conn.close()
        return "錯誤：找不到帳單"

    bill_amount, status = float(row[0]), row[1]

    # 已繳就禁止重複付款
    if status == "已繳":
        conn.close()
        return "此帳單已繳費，無法重複繳款。"

    # 防止在 Payments 表重複新增同筆繳費紀錄
    cursor.execute("SELECT COUNT(*) FROM Payments WHERE BillID = ?", (bill_id,))
    if cursor.fetchone()[0] > 0:
        conn.close()
        return "已有繳費紀錄，無法再次繳費。"

    # 驗證金額是否正確
    if float(pay_amount) != bill_amount:
        conn.close()
        return f"錯誤：繳費金額必須為 {bill_amount} 元"

    # 寫入繳費紀錄
    cursor.execute("""
        INSERT INTO Payments (BillID, PayMethod, PayAmount)
        VALUES (?, ?, ?)
    """, (bill_id, pay_method, pay_amount))

    # 更新帳單狀態
    cursor.execute("UPDATE Bills SET Status = ? WHERE BillID = ?", ('已繳', bill_id))

    conn.commit()
    conn.close()

    return f"操作成功，繳費方式：{pay_method}，金額：{pay_amount} 元，請等待負責人員完成確認，謝謝!"


# ==========================================================
# 3️⃣ 查繳費紀錄：僅顯示最新一筆
# ==========================================================
def get_payments_by_user(user_id):
    """
    查詢指定住戶的繳費紀錄：
    - 每筆帳單只顯示最新一筆繳費
    - 依照日期（PayDate）由新到舊排序
    - 顯示住戶姓名與房號
    """
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("""
        SELECT 
            p.PaymentID, 
            u.UserName, 
            u.RoomNumber,
            f.ItemName, 
            p.PayAmount, 
            CONVERT(VARCHAR(10), p.PayDate, 120) AS PayDate,  -- ✅ 格式化為 YYYY-MM-DD
            p.PayMethod
        FROM (
            SELECT 
                PaymentID, BillID, PayAmount, PayDate, PayMethod,
                ROW_NUMBER() OVER (PARTITION BY BillID ORDER BY PayDate DESC) AS rn
            FROM Payments
        ) AS p
        JOIN Bills b ON p.BillID = b.BillID
        JOIN FeeItems f ON b.FeeItemID = f.FeeItemID
        JOIN Users u ON b.UserID = u.UserID
        WHERE b.UserID = ? AND p.rn = 1
        ORDER BY p.PayDate DESC
    """, (user_id,))

    payments = cursor.fetchall()
    conn.close()
    return payments

# ==========================================================
# 4️⃣ 後台新增帳單
# ==========================================================
def create_bill_for_user(user_id, fee_item, amount=None):
    """
    後台新增帳單：
    - 管理費固定 3000 元
    - 水費、電費由管理員輸入金額
    """
    conn = get_connection()
    cursor = conn.cursor()

    # 決定金額
    if fee_item == "管理費":
        amount = 3000
    elif amount is None:
        conn.close()
        return "錯誤：水費與電費必須輸入金額"

    # 查收費項目
    cursor.execute("SELECT FeeItemID FROM FeeItems WHERE ItemName = ?", (fee_item,))
    row = cursor.fetchone()
    if not row:
        conn.close()
        return f"錯誤：收費項目 {fee_item} 不存在，請先建立"
    fee_item_id = row[0]

    # 新增帳單
    cursor.execute("""
        INSERT INTO Bills (UserID, FeeItemID, Amount, Status)
        VALUES (?, ?, ?, N'未繳')
    """, (user_id, fee_item_id, amount))

    conn.commit()
    conn.close()
    return f"已成功新增 {fee_item} 帳單給住戶 {user_id}，金額 {amount} 元"
