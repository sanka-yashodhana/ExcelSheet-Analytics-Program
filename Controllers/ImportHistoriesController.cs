using ClosedXML.Excel;
using EmployeeExcelAnalytics.Api.Data;
using EmployeeExcelAnalytics.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeExcelAnalytics.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportHistoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ImportHistoriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetImportHistories()
        {
            var rawHistories = await _context.ImportHistories
                .AsNoTracking()
                .OrderByDescending(history => history.UploadedDate)
                .Select(history => new
                {
                    history.Id,
                    history.FileName,
                    history.UploadedDate,
                    history.TotalRows,
                    history.ProcessedRows,
                    history.ImportedRows,
                    history.RejectedRows,
                    EmployeeCount = history.Employees.Count,
                    history.Status,
                    history.ErrorReportPath,
                    history.JobId,
                    history.ErrorMessage
                })
                .ToListAsync();

            var histories = rawHistories.Select(h => new ImportHistoryResponseDto
            {
                Id = h.Id,
                FileName = h.FileName,
                UploadedDate = h.UploadedDate,
                TotalRows = h.TotalRows,
                ProcessedRows = h.ProcessedRows > 0 ? h.ProcessedRows : h.TotalRows,
                ImportedRows = h.ImportedRows > 0 ? h.ImportedRows : (string.Equals(h.Status, "Completed", StringComparison.OrdinalIgnoreCase) ? h.TotalRows : 0),
                RejectedRows = h.RejectedRows,
                EmployeeCount = h.EmployeeCount,
                Status = h.Status,
                HasErrorReport = !string.IsNullOrWhiteSpace(h.ErrorReportPath) && System.IO.File.Exists(h.ErrorReportPath),
                JobId = h.JobId,
                ErrorMessage = h.ErrorMessage
            }).ToList();

            return Ok(histories);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetImportHistoryById(int id)
        {
            var h = await _context.ImportHistories
                .AsNoTracking()
                .Where(history => history.Id == id)
                .Select(history => new
                {
                    history.Id,
                    history.FileName,
                    history.UploadedDate,
                    history.TotalRows,
                    history.ProcessedRows,
                    history.ImportedRows,
                    history.RejectedRows,
                    EmployeeCount = history.Employees.Count,
                    history.Status,
                    history.ErrorReportPath,
                    history.JobId,
                    history.ErrorMessage
                })
                .FirstOrDefaultAsync();

            if (h == null)
            {
                return NotFound(new { message = "Import history record not found." });
            }

            return Ok(new ImportHistoryResponseDto
            {
                Id = h.Id,
                FileName = h.FileName,
                UploadedDate = h.UploadedDate,
                TotalRows = h.TotalRows,
                ProcessedRows = h.ProcessedRows > 0 ? h.ProcessedRows : h.TotalRows,
                ImportedRows = h.ImportedRows > 0 ? h.ImportedRows : (string.Equals(h.Status, "Completed", StringComparison.OrdinalIgnoreCase) ? h.TotalRows : 0),
                RejectedRows = h.RejectedRows,
                EmployeeCount = h.EmployeeCount,
                Status = h.Status,
                HasErrorReport = !string.IsNullOrWhiteSpace(h.ErrorReportPath) && System.IO.File.Exists(h.ErrorReportPath),
                JobId = h.JobId,
                ErrorMessage = h.ErrorMessage
            });
        }

        // GET: api/importhistories/1/error-report
        [HttpGet("{id:int}/error-report")]
        public async Task<IActionResult> DownloadErrorReport(int id)
        {
            var history = await _context.ImportHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id);

            if (history == null)
            {
                return NotFound(new { message = "Import history record not found." });
            }

            if (string.IsNullOrWhiteSpace(history.ErrorReportPath) || !System.IO.File.Exists(history.ErrorReportPath))
            {
                return NotFound(new { message = "No error report is available for this import history." });
            }

            var baseName = string.IsNullOrWhiteSpace(history.FileName)
                ? $"import_{id}"
                : Path.GetFileNameWithoutExtension(history.FileName);

            return PhysicalFile(history.ErrorReportPath, "text/csv; charset=utf-8", $"{baseName}_error_report.csv");
        }

        // DELETE: api/importhistories/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteImportHistory(int id)
        {
            var history = await _context.ImportHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id);

            if (history == null)
            {
                return NotFound(new { message = "Import history record not found." });
            }

            if (string.Equals(history.Status, "Queued", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(history.Status, "Reading", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(history.Status, "Importing", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = "Cannot delete a workbook while its import is still in progress." });
            }

            int deletedEmployees = 0;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                deletedEmployees = await _context.Employees
                    .Where(e => e.ImportHistoryId == id)
                    .ExecuteDeleteAsync();

                await _context.ImportHistories
                    .Where(h => h.Id == id)
                    .ExecuteDeleteAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            if (!string.IsNullOrWhiteSpace(history.ErrorReportPath) && System.IO.File.Exists(history.ErrorReportPath))
            {
                try
                {
                    System.IO.File.Delete(history.ErrorReportPath);
                }
                catch
                {
                    // Ignore non-critical error report file cleanup failure
                }
            }

            return Ok(new
            {
                message = $"Deleted workbook '{history.FileName}' and {deletedEmployees:N0} assigned employee record(s).",
                importHistoryId = id,
                deletedEmployees
            });
        }

        // GET: api/importhistories/1/download
        [HttpGet("{id:int}/download")]
        public async Task<IActionResult> DownloadImportHistoryExcel(int id)
        {
            var history = await _context.ImportHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id);

            if (history == null)
            {
                return NotFound(new { message = "Import history record not found." });
            }

            var employees = await _context.Employees
                .AsNoTracking()
                .Where(e => e.ImportHistoryId == id)
                .OrderBy(e => e.Id)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Employees");

            // Headers
            worksheet.Cell(1, 1).Value = "EmployeeCode";
            worksheet.Cell(1, 2).Value = "FirstName";
            worksheet.Cell(1, 3).Value = "LastName";
            worksheet.Cell(1, 4).Value = "Department";
            worksheet.Cell(1, 5).Value = "JobTitle";
            worksheet.Cell(1, 6).Value = "Salary";
            worksheet.Cell(1, 7).Value = "HireDate";

            var headerRange = worksheet.Range("A1:G1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#164e29");
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            bool formatIndividualSalaryCells = employees.Count <= 5000;
            foreach (var employee in employees)
            {
                worksheet.Cell(row, 1).Value = employee.EmployeeCode;
                worksheet.Cell(row, 2).Value = employee.FirstName;
                worksheet.Cell(row, 3).Value = employee.LastName;
                worksheet.Cell(row, 4).Value = employee.Department;
                worksheet.Cell(row, 5).Value = employee.JobTitle;
                worksheet.Cell(row, 6).Value = employee.Salary;
                if (formatIndividualSalaryCells)
                {
                    worksheet.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                }
                worksheet.Cell(row, 7).Value = employee.HireDate.ToString("yyyy-MM-dd");
                row++;
            }

            if (employees.Count <= 2000)
            {
                worksheet.Columns().AdjustToContents();
            }
            else
            {
                worksheet.Column(1).Width = 16;
                worksheet.Column(2).Width = 18;
                worksheet.Column(3).Width = 18;
                worksheet.Column(4).Width = 24;
                worksheet.Column(5).Width = 24;
                worksheet.Column(6).Width = 15;
                worksheet.Column(7).Width = 14;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var baseFileName = string.IsNullOrWhiteSpace(history.FileName) ? $"import_history_{id}.xlsx" : history.FileName;
            var downloadFileName = baseFileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? baseFileName
                : $"{baseFileName}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                downloadFileName);
        }
    }
}
