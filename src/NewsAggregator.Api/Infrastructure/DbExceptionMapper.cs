using Microsoft.EntityFrameworkCore;

namespace NewsAggregator.Api.Infrastructure;

public static class DbExceptionMapper
{
    public static bool IsPostgresUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.NpgsqlException npg && npg.SqlState == "23505";
}
