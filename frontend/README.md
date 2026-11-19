# Sheetwise frontend

Angular frontend for the EmployeeExcelAnalytics API.

## Run locally

1. Start the .NET backend with its `http` launch profile (`http://localhost:5239`).
2. In this directory, install packages and start Angular:

   ```sh
   npm install
   npm start
   ```

3. Open `http://localhost:4200`.

The API URL is configured in `src/app/services/analytics-api.service.ts`. The upload accepts `.xlsx` files up to 5 MB. The worksheet must start with these columns in order: `EmployeeCode`, `FirstName`, `LastName`, `Department`, `JobTitle`, `Salary`, `HireDate`.
