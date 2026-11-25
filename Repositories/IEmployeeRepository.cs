using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Api.Models;

namespace EmployeeExcelAnalytics.Api.Repositories
{
    public interface IEmployeeRepository
    {
        Task<List<Employee>> GetAllAsync();

        Task<List<Employee>> GetByImportHistoryIdAsync(int importHistoryId);

        Task<PagedEmployeesResponseDto> GetPagedAsync(
            int page,
            int pageSize,
            string? search,
            string? department,
            int? importHistoryId,
            string? sortBy,
            string? sortDir);

        Task<EmployeeAnalyticsSummaryDto> GetAnalyticsSummaryAsync();

        Task<Employee?> GetByIdAsync(int id);

        Task<Employee> AddAsync(Employee employee);

        Task<bool> ExistsByEmployeeCodeAsync(string employeeCode);

        Task<int> DeleteAllAsync();
    }
}
