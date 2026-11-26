using EmployeeExcelAnalytics.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EmployeeExcelAnalytics.Api.Services
{
    public static class DatabaseSchemaInitializer
    {
        /// <summary>
        /// Ensures base tables (for fresh Azure SQL / SQL Server databases), tracking columns,
        /// and helpful indexes exist without breaking existing data.
        /// </summary>
        public static void EnsureSchemaAndIndexes(AppDbContext db)
        {
            const string sql = @"
IF OBJECT_ID('dbo.ImportHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ImportHistory (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ImportHistory PRIMARY KEY,
        FileName NVARCHAR(255) NOT NULL,
        UploadedDate DATETIME2 NOT NULL,
        TotalRows INT NOT NULL,
        ProcessedRows INT NOT NULL CONSTRAINT DF_ImportHistory_ProcessedRows DEFAULT 0,
        ImportedRows INT NOT NULL CONSTRAINT DF_ImportHistory_ImportedRows DEFAULT 0,
        RejectedRows INT NOT NULL CONSTRAINT DF_ImportHistory_RejectedRows DEFAULT 0,
        Status NVARCHAR(30) NOT NULL,
        ErrorReportPath NVARCHAR(500) NULL,
        JobId NVARCHAR(64) NULL,
        ErrorMessage NVARCHAR(1000) NULL
    );
END

IF OBJECT_ID('dbo.Employees', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employees (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Employees PRIMARY KEY,
        EmployeeCode NVARCHAR(50) NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Department NVARCHAR(100) NOT NULL,
        JobTitle NVARCHAR(100) NOT NULL,
        Salary DECIMAL(18,2) NOT NULL,
        HireDate DATE NOT NULL,
        ImportHistoryId INT NOT NULL,
        CONSTRAINT UQ_Employees_EmployeeCode UNIQUE (EmployeeCode),
        CONSTRAINT FK_Employees_ImportHistory FOREIGN KEY (ImportHistoryId) REFERENCES dbo.ImportHistory(Id)
    );
END

IF COL_LENGTH('dbo.ImportHistory', 'ProcessedRows') IS NULL
    ALTER TABLE dbo.ImportHistory ADD ProcessedRows INT NOT NULL CONSTRAINT DF_ImportHistory_ProcessedRows DEFAULT 0;

IF COL_LENGTH('dbo.ImportHistory', 'ImportedRows') IS NULL
    ALTER TABLE dbo.ImportHistory ADD ImportedRows INT NOT NULL CONSTRAINT DF_ImportHistory_ImportedRows DEFAULT 0;

IF COL_LENGTH('dbo.ImportHistory', 'RejectedRows') IS NULL
    ALTER TABLE dbo.ImportHistory ADD RejectedRows INT NOT NULL CONSTRAINT DF_ImportHistory_RejectedRows DEFAULT 0;

IF COL_LENGTH('dbo.ImportHistory', 'ErrorReportPath') IS NULL
    ALTER TABLE dbo.ImportHistory ADD ErrorReportPath NVARCHAR(500) NULL;

IF COL_LENGTH('dbo.ImportHistory', 'JobId') IS NULL
    ALTER TABLE dbo.ImportHistory ADD JobId NVARCHAR(64) NULL;

IF COL_LENGTH('dbo.ImportHistory', 'ErrorMessage') IS NULL
    ALTER TABLE dbo.ImportHistory ADD ErrorMessage NVARCHAR(1000) NULL;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_Department' AND object_id = OBJECT_ID('dbo.Employees'))
    DROP INDEX IX_Employees_Department ON dbo.Employees;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_HireDate' AND object_id = OBJECT_ID('dbo.Employees'))
    DROP INDEX IX_Employees_HireDate ON dbo.Employees;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_ImportHistoryId' AND object_id = OBJECT_ID('dbo.Employees'))
    CREATE NONCLUSTERED INDEX IX_Employees_ImportHistoryId ON dbo.Employees (ImportHistoryId);
";

            db.Database.ExecuteSqlRaw(sql);
        }
    }
}
