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
        public DbSet<OWTRView> OWTRView { get; set; }
        public DbSet<OCRDView> OCRDView { get; set; }
        public DbSet<ItemOnhand> ItemOnhand { get; set; }
        public DbSet<ItemOnhandEco> ItemOnhandEco { get; set; }
        public DbSet<ItemInfo> ItemInfo { get; set; }
        public DbSet<OrderView> OrderView { get; set; }
        public DbSet<AgreementDetails> AgreementDetails { get; set; }
        public DbSet<TransferView> TransferView { get; set; }
        public DbSet<GoodIssueView> GoodIssueView { get; set; }
        public DbSet<GoodReceiptView> GoodReceiptView { get; set; }
        public DbSet<DocEntryResult> DocEntryResults { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ItemDTO>()
            .HasNoKey()
            .ToView("vw_ItemMaterData");

            modelBuilder.Entity<DocEntryResult>().HasNoKey();
            modelBuilder.Entity<OWTRView>().HasNoKey().ToView("uv_OWTR");
            modelBuilder.Entity<OCRDView>().HasNoKey().ToView("uv_OCRD");
            modelBuilder.Entity<ItemOnhand>().HasNoKey().ToView("uv_OITW");
            modelBuilder.Entity<ItemOnhandEco>().HasNoKey().ToView("uv_Ecomain_Item");
            modelBuilder.Entity<ItemInfo>().HasNoKey().ToView("uv_OITM");
            modelBuilder.Entity<OrderView>().HasNoKey().ToView("uv_Order_Closed");
            modelBuilder.Entity<AgreementDetails>().HasNoKey().ToView("uv_BlanketAgrement");
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
