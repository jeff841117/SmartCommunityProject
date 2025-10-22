-- 使用資料庫
USE SmartCommunity;
GO

--------------------------------------------------------
-- 住戶資料表
--------------------------------------------------------
IF OBJECT_ID('Users', 'U') IS NOT NULL
    DROP TABLE Users;
GO

CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),     -- 主鍵，自動遞增
    UserName NVARCHAR(50) NOT NULL,           -- 住戶姓名
    RoomNumber NVARCHAR(10) NOT NULL,         -- 房號 (例：A101)
    Phone NVARCHAR(20),                       -- 聯絡電話
    Email NVARCHAR(50)                        -- 電子郵件
);
GO

--------------------------------------------------------
-- 收費項目 (例：管理費、水費、電費)
--------------------------------------------------------
IF OBJECT_ID('FeeItems', 'U') IS NOT NULL
    DROP TABLE FeeItems;
GO

CREATE TABLE FeeItems (
    FeeItemID INT PRIMARY KEY IDENTITY(1,1),  -- 主鍵
    ItemName NVARCHAR(50) NOT NULL,           -- 項目名稱 (管理費、水費...)
    UnitPrice DECIMAL(10,2) NOT NULL,         -- 單價
    Unit NVARCHAR(10) NULL                    -- 計算單位 (每月、每度...)
);
GO

--------------------------------------------------------
-- 帳單資料
--------------------------------------------------------
IF OBJECT_ID('Bills', 'U') IS NOT NULL
    DROP TABLE Bills;
GO

CREATE TABLE Bills (
    BillID INT PRIMARY KEY IDENTITY(1,1),     -- 主鍵
    UserID INT NOT NULL,                      -- 對應住戶
    FeeItemID INT NOT NULL,                   -- 對應收費項目
    Amount DECIMAL(10,2) NOT NULL,            -- 總金額
    DueDate DATE NOT NULL,                    -- 到期日
    Status NVARCHAR(20) DEFAULT '未繳',       -- 狀態 (未繳 / 已繳)
    CONSTRAINT FK_Bills_Users FOREIGN KEY(UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_Bills_FeeItems FOREIGN KEY(FeeItemID) REFERENCES FeeItems(FeeItemID)
);
GO

--------------------------------------------------------
-- 繳費紀錄
--------------------------------------------------------
IF OBJECT_ID('Payments', 'U') IS NOT NULL
    DROP TABLE Payments;
GO

CREATE TABLE Payments (
    PaymentID INT PRIMARY KEY IDENTITY(1,1),  -- 主鍵
    BillID INT NOT NULL,                      -- 對應帳單
    PayDate DATETIME DEFAULT GETDATE(),       -- 繳費日期
    PayMethod NVARCHAR(20),                   -- 繳費方式 (現金、轉帳、LinePay)
    PayAmount DECIMAL(10,2),                  -- 實際繳費金額
    CONSTRAINT FK_Payments_Bills FOREIGN KEY(BillID) REFERENCES Bills(BillID)
);
GO