using IpAddressesAPI.HostedServices;
using IpAddressesAPI.IPDetails.Factory;
using IpAddressesAPI.IPDetails.Implementations;
using IpAddressesAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register in-memory cache service for caching data
builder.Services.AddMemoryCache();

// Register ApplicationDbContext with the Dependency Injection container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the concrete implementations of the Factory Design Pattern for the information retrieval
builder.Services.AddTransient<GetDetailsFromCache>();
builder.Services.AddTransient<GetDetailsFromDatabase>();
builder.Services.AddTransient<GetDetailsFromExternalSource>();

// Register the Factory to generate objects of the concrete classes
builder.Services.AddTransient<IPDetailsFactory>();

// Create a singleton instance of UpdateDataService (periodic job to check each IP's details)
builder.Services.AddSingleton(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<UpdateDataService>>();
    var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();

    // Ensure the periodic job runs every 1 hour
    var interval = TimeSpan.FromHours(1);

    return new UpdateDataService(serviceProvider, logger, memoryCache, interval);
});

// Register UpdateDataService as a background service
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<UpdateDataService>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
