using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeExcelAnalytics.Api.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string Department { get; set; } = string.Empty;

        [Required]
        public string JobTitle { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }

        [Column(TypeName = "date")]
        public DateTime HireDate { get; set; }

        public int ImportHistoryId { get; set; }

        // Navigation Property
        public ImportHistory? ImportHistory { get; set; }
    }
}