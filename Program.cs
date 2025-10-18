using Invoqs.API.Extensions;
using Invoqs.API.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure QuestPDF license
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var connectionString = builder.Configuration.GetConnectionString("InvoqsDBConnection")
    ?? throw new InvalidOperationException("Connection string 'InvoqsDBConnection' not found.");

// Register services using extension methods
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddValidationServices();
builder.Services.AddMappingServices();
builder.Services.AddBusinessServices();
builder.Services.AddApiServices();
builder.Services.AddCorsServices();
builder.Services.AddAuthenticationServices(builder.Configuration);

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Invoqs API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseCors("BlazorClient");

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();