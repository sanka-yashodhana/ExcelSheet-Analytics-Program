namespace EmployeeExcelAnalytics.Api.Options
{
    public class ExcelImportOptions
    {
        public int MaxFileSizeMB { get; set; } = 100;
        public int MaxRecords { get; set; } = 500_000;
        public int BatchSize { get; set; } = 1_000;
        public int MaxInlineErrors { get; set; } = 100;
        public string TempStoragePath { get; set; } = "App_Data/Imports";
    }
}
