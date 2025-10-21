using Gridify;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using SAP_API.Data;
using SAP_API.Model;
using SAPbobsCOM;
using System.Linq;
using System.Net.Http;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Documents = SAP_API.Model.Documents;
using Message = SAP_API.Model.Message;

namespace SAP_API.Service
{
    public class OrderService
    {
        private readonly AppDbContext _db;
        private readonly SapDiApiHelper _sapHelper;
        private readonly SAPConnection _sapConnection;
        public OrderService(SapDiApiHelper sapHelper, AppDbContext db, SAPConnection sapConnection)
        {
            _sapHelper = sapHelper;
            _db = db;
            _sapConnection = sapConnection;
        }
        public async Task<(Message, List<Order>, int)> GetOrderClosedAsync(GridifyQuery q, DateTime? fromDate, DateTime? toDate, List<string> docId)
        {
            Message message = new Message();
            try
            {
                var baseQuery = _db.OrderView.AsQueryable();
                if (fromDate != null)
                    baseQuery = baseQuery.Where(e => e.DocDate.Date >= fromDate.Value.Date);
                if (toDate != null)
                    baseQuery = baseQuery.Where(e => e.DocDate.Date <= toDate.Value.Date);
                if (docId != null && docId.Any())
                {
                    baseQuery = baseQuery.Where(e => docId.Contains(e.DocEntry.ToString()));
                }
                var filtered = baseQuery.ApplyFiltering(q.Filter);
                var ordered = filtered.ApplyOrdering(q.OrderBy);
                var totalCount = await filtered.Select(e=>e.DocEntry).Distinct().CountAsync(); // tổng bản ghi trước phân trang

                // nếu page nhỏ hơn 1 thì set = 1
                var page = q.Page < 1 ? 1 : q.Page;
                var pageSize = q.PageSize < 1 ? 20 : q.PageSize;

                var paged = ordered
                    //.ApplyPaging(page, pageSize)
                    .ToList();
                var order = paged
                    .GroupBy(o => new
                    {
                        o.DocEntry,
                        o.DocDate,
                        o.DocStatus,
                        o.InvntSttus,
                        o.InvntSttusI,
                        o.U_SoHoaDon,
                        o.U_NgayHoaDon,
                        o.DocNum,
                        o.CANCELED
                    })
                    .Select(g => new Order
                    {
                        DocEntry = g.Key.DocEntry,
                        DocDate = g.Key.DocDate,
                        DocStatus = g.Key.DocStatus,
                        InvntSttus = g.Key.InvntSttus,
                        InvntSttusI = g.Key.InvntSttusI,
                        U_SoHoaDon = g.Key.U_SoHoaDon,
                        U_NgayHoaDon = g.Key.U_NgayHoaDon,
                        DocNum = g.Key.DocNum,
                        CANCELED = g.Key.CANCELED,

                        // gộp Order_Lines
                        Order_Lines = g
                            .GroupBy(x => new { x.Basetype, x.ItemCode })
                            .Select(x => new Order_Line
                            {
                                Basetype = x.Key.Basetype,
                                ItemCode = x.Key.ItemCode,
                                Quantity = x.Sum(v => v.Quantity)
                            })
                            .ToList()
                    })
                    .OrderByDescending(e => e.DocDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                return (null, order, totalCount);
            }
            catch (Exception ex)
            {
                message.Status = 400;
                message.Error = ex.Message;
                return (message, null, 0);
            }

        }

        public async Task<(Message, List<Agreement>, int)> GetAgreementAsync(GridifyQuery q)
        {
            Message message = new Message();
            try
            {
                var baseQuery = _db.AgreementDetails.AsQueryable();
                var filtered = baseQuery.ApplyFiltering(q.Filter);
                var ordered = filtered.ApplyOrdering(q.OrderBy);
                var totalCount = await filtered.Select(e => e.AbsID).Distinct().CountAsync(); // tổng bản ghi trước phân trang

                // nếu page nhỏ hơn 1 thì set = 1
                var page = q.Page < 1 ? 1 : q.Page;
                var pageSize = q.PageSize < 1 ? 20 : q.PageSize;

                var paged = ordered
                //.ApplyPaging(page, pageSize)
                    .ToList();
                var agreements = paged
                .GroupBy(ad => new
                {
                    ad.AbsID,
                    ad.BpCode,
                    ad.BpName,
                    ad.StartDate,
                    ad.EndDate,
                    ad.TermDate,
                    ad.Descript,
                    ad.Type,
                    ad.Status,
                    ad.UpdtDate,
                    ad.CreateDate,
                    ad.Cancelled,
                    ad.SignDate
                })
                .Select(group => new Agreement
                {
                    AbsID = group.Key.AbsID,
                    BpCode = group.Key.BpCode,
                    BpName = group.Key.BpName,
                    StartDate = group.Key.StartDate,
                    EndDate = group.Key.EndDate,
                    TermDate = group.Key.TermDate,
                    Descript = group.Key.Descript,
                    Type = group.Key.Type,
                    Status = group.Key.Status,
                    UpdtDate = group.Key.UpdtDate,
                    CreateDate = group.Key.CreateDate,
                    Cancelled = group.Key.Cancelled,
                    SignDate = group.Key.SignDate,
                    Agrement_Lines = group.Select(line => new Agreement_Line
                    {
                        AgrLineNum = line.AgrLineNum,
                        ItemCode = line.ItemCode,
                        ItemName = line.ItemName,
                        ItemGroup = line.ItemGroup,
                        PlanQty = line.PlanQty,
                        InvntryUom = line.InvntryUom
                    }).ToList()
                }).OrderByDescending(e => e.StartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (null, agreements, totalCount);
            }
            catch (Exception ex)
            {
                message.Status = 400;
                message.Error = ex.Message;
                return (message, null, 0);
            }

        }

        public async Task<(Message, int)> CreateOrder(Documents doc)
        {
            Message mes= new Message();
            try
            {
                if (!_sapConnection.Connect())
                {
                    mes.Error = "Kết nối đến SAP Business One thất bại";
                    mes.Status = 400;
                    return (mes, 0);
                }
                var oCompany = _sapConnection.Company;
                SAPbobsCOM.Documents salesOrder = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(BoObjectTypes.oOrders);
                salesOrder.CardCode = doc.CardCode;
                salesOrder.DocDate = doc.DocDate;
                salesOrder.DocDueDate = doc.DeliveryDate;
                foreach (var line in doc.Documents_Lines)
                {
                    salesOrder.Lines.ItemCode = line.ItemCode;
                    salesOrder.Lines.Quantity = line.Quantity;
                    salesOrder.Lines.Price = line.UnitPrice;
                    salesOrder.Lines.VatGroup = int.Parse(line.Tax.ToString()).ToString();

                    salesOrder.Lines.Add();
                }
                int result = salesOrder.Add();

                if (result != 0)
                {
                    // Get the error message from SAP
                    oCompany.GetLastError(out int errorCode, out string errorMessage);
                    mes.Error = errorMessage;
                    mes.Status = errorCode;
                    return (mes, 0);
                }

                string docEntry = oCompany.GetNewObjectKey();
                return (null, int.Parse(docEntry));
            } catch (Exception ex) {
                mes.Error = ex.Message;
                mes.Status = 400;
                return (mes, 0);
            }
        }
    }
}
