namespace EmployeeExcelAnalytics.Api.DTOs
{
    public class EmployeeImportSaveResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ImportHistoryId { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int RejectedRecords { get; set; }
        public string? JobId { get; set; }
        public bool HasErrorReport { get; set; }
        public string FileName { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }
}