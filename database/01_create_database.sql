<<<<<<< Updated upstream

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
=======
USE master;
GO

IF DB_ID(N'SecondHandMarketplace') IS NULL
BEGIN
    CREATE DATABASE SecondHandMarketplace COLLATE Vietnamese_CI_AS;
    PRINT N'Da tao database SecondHandMarketplace.';
END
ELSE
    PRINT N'Database SecondHandMarketplace da ton tai, bo qua.';
GO

-- Giảm khóa đọc/ghi khi nhiều người dùng thao tác cùng lúc
IF (SELECT is_read_committed_snapshot_on FROM sys.databases WHERE name = N'SecondHandMarketplace') = 0
    ALTER DATABASE SecondHandMarketplace SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO
>>>>>>> Stashed changes
