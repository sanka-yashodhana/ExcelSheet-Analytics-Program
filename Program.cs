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
        policy.WithOrigins("http://localhost:4200")
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

app.UseCors("AngularFrontend");
app.UseAuthorization();

app.MapControllers();

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

app.Run();
