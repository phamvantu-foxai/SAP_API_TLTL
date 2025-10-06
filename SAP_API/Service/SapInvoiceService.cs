using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAP_API.Data;
using SAP_API.Model;
using SAPbobsCOM;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Message = SAP_API.Model.Message;

namespace SAP_API.Service
{
    public class SapInvoiceService
    {
        private readonly SapDiApiHelper _sapHelper;
        private readonly HttpClient _httpClient;
        private readonly SapSessionManager _sessionManager;
        private readonly APISetting _api;
        private readonly Cookies cookies = new Cookies();
        public SapInvoiceService(SapDiApiHelper sapHelper, IOptions<APISetting> api, SapSessionManager sessionManager, AppDbContext db)
        {
            _sapHelper = sapHelper;
            _httpClient = new HttpClient();
            _api = api.Value;
            _sessionManager = sessionManager;
        }
        /// <summary>
        /// Tạo hóa đơn bán hàng và phiếu thu tiền mặt.
        /// </summary>
        public async Task<Respond> CreateInvoiceWithPaymentAsync(ARInvoice ar)
        {
            Respond respond = new Respond();
            if(cookies == null || cookies.SessionTime < DateTime.Now)
            {
                var (ckies, check, Mes) = await _sessionManager.LoginAsync();
                if(check)
                {
                    cookies.SessionTime = ckies.SessionTime;
                    cookies.B1SESSION = ckies.B1SESSION;
                    cookies.ROUTEID = ckies.ROUTEID;
                }    
            }
                
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
                var (DocEntry,check) = await CreateInvoiceAsync(arInvoice);
                if (check)
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
            if (cookies == null || cookies.SessionTime < DateTime.Now)
            {
                var (ckies, check, Mes) = await _sessionManager.LoginAsync();
                if (check)
                {
                    cookies.SessionTime = ckies.SessionTime;
                    cookies.B1SESSION = ckies.B1SESSION;
                    cookies.ROUTEID = ckies.ROUTEID;
                }
            }
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
            if (cookies == null || cookies.SessionTime < DateTime.Now)
            {
                var (ckies, check, Mes) = await _sessionManager.LoginAsync();
                if (check)
                {
                    cookies.SessionTime = ckies.SessionTime;
                    cookies.B1SESSION = ckies.B1SESSION;
                    cookies.ROUTEID = ckies.ROUTEID;
                }
            }
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
            if (cookies == null || cookies.SessionTime < DateTime.Now)
            {
                var (ckies, check, Mes) = await _sessionManager.LoginAsync();
                if (check)
                {
                    cookies.SessionTime = ckies.SessionTime;
                    cookies.B1SESSION = ckies.B1SESSION;
                    cookies.ROUTEID = ckies.ROUTEID;
                }
            }
            
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
                var (Entry, check) = await CreateCreditInvoiceAsync(creditMemo);
                if (check)
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
