namespace EmployeeExcelAnalytics.Api.Services
{
    public interface ILargeExcelImportProcessor
    {
        Task ProcessJobAsync(ImportJobQueueItem item, CancellationToken cancellationToken);
    }
}
