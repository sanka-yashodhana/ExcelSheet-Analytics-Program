using System.ComponentModel.DataAnnotations;

namespace EmployeeExcelAnalytics.Api.Models
{
    public class ImportHistory
    {
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        public DateTime UploadedDate { get; set; }

        public int TotalRows { get; set; }

        public int ProcessedRows { get; set; }

        public int ImportedRows { get; set; }

        public int RejectedRows { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        public string? ErrorReportPath { get; set; }

        public string? JobId { get; set; }

        public string? ErrorMessage { get; set; }

        // Navigation Property
        public ICollection<Employee> Employees { get; set; }
            = new List<Employee>();
    }
}