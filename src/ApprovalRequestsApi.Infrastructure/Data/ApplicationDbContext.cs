using ApprovalRequestsApi.Domain.Entities;
using ApprovalRequestsApi.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ApprovalRequestsApi.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApprovalRequest> ApprovalRequests { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ApprovalRequestConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
