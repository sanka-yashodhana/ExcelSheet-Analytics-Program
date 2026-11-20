using ClosedXML.Excel;
using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Api.Models;
using EmployeeExcelAnalytics.Api.Services;
using EmployeeExcelAnalytics.Dtos;
using EmployeeExcelAnalytics.Services;

namespace EmployeeExcelAnalytics.APi.Srvices
{
    public class ExcelImportService: IExcelImportService
    {
        private readonly ExcelHeaderDetectionService _headerDetectionService;

        public ExcelImportService(ExcelHeaderDetectionService headerDetectionService)
        {
            _headerDetectionService = headerDetectionService;
        }

        public ExcelImportResultDto ReadEmployees(
            Stream fileStream,
            IReadOnlyList<ExcelHeaderMappingDto>? headerMappings = null)
        {
            var result = new ExcelImportResultDto();

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                //Get the worksheet
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if(worksheet == null)
                {   
                    result.Erros.Add("Excel file does not contain a worksheet.");
                    return (result);
                }

                var lastHeaderColumn = worksheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
                if (lastHeaderColumn == 0)
                {
                    result.Erros.Add("No headers found in the first row of the Excel file.");
                    return result;
                }

                var excelHeaders = Enumerable.Range(1, lastHeaderColumn)
                    .Select(column => worksheet.Cell(1, column).GetString().Trim())
                    .ToList();
                IReadOnlyList<ExcelHeaderMappingDto> mappingsToUse;
                if (headerMappings == null)
                {
                    var headerDetection = _headerDetectionService.DetectHeaders(excelHeaders);
                    if (!headerDetection.IsValid)
                    {
                        result.Erros.Add(
                            $"Missing or unrecognized required Excel headers: {string.Join(", ", headerDetection.MissingRequiredFields)}.");
                        return result;
                    }

                    mappingsToUse = headerDetection.Headers;
                }
                else
                {
                    if (headerMappings.Any(mapping => mapping == null))
                    {
                        result.Erros.Add("The supplied column mapping is invalid.");
                        return result;
                    }

                    if (headerMappings.Count != excelHeaders.Count)
                    {
                        result.Erros.Add("The supplied column mapping does not match the uploaded workbook.");
                        return result;
                    }

                    for (var index = 0; index < headerMappings.Count; index++)
                    {
                        if (!string.Equals(
                            headerMappings[index].ExcelHeader?.Trim(),
                            excelHeaders[index],
                            StringComparison.OrdinalIgnoreCase))
                        {
                            result.Erros.Add("The supplied column mapping does not match the uploaded workbook.");
                            return result;
                        }
                    }

                    mappingsToUse = headerMappings;
                }

                var requiredFields = new[]
                {
                    "EmployeeCode",
                    "FirstName",
                    "LastName",
                    "Department",
                    "JobTitle",
                    "Salary",
                    "HireDate"
                };
                var mappedFields = mappingsToUse
                    .Where(mapping => !string.IsNullOrWhiteSpace(mapping.SuggestedField))
                    .Select(mapping => mapping.SuggestedField!.Trim())
                    .ToList();
                if (mappedFields.Any(field => !requiredFields.Contains(field, StringComparer.OrdinalIgnoreCase)) ||
                    mappedFields.Distinct(StringComparer.OrdinalIgnoreCase).Count() != mappedFields.Count)
                {
                    result.Erros.Add("Each employee field can be mapped to only one Excel column.");
                    return result;
                }

                var missingRequiredFields = requiredFields
                    .Where(field => !mappedFields.Contains(field, StringComparer.OrdinalIgnoreCase))
                    .ToList();
                if (missingRequiredFields.Count > 0)
                {
                    result.Erros.Add(
                        $"Missing required column mappings: {string.Join(", ", missingRequiredFields)}.");
                    return result;
                }

                var columnByField = mappingsToUse
                    .Select((header, index) => new
                    {
                        SuggestedField = header.SuggestedField?.Trim(),
                        Column = index + 1
                    })
                    .Where(mapping => mapping.SuggestedField != null)
                    .ToDictionary(
                        mapping => mapping.SuggestedField!,
                        mapping => mapping.Column,
                        StringComparer.OrdinalIgnoreCase);

                //Read employee records staring from row 2
                int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

                for (int row = 2; row <= lastRow; row++)
                {
                    string employeeCode = worksheet.Cell(row, columnByField["EmployeeCode"]).GetString().Trim();
                    string firstName = worksheet.Cell(row, columnByField["FirstName"]).GetString().Trim();
                    string lastName = worksheet.Cell(row, columnByField["LastName"]).GetString().Trim();
                    string department = worksheet.Cell(row, columnByField["Department"]).GetString().Trim();
                    string jobTitle = worksheet.Cell(row, columnByField["JobTitle"]).GetString().Trim();

                    //skip completely empty rows
                    bool isEmptyRow =
                        string.IsNullOrWhiteSpace(employeeCode) &&
                        string.IsNullOrWhiteSpace(firstName) &&
                        string.IsNullOrWhiteSpace(lastName) &&
                        string.IsNullOrWhiteSpace(department) &&
                        string.IsNullOrWhiteSpace(jobTitle) &&
                        worksheet.Cell(row, columnByField["Salary"]).IsEmpty() &&
                        worksheet.Cell(row, columnByField["HireDate"]).IsEmpty();

                    if (isEmptyRow)
                    {
                        continue;
                    }

                    //Validate required text fields
                    if (string.IsNullOrWhiteSpace(employeeCode) ||
                        string.IsNullOrWhiteSpace(firstName) ||
                        string.IsNullOrWhiteSpace(lastName) ||
                        string.IsNullOrWhiteSpace(department) ||
                        string.IsNullOrWhiteSpace(jobTitle))
                    {
                        result.Erros.Add($"Row {row}: required information is missing.");
                        continue;
                    }

                    //Validate salary
                    if(!worksheet.Cell(row, columnByField["Salary"]).TryGetValue<decimal>(out decimal salary) || salary <= 0)
                    {
                        result.Erros.Add(
                              $"Row{row}: Salary must be a valid positive number. "
                            );
                        continue;
                    }    
                    
                    //Validate hire date
                    if(!worksheet.Cell(row, columnByField["HireDate"]).TryGetValue<DateTime>(out DateTime hireDate))
                    {
                        result.Erros.Add(
                            $"Row{row}: HireDate must be a valid Excel date");
                        continue;
                    }

                    //Create Employee object
                    var employee = new Employee
                    {
                        EmployeeCode = employeeCode,
                        FirstName = firstName,
                        LastName = lastName,
                        Department = department,
                        JobTitle = jobTitle,
                        Salary = salary,
                        HireDate = hireDate
                    };

                    result.Employees.Add(employee);
                }

                if(result.Employees.Count == 0 && result.Erros.Count == 0)
                {
                    result.Erros.Add("Excel file does not contain employee records");
                }
            }
            catch(Exception)
            {
                result.Erros.Add("Unable to read the Excel file. Please upload a valid .xlsx file");
            }
            return result;
        }
    }
}