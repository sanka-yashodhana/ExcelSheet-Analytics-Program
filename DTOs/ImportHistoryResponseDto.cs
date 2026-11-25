namespace EmployeeExcelAnalytics.Api.DTOs
{
    public class ImportHistoryResponseDto
    {
        public int Id { get; set; }

        public string FileName { get; set; } = string.Empty;

        public DateTime UploadedDate { get; set; }

        public int TotalRows { get; set; }

        public int ProcessedRows { get; set; }

        public int ImportedRows { get; set; }

        public int RejectedRows { get; set; }

        public int EmployeeCount { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool HasErrorReport { get; set; }

        public string? JobId { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
