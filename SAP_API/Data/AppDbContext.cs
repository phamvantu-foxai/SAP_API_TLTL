using Microsoft.EntityFrameworkCore;
using SAP_API.Model;

namespace SAP_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<ItemDTO> ItemView { get; set; }
        public DbSet<PriceListView> PriceListView { get; set; }
        public DbSet<TransferView> TransferView { get; set; }
        public DbSet<GoodIssueView> GoodIssueView { get; set; }
        public DbSet<GoodReceiptView> GoodReceiptView { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ItemDTO>()
            .HasNoKey()
            .ToView("vw_ItemMaterData");

            modelBuilder.Entity<PriceListView>()
            .HasNoKey()
            .ToView("vw_PriceList");
            modelBuilder.Entity<TransferView>()
            .HasNoKey()
            .ToView("vw_Transfer");
            modelBuilder.Entity<GoodIssueView>()
            .HasNoKey()
            .ToView("vw_GoodIssue");
            modelBuilder.Entity<GoodReceiptView>()
            .HasNoKey()
            .ToView("vw_GoodReceipt");
        }
    }
}
