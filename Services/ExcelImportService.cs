using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Api.Options;
using EmployeeExcelAnalytics.Api.Services;
using Microsoft.Extensions.Options;

namespace EmployeeExcelAnalytics.APi.Srvices
{
    public class ExcelImportService : IExcelImportService
    {
        private readonly StreamingExcelReader _streamingReader;
        private readonly ExcelImportOptions _options;

        public ExcelImportService(
            StreamingExcelReader streamingReader,
            IOptions<ExcelImportOptions> options)
        {
            _streamingReader = streamingReader;
            _options = options.Value;
        }

        public ExcelImportResultDto ReadEmployees(
            Stream fileStream,
            IReadOnlyList<ExcelHeaderMappingDto>? headerMappings = null)
        {
            var result = new ExcelImportResultDto();

            try
            {
                using var bufferStream = new MemoryStream();
                fileStream.CopyTo(bufferStream);
                bufferStream.Position = 0;

                var excelHeaders = _streamingReader.ReadHeaders(bufferStream);
                var (isValidMapping, columnByField, mappingError) =
                    _streamingReader.ResolveColumnMappings(excelHeaders, headerMappings);

                if (!isValidMapping)
                {
                    result.Erros.Add(mappingError ?? "Invalid Excel headers.");
                    return result;
                }

                bufferStream.Position = 0;
                int rowCount = 0;

                foreach (var rawRow in _streamingReader.StreamRows(bufferStream, columnByField))
                {
                    rowCount++;
                    if (rowCount > _options.MaxRecords)
                    {
                        result.Erros.Add($"Excel file exceeds the maximum allowed limit of {_options.MaxRecords:N0} records.");
                        return result;
                    }

                    var validation = StreamingExcelReader.ValidateRow(rawRow);
                    if (!validation.IsValid || validation.Employee == null)
                    {
                        if (result.Erros.Count < _options.MaxInlineErrors)
                        {
                            result.Erros.Add(validation.ErrorMessage ?? $"Row {rawRow.RowNumber}: validation failed.");
                        }
                        continue;
                    }

                    result.Employees.Add(validation.Employee);
                }

                if (result.Employees.Count == 0 && result.Erros.Count == 0)
                {
                    result.Erros.Add("Excel file does not contain employee records");
                }
            }
            catch (Exception)
            {
                result.Erros.Add("Unable to read the Excel file. Please upload a valid .xlsx file");
            }

            return result;
        }
    }
}
