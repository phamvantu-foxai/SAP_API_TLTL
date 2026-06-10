using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace SAP_API.Model
{
    public class DocEntryResult
    {
        public int DocEntry { get; set; }
    }
    public class Message
    {
        public int Status { get; set; }
        public string Error { get; set; }
    }
    public class Request
    {
        public string? DocNumber { get; set; }
        public string Store { get; set; }
        public string? DocnumberPOS { get; set; }
    }
    public class Respond
    {
        public string InvoicePos { get; set; }
        public bool Success { get; set; }
        public string? DocEntry { get; set; }
        public string? Error { get; set; }
    }
    public class OWTR
    {
        public string WarehouseSapCode { get; set; }
        public DateTime? DocDate { get; set; }
        public string Note { get; set; }
        public ICollection<WTR1> ItemDetail { get; set; }
        public string DocEntry { get; set; }
    }
    public class WTR1
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public double Quantity { get; set; }
        public ICollection<Batch> Batch { get; set; }
    }
    public class OWTRView
    {
        public string FromWhsCodSapCode { get; set; }
        public DateTime? DocDate { get; set; }
        public string warehouseSapCode { get; set; }
        public string note { get; set; }
        public string itemCode { get; set; }
        public string itemName { get; set; }
        public decimal quantity { get; set; }
        public string BatchNum { get; set; }
        public decimal QtyBatch { get; set; }
        public DateTime? ExpDate { get; set; }
        public DateTime? MnfDate { get; set; }
        public Int32 DocEntry { get; set; }
        public string? Docnumber { get; set; }
        public string? U_POS { get; set; }
    }
    public class Batch
    {
        public DateTime? expDate { get; set; }
        public DateTime? mnfDate { get; set; }
        public string BatchNumber { get; set; }
        public double Quantity { get; set; }
    }
    public class OCRDView
    {
        public string CardCode { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public Int16? GroupCode { get; set; }
        public string? GroupName { get; set; }
        public string? LicTradNum { get; set; }
        public string? AliasName { get; set; }
        public string? Phone1 { get; set; }
        public string? E_Mail { get; set; }
        public string? U_NhomKH1 { get; set; }
        public string? NhomKH1Name { get; set; }
    }
}
