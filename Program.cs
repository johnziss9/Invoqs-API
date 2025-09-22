using Invoqs.API.Data;
using Invoqs.API.Extensions;
using Invoqs.API.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("InvoqsDBConnection")
    ?? throw new InvalidOperationException("Connection string 'InvoqsDBConnection' not found.");

// Register services using extension methods
builder.Services.AddDatabaseServices(connectionString);
builder.Services.AddValidationServices();
builder.Services.AddMappingServices();
// builder.Services.AddBusinessServices(); // TODO: Implement in Section 5
builder.Services.AddApiServices();
builder.Services.AddCorsServices();

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

app.UseGlobalExceptionHandling();

app.UseHttpsRedirection();

app.UseCors("BlazorClient");

app.UseAuthorization();

app.MapControllers();

app.Run();