using EmployeeExcelAnalytics.Api.Models;

namespace EmployeeExcelAnalytics.Api.Services
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetAllEmployeesAsync();

        Task<List<Employee>> GetEmployeesByImportHistoryIdAsync(int importHistoryId);

        Task<Employee?> GetEmployeeByIdAsync(int id);

        Task<int> DeleteAllEmployeesAsync();
    }
}