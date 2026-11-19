using EmployeeExcelAnalytics.Api.Models;

namespace EmployeeExcelAnalytics.Api.Repositories
{
    public interface IEmployeeRepository
    {
        Task<List<Employee>> GetAllAsync();

        Task<List<Employee>> GetByImportHistoryIdAsync(int importHistoryId);

        Task<Employee?> GetByIdAsync(int id);

        Task<Employee> AddAsync(Employee employee);

        Task<bool> ExistsByEmployeeCodeAsync(string employeeCode);

        Task<int> DeleteAllAsync();
    }
}