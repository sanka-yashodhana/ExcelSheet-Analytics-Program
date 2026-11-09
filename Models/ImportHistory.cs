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

        [Required]
        public string Status { get; set; } = string.Empty;

        // Navigation Property
        public ICollection<Employee> Employees { get; set; }
            = new List<Employee>();
    }
}