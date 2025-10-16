# -*- coding: utf-8 -*-

# ------------------------------
# 資料庫連線模組
# ------------------------------

import pyodbc  # 匯入 pyodbc 模組，用於連接 SQL Server

def get_connection():
    """
    建立並回傳 SQL Server 資料庫連線
    連線資訊：
        - SQL Server 執行個體: (localdb)\MSSQLLocalDB
        - 資料庫名稱: SmartCommunity
        - 驗證方式: Windows 驗證 (Trusted_Connection=yes)
    :return: pyodbc.Connection 物件
    """
    
    conn = pyodbc.connect(
        "Driver={ODBC Driver 17 for SQL Server};"   # 使用 ODBC Driver 17 連線 SQL Server
        "Server=(localdb)\\MSSQLLocalDB;"           # SQL Server 執行個體名稱
        "Database=SmartCommunity;"                  # 要連線的資料庫名稱
        "Trusted_Connection=yes;"                   # 使用 Windows 驗證登入
    )
    
    return conn                                     # 回傳資料庫連線物件，供其他模組使用