using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NewsAggregator.Api.Controllers;
using NewsAggregator.Api.Data;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=newsagg;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.AddSecurityDefinition("UserId", new OpenApiSecurityScheme
    {
        Description = "Ідентифікатор користувача (GUID), наприклад 3fa85f64-5717-4562-b3fc-2c963f66afa6",
        Name = HeaderNames.UserId,
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "UserId"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var bases = app.Urls.Count > 0
            ? app.Urls
            : new[] { "http://localhost:5153" };
        foreach (var u in bases)
        {
            var root = u.TrimEnd('/');
            Console.WriteLine();
            Console.WriteLine($"   Swagger: {root}/swagger");
        }
    });
}
else
    app.UseHsts();
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

if (!string.Equals(
        Environment.GetEnvironmentVariable("MIGRATE_AT_STARTUP"), "0", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (string.Equals(Environment.GetEnvironmentVariable("SEED_LARGE"), "1", StringComparison.OrdinalIgnoreCase)
        && !await db.Articles.AnyAsync())
    {
        await LargeDataSeeder.SeedAsync(db, cancellationToken: default);
    }
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();

public partial class Program { }
