using System.Threading.Channels;

namespace EmployeeExcelAnalytics.Api.Services
{
    public class ImportJobQueue : IImportJobQueue
    {
        private readonly Channel<ImportJobQueueItem> _channel = Channel.CreateUnbounded<ImportJobQueueItem>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        public ValueTask EnqueueAsync(ImportJobQueueItem item, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(item, cancellationToken);
        }

        public ValueTask<ImportJobQueueItem> DequeueAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
