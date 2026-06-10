using Gridify;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAP_API.Data;
using SAP_API.Model;
using SAPbouiCOM;
using System.Data;
using System.Data.Common;
using System.Net;
using System.Text.Json;
using System.Threading.Channels;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace SAP_API.Service
{
    public class OWTRService
    {
        private readonly Cookies cookies = new Cookies();
        private readonly SapSessionManager _sessionManager;
        private readonly SAPConnection _sapConnection;
        private readonly APISetting _api;
        private readonly AppDbContext _db;
        public OWTRService(SapSessionManager sessionManager, IOptions<APISetting> api ,AppDbContext db, SAPConnection sapConnection)
        {
            _sessionManager = sessionManager;
            _api = api.Value;
            _db = db;
            _sapConnection = sapConnection;
        }
        public async Task<(Message,List<OWTR>)> GetOwtrAsync(string? DocNumber, string store)
        {
            Message message = new Message();
            List<OWTR> owtrs = new List<OWTR>();
            try
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
                string whsCode = store;
                HttpWebRequest _httpWebRequests;
                if(DocNumber.IsNullOrEmpty())
                    _httpWebRequests = (HttpWebRequest)WebRequest.Create($"{_api.BaseUrl}/StockTransfers?$filter=ToWarehouse  eq '{store}'");
                else
                    _httpWebRequests = (HttpWebRequest)WebRequest.Create($"{_api.BaseUrl}/StockTransfers?$filter=DocEntry  eq {DocNumber}");
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
                HttpWebResponse httpResponse = null;
                try
                {
                    httpResponse = (HttpWebResponse)_httpWebRequests.GetResponse();
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        using (var reader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            var json = reader.ReadToEnd();
                            var jObject = JObject.Parse(json);
                            var valueArray = jObject["value"] as JArray;

                            if (valueArray != null)
                            {
                                foreach (var item in valueArray)
                                {
                                    if (DocNumber.IsNullOrEmpty())
                                    {
                                        if (item["ToWarehouse"].ToString() == whsCode & item["U_POS"].ToString() == "")
                                        {
                                            var owtr = new OWTR
                                            {
                                                DocEntry = item["DocEntry"]?.ToString(),
                                                Note = item["Comments"]?.ToString(),
                                                WarehouseSapCode = item["ToWarehouse"]?.ToString(),
                                                DocDate = DateTime.Parse(item["DocDate"].ToString())
                                            };
                                            owtrs.Add(owtr);

                                        }
                                    }else
                                    {
                                        if (item["ToWarehouse"].ToString() == whsCode & item["U_POS"].ToString() == "")
                                        {
                                            var owtr = new OWTR
                                            {
                                                DocEntry = item["DocEntry"]?.ToString(),
                                                Note = item["Comments"]?.ToString(),
                                                WarehouseSapCode = item["ToWarehouse"]?.ToString(),
                                                DocDate = DateTime.Parse(item["DocDate"].ToString()),
                                                ItemDetail = item["StockTransferLines"]?
                                            .Select(line => new WTR1
                                            {
                                                ItemCode = line["ItemCode"]?.ToString(),
                                                ItemName = line["ItemDescription"]?.ToString(),
                                                Quantity = line["Quantity"]?.ToObject<double>() ?? 0,
                                                Batch = line["BatchNumbers"]?.Select(b => new Batch
                                                {
                                                    BatchNumber = b["BatchNumberProperty"].ToString(),
                                                    Quantity = b["Quantity"]?.ToObject<double>() ?? 0,
                                                    mnfDate = DateTime.TryParse(b["ManufacturingDate"]?.ToString() ??"", out var mnf) ? mnf : DateTime.MinValue,
                                                    expDate = DateTime.TryParse(b["ExpiryDate"]?.ToString() ?? "", out var exp) ? exp : DateTime.MinValue
                                                }).ToList() ?? new List<Batch>()
                                            }).ToList() ?? new List<WTR1>()
                                            };
                                            owtrs.Add(owtr);

                                        }
                                    }    
                                        
                                }
                                return (null, owtrs);
                            }
                            else
                            {
                                return (null, new List<OWTR>());
                            }
                            
                        }
                        return (null, new List<OWTR>());
                    }
                    else
                    {
                        // Xử lý trường hợp không OK
                        using var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? Stream.Null);
                        string result = await streamReader.ReadToEndAsync();
                        var jObject = JObject.Parse(result);
                        string errorMessage = jObject["error"]?["message"]?["value"]?.ToString();
                        message.Status = 400;
                        message.Error = errorMessage;
                        return (message, null);

                    }
                }
                catch
                {
                    using var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? Stream.Null);
                    string result = await streamReader.ReadToEndAsync();
                    var jObject = JObject.Parse(result);
                    string errorMessage = jObject["error"]?["message"]?["value"]?.ToString();
                    message.Status = 400;
                    message.Error = errorMessage;
                    return (message, null);
                }
            }
            catch (Exception ex)
            {
                message.Status = 400;
                message.Error = ex.Message;
                return (message, null);
            }
            
        }
        public async Task<(Message, List<OWTR>)> GetOwtrDIAPIAsync(string? DocNumber, string store)
        {
            Message message = new Message();
            List<OWTR> owtrs = new List<OWTR>();
            try
            {
                if(DocNumber.IsNullOrEmpty())
                {
                    var flatList = await _db.OWTRView.Where(e => e.warehouseSapCode == store).ToListAsync();
                    owtrs = flatList.GroupBy(r => r.DocEntry)
                    .Select(g => new OWTR
                    {
                        WarehouseSapCode = g.First().warehouseSapCode,
                        DocEntry = g.First().Docnumber,
                        DocDate = g.First().DocDate,
                        Note = g.First().note,
                        ItemDetail = g.GroupBy(i => (string)i.itemCode)
                                      .Select(iGroup => new WTR1
                                      {
                                          ItemCode = iGroup.Key,
                                          ItemName = iGroup.First().itemName,
                                          Quantity = (double)iGroup.First().quantity,
                                          Batch = iGroup
                                              .Where(b => b.BatchNum != null)
                                              .Select(b => new Batch
                                              {
                                                  BatchNumber = b.BatchNum,
                                                  Quantity = (double)b.QtyBatch,
                                                  expDate = (DateTime?)b.ExpDate,
                                                  mnfDate = (DateTime?)b.MnfDate
                                              }).ToList()
                                      }).ToList()
                    }).ToList();
                    return (null, owtrs);
                }
                else
                {
                    var flatList = await _db.OWTRView.Where(e => e.warehouseSapCode == store && e.Docnumber == DocNumber).ToListAsync();
                    owtrs = flatList.GroupBy(r => r.DocEntry)
                    .Select(g => new OWTR
                    {
                        WarehouseSapCode = g.First().warehouseSapCode,
                        DocEntry = g.First().Docnumber,
                        DocDate = g.First().DocDate,
                        Note = g.First().note,
                        ItemDetail = g.GroupBy(i => (string)i.itemCode)
                                      .Select(iGroup => new WTR1
                                      {
                                          ItemCode = iGroup.Key,
                                          ItemName = iGroup.First().itemName,
                                          Quantity = (double)iGroup.First().quantity,
                                          Batch = iGroup
                                              .Where(b => b.BatchNum != null)
                                              .Select(b => new Batch
                                              {
                                                  BatchNumber = b.BatchNum,
                                                  Quantity = (double)b.QtyBatch,
                                                  expDate = (DateTime?)b.ExpDate,
                                                  mnfDate = (DateTime?)b.MnfDate
                                              }).ToList()
                                      }).ToList()
                    }).ToList();
                    return (null, owtrs);
                }
            }
            catch (Exception ex)
            {
                message.Status = 400;
                message.Error = ex.Message;
                return (message, null);
            }

        }
        public async Task<Message> UpdateOwtr(string DocNumber, string store, string DocNumPos)
        {
            Message message = new Message();
            try
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
                string whsCode = "";
                //if (store == "CSQO")
                //{
                //    whsCode = "CHGTSPQO";
                //}
                //else if (store == "CSNT")
                //{
                //    whsCode = "CHGTSPNT";
                //}
                //else
                //{
                //    message.Status = 400;
                //    message.Error = "Mã cửa hàng không đúng";
                //    return message;
                //}
                var _httpWebRequests = (HttpWebRequest)WebRequest.Create($"{_api.BaseUrl}/StockTransfers?$filter=DocEntry  eq {DocNumber} and ToWarehouse eq '"+ whsCode + "' and (U_POS eq null or U_POS eq '')");
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
                HttpWebResponse httpResponse = null;
                try
                {
                    httpResponse = (HttpWebResponse)_httpWebRequests.GetResponse();
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        using (var reader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            var json = reader.ReadToEnd();
                            var jObject = JObject.Parse(json);
                            var valueArray = jObject["value"] as JArray;

                            if (valueArray != null)
                            {
                                HttpWebRequest httpWebRequests = (HttpWebRequest)WebRequest.Create($"{_api.BaseUrl}" + "/StockTransfers("+ DocNumber +")");
                                httpWebRequests.ContentType = "application/json";
                                httpWebRequests.Method = "PATCH";
                                httpWebRequests.KeepAlive = true;
                                httpWebRequests.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                                httpWebRequests.Headers.Add("B1S-WCFCompatible", "true");
                                httpWebRequests.Headers.Add("B1S-MetadataWithoutSession", "true");
                                httpWebRequests.Accept = "*/*";
                                httpWebRequests.ServicePoint.Expect100Continue = false;
                                httpWebRequests.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                                httpWebRequests.Headers.Add("Cookie", cookies.B1SESSION + cookies.ROUTEID);
                                httpWebRequests.AutomaticDecompression = DecompressionMethods.GZip;
                                var body = new
                                {
                                    U_POS = DocNumPos
                                };
                                var jsonStock = JsonConvert.SerializeObject(body);
                                using (var streamWriter = new StreamWriter(httpWebRequests.GetRequestStream()))

                                { streamWriter.Write(jsonStock); }
                                try
                                {
                                    using (httpResponse = (HttpWebResponse)httpWebRequests.GetResponse())
                                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? Stream.Null))
                                    {
                                        string result = await streamReader.ReadToEndAsync();
                                        return (null);
                                    }
                                }
                                catch (WebException ex)
                                {
                                    using (var stream = ex.Response?.GetResponseStream())
                                    using (var reader1 = new StreamReader(stream ?? Stream.Null))
                                    {
                                        string errorText = await reader1.ReadToEndAsync();
                                        var jObj = JObject.Parse(errorText);

                                        // Lấy code
                                        string code = jObj["error"]?["code"]?.ToString();

                                        // Lấy message
                                        message.Error = jObj["error"]?["message"]?["value"]?.ToString();
                                        message.Status = 400;
                                        return (message);
                                    }

                                }
                            }
                            else
                            {
                                message.Error = "Không có bản ghi để cập nhập";
                                message.Status = 400;
                                return (message);
                            }

                        }
                    }
                    else
                    {
                        // Xử lý trường hợp không OK
                        using var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? Stream.Null);
                        string result = await streamReader.ReadToEndAsync();
                        var jObject = JObject.Parse(result);
                        string errorMessage = jObject["error"]?["message"]?["value"]?.ToString();
                        message.Status = 400;
                        message.Error = errorMessage;
                        return (message);

                    }
                }
                catch
                {
                    using var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? Stream.Null);
                    string result = await streamReader.ReadToEndAsync();
                    var jObject = JObject.Parse(result);
                    string errorMessage = jObject["error"]?["message"]?["value"]?.ToString();
                    message.Status = 400;
                    message.Error = errorMessage;
                    return (message);
                }
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                message.Status = 400;
                return message;
            }
        }



        public async Task<Message> UpdateOwtrDIAPI(string DocNumber, string store, string DocNumPos)
        {
            Message message = new Message();
            try
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE OWTR SET U_POS = @POS WHERE DocEntry = @DocNumbers",
                    new { POS = DocNumPos, DocNumbers = DocNumber }
                );
                return null;
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                message.Status = 400;
                return message;
            }
        }

        public async Task<(Message, List<OCRDView>, int)> GetOCRDAsync(GridifyQuery query)
        {
            Message message = new Message();
            try
            {
                var ocrd = _db.OCRDView
                   .AsNoTracking()
                   .ApplyFiltering(query);
                   
                var total = await ocrd.CountAsync();
                var doc = await ocrd.ApplyOrdering(query).ApplyPaging(query).ToListAsync();
                return (null, doc, total);
                    
            }
            catch (Exception ex)
            {
                message.Status = 400;
                message.Error = ex.Message;
                return (message, null,0);
            }

        }
    }
}
