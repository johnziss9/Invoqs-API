using Invoqs.API.Extensions;
using Invoqs.API.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Invoqs API");
    
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Invoqs.API"));

    // Configure QuestPDF license
    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

    var connectionString = builder.Configuration.GetConnectionString("InvoqsDBConnection")
        ?? throw new InvalidOperationException("Connection string 'InvoqsDBConnection' not found.");

    // Configure EmailSettings
    builder.Services.Configure<Invoqs.API.Models.EmailSettings>(
        builder.Configuration.GetSection("EmailSettings"));

    // Register services using extension methods
    builder.Services.AddDatabaseServices(builder.Configuration);
    builder.Services.AddValidationServices();
    builder.Services.AddMappingServices();
    builder.Services.AddBusinessServices();
    builder.Services.AddApiServices();
    builder.Services.AddCorsServices();
    builder.Services.AddAuthenticationServices(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging();

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
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}