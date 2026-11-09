using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Api.Models;

namespace EmployeeExcelAnalytics.Api.Services
{
    public interface IEmployeeImportService
    {
        Task<EmployeeImportSaveResultDto> ImportEmployeesAsync(
            List<Employee> employees,
            String fileName);
    }
}