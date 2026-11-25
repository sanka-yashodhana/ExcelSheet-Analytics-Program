using EmployeeExcelAnalytics.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EmployeeExcelAnalytics.Api.Services
{
    public static class DatabaseSchemaInitializer
    {
        /// <summary>
        /// Ensures required columns for large import tracking and helpful indexes exist
        /// on existing SQL Server databases without breaking existing data.
        /// </summary>
        public static void EnsureSchemaAndIndexes(AppDbContext db)
        {
            const string sql = @"
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
