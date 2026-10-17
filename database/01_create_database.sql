
USE master;
GO

IF DB_ID(N'SecondHandDB') IS NULL
BEGIN
    CREATE DATABASE SecondHandDB;
    PRINT N'Da tao database SecondHandDB.';
END
ELSE
BEGIN
    PRINT N'Database SecondHandDB da ton tai.';
END
GO
