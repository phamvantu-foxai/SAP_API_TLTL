using Gridify;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Model;
using SAP_API.Service;

namespace SAP_API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("2.0")]
    public class OrderController : Controller
    {
        private readonly OrderService _sapService;

        public OrderController(OrderService sapService)
        {
            _sapService = sapService;
        }
        [Authorize]
        [HttpGet("Closed")]
        public async Task<IActionResult> GetClosed(DateTime? fromDate, DateTime? toDate, string? listDocEntry,[FromQuery] GridifyQuery q)
        {

            List<string> items = new List<string>();
            if (!string.IsNullOrWhiteSpace(listDocEntry))
                items = listDocEntry
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            var (mess, data, total) = await _sapService.GetOrderClosedAsync(q, fromDate, toDate, items);
            if (mess != null)
            {
                return BadRequest(mess);
            }
            return Ok(new {data, total});
        }
        [Authorize]
        [HttpGet("Agreement")]
        public async Task<IActionResult> GetAgreement([FromQuery] GridifyQuery q)
        {
            var (mess, data, total) = await _sapService.GetAgreementAsync(q);
            if (mess != null)
            {
                return BadRequest(mess);
            }
            return Ok(new { data, total });
        }
        [Authorize]
        [HttpPost("Create")]
        public async Task<IActionResult> CreateOrder(Documents doc)
        {
            var (mess, DocEntry) = await _sapService.CreateOrder(doc);
            if (mess != null)
            {
                return BadRequest(mess);
            }
            return Ok(new { DocEntry });
        }
    }
}
