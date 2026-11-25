namespace EmployeeExcelAnalytics.Api.Services
{
    public class LargeExcelImportWorker : BackgroundService
    {
        private readonly IImportJobQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LargeExcelImportWorker> _logger;

        public LargeExcelImportWorker(
            IImportJobQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<LargeExcelImportWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Large Excel Import Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                ImportJobQueueItem item;
                try
                {
                    item = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<ILargeExcelImportProcessor>();
                    await processor.ProcessJobAsync(item, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception processing import job {JobId}", item.JobId);
                }
            }
        }
    }
}
