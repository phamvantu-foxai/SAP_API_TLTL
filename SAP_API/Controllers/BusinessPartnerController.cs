using Gridify;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SAP_API.Model;
using SAP_API.Service;

namespace SAP_API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("2.0")]
    public class BusinessPartnerController : Controller
    {
        private readonly OWTRService _sapService;

        public BusinessPartnerController(OWTRService sapService)
        {
            _sapService = sapService;
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> addARInvoice([FromQuery] GridifyQuery q)
        {
            var(mess, ocrd, total) = await _sapService.GetOCRDAsync(q);
            if(mess != null)
            {
                return BadRequest(mess);
            }    
            return Ok(new { ocrd , total});
        }
    }
}
