using EmployeeExcelAnalytics.Api.Data;
using EmployeeExcelAnalytics.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeExcelAnalytics.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportHistoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ImportHistoriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetImportHistories()
        {
            var histories = await _context.ImportHistories
                .OrderByDescending(history => history.UploadedDate)
                .Select(history => new ImportHistoryResponseDto
                {
                    Id = history.Id,
                    FileName = history.FileName,
                    UploadedDate = history.UploadedDate,
                    TotalRows = history.TotalRows,
                    EmployeeCount = history.Employees.Count,
                    Status = history.Status
                })
                .ToListAsync();

            return Ok(histories);
        }
    }
}
