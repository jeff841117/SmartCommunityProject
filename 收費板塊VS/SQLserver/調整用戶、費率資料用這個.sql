-- 使用資料庫
USE SmartCommunity;
GO

/* 依相依關係清表：付款 → 帳單 → 費用 → 住戶 */
DROP TABLE IF EXISTS Payments;
DROP TABLE IF EXISTS Bills;
DROP TABLE IF EXISTS FeeItems;
DROP TABLE IF EXISTS Users;
GO

/* 住戶 */
CREATE TABLE dbo.Users (
    UserID     INT IDENTITY(1,1) PRIMARY KEY,
    UserName   NVARCHAR(50) NOT NULL,
    RoomNumber NVARCHAR(10) NOT NULL,
    Phone      NVARCHAR(20) NULL,
    Email      NVARCHAR(50) NULL
);
GO

/* 收費項目（管理費 / 水費 / 電費） */
CREATE TABLE dbo.FeeItems (
    FeeItemID  INT IDENTITY(1,1) PRIMARY KEY,
    ItemName   NVARCHAR(50) NOT NULL,
    UnitPrice  DECIMAL(10,2) NOT NULL,
    [Unit]     NVARCHAR(10) NULL
);
GO

/* 帳單（含級聯：刪住戶→連帶刪帳單；刪費用項目不級聯，避免誤刪） */
CREATE TABLE dbo.Bills (
    BillID     INT IDENTITY(1,1) PRIMARY KEY,
    UserID     INT NOT NULL,
    FeeItemID  INT NOT NULL,
    Amount     DECIMAL(10,2) NOT NULL,
    [Status]   NVARCHAR(20) NOT NULL CONSTRAINT DF_Bills_Status DEFAULT N'未繳',
    CONSTRAINT FK_Bills_Users
        FOREIGN KEY(UserID)   REFERENCES dbo.Users(UserID)   ON DELETE CASCADE,
    CONSTRAINT FK_Bills_FeeItems
        FOREIGN KEY(FeeItemID) REFERENCES dbo.FeeItems(FeeItemID)
);
GO

/* 繳費紀錄（含級聯：刪帳單→連帶刪付款） */
CREATE TABLE dbo.Payments (
    PaymentID  INT IDENTITY(1,1) PRIMARY KEY,
    BillID     INT NOT NULL,
    PayDate    DATETIME NOT NULL CONSTRAINT DF_Payments_PayDate DEFAULT GETDATE(),
    PayMethod  NVARCHAR(20) NULL,
    PayAmount  DECIMAL(10,2) NULL,
    CONSTRAINT FK_Payments_Bills
        FOREIGN KEY(BillID) REFERENCES dbo.Bills(BillID) ON DELETE CASCADE
);
GO

/* 種子資料：僅保留費用項目（避免每次重建都塞住戶與帳單） */
IF NOT EXISTS (SELECT 1 FROM dbo.FeeItems)
BEGIN
    INSERT INTO dbo.FeeItems (ItemName, UnitPrice, [Unit]) VALUES
        (N'管理費', 3000, N'每月'),
        (N'電費',      5, N'每度'),
        (N'水費',     25, N'每度');
END
GO
