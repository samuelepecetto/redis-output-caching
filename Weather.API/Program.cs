using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.OutputCaching;

var builder = WebApplication
    .CreateSlimBuilder(args);

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddStackExchangeRedisOutputCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "Weather.API";
    })
    .AddOutputCache(options => options
        .AddBasePolicy(policyBuilder => policyBuilder
            .Expire(TimeSpan.FromMinutes(5))
            .With(c => c.HttpContext.Request.Path.StartsWithSegments("/weather"))
            .Tag("weather")));

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.RegisterWeatherEndpoint();
app.RegisterEvictionEndpoint();
app.UseOutputCache();
app.Run();

public static class WeApplicationExtensions
{
    public static void RegisterWeatherEndpoint(this WebApplication app)
    {
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering",
            "Scorching"
        };

        var weatherApi = app.MapGroup("/weather");
        app.MapGet("/weather/{city}", async (string city) =>
            {
                await Task.Delay(1000);
                return Results.Ok(new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ));
            })
            .WithName("GetWeatherForecast")
            .WithOpenApi()
            .CacheOutput();
    }

    public static void RegisterEvictionEndpoint(this WebApplication app)
    {
        app.MapPost("/purge/{tag}", async (IOutputCacheStore cache, string tag) =>
                await cache.EvictByTagAsync(tag, default))
            .WithName("Purge Cache by Tag")
            .WithOpenApi();
    }
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary);