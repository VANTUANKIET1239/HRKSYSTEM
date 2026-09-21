/*
    Global battle rules. Idempotent and safe to run more than once.
*/
USE [HRK];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.HRK_BattleConfigs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_BattleConfigs
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_BattleConfigs PRIMARY KEY,
        Code NVARCHAR(80) NOT NULL,
        Value DECIMAL(18,4) NOT NULL,
        ValueType NVARCHAR(20) NOT NULL CONSTRAINT DF_HRK_BattleConfigs_ValueType DEFAULT ('NUMBER'),
        Description NVARCHAR(500) NULL,
        IsEnabled BIT NOT NULL CONSTRAINT DF_HRK_BattleConfigs_IsEnabled DEFAULT (1),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_BattleConfigs_CreatedOn DEFAULT (SYSUTCDATETIME()),
        UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_BattleConfigs_UpdatedOn DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_HRK_BattleConfigs_Code UNIQUE (Code),
        CONSTRAINT CHK_HRK_BattleConfigs_ValueType CHECK (ValueType IN ('NUMBER','PERCENT','TURN','ENERGY'))
    );
END;

MERGE dbo.HRK_BattleConfigs AS target
USING (VALUES
    (N'MAX_ROUNDS',               CAST(100 AS DECIMAL(18,4)), N'TURN',   N'Số vòng tối đa trước khi trận đấu được xử hòa.', 1),
    (N'MAX_ENERGY',               CAST(100 AS DECIMAL(18,4)), N'ENERGY', N'Năng lượng tối đa của một võ tướng.', 1),
    (N'INITIAL_ENERGY',           CAST(0 AS DECIMAL(18,4)),   N'ENERGY', N'Năng lượng khi bắt đầu trận đấu.', 1),
    (N'BASIC_ATTACK_ENERGY_GAIN', CAST(25 AS DECIMAL(18,4)),  N'ENERGY', N'Năng lượng nhận được sau khi dùng kỹ năng cơ bản.', 1),
    (N'BASIC_ATTACK_HIT_ENERGY_GAIN', CAST(25 AS DECIMAL(18,4)), N'ENERGY', N'Năng lượng nhận được khi còn sống sau khi bị kỹ năng cơ bản gây sát thương.', 1)
) AS source(Code, Value, ValueType, Description, IsEnabled)
ON target.Code = source.Code
WHEN MATCHED THEN UPDATE SET
    Value = source.Value,
    ValueType = source.ValueType,
    Description = source.Description,
    IsEnabled = source.IsEnabled,
    UpdatedOn = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (Code, Value, ValueType, Description, IsEnabled)
    VALUES (source.Code, source.Value, source.ValueType, source.Description, source.IsEnabled);

COMMIT TRANSACTION;
GO
