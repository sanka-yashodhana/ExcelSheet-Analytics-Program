using System.Collections.Concurrent;
using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Api.Options;
using Microsoft.Extensions.Options;

namespace EmployeeExcelAnalytics.Api.Services
{
    public class ImportJobStore : IImportJobStore
    {
        private readonly ConcurrentDictionary<string, JobEntry> _jobs = new(StringComparer.OrdinalIgnoreCase);
        private readonly int _maxInlineErrors;

        private sealed class JobEntry
        {
            public readonly object Sync = new();
            public ImportJobProgressDto Progress { get; } = new();
            public string? ErrorReportPath { get; set; }
        }

        public ImportJobStore(IOptions<ExcelImportOptions> options)
        {
            _maxInlineErrors = Math.Max(10, options.Value.MaxInlineErrors);
        }

        public void InitializeJob(string jobId, int importHistoryId, string fileName, int totalRecords = 0)
        {
            var entry = new JobEntry();
            entry.Progress.JobId = jobId;
            entry.Progress.ImportHistoryId = importHistoryId;
            entry.Progress.FileName = fileName;
            entry.Progress.Status = "Queued";
            entry.Progress.TotalRecords = totalRecords;
            entry.Progress.ProcessedRecords = 0;
            entry.Progress.ImportedRecords = 0;
            entry.Progress.RejectedRecords = 0;
            entry.Progress.Percentage = 0;
            entry.Progress.CurrentMessage = "Queued for background processing...";
            entry.Progress.StartedAt = DateTime.UtcNow;

            _jobs[jobId] = entry;
            PruneOldEntriesIfNeeded();
        }

        public void UpdateProgress(
            string jobId,
            string status,
            int totalRecords,
            int processedRecords,
            int importedRecords,
            int rejectedRecords,
            string currentMessage,
            IEnumerable<string>? additionalErrors = null,
            string? errorReportPath = null,
            bool isCompleted = false)
        {
            if (!_jobs.TryGetValue(jobId, out var entry))
                return;

            lock (entry.Sync)
            {
                entry.Progress.Status = status;
                entry.Progress.TotalRecords = totalRecords;
                entry.Progress.ProcessedRecords = processedRecords;
                entry.Progress.ImportedRecords = importedRecords;
                entry.Progress.RejectedRecords = rejectedRecords;
                entry.Progress.CurrentMessage = currentMessage;

                if (totalRecords > 0)
                {
                    var pct = Math.Round((double)processedRecords / totalRecords * 100.0, 1);
                    entry.Progress.Percentage = Math.Clamp(pct, 0, 100);
                }
                else if (isCompleted && string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
                {
                    entry.Progress.Percentage = 0;
                }
                else if (isCompleted)
                {
                    entry.Progress.Percentage = 100;
                }

                if (!string.IsNullOrWhiteSpace(errorReportPath))
                {
                    entry.ErrorReportPath = errorReportPath;
                    entry.Progress.HasErrorReport = true;
                    entry.Progress.ErrorReportUrl = $"/api/ExcelImport/jobs/{jobId}/error-report";
                }

                if (additionalErrors != null)
                {
                    foreach (var err in additionalErrors)
                    {
                        if (entry.Progress.SampleErrors.Count >= _maxInlineErrors)
                            break;
                        entry.Progress.SampleErrors.Add(err);
                    }
                }

                if (isCompleted)
                {
                    entry.Progress.CompletedAt = DateTime.UtcNow;
                    if (!string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase) && totalRecords > 0)
                    {
                        entry.Progress.Percentage = 100;
                    }
                }
            }
        }

        public ImportJobProgressDto? GetJob(string jobId)
        {
            if (!_jobs.TryGetValue(jobId, out var entry))
                return null;

            lock (entry.Sync)
            {
                return new ImportJobProgressDto
                {
                    JobId = entry.Progress.JobId,
                    ImportHistoryId = entry.Progress.ImportHistoryId,
                    FileName = entry.Progress.FileName,
                    Status = entry.Progress.Status,
                    TotalRecords = entry.Progress.TotalRecords,
                    ProcessedRecords = entry.Progress.ProcessedRecords,
                    ImportedRecords = entry.Progress.ImportedRecords,
                    RejectedRecords = entry.Progress.RejectedRecords,
                    Percentage = entry.Progress.Percentage,
                    CurrentMessage = entry.Progress.CurrentMessage,
                    HasErrorReport = entry.Progress.HasErrorReport,
                    ErrorReportUrl = entry.Progress.ErrorReportUrl,
                    SampleErrors = new List<string>(entry.Progress.SampleErrors),
                    StartedAt = entry.Progress.StartedAt,
                    CompletedAt = entry.Progress.CompletedAt
                };
            }
        }

        public string? GetErrorReportPath(string jobId)
        {
            return _jobs.TryGetValue(jobId, out var entry) ? entry.ErrorReportPath : null;
        }

        private void PruneOldEntriesIfNeeded()
        {
            if (_jobs.Count <= 200) return;

            var oldestCompleted = _jobs
                .Where(kvp => kvp.Value.Progress.CompletedAt.HasValue)
                .OrderBy(kvp => kvp.Value.Progress.CompletedAt)
                .Take(50)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldestCompleted)
            {
                _jobs.TryRemove(key, out _);
            }
        }
    }
}
