using EmployeeExcelAnalytics.Api.Models;

namespace EmployeeExcelAnalytics.Api.DTOs
{
    public class  ExcelImportResultDto
    {
        public List<Employee> Employees { get; set; } = new();

        public List<String> Erros { get; set; } = new();

        public bool IsValid => Erros.Count == 0;
    }
}