using Microsoft.EntityFrameworkCore;
using UrPulse.Core.Entities;

namespace UrPulse.Core.Data;

public class UrPulseDbContext : DbContext
{
    public UrPulseDbContext(DbContextOptions<UrPulseDbContext> options) : base(options)
    {
    }

    public DbSet<HealthLog> HealthLogs { get; set; }
}