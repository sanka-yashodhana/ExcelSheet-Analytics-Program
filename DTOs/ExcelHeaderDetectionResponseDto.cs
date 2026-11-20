namespace EmployeeExcelAnalytics.Api.DTOs
{
    public class ExcelHeaderDetectionResponseDto
    {
        public List<ExcelHeaderMappingDto> Headers { get; set; } = new();

        public bool IsValid { get; set; }

        public List<string> MissingRequiredFields { get; set; } = new();
    }
}