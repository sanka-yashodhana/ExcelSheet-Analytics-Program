
using Microsoft.EntityFrameworkCore;
using EmployeeExcelAnalytics.Api.Data;
using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Api.Models;

namespace EmployeeExcelAnalytics.Api.Services
{
    public class EmployeeImportService : IEmployeeImportService
    {
        private readonly AppDbContext _context;

        public EmployeeImportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<EmployeeImportSaveResultDto> ImportEmployeesAsync(
            List<Employee> employees,
            string fileName)
        {
            var result = new EmployeeImportSaveResultDto();

            // Check duplicate codes inside the uploaded Excel file
            var duplicateCodes = employees
                .GroupBy(e => e.EmployeeCode.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateCodes.Any())
            {
                result.Success = false;
                result.Message = "Duplicate employee codes found in Excel file.";

                result.Errors.AddRange(
                    duplicateCodes.Select(code =>
                        $"Employee code {code} appears more than once."));

                return result;
            }

            // Check whether employee codes already exist in SQL Server
            var employeeCodes = employees
                .Select(e => e.EmployeeCode.Trim())
                .ToList();

            var existingCodes = await _context.Employees
                .Where(e => employeeCodes.Contains(e.EmployeeCode))
                .Select(e => e.EmployeeCode)
                .ToListAsync();

            if (existingCodes.Any())
            {
                result.Success = false;
                result.Message = "Some employee codes already exist.";

                result.Errors.AddRange(
                    existingCodes.Select(code =>
                        $"Employee code {code} already exists in the database."));

                return result;
            }

            // Start database transaction
            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Create import history
                var importHistory = new ImportHistory
                {
                    FileName = fileName,
                    UploadedDate = DateTime.Now,
                    TotalRows = employees.Count,
                    Status = "Completed"
                };

                _context.ImportHistories.Add(importHistory);

                await _context.SaveChangesAsync();

                // Connect every employee to the import history
                foreach (var employee in employees)
                {
                    employee.ImportHistoryId = importHistory.Id;
                }

                // Add employee records
                await _context.Employees.AddRangeAsync(employees);

                await _context.SaveChangesAsync();

                // Confirm transaction
                await transaction.CommitAsync();

                result.Success = true;
                result.Message = "Employees imported successfully.";
                result.ImportHistoryId = importHistory.Id;
                result.TotalEmployees = employees.Count;

                return result;
            }
            catch
            {
                // Undo database changes if saving fails
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
