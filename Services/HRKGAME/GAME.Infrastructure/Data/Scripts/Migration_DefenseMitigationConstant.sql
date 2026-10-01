USE [HRK];
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.HRK_BattleConfigs', N'U') IS NULL
    THROW 51000, 'Run Migration_BattleConfigs.sql first.', 1;

-- Insert only: rerunning this script preserves subsequent balance adjustments.
IF NOT EXISTS (SELECT 1 FROM dbo.HRK_BattleConfigs WITH (UPDLOCK, HOLDLOCK)
               WHERE Code = N'DEFENSE_MITIGATION_CONSTANT')
BEGIN
    INSERT INTO dbo.HRK_BattleConfigs (Code, Value, ValueType, Description, IsEnabled)
    VALUES (N'DEFENSE_MITIGATION_CONSTANT', 1000, N'NUMBER',
            N'Hệ số K giảm sát thương: damage * K / (K + giáp hoặc kháng phép). Phải lớn hơn 0.', 1);
END;

COMMIT TRANSACTION;
GO
