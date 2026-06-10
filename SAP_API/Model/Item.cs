using Microsoft.VisualBasic;
using SAPbobsCOM;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.AccessControl;

namespace SAP_API.Model
{
    public class Item
    {
    }
    public class ItemDTO
    {

        /// <summary>
        /// mã hàng
        /// </summary>
        [MaxLength(50)]
        public required string ItemCode { get; set; }
        /// <summary>
        /// tên hàng
        /// </summary>
        [MaxLength(254)]
        public required string ItemName { get; set; }
        /// <summary>
        /// id nhóm đơn vị tính
        /// </summary>
        public required string UgpEntry { get; set; }
        /// <summary>
        /// nhóm cha
        /// </summary>
        public string? ItemGroupId { get; set; }

        /// <summary>
        /// id nhóm 1
        /// </summary>
        public string? ItemGroupId1 { get; set; }

        /// <summary>
        /// id nhóm 2
        /// </summary>
        public string? ItemGroupId2 { get; set; }
        /// <summary>
        /// trạng thái
        /// </summary>

        [NotMapped]
        public bool IsActive = true;
        /// <summary>
        /// thuế
        /// </summary>
        public decimal? Tax { get; set; }

        /// <summary>
        /// quản lý lô: L - không quản lý :  N - quản lý seria : S
        /// </summary>
        public string? ManBtchNum { get; set; }
        /// <summary>
        /// thuộc tính 1
        /// </summary>
        public string? AttributeOne { get; set; }
        /// <summary>
        /// thuộc tính 2
        /// </summary>
        public string? AttributeTwo { get; set; }
        /// <summary>
        /// thuộc tính 3
        /// </summary>
        public string? AttributeThree { get; set; }
        /// <summary>
        /// thuộc tính 4
        /// </summary>
        public string? AttributeFour { get; set; }

        /// <summary>
        /// thuộc tính 5
        /// </summary>
        public string? AttributeFive { get; set; }
    }
    public class PriceListView
    {
        public Int16 PriceListCode { get; set; }
        public string PriceListName { get; set; }
        public string ItemCode { get; set; }
        public decimal SellingPrice { get; set; }
    }

    public class TransferView
    {
        public Int32 Stt { get; set; }
        public string MS_PT { get; set; }
        public string TeN_PT { get; set; }
        public string MS_LOAI_VT { get; set; }
        public string TeN_LOAI_VT { get; set; }
        public string Dvt { get; set; }
        public string TeN_DVT { get; set; }
    }

    public class GoodIssueView
    {
        public Int32 Stt { get; set; }
        public string MS_DH_XUAT_PT { get; set; }
        public string NgaY_XUAT { get; set; }
        public string MS_PHIEU_BAO_TRI { get; set; }
        public string MS_KHO { get; set; }
        public string TeN_KHO { get; set; }
        public string MS_PT { get; set; }
        public decimal SO_LUONG { get; set; }

        public decimal DoN_GIA { get; set; }
    }
    public class GoodReceiptView
    {
        public Int32 Stt { get; set; }
        public string mS_DH_NHAP_PT { get; set; }
        public string ngaY_NHAP { get; set; }
        public string MS_PHIEU_BAO_TRI { get; set; }
        public string MS_KHO { get; set; }
        public string TeN_KHO { get; set; }
        public string MS_PT { get; set; }
        public decimal SO_LUONG { get; set; }

        public decimal DoN_GIA { get; set; }
    }

    public class ProductPriceListSyncDto
    {
        public string? Creator { get; set; }
        public string? PriceListName { get; set; }
        public List<ProductPriceListLineSyncDto> ProductPriceListLine { get; set; } = new();
    }

    public class ProductPriceListLineSyncDto
    {
        public string? ItemCode { get; set; }
        public decimal SellingPrice { get; set; }

    }
    public class ItemOnhand
    {
        public string ItemCode { get; set; }
        public string Warehouse { get; set; }

        public decimal? OnHand { get; set; }
        public decimal? IsCommited { get; set; }
        public decimal? OnOrder { get; set; }
        public decimal? Available { get; set; }
    }
    public class ItemView
    {
        public string ItemCode { get; set; }
        public ICollection<OITW> OITW { get; set; }
    }
    public class OITW
    {
        public string Warehouse { get; set; }
        public decimal? OnHand { get; set; }
        public decimal? IsCommited { get; set; }
        public decimal? OnOrder { get; set; }
        public decimal? Available { get; set; }
    }

    public class ItemInfo
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public Int16 ItmsGrpCod { get; set; }
        public string validFor { get; set; }
        public string? UserText { get; set; }
        public decimal? PriceBeforeVAT { get; set; }
        public decimal? PriceAfterVAT { get; set; }
        public decimal? OriginPrice { get; set; }
    }
    public class OrderView
    {
        public DateTime DocDate { get; set; }
        public Int32 DocEntry { get; set; }
        public string DocStatus { get; set; }
        public string InvntSttus { get; set; }
        public string InvntSttusI { get; set; }
        public string? U_SoHoaDon { get; set; }
        public DateTime? U_NgayHoaDon { get; set; }
        public Int32 DocNum { get; set; }
        public string CANCELED { get; set; }
        public Int32 Basetype { get; set; }
        public string ItemCode { get; set; }
        public decimal Quantity { get; set; }
    }
    public class Order
    {
        public DateTime DocDate { get; set; }
        public Int32 DocEntry { get; set; }
        public string DocStatus { get; set; }
        public string InvntSttus { get; set; }
        public string InvntSttusI { get; set; }
        public string? U_SoHoaDon { get; set; }
        public DateTime? U_NgayHoaDon { get; set; }
        public Int32 DocNum { get; set; }
        public string CANCELED { get; set; }
        public ICollection<Order_Line>? Order_Lines { get; set; }
    }
    public class Order_Line
    {
        public Int32 Basetype { get; set; }
        public string ItemCode { get; set; }
        public decimal Quantity { get; set; }
    }
    public class Documents
    {
        public string CardCode { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public ICollection<Documents_Lines>? Documents_Lines { get; set; }
    }
    public class Documents_Lines
    {
        public string ItemCode{ get; set;}
        public double Quantity{ get; set; }
        public double UnitPrice { get; set; }
        public double Tax { get; set; }
    }
}