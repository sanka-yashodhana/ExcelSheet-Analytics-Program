using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Dtos;

namespace EmployeeExcelAnalytics.Api.Services
{
    public interface IExcelImportService
    {
        ExcelImportResultDto ReadEmployees(
            Stream fileStream,
            IReadOnlyList<ExcelHeaderMappingDto>? headerMappings = null);
    }
}