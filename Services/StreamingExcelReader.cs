using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Api.Models;
using EmployeeExcelAnalytics.Services;

namespace EmployeeExcelAnalytics.Api.Services
{
    public sealed class RawExcelEmployeeRow
    {
        public int RowNumber { get; init; }
        public string EmployeeCode { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Department { get; init; } = string.Empty;
        public string JobTitle { get; init; } = string.Empty;
        public string SalaryRaw { get; init; } = string.Empty;
        public string HireDateRaw { get; init; } = string.Empty;
    }

    public sealed class RowValidationOutcome
    {
        public bool IsValid { get; init; }
        public Employee? Employee { get; init; }
        public string? ErrorMessage { get; init; }
    }

    public class StreamingExcelReader
    {
        private static readonly Regex EmployeeCodePattern = new(@"^EMP\d{6,7}$", RegexOptions.Compiled);

        private static readonly string[] RequiredFields =
        {
            "EmployeeCode",
            "FirstName",
            "LastName",
            "Department",
            "JobTitle",
            "Salary",
            "HireDate"
        };

        private readonly ExcelHeaderDetectionService _headerDetectionService;

        public StreamingExcelReader(ExcelHeaderDetectionService headerDetectionService)
        {
            _headerDetectionService = headerDetectionService;
        }

        public List<string> ReadHeaders(Stream stream)
        {
            using var document = SpreadsheetDocument.Open(stream, false);
            return ReadHeadersFromDocument(document);
        }

        public List<string> ReadHeadersFromFile(string filePath)
        {
            using var document = SpreadsheetDocument.Open(filePath, false);
            return ReadHeadersFromDocument(document);
        }

        private static List<string> ReadHeadersFromDocument(SpreadsheetDocument document)
        {
            var workbookPart = document.WorkbookPart
                ?? throw new InvalidDataException("The Excel file has no workbook data.");

            var worksheetPart = GetFirstWorksheetPart(workbookPart)
                ?? throw new InvalidDataException("The Excel file has no worksheets.");

            string[]? sharedStrings = null;
            var headerValues = new SortedDictionary<int, string>();

            using var reader = OpenXmlReader.Create(worksheetPart);
            while (reader.Read())
            {
                if (reader.ElementType != typeof(Row))
                    continue;

                if (reader.IsStartElement)
                {
                    var row = (Row)reader.LoadCurrentElement()!;
                    uint rowIndex = row.RowIndex?.Value ?? 1;
                    if (rowIndex > 1)
                    {
                        break;
                    }

                    int fallbackCol = 1;
                    foreach (var cell in row.Elements<Cell>())
                    {
                        int colIndex = GetColumnIndex(cell.CellReference?.Value, ref fallbackCol);
                        if (cell.DataType?.Value == CellValues.SharedString && sharedStrings == null)
                        {
                            sharedStrings = LoadSharedStrings(workbookPart);
                        }

                        var text = ExtractCellValue(cell, sharedStrings).Trim();
                        headerValues[colIndex] = text;
                    }

                    break;
                }
            }

            if (headerValues.Count == 0)
            {
                return new List<string>();
            }

            int maxCol = headerValues.Keys.Max();
            var result = new List<string>(maxCol);
            for (int col = 1; col <= maxCol; col++)
            {
                result.Add(headerValues.TryGetValue(col, out var val) ? val : string.Empty);
            }

            // Trim trailing blank header cells cleanly to match LastCellUsed semantics
            while (result.Count > 0 && string.IsNullOrWhiteSpace(result[^1]))
            {
                result.RemoveAt(result.Count - 1);
            }

            return result;
        }

