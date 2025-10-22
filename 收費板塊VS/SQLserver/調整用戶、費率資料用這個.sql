-- 使用資料庫
USE SmartCommunity;
GO

--------------------------------------------------------
-- 若存在舊表，依外鍵順序刪除
--------------------------------------------------------
IF OBJECT_ID('Payments', 'U') IS NOT NULL DROP TABLE Payments;
IF OBJECT_ID('Bills', 'U') IS NOT NULL DROP TABLE Bills;
IF OBJECT_ID('FeeItems', 'U') IS NOT NULL DROP TABLE FeeItems;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
GO

--------------------------------------------------------
-- 住戶資料表
--------------------------------------------------------
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    UserName NVARCHAR(50) NOT NULL,
    RoomNumber NVARCHAR(10) NOT NULL,
    Phone NVARCHAR(20),
    Email NVARCHAR(50)
);
GO

--------------------------------------------------------
-- 收費項目（管理費 / 水費 / 電費）
--------------------------------------------------------
CREATE TABLE FeeItems (
    FeeItemID INT PRIMARY KEY IDENTITY(1,1),
    ItemName NVARCHAR(50) NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    Unit NVARCHAR(10)
);
GO

--------------------------------------------------------
-- 帳單資料（不含 DueDate）
--------------------------------------------------------
CREATE TABLE Bills (
    BillID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    FeeItemID INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    Status NVARCHAR(20) DEFAULT N'未繳',
    CONSTRAINT FK_Bills_Users FOREIGN KEY(UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_Bills_FeeItems FOREIGN KEY(FeeItemID) REFERENCES FeeItems(FeeItemID)
);
GO

--------------------------------------------------------
-- 繳費紀錄表
--------------------------------------------------------
CREATE TABLE Payments (
    PaymentID INT PRIMARY KEY IDENTITY(1,1),
    BillID INT NOT NULL,
    PayDate DATETIME DEFAULT GETDATE(),
    PayMethod NVARCHAR(20),
    PayAmount DECIMAL(10,2),
    CONSTRAINT FK_Payments_Bills FOREIGN KEY(BillID) REFERENCES Bills(BillID)
);
GO

--------------------------------------------------------
-- 初始化基本資料
--------------------------------------------------------

-- 插入住戶（可自行增加）
INSERT INTO Users (UserName, RoomNumber, Phone, Email)
VALUES 
(N'王小明', 'A101', '0912345678', 'ming@example.com'),
(N'李小華', 'A102', '0922333444', 'hua@example.com');
GO

-- 插入收費項目
INSERT INTO FeeItems (ItemName, UnitPrice, Unit)
VALUES
(N'管理費', 3000, N'每月'),
(N'電費', 5, N'每度'),
(N'水費', 25, N'每度');
GO

-- 建立初始帳單（每位住戶一筆管理費）
INSERT INTO Bills (UserID, FeeItemID, Amount, Status)
SELECT U.UserID, F.FeeItemID, F.UnitPrice, N'未繳'
FROM Users U
CROSS JOIN FeeItems F
WHERE F.ItemName = N'管理費';
GO
