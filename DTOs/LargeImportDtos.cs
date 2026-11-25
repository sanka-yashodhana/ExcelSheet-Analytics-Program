namespace EmployeeExcelAnalytics.Api.DTOs
{
    public class ExcelImportConfigDto
    {
        public int MaxFileSizeMB { get; set; }
        public long MaxFileSizeBytes { get; set; }
        public int MaxRecords { get; set; }
        public int BatchSize { get; set; }
    }

    public class StartImportResponseDto
    {
        public string JobId { get; set; } = string.Empty;
        public int ImportHistoryId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ImportJobProgressDto
    {
        public string JobId { get; set; } = string.Empty;
        public int ImportHistoryId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int ImportedRecords { get; set; }
        public int RejectedRecords { get; set; }
        public double Percentage { get; set; }
        public string CurrentMessage { get; set; } = string.Empty;
        public bool HasErrorReport { get; set; }
        public string? ErrorReportUrl { get; set; }
        public List<string> SampleErrors { get; set; } = new();
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class PagedEmployeesResponseDto
    {
        public List<EmployeeResponseDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalEmployeesInDb { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int DistinctDepartmentCount { get; set; }
    }

    public class DepartmentStatisticDto
    {
        public string Department { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
    }

    public class DepartmentSalaryStatisticDto
    {
        public string Department { get; set; } = string.Empty;
        public decimal TotalSalary { get; set; }
    }

    public class HiringTrendStatisticDto
    {
        public int Year { get; set; }
        public int EmployeeCount { get; set; }
    }

    public class EmployeeAnalyticsSummaryDto
    {
        public int TotalEmployees { get; set; }
        public int TotalDepartments { get; set; }
        public decimal AverageSalary { get; set; }
        public List<DepartmentStatisticDto> ByDepartment { get; set; } = new();
        public List<DepartmentSalaryStatisticDto> SalaryByDepartment { get; set; } = new();
        public List<HiringTrendStatisticDto> HiringTrends { get; set; } = new();
    }
}
