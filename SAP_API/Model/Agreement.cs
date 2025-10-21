namespace SAP_API.Model
{
    public class Agreement
    {
        public Int32 AbsID { get; set; }
        public string BpCode { get; set; }
        public string BpName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? TermDate { get; set; }
        public string? Descript { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public DateTime? UpdtDate { get; set; }
        public DateTime CreateDate { get; set; }
        public string Cancelled { get; set; }
        public DateTime? SignDate { get; set; }
        public List<Agreement_Line> Agrement_Lines { get; set; } = new List<Agreement_Line>();
    }
    public class Agreement_Line
    {
        public Int32 AgrLineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public Int16 ItemGroup { get; set; }
        public decimal PlanQty { get; set; }
        public string InvntryUom { get; set; }
    }
    public class AgreementDetails
    {
        public Int32 AbsID { get; set; }
        public string BpCode { get; set; }
        public string BpName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? TermDate { get; set; }
        public string? Descript { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public DateTime? UpdtDate { get; set; }
        public DateTime CreateDate { get; set; }
        public string Cancelled { get; set; }
        public DateTime? SignDate { get; set; }
        public Int32 AgrLineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public Int16 ItemGroup { get; set; }
        public decimal PlanQty { get; set; }
        public string InvntryUom { get; set; }
    }
}
