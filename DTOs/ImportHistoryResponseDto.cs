namespace EmployeeExcelAnalytics.Api.DTOs
{
    public class ImportHistoryResponseDto
    {
        public int Id { get; set; }

        public string FileName { get; set; } = string.Empty;

        public DateTime UploadedDate { get; set; }

        public int TotalRows { get; set; }

        public int EmployeeCount { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
