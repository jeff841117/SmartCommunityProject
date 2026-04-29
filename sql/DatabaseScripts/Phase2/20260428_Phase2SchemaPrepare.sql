/*
    第二階段第一批資料庫落地腳本
    目的：
    1. 先把第二階段會用到的欄位與資料表建立好
    2. 儘量用可重複執行的寫法，降低測試資料庫反覆執行時的風險
    3. 先新增，不先刪，避免舊功能在第二階段初期就被硬切斷
*/

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    /* =========================================================
       member：補帳號擴充欄位
       ========================================================= */
    IF COL_LENGTH('member', 'PasswordHash') IS NULL
        ALTER TABLE member ADD PasswordHash NVARCHAR(255) NULL;

    IF COL_LENGTH('member', 'DisplayName') IS NULL
        ALTER TABLE member ADD DisplayName NVARCHAR(100) NULL;

    IF COL_LENGTH('member', 'IsActive') IS NULL
        ALTER TABLE member ADD IsActive BIT NOT NULL CONSTRAINT DF_member_IsActive DEFAULT 1;

    IF COL_LENGTH('member', 'CreatedAt') IS NULL
        ALTER TABLE member ADD CreatedAt DATETIME NULL;

    IF COL_LENGTH('member', 'UpdatedAt') IS NULL
        ALTER TABLE member ADD UpdatedAt DATETIME NULL;

    IF COL_LENGTH('member', 'RoleCode') IS NULL
        ALTER TABLE member ADD RoleCode INT NULL;

    EXEC sp_executesql N'
        UPDATE member
        SET DisplayName = userName
        WHERE DisplayName IS NULL;
    ';

    EXEC sp_executesql N'
        UPDATE member
        SET CreatedAt = GETDATE()
        WHERE CreatedAt IS NULL;
    ';

    EXEC sp_executesql N'
        UPDATE member
        SET UpdatedAt = GETDATE()
        WHERE UpdatedAt IS NULL;
    ';

    EXEC sp_executesql N'
        UPDATE member
        SET RoleCode =
            CASE role
                WHEN ''user'' THEN 1
                WHEN ''manager'' THEN 2
                WHEN ''admin'' THEN 3
                ELSE 1
            END
        WHERE RoleCode IS NULL;
    ';

    /* =========================================================
       Equipment：補設備擴充欄位
       ========================================================= */
    IF COL_LENGTH('Equipment', 'IsEnabled') IS NULL
        ALTER TABLE Equipment ADD IsEnabled BIT NOT NULL CONSTRAINT DF_Equipment_IsEnabled DEFAULT 1;

    IF COL_LENGTH('Equipment', 'CreatedAt') IS NULL
        ALTER TABLE Equipment ADD CreatedAt DATETIME NULL;

    IF COL_LENGTH('Equipment', 'UpdatedAt') IS NULL
        ALTER TABLE Equipment ADD UpdatedAt DATETIME NULL;

    EXEC sp_executesql N'
        UPDATE Equipment
        SET CreatedAt = GETDATE()
        WHERE CreatedAt IS NULL;
    ';

    EXEC sp_executesql N'
        UPDATE Equipment
        SET UpdatedAt = GETDATE()
        WHERE UpdatedAt IS NULL;
    ';

    /* =========================================================
       Reservations：補預約主體欄位
       ========================================================= */
    IF COL_LENGTH('Reservations', 'CreatedAt') IS NULL
        ALTER TABLE Reservations ADD CreatedAt DATETIME NULL;

    IF COL_LENGTH('Reservations', 'ReservedStartTime') IS NULL
        ALTER TABLE Reservations ADD ReservedStartTime DATETIME NULL;

    IF COL_LENGTH('Reservations', 'ReservedEndTime') IS NULL
        ALTER TABLE Reservations ADD ReservedEndTime DATETIME NULL;

    IF COL_LENGTH('Reservations', 'DurationMinutes') IS NULL
        ALTER TABLE Reservations ADD DurationMinutes INT NULL;

    IF COL_LENGTH('Reservations', 'ActualStartTime') IS NULL
        ALTER TABLE Reservations ADD ActualStartTime DATETIME NULL;

    IF COL_LENGTH('Reservations', 'ActualEndTime') IS NULL
        ALTER TABLE Reservations ADD ActualEndTime DATETIME NULL;

    IF COL_LENGTH('Reservations', 'ReservationType') IS NULL
        ALTER TABLE Reservations ADD ReservationType INT NULL;

    IF COL_LENGTH('Reservations', 'CancelledAt') IS NULL
        ALTER TABLE Reservations ADD CancelledAt DATETIME NULL;

    IF COL_LENGTH('Reservations', 'CancelReason') IS NULL
        ALTER TABLE Reservations ADD CancelReason NVARCHAR(255) NULL;

    IF COL_LENGTH('Reservations', 'CancelledByUserId') IS NULL
        ALTER TABLE Reservations ADD CancelledByUserId INT NULL;

    IF COL_LENGTH('Reservations', 'EndedByType') IS NULL
        ALTER TABLE Reservations ADD EndedByType INT NULL;

    IF COL_LENGTH('Reservations', 'EndedByUserId') IS NULL
        ALTER TABLE Reservations ADD EndedByUserId INT NULL;

    IF COL_LENGTH('Reservations', 'Notes') IS NULL
        ALTER TABLE Reservations ADD Notes NVARCHAR(500) NULL;

    IF COL_LENGTH('Reservations', 'ReservationTime') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            UPDATE Reservations
            SET CreatedAt = ReservationTime
            WHERE CreatedAt IS NULL;
        ';
    END;

    EXEC sp_executesql N'
        UPDATE Reservations
        SET ReservedStartTime = StartTime
        WHERE ReservedStartTime IS NULL
          AND StartTime IS NOT NULL;
    ';

    EXEC sp_executesql N'
        UPDATE Reservations
        SET ActualStartTime = StartTime
        WHERE ActualStartTime IS NULL
          AND StartTime IS NOT NULL
          AND Status = 2;
    ';

    EXEC sp_executesql N'
        UPDATE Reservations
        SET ActualEndTime = EndTime
        WHERE ActualEndTime IS NULL
          AND EndTime IS NOT NULL;
    ';

    EXEC sp_executesql N'
        UPDATE Reservations
        SET ReservationType = 1
        WHERE ReservationType IS NULL;
    ';

    /* =========================================================
       WaitingQueue：補排隊主體欄位
       ========================================================= */
    IF COL_LENGTH('WaitingQueue', 'ReservationId') IS NULL
        ALTER TABLE WaitingQueue ADD ReservationId INT NULL;

    IF COL_LENGTH('WaitingQueue', 'QueueType') IS NULL
        ALTER TABLE WaitingQueue ADD QueueType INT NULL;

    IF COL_LENGTH('WaitingQueue', 'QueueStatus') IS NULL
        ALTER TABLE WaitingQueue ADD QueueStatus INT NULL;

    IF COL_LENGTH('WaitingQueue', 'QueuedAt') IS NULL
        ALTER TABLE WaitingQueue ADD QueuedAt DATETIME NULL;

    IF COL_LENGTH('WaitingQueue', 'QueuePosition') IS NULL
        ALTER TABLE WaitingQueue ADD QueuePosition INT NULL;

    IF COL_LENGTH('WaitingQueue', 'ExpectedAvailableTime') IS NULL
        ALTER TABLE WaitingQueue ADD ExpectedAvailableTime DATETIME NULL;

    IF COL_LENGTH('WaitingQueue', 'ConvertedToInProgressAt') IS NULL
        ALTER TABLE WaitingQueue ADD ConvertedToInProgressAt DATETIME NULL;

    IF COL_LENGTH('WaitingQueue', 'CancelledAt') IS NULL
        ALTER TABLE WaitingQueue ADD CancelledAt DATETIME NULL;

    IF COL_LENGTH('WaitingQueue', 'CancelReason') IS NULL
        ALTER TABLE WaitingQueue ADD CancelReason NVARCHAR(255) NULL;

    EXEC sp_executesql N'
        UPDATE WaitingQueue
        SET QueuedAt = QueueTime
        WHERE QueuedAt IS NULL
          AND QueueTime IS NOT NULL;
    ';

    EXEC sp_executesql N'
        UPDATE WaitingQueue
        SET QueuePosition = Position
        WHERE QueuePosition IS NULL
          AND Position IS NOT NULL;
    ';

    EXEC sp_executesql N'
        UPDATE WaitingQueue
        SET QueueType = 1
        WHERE QueueType IS NULL;
    ';

    EXEC sp_executesql N'
        UPDATE WaitingQueue
        SET QueueStatus = 1
        WHERE QueueStatus IS NULL;
    ';

    /* =========================================================
       PasswordResetCodes：忘記密碼驗證碼表
       ========================================================= */
    IF OBJECT_ID('dbo.PasswordResetCodes', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PasswordResetCodes
        (
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            UserId INT NOT NULL,
            Email NVARCHAR(100) NOT NULL,
            Code NVARCHAR(6) NOT NULL,
            ExpiredAt DATETIME NOT NULL,
            UsedAt DATETIME NULL,
            Status INT NOT NULL,
            CreatedAt DATETIME NOT NULL CONSTRAINT DF_PasswordResetCodes_CreatedAt DEFAULT GETDATE()
        );
    END;

    /* =========================================================
       AdminActionLogs：管理者操作紀錄表
       ========================================================= */
    IF OBJECT_ID('dbo.AdminActionLogs', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AdminActionLogs
        (
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            AdminUserId INT NOT NULL,
            ActionType INT NOT NULL,
            TargetType INT NOT NULL,
            TargetId INT NOT NULL,
            Reason NVARCHAR(255) NULL,
            CreatedAt DATETIME NOT NULL CONSTRAINT DF_AdminActionLogs_CreatedAt DEFAULT GETDATE()
        );
    END;

    /* =========================================================
       常用索引：先補第二階段最容易查詢的欄位
       ========================================================= */
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_Reservations_UserId_Status'
          AND object_id = OBJECT_ID('dbo.Reservations')
    )
    BEGIN
        CREATE INDEX IX_Reservations_UserId_Status
            ON dbo.Reservations(UserId, Status);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_Reservations_EquipmentId_Status'
          AND object_id = OBJECT_ID('dbo.Reservations')
    )
    BEGIN
        CREATE INDEX IX_Reservations_EquipmentId_Status
            ON dbo.Reservations(EquipmentId, Status);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_WaitingQueue_EquipmentId_QueueStatus_QueuePosition'
          AND object_id = OBJECT_ID('dbo.WaitingQueue')
    )
    BEGIN
        CREATE INDEX IX_WaitingQueue_EquipmentId_QueueStatus_QueuePosition
            ON dbo.WaitingQueue(EquipmentId, QueueStatus, QueuePosition);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_PasswordResetCodes_Email_Status'
          AND object_id = OBJECT_ID('dbo.PasswordResetCodes')
    )
    BEGIN
        CREATE INDEX IX_PasswordResetCodes_Email_Status
            ON dbo.PasswordResetCodes(Email, Status);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
