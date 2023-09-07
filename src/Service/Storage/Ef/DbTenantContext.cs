using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Passwordless.Service.AuditLog.Models;
using Passwordless.Service.Models;

namespace Passwordless.Service.Storage.Ef;

public abstract class DbTenantContext : DbGlobalContext
{
    public string Tenant { get; }

    public DbTenantContext(
        DbContextOptions options,
        ITenantProvider tenantProvider
    ) : base(options)
    {
        Tenant = tenantProvider.Tenant;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFStoredCredential>().HasQueryFilter(c => c.Tenant == Tenant);
        modelBuilder.Entity<TokenKey>().HasQueryFilter(c => c.Tenant == Tenant);
        modelBuilder.Entity<AccountMetaInformation>().HasQueryFilter(c => c.Tenant == Tenant);
        modelBuilder.Entity<ApiKeyDesc>().HasQueryFilter(c => c.Tenant == Tenant);
        modelBuilder.Entity<AliasPointer>().HasQueryFilter(c => c.Tenant == Tenant);
        modelBuilder.Entity<AppFeature>().HasQueryFilter(c => c.Tenant == Tenant);
        modelBuilder.Entity<ApplicationAuditEvent>().HasQueryFilter(c => c.TenantId == Tenant);

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        OnBeforeSaving();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default
    )
    {
        OnBeforeSaving();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess,
            cancellationToken);
    }

    private void OnBeforeSaving()
    {
        IEnumerable<EntityEntry> entries = ChangeTracker.Entries();

        foreach (EntityEntry entry in entries)
        {
            // for entities that inherit from PerTenant
            // Automatically set tenant value
            if (entry.Entity is PerTenant trackable)
            {
                trackable.Tenant = Tenant;
                if (entry.State == EntityState.Modified)
                {
                    entry.Property("Tenant").IsModified = false;
                }
            }
        }
    }
}