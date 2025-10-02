namespace SAP_API.Model
{
    public class Message
    {
        public int Status { get; set; }
        public string Error { get; set; }
    }
    public class Request
    {
        public string? DocNumber {  get; set; }
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
        public string WarehouseSapCode {  get; set; }
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
    public class Batch
    {
        public DateTime? expDate { get; set; }
        public DateTime? mnfDate { get; set; }
        public string BatchNumber { get; set; }
        public double Quantity { get; set; }
    }
}
