using Microsoft.EntityFrameworkCore;
using Invoqs.API.Models;

namespace Invoqs.API.Data;

public class InvoqsDbContext : DbContext
{
    public InvoqsDbContext(DbContextOptions<InvoqsDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Soft delete global query filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Job entity
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Type).HasConversion<int>();

            // Foreign key relationship
            entity.HasOne(j => j.Customer)
                  .WithMany(c => c.Jobs)
                  .HasForeignKey(j => j.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Foreign key relationship for Invoice (optional)
            entity.HasOne(j => j.Invoice)
                  .WithMany()
                  .HasForeignKey(j => j.InvoiceId)
                  .OnDelete(DeleteBehavior.SetNull);  // Clear reference when invoice deleted

            // Soft delete global query filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Invoice entity
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasConversion<int>();

            // Foreign key relationship
            entity.HasOne(i => i.Customer)
                  .WithMany(c => c.Invoices)
                  .HasForeignKey(i => i.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Soft delete global query filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure InvoiceLineItem entity
        modelBuilder.Entity<InvoiceLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Foreign key relationships
            entity.HasOne(ili => ili.Invoice)
                  .WithMany(i => i.LineItems)
                  .HasForeignKey(ili => ili.InvoiceId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(ili => ili.Job)
                  .WithMany()
                  .HasForeignKey(ili => ili.JobId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Property("CreatedDate") != null)
                    entry.Property("CreatedDate").CurrentValue = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                if (entry.Property("UpdatedDate") != null)
                    entry.Property("UpdatedDate").CurrentValue = DateTime.UtcNow;
            }
        }
    }
}