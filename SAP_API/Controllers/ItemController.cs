using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        
    }
}
