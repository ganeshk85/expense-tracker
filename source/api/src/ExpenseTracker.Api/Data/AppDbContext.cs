using ExpenseTracker.Audit.Entities;
using ExpenseTracker.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using ExpenseEntity = ExpenseTracker.Ocr.Entities.Expense;
using ExpenseItemEntity = ExpenseTracker.Ocr.Entities.ExpenseItem;
using ReceiptEntity = ExpenseTracker.Receipt.Entities.Receipt;

namespace ExpenseTracker.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<InviteToken> InviteTokens => Set<InviteToken>();
    public DbSet<ReceiptEntity> Receipts => Set<ReceiptEntity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();
    public DbSet<ExpenseItemEntity> ExpenseItems => Set<ExpenseItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
        });

        modelBuilder.Entity<InviteToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Token).IsUnique();
            e.Property(t => t.AssignedRole).HasConversion<string>();
        });

        modelBuilder.Entity<ReceiptEntity>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Status).HasConversion<string>();
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.ToTable("audit_logs");
            e.Property(a => a.Action).HasMaxLength(64).IsRequired();
            e.Property(a => a.ResourceType).HasMaxLength(64);
            e.Property(a => a.IpAddress).HasMaxLength(45).IsRequired();
            e.Property(a => a.BeforeJson).HasColumnType("jsonb");
            e.Property(a => a.AfterJson).HasColumnType("jsonb");
            // CreatedAt defaults to now() at the DB level; EF should not set it on insert.
            e.Property(a => a.CreatedAt).HasDefaultValueSql("now()");
            e.HasIndex(a => a.Action);
            e.HasIndex(a => new { a.UserId, a.CreatedAt });
        });

        modelBuilder.Entity<ExpenseEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToTable("expenses");
            e.Property(x => x.OcrStatus).HasMaxLength(32).IsRequired();
            e.Property(x => x.Source).HasMaxLength(16).IsRequired();
            e.Property(x => x.Subtotal).HasColumnType("numeric(18,4)");
            e.Property(x => x.TaxAmount).HasColumnType("numeric(18,4)");
            e.Property(x => x.Total).HasColumnType("numeric(18,4)");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.Category).HasMaxLength(64);
            e.Property(x => x.Tags).HasColumnType("text[]");
            e.Property(x => x.ConfidenceJson).HasColumnType("jsonb");
            // ReceiptId is nullable — manual expenses have no associated receipt.
            e.HasOne<ReceiptEntity>()
             .WithMany()
             .HasForeignKey(x => x.ReceiptId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);
            // Partial unique index: allow multiple NULL ReceiptIds (manual expenses).
            e.HasIndex(x => x.ReceiptId).IsUnique()
             .HasFilter("\"ReceiptId\" IS NOT NULL");
            e.HasIndex(x => x.UserId);
            e.HasMany(x => x.Items)
             .WithOne()
             .HasForeignKey(i => i.ExpenseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExpenseItemEntity>(e =>
        {
            e.HasKey(i => i.Id);
            e.ToTable("expense_items");
            e.Property(i => i.Quantity).HasColumnType("numeric(18,4)");
            e.Property(i => i.UnitPrice).HasColumnType("numeric(18,4)");
            e.Property(i => i.CreatedAt).HasDefaultValueSql("now()");
        });
    }
}
