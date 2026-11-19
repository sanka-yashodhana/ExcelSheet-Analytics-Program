using Microsoft.AspNetCore.Mvc;
using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Api.Services;

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

        // GET: api/employees/1
        [HttpGet("{id}")]
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