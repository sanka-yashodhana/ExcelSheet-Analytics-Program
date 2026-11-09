using EmployeeExcelAnalytics.Api.Models;

namespace EmployeeExcelAnalytics.Api.Repositories
{
    public interface IEmployeeRepository
    {
        Task<List<Employee>> GetAllAsync();

        Task<Employee?> GetByIdAsync(int id);

        Task<Employee> AddAsync(Employee employee);

        Task<bool> ExistsByEmployeeCodeAsync(string employeeCode);
    }
}