using EmployeeExcelAnalytics.Api.DTOs;

namespace EmployeeExcelAnalytics.Api.Services
{
    public interface IExcelImportService
    {
        ExcelImportResultDto ReadEmployees(Stream fileStream);
    }
}