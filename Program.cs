using EmployeeExcelAnalytics.Api.Data;
using EmployeeExcelAnalytics.Api.Repositories;
using EmployeeExcelAnalytics.Api.Services;
using EmployeeExcelAnalytics.APi.Srvices;
using EmployeeExcelAnalytics.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Repository Registration
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

//Service Registration
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IEmployeeImportService, EmployeeImportService>();
builder.Services.AddScoped<ExcelHeaderDetectionService>();

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularFrontend", policy =>
        policy.WithOrigins("http://localhost:4200", "https://your-production-app.com")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".mp4"] = "video/mp4";

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider
});

app.UseCors("AngularFrontend");
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.MapGet("/api/database-test", async (AppDbContext context) =>
{
    bool isConnected = await context.Database.CanConnectAsync();

    if (isConnected)
    {
        return Results.Ok(new
        {
            message = "Successfully connected to SQL Server!"
        });
    }

    return Results.Problem("Database connection failed.");
});

app.MapGet("/api/departments", () => Results.Ok(PredefinedDepartments.All));

// Normalize any existing employee records in the database to predefined departments
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.CanConnect())
        {
            var employees = db.Employees.ToList();
            bool hasModifications = false;
            foreach (var employee in employees)
            {
                if (PredefinedDepartments.TryNormalize(employee.Department, out var normalizedDept) &&
                    employee.Department != normalizedDept)
                {
                    employee.Department = normalizedDept;
                    hasModifications = true;
                }
            }

            if (hasModifications)
            {
                db.SaveChanges();
            }
        }
    }
    catch
    {
        // Safe fallback if database is unavailable at startup
    }
}

app.Run();
