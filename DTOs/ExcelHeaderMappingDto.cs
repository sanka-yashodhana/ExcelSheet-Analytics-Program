
namespace EmployeeExcelAnalytics.Dtos
{
    public class ExcelHeaderMappingDto
    {
        public string ExcelHeader { get; set; } = string.Empty;

        public string? SuggestedField { get; set; }

        public bool IsRequired { get; set; }

        public bool IsMatched { get; set; }
    }
}
