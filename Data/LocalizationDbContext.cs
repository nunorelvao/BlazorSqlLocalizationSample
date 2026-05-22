using Microsoft.EntityFrameworkCore;

namespace BlazorSqlLocalizationSample.Data;

public class LocalizationDbContext : DbContext
{
    public LocalizationDbContext(
        DbContextOptions<LocalizationDbContext> options)
        : base(options)
    {
    }

    public DbSet<LocalizationResource> LocalizationResources =>
        Set<LocalizationResource>();
}