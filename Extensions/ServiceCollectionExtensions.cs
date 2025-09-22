using FluentValidation;
using FluentValidation.AspNetCore;
using Invoqs.API.Data;
using Invoqs.API.Interfaces;
using Invoqs.API.Mappings;
using Invoqs.API.Services;
using Invoqs.API.Validators;
using Microsoft.EntityFrameworkCore;

namespace Invoqs.API.Extensions;

/// <summary>
/// Extension methods for registering application services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register database services
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InvoqsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("InvoqsDBConnection")));

        return services;
    }

    /// <summary>
    /// Register FluentValidation validators
    /// </summary>
    public static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
            
        services.AddValidatorsFromAssemblyContaining<CreateCustomerValidator>();

        // Register validators explicitly for better control
        services.AddScoped<CreateCustomerValidator>();
        services.AddScoped<UpdateCustomerValidator>();
        services.AddScoped<CreateJobValidator>();
        services.AddScoped<UpdateJobValidator>();
        services.AddScoped<UpdateJobStatusValidator>();
        services.AddScoped<MarkJobsAsInvoicedValidator>();
        services.AddScoped<RemoveJobsFromInvoiceValidator>();
        services.AddScoped<CreateInvoiceValidator>();
        services.AddScoped<UpdateInvoiceValidator>();
        services.AddScoped<MarkInvoiceAsSentValidator>();
        services.AddScoped<MarkInvoiceAsPaidValidator>();

        return services;
    }

    /// <summary>
    /// Register AutoMapper profiles (optional but recommended)
    /// </summary>
    public static IServiceCollection AddMappingServices(this IServiceCollection services)
    {
        // AutoMapper configuration
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(typeof(Program).Assembly);
        });

        return services;
    }

    /// <summary>
    /// Register business logic services (to be implemented in Section 5)
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }

    /// <summary>
    /// Register API configuration services
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                options.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat;
                options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
            });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "Invoqs API",
                Version = "v1",
                Description = "API for Invoqs invoice management system"
            });
        });

        return services;
    }

    /// <summary>
    /// Register CORS policies for Blazor frontend
    /// </summary>
    public static IServiceCollection AddCorsServices(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("BlazorClient", builder =>
            {
                builder.WithOrigins(
                    // "https://localhost:5001", // Blazor HTTPS
                    "http://localhost:5086"  // Blazor HTTP
                    // "https://localhost:7001", // Alternative HTTPS
                    // "http://localhost:7000"   // Alternative HTTP
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
            });
        });

        return services;
    }
}