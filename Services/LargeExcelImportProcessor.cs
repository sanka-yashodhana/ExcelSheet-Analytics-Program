using System.Data;
using System.Text;
using EmployeeExcelAnalytics.Api.Data;
using EmployeeExcelAnalytics.Api.Models;
using EmployeeExcelAnalytics.Api.Options;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmployeeExcelAnalytics.Api.Services
{
    public class LargeExcelImportProcessor : ILargeExcelImportProcessor
    {
        private readonly AppDbContext _context;
        private readonly StreamingExcelReader _excelReader;
        private readonly IImportJobStore _jobStore;
        private readonly ExcelImportOptions _options;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LargeExcelImportProcessor> _logger;

        public LargeExcelImportProcessor(
            AppDbContext context,
            StreamingExcelReader excelReader,
            IImportJobStore jobStore,
            IOptions<ExcelImportOptions> options,
            IWebHostEnvironment env,
            ILogger<LargeExcelImportProcessor> logger)
        {
            _context = context;
            _excelReader = excelReader;
            _jobStore = jobStore;
            _options = options.Value;
            _env = env;
            _logger = logger;
        }

        public async Task ProcessJobAsync(ImportJobQueueItem item, CancellationToken cancellationToken)
        {
            var jobId = item.JobId;
            int totalRecords = 0;
            int processedRecords = 0;
            int importedRecords = 0;
            int rejectedRecords = 0;
            string? errorReportPath = null;
            StreamWriter? errorWriter = null;
            var sampleErrors = new List<string>();

            try
            {
                _jobStore.UpdateProgress(
                    jobId, "Reading", 0, 0, 0, 0,
                    "Inspecting Excel headers and row count...");

                var headers = _excelReader.ReadHeadersFromFile(item.TempFilePath);
                var (isValidMapping, columnByField, mappingError) =
                    _excelReader.ResolveColumnMappings(headers, item.HeaderMappings);

                if (!isValidMapping)
                {
                    await FailJobAsync(item, totalRecords, processedRecords, rejectedRecords, mappingError ?? "Invalid column mapping.");
                    return;
                }

                totalRecords = _excelReader.CountNonEmptyDataRows(item.TempFilePath, columnByField);
                if (totalRecords == 0)
                {
                    await FailJobAsync(item, 0, 0, 0, "The uploaded Excel file does not contain any employee records.");
                    return;
                }

                if (totalRecords > _options.MaxRecords)
                {
                    await FailJobAsync(
                        item,
                        totalRecords,
                        0,
                        0,
                        $"Workbook contains {totalRecords:N0} rows, which exceeds the configured limit of {_options.MaxRecords:N0} records.");
                    return;
                }

                _jobStore.UpdateProgress(
                    jobId, "Importing", totalRecords, 0, 0, 0,
                    $"Starting batch import of {totalRecords:N0} records...");

                var errorDir = Path.Combine(_env.ContentRootPath, _options.TempStoragePath, "ErrorReports");
                Directory.CreateDirectory(errorDir);
                var candidateErrorPath = Path.Combine(errorDir, $"{jobId}_errors.csv");

                var connectionString = _context.Database.GetConnectionString()
                    ?? throw new InvalidOperationException("SQL Server connection string is missing.");

                await using var sqlConnection = new SqlConnection(connectionString);
                await sqlConnection.OpenAsync(cancellationToken);

                await using var transaction = (SqlTransaction)await sqlConnection.BeginTransactionAsync(cancellationToken);

                try
                {
                    await EnsureTempLookupTableAsync(sqlConnection, transaction, cancellationToken);

                    var seenCodesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    int batchSize = Math.Max(100, _options.BatchSize);
                    var pendingBatch = new List<(RawExcelEmployeeRow Raw, Employee Employee)>(batchSize);

                    void RecordError(RawExcelEmployeeRow raw, string reason)
                    {
                        rejectedRecords++;
                        if (errorWriter == null)
                        {
                            errorReportPath = candidateErrorPath;
                            errorWriter = new StreamWriter(errorReportPath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                            errorWriter.WriteLine("RowNumber,EmployeeCode,FirstName,LastName,Department,JobTitle,Salary,HireDate,ErrorMessage");
                        }

                        errorWriter.WriteLine(string.Join(",",
                            raw.RowNumber.ToString(),
                            EscapeCsv(raw.EmployeeCode),
                            EscapeCsv(raw.FirstName),
                            EscapeCsv(raw.LastName),
                            EscapeCsv(raw.Department),
                            EscapeCsv(raw.JobTitle),
                            EscapeCsv(raw.SalaryRaw),
                            EscapeCsv(raw.HireDateRaw),
                            EscapeCsv(reason)));

                        if (sampleErrors.Count < _options.MaxInlineErrors)
                        {
                            sampleErrors.Add(reason);
                        }
                    }

                    foreach (var rawRow in _excelReader.StreamRows(item.TempFilePath, columnByField, cancellationToken))
                    {
                        var validation = StreamingExcelReader.ValidateRow(rawRow);
                        if (!validation.IsValid || validation.Employee == null)
                        {
                            RecordError(rawRow, validation.ErrorMessage ?? $"Row {rawRow.RowNumber}: validation failed.");
                            processedRecords++;

                            if (processedRecords % 500 == 0)
                            {
                                _jobStore.UpdateProgress(
                                    jobId, "Importing", totalRecords, processedRecords, importedRecords, rejectedRecords,
                                    $"Validating and importing batches ({processedRecords:N0} / {totalRecords:N0})...",
                                    FlushSampleErrors(sampleErrors),
                                    errorReportPath);
                            }
                            continue;
                        }

                        if (!seenCodesInFile.Add(validation.Employee.EmployeeCode))
                        {
                            RecordError(
                                rawRow,
                                $"Row {rawRow.RowNumber}: Duplicate EmployeeCode '{validation.Employee.EmployeeCode}' appears multiple times in this workbook.");
                            processedRecords++;
                            continue;
                        }

                        validation.Employee.ImportHistoryId = item.ImportHistoryId;
                        pendingBatch.Add((rawRow, validation.Employee));

                        if (pendingBatch.Count >= batchSize)
                        {
                            int batchInserted = await FlushBatchAsync(
                                pendingBatch,
                                sqlConnection,
                                transaction,
                                batchSize,
                                RecordError,
                                cancellationToken);

                            importedRecords += batchInserted;
                            processedRecords += pendingBatch.Count;
                            pendingBatch.Clear();

                            _jobStore.UpdateProgress(
                                jobId, "Importing", totalRecords, processedRecords, importedRecords, rejectedRecords,
                                $"Processed {processedRecords:N0} of {totalRecords:N0} records...",
                                FlushSampleErrors(sampleErrors),
                                errorReportPath);
                        }
                    }

                    if (pendingBatch.Count > 0)
                    {
                        int batchInserted = await FlushBatchAsync(
                            pendingBatch,
                            sqlConnection,
                            transaction,
                            batchSize,
                            RecordError,
                            cancellationToken);

                        importedRecords += batchInserted;
                        processedRecords += pendingBatch.Count;
                        pendingBatch.Clear();
                    }

                    if (errorWriter != null)
                    {
                        await errorWriter.FlushAsync(cancellationToken);
                        await errorWriter.DisposeAsync();
                        errorWriter = null;
                    }

                    string finalStatus = importedRecords == 0 && rejectedRecords > 0
                        ? "Failed"
                        : rejectedRecords > 0
                            ? "CompletedWithErrors"
                            : "Completed";

                    await UpdateImportHistoryInTransactionAsync(
                        sqlConnection,
                        transaction,
                        item.ImportHistoryId,
                        totalRecords,
                        processedRecords,
                        importedRecords,
                        rejectedRecords,
                        finalStatus,
                        errorReportPath,
                        importedRecords == 0 && rejectedRecords > 0 ? "All rows failed validation. Download the error report for details." : null,
                        cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    string finalMessage = finalStatus switch
                    {
                        "Completed" => $"Successfully imported all {importedRecords:N0} employee records.",
                        "CompletedWithErrors" => $"Imported {importedRecords:N0} employee records; {rejectedRecords:N0} rows were rejected and logged in the error report.",
                        _ => $"Import finished with 0 imported records and {rejectedRecords:N0} rejected rows. Download the error report for details."
                    };

                    _jobStore.UpdateProgress(
                        jobId,
                        finalStatus,
                        totalRecords,
                        processedRecords,
                        importedRecords,
                        rejectedRecords,
                        finalMessage,
                        FlushSampleErrors(sampleErrors),
                        errorReportPath,
                        isCompleted: true);
                }
                catch
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw;
                }
            }
            catch (OperationCanceledException)
            {
                await FailJobAsync(item, totalRecords, processedRecords, rejectedRecords, "Import was cancelled due to application shutdown.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Large Excel import failed for job {JobId}", jobId);
                await FailJobAsync(
                    item,
                    totalRecords,
                    processedRecords,
                    rejectedRecords,
                    "An error occurred while bulk importing employee records. All database changes for this import were rolled back.");
            }
            finally
            {
                if (errorWriter != null)
                {
                    await errorWriter.DisposeAsync();
                }

                try
                {
                    if (File.Exists(item.TempFilePath))
                    {
                        File.Delete(item.TempFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary import file {FilePath}", item.TempFilePath);
                }
            }
        }

        private async Task<int> FlushBatchAsync(
            List<(RawExcelEmployeeRow Raw, Employee Employee)> batch,
            SqlConnection connection,
            SqlTransaction transaction,
            int batchSize,
            Action<RawExcelEmployeeRow, string> recordError,
            CancellationToken cancellationToken)
        {
            if (batch.Count == 0) return 0;

            // 1. Check which EmployeeCodes in this batch already exist in SQL Server using #BatchCodes + LOOP JOIN (zero memory grant)
            var codeTable = new DataTable();
            var codeCol = codeTable.Columns.Add("EmployeeCode", typeof(string));
            codeCol.MaxLength = 50;
            foreach (var item in batch)
            {
                codeTable.Rows.Add(item.Employee.EmployeeCode);
            }

            using (var codeBulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
            {
                codeBulk.DestinationTableName = "#BatchCodes";
                codeBulk.BatchSize = batch.Count;
                codeBulk.ColumnMappings.Add("EmployeeCode", "EmployeeCode");
                await codeBulk.WriteToServerAsync(codeTable, cancellationToken);
            }

            var existingInDb = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = new SqlCommand(
                "SELECT b.EmployeeCode FROM #BatchCodes b INNER JOIN dbo.Employees e ON e.EmployeeCode = b.EmployeeCode OPTION (LOOP JOIN);",
                connection,
                transaction))
            {
                cmd.CommandTimeout = 120;
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    existingInDb.Add(reader.GetString(0));
                }
            }

            using (var truncateCmd = new SqlCommand("TRUNCATE TABLE #BatchCodes;", connection, transaction))
            {
                await truncateCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // 2. Partition valid vs duplicate-in-DB rows
            var employeeTable = CreateEmployeeDataTable();
            int validCount = 0;

            foreach (var (raw, emp) in batch)
            {
                if (existingInDb.Contains(emp.EmployeeCode))
                {
                    recordError(
                        raw,
                        $"Row {raw.RowNumber}: EmployeeCode '{emp.EmployeeCode}' already exists in the database.");
                    continue;
                }

                employeeTable.Rows.Add(
                    emp.EmployeeCode,
                    emp.FirstName,
                    emp.LastName,
                    emp.Department,
                    emp.JobTitle,
                    emp.Salary,
                    emp.HireDate,
                    emp.ImportHistoryId);
                validCount++;
            }

            if (validCount == 0)
            {
                return 0;
            }

            // 3. Bulk insert valid employees into dbo.Employees via SqlBulkCopy
            using var bulkCopy = new SqlBulkCopy(
                connection,
                SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers,
                transaction)
            {
                DestinationTableName = "dbo.Employees",
                BatchSize = batchSize,
                BulkCopyTimeout = 120
            };

            bulkCopy.ColumnMappings.Add("EmployeeCode", "EmployeeCode");
            bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
            bulkCopy.ColumnMappings.Add("LastName", "LastName");
            bulkCopy.ColumnMappings.Add("Department", "Department");
            bulkCopy.ColumnMappings.Add("JobTitle", "JobTitle");
            bulkCopy.ColumnMappings.Add("Salary", "Salary");
            bulkCopy.ColumnMappings.Add("HireDate", "HireDate");
            bulkCopy.ColumnMappings.Add("ImportHistoryId", "ImportHistoryId");

            await bulkCopy.WriteToServerAsync(employeeTable, cancellationToken);
            return validCount;
        }

        private static async Task EnsureTempLookupTableAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            CancellationToken cancellationToken)
        {
            const string sql = @"
IF OBJECT_ID('tempdb..#BatchCodes') IS NULL
    CREATE TABLE #BatchCodes (EmployeeCode NVARCHAR(50) COLLATE DATABASE_DEFAULT NOT NULL);
ELSE
    TRUNCATE TABLE #BatchCodes;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private static DataTable CreateEmployeeDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("EmployeeCode", typeof(string)).MaxLength = 50;
            table.Columns.Add("FirstName", typeof(string)).MaxLength = 100;
            table.Columns.Add("LastName", typeof(string)).MaxLength = 100;
            table.Columns.Add("Department", typeof(string)).MaxLength = 100;
            table.Columns.Add("JobTitle", typeof(string)).MaxLength = 100;
            table.Columns.Add("Salary", typeof(decimal));
            table.Columns.Add("HireDate", typeof(DateTime));
            table.Columns.Add("ImportHistoryId", typeof(int));
            return table;
        }

        private static async Task UpdateImportHistoryInTransactionAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            int importHistoryId,
            int totalRows,
            int processedRows,
            int importedRows,
            int rejectedRows,
            string status,
            string? errorReportPath,
            string? errorMessage,
            CancellationToken cancellationToken)
        {
            const string sql = @"
UPDATE dbo.ImportHistory
SET TotalRows = @TotalRows,
    ProcessedRows = @ProcessedRows,
    ImportedRows = @ImportedRows,
    RejectedRows = @RejectedRows,
    Status = @Status,
    ErrorReportPath = @ErrorReportPath,
    ErrorMessage = @ErrorMessage
WHERE Id = @Id;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("@TotalRows", totalRows);
            cmd.Parameters.AddWithValue("@ProcessedRows", processedRows);
            cmd.Parameters.AddWithValue("@ImportedRows", importedRows);
            cmd.Parameters.AddWithValue("@RejectedRows", rejectedRows);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@ErrorReportPath", (object?)errorReportPath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Id", importHistoryId);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task FailJobAsync(
            ImportJobQueueItem item,
            int totalRecords,
            int processedRecords,
            int rejectedRecords,
            string errorMessage)
        {
            try
            {
                var history = await _context.ImportHistories.FirstOrDefaultAsync(h => h.Id == item.ImportHistoryId);
                if (history != null)
                {
                    history.TotalRows = totalRecords;
                    history.ProcessedRows = processedRecords;
                    history.ImportedRows = 0;
                    history.RejectedRows = rejectedRecords;
                    history.Status = "Failed";
                    history.ErrorMessage = errorMessage.Length > 1000 ? errorMessage[..1000] : errorMessage;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not update failed ImportHistory #{ImportHistoryId}", item.ImportHistoryId);
            }

            _jobStore.UpdateProgress(
                item.JobId,
                "Failed",
                totalRecords,
                processedRecords,
                0,
                rejectedRecords,
                errorMessage,
                new[] { errorMessage },
                isCompleted: true);
        }

        private static List<string> FlushSampleErrors(List<string> buffer)
        {
            if (buffer.Count == 0) return new List<string>();
            var copy = new List<string>(buffer);
            buffer.Clear();
            return copy;
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
