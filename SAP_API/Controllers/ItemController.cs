using Gridify;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Model;
using SAP_API.Service;

namespace SAP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemController : Controller
    {
        private readonly ItemService _oitmService;
        public ItemController(ItemService oitmService)
        {
            _oitmService = oitmService;
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> getItem()
        {
            await _oitmService.GetItemMasterData();
            return Ok();
        }
        [HttpGet("price")]
        [AllowAnonymous]
        public async Task<IActionResult> getItemPrice()
        {
            await _oitmService.GetItemPrice();
            return Ok();
        }
        [HttpGet("item")]
        [AllowAnonymous]
        public async Task<IActionResult> getItemEcomaint()
        {
            await _oitmService.GetTransfer();
            return Ok();
        }
        [HttpGet("goodIssue")]
        [AllowAnonymous]
        public async Task<IActionResult> getGoodIssue()
        {
            await _oitmService.GetIssue();
            return Ok();
        }
        [HttpGet("goodReceipt")]
        [AllowAnonymous]
        public async Task<IActionResult> getGoodReceippt()
        {
            await _oitmService.GetGoodReceipt();
            return Ok();
        }
        [AllowAnonymous]
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailable([FromQuery] GridifyQuery q, [FromQuery] string? ItemCode)
        {
            List<string> itemCodes = new List<string>() ;
            if (!string.IsNullOrWhiteSpace(ItemCode))
                itemCodes  = ItemCode
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();


            var (mess, data, total) = await _oitmService.GetItemOnhandByLocationEcoAsync(q,itemCodes);
            if (mess != null)
            {
                return BadRequest(mess);
            }


            return Ok(new { data, total });
        }
    }
}
