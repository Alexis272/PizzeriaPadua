using PizzeriaPadua.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace PizzeriaPadua.API.Data
{
    public class PizzeriaPaduaContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<StoreSettings> StoreSettings { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Review> Reviews { get; set; }

        // Entidades del Módulo de Ventas / POS Padua
        public DbSet<Branch> Branches { get; set; }
        public DbSet<CashRegister> CashRegisters { get; set; }
        public DbSet<ShiftConfig> ShiftConfigs { get; set; }
        public DbSet<CashSession> CashSessions { get; set; }
        public DbSet<CashMovement> CashMovements { get; set; }
        public DbSet<CashCut> CashCuts { get; set; }
        public DbSet<SalePayment> SalePayments { get; set; }
        public DbSet<SaleRefund> SaleRefunds { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<SalesGoal> SalesGoals { get; set; }
        public DbSet<CatalogReason> CatalogReasons { get; set; }
        public DbSet<PaymentMethodConfig> PaymentMethodConfigs { get; set; }
        public DbSet<SalesChannelConfig> SalesChannelConfigs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public PizzeriaPaduaContext(DbContextOptions<PizzeriaPaduaContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Product>().HasIndex(p => p.Name).IsUnique();
            builder.Entity<Category>().HasIndex(c => c.Name).IsUnique();
            builder.Entity<Table>().HasIndex(t => t.TableNumber).IsUnique();
            builder.Entity<Employee>().HasIndex(e => e.Name).IsUnique();
            builder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            builder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            builder.Entity<User>().Property(u => u.PhoneNumber).HasDefaultValue("");

            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .HasOne(o => o.Table)
                .WithMany(t => t.Orders)
                .HasForeignKey(o => o.TableId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Order>()
                .HasOne(o => o.Employee)
                .WithMany(e => e.Orders)
                .HasForeignKey(o => o.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<SalePayment>()
                .HasOne(sp => sp.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(sp => sp.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SaleRefund>()
                .HasOne(sr => sr.Order)
                .WithMany(o => o.Refunds)
                .HasForeignKey(sr => sr.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
