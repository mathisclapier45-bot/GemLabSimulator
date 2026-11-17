"""Sending mail. In local mode mails are only stored (var/outbox + /dev/courriels page)."""
import smtplib
import sys
from email.message import EmailMessage

from . import config
from .db import now


def send(conn, to_addr, subject, body):
    if config.MAIL_MODE == "smtp":
        msg = EmailMessage()
        msg["From"] = config.MAIL_FROM
        msg["To"] = to_addr
        msg["Subject"] = subject
        msg.set_content(body)
        try:
            with smtplib.SMTP(config.SMTP_HOST, config.SMTP_PORT, timeout=15) as smtp:
                smtp.starttls()
                if config.SMTP_USER:
                    smtp.login(config.SMTP_USER, config.SMTP_PASSWORD)
                smtp.send_message(msg)
        except Exception as exc:  # a failed mail must not break the page
            sys.stderr.write("MAIL ERROR: {!r}\n".format(exc))
        return
    conn.execute("INSERT INTO outbox (to_addr, subject, body, created_at) VALUES (?,?,?,?)",
                 (to_addr, subject, body, now()))
