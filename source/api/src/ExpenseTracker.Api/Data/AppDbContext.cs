using ExpenseTracker.Audit.Entities;
using ExpenseTracker.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using BudgetEntity = ExpenseTracker.Budget.Entities.Budget;
using BudgetHistoryEntity = ExpenseTracker.Budget.Entities.BudgetHistory;
using ExpenseAttachmentEntity = ExpenseTracker.Ocr.Entities.ExpenseAttachment;
using ExpenseEntity = ExpenseTracker.Ocr.Entities.Expense;
using ExpenseItemEntity = ExpenseTracker.Ocr.Entities.ExpenseItem;
using ExpenseShareEntity = ExpenseTracker.Ocr.Entities.ExpenseShare;
using NotificationEntity = ExpenseTracker.Budget.Entities.Notification;
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
    public DbSet<ExpenseShareEntity> ExpenseShares => Set<ExpenseShareEntity>();
    public DbSet<ExpenseAttachmentEntity> ExpenseAttachments => Set<ExpenseAttachmentEntity>();
    public DbSet<BudgetEntity> Budgets => Set<BudgetEntity>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    public DbSet<BudgetHistoryEntity> BudgetHistories => Set<BudgetHistoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
        });

        modelBuilder.Entity<InviteToken>(e =>
        {
            e.ToTable("invite_tokens");
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Token).IsUnique();
            e.Property(t => t.AssignedRole).HasConversion<string>();
        });

        modelBuilder.Entity<ReceiptEntity>(e =>
        {
            e.ToTable("receipts");
            e.HasKey(r => r.Id);
            e.Property(r => r.Status).HasConversion<string>();
            e.Property(r => r.ImageQuality).HasMaxLength(8);
            // ExpenseId links a receipt to an expense for multi-receipt support.
            // Stored as a plain column; no FK constraint to avoid circular reference with expenses.ReceiptId.
            e.Property(r => r.ExpenseId);
            e.HasIndex(r => r.ExpenseId);
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
            e.Property(x => x.BarcodeType).HasMaxLength(32);
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
            // Analytics queries filter and group on date, category, and merchant_name.
            e.HasIndex(x => x.Date);
            e.HasIndex(x => x.Category);
            e.HasIndex(x => x.MerchantName);
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

        modelBuilder.Entity<ExpenseShareEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.ToTable("expense_shares");
            e.Property(s => s.Amount).HasColumnType("numeric(18,4)");
            e.Property(s => s.Percentage).HasColumnType("numeric(5,2)");
            e.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne<ExpenseEntity>()
             .WithMany(x => x.Shares)
             .HasForeignKey(s => s.ExpenseId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => s.ExpenseId);
            e.HasIndex(s => s.UserId);
        });

        modelBuilder.Entity<ExpenseAttachmentEntity>(e =>
        {
            e.HasKey(a => a.Id);
            e.ToTable("expense_attachments");
            e.Property(a => a.FileName).HasMaxLength(255).IsRequired();
            e.Property(a => a.StoragePath).HasMaxLength(1024).IsRequired();
            e.Property(a => a.ContentType).HasMaxLength(128).IsRequired();
            e.Property(a => a.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne<ExpenseEntity>()
             .WithMany()
             .HasForeignKey(a => a.ExpenseId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => a.ExpenseId);
        });

        modelBuilder.Entity<BudgetEntity>(e =>
        {
            e.HasKey(b => b.Id);
            e.ToTable("budgets");
            e.Property(b => b.Category).HasMaxLength(64).IsRequired();
            e.Property(b => b.MonthlyLimit).HasColumnType("numeric(18,4)").IsRequired();
            e.Property(b => b.Type).HasMaxLength(16).IsRequired().HasDefaultValue("category");
            e.Property(b => b.CreatedAt).HasDefaultValueSql("now()");
            // Unique constraint: one budget per (user, category) — household budgets use category="household"
            e.HasIndex(b => new { b.UserId, b.Category }).IsUnique();
        });

        modelBuilder.Entity<NotificationEntity>(e =>
        {
            e.HasKey(n => n.Id);
            e.ToTable("notifications");
            e.Property(n => n.Type).HasMaxLength(32).IsRequired();
            e.Property(n => n.Message).HasMaxLength(512).IsRequired();
            e.Property(n => n.CreatedAt).HasDefaultValueSql("now()");
            e.HasIndex(n => new { n.UserId, n.CreatedAt });
            e.HasIndex(n => n.BudgetId);
        });

        modelBuilder.Entity<BudgetHistoryEntity>(e =>
        {
            e.HasKey(h => h.Id);
            e.ToTable("budget_history");
            e.Property(h => h.Limit).HasColumnType("numeric(18,4)").IsRequired();
            e.Property(h => h.Spent).HasColumnType("numeric(18,4)").IsRequired();
            e.Property(h => h.CreatedAt).HasDefaultValueSql("now()");
            // Prevent duplicate snapshots for the same budget+month.
            e.HasIndex(h => new { h.BudgetId, h.Month }).IsUnique();
        });
    }
}
