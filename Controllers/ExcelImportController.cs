
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeExcelAnalytics.Api.Services;

namespace EmployeeExcelAnalytics.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExcelImportController : ControllerBase
    {
        private readonly IExcelImportService _excelImportService;
        private readonly IEmployeeImportService _employeeImportService;

        public ExcelImportController(
            IExcelImportService excelImportService,
            IEmployeeImportService employeeImportService)
        {
            _excelImportService = excelImportService;
            _employeeImportService = employeeImportService;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadExcel(IFormFile file)
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

            // Read Excel file
            using var stream = file.OpenReadStream();

            var excelResult = _excelImportService.ReadEmployees(stream);

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
    }
}
