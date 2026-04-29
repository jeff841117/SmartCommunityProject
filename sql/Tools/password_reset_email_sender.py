import json
import os
import smtplib
import sys
from email.message import EmailMessage


def main() -> int:
    if len(sys.argv) < 2:
        print("Missing payload path.", file=sys.stderr)
        return 2

    payload_path = sys.argv[1]
    with open(payload_path, "r", encoding="utf-8") as payload_file:
        payload = json.load(payload_file)

    sender_email = os.environ.get("PASSWORD_RESET_SENDER_EMAIL", "")
    sender_password = os.environ.get("PASSWORD_RESET_SENDER_PASSWORD", "")
    smtp_host = os.environ.get("PASSWORD_RESET_SMTP_HOST", "smtp.gmail.com")
    smtp_port = int(os.environ.get("PASSWORD_RESET_SMTP_PORT", "587"))

    if not sender_email or not sender_password:
        print("Missing sender email or password.", file=sys.stderr)
        return 3

    message = EmailMessage()
    message["Subject"] = "智能社區密碼重設驗證碼"
    message["From"] = sender_email
    message["To"] = payload["Email"]
    message.set_content(
        "您好 {user_name}，\n\n"
        "您的密碼重設驗證碼為：{code}\n"
        "驗證碼將於 {expired_at} 失效。\n"
        "若這不是您本人操作，請忽略這封信。\n".format(
            user_name=payload["UserName"],
            code=payload["VerificationCode"],
            expired_at=payload["ExpiredAt"],
        ),
        charset="utf-8",
    )

    with smtplib.SMTP(smtp_host, smtp_port, timeout=30) as smtp:
        smtp.starttls()
        smtp.login(sender_email, sender_password)
        smtp.send_message(message)

    print("Email sent successfully.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
