using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAP_API.Data;
using SAP_API.Model;
using SAPbouiCOM;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

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
        static void WriteLog(string message)
        {
            Directory.CreateDirectory("Logs");
            string path = "Logs/error.log";
            File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
        }
        public async Task<bool> GetItemMasterData()
        {
            var item = await _db.ItemView.ToListAsync();
            var json = JsonConvert.SerializeObject(item, Formatting.Indented);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_api.BaseUrl+"api/Item/sync-OITM", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return false;
            }

            return true;
        }
        public async Task<bool> GetItemPrice()
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

            return true;
        }
        public async Task<bool> GetTransfer()
        {
            var items = await _db.TransferView.ToListAsync();
            List<TransferView> ls = new List<TransferView>();
            foreach(var item in items.Select(e=>e.Stt).Distinct())
            {
                ls.AddRange(items.Where(e=>e.Stt == item).ToList());
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
                    _db.Database.ExecuteSqlRaw("update OWTR set U_MaPhieuBaoTri = {0} where DocEntry = {1}", item.ToString(), item);
                }
                ls = new List<TransferView>();
            } 
            return true;
        }
        public async Task<bool> GetIssue()
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
                    _db.Database.ExecuteSqlRaw("update OIGE set U_POS = {0} where DocEntry = {1}", item.ToString(), item);
                }
                ls = new List<GoodIssueView>();
            }
            return true;
        }
        public async Task<bool> GetGoodReceipt()
        {
            var items = await _db.GoodReceiptView.ToListAsync();
            List<GoodReceiptView> ls = new List<GoodReceiptView>();
            foreach (var item in items.Select(e => e.Stt).Distinct())
            {
                ls.AddRange(items.Where(e => e.Stt == item).ToList());
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
                    _db.Database.ExecuteSqlRaw("update OIGN set U_POS = {0} where DocEntry = {1}", item.ToString(), item);
                }
                ls = new List<GoodReceiptView>();
            }
            return true;
        }
    }
}
