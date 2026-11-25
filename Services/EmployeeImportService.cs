using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using EmployeeExcelAnalytics.Api.Data;
using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Api.Models;
using EmployeeExcelAnalytics.Api.Options;

namespace EmployeeExcelAnalytics.Api.Services
{
    public class EmployeeImportService : IEmployeeImportService
    {
        private readonly AppDbContext _context;
        private readonly ExcelImportOptions _options;

        public EmployeeImportService(
            AppDbContext context,
            IOptions<ExcelImportOptions> options)
        {
            _context = context;
            _options = options.Value;
        }

        public async Task<EmployeeImportSaveResultDto> ImportEmployeesAsync(
            List<Employee> employees,
            string fileName)
        {
            var result = new EmployeeImportSaveResultDto();

            // Check duplicate codes inside the uploaded Excel file
            var duplicateCodes = employees
                .GroupBy(e => e.EmployeeCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .Take(_options.MaxInlineErrors)
                .ToList();

            if (duplicateCodes.Count > 0)
            {
                result.Success = false;
                result.Message = "Duplicate employee codes found in Excel file.";
                result.Errors.AddRange(duplicateCodes.Select(code => $"Employee code {code} appears more than once."));
                return result;
            }

            var connectionString = _context.Database.GetConnectionString()
                ?? throw new InvalidOperationException("Database connection string is not configured.");

            await using var sqlConnection = new SqlConnection(connectionString);
            await sqlConnection.OpenAsync();
            await using var transaction = (SqlTransaction)await sqlConnection.BeginTransactionAsync();

            try
            {
                // Efficient duplicate lookup against SQL Server using temp table #BatchCodes
                const string createTempSql = @"
IF OBJECT_ID('tempdb..#BatchCodes') IS NULL
    CREATE TABLE #BatchCodes (EmployeeCode NVARCHAR(50) COLLATE DATABASE_DEFAULT NOT NULL);
ELSE
    TRUNCATE TABLE #BatchCodes;";

                using (var createCmd = new SqlCommand(createTempSql, sqlConnection, transaction))
                {
                    await createCmd.ExecuteNonQueryAsync();
                }

                var codeTable = new DataTable();
                codeTable.Columns.Add("EmployeeCode", typeof(string)).MaxLength = 50;
                foreach (var emp in employees)
                {
                    codeTable.Rows.Add(emp.EmployeeCode.Trim());
                }

                using (var codeBulk = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, transaction))
                {
                    codeBulk.DestinationTableName = "#BatchCodes";
                    codeBulk.BatchSize = Math.Max(1000, _options.BatchSize);
                    codeBulk.ColumnMappings.Add("EmployeeCode", "EmployeeCode");
                    await codeBulk.WriteToServerAsync(codeTable);
                }

                var existingCodes = new List<string>();
                using (var dupCmd = new SqlCommand(
                    $"SELECT TOP ({_options.MaxInlineErrors}) b.EmployeeCode FROM #BatchCodes b INNER JOIN dbo.Employees e ON e.EmployeeCode = b.EmployeeCode OPTION (LOOP JOIN);",
                    sqlConnection,
                    transaction))
                {
                    await using var reader = await dupCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        existingCodes.Add(reader.GetString(0));
                    }
                }

                if (existingCodes.Count > 0)
                {
                    await transaction.RollbackAsync();
                    result.Success = false;
                    result.Message = "Some employee codes already exist.";
                    result.Errors.AddRange(existingCodes.Select(code => $"Employee code {code} already exists in the database."));
                    return result;
                }

                int importHistoryId;
                const string insertHistorySql = @"
INSERT INTO dbo.ImportHistory (FileName, UploadedDate, TotalRows, ProcessedRows, ImportedRows, RejectedRows, Status)
OUTPUT INSERTED.Id
VALUES (@FileName, @UploadedDate, @TotalRows, @ProcessedRows, @ImportedRows, @RejectedRows, @Status);";

                using (var historyCmd = new SqlCommand(insertHistorySql, sqlConnection, transaction))
                {
                    historyCmd.Parameters.AddWithValue("@FileName", fileName);
                    historyCmd.Parameters.AddWithValue("@UploadedDate", DateTime.Now);
                    historyCmd.Parameters.AddWithValue("@TotalRows", employees.Count);
                    historyCmd.Parameters.AddWithValue("@ProcessedRows", employees.Count);
                    historyCmd.Parameters.AddWithValue("@ImportedRows", employees.Count);
                    historyCmd.Parameters.AddWithValue("@RejectedRows", 0);
                    historyCmd.Parameters.AddWithValue("@Status", "Completed");

                    importHistoryId = (int)(await historyCmd.ExecuteScalarAsync() ?? 0);
                }

                int batchSize = Math.Max(500, _options.BatchSize);
                for (int offset = 0; offset < employees.Count; offset += batchSize)
                {
                    var employeeTable = new DataTable();
                    employeeTable.Columns.Add("EmployeeCode", typeof(string));
                    employeeTable.Columns.Add("FirstName", typeof(string));
                    employeeTable.Columns.Add("LastName", typeof(string));
                    employeeTable.Columns.Add("Department", typeof(string));
                    employeeTable.Columns.Add("JobTitle", typeof(string));
                    employeeTable.Columns.Add("Salary", typeof(decimal));
                    employeeTable.Columns.Add("HireDate", typeof(DateTime));
                    employeeTable.Columns.Add("ImportHistoryId", typeof(int));

                    int limit = Math.Min(offset + batchSize, employees.Count);
                    for (int i = offset; i < limit; i++)
                    {
                        var emp = employees[i];
                        employeeTable.Rows.Add(
                            emp.EmployeeCode.Trim(),
                            emp.FirstName.Trim(),
                            emp.LastName.Trim(),
                            emp.Department.Trim(),
                            emp.JobTitle.Trim(),
                            emp.Salary,
                            emp.HireDate.Date,
                            importHistoryId);
                    }

                    using var bulkCopy = new SqlBulkCopy(
                        sqlConnection,
                        SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers,
                        transaction)
                    {
                        DestinationTableName = "dbo.Employees",
                        BatchSize = batchSize,
                        BulkCopyTimeout = 180
                    };

                    bulkCopy.ColumnMappings.Add("EmployeeCode", "EmployeeCode");
                    bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
                    bulkCopy.ColumnMappings.Add("LastName", "LastName");
                    bulkCopy.ColumnMappings.Add("Department", "Department");
                    bulkCopy.ColumnMappings.Add("JobTitle", "JobTitle");
                    bulkCopy.ColumnMappings.Add("Salary", "Salary");
                    bulkCopy.ColumnMappings.Add("HireDate", "HireDate");
                    bulkCopy.ColumnMappings.Add("ImportHistoryId", "ImportHistoryId");

                    await bulkCopy.WriteToServerAsync(employeeTable);
                }

                await transaction.CommitAsync();

                result.Success = true;
                result.Message = "Employees imported successfully.";
                result.ImportHistoryId = importHistoryId;
                result.TotalEmployees = employees.Count;
                result.TotalRecords = employees.Count;
                result.ProcessedRecords = employees.Count;
                result.RejectedRecords = 0;
                result.FileName = fileName;

                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
