using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAP_API.Data;
using SAP_API.Model;
using SAP_API.Service;

namespace SAP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GoodReceiptController : ControllerBase
    {
        private readonly OWTRService _owtrService;
        public GoodReceiptController(OWTRService owtrService)

        {
            _owtrService = owtrService;
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> getByGoodReceipt([FromBody] Request request)
        {
            var (mess, owtr) = await _owtrService.GetOwtrDIAPIAsync(request.DocNumber ?? "", request.Store);
            if (mess != null)
            {
                return BadRequest(mess);
            }
            return Ok(owtr);
        }
        [HttpPut]
        [AllowAnonymous]
        public async Task<IActionResult> updateByGoodReceipt([FromBody] Request request)
        {
            var mess = await _owtrService.UpdateOwtrDIAPI(request.DocNumber, request.Store, request.DocnumberPOS);
            if (mess != null)
            {
                return BadRequest(mess);
            }
            return Ok();
        }
    }
}
