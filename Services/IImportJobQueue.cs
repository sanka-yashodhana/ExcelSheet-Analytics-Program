using EmployeeExcelAnalytics.Api.DTOs;

namespace EmployeeExcelAnalytics.Api.Services
{
    public sealed class ImportJobQueueItem
    {
        public string JobId { get; init; } = string.Empty;
        public int ImportHistoryId { get; init; }
        public string FileName { get; init; } = string.Empty;
        public string TempFilePath { get; init; } = string.Empty;
        public List<ExcelHeaderMappingDto>? HeaderMappings { get; init; }
    }

    public interface IImportJobQueue
    {
        ValueTask EnqueueAsync(ImportJobQueueItem item, CancellationToken cancellationToken = default);
        ValueTask<ImportJobQueueItem> DequeueAsync(CancellationToken cancellationToken);
    }
}
