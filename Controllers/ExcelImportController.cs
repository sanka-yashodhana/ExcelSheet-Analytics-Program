using System.Text.Json;
using EmployeeExcelAnalytics.Api.Data;
using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Api.Models;
using EmployeeExcelAnalytics.Api.Options;
using EmployeeExcelAnalytics.Api.Services;
using EmployeeExcelAnalytics.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmployeeExcelAnalytics.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExcelImportController : ControllerBase
    {
        private readonly IExcelImportService _excelImportService;
        private readonly IEmployeeImportService _employeeImportService;
        private readonly ExcelHeaderDetectionService _headerDetectionService;
        private readonly StreamingExcelReader _streamingReader;
        private readonly IImportJobStore _jobStore;
        private readonly IImportJobQueue _jobQueue;
        private readonly AppDbContext _context;
        private readonly ExcelImportOptions _options;
        private readonly IWebHostEnvironment _env;

        public ExcelImportController(
            IExcelImportService excelImportService,
            IEmployeeImportService employeeImportService,
            ExcelHeaderDetectionService headerDetectionService,
            StreamingExcelReader streamingReader,
            IImportJobStore jobStore,
            IImportJobQueue jobQueue,
            AppDbContext context,
            IOptions<ExcelImportOptions> options,
            IWebHostEnvironment env)
        {
            _excelImportService = excelImportService;
            _employeeImportService = employeeImportService;
            _headerDetectionService = headerDetectionService;
            _streamingReader = streamingReader;
            _jobStore = jobStore;
            _jobQueue = jobQueue;
            _context = context;
            _options = options.Value;
            _env = env;
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            long maxBytes = (long)_options.MaxFileSizeMB * 1024 * 1024;
            return Ok(new ExcelImportConfigDto
            {
                MaxFileSizeMB = _options.MaxFileSizeMB,
                MaxFileSizeBytes = maxBytes,
                MaxRecords = _options.MaxRecords,
                BatchSize = _options.BatchSize
            });
        }

        [HttpPost("start")]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> StartLargeImport(
            [FromForm] IFormFile file,
            [FromForm] string? headerMappings)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Please select an Excel file." });
            }

            long maxBytes = (long)_options.MaxFileSizeMB * 1024 * 1024;
            if (file.Length > maxBytes)
            {
                return BadRequest(new { message = $"File size must not exceed {_options.MaxFileSizeMB} MB." });
            }

            string extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Only .xlsx Excel files are allowed." });
            }

            List<ExcelHeaderMappingDto>? mappings = null;
            if (!string.IsNullOrWhiteSpace(headerMappings))
            {
                try
                {
                    mappings = JsonSerializer.Deserialize<List<ExcelHeaderMappingDto>>(
                        headerMappings,
                        new JsonSerializerOptions(JsonSerializerDefaults.Web));
                }
                catch (JsonException)
                {
                    return BadRequest(new { message = "The supplied column mapping is invalid." });
                }

                if (mappings == null)
                {
                    return BadRequest(new { message = "The supplied column mapping is invalid." });
                }
            }

            string jobId = Guid.NewGuid().ToString("N");
            string uploadDir = Path.Combine(_env.ContentRootPath, _options.TempStoragePath);
            Directory.CreateDirectory(uploadDir);
            string tempFilePath = Path.Combine(uploadDir, $"{jobId}.xlsx");

            try
            {
                await using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await file.CopyToAsync(fs);
                }

                var headers = _streamingReader.ReadHeadersFromFile(tempFilePath);
                var (isValidMapping, _, mappingError) = _streamingReader.ResolveColumnMappings(headers, mappings);
                if (!isValidMapping)
                {
                    TryDeleteFile(tempFilePath);
                    return BadRequest(new { message = mappingError ?? "Invalid Excel column mapping." });
                }
            }
            catch (Exception)
            {
                TryDeleteFile(tempFilePath);
                return BadRequest(new { message = "Unable to read the Excel file. Please upload a valid .xlsx file." });
            }

            var safeFileName = Path.GetFileName(file.FileName);
            if (safeFileName.Length > 255)
            {
                safeFileName = safeFileName[^255..];
            }

            var history = new ImportHistory
            {
                FileName = safeFileName,
                UploadedDate = DateTime.Now,
                TotalRows = 0,
                ProcessedRows = 0,
                ImportedRows = 0,
                RejectedRows = 0,
                Status = "Queued",
                JobId = jobId
            };

            _context.ImportHistories.Add(history);
            await _context.SaveChangesAsync();

            _jobStore.InitializeJob(jobId, history.Id, safeFileName);
            await _jobQueue.EnqueueAsync(new ImportJobQueueItem
            {
                JobId = jobId,
                ImportHistoryId = history.Id,
                FileName = safeFileName,
                TempFilePath = tempFilePath,
                HeaderMappings = mappings
            });

            return Accepted(new StartImportResponseDto
            {
                JobId = jobId,
                ImportHistoryId = history.Id,
                FileName = safeFileName,
                Status = "Queued",
                Message = "Import queued for background processing."
            });
        }

        [HttpGet("jobs/{jobId}")]
        public async Task<IActionResult> GetJobProgress(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest(new { message = "Job ID is required." });
            }

            var job = _jobStore.GetJob(jobId);
            if (job != null)
            {
                return Ok(job);
            }

            var history = await _context.ImportHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.JobId == jobId);

            if (history == null)
            {
                return NotFound(new { message = "Import job not found." });
            }

            bool hasReport = !string.IsNullOrWhiteSpace(history.ErrorReportPath) && System.IO.File.Exists(history.ErrorReportPath);
            double percentage = history.TotalRows > 0
                ? Math.Clamp(Math.Round((double)history.ProcessedRows / history.TotalRows * 100.0, 1), 0, 100)
                : string.Equals(history.Status, "Completed", StringComparison.OrdinalIgnoreCase) ? 100 : 0;

            return Ok(new ImportJobProgressDto
            {
                JobId = jobId,
                ImportHistoryId = history.Id,
                FileName = history.FileName,
                Status = history.Status,
                TotalRecords = history.TotalRows,
                ProcessedRecords = history.ProcessedRows,
                ImportedRecords = history.ImportedRows,
                RejectedRecords = history.RejectedRows,
                Percentage = percentage,
                CurrentMessage = history.ErrorMessage ?? $"Import status: {history.Status}",
                HasErrorReport = hasReport,
                ErrorReportUrl = hasReport ? $"/api/ExcelImport/jobs/{jobId}/error-report" : null,
                SampleErrors = string.IsNullOrWhiteSpace(history.ErrorMessage) ? new List<string>() : new List<string> { history.ErrorMessage },
                StartedAt = history.UploadedDate
            });
        }

        [HttpGet("jobs/{jobId}/error-report")]
        public async Task<IActionResult> DownloadJobErrorReport(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest(new { message = "Job ID is required." });
            }

            string? path = _jobStore.GetErrorReportPath(jobId);
            string? originalFileName = _jobStore.GetJob(jobId)?.FileName;

            if (string.IsNullOrWhiteSpace(path))
            {
                var history = await _context.ImportHistories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.JobId == jobId);

                path = history?.ErrorReportPath;
                originalFileName ??= history?.FileName;
            }

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return NotFound(new { message = "No error report is available for this import job." });
            }

            var baseName = string.IsNullOrWhiteSpace(originalFileName)
                ? $"import_{jobId}"
                : Path.GetFileNameWithoutExtension(originalFileName);

            return PhysicalFile(path, "text/csv; charset=utf-8", $"{baseName}_error_report.csv");
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadExcel(
            [FromForm] IFormFile file,
            [FromForm] string? headerMappings)
        {
            // Check uploaded file
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Please select an Excel file."
                });
            }

            // Check configurable file size
            long maxBytes = (long)_options.MaxFileSizeMB * 1024 * 1024;
            if (file.Length > maxBytes)
            {
                return BadRequest(new
                {
                    message = $"File size must not exceed {_options.MaxFileSizeMB} MB."
                });
            }

            // Check extension
            string extension = Path.GetExtension(file.FileName);

            if (!string.Equals(
                extension,
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Only .xlsx Excel files are allowed."
                });
            }

            List<ExcelHeaderMappingDto>? mappings = null;
            if (!string.IsNullOrWhiteSpace(headerMappings))
            {
                try
                {
                    mappings = JsonSerializer.Deserialize<List<ExcelHeaderMappingDto>>(
                        headerMappings,
                        new JsonSerializerOptions(JsonSerializerDefaults.Web));
                }
                catch (JsonException)
                {
                    return BadRequest(new { message = "The supplied column mapping is invalid." });
                }

                if (mappings == null)
                {
                    return BadRequest(new { message = "The supplied column mapping is invalid." });
                }
            }

            // Read Excel file
            using var stream = file.OpenReadStream();

            var excelResult = _excelImportService.ReadEmployees(stream, mappings);

            // Check Excel validation
            if (!excelResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Excel validation failed.",
                    errors = excelResult.Erros
                });
            }

            try
            {
                // Save employees into SQL Server
                var saveResult =
                    await _employeeImportService.ImportEmployeesAsync(
                        excelResult.Employees,
                        Path.GetFileName(file.FileName));

                if (!saveResult.Success)
                {
                    return BadRequest(saveResult);
                }

                return Ok(saveResult);
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "A database conflict occurred. Check for duplicate employee codes."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred while importing employees."
                });
            }
        }

        [HttpPost("detect-headers")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> DetectHeaders(IFormFile file)
        {
            // 1. Check whether a file was uploaded
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please upload an Excel file.");
            }

            long maxBytes = (long)_options.MaxFileSizeMB * 1024 * 1024;
            if (file.Length > maxBytes)
            {
                return BadRequest($"File size must not exceed {_options.MaxFileSizeMB} MB.");
            }

            // 2. Check file extension
            var extension = Path.GetExtension(file.FileName);

            if (!string.Equals(
                extension,
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only .xlsx files are supported.");
            }

            // 3. Stream to a temporary file on disk to avoid large MemoryStream allocations on big Excel files
            string tempDir = Path.Combine(_env.ContentRootPath, _options.TempStoragePath);
            Directory.CreateDirectory(tempDir);
            string tempPath = Path.Combine(tempDir, $"hdr_{Guid.NewGuid():N}.xlsx");

            try
            {
                await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await file.CopyToAsync(fs);
                }

                var headers = _streamingReader.ReadHeadersFromFile(tempPath);

                if (headers.All(string.IsNullOrWhiteSpace))
                {
                    return BadRequest("No headers found in the Excel file.");
                }

                // 5. Detect matching fields
                var result = _headerDetectionService.DetectHeaders(headers);

                // 6. Return detection result
                return Ok(result);
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return BadRequest("Unable to read headers from the Excel file. Ensure it is a valid .xlsx workbook.");
            }
            finally
            {
                TryDeleteFile(tempPath);
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            catch
            {
                // Ignore temp cleanup failure
            }
        }
    }
}