        public (bool IsValid, Dictionary<string, int> ColumnByField, string? ErrorMessage) ResolveColumnMappings(
            List<string> excelHeaders,
            IReadOnlyList<ExcelHeaderMappingDto>? suppliedMappings)
        {
            if (excelHeaders.Count == 0 || excelHeaders.All(string.IsNullOrWhiteSpace))
            {
                return (false, new Dictionary<string, int>(), "No headers found in the first row of the Excel file.");
            }

            IReadOnlyList<ExcelHeaderMappingDto> mappingsToUse;
            if (suppliedMappings == null || suppliedMappings.Count == 0)
            {
                var detected = _headerDetectionService.DetectHeaders(excelHeaders);
                if (!detected.IsValid)
                {
                    return (
                        false,
                        new Dictionary<string, int>(),
                        $"Missing or unrecognized required Excel headers: {string.Join(", ", detected.MissingRequiredFields)}.");
                }

                mappingsToUse = detected.Headers;
            }
            else
            {
                if (suppliedMappings.Any(m => m == null))
                {
                    return (false, new Dictionary<string, int>(), "The supplied column mapping is invalid.");
                }

                if (suppliedMappings.Count != excelHeaders.Count)
                {
                    return (false, new Dictionary<string, int>(), "The supplied column mapping does not match the uploaded workbook.");
                }

                for (int i = 0; i < suppliedMappings.Count; i++)
                {
                    if (!string.Equals(
                        suppliedMappings[i].ExcelHeader?.Trim(),
                        excelHeaders[i].Trim(),
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, new Dictionary<string, int>(), "The supplied column mapping does not match the uploaded workbook.");
                    }
                }

                mappingsToUse = suppliedMappings;
            }

            var mappedFields = mappingsToUse
                .Where(m => !string.IsNullOrWhiteSpace(m.SuggestedField))
                .Select(m => m.SuggestedField!.Trim())
                .ToList();

            if (mappedFields.Any(f => !RequiredFields.Contains(f, StringComparer.OrdinalIgnoreCase)) ||
                mappedFields.Distinct(StringComparer.OrdinalIgnoreCase).Count() != mappedFields.Count)
            {
                return (false, new Dictionary<string, int>(), "Each employee field can be mapped to only one Excel column.");
            }

            var missingRequired = RequiredFields
                .Where(f => !mappedFields.Contains(f, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (missingRequired.Count > 0)
            {
                return (
                    false,
                    new Dictionary<string, int>(),
                    $"Missing required column mappings: {string.Join(", ", missingRequired)}.");
            }

            var columnByField = mappingsToUse
                .Select((header, index) => new
                {
                    SuggestedField = header.SuggestedField?.Trim(),
                    Column = index + 1
                })
                .Where(m => !string.IsNullOrWhiteSpace(m.SuggestedField))
                .ToDictionary(m => m.SuggestedField!, m => m.Column, StringComparer.OrdinalIgnoreCase);

            return (true, columnByField, null);
        }

        public int CountNonEmptyDataRows(string filePath, Dictionary<string, int> columnByField)
        {
            using var document = SpreadsheetDocument.Open(filePath, false);
            var workbookPart = document.WorkbookPart;
            if (workbookPart == null) return 0;

            var worksheetPart = GetFirstWorksheetPart(workbookPart);
            if (worksheetPart == null) return 0;

            var sharedStrings = LoadSharedStrings(workbookPart);
            int codeCol = columnByField["EmployeeCode"];
            int firstCol = columnByField["FirstName"];
            int lastCol = columnByField["LastName"];
            int deptCol = columnByField["Department"];
            int titleCol = columnByField["JobTitle"];
            int salaryCol = columnByField["Salary"];
            int hireCol = columnByField["HireDate"];

            int count = 0;
            uint implicitRow = 0;

            using var reader = OpenXmlReader.Create(worksheetPart);
            while (reader.Read())
            {
                if (reader.ElementType != typeof(Row) || !reader.IsStartElement)
                    continue;

                implicitRow++;
                var rowEl = (Row)reader.LoadCurrentElement()!;
                uint rowNum = rowEl.RowIndex?.Value ?? implicitRow;
                if (rowNum <= 1) continue;

                bool hasAny = false;
                int fallbackCol = 1;
                foreach (var cell in rowEl.Elements<Cell>())
                {
                    int col = GetColumnIndex(cell.CellReference?.Value, ref fallbackCol);
                    if (col != codeCol && col != firstCol && col != lastCol &&
                        col != deptCol && col != titleCol && col != salaryCol && col != hireCol)
                    {
                        continue;
                    }

                    var val = ExtractCellValue(cell, sharedStrings);
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        hasAny = true;
                        break;
                    }
                }

                if (hasAny)
                {
                    count++;
                }
            }

            return count;
        }

        public IEnumerable<RawExcelEmployeeRow> StreamRows(
            string filePath,
            Dictionary<string, int> columnByField,
            CancellationToken cancellationToken = default)
        {
            using var document = SpreadsheetDocument.Open(filePath, false);
            foreach (var row in StreamRowsFromDocument(document, columnByField, cancellationToken))
            {
                yield return row;
            }
        }

        public IEnumerable<RawExcelEmployeeRow> StreamRows(
            Stream stream,
            Dictionary<string, int> columnByField,
            CancellationToken cancellationToken = default)
        {
            using var document = SpreadsheetDocument.Open(stream, false);
            foreach (var row in StreamRowsFromDocument(document, columnByField, cancellationToken))
            {
                yield return row;
            }
        }

        private static IEnumerable<RawExcelEmployeeRow> StreamRowsFromDocument(
            SpreadsheetDocument document,
            Dictionary<string, int> columnByField,
            CancellationToken cancellationToken)
        {
            var workbookPart = document.WorkbookPart
                ?? throw new InvalidDataException("Excel file does not contain a workbook.");

            var worksheetPart = GetFirstWorksheetPart(workbookPart)
                ?? throw new InvalidDataException("Excel file does not contain a worksheet.");

            var sharedStrings = LoadSharedStrings(workbookPart);

            int codeCol = columnByField["EmployeeCode"];
            int firstCol = columnByField["FirstName"];
            int lastCol = columnByField["LastName"];
            int deptCol = columnByField["Department"];
            int titleCol = columnByField["JobTitle"];
            int salaryCol = columnByField["Salary"];
            int hireCol = columnByField["HireDate"];

            uint implicitRow = 0;
            using var reader = OpenXmlReader.Create(worksheetPart);
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.ElementType != typeof(Row) || !reader.IsStartElement)
                    continue;

                implicitRow++;
                var rowEl = (Row)reader.LoadCurrentElement()!;
                int rowNum = (int)(rowEl.RowIndex?.Value ?? implicitRow);
                if (rowNum <= 1) continue;

                string employeeCode = string.Empty;
                string firstName = string.Empty;
                string lastName = string.Empty;
                string department = string.Empty;
                string jobTitle = string.Empty;
                string salaryRaw = string.Empty;
                string hireDateRaw = string.Empty;

                int fallbackCol = 1;
                foreach (var cell in rowEl.Elements<Cell>())
                {
                    int col = GetColumnIndex(cell.CellReference?.Value, ref fallbackCol);
                    if (col == codeCol)
                        employeeCode = ExtractCellValue(cell, sharedStrings).Trim();
                    else if (col == firstCol)
                        firstName = ExtractCellValue(cell, sharedStrings).Trim();
                    else if (col == lastCol)
                        lastName = ExtractCellValue(cell, sharedStrings).Trim();
                    else if (col == deptCol)
                        department = ExtractCellValue(cell, sharedStrings).Trim();
                    else if (col == titleCol)
                        jobTitle = ExtractCellValue(cell, sharedStrings).Trim();
                    else if (col == salaryCol)
                        salaryRaw = ExtractCellValue(cell, sharedStrings).Trim();
                    else if (col == hireCol)
                        hireDateRaw = ExtractCellValue(cell, sharedStrings).Trim();
                }

                bool isEmptyRow =
                    string.IsNullOrWhiteSpace(employeeCode) &&
                    string.IsNullOrWhiteSpace(firstName) &&
                    string.IsNullOrWhiteSpace(lastName) &&
                    string.IsNullOrWhiteSpace(department) &&
                    string.IsNullOrWhiteSpace(jobTitle) &&
                    string.IsNullOrWhiteSpace(salaryRaw) &&
                    string.IsNullOrWhiteSpace(hireDateRaw);

                if (isEmptyRow)
                    continue;

                yield return new RawExcelEmployeeRow
                {
                    RowNumber = rowNum,
                    EmployeeCode = employeeCode,
                    FirstName = firstName,
                    LastName = lastName,
                    Department = department,
                    JobTitle = jobTitle,
                    SalaryRaw = salaryRaw,
                    HireDateRaw = hireDateRaw
                };
            }
        }

