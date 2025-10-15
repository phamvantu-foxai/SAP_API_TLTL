using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAP_API.Data;
using SAP_API.Model;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Message = SAP_API.Model.Message;

namespace SAP_API.Service
{
    public class SapInvoiceService
    {
        private readonly AppDbContext _db;
        private readonly SAPConnection _sapConnection;
        private readonly SapDiApiHelper _sapHelper;
        private readonly HttpClient _httpClient;
        private readonly SapSessionManager _sessionManager;
        private readonly APISetting _api;
        private readonly Cookies cookies = new Cookies();
        public SapInvoiceService(SAPConnection sapConnection,SapDiApiHelper sapHelper, IOptions<APISetting> api, SapSessionManager sessionManager, AppDbContext db)
        {
            _sapHelper = sapHelper;
            _httpClient = new HttpClient();
            _api = api.Value;
            _sessionManager = sessionManager;
            _db = db;
            _sapConnection = sapConnection;
        }
        /// <summary>
        /// Tạo hóa đơn bán hàng và phiếu thu tiền mặt.
        /// </summary>
        /// 
        public async Task<Respond> CreateCancelInvoiceWithPaymentDIAPIAsync(CancelARInvoice cancel)
        {
            Respond respond = new Respond();
            var docEntries = await _db.Set<DocEntryResult>()
            .FromSqlRaw("SELECT DocEntry FROM OINV WHERE U_POS = {0}", cancel.InvoiceCode)
            .Select(x => x.DocEntry).FirstOrDefaultAsync();
            if (docEntries > 0)
                return new Respond
                {
                    InvoicePos = cancel.InvoiceCode,
                    Success = false,
                    DocEntry = docEntries.ToString(),
                    Error = $"Hóa đơn hủy với U_POS {cancel.InvoiceCode} đã được đồng bộ trước đó."
                };

            var result = new Respond();

            try
            {
                var (DocEntry, check2) = await CreateCancelInvoiceDIAPIAsync(cancel.InvoiceCode, cancel.OriginalInvoiceCode);
                if (check2)
                {
                    result.DocEntry = DocEntry;
                    result.Success = true;
                    result.InvoicePos = cancel.InvoiceCode;
                }

                else
                {
                    result.Success = false;
                    result.Error = DocEntry;
                    result.InvoicePos = cancel.InvoiceCode;
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
        }
        public async Task<Respond> CreateInvoiceWithPaymentDIAPIAsync(ARInvoice ar)
        {
            Respond respond = new Respond();
            var docEntries = await _db.Set<DocEntryResult>()
            .FromSqlRaw("SELECT DocEntry FROM OINV WHERE U_POS = {0}", ar.InvoiceCode)
            .Select(x => x.DocEntry).FirstOrDefaultAsync();
            if (docEntries > 0)
                return new Respond
                {
                    InvoicePos = ar.InvoiceCode,
                    Success = false,
                    DocEntry = docEntries.ToString(),
                    Error = $"Hóa đơn với U_POS {ar.InvoiceCode} đã được đồng bộ trước đó."
                };

            var result = new Respond();

            try
            {
                var (DocEntry, check2) = await CreateInvoiceDIAPIAsync(ar);
                if (check2)
                {
                    result.DocEntry = DocEntry;
                    result.Success = true;
                    result.InvoicePos = ar.InvoiceCode;
                }

                else
                {
                    result.Success = false;
                    result.Error = DocEntry;
                    result.InvoicePos = ar.InvoiceCode;
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
        }
        public async Task<Respond> CreateCreditInvoiceDIAPIWithPaymentAsync(ARInvoice ar)
        {
            Respond respond = new Respond();
            var docEntries = await _db.Set<DocEntryResult>()
            .FromSqlRaw("SELECT DocEntry FROM ORIN WHERE U_POS = {0}", ar.InvoiceCode)
            .Select(x => x.DocEntry).FirstOrDefaultAsync();
            if (docEntries > 0)
                return new Respond
                {
                    InvoicePos = ar.InvoiceCode,
                    Success = false,
                    DocEntry = docEntries.ToString(),
                    Error = $"Hóa đơn điều chỉnh với U_POS {ar.InvoiceCode} đã được đồng bộ trước đó."
                };

            var result = new Respond();

            try
            {
                var (DocEntry, check2) = await CreateAdjustmentDIAPIInvoice(ar);
                if (check2)
                {
                    result.DocEntry = DocEntry;
                    result.Success = true;
                    result.InvoicePos = ar.InvoiceCode;
                }

                else
                {
                    result.Success = false;
                    result.Error = DocEntry;
                    result.InvoicePos = ar.InvoiceCode;
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
        }
        public async Task<(string, bool)> CreateAdjustmentDIAPIInvoice(ARInvoice rq)
        {
            if (!_sapConnection.Connect())
                return ("Kết nối đến SAP Business One thất bại", false);

            var oCompany = _sapConnection.Company;
            var docEntries =  _db.Set<DocEntryResult>()
            .FromSqlRaw("SELECT DocEntry FROM OINV WHERE U_POS = {0}", rq.OriginalInvoiceCode)
            .Select(x => x.DocEntry).FirstOrDefault();
            if (docEntries == null)
                return ("Không tìm thấy hóa số đơn để điều chỉnh", false);
            // 🔹 Lấy hóa đơn gốc
            SAPbobsCOM.Documents oBaseInvoice =
                (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInvoices);

            if (!oBaseInvoice.GetByKey(Convert.ToInt32(docEntries)))
                return ($"Không tìm thấy hóa số đơn để điều chỉnh (OriginalInvoiceCode: {rq.OriginalInvoiceCode})", false);

            // 🔹 Tạo Credit Memo
            SAPbobsCOM.Documents oCredit =
                (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oCreditNotes);

            oCredit.CardCode = rq.CardCode;
            oCredit.DocDate = rq.DocDate;
            oCredit.TaxDate = rq.DocDate;
            oCredit.DocDueDate = rq.DocDate;
            oCredit.NumAtCard = rq.InvoiceCode;
            oCredit.Comments = "Tạo hóa đơn bán hàng điều chỉnh tại POS";
            if (!string.IsNullOrEmpty(rq.MaSoHD)) oCredit.UserFields.Fields.Item("U_SoSeries").Value = rq.MaSoHD;
            if (!string.IsNullOrEmpty(rq.KyHieuHD)) oCredit.UserFields.Fields.Item("U_KyHieuHD").Value = rq.KyHieuHD;
            if (!string.IsNullOrEmpty(rq.InvoiceCode)) oCredit.UserFields.Fields.Item("U_SoChungTu").Value = "POS" + rq.InvoiceCode;
            oCredit.UserFields.Fields.Item("U_LoaiHoaDonBan").Value = "HDBH01";
            oCredit.UserFields.Fields.Item("U_ThueTTDB").Value = 75.00;
            if (!string.IsNullOrEmpty(rq.InvoiceCode)) oCredit.UserFields.Fields.Item("U_POS").Value = rq.InvoiceCode;
            if (!string.IsNullOrEmpty(rq.OriginalInvoiceCode)) oCredit.UserFields.Fields.Item("U_PBG").Value = rq.OriginalInvoiceCode;
            if (!string.IsNullOrEmpty(rq.CardNumber)) oCredit.UserFields.Fields.Item("U_CCCD").Value = rq.CardNumber;
            if (!string.IsNullOrEmpty(rq.CardName)) oCredit.UserFields.Fields.Item("U_HoTen").Value = rq.CardName;
            // 🔹 Thêm dòng điều chỉnh (tham chiếu tới hóa đơn gốc)
            for (int i = 0; i < rq.ARInvoice_Lines.Count; i++)
            {
                var line = rq.ARInvoice_Lines[i];
                if (i > 0) oCredit.Lines.Add();

                oCredit.Lines.BaseEntry = docEntries; 
                oCredit.Lines.BaseType = (int)SAPbobsCOM.BoObjectTypes.oInvoices;

                int baseLine = FindBaseLine(oCompany, docEntries, line);

                if (baseLine < 0)
                    return ($"Không tìm thấy dòng gốc cho ItemCode {line.ItemCode}", false);
                oCredit.Lines.BaseLine = baseLine;
                oCredit.Lines.ItemCode = line.ItemCode;
                oCredit.Lines.Quantity = line.Quantity;
                oCredit.Lines.UnitPrice = line.Price;
                oCredit.Lines.WarehouseCode = rq.WhsCode;
                oCredit.Lines.VatGroup = GetVatGroup(line.VatPercent ?? 10);
                if (line.Batches != null && line.Batches.Any())
                {
                    foreach (var batch in line.Batches)
                    {
                        oCredit.Lines.BatchNumbers.BatchNumber = batch.BatchNumber;
                        oCredit.Lines.BatchNumbers.Quantity = batch.Quantity;
                        oCredit.Lines.BatchNumbers.Add();
                    }
                }
            }
            int res = oCredit.Add();
            if (res != 0)
            {
                oCompany.GetLastError(out int errCode, out string errMsg);
                return ($"Failed to create adjustment invoice: [{errCode}] {errMsg}", false);
            }

            string newDocEntry = oCompany.GetNewObjectKey();
            return ($"Adjustment Invoice created successfully. DocEntry: {newDocEntry}", true);
        }
        private int FindBaseLine(SAPbobsCOM.Company oCompany, int baseEntry, InvoiceLineDto line)
        {
            SAPbobsCOM.Recordset rs =
                (SAPbobsCOM.Recordset)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            string sql;

            // Ưu tiên tìm theo batch nếu có
            if (line.Batches != null && line.Batches.Any())
            {
                string batch = line.Batches.First().BatchNumber;
                sql = $@"
            SELECT TOP 1 INV1.LineNum
            FROM INV1
            INNER JOIN OIBT ON INV1.ItemCode = OIBT.ItemCode
            WHERE INV1.DocEntry = {baseEntry}
              AND INV1.ItemCode = '{line.ItemCode}'
              AND OIBT.BatchNum = '{batch}'
            ORDER BY INV1.LineNum";
            }
            else
            {
                // fallback tìm theo ItemCode + Quantity gần đúng nhất
                sql = $@"
            SELECT TOP 1 LineNum
            FROM INV1
            WHERE DocEntry = {baseEntry}
              AND ItemCode = '{line.ItemCode}'
            ORDER BY ABS(Quantity - {line.Quantity})";
            }

            rs.DoQuery(sql);

            if (!rs.EoF)
                return Convert.ToInt32(rs.Fields.Item("LineNum").Value);

            return -1; // Không tìm thấy
        }
        public async Task<Respond> CreateInvoiceWithPaymentAsync(ARInvoice ar)
        {
            Respond respond = new Respond();
            //if(cookies == null || cookies.SessionTime < DateTime.Now)
            //{
                var (ckies, check1, Mes) = await _sessionManager.LoginAsync();
                if(check1)
                {
                    cookies.SessionTime = ckies.SessionTime;
                    cookies.B1SESSION = ckies.B1SESSION;
                    cookies.ROUTEID = ckies.ROUTEID;
                }    
            //}
                
            var _httpWebRequests = (HttpWebRequest)WebRequest.Create($"{_api.BaseUrl}/Invoices?$select=DocEntry&$filter=U_POS eq '{ar.InvoiceCode}'");
            _httpWebRequests.ContentType = "application/json";
            _httpWebRequests.Method = "GET";
            _httpWebRequests.KeepAlive = true;
            _httpWebRequests.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            _httpWebRequests.Headers.Add("B1S-WCFCompatible", "true");
            _httpWebRequests.Headers.Add("B1S-MetadataWithoutSession", "true");
            _httpWebRequests.Accept = "*/*";
            _httpWebRequests.ServicePoint.Expect100Continue = false;
            _httpWebRequests.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            _httpWebRequests.Headers.Add("Cookie" , cookies.B1SESSION + cookies.ROUTEID);
            _httpWebRequests.AutomaticDecompression = DecompressionMethods.GZip;
            var httpResponse = (HttpWebResponse)_httpWebRequests.GetResponse();
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                    using (var reader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var json = reader.ReadToEnd();
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("value", out var valueProp) && valueProp.ValueKind == JsonValueKind.Array)
                        {
                            var first = valueProp.EnumerateArray().FirstOrDefault();
                            if (first.ValueKind != JsonValueKind.Undefined && first.TryGetProperty("DocEntry", out var docEntryProp))
                            {
                                return new Respond
                                {
                                    InvoicePos = ar.InvoiceCode,
                                    Success = false,
                                    DocEntry = docEntryProp.ToString(),
                                    Error = $"Hóa đơn với U_POS {ar.InvoiceCode} đã được đồng bộ trước đó."
                                };
                            }
                        }
                    }
                    
            }
            
            var arInvoice = SapDiApiHelper.ToARInvoiceRequest(ar, "");
            string invoiceJson = JsonSerializer.Serialize(arInvoice);
            
            var result = new Respond();

            try
            {
                var (DocEntry,check2) = await CreateInvoiceAsync(arInvoice);
                if (check2)
                {
                    result.DocEntry = DocEntry;
                    result.Success = true;
                    result.InvoicePos = ar.InvoiceCode;
                }    
                    
                else
                {
                    result.Success = false;
                    result.Error = DocEntry;
                    result.InvoicePos = ar.InvoiceCode;
                    return result;
                }  
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
        }
        public async Task<(ARInvoiceCreditRequestDTO, int)> GetDocEntryARInvoiceAsync(string OriginalInvoiceCode)
        {
            //if (cookies == null || cookies.SessionTime < DateTime.Now)
            //{
                var (ckies, check, Mes) = await _sessionManager.LoginAsync();
                if (check)
                {
                    cookies.SessionTime = ckies.SessionTime;
                    cookies.B1SESSION = ckies.B1SESSION;
                    cookies.ROUTEID = ckies.ROUTEID;
                }
            //}
            var _httpWebRequests = (HttpWebRequest)WebRequest.Create($"{_api.BaseUrl}/Invoices?$select=DocEntry&$filter=U_POS eq '{OriginalInvoiceCode}'");
            _httpWebRequests.ContentType = "application/json";
            _httpWebRequests.Method = "GET";
            _httpWebRequests.KeepAlive = true;
            _httpWebRequests.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            _httpWebRequests.Headers.Add("B1S-WCFCompatible", "true");
            _httpWebRequests.Headers.Add("B1S-MetadataWithoutSession", "true");
            _httpWebRequests.Accept = "*/*";
            _httpWebRequests.ServicePoint.Expect100Continue = false;
            _httpWebRequests.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            _httpWebRequests.Headers.Add("Cookie", cookies.B1SESSION + cookies.ROUTEID);
            _httpWebRequests.AutomaticDecompression = DecompressionMethods.GZip;
            var httpResponse = (HttpWebResponse)_httpWebRequests.GetResponse();
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                using (var reader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var json = reader.ReadToEnd();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("value", out var valueProp) && valueProp.ValueKind == JsonValueKind.Array)
                    {
                        var first = valueProp.EnumerateArray().FirstOrDefault();
                        if (first.ValueKind != JsonValueKind.Undefined && first.TryGetProperty("DocEntry", out var docEntryProp))
                        {
                            int DocEntry = int.Parse(docEntryProp.ToString());
                            var result = GetARInvoiceAsync(DocEntry);
                            return (result.Result, DocEntry);
                        }    
                            
                        else
                            return (null,0);   
                    }
                }
                return (null, 0);
            }
            else
                return (null, 0);
        }
        public async Task<ARInvoiceCreditRequestDTO> GetARInvoiceAsync(int DocEntry)
        {
            //if (cookies == null || cookies.SessionTime < DateTime.Now)
            //{
                var (ckies, check, Mes) = await _sessionManager.LoginAsync();
                if (check)
                {
                    cookies.SessionTime = ckies.SessionTime;
                    cookies.B1SESSION = ckies.B1SESSION;
                    cookies.ROUTEID = ckies.ROUTEID;
                }
            //}
            var _httpWebRequests = (HttpWebRequest)WebRequest.Create($"{_api.BaseUrl}/Invoices("+ DocEntry + ")");
            _httpWebRequests.ContentType = "application/json";
            _httpWebRequests.Method = "GET";
            _httpWebRequests.KeepAlive = true;
            _httpWebRequests.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            _httpWebRequests.Headers.Add("B1S-WCFCompatible", "true");
            _httpWebRequests.Headers.Add("B1S-MetadataWithoutSession", "true");
            _httpWebRequests.Accept = "*/*";
            _httpWebRequests.ServicePoint.Expect100Continue = false;
            _httpWebRequests.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            _httpWebRequests.Headers.Add("Cookie", cookies.B1SESSION + cookies.ROUTEID);
            _httpWebRequests.AutomaticDecompression = DecompressionMethods.GZip;
            try
            {
                var httpResponse = (HttpWebResponse)_httpWebRequests.GetResponse();
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (var reader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var json = reader.ReadToEnd();
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        ARInvoiceCreditRequestDTO invoice = JsonSerializer.Deserialize<ARInvoiceCreditRequestDTO>(json, options);
                        return invoice;
                    }
                }
                else
                    return null;
            }
            catch { return null; }
            
        }
        public async Task<Respond> CreateCreditInvoiceWithPaymentAsync(ARInvoice ar)
        {
            Respond respond = new Respond();
            //if (cookies == null || cookies.SessionTime < DateTime.Now)
            //{
                var (ckies, check, Mes) = await _sessionManager.LoginAsync();
                if (check)
                {
                    cookies.SessionTime = ckies.SessionTime;
                    cookies.B1SESSION = ckies.B1SESSION;
                    cookies.ROUTEID = ckies.ROUTEID;
                }
            //}
            
            var _httpWebRequests = (HttpWebRequest)WebRequest.Create($"{_api.BaseUrl}/CreditNotes?$select=DocEntry&$filter=U_POS eq '{ar.InvoiceCode}'");
            _httpWebRequests.ContentType = "application/json";
            _httpWebRequests.Method = "GET";
            _httpWebRequests.KeepAlive = true;
            _httpWebRequests.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            _httpWebRequests.Headers.Add("B1S-WCFCompatible", "true");
            _httpWebRequests.Headers.Add("B1S-MetadataWithoutSession", "true");
            _httpWebRequests.Accept = "*/*";
            _httpWebRequests.ServicePoint.Expect100Continue = false;
            _httpWebRequests.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            _httpWebRequests.Headers.Add("Cookie", cookies.B1SESSION + cookies.ROUTEID);
            _httpWebRequests.AutomaticDecompression = DecompressionMethods.GZip;
            var httpResponse = (HttpWebResponse)_httpWebRequests.GetResponse();
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                using (var reader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var json = reader.ReadToEnd();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("value", out var valueProp) && valueProp.ValueKind == JsonValueKind.Array)
                    {
                        var first = valueProp.EnumerateArray().FirstOrDefault();
                        if (first.ValueKind != JsonValueKind.Undefined && first.TryGetProperty("DocEntry", out var docEntryProp))
                        {
                            return new Respond
                            {
                                InvoicePos = ar.InvoiceCode,
                                Success = false,
                                DocEntry = docEntryProp.ToString(),
                                Error = $"Hóa đơn điều chỉnh với U_POS {ar.InvoiceCode} đã được đồng bộ trước đó."
                            };
                        }
                    }
                }

            }
            var (arInvoice, DocEntry) = await GetDocEntryARInvoiceAsync(ar.OriginalInvoiceCode);
            var creditMemo = new ARInvoiceCreditRequest
            {
                DocDate = ar.DocDate,
                DocDueDate = ar.DocDate,
                TaxDate =  ar.DocDate,
                CardCode = arInvoice.CardCode,
                Comments = "Tạo hóa đơn bán hàng điều chỉnh tại POS",
                U_SoSeries = ar.MaSoHD,
                U_KyHieuHD = ar.KyHieuHD,
                U_SoChungTu = "POS" + ar.OriginalInvoiceCode,
                U_LoaiHoaDonBan = "HDBH01",
                U_POS = ar.InvoiceCode,
                U_PBG = ar.OriginalInvoiceCode,
                U_CCCD = ar.CardNumber,
                U_HoTen = ar.CardName,
                DocumentLines = arInvoice.DocumentLines.Select((line, index) => new ARInvoiceCreditLine
                {
                    BaseType = 13,
                    BaseEntry = DocEntry,
                    BaseLine = line.LineNum,
                    Quantity = ar.ARInvoice_Lines.FirstOrDefault(e=>e.ItemCode == line.ItemCode)?.Quantity ?? 0,
                    UnitPrice = line.UnitPrice,
                    VatGroup = (line.VatGroup ?? "").ToString(),
                    WarehouseCode = line.WarehouseCode,
                    BatchNumbers = line.BatchNumbers.Select(b => new ARInvoiceLineBatch
                    {
                        BatchNumber = ar.ARInvoice_Lines.FirstOrDefault(e => e.ItemCode == line.ItemCode)?.Batches?.FirstOrDefault(e => e.BatchNumber == b.BatchNumberProperty)?.BatchNumber ?? "",
                        Quantity = ar.ARInvoice_Lines.FirstOrDefault(e => e.ItemCode == line.ItemCode)?.Batches?.FirstOrDefault(e => e.BatchNumber == b.BatchNumberProperty)?.Quantity ?? 0
                    }).ToList()
                }).ToList()
            };
            string invoiceJson = JsonSerializer.Serialize(creditMemo);
            var result = new Respond();

            try
            {
                var (Entry, check3) = await CreateCreditInvoiceAsync(creditMemo);
                if (check3)
                {
                    result.DocEntry = Entry;
                    result.Success = true;
                    result.InvoicePos = ar.InvoiceCode;
                }
                else
                {
                    result.Success = false;
                    result.Error = Entry;
                    result.InvoicePos = ar.InvoiceCode;
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
        }

        public  async Task<(string, bool)> CreateCancelInvoiceDIAPIAsync(string Invoice, string OriginalInvoiceCode)
        {
            if (!_sapConnection.Connect())
                return ("Kết nối đến SAP Business One thất bại", false);

            var oCompany = _sapConnection.Company;
            var docEntries = _db.Set<DocEntryResult>()
            .FromSqlRaw("SELECT DocEntry FROM OINV WHERE U_POS = {0}", OriginalInvoiceCode)
            .Select(x => x.DocEntry).FirstOrDefault();
            if (docEntries == null)
                return ("Không tìm thấy hóa số đơn để huy", false);
            SAPbobsCOM.Documents oInvoice =
                (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInvoices);

            if (!oInvoice.GetByKey(docEntries))
                return ($"Không tìm thấy hóa đơn DocEntry {docEntries}", false);

            if (oInvoice.Cancelled == SAPbobsCOM.BoYesNoEnum.tYES)
                return ($"Hóa đơn {docEntries} đã bị hủy trước đó", false);

            var CancelInvoice = oInvoice.CreateCancellationDocument();
            if (CancelInvoice.Add() != 0)
            {
                oCompany.GetLastError(out int errCode, out string errMsg);
                return ($"Lỗi khi hủy hóa đơn: {errMsg}", false);
            }
            string newDocEntry = oCompany.GetNewObjectKey();
            SAPbobsCOM.Documents oCancelDoc =
            (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInvoices);
            if (oCancelDoc.GetByKey(int.Parse(newDocEntry)))
            {
                oCancelDoc.UserFields.Fields.Item("U_PBG").Value = OriginalInvoiceCode;
                oCancelDoc.UserFields.Fields.Item("U_POS").Value = Invoice;
                if (oCancelDoc.Update() != 0)
                {
                    oCompany.GetLastError(out int eCode, out string eMsg);
                }
            }
            return ($"Đã hủy hóa đơn {OriginalInvoiceCode} thành công, chứng từ hủy DocEntry = {newDocEntry}", true);

        }

        public async Task<Respond> CreateCCancleInvoice(ARInvoice ar)
        {
            Respond respond = new Respond();
            //if (cookies == null || cookies.SessionTime < DateTime.Now)
            //{
                var (ckies, check, Mes) = await _sessionManager.LoginAsync();
                if (check)
                {
                    cookies.SessionTime = ckies.SessionTime;
                    cookies.B1SESSION = ckies.B1SESSION;
                    cookies.ROUTEID = ckies.ROUTEID;
                }
            //}

            var _httpWebRequests = (HttpWebRequest)WebRequest.Create($"{_api.BaseUrl}/Invoices?$select=DocEntry&$filter=U_POS eq '{ar.OriginalInvoiceCode}'");
            _httpWebRequests.ContentType = "application/json";
            _httpWebRequests.Method = "GET";
            _httpWebRequests.KeepAlive = true;
            _httpWebRequests.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            _httpWebRequests.Headers.Add("B1S-WCFCompatible", "true");
            _httpWebRequests.Headers.Add("B1S-MetadataWithoutSession", "true");
            _httpWebRequests.Accept = "*/*";
            _httpWebRequests.ServicePoint.Expect100Continue = false;
            _httpWebRequests.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            _httpWebRequests.Headers.Add("Cookie", cookies.B1SESSION + cookies.ROUTEID);
            _httpWebRequests.AutomaticDecompression = DecompressionMethods.GZip;
            var httpResponse = (HttpWebResponse)_httpWebRequests.GetResponse();
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                using (var reader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var json = reader.ReadToEnd();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("value", out var valueProp) && valueProp.ValueKind == JsonValueKind.Array)
                    {
                        var first = valueProp.EnumerateArray().FirstOrDefault();
                        if (first.ValueKind != JsonValueKind.Undefined && first.TryGetProperty("DocEntry", out var docEntryProp))
                        {
                            return new Respond
                            {
                                InvoicePos = ar.InvoiceCode,
                                Success = false,
                                DocEntry = docEntryProp.ToString(),
                                Error = $"Hóa đơn điều chỉnh với U_POS {ar.InvoiceCode} đã được đồng bộ trước đó."
                            };
                        }
                    }
                }

            }
            var (arInvoice, DocEntry) = await GetDocEntryARInvoiceAsync(ar.OriginalInvoiceCode);
            var creditMemo = new ARInvoiceCreditRequest
            {
                DocDate = ar.DocDate,
                DocDueDate = ar.DocDate,
                TaxDate = ar.DocDate,
                CardCode = arInvoice.CardCode,
                Comments = "Tạo hóa đơn bán hàng điều chỉnh tại POS",
                U_SoSeries = ar.MaSoHD,
                U_KyHieuHD = ar.KyHieuHD,
                U_SoChungTu = "POS" + ar.OriginalInvoiceCode,
                U_LoaiHoaDonBan = "HDBH01",
                U_POS = ar.InvoiceCode,
                U_PBG = ar.OriginalInvoiceCode,
                U_CCCD = ar.CardNumber,
                U_HoTen = ar.CardName,
                DocumentLines = arInvoice.DocumentLines.Select((line, index) => new ARInvoiceCreditLine
                {
                    BaseType = 13,
                    BaseEntry = DocEntry,
                    BaseLine = line.LineNum,
                    Quantity = ar.ARInvoice_Lines.FirstOrDefault(e => e.ItemCode == line.ItemCode)?.Quantity ?? 0,
                    UnitPrice = line.UnitPrice,
                    VatGroup = (line.VatGroup ?? "").ToString(),
                    WarehouseCode = line.WarehouseCode,
                    BatchNumbers = line.BatchNumbers.Select(b => new ARInvoiceLineBatch
                    {
                        BatchNumber = ar.ARInvoice_Lines.FirstOrDefault(e => e.ItemCode == line.ItemCode)?.Batches?.FirstOrDefault(e => e.BatchNumber == b.BatchNumberProperty)?.BatchNumber ?? "",
                        Quantity = ar.ARInvoice_Lines.FirstOrDefault(e => e.ItemCode == line.ItemCode)?.Batches?.FirstOrDefault(e => e.BatchNumber == b.BatchNumberProperty)?.Quantity ?? 0
                    }).ToList()
                }).ToList()
            };
            string invoiceJson = JsonSerializer.Serialize(creditMemo);
            var result = new Respond();

            try
            {
                var (Entry, check4) = await CreateCreditInvoiceAsync(creditMemo);
                if (check4)
                {
                    result.DocEntry = Entry;
                    result.Success = true;
                    result.InvoicePos = ar.InvoiceCode;
                }
                else
                {
                    result.Success = false;
                    result.Error = Entry;
                    result.InvoicePos = ar.InvoiceCode;
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
        }


        public async Task<(string, bool)> CreateInvoiceDIAPIAsync(ARInvoice rq)
        {
            if (!_sapConnection.Connect())
                return ("Kết nối đến SAP Business One thất bại", false);

            var oCompany = _sapConnection.Company;

            // Đến đây oCompany chắc chắn đã có và đã connect
            SAPbobsCOM.Documents oInvoice = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInvoices);
            oInvoice.CardCode = rq.CardCode;
            oInvoice.DocDate = rq.DocDate;
            oInvoice.DocDueDate = rq.DocDate;
            oInvoice.Comments = "Tạo hóa đơn bán hàng tại POS";
            if (!string.IsNullOrEmpty(rq.MaSoHD)) oInvoice.UserFields.Fields.Item("U_SoSeries").Value = rq.MaSoHD;
            if (!string.IsNullOrEmpty(rq.KyHieuHD)) oInvoice.UserFields.Fields.Item("U_KyHieuHD").Value = rq.KyHieuHD;
            if (!string.IsNullOrEmpty(rq.InvoiceCode)) oInvoice.UserFields.Fields.Item("U_SoChungTu").Value = "POS" + rq.InvoiceCode;
            oInvoice.UserFields.Fields.Item("U_LoaiHoaDonBan").Value = "HDBH01";
            oInvoice.UserFields.Fields.Item("U_ThueTTDB").Value = 75.00;
            if (!string.IsNullOrEmpty(rq.InvoiceCode)) oInvoice.UserFields.Fields.Item("U_POS").Value = rq.InvoiceCode;
            if (!string.IsNullOrEmpty(rq.CardNumber)) oInvoice.UserFields.Fields.Item("U_CCCD").Value = rq.CardNumber;
            if (!string.IsNullOrEmpty(rq.CardName)) oInvoice.UserFields.Fields.Item("U_HoTen").Value = rq.CardName;
            for (int i = 0; i < rq.ARInvoice_Lines.Count; i++)
            {
                var line = rq.ARInvoice_Lines[i];
                if (i > 0) oInvoice.Lines.Add();

                oInvoice.Lines.ItemCode = line.ItemCode;
                oInvoice.Lines.Quantity = line.Quantity;
                oInvoice.Lines.UnitPrice = line.Price;
                oInvoice.Lines.WarehouseCode = rq.WhsCode;
                oInvoice.Lines.VatGroup = GetVatGroup(line.VatPercent ?? 10);

                // 🔹 Thêm lô hàng (nếu có)
                if (line.Batches != null && line.Batches.Any())
                {
                    foreach (var batch in line.Batches)
                    {
                        oInvoice.Lines.BatchNumbers.BatchNumber = batch.BatchNumber;
                        oInvoice.Lines.BatchNumbers.Quantity = batch.Quantity;
                        oInvoice.Lines.BatchNumbers.Add();
                    }
                }
            }
            int res = oInvoice.Add();
            if (res != 0)
            {
                oCompany.GetLastError(out int errCode, out string errMsg);
                return ($"Failed to create AR Invoice: [{errCode}] {errMsg}", false);
            }

            // 🔹 Lấy DocEntry của Invoice vừa tạo
            string newDocEntry = oCompany.GetNewObjectKey();
            return ($"AR Invoice created successfully. DocEntry: {newDocEntry}", true);
        }
        private string GetVatGroup(double vatPercent)
        {
            // ⚠️ Thay bằng mã nhóm VAT thực tế trong SAP (VD: "10%" => "V10")
            if (vatPercent == 10) return "10";
            if (vatPercent == 8) return "8";
            return "10"; // 0% VAT
        }
        public async Task<(string, bool)> CreateInvoiceAsync(ARInvoiceRequest rq)
        {
            HttpWebRequest httpWebRequests = (HttpWebRequest)WebRequest.Create($"{_api.BaseUrl}" + "/Invoices");
            httpWebRequests.ContentType = "application/json";
            httpWebRequests.Method = "POST";
            httpWebRequests.KeepAlive = true;
            httpWebRequests.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            httpWebRequests.Headers.Add("B1S-WCFCompatible", "true");
            httpWebRequests.Headers.Add("B1S-MetadataWithoutSession", "true");
            httpWebRequests.Accept = "*/*";
            httpWebRequests.ServicePoint.Expect100Continue = false;
            httpWebRequests.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            httpWebRequests.Headers.Add("Cookie", cookies.B1SESSION + cookies.ROUTEID);
            httpWebRequests.AutomaticDecompression = DecompressionMethods.GZip;
            var json = JsonConvert.SerializeObject(rq);
            using (var streamWriter = new StreamWriter(httpWebRequests.GetRequestStream()))

            { streamWriter.Write(json); }
            try
            {
                using (var httpResponse = (HttpWebResponse)httpWebRequests.GetResponse())
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? Stream.Null))
                {
                    string result = await streamReader.ReadToEndAsync();
                    Console.WriteLine("Response JSON: " + result);
                    var jObject = JObject.Parse(result);
                    var docEntry = jObject["DocEntry"]?.ToString();
                    Console.WriteLine("DocEntry: " + docEntry);
                    return (docEntry, true);
                }
            }
            catch (WebException ex)
            {
                using (var stream = ex.Response?.GetResponseStream())
                using (var reader = new StreamReader(stream ?? Stream.Null))
                {
                    string errorText = await reader.ReadToEndAsync();
                    var jObj = JObject.Parse(errorText);

                    // Lấy code
                    string code = jObj["error"]?["code"]?.ToString();

                    // Lấy message
                    string message = jObj["error"]?["message"]?["value"]?.ToString();
                    return (message, false);
                }
                
            }
        }
        public async Task<(string,bool)> CreatePaymentAsync(string paymentJson)
        {
            //var (sessionId, routId) = await _sessionManager.GetSessionAsync();
            HttpWebRequest httpWebRequests = (HttpWebRequest)WebRequest.Create($"{_api.BaseUrl}" + "/IncomingPayments");
            httpWebRequests.ContentType = "application/json";
            httpWebRequests.Method = "POST";
            httpWebRequests.KeepAlive = true;
            httpWebRequests.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            httpWebRequests.Headers.Add("B1S-WCFCompatible", "true");
            httpWebRequests.Headers.Add("B1S-MetadataWithoutSession", "true");
            httpWebRequests.Accept = "*/*";
            httpWebRequests.ServicePoint.Expect100Continue = false;
            httpWebRequests.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            httpWebRequests.Headers.Add("Cookie", cookies.B1SESSION + cookies.ROUTEID);
            httpWebRequests.AutomaticDecompression = DecompressionMethods.GZip;
            using (var streamWriter = new StreamWriter(httpWebRequests.GetRequestStream()))

            { streamWriter.Write(paymentJson); }
            try
            {
                using (var httpResponse = (HttpWebResponse)httpWebRequests.GetResponse())
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? Stream.Null))
                {
                    string result = await streamReader.ReadToEndAsync();
                    Console.WriteLine("Response JSON: " + result);
                    var jObject = JObject.Parse(result);
                    var docEntry = jObject["DocNum"]?.ToString();
                    Console.WriteLine("DocNum: " + docEntry);
                    return (docEntry, true);
                }
            }
            catch (WebException ex)
            {
                using (var stream = ex.Response?.GetResponseStream())
                using (var reader = new StreamReader(stream ?? Stream.Null))
                {
                    string errorText = await reader.ReadToEndAsync();
                    Console.WriteLine("Error response: " + errorText);
                    return ("Error response: " + errorText, false);
                }

            }
        }
        public async Task<(string, bool)> CreateCreditInvoiceAsync(ARInvoiceCreditRequest rq)
        {
            HttpWebRequest httpWebRequests = (HttpWebRequest)WebRequest.Create($"{_api.BaseUrl}" + "/CreditNotes");
            httpWebRequests.ContentType = "application/json";
            httpWebRequests.Method = "POST";
            httpWebRequests.KeepAlive = true;
            httpWebRequests.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            httpWebRequests.Headers.Add("B1S-WCFCompatible", "true");
            httpWebRequests.Headers.Add("B1S-MetadataWithoutSession", "true");
            httpWebRequests.Accept = "*/*";
            httpWebRequests.ServicePoint.Expect100Continue = false;
            httpWebRequests.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            httpWebRequests.Headers.Add("Cookie", cookies.B1SESSION + cookies.ROUTEID);
            httpWebRequests.AutomaticDecompression = DecompressionMethods.GZip;
            var json = JsonConvert.SerializeObject(rq);
            using (var streamWriter = new StreamWriter(httpWebRequests.GetRequestStream()))

            { streamWriter.Write(json); }
            try
            {
                using (var httpResponse = (HttpWebResponse)httpWebRequests.GetResponse())
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? Stream.Null))
                {
                    string result = await streamReader.ReadToEndAsync();
                    Console.WriteLine("Response JSON: " + result);
                    var jObject = JObject.Parse(result);
                    var docEntry = jObject["DocEntry"]?.ToString();
                    Console.WriteLine("DocEntry: " + docEntry);
                    return (docEntry, true);
                }
            }
            catch (WebException ex)
            {
                using (var stream = ex.Response?.GetResponseStream())
                using (var reader = new StreamReader(stream ?? Stream.Null))
                {
                    string errorText = await reader.ReadToEndAsync();
                    var jObj = JObject.Parse(errorText);

                    // Lấy code
                    string code = jObj["error"]?["code"]?.ToString();

                    // Lấy message
                    string message = jObj["error"]?["message"]?["value"]?.ToString();
                    return (message, false);
                }

            }
        }
        public async Task<(string, bool)> CreateOutPaymentAsync(string paymentJson)
        {
            //var (sessionId, routId) = await _sessionManager.GetSessionAsync();
            HttpWebRequest httpWebRequests = (HttpWebRequest)WebRequest.Create($"{_api.BaseUrl}" + "/VendorPayments");
            httpWebRequests.ContentType = "application/json";
            httpWebRequests.Method = "POST";
            httpWebRequests.KeepAlive = true;
            httpWebRequests.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            httpWebRequests.Headers.Add("B1S-WCFCompatible", "true");
            httpWebRequests.Headers.Add("B1S-MetadataWithoutSession", "true");
            httpWebRequests.Accept = "*/*";
            httpWebRequests.ServicePoint.Expect100Continue = false;
            httpWebRequests.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            httpWebRequests.Headers.Add("Cookie", cookies.B1SESSION + cookies.ROUTEID);
            httpWebRequests.AutomaticDecompression = DecompressionMethods.GZip;
            using (var streamWriter = new StreamWriter(httpWebRequests.GetRequestStream()))

            { streamWriter.Write(paymentJson); }
            try
            {
                using (var httpResponse = (HttpWebResponse)httpWebRequests.GetResponse())
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? Stream.Null))
                {
                    string result = await streamReader.ReadToEndAsync();
                    Console.WriteLine("Response JSON: " + result);
                    var jObject = JObject.Parse(result);
                    var docEntry = jObject["DocNum"]?.ToString();
                    Console.WriteLine("DocNum: " + docEntry);
                    return (docEntry, true);
                }
            }
            catch (WebException ex)
            {
                using (var stream = ex.Response?.GetResponseStream())
                using (var reader = new StreamReader(stream ?? Stream.Null))
                {
                    string errorText = await reader.ReadToEndAsync();
                    Console.WriteLine("Error response: " + errorText);
                    return ("Error response: " + errorText, false);
                }

            }
        }
    }
    public class SapDiApiHelper
    {
        public static Respond ParseBatchResponse(string rawResponse)
        {
            int depth = 0;
            int startIndex = -1;

            for (int i = 0; i < rawResponse.Length; i++)
            {
                char c = rawResponse[i];

                if (c == '{')
                {
                    if (depth == 0)
                        startIndex = i;
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && startIndex >= 0)
                    {
                        string json = rawResponse.Substring(startIndex, i - startIndex + 1);

                        try
                        {
                            JObject obj = JObject.Parse(json);
                            if (obj["error"] == null)
                            {
                                return new Respond
                                {
                                    Success = true,
                                    DocEntry = obj["DocEntry"]?.ToString(),
                                    Error = null
                                };
                            }
                            else
                            {
                                

                                JObject jObj = JObject.Parse(json);
                                string errorMessage = obj["error"]?["message"]?["value"]?.ToString();
                                return new Respond
                                {
                                    Success = false,
                                    Error = errorMessage
                                };
                            }    
                        }
                        catch
                        {
                            return new Respond
                            {
                                InvoicePos = null!,
                                Success = false,
                                DocEntry = null,
                                Error = "Lỗi không đọc được json"
                            };
                        }

                        startIndex = -1;
                    }
                }
            }

            // Không tìm thấy JSON thành công nào
            return new Respond
            {
                InvoicePos = null!,
                Success = false,
                DocEntry = null,
                Error = "Không tìm thấy JSON top-level thành công"
            };
        }


        public static ARInvoiceRequest ToARInvoiceRequest(ARInvoice src, string warehouseCode)
        {
            var req = new ARInvoiceRequest
            {
                DocDate = src.DocDate,
                DocDueDate = src.DocDate,
                TaxDate = src.DocDate,
                CardCode = src.CardCode,
                Comments = "Tạo hóa đơn bán hàng tại POS",
                U_SoSeries = src.MaSoHD,
                U_KyHieuHD = src.KyHieuHD,
                U_SoChungTu = "POS" + src.InvoiceCode,
                U_LoaiHoaDonBan = "HDBH01",
                U_POS = src.InvoiceCode,
                U_CCCD = src.CardNumber,
                U_HoTen = src.CardName,
                DocumentLines = src.ARInvoice_Lines.Select(line => new ARInvoiceLine
                {
                    ItemCode = line.ItemCode,
                    Quantity = line.Quantity,
                    UnitPrice = line.Price,
                    VatGroup = (line.VatPercent ?? 0).ToString(),
                    WarehouseCode = src.WhsCode,
                    BatchNumbers = line.Batches.Select(b => new ARInvoiceLineBatch
                    {
                        BatchNumber = b.BatchNumber,
                        Quantity = b.Quantity
                    }).ToList()
                }).ToList()
            };

            return req;
        }

        //public static ARInvoiceRequest ToARCreditInvoiceRequest(ARInvoice src, string warehouseCode)
        //{

        //    var req = new ARInvoiceRequest
        //    {
        //        CardCode = src.CardCode,
        //        DocDate = src.DocDate,
        //        DocDueDate = src.DocDate,
        //        TaxDate = src.DocDate,
        //        Comments = "Tạo Điều chỉnh hóa đơn bán hàng tại POS",
        //        U_SoSeries = src.MaSoHD,
        //        U_KyHieuHD = src.KyHieuHD,
        //        U_LoaiHoaDonBan = "HDBH01",
        //        U_POS = src.InvoiceCode,
        //        U_SoChungTu = "POS" +src.InvoiceCode,
        //        U_CCCD = src.CardNumber,
        //        U_HoTen = src.CardName,
        //        U_PBG = src.OriginalInvoiceCode ?? "",
        //        DocumentLines = src.ARInvoice_Lines.Select(line => new ARInvoiceLine
        //        {
        //            BaseType = line.BaseType,
        //            BaseEntry = line.BaseEntry,
        //            BaseLine = line.BaseLine,
        //            ItemCode = line.ItemCode,
        //            Quantity = line.Quantity,
        //            UnitPrice = line.Price,
        //            VatGroup = (line.VatPercent ?? 0).ToString(),
        //            WarehouseCode = src.WhsCode,
        //            BatchNumbers = line.Batches.Select(b => new ARInvoiceLineBatch
        //            {
        //                BatchNumber = b.BatchNumber,
        //                Quantity = b.Quantity
        //            }).ToList()
        //        }).ToList()
        //    };

        //    return req;
        //}
    }
}
