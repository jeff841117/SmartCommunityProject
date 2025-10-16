USE SmartCommunity;
GO

IF OBJECT_ID('DeletePaymentsByRoom', 'P') IS NOT NULL
    DROP PROCEDURE DeletePaymentsByRoom;
GO

CREATE PROCEDURE DeletePaymentsByRoom
    @RoomNumber NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserID INT;
    SELECT @UserID = UserID FROM Users WHERE RoomNumber = @RoomNumber;

    IF @UserID IS NULL
    BEGIN
        PRINT N' 錯誤：找不到房號 ' + @RoomNumber;
        RETURN;
    END

    DELETE FROM Payments
    WHERE BillID IN (SELECT BillID FROM Bills WHERE UserID = @UserID);

    PRINT N' 已刪除房號 ' + @RoomNumber + N' 的所有繳費紀錄。';
END;
GO