using System;
using System.IO;
using LabStock.Models;
using Microsoft.EntityFrameworkCore;

namespace LabStock.Data
{
    public class AppDbContext : DbContext
    {
        public static string DatabasePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "labstock.db");

        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Location> Locations => Set<Location>();
        public DbSet<Material> Materials => Set<Material>();
        public DbSet<Lot> Lots => Set<Lot>();
        public DbSet<Analysis> Analyses => Set<Analysis>();
        public DbSet<AnalysisMaterial> AnalysisMaterials => Set<AnalysisMaterial>();
        public DbSet<AnalysisRun> AnalysisRuns => Set<AnalysisRun>();
        public DbSet<Receipt> Receipts => Set<Receipt>();
        public DbSet<ReceiptItem> ReceiptItems => Set<ReceiptItem>();
        public DbSet<Writeoff> Writeoffs => Set<Writeoff>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();
        public DbSet<InventoryCheck> InventoryChecks => Set<InventoryCheck>();
        public DbSet<InventoryCheckItem> InventoryCheckItems => Set<InventoryCheckItem>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Setting> Settings => Set<Setting>();

        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // AnalysisMaterial composite or relations
            modelBuilder.Entity<AnalysisMaterial>()
                .HasOne(am => am.Analysis)
                .WithMany(a => a.Materials)
                .HasForeignKey(am => am.AnalysisId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnalysisMaterial>()
                .HasOne(am => am.Material)
                .WithMany(m => m.AnalysisMaterials)
                .HasForeignKey(am => am.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            // Receipt & ReceiptItem
            modelBuilder.Entity<ReceiptItem>()
                .HasOne(ri => ri.Receipt)
                .WithMany(r => r.Items)
                .HasForeignKey(ri => ri.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            // InventoryCheck & InventoryCheckItem
            modelBuilder.Entity<InventoryCheckItem>()
                .HasOne(ici => ici.InventoryCheck)
                .WithMany(ic => ic.Items)
                .HasForeignKey(ici => ici.InventoryCheckId)
                .OnDelete(DeleteBehavior.Cascade);

            // StockMovement relations
            modelBuilder.Entity<StockMovement>()
                .HasOne(sm => sm.Material)
                .WithMany(m => m.StockMovements)
                .HasForeignKey(sm => sm.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            // Decimal precision for SQLite
            modelBuilder.Entity<Material>().Property(p => p.MinQuantity).HasConversion<double>();
            modelBuilder.Entity<Material>().Property(p => p.MaxQuantity).HasConversion<double>();
            modelBuilder.Entity<Lot>().Property(p => p.InitialQuantity).HasConversion<double>();
            modelBuilder.Entity<Lot>().Property(p => p.CurrentQuantity).HasConversion<double>();
            modelBuilder.Entity<Lot>().Property(p => p.PurchasePrice).HasConversion<double>();
            modelBuilder.Entity<Analysis>().Property(p => p.Price).HasConversion<double>();
            modelBuilder.Entity<AnalysisMaterial>().Property(p => p.RequiredQuantity).HasConversion<double>();
            modelBuilder.Entity<Receipt>().Property(p => p.TotalAmount).HasConversion<double>();
            modelBuilder.Entity<ReceiptItem>().Property(p => p.Quantity).HasConversion<double>();
            modelBuilder.Entity<ReceiptItem>().Property(p => p.Price).HasConversion<double>();
            modelBuilder.Entity<Writeoff>().Property(p => p.Quantity).HasConversion<double>();
            modelBuilder.Entity<StockMovement>().Property(p => p.Quantity).HasConversion<double>();
            modelBuilder.Entity<StockMovement>().Property(p => p.BalanceAfter).HasConversion<double>();
            modelBuilder.Entity<InventoryCheckItem>().Property(p => p.SystemQuantity).HasConversion<double>();
            modelBuilder.Entity<InventoryCheckItem>().Property(p => p.ActualQuantity).HasConversion<double>();
            modelBuilder.Entity<InventoryCheckItem>().Property(p => p.Difference).HasConversion<double>();
        }
    }
}
