USE SecondHandMarketplace;
GO
IF OBJECT_ID(N'dbo.PasswordResetOtp', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetOtp
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PasswordResetOtp PRIMARY KEY,
        Email VARCHAR(256) NOT NULL,
        CodeProtected NVARCHAR(512) NOT NULL,
        CreatedAtUtc DATETIME2(0) NOT NULL,
        ExpiresAtUtc DATETIME2(0) NOT NULL,
        UsedAtUtc DATETIME2(0) NULL,
        FailedAttempts INT NOT NULL CONSTRAINT DF_PasswordResetOtp_FailedAttempts DEFAULT (0),
        CONSTRAINT CK_PasswordResetOtp_FailedAttempts CHECK (FailedAttempts BETWEEN 0 AND 5)
    );
    CREATE INDEX IX_PasswordResetOtp_Email_CreatedAtUtc ON dbo.PasswordResetOtp(Email, CreatedAtUtc DESC);
END;
GO
