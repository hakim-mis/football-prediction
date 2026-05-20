/*
    Football Prediction Game - SQL Server Database Creation Script
    ------------------------------------------------------------
    Run this script in SQL Server Management Studio before starting the ASP.NET Core MVC application.
    It creates the database and all ASP.NET Core Identity + application tables.

    Default admin is created by the application on first run using appsettings.json:
    Email:    admin@football.local
    Password: Admin@12345
*/
USE master;
GO

IF DB_ID(N'FootballPredictionDb') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [FootballPredictionDb]');
END
GO

ALTER DATABASE [FootballPredictionDb] SET READ_COMMITTED_SNAPSHOT ON;
GO

USE [FootballPredictionDb];
GO

/* ASP.NET Core Identity tables */
IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoles
    (
        Id NVARCHAR(450) NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
        Name NVARCHAR(256) NULL,
        NormalizedName NVARCHAR(256) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUsers
    (
        Id NVARCHAR(450) NOT NULL CONSTRAINT PK_AspNetUsers PRIMARY KEY,
        UserName NVARCHAR(256) NULL,
        NormalizedUserName NVARCHAR(256) NULL,
        Email NVARCHAR(256) NULL,
        NormalizedEmail NVARCHAR(256) NULL,
        EmailConfirmed BIT NOT NULL CONSTRAINT DF_AspNetUsers_EmailConfirmed DEFAULT (0),
        PasswordHash NVARCHAR(MAX) NULL,
        SecurityStamp NVARCHAR(MAX) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL,
        PhoneNumber NVARCHAR(MAX) NULL,
        PhoneNumberConfirmed BIT NOT NULL CONSTRAINT DF_AspNetUsers_PhoneNumberConfirmed DEFAULT (0),
        TwoFactorEnabled BIT NOT NULL CONSTRAINT DF_AspNetUsers_TwoFactorEnabled DEFAULT (0),
        LockoutEnd DATETIMEOFFSET NULL,
        LockoutEnabled BIT NOT NULL CONSTRAINT DF_AspNetUsers_LockoutEnabled DEFAULT (0),
        AccessFailedCount INT NOT NULL CONSTRAINT DF_AspNetUsers_AccessFailedCount DEFAULT (0),

        FullName NVARCHAR(150) NOT NULL,
        MobileNo NVARCHAR(30) NULL,
        ProfilePhotoPath NVARCHAR(300) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_AspNetUsers_IsActive DEFAULT (0),
        MustChangePassword BIT NOT NULL CONSTRAINT DF_AspNetUsers_MustChangePassword DEFAULT (0),
        PasswordChangedAt DATETIME2 NULL,
        TotalScore INT NOT NULL CONSTRAINT DF_AspNetUsers_TotalScore DEFAULT (0),
        ExactPredictionCount INT NOT NULL CONSTRAINT DF_AspNetUsers_ExactPredictionCount DEFAULT (0),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AspNetUsers_CreatedAt DEFAULT (GETDATE()),
        UpdatedAt DATETIME2 NULL
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetRoleClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoleClaims
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY,
        RoleId NVARCHAR(450) NOT NULL,
        ClaimType NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId
            FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserClaims
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetUserClaims PRIMARY KEY,
        UserId NVARCHAR(450) NOT NULL,
        ClaimType NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserLogins
    (
        LoginProvider NVARCHAR(450) NOT NULL,
        ProviderKey NVARCHAR(450) NOT NULL,
        ProviderDisplayName NVARCHAR(MAX) NULL,
        UserId NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserRoles
    (
        UserId NVARCHAR(450) NOT NULL,
        RoleId NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId
            FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserTokens
    (
        UserId NVARCHAR(450) NOT NULL,
        LoginProvider NVARCHAR(450) NOT NULL,
        Name NVARCHAR(450) NOT NULL,
        Value NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END
GO

/* Application tables */
IF OBJECT_ID(N'dbo.Fixtures', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Fixtures
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Fixtures PRIMARY KEY,
        TeamOneName NVARCHAR(100) NOT NULL,
        TeamOneFlagPath NVARCHAR(300) NULL,
        TeamTwoName NVARCHAR(100) NOT NULL,
        TeamTwoFlagPath NVARCHAR(300) NULL,
        Stage INT NOT NULL CONSTRAINT DF_Fixtures_Stage DEFAULT (1),
        MatchDateTime DATETIME2 NOT NULL,
        TeamOneActualGoal INT NULL,
        TeamTwoActualGoal INT NULL,
        Status INT NOT NULL CONSTRAINT DF_Fixtures_Status DEFAULT (1),
        IsPublished BIT NOT NULL CONSTRAINT DF_Fixtures_IsPublished DEFAULT (1),
        IsProcessed BIT NOT NULL CONSTRAINT DF_Fixtures_IsProcessed DEFAULT (0),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Fixtures_CreatedAt DEFAULT (GETDATE()),
        UpdatedAt DATETIME2 NULL
    );
END
GO

IF OBJECT_ID(N'dbo.Predictions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Predictions
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Predictions PRIMARY KEY,
        UserId NVARCHAR(450) NOT NULL,
        FixtureId INT NOT NULL,
        TeamOnePredictedGoal INT NOT NULL,
        TeamTwoPredictedGoal INT NOT NULL,
        EarnedPoint INT NOT NULL CONSTRAINT DF_Predictions_EarnedPoint DEFAULT (0),
        IsProcessed BIT NOT NULL CONSTRAINT DF_Predictions_IsProcessed DEFAULT (0),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Predictions_CreatedAt DEFAULT (GETDATE()),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_Predictions_AspNetUsers_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Predictions_Fixtures_FixtureId
            FOREIGN KEY (FixtureId) REFERENCES dbo.Fixtures(Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.ResultProcessingLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ResultProcessingLogs
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResultProcessingLogs PRIMARY KEY,
        FixtureId INT NOT NULL,
        ProcessedAt DATETIME2 NOT NULL CONSTRAINT DF_ResultProcessingLogs_ProcessedAt DEFAULT (GETDATE()),
        ProcessedByUserId NVARCHAR(450) NULL,
        TotalPredictionsProcessed INT NOT NULL,
        CONSTRAINT FK_ResultProcessingLogs_Fixtures_FixtureId
            FOREIGN KEY (FixtureId) REFERENCES dbo.Fixtures(Id) ON DELETE NO ACTION
    );
END
GO

/* Upgrade helpers for existing databases */
IF COL_LENGTH(N'dbo.AspNetUsers', N'MustChangePassword') IS NULL
    ALTER TABLE dbo.AspNetUsers ADD MustChangePassword BIT NOT NULL CONSTRAINT DF_AspNetUsers_MustChangePassword DEFAULT (0);
GO

IF COL_LENGTH(N'dbo.AspNetUsers', N'PasswordChangedAt') IS NULL
    ALTER TABLE dbo.AspNetUsers ADD PasswordChangedAt DATETIME2 NULL;
GO

IF COL_LENGTH(N'dbo.Fixtures', N'Stage') IS NULL
    ALTER TABLE dbo.Fixtures ADD Stage INT NOT NULL CONSTRAINT DF_Fixtures_Stage DEFAULT (1);
GO

/* Indexes */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'RoleNameIndex' AND object_id = OBJECT_ID(N'dbo.AspNetRoles'))
    CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'EmailIndex' AND object_id = OBJECT_ID(N'dbo.AspNetUsers'))
    CREATE INDEX EmailIndex ON dbo.AspNetUsers(NormalizedEmail);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UserNameIndex' AND object_id = OBJECT_ID(N'dbo.AspNetUsers'))
    CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetRoleClaims_RoleId' AND object_id = OBJECT_ID(N'dbo.AspNetRoleClaims'))
    CREATE INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims(RoleId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserClaims_UserId' AND object_id = OBJECT_ID(N'dbo.AspNetUserClaims'))
    CREATE INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims(UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserLogins_UserId' AND object_id = OBJECT_ID(N'dbo.AspNetUserLogins'))
    CREATE INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins(UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserRoles_RoleId' AND object_id = OBJECT_ID(N'dbo.AspNetUserRoles'))
    CREATE INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles(RoleId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Predictions_FixtureId' AND object_id = OBJECT_ID(N'dbo.Predictions'))
    CREATE INDEX IX_Predictions_FixtureId ON dbo.Predictions(FixtureId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Predictions_UserId_FixtureId' AND object_id = OBJECT_ID(N'dbo.Predictions'))
    CREATE UNIQUE INDEX IX_Predictions_UserId_FixtureId ON dbo.Predictions(UserId, FixtureId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ResultProcessingLogs_FixtureId' AND object_id = OBJECT_ID(N'dbo.ResultProcessingLogs'))
    CREATE INDEX IX_ResultProcessingLogs_FixtureId ON dbo.ResultProcessingLogs(FixtureId);
GO

SELECT
    DB_NAME() AS DatabaseName,
    'FootballPredictionDb is ready. Run the ASP.NET Core MVC application; it will seed Admin/User roles and the default admin account. Admin can reset user password to User@12345 and user must change it after login.' AS Message;
GO
