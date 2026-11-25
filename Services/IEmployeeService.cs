using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Api.Models;

namespace EmployeeExcelAnalytics.Api.Services
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetAllEmployeesAsync();

        Task<List<Employee>> GetEmployeesByImportHistoryIdAsync(int importHistoryId);

        Task<PagedEmployeesResponseDto> GetPagedEmployeesAsync(
            int page,
            int pageSize,
            string? search,
            string? department,
            int? importHistoryId,
            string? sortBy,
            string? sortDir);

        Task<EmployeeAnalyticsSummaryDto> GetAnalyticsSummaryAsync();

        Task<Employee?> GetEmployeeByIdAsync(int id);

        Task<int> DeleteAllEmployeesAsync();
    }
}
