using Gridify;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Model;
using SAP_API.Service;

namespace SAP_API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("2.0")]
    public class ItemMasterDataController : Controller
    {
        private readonly ItemService _sapService;

        public ItemMasterDataController(ItemService sapService)
        {
            _sapService = sapService;
        }
        [Authorize]
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailable([FromQuery] string ItemCode)
        {
            if (string.IsNullOrWhiteSpace(ItemCode))
                return BadRequest("Vui lòng truyền danh sách ItemCode, ví dụ: ?itemCode=Item1,Item2");

            // Tách danh sách item codes từ query string
            var itemCodes = ItemCode
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();


            var (mess,data) = await _sapService.GetItemOnhandByLocationAsync(itemCodes);
            if (mess != null)
            {
                return BadRequest(mess);
            }
            var result = data
                .GroupBy(x => x.ItemCode)
                .Select(g => new ItemView
                {
                    ItemCode = g.Key,
                    OITW = g.Select(x => new OITW
                    {
                        Warehouse = x.Warehouse,
                        OnHand = x.OnHand,
                        IsCommited = x.IsCommited,
                        OnOrder = x.OnOrder,
                        Available = x.Available
                    }).ToList()
                }).ToList();

            return Ok(result);
        }
        [Authorize]
        [HttpGet()]
        public async Task<IActionResult> GetItemInfor([FromQuery] GridifyQuery q,[FromQuery] string? ItemCode)
        {
            List<string> items = new List<string>();
            if (! string.IsNullOrWhiteSpace(ItemCode))
                items = ItemCode
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();


            var (mess, data, total) = await _sapService.GetItemInforAsync(q, items);
            if (mess != null)
            {
                return BadRequest(mess);
            }
            return Ok(new { data , total});
        }
    }
}