        public static RowValidationOutcome ValidateRow(RawExcelEmployeeRow raw)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(raw.EmployeeCode))
                errors.Add("EmployeeCode is required");
            else if (raw.EmployeeCode.Length > 50)
                errors.Add("EmployeeCode exceeds max length of 50 characters");
            else if (!EmployeeCodePattern.IsMatch(raw.EmployeeCode))
                errors.Add($"EmployeeCode '{raw.EmployeeCode}' must match format EMP000001 (EMP followed by 6 or 7 digits)");

            if (string.IsNullOrWhiteSpace(raw.FirstName))
                errors.Add("FirstName is required");
            else if (raw.FirstName.Length > 100)
                errors.Add("FirstName exceeds max length of 100 characters");

            if (string.IsNullOrWhiteSpace(raw.LastName))
                errors.Add("LastName is required");
            else if (raw.LastName.Length > 100)
                errors.Add("LastName exceeds max length of 100 characters");

            string normalizedDepartment = string.Empty;
            if (string.IsNullOrWhiteSpace(raw.Department))
                errors.Add("Department is required");
            else if (!PredefinedDepartments.TryNormalize(raw.Department, out normalizedDepartment))
                errors.Add($"Department '{raw.Department}' is not recognized (allowed: {string.Join(", ", PredefinedDepartments.All)})");
            else if (normalizedDepartment.Length > 100)
                errors.Add("Department exceeds max length of 100 characters");

