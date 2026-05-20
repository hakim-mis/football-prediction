/*
    DEVELOPMENT ONLY: This drops the database and all data.
    Do not run this on production.
*/
USE master;
GO

IF DB_ID(N'FootballPredictionDb') IS NOT NULL
BEGIN
    ALTER DATABASE [FootballPredictionDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [FootballPredictionDb];
END
GO

EXEC(N'CREATE DATABASE [FootballPredictionDb]');
GO

ALTER DATABASE [FootballPredictionDb] SET READ_COMMITTED_SNAPSHOT ON;
GO
