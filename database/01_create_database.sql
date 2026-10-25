USE master;
GO

IF DB_ID('SecondHandMarketplace') IS NOT NULL
BEGIN
    ALTER DATABASE SecondHandMarketplace
    SET SINGLE_USER
    WITH ROLLBACK IMMEDIATE;

    DROP DATABASE SecondHandMarketplace;
END
GO

CREATE DATABASE SecondHandMarketplace;
GO

USE SecondHandMarketplace;
GO

