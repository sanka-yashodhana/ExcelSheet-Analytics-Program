
using EmployeeExcelAnalytics.Api.Services;
using EmployeeExcelAnalytics.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using EmployeeExcelAnalytics.Dtos;
using System.Text.Json;

namespace EmployeeExcelAnalytics.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExcelImportController : ControllerBase
    {
        private readonly IExcelImportService _excelImportService;
        private readonly IEmployeeImportService _employeeImportService;
        private readonly ExcelHeaderDetectionService _headerDetectionService;

        public ExcelImportController(
            IExcelImportService excelImportService,
            IEmployeeImportService employeeImportService,
            ExcelHeaderDetectionService headerDetectionService)
            
        {
            _excelImportService = excelImportService;
            _employeeImportService = employeeImportService;
            _headerDetectionService = headerDetectionService;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadExcel(
            [FromForm] IFormFile file,
            [FromForm] string? headerMappings)
        {
            // Check uploaded file
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Please select an Excel file."
                });
            }

            // Check file size
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new
                {
                    message = "File size must not exceed 5 MB."
                });
            }

            // Check extension
            string extension = Path.GetExtension(file.FileName);

            if (!string.Equals(
                extension,
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Only .xlsx Excel files are allowed."
                });
            }

            List<ExcelHeaderMappingDto>? mappings = null;
            if (!string.IsNullOrWhiteSpace(headerMappings))
            {
                try
                {
                    mappings = JsonSerializer.Deserialize<List<ExcelHeaderMappingDto>>(
                        headerMappings,
                        new JsonSerializerOptions(JsonSerializerDefaults.Web));
                }
                catch (JsonException)
                {
                    return BadRequest(new { message = "The supplied column mapping is invalid." });
                }

                if (mappings == null)
                {
                    return BadRequest(new { message = "The supplied column mapping is invalid." });
                }
            }

            // Read Excel file
            using var stream = file.OpenReadStream();

            var excelResult = _excelImportService.ReadEmployees(stream, mappings);

            // Check Excel validation
            if (!excelResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Excel validation failed.",
                    errors = excelResult.Erros
                });
            }

            try
            {
                // Save employees into SQL Server
                var saveResult =
                    await _employeeImportService.ImportEmployeesAsync(
                        excelResult.Employees,
                        Path.GetFileName(file.FileName));

                if (!saveResult.Success)
                {
                    return BadRequest(saveResult);
                }

                return Ok(saveResult);
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "A database conflict occurred. Check for duplicate employee codes."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred while importing employees."
                });
            }
        }


        [HttpPost("detect-headers")]
        public async Task<IActionResult> DetectHeaders(IFormFile file)
        {
            // 1. Check whether a file was uploaded
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please upload an Excel file.");
            }

            // 2. Check file extension
            var extension = Path.GetExtension(file.FileName);

            if (!string.Equals(
                extension,
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only .xlsx files are supported.");
            }

            // 3. Read Excel file
            using var stream = new MemoryStream();

            await file.CopyToAsync(stream);

            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return BadRequest("The Excel file has no worksheets.");
            }

            // 4. Read headers from the first row
            var headerRow = worksheet.Row(1);

            var lastHeaderColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            var headers = Enumerable.Range(1, lastHeaderColumn)
                .Select(column => headerRow.Cell(column).GetString().Trim())
                .ToList();

            if (headers.All(string.IsNullOrWhiteSpace))
            {
                return BadRequest("No headers found in the Excel file.");
            }

            // 5. Detect matching fields
            var result = _headerDetectionService.DetectHeaders(headers);

            // 6. Return detection result
            return Ok(result);
        }

    }
}
