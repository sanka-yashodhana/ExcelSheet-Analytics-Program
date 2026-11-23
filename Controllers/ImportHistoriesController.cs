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
            var histories = await _context.ImportHistories
                .OrderByDescending(history => history.UploadedDate)
                .Select(history => new ImportHistoryResponseDto
                {
                    Id = history.Id,
                    FileName = history.FileName,
                    UploadedDate = history.UploadedDate,
                    TotalRows = history.TotalRows,
                    EmployeeCount = history.Employees.Count,
                    Status = history.Status
                })
                .ToListAsync();

            return Ok(histories);
        }

        // GET: api/importhistories/1/download
        [HttpGet("{id:int}/download")]
        public async Task<IActionResult> DownloadImportHistoryExcel(int id)
        {
            var history = await _context.ImportHistories
                .Include(h => h.Employees)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (history == null)
            {
                return NotFound(new { message = "Import history record not found." });
            }

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
            foreach (var employee in history.Employees.OrderBy(e => e.Id))
            {
                worksheet.Cell(row, 1).Value = employee.EmployeeCode;
                worksheet.Cell(row, 2).Value = employee.FirstName;
                worksheet.Cell(row, 3).Value = employee.LastName;
                worksheet.Cell(row, 4).Value = employee.Department;
                worksheet.Cell(row, 5).Value = employee.JobTitle;
                worksheet.Cell(row, 6).Value = employee.Salary;
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                worksheet.Cell(row, 7).Value = employee.HireDate.ToString("yyyy-MM-dd");
                row++;
            }

            worksheet.Columns().AdjustToContents();

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
