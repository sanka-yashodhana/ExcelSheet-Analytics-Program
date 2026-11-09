using EmployeeExcelAnalytics.Api.Models;
using EmployeeExcelAnalytics.Api.Repositories;

namespace EmployeeExcelAnalytics.Api.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeService(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            return await _employeeRepository.GetAllAsync();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            if (id <= 0)
            {
                return null;
            }

            return await _employeeRepository.GetByIdAsync(id);
        }
    }
}