            if (string.IsNullOrWhiteSpace(raw.JobTitle))
                errors.Add("JobTitle is required");
            else if (raw.JobTitle.Length > 100)
                errors.Add("JobTitle exceeds max length of 100 characters");

            decimal salary = 0;
            if (string.IsNullOrWhiteSpace(raw.SalaryRaw))
            {
                errors.Add("Salary is required");
            }
            else if (!TryParseSalary(raw.SalaryRaw, out salary) || salary <= 0 || salary > 9999999999999999.99m)
            {
                errors.Add($"Salary '{raw.SalaryRaw}' must be a valid positive number");
            }

            DateTime hireDate = default;
            if (string.IsNullOrWhiteSpace(raw.HireDateRaw))
            {
                errors.Add("HireDate is required");
            }
            else if (!TryParseHireDate(raw.HireDateRaw, out hireDate))
            {
                errors.Add($"HireDate '{raw.HireDateRaw}' must be a valid date");
            }

            if (errors.Count > 0)
            {
                return new RowValidationOutcome
                {
                    IsValid = false,
                    ErrorMessage = $"Row {raw.RowNumber}: {string.Join("; ", errors)}."
                };
            }

            return new RowValidationOutcome
            {
                IsValid = true,
                Employee = new Employee
                {
                    EmployeeCode = raw.EmployeeCode,
                    FirstName = raw.FirstName,
                    LastName = raw.LastName,
                    Department = normalizedDepartment,
                    JobTitle = raw.JobTitle,
                    Salary = Math.Round(salary, 2, MidpointRounding.AwayFromZero),
                    HireDate = hireDate.Date
                }
            };
        }

        private static bool TryParseSalary(string raw, out decimal salary)
        {
            var cleaned = raw.Replace("$", string.Empty).Replace(",", string.Empty).Trim();
            return decimal.TryParse(
                cleaned,
                NumberStyles.Number | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out salary);
        }

        private static bool TryParseHireDate(string raw, out DateTime hireDate)
        {
            // Check Excel OADate serial numbers first (standard Excel numeric date storage)
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double oaSerial))
            {
                // Valid Excel serial dates between 1900-01-01 (1) and 2100-12-31 (73415)
                if (oaSerial >= 1 && oaSerial <= 73415)
                {
                    try
                    {
                        var dt = DateTime.FromOADate(oaSerial).Date;
                        if (dt.Year >= 1900 && dt.Year <= 2100)
                        {
                            hireDate = dt;
                            return true;
                        }
                    }
                    catch
                    {
                        // fall through to string date parsing
                    }
                }
            }

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed) ||
                DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out parsed))
            {
                if (parsed.Year >= 1900 && parsed.Year <= 2100)
                {
                    hireDate = parsed.Date;
                    return true;
                }
            }

            hireDate = default;
            return false;
        }

        private static WorksheetPart? GetFirstWorksheetPart(WorkbookPart workbookPart)
        {
            var firstSheet = workbookPart.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault();
            if (firstSheet?.Id?.Value is string relId)
            {
                return workbookPart.GetPartById(relId) as WorksheetPart;
            }
            return workbookPart.WorksheetParts.FirstOrDefault();
        }

        private static string[] LoadSharedStrings(WorkbookPart workbookPart)
        {
            var sstPart = workbookPart.SharedStringTablePart;
            if (sstPart == null) return Array.Empty<string>();

            var strings = new List<string>();
            using var reader = OpenXmlReader.Create(sstPart);
            while (reader.Read())
            {
                if (reader.ElementType == typeof(SharedStringItem) && reader.IsStartElement)
                {
                    var item = (SharedStringItem)reader.LoadCurrentElement()!;
                    strings.Add(item.InnerText ?? string.Empty);
                }
            }

            return strings.ToArray();
        }

        private static string ExtractCellValue(Cell cell, string[]? sharedStrings)
        {
            if (cell.DataType?.Value == CellValues.InlineString)
            {
                return cell.InlineString?.InnerText ?? cell.InnerText ?? string.Empty;
            }

            var rawValue = cell.CellValue?.Text ?? string.Empty;
            if (cell.DataType == null)
            {
                return rawValue;
            }

            var dataType = cell.DataType.Value;
            if (dataType == CellValues.SharedString)
            {
                if (sharedStrings != null &&
                    int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sstIndex) &&
                    sstIndex >= 0 && sstIndex < sharedStrings.Length)
                {
                    return sharedStrings[sstIndex];
                }
                return string.Empty;
            }

            if (dataType == CellValues.Boolean)
            {
                return rawValue == "1" ? "true" : "false";
            }

            if (dataType == CellValues.String || dataType == CellValues.Date || dataType == CellValues.Number)
            {
                return rawValue;
            }

            return cell.InnerText ?? rawValue;
        }

        private static int GetColumnIndex(string? cellReference, ref int fallbackColumn)
        {
            if (string.IsNullOrWhiteSpace(cellReference))
            {
                return fallbackColumn++;
            }

            int col = 0;
            for (int i = 0; i < cellReference.Length; i++)
            {
                char ch = cellReference[i];
                if (ch >= 'A' && ch <= 'Z')
                {
                    col = col * 26 + (ch - 'A' + 1);
                }
                else if (ch >= 'a' && ch <= 'z')
                {
                    col = col * 26 + (ch - 'a' + 1);
                }
                else
                {
                    break;
                }
            }

            if (col > 0)
            {
                fallbackColumn = col + 1;
                return col;
            }

            return fallbackColumn++;
        }
    }
}
