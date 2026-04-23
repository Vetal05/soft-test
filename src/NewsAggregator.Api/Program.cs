using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Data;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=newsagg;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
