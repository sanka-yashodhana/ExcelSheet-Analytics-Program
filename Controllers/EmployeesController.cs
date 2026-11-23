using Microsoft.AspNetCore.Mvc;
using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Api.Services;
using EmployeeExcelAnalytics.Services;

namespace EmployeeExcelAnalytics.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        // GET: api/employees
        [HttpGet]
        public async Task<IActionResult> GetAllEmployees([FromQuery] int? importHistoryId)
        {
            if (importHistoryId.HasValue && importHistoryId.Value <= 0)
            {
                return BadRequest(new { message = "Import history ID must be a positive number." });
            }

            var employees = importHistoryId.HasValue
                ? await _employeeService.GetEmployeesByImportHistoryIdAsync(importHistoryId.Value)
                : await _employeeService.GetAllEmployeesAsync();

            var response = employees.Select(e => new EmployeeResponseDto
            {
                Id = e.Id,
                EmployeeCode = e.EmployeeCode,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Department = e.Department,
                JobTitle = e.JobTitle,
                Salary = e.Salary,
                HireDate = e.HireDate,
                ImportHistoryId = e.ImportHistoryId,
                ImportFileName = e.ImportHistory?.FileName ?? string.Empty
            });

            return Ok(response);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAllEmployees()
        {
            var deletedCount = await _employeeService.DeleteAllEmployeesAsync();

            return Ok(new
            {
                message = $"Deleted {deletedCount} employee records. Import history has been preserved.",
                deletedCount
            });
        }

        // GET: api/employees/download
        [HttpGet("download")]
        public async Task<IActionResult> DownloadExcel([FromQuery] int? importHistoryId)
        {
            var employees = importHistoryId.HasValue
                ? await _employeeService.GetEmployeesByImportHistoryIdAsync(importHistoryId.Value)
                : await _employeeService.GetAllEmployeesAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Employees");

            worksheet.Cell(1, 1).Value = "EmployeeCode";
            worksheet.Cell(1, 2).Value = "FirstName";
            worksheet.Cell(1, 3).Value = "LastName";
            worksheet.Cell(1, 4).Value = "Department";
            worksheet.Cell(1, 5).Value = "JobTitle";
            worksheet.Cell(1, 6).Value = "Salary";
            worksheet.Cell(1, 7).Value = "HireDate";

            var headerRange = worksheet.Range("A1:G1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#164e29");
            headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            int row = 2;
            foreach (var employee in employees.OrderBy(e => e.Id))
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

            var fileName = importHistoryId.HasValue ? $"employees_import_{importHistoryId.Value}.xlsx" : "employees_export.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // GET: api/employees/departments
        [HttpGet("departments")]
        public IActionResult GetPredefinedDepartments()
        {
            return Ok(PredefinedDepartments.All);
        }

        // GET: api/employees/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id);

            if (employee == null)
            {
                return NotFound(new
                {
                    message = "Employee not found."
                });
            }

            var response = new EmployeeResponseDto
            {
                Id = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Department = employee.Department,
                JobTitle = employee.JobTitle,
                Salary = employee.Salary,
                HireDate = employee.HireDate,
                ImportHistoryId = employee.ImportHistoryId,
                ImportFileName = employee.ImportHistory?.FileName ?? string.Empty
            };

            return Ok(response);
        }
    }
}