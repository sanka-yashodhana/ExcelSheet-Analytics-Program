using Microsoft.EntityFrameworkCore;
using EmployeeExcelAnalytics.Api.Data;
using EmployeeExcelAnalytics.Api.Models;

namespace EmployeeExcelAnalytics.Api.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly AppDbContext _context;

        public EmployeeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Employee>> GetAllAsync()
        {
            return await _context.Employees.ToListAsync();
        }

        public async Task<List<Employee>> GetByImportHistoryIdAsync(int importHistoryId)
        {
            return await _context.Employees
                .Include(employee => employee.ImportHistory)
                .Where(employee => employee.ImportHistoryId == importHistoryId)
                .ToListAsync();
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            return await _context.Employees
                .Include(employee => employee.ImportHistory)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Employee> AddAsync(Employee employee)
        {
            await _context.Employees.AddAsync(employee);

            await _context.SaveChangesAsync();

            return employee;
        }

        public async Task<bool> ExistsByEmployeeCodeAsync(string employeeCode)
        {
            return await _context.Employees
                .AnyAsync(e => e.EmployeeCode == employeeCode);
        }

        public Task<int> DeleteAllAsync()
        {
            return _context.Employees.ExecuteDeleteAsync();
        }
    }
}