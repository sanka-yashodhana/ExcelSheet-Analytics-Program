using EmployeeExcelAnalytics.Api.Models;

namespace EmployeeExcelAnalytics.Api.Services
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetAllEmployeesAsync();

        Task<Employee?> GetEmployeeByIdAsync(int id);
    }
}