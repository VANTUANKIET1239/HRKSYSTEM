-- Optional, run on the SQL Server selected for the language service.
-- The migration and seed target this dedicated database; they do not modify HRK/GAME tables.
IF DB_ID(N'HRK_LANGUAGE') IS NULL
    EXEC(N'CREATE DATABASE [HRK_LANGUAGE]');
GO
