using Gridify;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAP_API.Data;
using SAP_API.Model;
using SAPbobsCOM;
using SAPbouiCOM;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using ItemInfo = SAP_API.Model.ItemInfo;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using Message = SAP_API.Model.Message;

namespace SAP_API.Service
{
    public class ItemService
    {
        private readonly SapDiApiHelper _sapHelper;
        private readonly HttpClient _httpClient;
        private readonly SapSessionManager _sessionManager;
        private readonly APISyn _api;
        private readonly APIEcomaint _apiAPIEcomaint;
        private readonly Cookies cookies = new Cookies();
        private readonly AppDbContext _db;
        public ItemService(SapDiApiHelper sapHelper, IOptions<APISyn> api, IOptions<APIEcomaint> apiAPIEcomaint, AppDbContext db)
        {
            _sapHelper = sapHelper;
            _httpClient = new HttpClient();
            _apiAPIEcomaint = apiAPIEcomaint.Value;
            _api = api.Value;
            _db = db;
        }
        public async Task<(Message, List<ItemOnhand>)> GetItemOnhandByLocationAsync(List<string> itemCodes)
        {
            Message message = new Message();
            try
            {
                var query = _db.Set<ItemOnhand>().AsQueryable();

                if (itemCodes != null && itemCodes.Any())
                {
                    query = query.Where(e => itemCodes.Contains(e.ItemCode));
                }

                return (null, await query.ToListAsync());
            }
            catch (Exception ex)
            {
                message.Status = 400;
                message.Error = ex.Message;
                return (message, null);
            }
            
        }
        public async Task<(Message, List<ItemInfo>, int)> GetItemInforAsync(GridifyQuery q,List<string> itemCodes)
        {
            Message message = new Message();
            try
            {
                var query = _db.ItemInfo
                   .AsNoTracking()
                   .ApplyFiltering(q);
                if (itemCodes != null && itemCodes.Any())
                {
                    query = query.Where(e => itemCodes.Contains(e.ItemCode));
                }
                var total = await query.CountAsync();
                var doc = await query.ApplyOrdering(q).ApplyPaging(q).ToListAsync();

                

                return (null, doc, total);
            }
            catch (Exception ex)
            {
                message.Status = 400;
                message.Error = ex.Message;
                return (message, null,0);
            }

        }
        static void WriteLog(string message)
        {
            Directory.CreateDirectory("Logs");
            string path = "Logs/error.log";
            File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
        }
        public async Task<bool> GetItemMasterData()
        {
            try
            {
                var item = await _db.ItemView.ToListAsync();
                var json = JsonConvert.SerializeObject(item, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_api.BaseUrl + "api/Item/sync-OITM", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return false;
                }
            }
            catch { }

            return true;
        }
        public async Task<bool> GetItemPrice()
        {
            try
            {
                var priceListData = await _db.PriceListView
            .ToListAsync();
                var dto = new ProductPriceListSyncDto
                {
                    Creator = priceListData.FirstOrDefault()?.PriceListCode.ToString(),
                    PriceListName = priceListData.FirstOrDefault()?.PriceListName,
                    ProductPriceListLine = priceListData
                        .Select(x => new ProductPriceListLineSyncDto
                        {
                            ItemCode = x.ItemCode,
                            SellingPrice = x.SellingPrice
                        })
                        .ToList()
                };

                var json = JsonConvert.SerializeObject(dto, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_api.BaseUrl + "api/ProductPriceList/sync", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return false;
                }
            }
            catch
            {

            }

            return true;
        }
        public async Task<bool> GetTransfer()
        {
            try
            {
                var items = await _db.TransferView.ToListAsync();
                List<TransferView> ls = new List<TransferView>();
                foreach (var item in items.Select(e => e.Stt).Distinct())
                {
                    ls.AddRange(items.Where(e => e.Stt == item).ToList());
                    var json = JsonConvert.SerializeObject(ls, Formatting.Indented);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(_apiAPIEcomaint.BaseUrl + "insert-multi_phu_tung", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        WriteLog(error);
                    }
                    else
                    {
                        WriteLog("thành công 2");
                        _db.Database.ExecuteSqlRaw("update OWTR set U_MaPhieuBaoTri = {0} where DocEntry = {1}", item.ToString(), item);
                        WriteLog("thành công 2");
                    }
                    ls = new List<TransferView>();
                }
            }catch
            {

            }
            return true;
        }
        public async Task<bool> GetIssue()
        {
            try
            {
                var items = await _db.GoodIssueView.ToListAsync();
                List<GoodIssueView> ls = new List<GoodIssueView>();
                foreach (var item in items.Select(e => e.MS_DH_XUAT_PT).Distinct())
                {
                    ls.AddRange(items.Where(e => e.MS_DH_XUAT_PT == item).ToList());
                    var json = JsonConvert.SerializeObject(ls, Formatting.Indented);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(_apiAPIEcomaint.BaseUrl + "insert-multi_xuat_phu_tung", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        WriteLog(error);
                    }
                    else
                    {
                        WriteLog("thành công 1");
                        _db.Database.ExecuteSqlRaw("update OIGE set U_POS = {0} where DocEntry = {1}", item.ToString(), item);
                        WriteLog("thành công 1");
                    }
                    ls = new List<GoodIssueView>();
                }
            }
            catch
            {

            }
            return true;
        }
        public async Task<bool> GetGoodReceipt()
        {
            try
            {
                var items = await _db.GoodReceiptView.ToListAsync();
                List<GoodReceiptView> ls = new List<GoodReceiptView>();
                foreach (var item in items.Select(e => e.mS_DH_NHAP_PT).Distinct())
                {
                    ls.AddRange(items.Where(e => e.mS_DH_NHAP_PT == item).ToList());
                    var json = JsonConvert.SerializeObject(ls, Formatting.Indented);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(_apiAPIEcomaint.BaseUrl + "insert-multi_nhap_phu_tung", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        WriteLog(error);
                    }
                    else
                    {
                        WriteLog("thành công");
                        _db.Database.ExecuteSqlRaw("update OIGN set U_POS = {0} where DocEntry = {1}", item, item);
                        WriteLog("thành công");
                    }
                    ls = new List<GoodReceiptView>();
                }
            }
            catch
            {

            }
            return true;
        }
    }
}
