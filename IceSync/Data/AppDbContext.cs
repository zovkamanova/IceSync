using IceSync.Data;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Workflow> Workflows { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}