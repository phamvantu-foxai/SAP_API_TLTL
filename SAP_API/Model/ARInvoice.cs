using System.Text.Json.Serialization;

namespace SAP_API.Model
{

    public class ARInvoice
    {
        public string CardCode { get; set; }          // Mã khách hàng
        public string WhsCode { get; set; }
        public DateTime DocDate { get; set; }         // Ngày chứng từ
        public string KyHieuHD { get; set; }          // UDF: Ký hiệu HĐ
        public string MaSoHD { get; set; }          // UDF: Mã số thuế KH
        public string InvoiceCode { get; set; }
        public string? OriginalInvoiceCode { get; set; }
        public string? CardName { get; set; }
        public string? CardNumber { get; set; }

        public List<InvoiceLineDto> ARInvoice_Lines { get; set; } = new();
    }

    public class InvoiceLineDto
    {
        public string ItemCode { get; set; }          // Mã hàng
        public double Quantity { get; set; }          // Tổng số lượng
        public double Price { get; set; }             // Đơn giá
        public double? VatPercent { get; set; }       // % VAT (nếu manual)

        public List<BatchDto> Batches { get; set; } = new();
    }

    public class BatchDto
    {
        public string BatchNumber { get; set; }       // Số lô
        public double Quantity { get; set; }          // Số lượng xuất từ lô này
    }
    public class ARInvoiceCreditLineBatchDto
    {
        public string BatchNumberProperty { get; set; }       // Số lô
        public double Quantity { get; set; }          // Số lượng xuất từ lô này
    }

    public class ARInvoiceLineBatch
    {
        public string BatchNumber { get; set; }
        public double Quantity { get; set; }
    }

    public class ARInvoiceLine
    {
        public string ItemCode { get; set; }
        public double Quantity { get; set; }
        public double UnitPrice { get; set; }
        public string VatGroup { get; set; }
        public string WarehouseCode { get; set; }
        public List<ARInvoiceLineBatch> BatchNumbers { get; set; } = new();
    }
    public class ARInvoiceCreditLine
    {
        public int BaseType { get; set; } = -1;
        public int? BaseEntry { get; set; } = null;
        public int? BaseLine { get; set; } = null;
        public string ItemCode { get; set; }
        public double Quantity { get; set; }
        public double UnitPrice { get; set; }
        public string VatGroup { get; set; }
        public string WarehouseCode { get; set; }
        public List<ARInvoiceLineBatch> BatchNumbers { get; set; } = new();
    }
    public class ARInvoiceCreditLineDTO
    {
        public int? LineNum { get; set; } = null;
        public string ItemCode { get; set; }
        public double Quantity { get; set; }
        public double UnitPrice { get; set; }
        public string VatGroup { get; set; }
        public string WarehouseCode { get; set; }
        public List<ARInvoiceCreditLineBatchDto> BatchNumbers { get; set; } = new();
    }

    public class ARInvoiceRequest
    {
        public string CardCode { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DocDueDate { get; set; }
        public DateTime TaxDate { get; set; }
        public string Comments { get; set; }
        public string U_SoSeries { get; set; }
        public string U_KyHieuHD { get; set; }
        public string U_SoChungTu { get; set; }
        public double U_ThueTTDB { get; set; } = 75.00;
        public string U_LoaiHoaDonBan { get; set; } = "HDBH01";
        public string? U_HoTen { get; set; }
        public string? U_CCCD { get; set; }
        public string U_POS { get; set; }
        public string? U_PBG { get; set; }
        public List<ARInvoiceLine> DocumentLines { get; set; } = new();
    }
    public class ARInvoiceCreditRequest
    {
        public string CardCode { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DocDueDate { get; set; }
        public DateTime TaxDate { get; set; }
        public string Comments { get; set; }
        public string U_SoSeries { get; set; }
        public string U_KyHieuHD { get; set; }
        public string U_SoChungTu { get; set; }
        public double U_ThueTTDB { get; set; } = 75.00;
        public string U_LoaiHoaDonBan { get; set; } = "HDBH01";
        public string? U_HoTen { get; set; }
        public string? U_CCCD { get; set; }
        public string U_POS { get; set; }
        public string? U_PBG { get; set; }
        public List<ARInvoiceCreditLine> DocumentLines { get; set; } = new();
    }
    public class PaymentInvoice
    {
        public string InvoiceType { get; set; } = "it_Invoice";
        public int DocEntry { get; set; }
        public double SumApplied { get; set; }
    }

    public class PaymentRequest
    {
        public string CardCode { get; set; }
        public DateTime DocDate { get; set; }
        public string CashAccount { get; set; }
        public double CashSum { get; set; }
        public List<PaymentInvoice> Invoices { get; set; } = new();
    }
    

    public class ARInvoiceCreditRequestDTO
    {
        public string CardCode { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DocDueDate { get; set; }
        public DateTime TaxDate { get; set; }
        public string Comments { get; set; }
        public string U_SoSeries { get; set; }
        public string U_KyHieuHD { get; set; }
        public string U_SoChungTu { get; set; }
        public double U_ThueTTDB { get; set; } = 75.00;
        public string U_LoaiHoaDonBan { get; set; } = "HDBH01";
        public string? U_HoTen { get; set; }
        public string? U_CCCD { get; set; }
        public string U_POS { get; set; }
        public string? U_PBG { get; set; }
        public List<ARInvoiceCreditLineDTO> DocumentLines { get; set; } = new();
    }
}
