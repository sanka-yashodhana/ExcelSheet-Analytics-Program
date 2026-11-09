using ClosedXML.Excel;
using EmployeeExcelAnalytics.Api.DTOs;
using EmployeeExcelAnalytics.Api.Models;
using EmployeeExcelAnalytics.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace EmployeeExcelAnalytics.APi.Srvices
{
    public class ExcelImportService: IExcelImportService
    {
        public ExcelImportResultDto ReadEmployees(Stream fileStream)
        {
            var result = new ExcelImportResultDto();

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                //Get the worksheet
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if(workbook == null)
                {
                    result.Erros.Add("Excel file does not contain a worksheet");
                    return (result);
                }

                //Expected Columns headings
                string[] expectedHeaders =
                {
                    "EmployeeCode",
                    "FirstName",
                    "LastName",
                    "Department",
                    "JobTitle",
                    "Salary",
                    "HireDate"
                };

                //Validate Column Headings
                for(int i = 0; i < expectedHeaders.Length; i++)
                {
                    string actualHeader = worksheet.Cell(1, i + 1).GetString().Trim();

                    if(!string.Equals(actualHeader, expectedHeaders[i],
                        StringComparison.OrdinalIgnoreCase))
                    {
                        result.Erros.Add(
                            $"Invalid heading in cloumn {i + 1}. " +
                            $"Expected : {expectedHeaders[i]}");

                        return (result);
                    }
                }

                //Read employee records staring from row 2
                int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

                for (int row = 2; row <= lastRow; row++)
                {
                    string employeeCode = worksheet.Cell(row, 1).GetString().Trim();
                    string firstName = worksheet.Cell(row, 2).GetString().Trim();
                    string lastName = worksheet.Cell(row, 3).GetString().Trim();
                    string department = worksheet.Cell(row, 4).GetString().Trim();
                    string jobTitle = worksheet.Cell(row, 5).GetString().Trim();

                    //skip completely empty rows
                    bool isEmptyRow =
                        string.IsNullOrWhiteSpace(employeeCode) &&
                        string.IsNullOrWhiteSpace(firstName) &&
                        string.IsNullOrWhiteSpace(lastName) &&
                        string.IsNullOrWhiteSpace(department) &&
                        string.IsNullOrWhiteSpace(jobTitle) &&
                        worksheet.Cell(row, 6).IsEmpty() &&
                        worksheet.Cell(row, 7).IsEmpty();

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
                    if(!worksheet.Cell(row,6).TryGetValue<decimal>(out decimal salary) || salary <= 0)
                    {
                        result.Erros.Add(
                              $"Row{row}: Salary must be a valid positive number. "
                            );
                        continue;
                    }    
                    
                    //Validate hire date
                    if(!worksheet.Cell(row,7).TryGetValue<DateTime>(out DateTime hireDate))
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