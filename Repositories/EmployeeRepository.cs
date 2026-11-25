using Microsoft.EntityFrameworkCore;
using EmployeeExcelAnalytics.Api.Data;
using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Api.Models;
using EmployeeExcelAnalytics.Services;

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
            return await _context.Employees
                .AsNoTracking()
                .Include(employee => employee.ImportHistory)
                .ToListAsync();
        }

        public async Task<List<Employee>> GetByImportHistoryIdAsync(int importHistoryId)
        {
            return await _context.Employees
                .AsNoTracking()
                .Include(employee => employee.ImportHistory)
                .Where(employee => employee.ImportHistoryId == importHistoryId)
                .ToListAsync();
        }

        public async Task<PagedEmployeesResponseDto> GetPagedAsync(
            int page,
            int pageSize,
            string? search,
            string? department,
            int? importHistoryId,
            string? sortBy,
            string? sortDir)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var baseQuery = _context.Employees.AsNoTracking().AsQueryable();

            if (importHistoryId.HasValue)
            {
                baseQuery = baseQuery.Where(e => e.ImportHistoryId == importHistoryId.Value);
            }

            int totalInScope = await baseQuery.CountAsync();
            int distinctDepartmentCount = await baseQuery
                .Select(e => e.Department)
                .Distinct()
                .CountAsync();

            var filteredQuery = baseQuery;

            if (!string.IsNullOrWhiteSpace(department))
            {
                var deptTrimmed = department.Trim();
                filteredQuery = filteredQuery.Where(e => e.Department == deptTrimmed);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                filteredQuery = filteredQuery.Where(e =>
                    e.FirstName.Contains(term) ||
                    e.LastName.Contains(term) ||
                    e.EmployeeCode.Contains(term) ||
                    e.Department.Contains(term) ||
                    e.JobTitle.Contains(term));
            }

            int filteredCount = await filteredQuery.CountAsync();
            int totalPages = filteredCount == 0
                ? 1
                : (int)Math.Ceiling(filteredCount / (double)pageSize);

            if (page > totalPages)
            {
                page = totalPages;
            }

            bool descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

            IOrderedQueryable<Employee> orderedQuery = (sortBy?.Trim().ToLowerInvariant()) switch
            {
                "name" => descending
                    ? filteredQuery.OrderByDescending(e => e.FirstName).ThenByDescending(e => e.LastName)
                    : filteredQuery.OrderBy(e => e.FirstName).ThenBy(e => e.LastName),
                "employeecode" or "code" => descending
                    ? filteredQuery.OrderByDescending(e => e.EmployeeCode)
                    : filteredQuery.OrderBy(e => e.EmployeeCode),
                "department" => descending
                    ? filteredQuery.OrderByDescending(e => e.Department)
                    : filteredQuery.OrderBy(e => e.Department),
                "salary" => descending
                    ? filteredQuery.OrderByDescending(e => e.Salary)
                    : filteredQuery.OrderBy(e => e.Salary),
                "hiredate" => descending
                    ? filteredQuery.OrderByDescending(e => e.HireDate)
                    : filteredQuery.OrderBy(e => e.HireDate),
                _ => descending
                    ? filteredQuery.OrderByDescending(e => e.Id)
                    : filteredQuery.OrderBy(e => e.Id)
            };

            var items = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EmployeeResponseDto
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
                    ImportFileName = e.ImportHistory != null ? e.ImportHistory.FileName : string.Empty
                })
                .ToListAsync();

            return new PagedEmployeesResponseDto
            {
                Items = items,
                TotalCount = filteredCount,
                TotalEmployeesInDb = totalInScope,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                DistinctDepartmentCount = distinctDepartmentCount
            };
        }

        public async Task<EmployeeAnalyticsSummaryDto> GetAnalyticsSummaryAsync()
        {
            var rawDepartmentStats = await _context.Employees
                .AsNoTracking()
                .GroupBy(e => e.Department)
                .Select(g => new
                {
                    Department = g.Key,
                    Count = g.Count(),
                    TotalSalary = g.Sum(x => x.Salary)
                })
                .ToListAsync();

            var deptCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var deptSalaries = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            int totalEmployees = 0;
            decimal totalSalarySum = 0m;

            foreach (var row in rawDepartmentStats)
            {
                totalEmployees += row.Count;
                totalSalarySum += row.TotalSalary;

                if (PredefinedDepartments.TryNormalize(row.Department, out var normalized))
                {
                    deptCounts[normalized] = deptCounts.GetValueOrDefault(normalized) + row.Count;
                    deptSalaries[normalized] = deptSalaries.GetValueOrDefault(normalized) + row.TotalSalary;
                }
            }

            var byDepartment = deptCounts
                .Select(kvp => new DepartmentStatisticDto
                {
                    Department = kvp.Key,
                    EmployeeCount = kvp.Value
                })
                .OrderByDescending(d => d.EmployeeCount)
                .ToList();

            var salaryByDepartment = deptSalaries
                .Select(kvp => new DepartmentSalaryStatisticDto
                {
                    Department = kvp.Key,
                    TotalSalary = kvp.Value
                })
                .OrderByDescending(d => d.TotalSalary)
                .ToList();

            var hiringTrends = await _context.Employees
                .AsNoTracking()
                .GroupBy(e => e.HireDate.Year)
                .Select(g => new HiringTrendStatisticDto
                {
                    Year = g.Key,
                    EmployeeCount = g.Count()
                })
                .OrderBy(t => t.Year)
                .ToListAsync();

            return new EmployeeAnalyticsSummaryDto
            {
                TotalEmployees = totalEmployees,
                TotalDepartments = byDepartment.Count,
                AverageSalary = totalEmployees > 0
                    ? Math.Round(totalSalarySum / totalEmployees, 2, MidpointRounding.AwayFromZero)
                    : 0m,
                ByDepartment = byDepartment,
                SalaryByDepartment = salaryByDepartment,
                HiringTrends = hiringTrends
            };
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            return await _context.Employees
                .AsNoTracking()
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
