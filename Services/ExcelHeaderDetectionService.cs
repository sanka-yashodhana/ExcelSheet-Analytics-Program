
using EmployeeExcelAnalytics.Api.DTOs;

namespace EmployeeExcelAnalytics.Services
{
    public class ExcelHeaderDetectionService
    {
        // Employee fields and their accepted Excel header names
        private readonly Dictionary<string, string[]> _headerMappings =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["EmployeeCode"] = new[]
                {
                    "EmployeeCode",
                    "Employee Code",
                    "EmployeeID",
                    "Employee ID",
                    "Emp ID",
                    "EmpCode"
                },

                ["FirstName"] = new[]
                {
                    "FirstName",
                    "First Name",
                    "Given Name",
                    "GivenName"
                },

                ["LastName"] = new[]
                {
                    "LastName",
                    "Last Name",
                    "Surname",
                    "Family Name"
                },

                ["Department"] = new[]
                {
                    "Department",
                    "Dept"
                },

                ["JobTitle"] = new[]
                {
                    "JobTitle",
                    "Job Title",
                    "Position",
                    "Designation"
                },

                ["Salary"] = new[]
                {
                    "Salary",
                    "Basic Salary",
                    "Monthly Salary"
                },

                ["HireDate"] = new[]
                {
                    "HireDate",
                    "Hire Date",
                    "Joining Date",
                    "Date Joined"
                }
            };

        // Required employee fields
        private readonly string[] _requiredFields =
        {
            "EmployeeCode",
            "FirstName",
            "LastName",
            "Department",
            "JobTitle",
            "Salary",
            "HireDate"
        };

        public ExcelHeaderDetectionResponseDto DetectHeaders(
            List<string> excelHeaders)
        {
            var response = new ExcelHeaderDetectionResponseDto();
            var suggestedFields = new string?[excelHeaders.Count];

            for (var fieldIndex = 0; fieldIndex < _headerMappings.Count; fieldIndex++)
            {
                var mapping = _headerMappings.ElementAt(fieldIndex);
                var normalizedAliases = mapping.Value
                    .Select(NormalizeHeader)
                    .ToHashSet(StringComparer.Ordinal);

                var matchingHeaderIndexes = Enumerable.Range(0, excelHeaders.Count)
                    .Where(index => normalizedAliases.Contains(
                        NormalizeHeader(excelHeaders[index])))
                    .ToList();

                if (matchingHeaderIndexes.Count == 0)
                {
                    continue;
                }

                var canonicalHeader = NormalizeHeader(mapping.Key);
                var selectedIndex = matchingHeaderIndexes.FirstOrDefault(
                    index => NormalizeHeader(excelHeaders[index]) == canonicalHeader,
                    matchingHeaderIndexes[0]);
                suggestedFields[selectedIndex] = mapping.Key;
            }

            for (var index = 0; index < excelHeaders.Count; index++)
            {
                var matchedField = suggestedFields[index];
                var header = excelHeaders[index];
                response.Headers.Add(new ExcelHeaderMappingDto
                {
                    ExcelHeader = header,
                    SuggestedField = matchedField,
                    IsMatched = matchedField != null,
                    IsRequired = matchedField != null &&
                                 _requiredFields.Contains(matchedField)
                });
            }

            // Find required fields that are missing
            var detectedFields = response.Headers
                .Where(h => h.SuggestedField != null)
                .Select(h => h.SuggestedField!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            response.MissingRequiredFields = _requiredFields
                .Where(field => !detectedFields.Contains(field))
                .ToList();

            response.IsValid = response.MissingRequiredFields.Count == 0;

            return response;
        }

        // Normalize headers before comparing them
        private static string NormalizeHeader(string header)
        {
            return new string(
                header.Where(char.IsLetterOrDigit)
                      .Select(char.ToLowerInvariant)
                      .ToArray()
            );
        }
    }
}
