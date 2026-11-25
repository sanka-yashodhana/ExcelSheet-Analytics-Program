using EmployeeExcelAnalytics.Api.DTOs;

namespace EmployeeExcelAnalytics.Api.Services
{
    public interface IImportJobStore
    {
        void InitializeJob(string jobId, int importHistoryId, string fileName, int totalRecords = 0);
        void UpdateProgress(
            string jobId,
            string status,
            int totalRecords,
            int processedRecords,
            int importedRecords,
            int rejectedRecords,
            string currentMessage,
            IEnumerable<string>? additionalErrors = null,
            string? errorReportPath = null,
            bool isCompleted = false);
        ImportJobProgressDto? GetJob(string jobId);
        string? GetErrorReportPath(string jobId);
    }
}
