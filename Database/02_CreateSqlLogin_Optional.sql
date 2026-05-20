/*
    OPTIONAL SCRIPT
    Use this only if you want SQL Server authentication instead of Windows authentication.
    Change the password before running in a real environment.
*/
USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'football_app_user')
BEGIN
    CREATE LOGIN [football_app_user] WITH PASSWORD = N'ChangeThis@12345';
END
GO

USE [FootballPredictionDb];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'football_app_user')
BEGIN
    CREATE USER [football_app_user] FOR LOGIN [football_app_user];
END
GO

ALTER ROLE db_owner ADD MEMBER [football_app_user];
GO

/*
    If you use this login, update appsettings.json:
    "DefaultConnection": "Server=.;Database=FootballPredictionDb;User Id=football_app_user;Password=ChangeThis@12345;TrustServerCertificate=True;MultipleActiveResultSets=true"
*/
