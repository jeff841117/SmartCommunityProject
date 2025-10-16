# -*- coding: utf-8 -*-

# -*- coding: utf-8 -*-
"""
app.py
==========
智能社區 - 收費系統 (Flask 版本)

功能說明：
住戶註冊：新增使用者資料，自動建立管理費帳單
查帳單：查詢該住戶所有帳單（未繳／已繳）
繳費：更新帳單狀態為「已繳」並新增繳費紀錄
查繳費紀錄：顯示該住戶所有繳費紀錄
"""

from flask import Flask, render_template, request, redirect, url_for
from bill_model import get_bills_by_user, pay_bill, get_payments_by_user
from db_connection import get_connection

app = Flask(__name__)
app.config['JSON_AS_ASCII'] = False  # 避免中文亂碼

# -------------------------------------------------------------
# 首頁：功能選單（選擇住戶ID 或 點註冊）
# -------------------------------------------------------------
@app.route("/")
def home():
    return render_template("menu.html")

# -------------------------------------------------------------
# 住戶註冊功能
# -------------------------------------------------------------
@app.route("/register", methods=["GET", "POST"])
def register():
    """
    註冊新住戶（輸入姓名、房號、電話、Email）
    註冊成功後自動建立一筆「管理費」帳單
    """
    if request.method == "POST":
        name = request.form.get("name")
        room = request.form.get("room")
        phone = request.form.get("phone")
        email = request.form.get("email")

        conn = get_connection()
        cursor = conn.cursor()

        # 新增住戶資料
        cursor.execute("""
            INSERT INTO Users (UserName, RoomNumber, Phone, Email)
            VALUES (?, ?, ?, ?)
        """, (name, room, phone, email))

        # 取得剛建立的 UserID
        cursor.execute("SELECT @@IDENTITY")
        new_user_id = int(cursor.fetchone()[0])

        # 自動新增「管理費帳單」
        cursor.execute("""
            INSERT INTO Bills (UserID, FeeItemID, Amount, Status)
            SELECT ?, FeeItemID, UnitPrice, N'未繳'
            FROM FeeItems WHERE ItemName = N'管理費'
        """, (new_user_id,))

        conn.commit()
        conn.close()

        # 顯示註冊結果
        return render_template("result.html", message=f"註冊成功！新住戶 ID 為 {new_user_id}")

    # GET 方法 → 顯示註冊表單
    return render_template("register.html")

# -------------------------------------------------------------
# 查帳單
# -------------------------------------------------------------
@app.route("/bills", methods=["POST"])
def bills():
    room_number = request.form.get("room_number")
    if not room_number:
        return redirect(url_for("home"))

    # 在資料庫查出對應的 user_id
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT UserID FROM Users WHERE RoomNumber = ?", ("A" + room_number,))
    row = cursor.fetchone()
    conn.close()

    if not row:
        return render_template("bills.html", user_id=None, bills=None)

    user_id = row[0]
    bills = get_bills_by_user(user_id)
    return render_template("bills.html", user_id=user_id, bills=bills)

# -------------------------------------------------------------
# 繳費
# -------------------------------------------------------------
@app.route("/pay", methods=["POST"])
def pay():
    """
    使用者在帳單頁面選擇繳費方式並送出
    系統會：
      1. 驗證金額是否正確
      2. 新增繳費紀錄 (Payments)
      3. 更新帳單狀態為「已繳」
    """
    user_id    = request.form.get("user_id")
    bill_id    = request.form.get("bill_id")
    pay_method = request.form.get("pay_method")
    pay_amount = request.form.get("pay_amount")

    # 嚴格檢查（避免 None 轉型失敗）
    if not all([user_id, bill_id, pay_method, pay_amount]):
        return render_template("result.html", message="參數不足，請回上一頁重試", user_id=user_id or 0)

    # 轉型前再簡單驗證
    try:
        bill_id_int = int(bill_id)
        pay_amount_float = float(pay_amount)
    except Exception:
        return render_template("result.html", message="資料格式錯誤，請重新操作", user_id=int(user_id) if user_id else 0)

    result = pay_bill(bill_id_int, str(pay_method), pay_amount_float)
    return render_template("result.html", message=result, user_id=int(user_id))

# -------------------------------------------------------------
# 選擇付款方式後顯示付款頁面
# -------------------------------------------------------------
@app.route("/pay_method", methods=["POST"])
def pay_method():
    user_id   = request.form.get("user_id")
    bill_id   = request.form.get("bill_id")
    pay_method = request.form.get("pay_method")
    pay_amount = request.form.get("pay_amount")

    # 防呆：必須都有
    if not all([user_id, bill_id, pay_method, pay_amount]):
        return render_template("result.html", message="參數不足，請回上一頁重試", user_id=user_id or 0)

    return render_template(
        "pay_method.html",
        user_id=user_id,
        bill_id=bill_id,
        pay_method=pay_method,
        pay_amount=pay_amount
    )

# -------------------------------------------------------------
# 查繳費紀錄
# -------------------------------------------------------------
@app.route("/payments", methods=["POST"])
def payments():
    room_number = request.form.get("room_number")
    if not room_number:
        return redirect(url_for("home"))

    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT UserID FROM Users WHERE RoomNumber = ?", ("A" + room_number,))
    row = cursor.fetchone()
    conn.close()

    if not row:
        return render_template("payments.html", user_id=None, payments=None)

    user_id = row[0]
    records = get_payments_by_user(user_id)
    return render_template("payments.html", user_id=user_id, payments=records)

if __name__ == "__main__":
    app.run(debug=True)

# -------------------------------------------------------------
# 程式進入點
# -------------------------------------------------------------
if __name__ == "__main__":
    app.run(debug=True)