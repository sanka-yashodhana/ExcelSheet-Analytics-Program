using EmployeeExcelAnalytics.Api.Data;
using EmployeeExcelAnalytics.Api.Options;
using EmployeeExcelAnalytics.Api.Repositories;
using EmployeeExcelAnalytics.Api.Services;
using EmployeeExcelAnalytics.APi.Srvices;
using EmployeeExcelAnalytics.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Excel Import limits
builder.Services.Configure<ExcelImportOptions>(builder.Configuration.GetSection("ExcelImport"));
var maxFileSizeMb = builder.Configuration.GetValue<long?>("ExcelImport:MaxFileSizeMB") ?? 100;
var maxRequestBytes = (maxFileSizeMb * 1024L * 1024L) + (2L * 1024L * 1024L);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = maxRequestBytes;
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxRequestBytes;
});

// Repository Registration
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

// Service Registration
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IEmployeeImportService, EmployeeImportService>();
builder.Services.AddScoped<ExcelHeaderDetectionService>();
builder.Services.AddScoped<StreamingExcelReader>();

// Large Dataset Import Background Worker & Job Infrastructure
builder.Services.AddSingleton<IImportJobStore, ImportJobStore>();
builder.Services.AddSingleton<IImportJobQueue, ImportJobQueue>();
builder.Services.AddScoped<ILargeExcelImportProcessor, LargeExcelImportProcessor>();
builder.Services.AddHostedService<LargeExcelImportWorker>();

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

app.Map("/api/{**catchAll}", () => Results.NotFound(new { message = "API endpoint not found." }));

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

// Ensure large-import schema columns/indexes exist and normalize any legacy non-standard departments efficiently
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.CanConnect())
        {
            DatabaseSchemaInitializer.EnsureSchemaAndIndexes(db);

            var unnormalizedEmployees = db.Employees
                .Where(e => !PredefinedDepartments.All.Contains(e.Department))
                .ToList();

            bool hasModifications = false;
            foreach (var employee in unnormalizedEmployees)
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
