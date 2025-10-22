USE SmartCommunity;
GO

IF OBJECT_ID('AddBill', 'P') IS NOT NULL
    DROP PROCEDURE AddBill;
GO

CREATE PROCEDURE AddBill
    @UserName NVARCHAR(50),
    @ItemName NVARCHAR(50),
    @Usage DECIMAL(10,2) = NULL,  -- 水/電度數，管理費可留空
    @DueDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @DueDate IS NULL
        SET @DueDate = DATEADD(DAY, 30, GETDATE()); -- 預設到期日 = 今天 + 30天

    DECLARE @UserID INT, @FeeItemID INT, @Amount DECIMAL(10,2);

    -- 找使用者與項目ID
    SELECT @UserID = UserID FROM Users WHERE UserName = @UserName;
    SELECT @FeeItemID = FeeItemID FROM FeeItems WHERE ItemName = @ItemName;

    IF @UserID IS NULL
    BEGIN
        PRINT N'錯誤：找不到使用者 ' + @UserName;
        RETURN;
    END

    IF @FeeItemID IS NULL
    BEGIN
        PRINT N'錯誤：找不到收費項目 ' + @ItemName;
        RETURN;
    END

    -- 計算金額（管理費固定，水/電費 = 單價 * 用量）
    IF @ItemName = N'管理費'
        SELECT @Amount = UnitPrice FROM FeeItems WHERE FeeItemID = @FeeItemID;
    ELSE
    BEGIN
        IF @Usage IS NULL
        BEGIN
            PRINT N'錯誤：請輸入使用度數（Usage）';
            RETURN;
        END
        SELECT @Amount = UnitPrice * @Usage FROM FeeItems WHERE FeeItemID = @FeeItemID;
    END

    -- 寫入帳單
    INSERT INTO Bills (UserID, FeeItemID, Amount, DueDate, Status)
    VALUES (@UserID, @FeeItemID, @Amount, @DueDate, N'未繳');

    PRINT N'成功：已新增 ' + @ItemName + N' 帳單給 ' + @UserName + N'，金額 ' + CAST(@Amount AS NVARCHAR(20)) + N' 元';
END;
GO